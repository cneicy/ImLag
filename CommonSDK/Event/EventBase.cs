using System.Reflection;

namespace CommonSDK.Event;

/// <summary>
/// 事件基类，所有自定义事件都应继承此类
/// </summary>
/// <remarks>
/// <para>支持取消、结果设置和优先级处理的增强事件基类</para>
/// <para>提供完整的事件生命周期管理，包括阶段控制、处理程序信息跟踪等</para>
/// <para>集成了监听器列表管理，支持运行时查询和调试</para>
/// <para>所有事件相关的核心功能都通过此基类提供</para>
/// </remarks>
/// <example>
/// <code>
/// [Cancelable, HasResult]
/// public class PlayerActionEvent : EventBase
/// {
///     public Player Player { get; set; }
///     public string Action { get; set; }
///     
///     public PlayerActionEvent(Player player, string action)
///     {
///         Player = player;
///         Action = action;
///     }
/// }
/// </code>
/// </example>
public abstract class EventBase
{
    /// <summary>
    /// 事件是否被取消的内部状态
    /// </summary>
    private bool _canceled;

    /// <summary>
    /// 事件处理结果的内部状态
    /// </summary>
    private EventResult _result = EventResult.DEFAULT;

    /// <summary>
    /// 事件是否可取消的标记
    /// </summary>
    private readonly bool _cancelable;

    /// <summary>
    /// 事件是否有结果的标记
    /// </summary>
    private readonly bool _hasResult;

    /// <summary>
    /// 当前事件处理阶段
    /// </summary>
    private EventPriority? _phase;

    /// <summary>
    /// 事件的监听器列表
    /// </summary>
    private readonly ListenerList _listenerList;

    /// <summary>
    /// 获取或设置当前正在处理此事件的处理程序信息
    /// </summary>
    /// <value>当前处理程序的详细信息，未在处理时为 null</value>
    /// <remarks>在事件处理过程中自动设置，处理完成后自动清空</remarks>
    public EventHandlerInfo? CurrentHandler { get; internal set; }

    /// <summary>
    /// 获取事件触发的时间戳
    /// </summary>
    /// <value>事件创建时的 UTC 时间</value>
    /// <remarks>用于性能分析、调试和事件追踪</remarks>
    public DateTime EventTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// 获取事件的唯一标识符
    /// </summary>
    /// <value>事件的全局唯一标识符</value>
    /// <remarks>每个事件实例都有唯一的 GUID，用于追踪和调试</remarks>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// 初始化 <see cref="EventBase"/> 类的新实例
    /// </summary>
    /// <remarks>
    /// <para>构造函数会自动检查事件类上的特性以确定功能支持</para>
    /// <para>初始化监听器列表并调用 Setup 方法进行额外设置</para>
    /// </remarks>
    protected EventBase()
    {
        _cancelable = GetType().GetCustomAttribute<CancelableAttribute>() != null;
        _hasResult = GetType().GetCustomAttribute<HasResultAttribute>() != null;
        _listenerList = new ListenerList();
        Setup();
    }

    /// <summary>
    /// 由基础构造函数调用，用于设置各种功能
    /// </summary>
    /// <remarks>
    /// <para>子类可以重写此方法进行额外设置</para>
    /// <para>在构造函数最后调用，此时所有基础属性已经初始化完成</para>
    /// </remarks>
    protected virtual void Setup()
    {
        // 子类可以重写此方法进行额外设置
    }

    /// <summary>
    /// 获取事件是否可以被取消
    /// </summary>
    /// <value>如果事件标记了 <see cref="CancelableAttribute"/> 则返回 true</value>
    /// <remarks>只有可取消的事件才能调用取消相关的方法</remarks>
    public bool IsCancelable => _cancelable;

    /// <summary>
    /// 获取事件是否有结果
    /// </summary>
    /// <value>如果事件标记了 <see cref="HasResultAttribute"/> 则返回 true</value>
    /// <remarks>只有有结果的事件才能设置处理结果</remarks>
    public bool HasResult => _hasResult;

    /// <summary>
    /// 获取或设置事件是否被取消
    /// </summary>
    /// <value>事件的取消状态</value>
    /// <exception cref="UnsupportedOperationException">当事件不支持取消时抛出</exception>
    /// <remarks>
    /// <para>只有标记了 <see cref="CancelableAttribute"/> 的事件才能设置此属性</para>
    /// <para>已取消的事件默认不会传递给后续处理程序，除非处理程序设置了接收已取消事件</para>
    /// </remarks>
    public bool IsCanceled
    {
        get => _canceled;
        set
        {
            if (!_cancelable)
            {
                throw new UnsupportedOperationException($"事件 {GetType().Name} 不支持取消操作");
            }

            _canceled = value;
        }
    }

