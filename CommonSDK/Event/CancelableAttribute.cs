namespace CommonSDK.Event;

/// <summary>
/// 可取消的事件特性
/// </summary>
/// <remarks>
/// <para>标记在事件类上，表示该事件支持取消操作</para>
/// <para>只有标记了此特性的事件类才能调用 <see cref="EventBase.SetCanceled(bool)"/> 方法</para>
/// <para>已取消的事件默认不会传递给后续处理程序，除非处理程序设置了 ReceiveCanceled = true</para>
/// </remarks>
/// <example>
/// <code>
/// [Cancelable]
/// public class PlayerMoveEvent : EventBase
/// {
///     public Vector3 NewPosition { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class CancelableAttribute : Attribute;