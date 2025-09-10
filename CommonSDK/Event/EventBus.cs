using System.Reflection;
using System.Runtime.CompilerServices;
using CommonSDK.Logger;
using Godot;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace CommonSDK.Event;

/// <summary>
/// 事件总线
/// </summary>
/// <remarks>
/// <para>提供基于事件类型的发布-订阅模式实现</para>
/// <para>支持类型安全的事件处理和自动注册基于特性的事件处理程序</para>
/// <para>支持处理程序优先级排序，数值越大优先级越高</para>
/// <para>支持同步和异步事件处理程序</para>
/// <para>提供自动注册和手动注册两种方式</para>
/// <para>线程安全设计，支持多线程环境下的事件处理</para>
/// </remarks>
/// <example>
/// <code>
/// // 手动注册事件处理程序
/// EventBus.RegisterEvent&lt;PlayerMoveEvent&gt;(OnPlayerMove, EventPriority.HIGH);
/// 
/// // 触发事件
/// var moveEvent = new PlayerMoveEvent(player, newPosition);
/// bool handled = await EventBus.TriggerEventAsync(moveEvent);
/// 
/// // 自动注册对象的事件处理程序
/// EventBus.AutoRegister(gameManager);
/// </code>
/// </example>
public static class EventBus
{
    /// <summary>
    /// 存储所有已注册的事件处理程序（带优先级）
    /// </summary>
    /// <remarks>
    /// <para>以事件类型为键，监听器列表为值的字典</para>
    /// <para>所有访问都需要加锁以保证线程安全</para>
    /// </remarks>
    private static readonly Dictionary<Type, ListenerList> EventHandlers = new();

    /// <summary>
    /// 获取日志记录器实例
    /// </summary>
    /// <value>用于事件总线系统的日志记录器</value>
    /// <remarks>所有EventBus相关的日志都通过此实例输出</remarks>
    public static readonly LogHelper Logger = new("EventBus");

    /// <summary>
    /// 存储已经注册过的实例，避免重复注册（实例级别）
    /// </summary>
    /// <remarks>
    /// <para>跟踪已通过自动注册的对象实例</para>
    /// <para>防止同一对象多次注册导致重复处理</para>
    /// </remarks>
    private static readonly HashSet<object> RegisteredInstances = new();

    /// <summary>
    /// 实例注册操作的线程安全锁
    /// </summary>
    /// <remarks>保护 RegisteredInstances 集合的并发访问</remarks>
    private static readonly object InstanceLock = new();

    /// <summary>
    /// 标记是否已确保自动注册管理器初始化
    /// </summary>
    /// <remarks>确保自动注册系统只初始化一次</remarks>
    private static bool _hasEnsuredAutoRegInit;

    /// <summary>
    /// 自动注册初始化操作的线程安全锁
    /// </summary>
    /// <remarks>保护自动注册初始化过程的线程安全</remarks>
    private static readonly object AutoRegInitLock = new();

    /// <summary>
    /// 确保自动注册管理器已初始化
    /// </summary>
    /// <remarks>
    /// <para>使用双重检查锁定模式确保线程安全</para>
    /// <para>只在第一次调用时执行初始化</para>
    /// </remarks>
    private static void EnsureAutoManagerInitialized()
    {
        if (_hasEnsuredAutoRegInit) return;

        lock (AutoRegInitLock)
        {
            if (_hasEnsuredAutoRegInit) return;
            EventAutoRegHelper.EnsureInitialized();
            _hasEnsuredAutoRegInit = true;
        }
    }

    /// <summary>
    /// 静态构造函数，初始化时注册当前程序集中的静态事件处理程序
    /// </summary>
    /// <remarks>
    /// <para>在类型首次使用时自动调用</para>
    /// <para>负责扫描和注册所有静态事件处理程序</para>
    /// </remarks>
    static EventBus()
    {
        RegStaticEventHandler();
    }

