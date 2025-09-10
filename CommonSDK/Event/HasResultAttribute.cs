namespace CommonSDK.Event;

/// <summary>
/// 有结果的事件特性
/// </summary>
/// <remarks>
/// <para>标记在事件类上，表示该事件支持设置处理结果</para>
/// <para>只有标记了此特性的事件类才能调用 <see cref="EventBase.SetResult(EventResult)"/> 方法</para>
/// <para>通常用于需要影响后续业务逻辑的事件，如权限验证、条件检查等</para>
/// </remarks>
/// <example>
/// <code>
/// [HasResult]
/// public class ItemUseEvent : EventBase
/// {
///     public Item Item { get; set; }
///     public Player Player { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class HasResultAttribute : Attribute;