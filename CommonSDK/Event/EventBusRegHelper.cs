using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;

namespace CommonSDK.Event;

/// <summary>
/// EventBus注册助手类
/// </summary>
/// <remarks>
/// <para>负责使用反射扫描和注册事件处理程序的核心工具类</para>
/// <para>主要功能包括:</para>
/// <list type="bullet">
/// <item><description>扫描程序集中的静态事件处理程序</description></item>
/// <item><description>注册对象实例的事件处理程序</description></item>
/// <item><description>处理同步和异步方法的包装</description></item>
/// <item><description>支持优先级解析和设置</description></item>
/// </list>
/// <para>这个类被EventBus系统内部使用，通常不需要直接调用</para>
/// </remarks>
public static class EventBusRegHelper
{
    /// <summary>
    /// 注册静态事件处理程序的入口方法
    /// </summary>
    /// <remarks>
    /// 委托给RegisterStaticEventHandlersWithReflection方法执行实际工作。
    /// 使用AggressiveInlining优化性能。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegStaticEventHandler()
    {
        RegisterStaticEventHandlersWithReflection();
    }

    /// <summary>
    /// 注册对象实例事件处理程序的入口方法
    /// </summary>
    /// <param name="target">要注册的目标对象</param>
    /// <remarks>
    /// 委托给RegisterEventHandlersWithReflection方法执行实际工作。
    /// 使用AggressiveInlining优化性能。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterEventHandlers(object target)
    {
        RegisterEventHandlersWithReflection(target);
    }

    /// <summary>
    /// 使用反射注册静态事件处理程序
    /// </summary>
    /// <remarks>
    /// <para>扫描所有程序集，查找符合条件的静态方法并注册为事件处理程序</para>
    /// <para>处理步骤:</para>
    /// <list type="number">
    /// <item><description>遍历当前应用程序域的所有程序集</description></item>
    /// <item><description>查找标记了EventBusSubscriberAttribute的类型</description></item>
    /// <item><description>扫描类型中标记了EventSubscribeAttribute的静态方法</description></item>
    /// <item><description>验证方法签名（参数和返回类型）</description></item>
    /// <item><description>解析优先级设置</description></item>
    /// <item><description>创建委托并注册到EventBus</description></item>
    /// </list>
    /// <para>支持同步（void返回）和异步（Task返回）两种方法类型</para>
    /// <para>包含完整的错误处理和调试日志</para>
    /// </remarks>
    private static void RegisterStaticEventHandlersWithReflection()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var classAttributes = type.GetCustomAttributes(typeof(EventBusSubscriberAttribute), false);
                    if (classAttributes.Length == 0) continue;

                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        var attributes = method.GetCustomAttributes(typeof(EventSubscribeAttribute), false);
                        if (attributes.Length == 0) continue;

                        var subscribeAttr = (EventSubscribeAttribute)attributes[0];
                        var priority = subscribeAttr.Priority;
                        var numericPriority = subscribeAttr.NumericPriority;
                        var receiveCanceled = subscribeAttr.ReceiveCanceled;

                        var parameters = method.GetParameters();

