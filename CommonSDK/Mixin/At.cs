// ReSharper disable InconsistentNaming
namespace CommonSDK.Mixin;

/// <summary>
/// 注入位置枚举
/// </summary>
public enum At
{
    /// <summary>
    /// 在方法开头注入
    /// </summary>
    HEAD,

    /// <summary>
    /// 在方法结尾注入
    /// </summary>
    TAIL,

    /// <summary>
    /// 在方法调用前注入
    /// </summary>
    INVOKE,

    /// <summary>
    /// 在方法调用后注入
    /// </summary>
    INVOKE_AFTER,

    /// <summary>
    /// 在 return 语句前注入
    /// </summary>
    RETURN,

    /// <summary>
    /// 在指定的字段访问前注入
    /// </summary>
    FIELD_GET,

    /// <summary>
    /// 在指定的字段赋值前注入
    /// </summary>
    FIELD_SET,

    /// <summary>
    /// 在异常抛出前注入
    /// </summary>
    THROW,

    /// <summary>
    /// 在 new 对象创建前注入
    /// </summary>
    NEW
}