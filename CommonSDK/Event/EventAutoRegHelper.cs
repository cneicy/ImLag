using System.Collections.Concurrent;
using System.Reflection;
using CommonSDK.Logger;
using Godot;

namespace CommonSDK.Event;

/// <summary>
/// 静态自动EventBus注册管理器
/// </summary>
/// <remarks>
/// <para>负责自动发现和注册Godot节点中的事件处理程序</para>
/// <para>主要功能包括:</para>
/// <list type="bullet">
/// <item><description>扫描程序集，发现标记了EventBusSubscriber特性的Node类型</description></item>
/// <item><description>监控场景树的变化，自动注册新增的订阅者节点</description></item>
/// <item><description>在节点移除时自动清理注册信息</description></item>
/// <item><description>提供统计和调试信息</description></item>
/// </list>
/// <para>这个类在EventBus系统启动时自动初始化，无需手动调用</para>
/// <para>线程安全设计，支持并发操作</para>
/// </remarks>
public static class EventAutoRegHelper
{
    /// <summary>
    /// 存储发现的订阅者类型
    /// </summary>
    /// <remarks>
    /// 缓存所有标记了EventBusSubscriber特性且继承自Node的类型。
    /// 使用ConcurrentDictionary确保线程安全。
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, bool> SubscriberTypes = new();
    
    /// <summary>
    /// 存储已注册的节点实例
    /// </summary>
    /// <remarks>
    /// 跟踪已经注册到EventBus的节点实例，防止重复注册。
    /// 使用ConcurrentDictionary确保线程安全。
    /// </remarks>
    private static readonly ConcurrentDictionary<Node, bool> RegisteredNodes = new();
    
    /// <summary>
    /// 存储已处理的节点ID
    /// </summary>
    /// <remarks>
    /// 使用节点的InstanceId跟踪已处理的节点，避免重复处理。
    /// 这是一个额外的安全措施，防止节点引用变化导致的问题。
    /// </remarks>
    private static readonly ConcurrentDictionary<ulong, bool> ProcessedNodeIds = new();
    
    /// <summary>
    /// 场景树引用
    /// </summary>
    /// <remarks>
    /// 缓存的SceneTree引用，用于监听节点变化事件。
    /// 可能为null，在初始化完成前。
    /// </remarks>
    private static SceneTree? _sceneTree;
    
    /// <summary>
    /// 是否正在监控场景树变化
    /// </summary>
    private static bool _isMonitoring;
    
    /// <summary>
    /// 初始化操作的线程安全锁
    /// </summary>
    private static readonly object InitLock = new();
    
    /// <summary>
    /// 日志记录器实例
    /// </summary>
    private static readonly LogHelper Logger = new("EventAutoRegHelper");
    
    /// <summary>
    /// 静态构造函数，启动异步初始化过程
    /// </summary>
    /// <remarks>
    /// 在第一次访问EventAutoRegHelper时自动执行。
    /// 启动后台任务来异步初始化整个自动注册系统。
    /// </remarks>
    static EventAutoRegHelper()
    {
        // 启动异步初始化
        _ = Task.Run(InitializeAsync);
    }
    
    /// <summary>
    /// 异步初始化方法
    /// </summary>
    /// <returns>表示异步初始化操作的Task</returns>
    /// <remarks>
    /// <para>等待Godot引擎和场景树准备就绪后开始初始化</para>
    /// <para>使用CallDeferred确保初始化在主线程执行</para>
    /// <para>包含超时机制，防止无限等待</para>
    /// </remarks>
    private static async Task InitializeAsync()
    {
        // 等待引擎和场景树准备就绪
        var maxWaitTime = 5000; // 最大等待5秒
        var waited = 0;
        
        while (Engine.GetMainLoop() == null && waited < maxWaitTime)
        {
            await Task.Delay(50);
            waited += 50;
        }
        
        if (Engine.GetMainLoop() == null)
        {
            Logger.LogError("等待场景树超时，自动注册管理器初始化失败");
            return;
        }
        
        Callable.From(InitializeSystem).CallDeferred();
    }
    