    /// <summary>
    /// 获取或设置事件结果
    /// </summary>
    /// <value>事件的处理结果</value>
    /// <exception cref="InvalidOperationException">当事件不支持结果设置时抛出</exception>
    /// <remarks>
    /// <para>只有标记了 <see cref="HasResultAttribute"/> 的事件才能设置此属性</para>
    /// <para>事件结果用于影响后续的业务逻辑处理</para>
    /// </remarks>
    public EventResult Result
    {
        get => _result;
        set
        {
            if (!_hasResult)
            {
                throw new InvalidOperationException($"事件 {GetType().Name} 不支持结果设置");
            }

            _result = value;
        }
    }

    /// <summary>
    /// 获取当前事件阶段
    /// </summary>
    /// <value>当前正在处理的事件阶段，未开始处理时为 null</value>
    /// <remarks>
    /// <para>事件阶段按优先级顺序递增</para>
    /// <para>用于跟踪事件处理的进度和当前状态</para>
    /// </remarks>
    public EventPriority? Phase => _phase;

    /// <summary>
    /// 设置事件阶段（内部使用）
    /// </summary>
    /// <param name="value">新的事件阶段</param>
    /// <exception cref="ArgumentException">当尝试设置更早的阶段时抛出</exception>
    /// <remarks>
    /// <para>事件阶段只能向前推进，不能回退</para>
    /// <para>由 EventBus 系统内部调用，外部代码不应直接调用</para>
    /// </remarks>
    internal void SetPhase(EventPriority value)
    {
        
        if (_phase == value)
        {
            return;
        }
    
        if (_phase != null && _phase.Value.CompareTo(value) > 0)
        {
            throw new ArgumentException($"尝试将事件阶段设置为 {value}，但当前已经是 {_phase}");
        }

        _phase = value;
    }

    /// <summary>
    /// 设置事件为已取消
    /// </summary>
    /// <param name="canceled">是否取消事件</param>
    /// <exception cref="UnsupportedOperationException">当事件不支持取消时抛出</exception>
    /// <remarks>这是 <see cref="IsCanceled"/> 属性的便捷设置方法</remarks>
    public void SetCanceled(bool canceled)
    {
        IsCanceled = canceled;
    }

    /// <summary>
    /// 设置事件结果
    /// </summary>
    /// <param name="result">事件处理结果</param>
    /// <exception cref="InvalidOperationException">当事件不支持结果设置时抛出</exception>
    /// <remarks>这是 <see cref="Result"/> 属性的便捷设置方法</remarks>
    public void SetResult(EventResult result)
    {
        Result = result;
    }

    /// <summary>
    /// 获取包含所有已注册到此事件的监听器的ListenerList对象
    /// </summary>
    /// <returns>事件的监听器列表</returns>
    /// <remarks>
    /// <para>返回的是实际的监听器列表对象，可以用于查询和管理处理程序</para>
    /// <para>主要用于运行时分析和调试</para>
    /// </remarks>
    public ListenerList GetListenerList()
    {
        return _listenerList;
    }

    /// <summary>
    /// 获取当前事件的所有订阅者信息
    /// </summary>
    /// <returns>所有订阅者的详细信息数组</returns>
    /// <remarks>
    /// <para>返回当前注册到此事件实例的所有处理程序信息</para>
    /// <para>用于调试、监控和运行时分析</para>
    /// </remarks>
    public EventHandlerInfo[] GetSubscribers()
    {
        return _listenerList.GetAllHandlers();
    }

    /// <summary>
    /// 获取事件的详细调试信息
    /// </summary>
    /// <returns>包含事件完整状态的格式化字符串</returns>
    /// <remarks>
    /// <para>包含事件的所有重要信息：类型、ID、时间、状态、处理程序等</para>
    /// <para>主要用于调试、日志记录和问题诊断</para>
    /// <para>输出格式友好，便于阅读和分析</para>
    /// </remarks>
    public string GetEventDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"事件类型: {GetType().FullName}");
        sb.AppendLine($"事件ID: {EventId}");
        sb.AppendLine($"触发时间: {EventTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
        sb.AppendLine($"可取消: {IsCancelable}");
        sb.AppendLine($"有结果: {HasResult}");
        sb.AppendLine($"已取消: {IsCanceled}");
        sb.AppendLine($"结果: {Result}");
        sb.AppendLine($"当前阶段: {Phase}");

        if (CurrentHandler != null)
        {
            sb.AppendLine($"当前处理程序: {CurrentHandler.DeclaringType?.Name}.{CurrentHandler.MethodName}");
        }

        sb.AppendLine($"订阅者数量: {_listenerList.Count}");
        sb.AppendLine("订阅者详情:");
        sb.Append(_listenerList.GetDebugInfo());

        return sb.ToString();
    }
}