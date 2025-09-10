// ReSharper disable InconsistentNaming
namespace CommonSDK.Event;

/// <summary>
/// 事件优先级枚举 - 类似 Forge EventPriority
/// </summary>
/// <remarks>
/// <para>定义事件处理程序的执行顺序，数值越小优先级越高</para>
/// <para>MONITOR 优先级是特殊的，在所有其他处理程序完成后执行，用于监控和日志记录</para>
/// <para>在 MONITOR 阶段不应该修改事件状态，主要用于观察和记录</para>
/// </remarks>
public enum EventPriority
{
    /// <summary>
    /// 最高优先级 - 最先执行，用于最重要的修改
    /// </summary>
    /// <remarks>用于需要最早处理的关键逻辑，如权限检查、初始化等</remarks>
    HIGHEST = 0,

    /// <summary>
    /// 高优先级 - 早期执行，用于重要的修改
    /// </summary>
    /// <remarks>用于重要但不是最关键的处理逻辑</remarks>
    HIGH = 1,

    /// <summary>
    /// 普通优先级 - 默认优先级，用于一般的修改
    /// </summary>
    /// <remarks>大多数业务逻辑使用此优先级</remarks>
    NORMAL = 2,

    /// <summary>
    /// 低优先级 - 后期执行，用于不太重要的修改
    /// </summary>
    /// <remarks>用于次要的处理逻辑或补充操作</remarks>
    LOW = 3,

    /// <summary>
    /// 最低优先级 - 最后执行，用于清理和收尾工作
    /// </summary>
    /// <remarks>用于清理资源、收尾操作等最后执行的逻辑</remarks>
    LOWEST = 4,

    /// <summary>
    /// 监控优先级 - 在所有处理完成后执行，用于监控和日志记录
    /// 在此阶段不应该修改事件状态
    /// </summary>
    /// <remarks>
    /// 特殊优先级，专门用于监控、统计、日志记录等不影响业务逻辑的操作
    /// </remarks>
    MONITOR = 5
}