    /// <summary>
    /// 初始化系统的主要方法
    /// </summary>
    /// <remarks>
    /// <para>在主线程中执行的初始化逻辑，包括:</para>
    /// <list type="bullet">
    /// <item><description>获取SceneTree引用</description></item>
    /// <item><description>扫描程序集发现订阅者类型</description></item>
    /// <item><description>启动场景树变化监控</description></item>
    /// <item><description>扫描现有节点进行注册</description></item>
    /// </list>
    /// <para>使用锁确保只初始化一次</para>
    /// </remarks>
    private static void InitializeSystem()
    {
        lock (InitLock)
        {
            if (IsInitialized) return;
            
            _sceneTree = Engine.GetMainLoop() as SceneTree;
            if (_sceneTree == null)
            {
                Logger.LogError("无法获取SceneTree");
                return;
            }
            
            // 扫描所有EventBus订阅者类型
            ScanEventBusSubscribers();
            
            // 启动监控
            StartMonitoring();
            
            // 扫描现有节点
            ScanExistingNodes();
            
            IsInitialized = true;
        }
    }
    
    /// <summary>
    /// 扫描现有场景树中的所有节点
    /// </summary>
    /// <remarks>
    /// 遍历当前场景树的根节点及其所有子节点，
    /// 对符合条件的节点执行自动注册。
    /// </remarks>
    private static void ScanExistingNodes()
    {
        if (_sceneTree?.Root == null) return;
        TraverseAndRegister(_sceneTree.Root);
    }
    
    /// <summary>
    /// 确保自动注册管理器已完成初始化
    /// </summary>
    /// <remarks>
    /// 公共方法，用于在需要时强制完成初始化。
    /// 如果已经初始化则直接返回，否则尝试立即初始化。
    /// 主要用于EventBus系统的启动流程。
    /// </remarks>
    public static void EnsureInitialized()
    {
        if (IsInitialized) return;
        lock (InitLock)
        {
            if (!IsInitialized && Engine.GetMainLoop() is SceneTree)
            {
                InitializeSystem();
            }
        }
    }
    
