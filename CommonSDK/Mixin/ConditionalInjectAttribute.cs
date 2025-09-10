namespace CommonSDK.Mixin;

/// <summary>
/// 条件注入 - 只有满足条件时才执行注入
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ConditionalInjectAttribute : Attribute
{
    /// <summary>
    /// 目标方法名
    /// </summary>
    public string TargetMethod { get; }

    /// <summary>
    /// 注入位置
    /// </summary>
    public At At { get; }

    /// <summary>
    /// 注入条件（C# 表达式字符串）
    /// </summary>
    public string Condition { get; }

    /// <summary>
    /// 注入优先级
    /// </summary>
    public int Priority { get; set; } = 1000;

    /// <summary>
    /// 条件检查类型
    /// </summary>
    public ConditionType ConditionType { get; set; } = ConditionType.Runtime;

    /// <summary>
    /// 目标方法签名
    /// </summary>
    public Type[] Signature { get; set; } = Array.Empty<Type>();

    public ConditionalInjectAttribute(string targetMethod, At at, string condition)
    {
        TargetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
        At = at;
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }
}

/// <summary>
/// 条件检查类型
/// </summary>
public enum ConditionType
{
    /// <summary>
    /// 运行时检查条件
    /// </summary>
    Runtime,

    /// <summary>
    /// 编译时检查条件
    /// </summary>
    CompileTime
}