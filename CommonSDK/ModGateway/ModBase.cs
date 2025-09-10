using CommonSDK.Logger;
using Godot;

namespace CommonSDK.ModGateway;

/// <summary>
/// 模组基类
/// <para>提供模组的基础实现和单例模式</para>
/// <para>所有模组都应继承此类</para>
/// </summary>
/// <typeparam name="T">具体的模组类型</typeparam>
public partial class ModBase<T> : Node, IMod where T : ModBase<T>, new()
{
    private static readonly Lazy<T> _instance = new(() => new T());
    public static readonly LogHelper Logger = new(typeof(T).Name);

    /// <summary>
    /// 获取模组单例实例
    /// </summary>
    public static T Instance => _instance.Value;

    /// <summary>
    /// 模组ID
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 模组版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 模组作者
    /// </summary>
    public string[] Authors { get; set; } = [];
    
    /// <summary>
    /// 模组作者
    /// </summary>
    public string Author 
    { 
        get => Authors.Length > 0 ? string.Join(", ", Authors) : string.Empty;
        set => Authors = string.IsNullOrEmpty(value) ? [] : [value];
    }

    /// <summary>
    /// 模组描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 🎯 异步初始化模组
    /// </summary>
    public virtual Task InitAsync()
    {
        Logger.LogInfo($"模组 {ModId} 初始化中...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 🎯 异步启动模组
    /// </summary>
    public virtual Task StartAsync()
    {
        Logger.LogInfo($"模组 {ModId} 启动中...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 模组循环更新
    /// </summary>
    public virtual void Loop(double delta)
    {
        // 子类实现
    }

    /// <summary>
    /// 模组物理循环更新
    /// </summary>
    public virtual void PhysicsLoop(double delta)
    {
        // 子类实现
    }

    /// <summary>
    /// 🎯 线程安全的添加子节点方法
    /// </summary>
    protected async Task CallDeferredAddChildAsync(Node child)
    {
        var taskManager = ModGateway.GetTaskManager();
        if (taskManager == null)
        {
            Logger.LogError("TaskManager 未初始化，无法执行线程安全操作");
            return;
        }

        await taskManager.CallDeferredAsync(() => AddChild(child));
    }

    /// <summary>
    /// 🎯 线程安全的移除子节点方法
    /// </summary>
    protected async Task CallDeferredRemoveChildAsync(Node child)
    {
        var taskManager = ModGateway.GetTaskManager();
        if (taskManager == null)
        {
            Logger.LogError("TaskManager 未初始化，无法执行线程安全操作");
            return;
        }

        await taskManager.CallDeferredAsync(() => RemoveChild(child));
    }

    /// <summary>
    /// 🎯 线程安全的执行任意主线程操作
    /// </summary>
    protected async Task CallDeferredAsync(Action action)
    {
        var taskManager = ModGateway.GetTaskManager();
        if (taskManager == null)
        {
            Logger.LogError("TaskManager 未初始化，无法执行线程安全操作");
            return;
        }

        await taskManager.CallDeferredAsync(action);
    }

    /// <summary>
    /// 🎯 线程安全的执行任意主线程操作（带返回值）
    /// </summary>
    protected async Task<TResult> CallDeferredAsync<TResult>(Func<TResult> func)
    {
        var taskManager = ModGateway.GetTaskManager();
        if (taskManager == null)
        {
            Logger.LogError("TaskManager 未初始化，无法执行线程安全操作");
            return default(TResult);
        }

        return await taskManager.CallDeferredAsync(func);
    }
}