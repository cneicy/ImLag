namespace CommonSDK.Mixin;

/// <summary>
/// 在目标方法中注入代码
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InjectAttribute : Attribute
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
    /// 注入优先级，数字越小优先级越高
    /// </summary>
    public int Priority { get; set; } = 1000;

    /// <summary>
    /// 是否可取消原方法执行（仅在 HEAD 时有效）
    /// </summary>
    public bool Cancellable { get; set; } = false;

    /// <summary>
    /// 目标方法的签名（用于重载方法区分）
    /// </summary>
    public Type[] Signature { get; set; } = Array.Empty<Type>();

    /// <summary>
    /// 注入条件（C# 表达式字符串）
    /// </summary>
    public string Condition { get; set; } = "";

    /// <summary>
    /// 依赖的其他注入
    /// </summary>
    public string[] RequireInjects { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 注入的唯一标识符
    /// </summary>
    public string Id { get; set; } = "";

    public InjectAttribute(string targetMethod, At at)
    {
        TargetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
        At = at;
    }
}