    /// <summary>
    /// 注册静态事件处理程序
    /// </summary>
    /// <remarks>
    /// <para>委托给 EventBusRegHelper 执行实际的注册工作</para>
    /// <para>使用 AggressiveInlining 优化性能</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RegStaticEventHandler()
    {
        EventBusRegHelper.RegStaticEventHandler();
    }

    /// <summary>
    /// 注册事件处理程序（同步版本，支持EventPriority和方法信息）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理程序</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="declaringType">声明类型</param>
    /// <param name="priority">事件优先级，默认为 NORMAL</param>
    /// <param name="receiveCanceled">是否接收已取消的事件，默认为 false</param>
    /// <remarks>
    /// <para>手动注册的处理程序，支持指定方法信息用于调试显示</para>
    /// <para>内部会将同步处理程序包装为异步版本</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterEvent<TEvent>(Action<TEvent> handler, string methodName, Type declaringType,
        EventPriority priority = EventPriority.NORMAL, bool receiveCanceled = false) where TEvent : EventBase
    {
        // 创建一个假的MethodInfo用于显示
        var methodInfo = declaringType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        Func<TEvent, Task> asyncHandler = arg =>
        {
            handler(arg);
            return Task.CompletedTask;
        };
        RegisterEventInternal(typeof(TEvent), asyncHandler, priority, 0, receiveCanceled,
            $"Manual Sync Handler (Priority: {priority})", methodInfo);
    }

    /// <summary>
    /// 注册事件处理程序（异步版本，向后兼容数字优先级）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">异步事件处理程序</param>
    /// <param name="priority">数字优先级，默认为 0</param>
    /// <remarks>
    /// <para>向后兼容的注册方法，支持数字优先级</para>
    /// <para>数字优先级会自动转换为对应的 EventPriority 枚举值</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterEvent<TEvent>(Func<TEvent, Task> handler, int priority = 0) where TEvent : EventBase
    {
        var eventPriority = ConvertToEventPriority(priority);
        RegisterEventInternal(typeof(TEvent), handler, eventPriority, priority, false,
            $"Manual Async Handler (Priority: {priority})");
    }

    /// <summary>
    /// 注册同步事件处理程序（支持EventPriority）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">同步事件处理程序</param>
    /// <param name="priority">事件优先级，默认为 NORMAL</param>
    /// <param name="receiveCanceled">是否接收已取消的事件，默认为 false</param>
    /// <remarks>
    /// <para>推荐的同步处理程序注册方法</para>
    /// <para>内部会自动包装为异步版本以统一处理</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterEvent<TEvent>(Action<TEvent> handler, EventPriority priority = EventPriority.NORMAL,
        bool receiveCanceled = false) where TEvent : EventBase
    {
        Func<TEvent, Task> asyncHandler = arg =>
        {
            handler(arg);
            return Task.CompletedTask;
        };
        RegisterEventInternal(typeof(TEvent), asyncHandler, priority, 0, receiveCanceled,
            $"Manual Sync Handler (Priority: {priority})");
    }

    /// <summary>
    /// 注册同步事件处理程序（向后兼容数字优先级）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">同步事件处理程序</param>
    /// <param name="priority">数字优先级，默认为 0</param>
    /// <remarks>
    /// <para>向后兼容的同步处理程序注册方法</para>
    /// <para>数字优先级会自动转换为对应的 EventPriority 枚举值</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterEvent<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : EventBase
    {
        var eventPriority = ConvertToEventPriority(priority);
        Func<TEvent, Task> asyncHandler = arg =>
        {
            handler(arg);
            return Task.CompletedTask;
        };
        RegisterEventInternal(typeof(TEvent), asyncHandler, eventPriority, priority, false,
            $"Manual Sync Handler (Priority: {priority})");
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

    /// <summary>
    /// 内部事件注册方法
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handler">事件处理程序委托</param>
    /// <param name="priority">事件优先级</param>
    /// <param name="numericPriority">数字优先级</param>
    /// <param name="receiveCanceled">是否接收已取消的事件</param>
    /// <param name="debugInfo">调试信息</param>
    /// <param name="originalMethod">原始方法信息</param>
    /// <remarks>
    /// <para>所有注册方法的最终入口</para>
    /// <para>负责创建或获取监听器列表并添加处理程序</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RegisterEventInternal(Type eventType, Delegate handler, EventPriority priority,
        int numericPriority, bool receiveCanceled, string debugInfo = "", MethodInfo? originalMethod = null)
    {
        lock (EventHandlers)
        {
            if (!EventHandlers.TryGetValue(eventType, out var collection))
            {
                collection = new ListenerList();
                EventHandlers[eventType] = collection;
            }

            collection.Add(handler, priority, numericPriority, receiveCanceled, debugInfo, originalMethod);
        }
    }

    /// <summary>
    /// 注销事件处理程序（异步版本）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">要注销的异步事件处理程序</param>
    /// <remarks>
    /// <para>从指定事件类型的处理程序列表中移除处理程序</para>
    /// <para>如果移除后列表为空，会自动清理整个事件类型的注册</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnregisterEvent<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase
    {
        var eventType = typeof(TEvent);
        lock (EventHandlers)
        {
            if (!EventHandlers.TryGetValue(eventType, out var collection)) return;
            collection.Remove(handler);
            if (collection.Count == 0)
            {
                EventHandlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 注销事件处理程序（同步版本）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">要注销的同步事件处理程序</param>
    /// <remarks>
    /// <para>需要查找对应的包装后的异步处理程序进行移除</para>
    /// <para>使用反射技术识别包装的同步处理程序</para>
    /// <para>比异步版本的注销稍微复杂，因为需要处理包装器匹配</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnregisterEvent<TEvent>(Action<TEvent> handler) where TEvent : EventBase
    {
        var eventType = typeof(TEvent);
        lock (EventHandlers)
        {
            if (!EventHandlers.TryGetValue(eventType, out var collection)) return;
            // 查找对应的包装后的异步处理程序
            var handlers = collection.GetSortedHandlers();
            foreach (var handlerInfo in handlers)
            {
                if (!IsWrappedSyncHandler(handlerInfo.Handler, handler)) continue;
                collection.Remove(handlerInfo.Handler);
                break;
            }

            if (collection.Count == 0)
            {
                EventHandlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 检查是否是包装的同步处理程序
    /// </summary>
    /// <param name="asyncHandler">异步处理程序委托</param>
    /// <param name="targetHandler">目标同步处理程序委托</param>
    /// <returns>如果是匹配的包装处理程序则返回 true</returns>
    /// <remarks>
    /// <para>使用反射技术检查异步处理程序是否是指定同步处理程序的包装版本</para>
    /// <para>处理编译器生成的匿名方法和自定义包装器</para>
    /// <para>包含异常处理，确保反射操作的安全性</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWrappedSyncHandler(Delegate asyncHandler, Delegate targetHandler)
    {
        try
        {
            var asyncMethodInfo = asyncHandler.Method;

            if (asyncMethodInfo.Name.Contains('<'))
            {
                var target = asyncHandler.Target;
                if (target == null) return false;

                var fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType != typeof(Action<>) && field.FieldType != typeof(Delegate)) continue;
                    if (field.GetValue(target) is Delegate actualDelegate && actualDelegate.Target == targetHandler.Target &&
                        actualDelegate.Method == targetHandler.Method)
                    {
                        return true;
                    }
                }
            }
            else if (asyncHandler.Target == targetHandler.Target &&
                     asyncHandler.Method == targetHandler.Method)
            {
                return true;
            }
        }
        catch (Exception)
        {
            // 忽略反射操作中的异常
        }

        return false;
    }

    /// <summary>
    /// 异步触发指定类型的事件 - 支持阶段处理和取消机制
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>如果有处理程序处理了事件则返回 true，否则返回 false</returns>
    /// <remarks>
    /// <para>这是事件触发的核心方法，支持完整的事件处理流程</para>
    /// <para>处理流程：</para>
    /// <list type="number">
    /// <item>确保自动注册管理器已初始化</item>
    /// <item>查找事件类型的处理程序列表</item>
    /// <item>按优先级顺序执行处理程序</item>
    /// <item>处理取消事件的跳过逻辑</item>
    /// <item>设置事件阶段和当前处理程序信息</item>
    /// <item>执行处理程序并处理异常</item>
    /// <item>清理当前处理程序信息</item>
    /// </list>
    /// <para>包含完整的调试日志和异常处理</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<bool> TriggerEventAsync<TEvent>(TEvent eventArgs) where TEvent : EventBase
    {
        EnsureAutoManagerInitialized();
        var eventType = typeof(TEvent);

        ListenerList? collection;
        lock (EventHandlers)
        {
            if (!EventHandlers.TryGetValue(eventType, out collection))
            {
                if (OS.IsDebugBuild())
                {
                    Logger.LogWarn($"没有找到事件 {eventType.FullName} 的处理程序");
                }

                return false;
            }
        }

        if (collection == null)
        {
            if (OS.IsDebugBuild())
            {
                Logger.LogWarn($"事件 {eventType.Name} 的处理程序集合为 null");
            }

            return false;
        }

        // 将处理程序信息同步到事件的监听器列表
        var handlers = collection.GetSortedHandlers();
        foreach (var handler in handlers)
        {
            eventArgs.GetListenerList().Add(handler.Handler, handler.Priority, handler.NumericPriority,
                handler.ReceiveCanceled, handler.DebugInfo);
        }

        var wasHandled = false;

        foreach (var handlerInfo in handlers)
        {
            // 设置当前处理程序信息
            eventArgs.CurrentHandler = handlerInfo;

            // 设置事件阶段
            eventArgs.SetPhase(handlerInfo.Priority);

            // 检查是否应该跳过已取消的事件
            if (eventArgs is { IsCancelable: true, IsCanceled: true } && !handlerInfo.ReceiveCanceled)
            {
                continue;
            }

            if (handlerInfo.Handler is Func<TEvent, Task> typedHandler)
            {
                try
                {
                    await typedHandler(eventArgs);
                    wasHandled = true;
                }
                catch (Exception ex)
                {
                    if (OS.IsDebugBuild())
                    {
                        Logger.LogError($"执行事件 {eventType.Name} 处理程序时发生异常: {ex}");
                    }
                }
            }
            else
            {
                if (OS.IsDebugBuild())
                {
                    Logger.LogError(
                        $"事件 {eventType.Name} 的处理程序类型不匹配。预期 Func<{eventType.Name}, Task>，实际为 {handlerInfo.Handler.GetType().Name}");
                }
            }
        }
        eventArgs.CurrentHandler = null;

        return wasHandled;
    }

    /// <summary>
    /// 同步触发指定类型的事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>如果有处理程序处理了事件则返回 true，否则返回 false</returns>
    /// <remarks>
    /// <para>异步触发方法的同步版本</para>
    /// <para>内部调用异步版本并等待完成</para>
    /// <para>建议在可能的情况下使用异步版本以获得更好的性能</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TriggerEvent<TEvent>(TEvent eventArgs) where TEvent : EventBase
    {
        return TriggerEventAsync(eventArgs).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取指定事件类型的所有订阅者信息
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>事件处理程序信息数组，如果没有注册则返回空数组</returns>
    /// <remarks>
    /// <para>用于查询和分析特定事件类型的所有处理程序</para>
    /// <para>返回的是副本，不会影响内部状态</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    public static EventHandlerInfo[] GetEventSubscribers<TEvent>() where TEvent : EventBase
    {
        var eventType = typeof(TEvent);
        lock (EventHandlers)
        {
            return EventHandlers.TryGetValue(eventType, out var collection) ? collection.GetAllHandlers() : [];
        }
    }

    /// <summary>
    /// 获取指定事件类型的监听器列表
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>事件的监听器列表，如果没有注册则返回新的空列表</returns>
    /// <remarks>
    /// <para>用于运行时查询和管理特定事件类型的处理程序</para>
    /// <para>返回的是实际的监听器列表对象，可以用于添加、删除处理程序</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    public static ListenerList GetListenerList<TEvent>() where TEvent : EventBase
    {
        var eventType = typeof(TEvent);
        lock (EventHandlers)
        {
            if (EventHandlers.TryGetValue(eventType, out var collection))
            {
                return collection;
            }

            return new ListenerList();
        }
    }

    /// <summary>
    /// 取消指定事件类型的所有处理程序
    /// </summary>
    /// <typeparam name="TEvent">要取消的事件类型</typeparam>
    /// <remarks>
    /// <para>完全移除指定事件类型的所有处理程序和相关信息</para>
    /// <para>此操作不可逆，应谨慎使用</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CancelEvent<TEvent>() where TEvent : EventBase
    {
        var eventType = typeof(TEvent);
        lock (EventHandlers)
        {
            EventHandlers.Remove(eventType);
        }
    }

    /// <summary>
    /// 注销指定对象的所有事件处理程序
    /// </summary>
    /// <param name="targetObject">要注销处理程序的目标对象</param>
    /// <remarks>
    /// <para>移除与指定对象相关的所有事件处理程序</para>
    /// <para>通常在对象销毁时调用以防止内存泄漏</para>
    /// <para>如果目标对象为 null，操作会被忽略</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnregisterAllEventsForObject(object targetObject)
    {
        if (targetObject is null)
        {
            if (OS.IsDebugBuild())
            {
                Logger.LogInfo("目标对象为 null，无法取消订阅。");
            }

            return;
        }

        lock (EventHandlers)
        {
            var eventTypesToRemove = new List<Type>();

            foreach (var kvp in EventHandlers)
            {
                kvp.Value.RemoveTarget(targetObject);
                if (kvp.Value.Count == 0)
                {
                    eventTypesToRemove.Add(kvp.Key);
                }
            }

            foreach (var eventType in eventTypesToRemove)
            {
                EventHandlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 注销指定实例的所有事件处理程序并从注册实例列表中移除
    /// </summary>
    /// <param name="targetObject">要注销的目标对象实例</param>
    /// <remarks>
    /// <para>结合了 <see cref="UnregisterAllEventsForObject"/> 和实例列表清理的功能</para>
    /// <para>确保对象完全从EventBus系统中移除</para>
    /// <para>推荐在对象生命周期结束时调用此方法</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnregisterInstance(object targetObject)
    {
        if (targetObject is null)
        {
            if (OS.IsDebugBuild())
            {
                Logger.LogInfo("目标对象为 null，无法取消订阅。");
            }

            return;
        }

        lock (InstanceLock)
        {
            RegisteredInstances.Remove(targetObject);
        }

        UnregisterAllEventsForObject(targetObject);
    }

    /// <summary>
    /// 注销所有事件处理程序并清理所有注册信息
    /// </summary>
    /// <remarks>
    /// <para>完全重置EventBus系统，清除所有事件处理程序和注册实例</para>
    /// <para>通常在应用程序关闭或重启时调用</para>
    /// <para>此操作不可逆，会影响整个系统</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnregisterAllEvents()
    {
        lock (EventHandlers)
        {
            EventHandlers.Clear();
        }

        lock (InstanceLock)
        {
            RegisteredInstances.Clear();
        }

        if (OS.IsDebugBuild())
        {
            Logger.LogInfo("所有事件订阅已被注销。");
        }
    }

    /// <summary>
    /// 自动注册对象的事件处理程序
    /// </summary>
    /// <param name="target">要注册的目标对象</param>
    /// <remarks>
    /// <para>扫描目标对象的类型，查找并注册所有标记了特性的事件处理程序</para>
    /// <para>防止重复注册，同一对象只会注册一次</para>
    /// <para>支持继承层次结构，会处理所有层级的方法</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [EventBusSubscriber]
    /// public class GameManager : Node
    /// {
    ///     [EventSubscribe(EventPriority.HIGH)]
    ///     public void OnPlayerJoin(PlayerJoinEvent evt) { }
    /// }
    /// 
    /// var gameManager = new GameManager();
    /// EventBus.AutoRegister(gameManager);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AutoRegister(object target)
    {
        if (target == null)
        {
            if (OS.IsDebugBuild()) Logger.LogError("AutoRegister: 目标对象为 null。");
            return;
        }

        lock (InstanceLock)
        {
            if (RegisteredInstances.Contains(target))
            {
                return;
            }

            EventBusRegHelper.RegisterEventHandlers(target);
            RegisteredInstances.Add(target);
        }
    }

    /// <summary>
    /// 检查指定实例是否已在EventBus中注册
    /// </summary>
    /// <param name="target">要检查的目标对象</param>
    /// <returns>如果对象已注册则返回 true，否则返回 false</returns>
    /// <remarks>
    /// <para>用于避免重复注册或查询对象状态</para>
    /// <para>null 对象始终返回 false</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInstanceRegistered(object target)
    {
        if (target == null) return false;

        lock (InstanceLock)
        {
            return RegisteredInstances.Contains(target);
        }
    }

    /// <summary>
    /// 获取当前已注册的实例数量
    /// </summary>
    /// <returns>已通过自动注册的实例数量</returns>
    /// <remarks>
    /// <para>用于监控和调试EventBus系统的使用状况</para>
    /// <para>只统计通过 <see cref="AutoRegister"/> 注册的实例</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRegisteredInstanceCount()
    {
        lock (InstanceLock)
        {
            return RegisteredInstances.Count;
        }
    }

    /// <summary>
    /// 获取当前已注册的事件类型数量
    /// </summary>
    /// <returns>已注册的不同事件类型数量</returns>
    /// <remarks>
    /// <para>用于监控EventBus系统中事件类型的多样性</para>
    /// <para>反映了系统中定义的事件种类数量</para>
    /// <para>线程安全的操作</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRegisteredEventTypeCount()
    {
        lock (EventHandlers)
        {
            return EventHandlers.Count;
        }
    }

    /// <summary>
    /// 获取指定事件类型的详细调试信息
    /// </summary>
    /// <typeparam name="TEvent">要查询的事件类型</typeparam>
    /// <returns>格式化的调试信息字符串</returns>
    /// <remarks>
    /// <para>提供事件类型的完整信息，包括处理程序列表和优先级</para>
    /// <para>主要用于调试、日志记录和系统分析</para>
    /// <para>输出格式友好，便于阅读</para>
    /// </remarks>
    public static string GetEventTypeDebugInfo<TEvent>() where TEvent : EventBase
    {
        var eventType = typeof(TEvent);

        lock (EventHandlers)
        {
            if (!EventHandlers.TryGetValue(eventType, out var collection))
                return $"事件类型 {eventType.FullName} 未注册任何处理程序";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"事件类型: {eventType.FullName}");
            sb.AppendLine($"处理程序数量: {collection.Count}");
            sb.AppendLine("处理程序详情 (按优先级排序):");
            sb.Append(collection.GetDebugInfo());
            return sb.ToString();

        }
    }

    /// <summary>
    /// 获取所有已注册事件类型的统计信息
    /// </summary>
    /// <returns>以事件类型为键、处理程序数量为值的字典</returns>
    /// <remarks>
    /// <para>提供EventBus系统的整体使用统计</para>
    /// <para>用于性能分析、监控和系统优化</para>
    /// <para>线程安全的操作，返回当前时刻的快照</para>
    /// </remarks>
    public static Dictionary<Type, int> GetEventStatistics()
    {
        lock (EventHandlers)
        {
            return EventHandlers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
    }
}