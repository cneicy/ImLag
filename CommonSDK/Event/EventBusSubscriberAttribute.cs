namespace CommonSDK.Event;

/// <summary>
/// 自动注册事件处理程序特性
/// </summary>
/// <remarks>
/// <para>标记在类上的特性，表示该类支持EventBus自动注册</para>
/// <para>只有标记了此特性的类，其实例才会被自动注册系统处理</para>
/// <para>该特性本身不执行任何逻辑，仅用作标识符</para>
/// <para>通常与EventSubscribe特性配合使用</para>
/// </remarks>
/// <example>
/// <code>
/// [EventBusSubscriber]
/// public partial class GameManager : Node
/// {
///     [EventSubscribe(Priority = EventPriority.HIGH)]
///     public void OnGameStart(GameStartEvent evt)
///     {
///         InitializeGame();
///     }
///     
///     [EventSubscribe]
///     public async Task OnGameEnd(GameEndEvent evt)
///     {
///         await SaveProgress();
///         ShowMainMenu();
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class EventBusSubscriberAttribute : Attribute;