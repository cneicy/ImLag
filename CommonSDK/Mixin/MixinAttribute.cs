namespace CommonSDK.Mixin;

/// <summary>
/// 标记一个类为 Mixin 类，用于修改目标类型
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MixinAttribute : Attribute
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Mixin 优先级，数字越小优先级越高
    /// </summary>
    public int Priority { get; set; } = 1000;

    /// <summary>
    /// 依赖的其他 Mixin
    /// </summary>
    public string[] RequireMixins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 与此 Mixin 冲突的 Mixin
    /// </summary>
    public string[] ConflictsWith { get; set; } = Array.Empty<string>();

    public MixinAttribute(Type targetType)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }
}