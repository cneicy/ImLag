// ReSharper disable InconsistentNaming
namespace CommonSDK.Event;

/// <summary>
/// 事件结果枚举 - 类似 Forge Event.Result
/// </summary>
/// <remarks>
/// <para>用于控制事件的最终处理结果，影响后续的业务逻辑</para>
/// <para>只有标记了 <see cref="HasResultAttribute"/> 的事件才能设置结果</para>
/// </remarks>
public enum EventResult
{
    /// <summary>
    /// 默认行为 - 使用原版/默认逻辑
    /// </summary>
    /// <remarks>不干预原有逻辑，保持默认行为</remarks>
    DEFAULT,

    /// <summary>
    /// 允许操作 - 强制允许，即使原本不允许
    /// </summary>
    /// <remarks>强制操作被允许执行，覆盖原有的拒绝逻辑</remarks>
    ALLOW,

    /// <summary>
    /// 拒绝操作 - 强制拒绝，即使原本允许
    /// </summary>
    /// <remarks>强制操作被拒绝执行，覆盖原有的允许逻辑</remarks>
    DENY
}