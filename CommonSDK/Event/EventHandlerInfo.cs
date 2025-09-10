using System.Reflection;

namespace CommonSDK.Event;

/// <summary>
/// 事件处理程序信息
/// </summary>
/// <remarks>
/// <para>包含事件处理程序的完整元数据信息</para>
/// <para>用于调试、监控和运行时分析事件处理程序的执行情况</para>
/// <para>支持原始方法信息的提取，确保调试时显示真实的方法名而不是编译器生成的匿名方法名</para>
/// </remarks>
public class EventHandlerInfo
{
    /// <summary>
    /// 获取事件处理程序委托
    /// </summary>
    /// <value>实际的事件处理程序委托实例</value>
    public Delegate Handler { get; }

    /// <summary>
    /// 获取事件处理程序的优先级
    /// </summary>
    /// <value>处理程序的 <see cref="EventPriority"/> 枚举值</value>
    public EventPriority Priority { get; }

    /// <summary>
    /// 获取数字优先级（向后兼容）
    /// </summary>
    /// <value>数字形式的优先级值，用于更细粒度的排序</value>
    public int NumericPriority { get; }

    /// <summary>
    /// 获取是否接收已取消的事件
    /// </summary>
    /// <value>如果为 true，即使事件被取消也会执行此处理程序</value>
    public bool ReceiveCanceled { get; }

    /// <summary>
    /// 获取处理程序的目标对象（对于实例方法）
    /// </summary>
    /// <value>处理程序绑定的对象实例，静态方法时为 null</value>
    public object? Target { get; }

    /// <summary>
    /// 获取处理程序的方法信息
    /// </summary>
    /// <value>处理程序对应的 <see cref="MethodInfo"/> 实例</value>
    public MethodInfo Method { get; }

    /// <summary>
    /// 获取调试信息字符串
    /// </summary>
    /// <value>包含处理程序详细信息的调试字符串</value>
    public string DebugInfo { get; }

    /// <summary>
    /// 获取方法的声明类型
    /// </summary>
    /// <value>定义此方法的类型</value>
    public Type DeclaringType { get; }

    /// <summary>
    /// 获取方法名称
    /// </summary>
    /// <value>处理程序方法的名称</value>
    public string MethodName { get; }

    /// <summary>
    /// 获取原始方法信息（用于包装的方法）
    /// </summary>
    /// <value>
    /// 对于被包装的同步方法，这是原始的 <see cref="MethodInfo"/>；
    /// 对于未包装的方法，这可能为 null
    /// </value>
    public MethodInfo? OriginalMethod { get; }

    /// <summary>
    /// 获取原始方法名称
    /// </summary>
    /// <value>原始方法的名称，用于调试显示</value>
    public string OriginalMethodName { get; }

    /// <summary>
    /// 获取原始方法的声明类型
    /// </summary>
    /// <value>原始方法的声明类型</value>
    public Type? OriginalDeclaringType { get; }

    /// <summary>
    /// 初始化 <see cref="EventHandlerInfo"/> 类的新实例
    /// </summary>
    /// <param name="handler">事件处理程序委托</param>
    /// <param name="priority">处理程序优先级</param>
    /// <param name="numericPriority">数字优先级</param>
    /// <param name="receiveCanceled">是否接收已取消的事件</param>
    /// <param name="debugInfo">调试信息</param>
    /// <param name="originalMethod">原始方法信息（可选）</param>
    /// <remarks>
    /// <para>构造函数会自动提取方法信息并尝试从包装器中恢复原始方法信息</para>
    /// <para>如果无法提取原始方法信息，将使用当前处理程序的方法信息作为显示信息</para>
    /// </remarks>
    public EventHandlerInfo(Delegate handler, EventPriority priority, int numericPriority, bool receiveCanceled,
        string debugInfo = "", MethodInfo? originalMethod = null)
    {
        Handler = handler;
        Priority = priority;
        NumericPriority = numericPriority;
        ReceiveCanceled = receiveCanceled;
        Target = handler.Target;
        Method = handler.Method;
        DebugInfo = debugInfo;
        DeclaringType = Method.DeclaringType ?? typeof(object);
        MethodName = Method.Name;

        OriginalMethod = ExtractOriginalMethodFromWrapper(handler) ?? originalMethod;

        if (OriginalMethod != null)
        {
            OriginalMethodName = OriginalMethod.Name;
            OriginalDeclaringType = OriginalMethod.DeclaringType;
        }
        else
        {
            OriginalMethodName = MethodName;
            OriginalDeclaringType = DeclaringType;
        }
    }

    /// <summary>
    /// 从包装器中提取原始方法信息
    /// </summary>
    /// <param name="handler">处理程序委托</param>
    /// <returns>提取到的原始方法信息，如果无法提取则返回 null</returns>
    /// <remarks>
    /// <para>尝试从方法信息保留包装器中提取原始方法信息</para>
    /// <para>这对于显示真实的方法名而不是编译器生成的匿名方法名非常重要</para>
    /// <para>如果提取失败，方法会静默失败并返回 null</para>
    /// </remarks>
    private static MethodInfo? ExtractOriginalMethodFromWrapper(Delegate handler)
    {
        if (handler.Target == null) return null;

        var targetType = handler.Target.GetType();

        if (!targetType.Name.Contains("MethodInfoPreservingWrapper")) return null;
        try
        {
            var originalMethodProperty = targetType.GetProperty("OriginalMethod");
            if (originalMethodProperty != null)
            {
                return originalMethodProperty.GetValue(handler.Target) as MethodInfo;
            }
        }
        catch
        {
            // 如果提取失败，继续使用其他方法
        }

        return null;
    }

    /// <summary>
    /// 获取显示用的方法名（优先使用原始方法名）
    /// </summary>
    /// <value>用于显示的方法名，优先显示原始方法名</value>
    /// <remarks>在调试输出中显示真实的方法名而不是包装器生成的名称</remarks>
    public string DisplayMethodName => OriginalMethodName;

    /// <summary>
    /// 获取显示用的声明类型（优先使用原始声明类型）
    /// </summary>
    /// <value>用于显示的声明类型，优先显示原始类型</value>
    /// <remarks>在调试输出中显示真实的声明类型而不是包装器类型</remarks>
    public Type DisplayDeclaringType => OriginalDeclaringType ?? DeclaringType;
}