    /// <summary>
    /// 扫描程序集发现EventBus订阅者类型
    /// </summary>
    /// <remarks>
    /// <para>遍历当前应用程序域的所有程序集，查找符合条件的类型:</para>
    /// <list type="bullet">
    /// <item><description>标记了EventBusSubscriberAttribute特性</description></item>
    /// <item><description>继承自Godot.Node类</description></item>
    /// </list>
    /// <para>跳过系统程序集以提高扫描性能</para>
    /// <para>发现的类型会被缓存到SubscriberTypes字典中</para>
    /// </remarks>
    private static void ScanEventBusSubscribers()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                if (IsSystemAssembly(assembly)) continue;
                
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetCustomAttribute<EventBusSubscriberAttribute>() != null &&
                        typeof(Node).IsAssignableFrom(type))
                    {
                        SubscriberTypes.TryAdd(type, true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (OS.IsDebugBuild())
                    Logger.LogError($"扫描程序集失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 检查是否为系统程序集
    /// </summary>
    /// <param name="assembly">要检查的程序集</param>
    /// <returns>如果是系统程序集返回true，否则返回false</returns>
    /// <remarks>
    /// 识别系统级程序集，在扫描时跳过这些程序集以提高性能。
    /// 系统程序集通常不包含用户定义的事件订阅者。
    /// </remarks>
    private static bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.FullName ?? "";
        return name.StartsWith("System.") || 
               name.StartsWith("Microsoft.") || 
               name.StartsWith("mscorlib") ||
               name.StartsWith("netstandard") ||
               name.StartsWith("Godot.");
    }
    
    /// <summary>
    /// 启动场景树变化监控
    /// </summary>
    /// <remarks>
    /// <para>注册场景树的事件监听器:</para>
    /// <list type="bullet">
    /// <item><description>NodeAdded: 节点添加时触发</description></item>
    /// <item><description>NodeRemoved: 节点移除时触发</description></item>
    /// <item><description>TreeChanged: 树结构变化时触发</description></item>
    /// </list>
    /// <para>确保只启动一次监控，避免重复订阅</para>
    /// </remarks>
    private static void StartMonitoring()
    {
        if (_sceneTree == null || _isMonitoring) return;
    
        // 监听节点添加事件
        _sceneTree.NodeAdded += OnNodeAdded;
    
        // 监听节点移除事件
        _sceneTree.NodeRemoved += OnNodeRemoved;
    
        // 监听树结构变化
        _sceneTree.TreeChanged += OnTreeChanged;
    
        _isMonitoring = true;
    }
    
    /// <summary>
    /// 处理节点添加事件
    /// </summary>
    /// <param name="node">新添加的节点</param>
    /// <remarks>
    /// 当场景树中添加新节点时被调用。
    /// 检查节点类型是否为订阅者，如果是则自动注册。
    /// </remarks>
    private static void OnNodeAdded(Node node)
    {
        if (node == null) return;
        
        // 直接处理节点注册
        ProcessNodeRegistration(node);
    }
    
    /// <summary>
    /// 处理场景树结构变化事件
    /// </summary>
    /// <remarks>
    /// 当场景树结构发生变化时被调用。
    /// 目前实现为空，预留给未来可能的扩展需求。
    /// </remarks>
    private static void OnTreeChanged()
    {
        
    }
    
    /// <summary>
    /// 处理节点移除事件
    /// </summary>
    /// <param name="node">被移除的节点</param>
    /// <remarks>
    /// 当节点从场景树中移除时被调用。
    /// 自动清理该节点的所有注册信息，防止内存泄漏。
    /// </remarks>
    private static void OnNodeRemoved(Node node)
    {
        if (node == null) return;
    
        // 自动清理注册记录
        UnregisterNode(node);
    }
    
    /// <summary>
    /// 处理节点注册逻辑
    /// </summary>
    /// <param name="node">要处理的节点</param>
    /// <remarks>
    /// <para>节点注册的核心处理逻辑，包括:</para>
    /// <list type="bullet">
    /// <item><description>检查节点是否有效且未被标记为删除</description></item>
    /// <item><description>验证节点类型是否为EventBus订阅者</description></item>
    /// <item><description>防止重复处理同一个节点</description></item>
    /// <item><description>执行实际的注册操作</description></item>
    /// </list>
    /// <para>使用try-catch包装以确保异常不会影响其他节点的处理</para>
    /// </remarks>
    private static void ProcessNodeRegistration(Node node)
    {
        try
        {
            if (node == null || node.IsQueuedForDeletion()) return;
            
            var nodeType = node.GetType();
            var instanceId = node.GetInstanceId();
            
            // 检查是否是EventBus订阅者
            if (!SubscriberTypes.ContainsKey(nodeType)) 
            {
                return;
            }
            
            // 避免重复处理
            if (ProcessedNodeIds.ContainsKey(instanceId)) 
            {
                return;
            }
            
            // 标记为已处理
            ProcessedNodeIds.TryAdd(instanceId, true);
            
            // 立即注册
            RegisterNode(node);
        }
        catch (Exception)
        {
            // 静默处理异常，防止影响其他节点的处理
        }
    }
    
    /// <summary>
    /// 注册单个节点到EventBus系统
    /// </summary>
    /// <param name="node">要注册的节点</param>
    /// <remarks>
    /// <para>执行实际的节点注册操作:</para>
    /// <list type="bullet">
    /// <item><description>检查是否已经注册过，避免重复注册</description></item>
    /// <item><description>调用EventBus.AutoRegister进行实际注册</description></item>
    /// <item><description>更新注册记录，用于后续的重复检查</description></item>
    /// <item><description>在注册失败时进行错误处理和清理</description></item>
    /// </list>
    /// <para>线程安全操作，使用锁保护注册状态</para>
    /// </remarks>
    private static void RegisterNode(Node node)
    {
        if (node == null) return;
        
        lock (RegisteredNodes)
        {
            // 再次检查避免重复注册
            if (RegisteredNodes.ContainsKey(node)) 
            {
                return;
            }
            
            try
            {
                RegisteredNodes.TryAdd(node, true);
                
                EventBus.AutoRegister(node);
            }
            catch (Exception ex)
            {
                RegisteredNodes.TryRemove(node, out _);
                Logger.LogError($"自动注册失败 {node.GetType().FullName}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 获取系统统计信息
    /// </summary>
    /// <returns>包含各项统计数据的元组</returns>
    /// <remarks>
    /// <para>返回当前系统状态的统计信息，包括:</para>
    /// <list type="bullet">
    /// <item><description>SubscriberTypes: 发现的订阅者类型数量</description></item>
    /// <item><description>RegisteredNodes: 已注册的节点数量</description></item>
    /// <item><description>ProcessedNodes: 已处理的节点数量</description></item>
    /// <item><description>EventBusInstances: EventBus中的实例数量</description></item>
    /// <item><description>EventTypes: EventBus中的事件类型数量</description></item>
    /// </list>
    /// <para>主要用于监控和调试目的</para>
    /// </remarks>
    public static (int SubscriberTypes, int RegisteredNodes, int ProcessedNodes, int EventBusInstances, int EventTypes) GetStatistics()
    {
        return (
            SubscriberTypes.Count, 
            RegisteredNodes.Count, 
            ProcessedNodeIds.Count,
            EventBus.GetRegisteredInstanceCount(),
            EventBus.GetRegisteredEventTypeCount()
        );
    }
    
    /// <summary>
    /// 清理指定节点的注册记录
    /// </summary>
    /// <param name="node">要清理的节点</param>
    /// <remarks>
    /// <para>完全清理节点的所有注册信息:</para>
    /// <list type="bullet">
    /// <item><description>从RegisteredNodes中移除节点记录</description></item>
    /// <item><description>从ProcessedNodeIds中移除节点ID记录</description></item>
    /// <item><description>调用EventBus.UnregisterInstance清理EventBus注册</description></item>
    /// </list>
    /// <para>这个方法通常在节点从场景树移除时自动调用</para>
    /// <para>也可以手动调用来强制清理特定节点</para>
    /// </remarks>
    public static void UnregisterNode(Node node)
    {
        if (node == null) return;
        
        lock (RegisteredNodes)
        {
            RegisteredNodes.TryRemove(node, out _);
            ProcessedNodeIds.TryRemove(node.GetInstanceId(), out _);
        }
        
        EventBus.UnregisterInstance(node);
    }
    
    /// <summary>
    /// 手动强制扫描当前场景中的所有节点
    /// </summary>
    /// <remarks>
    /// <para>强制扫描并注册当前场景中的所有符合条件的节点</para>
    /// <para>这个方法在以下情况下可能有用:</para>
    /// <list type="bullet">
    /// <item><description>自动注册系统初始化失败时的备用方案</description></item>
    /// <item><description>需要重新扫描场景以确保所有节点都被注册</description></item>
    /// <item><description>调试和测试场景下的手动触发</description></item>
    /// </list>
    /// <para>会输出开始和完成的日志信息</para>
    /// </remarks>
    public static void ForceScanCurrentScene()
    {
        if (_sceneTree?.CurrentScene == null) return;
        
        Logger.LogInfo("开始强制扫描当前场景");
        TraverseAndRegister(_sceneTree.CurrentScene);
        Logger.LogInfo("强制扫描完成");
    }
    
    /// <summary>
    /// 递归遍历节点树并注册符合条件的节点
    /// </summary>
    /// <param name="node">要遍历的根节点</param>
    /// <remarks>
    /// <para>深度优先遍历节点树，对每个节点执行以下操作:</para>
    /// <list type="bullet">
    /// <item><description>处理当前节点的注册</description></item>
    /// <item><description>递归处理所有子节点</description></item>
    /// </list>
    /// <para>这确保了整个节点树中的所有符合条件的节点都会被发现和注册</para>
    /// </remarks>
    private static void TraverseAndRegister(Node node)
    {
        if (node == null) return;
        
        // 处理当前节点
        ProcessNodeRegistration(node);
        
        // 递归处理子节点
        foreach (var child in node.GetChildren())
        {
            TraverseAndRegister(child);
        }
    }
    
    /// <summary>
    /// 获取管理器是否已完成初始化
    /// </summary>
    /// <value>如果已初始化返回true，否则返回false</value>
    /// <remarks>
    /// 表示EventAutoRegHelper是否已完成完整的初始化过程。
    /// 只有初始化完成后，自动注册功能才会正常工作。
    /// </remarks>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// 清理所有资源并重置管理器状态
    /// </summary>
    /// <remarks>
    /// <para>完全清理自动注册管理器的所有资源:</para>
    /// <list type="bullet">
    /// <item><description>停止场景树变化监控</description></item>
    /// <item><description>清理所有缓存的注册记录</description></item>
    /// <item><description>重置初始化状态</description></item>
    /// <item><description>释放场景树引用</description></item>
    /// </list>
    /// <para>主要用于系统关闭或需要完全重置时</para>
    /// <para>清理后需要重新初始化才能正常使用</para>
    /// </remarks>
    public static void Cleanup()
    {
        lock (InitLock)
        {
            // 停止监控
            if (_sceneTree != null && _isMonitoring)
            {
                _sceneTree.NodeAdded -= OnNodeAdded;
                _sceneTree.NodeRemoved -= OnNodeRemoved;
                _sceneTree.TreeChanged -= OnTreeChanged;
            }
            
            // 清理所有记录
            RegisteredNodes.Clear();
            ProcessedNodeIds.Clear();
            SubscriberTypes.Clear();
            
            IsInitialized = false;
            _isMonitoring = false;
            _sceneTree = null;
            
            Logger.LogInfo("静态自动注册管理器已清理");
        }
    }
    
    /// <summary>
    /// 获取详细的统计信息字符串（用于调试）
    /// </summary>
    /// <returns>格式化的详细统计信息字符串</returns>
    /// <remarks>
    /// <para>返回包含完整系统状态的详细报告，包括:</para>
    /// <list type="bullet">
    /// <item><description>基本统计数据（实例数、事件类型数等）</description></item>
    /// <item><description>系统状态（是否已初始化、是否正在监控）</description></item>
    /// <item><description>已发现的订阅者类型列表</description></item>
    /// </list>
    /// <para>主要用于调试和监控，提供系统运行状态的全面视图</para>
    /// </remarks>
    public static string GetDetailedStatistics()
    {
        var stats = GetStatistics();
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("EventAutoRegHelper 统计");
        sb.AppendLine($"订阅者类型数: {stats.SubscriberTypes}");
        sb.AppendLine($"已注册节点数: {stats.RegisteredNodes}");
        sb.AppendLine($"已处理节点数: {stats.ProcessedNodes}");
        sb.AppendLine($"EventBus实例数: {stats.EventBusInstances}");
        sb.AppendLine($"EventBus事件类型数: {stats.EventTypes}");
        sb.AppendLine($"是否已初始化: {IsInitialized}");
        sb.AppendLine($"是否正在监控: {_isMonitoring}");
        
        sb.AppendLine("\n已发现的订阅者类型:");
        foreach (var type in SubscriberTypes.Keys)
        {
            sb.AppendLine($"  - {type.FullName}");
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// Node类的EventBus扩展方法
/// </summary>
/// <remarks>
/// <para>为Godot Node类提供便捷的EventBus操作方法</para>
/// <para>这些扩展方法简化了节点与EventBus系统的交互</para>
/// <para>所有方法都是线程安全的</para>
/// </remarks>
public static class NodeEventBusExtensions
{
    /// <summary>
    /// 通知节点已被移除，清理相关注册记录
    /// </summary>
    /// <param name="node">被移除的节点</param>
    /// <remarks>
    /// <para>手动通知自动注册管理器节点已被移除</para>
    /// <para>通常情况下不需要手动调用，系统会自动处理节点移除事件</para>
    /// <para>但在某些特殊情况下（如手动管理节点生命周期）可能需要显式调用</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 在手动删除节点时调用
    /// someNode.NotifyNodeRemoved();
    /// someNode.QueueFree();
    /// </code>
    /// </example>
    public static void NotifyNodeRemoved(this Node node)
    {
        EventAutoRegHelper.UnregisterNode(node);
    }
    
    /// <summary>
    /// 检查节点是否已在EventBus中注册
    /// </summary>
    /// <param name="node">要检查的节点</param>
    /// <returns>如果已注册返回true，否则返回false</returns>
    /// <remarks>
    /// 查询节点是否已经通过AutoRegister方法注册到EventBus系统中。
    /// 这个方法用于调试和状态检查，帮助确认自动注册是否正常工作。
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void _Ready()
    /// {
    ///     // 检查自动注册状态
    ///     if (this.IsEventBusRegistered())
    ///     {
    ///         GD.Print("节点已自动注册到EventBus");
    ///     }
    ///     else
    ///     {
    ///         GD.Print("节点未注册，可能需要手动注册");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static bool IsEventBusRegistered(this Node node)
    {
        return EventBus.IsInstanceRegistered(node);
    }
    
    /// <summary>
    /// 手动从EventBus注销节点
    /// </summary>
    /// <param name="node">要注销的节点</param>
    /// <remarks>
    /// <para>完全从EventBus系统中移除节点的所有注册信息</para>
    /// <para>包括以下操作:</para>
    /// <list type="bullet">
    /// <item><description>移除节点的所有事件处理程序</description></item>
    /// <item><description>清理EventBus中的实例记录</description></item>
    /// <item><description>清理自动注册管理器中的记录</description></item>
    /// </list>
    /// <para>通常在节点提前销毁或需要停止接收事件时调用</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void _ExitTree()
    /// {
    ///     // 确保清理所有EventBus注册
    ///     this.UnregisterFromEventBus();
    ///     base._ExitTree();
    /// }
    /// </code>
    /// </example>
    public static void UnregisterFromEventBus(this Node node)
    {
        EventBus.UnregisterInstance(node);
        EventAutoRegHelper.UnregisterNode(node);
    }
    
    /// <summary>
    /// 手动注册节点到EventBus
    /// </summary>
    /// <param name="node">要注册的节点</param>
    /// <remarks>
    /// <para>手动将节点注册到EventBus系统中</para>
    /// <para>这个方法在以下情况下可能有用:</para>
    /// <list type="bullet">
    /// <item><description>自动注册系统未能及时注册节点</description></item>
    /// <item><description>需要在特定时机手动触发注册</description></item>
    /// <item><description>调试和测试场景下的手动控制</description></item>
    /// </list>
    /// <para>如果节点已经注册过，此操作不会产生副作用</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void _Ready()
    /// {
    ///     base._Ready();
    ///     
    ///     // 确保节点已注册到EventBus
    ///     if (!this.IsEventBusRegistered())
    ///     {
    ///         this.RegisterToEventBus();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static void RegisterToEventBus(this Node node)
    {
        EventBus.AutoRegister(node);
    }
}