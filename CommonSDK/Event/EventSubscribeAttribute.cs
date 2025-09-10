namespace CommonSDK.Event;

/// <summary>
/// 事件订阅特性 - 支持优先级和接收已取消事件
/// </summary>
/// <remarks>
/// <para>标记在方法上，指示该方法是一个事件处理程序</para>
/// <para>支持优先级设置、取消事件处理等高级功能</para>
/// <para>兼容传统的数字优先级和新的枚举优先级</para>
/// <para>只能用于标记了 <see cref="EventBusSubscriberAttribute"/> 的类中的方法</para>
/// </remarks>
/// <example>
/// <code>
/// [EventSubscribe(EventPriority.HIGH, receiveCanceled: true)]
/// public void OnPlayerAction(PlayerActionEvent evt)
/// {
///     // 处理逻辑，即使事件被取消也会执行
/// }
/// 
/// [EventSubscribe(100)] // 传统数字优先级
/// public async Task OnAsyncEvent(AsyncEvent evt)
/// {
///     await ProcessAsync();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class EventSubscribeAttribute : Attribute
{
    /// <summary>
    /// 获取或设置处理程序优先级
    /// </summary>
    /// <value>处理程序的优先级，决定执行顺序</value>
    /// <remarks>
    /// 优先级越高（数值越小）越先执行，MONITOR 优先级总是最后执行
    /// </remarks>
    public EventPriority Priority { get; set; }

    /// <summary>
    /// 获取或设置是否接收已取消的事件
    /// </summary>
    /// <value>如果为 true，即使事件被取消也会调用此处理程序</value>
    /// <remarks>
    /// 通常用于监控、日志记录或清理操作，这些操作需要在事件被取消后仍然执行
    /// </remarks>
    public bool ReceiveCanceled { get; set; }

    /// <summary>
    /// 获取或设置传统的数字优先级支持（向后兼容）
    /// </summary>
    /// <value>数字形式的优先级，用于更细粒度的排序</value>
    /// <remarks>
    /// <para>在同一 <see cref="Priority"/> 内，数字优先级越大越先执行</para>
    /// <para>主要用于向后兼容和细粒度控制</para>
    /// </remarks>
    public int NumericPriority { get; set; }

    /// <summary>
    /// 默认构造函数 - 使用默认优先级
    /// </summary>
    /// <remarks>
    /// 创建具有 NORMAL 优先级、不接收已取消事件、数字优先级为 0 的事件订阅特性
    /// </remarks>
    public EventSubscribeAttribute()
    {
        Priority = EventPriority.NORMAL;
        ReceiveCanceled = false;
        NumericPriority = 0;
    }

    /// <summary>
    /// 使用EventPriority的构造函数
    /// </summary>
    /// <param name="priority">事件处理优先级</param>
    /// <param name="receiveCanceled">是否接收已取消的事件，默认为 false</param>
    /// <remarks>
    /// 推荐的构造函数，使用枚举优先级提供更好的可读性和类型安全
    /// </remarks>
    public EventSubscribeAttribute(EventPriority priority, bool receiveCanceled = false)
    {
        Priority = priority;
        ReceiveCanceled = receiveCanceled;
        NumericPriority = 0;
    }

    /// <summary>
    /// 向后兼容的数字优先级构造函数
    /// </summary>
    /// <param name="priority">数字优先级</param>
    /// <remarks>
    /// <para>为保持向后兼容而提供的构造函数</para>
    /// <para>数字优先级会自动转换为对应的 <see cref="EventPriority"/> 枚举值</para>
    /// <para>建议在新代码中使用枚举版本的构造函数</para>
    /// </remarks>
    public EventSubscribeAttribute(int priority)
    {
        NumericPriority = priority;
        Priority = ConvertToEventPriority(priority);
        ReceiveCanceled = false;
    }

    /// <summary>
    /// 将数字优先级转换为事件优先级枚举
    /// </summary>
    /// <param name="numericPriority">数字优先级</param>
    /// <returns>对应的事件优先级枚举值</returns>
    /// <remarks>
    /// <para>转换规则：</para>
    /// <list type="bullet">
    /// <item>≥100: HIGHEST</item>
    /// <item>≥50: HIGH</item>
    /// <item>&gt;0: NORMAL</item>
    /// <item>≥-50: LOW</item>
    /// <item>&lt;-50: LOWEST</item>
    /// </list>
    /// </remarks>
    private static EventPriority ConvertToEventPriority(int numericPriority)
    {
        return numericPriority switch
        {
            >= 100 => EventPriority.HIGHEST,
            >= 50 => EventPriority.HIGH,
            > 0 => EventPriority.NORMAL,
            >= -50 => EventPriority.LOW,
            _ => EventPriority.LOWEST
        };
    }
}