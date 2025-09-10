using System.Reflection;
using CommonSDK.Logger;
using CommonSDK.Utils;
using Godot;
// ReSharper disable InconsistentlySynchronizedField
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

// ReSharper disable StaticMemberInGenericType
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // 可能返回 null 引用。

namespace CommonSDK;

/// <summary>
/// 通用单例模式基类
/// <para>提供自动创建、管理和销毁单例实例的功能</para>
/// <para>支持从场景文件、脚本文件或直接实例化创建单例</para>
/// </summary>
/// <typeparam name="T">继承自Singleton的具体类型</typeparam>
public abstract partial class Singleton<T> : Node where T : Singleton<T>, new()
{
    /// <summary>
    /// 单例实例
    /// </summary>
    private static T _instance;

    /// <summary>
    /// 线程锁对象，用于确保单例创建的线程安全
    /// </summary>
    private static readonly object Lock = new();

    /// <summary>
    /// 标记应用程序是否正在退出
    /// </summary>
    private static bool _isQuitting;

    /// <summary>
    /// 日志帮助类实例
    /// </summary>
    private static readonly LogHelper LogHelper = new($"Singleton Of {typeof(T).Name}");

    static Singleton()
    {
        var attr = typeof(T).GetCustomAttribute<SingletonAttribute>();
        var isEagerLoad = attr?.LoadMode == LoadMode.Eager;

        if (isEagerLoad)
        {
            // 强加载：立即创建实例
            lock (Lock)
            {
                if (_instance == null && !_isQuitting)
                {
                    CreateInstance();
                }
            }
        }
    }
    
    /// <summary>
    /// 获取单例实例
    /// <para>如果实例不存在且应用未退出，则自动创建实例</para>
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;
            lock (Lock)
            {
                if (_instance == null && !_isQuitting)
                {
                    CreateInstance();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 检查单例实例是否已存在
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// 创建单例实例
    /// <para>优先查找现有实例，如果不存在则创建新实例</para>
    /// </summary>
    private static void CreateInstance()
    {
        var mainLoop = Engine.GetMainLoop();
        if (mainLoop is not SceneTree sceneTree)
        {
            LogHelper.LogError($"{typeof(T).Name}创建失败: 场景树不可用");
            return;
        }
        
        var root = sceneTree.Root;
        var existingInstance = root.FindObjectOfType<T>();
        if (existingInstance != null)
        {
            _instance = existingInstance;
            LogHelper.LogInfo($"{typeof(T).Name} 找到现有实例，路径为{_instance.GetPath()}");
            return;
        }

        // 使用扩展方法通过组查找实例
        var groupInstance = root.FindWithGroup<T>($"singleton_{typeof(T).Name}");
        if (groupInstance != null)
        {
            _instance = groupInstance;
            LogHelper.LogInfo($"{typeof(T).Name} 通过组找到现有实例，路径为{_instance.GetPath()}");
            return;
        }

        _instance = CreateNewInstance();

        if (_instance == null)
        {
            LogHelper.LogError($"{typeof(T).Name} 创建失败");
            return;
        }

        var rootNode = sceneTree.Root.GetNode("/root");
        rootNode.CallDeferred(Node.MethodName.AddChild, _instance);

        _instance.AddToGroup($"singleton_{typeof(T).Name}");
        _instance.ProcessMode = ProcessModeEnum.Always;

        _instance.CallDeferred(MethodName.PrintInstancePath);
    }

    /// <summary>
    /// 创建新的单例实例
    /// <para>按优先级尝试：1.从场景文件加载 2.从脚本文件加载 3.直接实例化</para>
    /// </summary>
    /// <returns>创建的单例实例</returns>
    private static T CreateNewInstance()
    {
        var scenePath = $"res://singletons/{typeof(T).Name}.tscn";
        if (ResourceLoader.Exists(scenePath))
        {
            var scene = GD.Load<PackedScene>(scenePath);
            if (scene.Instantiate() is T instance)
            {
                LogHelper.LogInfo($"{typeof(T).Name} 从场景文件创建");
                return instance;
            }
        }

        var scriptPath = $"res://scripts/{typeof(T).Name}.cs";
        if (ResourceLoader.Exists(scriptPath))
        {
            var script = GD.Load<Script>(scriptPath);
            var node = new Node();
            node.SetScript(script);
            var instance = node as T;
            if (instance != null)
            {
                LogHelper.LogInfo($"{typeof(T).Name} 通过脚本创建");
                return instance;
            }
        }

        LogHelper.LogInfo($"{typeof(T).Name} 直接创建（可能缺少脚本）");
        return new T();
    }

    /// <summary>
    /// 打印单例实例在场景树中的路径
    /// </summary>
    private void PrintInstancePath()
    {
        LogHelper.LogInfo($"{typeof(T).Name} 实例已经创建并添加到场景树，路径为{GetPath()}");
    }

    /// <summary>
    /// 节点就绪时的处理
    /// <para>确保单例实例的唯一性，销毁重复实例</para>
    /// </summary>
    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = (T)this;
            AddToGroup($"singleton_{typeof(T).Name}");
            ProcessMode = ProcessModeEnum.Always;
            LogHelper.LogInfo($"{typeof(T).Name} 已经从场景实例化");
        }
        else if (_instance != this)
        {
            LogHelper.LogError($"{typeof(T).Name} 实例已存在，正在销毁");
            QueueFree();
            return;
        }

        OnSingletonReady();
    }

    /// <summary>
    /// 节点退出场景树时的处理
    /// <para>清理单例实例</para>
    /// </summary>
    public override void _ExitTree()
    {
        if (_instance != this) return;
        OnSingletonDestroy();
        _isQuitting = true;
        _instance = null;
        LogHelper.LogInfo($"{typeof(T).Name} 实例已销毁");
    }

    /// <summary>
    /// 单例就绪时的回调方法
    /// <para>可由子类重写以实现自定义初始化逻辑</para>
    /// </summary>
    protected virtual void OnSingletonReady()
    {
    }

    /// <summary>
    /// 单例销毁前的回调方法
    /// <para>可由子类重写以实现自定义清理逻辑</para>
    /// </summary>
    protected virtual void OnSingletonDestroy()
    {
    }

    /// <summary>
    /// 手动销毁单例实例
    /// </summary>
    public static void DestroySingleton()
    {
        _instance?.QueueFree();
    }

    /// <summary>
    /// 处理Godot通知
    /// <para>监听窗口关闭请求，设置退出标志</para>
    /// </summary>
    /// <param name="what">通知类型</param>
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _isQuitting = true;
        }
    }
    
    /// <summary>
    /// 单例加载模式特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonAttribute : Attribute
    {
        public LoadMode LoadMode { get; }

        public SingletonAttribute(LoadMode loadMode)
        {
            LoadMode = loadMode;
        }
    }

    /// <summary>
    /// 加载模式枚举
    /// </summary>
    public enum LoadMode
    {
        /// <summary>
        /// 懒加载：第一次访问 Instance 时创建
        /// </summary>
        Lazy,

        /// <summary>
        /// 强加载：在程序启动时就创建实例
        /// </summary>
        Eager
    }
}