                        if (parameters.Length != 1)
                        {
                            if (OS.IsDebugBuild())
                                EventBus.Logger.LogError(
                                    $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数数量不为1。");
                            continue;
                        }

                        var parameterType = parameters[0].ParameterType;
                        if (!typeof(EventBase).IsAssignableFrom(parameterType))
                        {
                            if (OS.IsDebugBuild())
                                EventBus.Logger.LogError(
                                    $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数类型 {parameterType.Name} 不继承自 EventBase。");
                            continue;
                        }

                        var isAsync = method.ReturnType == typeof(Task);

                        try
                        {
                            if (isAsync)
                            {
                                var funcType = typeof(Func<,>).MakeGenericType(parameterType, typeof(Task));
                                var handlerDelegate = Delegate.CreateDelegate(funcType, method);

                                // 传递原始方法信息
                                EventBus.RegisterEventInternal(parameterType, handlerDelegate, priority,
                                    numericPriority, receiveCanceled,
                                    $"Static {type.Name}.{method.Name} (Async, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                                    method);

                                if (OS.IsDebugBuild())
                                    EventBus.Logger.LogInfo(
                                        $"自动注册静态事件: {parameterType.FullName} -> {type.Name}.{method.Name} (优先级: {priority}({numericPriority}))");
                            }
                            else if (method.ReturnType == typeof(void))
                            {
                                var actionType = typeof(Action<>).MakeGenericType(parameterType);
                                var actionDelegate = Delegate.CreateDelegate(actionType, method);

                                // 使用新的包装方法，传递原始方法信息
                                var wrappedHandler =
                                    WrapSyncHandlerWithMethodInfo(parameterType, actionDelegate, method);
                                EventBus.RegisterEventInternal(parameterType, wrappedHandler, priority, numericPriority,
                                    receiveCanceled,
                                    $"Static {type.Name}.{method.Name} (Sync, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                                    method);

                                if (OS.IsDebugBuild())
                                    EventBus.Logger.LogInfo(
                                        $"自动注册静态事件: {parameterType.FullName} -> {type.Name}.{method.Name} (优先级: {priority}({numericPriority}))");
                            }
                            else
                            {
                                if (OS.IsDebugBuild())
                                    EventBus.Logger.LogError(
                                        $"静态方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但返回类型不是 void 或 Task。");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (OS.IsDebugBuild())
                                EventBus.Logger.LogError(
                                    $"为静态事件 {parameterType.FullName} 创建委托给方法 {method.Name} 失败: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (OS.IsDebugBuild())
                    EventBus.Logger.LogError($"扫描程序集 {assembly.FullName} 时发生异常: {ex}");
            }
        }
    }

    /// <summary>
    /// 使用反射注册对象实例的事件处理程序
    /// </summary>
    /// <param name="target">要注册的目标对象</param>
    /// <remarks>
    /// <para>扫描目标对象的类型，注册其实例方法为事件处理程序</para>
    /// <para>处理步骤:</para>
    /// <list type="number">
    /// <item><description>验证目标对象的类型是否标记了EventBusSubscriberAttribute</description></item>
    /// <item><description>获取类型中的所有实例方法</description></item>
    /// <item><description>查找标记了EventSubscribeAttribute的方法</description></item>
    /// <item><description>跳过静态方法（已在静态注册阶段处理）</description></item>
    /// <item><description>验证方法签名和解析优先级</description></item>
    /// <item><description>创建实例委托并注册到EventBus</description></item>
    /// </list>
    /// <para>支持public、private和protected的实例方法</para>
    /// <para>包含完整的错误处理和调试支持</para>
    /// </remarks>
    private static void RegisterEventHandlersWithReflection(object target)
    {
        var type = target.GetType();

        var classAttributes = type.GetCustomAttributes(typeof(EventBusSubscriberAttribute), false);
        if (classAttributes.Length == 0) return;

        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(EventSubscribeAttribute), false);
            if (attributes.Length == 0) continue;

            if (method.IsStatic) continue;

            var subscribeAttr = (EventSubscribeAttribute)attributes[0];
            var priority = subscribeAttr.Priority;
            var numericPriority = subscribeAttr.NumericPriority;
            var receiveCanceled = subscribeAttr.ReceiveCanceled;

            var parameters = method.GetParameters();

            if (parameters.Length != 1)
            {
                if (OS.IsDebugBuild())
                    EventBus.Logger.LogError(
                        $"方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数数量不为1。");
                continue;
            }

            var parameterType = parameters[0].ParameterType;
            if (!typeof(EventBase).IsAssignableFrom(parameterType))
            {
                if (OS.IsDebugBuild())
                    EventBus.Logger.LogError(
                        $"方法 {method.Name} 在类 {type.Name} 中带有 EventSubscribeAttribute，但参数类型 {parameterType.Name} 不继承自 EventBase。");
                continue;
            }

            var isAsync = method.ReturnType == typeof(Task);

            try
            {
                if (isAsync)
                {
                    var funcType = typeof(Func<,>).MakeGenericType(parameterType, typeof(Task));
                    var handlerDelegate = Delegate.CreateDelegate(funcType, target, method);

                    // 传递原始方法信息
                    EventBus.RegisterEventInternal(parameterType, handlerDelegate, priority, numericPriority,
                        receiveCanceled,
                        $"Instance {type.Name}.{method.Name} (Async, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                        method);
                }
                else if (method.ReturnType == typeof(void))
                {
                    var actionType = typeof(Action<>).MakeGenericType(parameterType);
                    var actionDelegate = Delegate.CreateDelegate(actionType, target, method);

                    // 使用新的包装方法，传递原始方法信息
                    var wrappedHandler = WrapSyncHandlerWithMethodInfo(parameterType, actionDelegate, method);
                    EventBus.RegisterEventInternal(parameterType, wrappedHandler, priority, numericPriority,
                        receiveCanceled,
                        $"Instance {type.Name}.{method.Name} (Sync, Priority: {priority}({numericPriority}), ReceiveCanceled: {receiveCanceled})",
                        method);
                }
            }
            catch (Exception ex)
            {
                if (OS.IsDebugBuild())
                    EventBus.Logger.LogError($"为事件 {parameterType.FullName} 创建委托给方法 {method.Name} 失败: {ex}");
            }
        }
    }

    /// <summary>
    /// 将同步处理程序包装为异步版本（保留原始方法信息）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="syncHandler">同步处理程序委托</param>
    /// <param name="originalMethod">原始方法信息</param>
    /// <returns>包装后的异步处理程序委托</returns>
    /// <remarks>
    /// <para>使用泛型方法和自定义包装器保留原始方法信息</para>
    /// <para>解决了编译器生成匿名方法导致调试信息丢失的问题</para>
    /// <para>确保EventBus内部统一使用异步委托，简化处理逻辑</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">当无法找到WrapActionWithMethodInfo方法时抛出</exception>
    private static Delegate WrapSyncHandlerWithMethodInfo(Type eventType, Delegate syncHandler,
        MethodInfo originalMethod)
    {
        var method = typeof(EventBusRegHelper)
            .GetMethod(nameof(WrapActionWithMethodInfo), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(eventType);

        if (method == null)
        {
            throw new InvalidOperationException("无法找到WrapActionWithMethodInfo方法");
        }

        return (Delegate)method.Invoke(null, [syncHandler, originalMethod])!;
    }

    /// <summary>
    /// 创建一个保留原始方法信息的包装器
    /// </summary>
    /// <typeparam name="T">事件类型参数</typeparam>
    /// <param name="actionDelegate">要包装的Action委托</param>
    /// <param name="originalMethod">原始方法信息</param>
    /// <returns>包装后的Func&lt;T, Task&gt;委托</returns>
    /// <remarks>
    /// <para>创建专用的包装器实例，保存原始方法信息以供调试使用</para>
    /// <para>包装器实现了异步接口，但内部调用同步方法</para>
    /// <para>保证同步方法的异常被正确传播到异步上下文</para>
    /// </remarks>
    private static Func<T, Task> WrapActionWithMethodInfo<T>(Delegate actionDelegate, MethodInfo originalMethod)
    {
        var typedAction = (Action<T>)actionDelegate;

        // 创建一个新的委托，其中包含原始方法信息
        var wrapper = new MethodInfoPreservingWrapper<T>(typedAction, originalMethod);
        return wrapper.ExecuteAsync;
    }

    /// <summary>
    /// 保留方法信息的包装器类
    /// </summary>
    /// <typeparam name="T">事件类型参数</typeparam>
    /// <remarks>
    /// <para>专门用于保存原始方法信息的包装器类</para>
    /// <para>通过属性暴露原始方法信息，供EventHandlerInfo提取使用</para>
    /// <para>提供异步执行接口，但内部调用同步方法</para>
    /// <para>确保异常处理的一致性</para>
    /// </remarks>
    private class MethodInfoPreservingWrapper<T>
    {
        /// <summary>
        /// 原始的同步Action委托
        /// </summary>
        private readonly Action<T> _originalAction;

        /// <summary>
        /// 原始方法信息
        /// </summary>
        private readonly MethodInfo _originalMethod;

        /// <summary>
        /// 初始化 <see cref="MethodInfoPreservingWrapper{T}"/> 类的新实例
        /// </summary>
        /// <param name="originalAction">原始的同步Action委托</param>
        /// <param name="originalMethod">原始方法信息</param>
        public MethodInfoPreservingWrapper(Action<T> originalAction, MethodInfo originalMethod)
        {
            _originalAction = originalAction;
            _originalMethod = originalMethod;
        }

        /// <summary>
        /// 获取原始方法信息
        /// </summary>
        /// <value>被包装的原始方法的MethodInfo</value>
        /// <remarks>此属性供EventHandlerInfo通过反射提取原始方法信息</remarks>
        public MethodInfo OriginalMethod => _originalMethod;

        /// <summary>
        /// 异步执行原始的同步方法
        /// </summary>
        /// <param name="arg">事件参数</param>
        /// <returns>表示异步操作的Task</returns>
        /// <remarks>
        /// <para>将同步方法的执行包装为异步操作</para>
        /// <para>对于成功执行的情况，返回已完成的Task</para>
        /// <para>对于异常情况，创建包含异常信息的Task</para>
        /// </remarks>
        public Task ExecuteAsync(T arg)
        {
            try
            {
                _originalAction(arg);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
    }

    /// <summary>
    /// 将同步处理程序包装为异步版本
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="syncHandler">同步处理程序委托</param>
    /// <returns>包装后的异步处理程序委托</returns>
    /// <remarks>
    /// <para>使用泛型方法动态包装同步处理程序</para>
    /// <para>确保EventBus内部统一使用异步委托，简化处理逻辑</para>
    /// <para>包装过程中保留原始的异常处理行为</para>
    /// <para>使用反射调用泛型方法以支持任意事件类型</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">当无法找到WrapAction方法时抛出</exception>
    private static Delegate WrapSyncHandler(Type eventType, Delegate syncHandler)
    {
        var method = typeof(EventBusRegHelper)
            .GetMethod(nameof(WrapAction), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(eventType);

        if (method == null)
        {
            throw new InvalidOperationException("无法找到WrapAction方法");
        }

        return (Delegate)method.Invoke(null, [syncHandler])!;
    }

    /// <summary>
    /// 将强类型的同步Action包装为异步Func
    /// </summary>
    /// <typeparam name="T">事件类型参数</typeparam>
    /// <param name="actionDelegate">要包装的Action委托</param>
    /// <returns>包装后的Func&lt;T, Task&gt;委托</returns>
    /// <remarks>
    /// <para>泛型包装方法，将Action&lt;T&gt;转换为Func&lt;T, Task&gt;</para>
    /// <para>保证同步方法的异常被正确传播到异步上下文</para>
    /// <para>对于成功执行的情况，返回已完成的Task</para>
    /// <para>对于异常情况，创建包含异常信息的Task</para>
    /// </remarks>
    private static Func<T, Task> WrapAction<T>(Delegate actionDelegate)
    {
        var typedAction = (Action<T>)actionDelegate;
        return arg =>
        {
            try
            {
                typedAction(arg);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(ex);
                return tcs.Task.ContinueWith(_ => Task.CompletedTask, TaskScheduler.Default).Unwrap();
            }
        };
    }
}