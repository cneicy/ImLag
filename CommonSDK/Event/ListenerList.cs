using System.Reflection;

namespace CommonSDK.Event;

/// <summary>
/// 监听器列表
/// </summary>
/// <remarks>
/// <para>管理特定事件类型的所有处理程序的线程安全集合</para>
/// <para>支持按优先级自动排序，确保处理程序按正确顺序执行</para>
/// <para>提供添加、删除、清理等完整的生命周期管理功能</para>
/// <para>所有操作都是线程安全的，支持多线程环境下的并发访问</para>
/// </remarks>
public class ListenerList
{
    /// <summary>
    /// 存储事件处理程序信息的内部列表
    /// </summary>
    private readonly List<EventHandlerInfo> _handlers = [];

    /// <summary>
    /// 线程同步锁对象
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 标记是否需要重新排序
    /// </summary>
    private bool _needsSort = true;

    /// <summary>
    /// 添加事件处理程序到列表
    /// </summary>
    /// <param name="handler">事件处理程序委托</param>
    /// <param name="priority">处理程序优先级</param>
    /// <param name="numericPriority">数字优先级</param>
    /// <param name="receiveCanceled">是否接收已取消的事件</param>
    /// <param name="debugInfo">调试信息</param>
    /// <param name="originalMethod">原始方法信息</param>
    /// <remarks>
    /// <para>线程安全的添加操作</para>
    /// <para>添加后会标记需要重新排序，下次获取时会自动排序</para>
    /// </remarks>
    public void Add(Delegate handler, EventPriority priority, int numericPriority, bool receiveCanceled,
        string debugInfo = "", MethodInfo? originalMethod = null)
    {
        lock (_lock)
        {
            _handlers.Add(new EventHandlerInfo(handler, priority, numericPriority, receiveCanceled, debugInfo,
                originalMethod));
            _needsSort = true;
        }
    }

    /// <summary>
    /// 从列表中移除指定的事件处理程序
    /// </summary>
    /// <param name="handler">要移除的事件处理程序委托</param>
    /// <returns>如果成功移除则返回 true，否则返回 false</returns>
    /// <remarks>
    /// <para>线程安全的移除操作</para>
    /// <para>使用委托的 Equals 方法进行比较</para>
    /// <para>从列表末尾开始搜索以优化性能</para>
    /// </remarks>
    public bool Remove(Delegate handler)
    {
        lock (_lock)
        {
            for (var i = _handlers.Count - 1; i >= 0; i--)
            {
                if (!_handlers[i].Handler.Equals(handler)) continue;
                _handlers.RemoveAt(i);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 移除指定目标对象的所有事件处理程序
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <remarks>
    /// <para>线程安全的批量移除操作</para>
    /// <para>通常在对象销毁时调用以清理所有相关的事件处理程序</para>
    /// <para>从列表末尾开始搜索以避免索引问题</para>
    /// </remarks>
    public void RemoveTarget(object target)
    {
        lock (_lock)
        {
            for (var i = _handlers.Count - 1; i >= 0; i--)
            {
                if (_handlers[i].Target == target)
                {
                    _handlers.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 获取按优先级排序的处理程序数组
    /// </summary>
    /// <returns>按优先级排序的事件处理程序信息数组</returns>
    /// <remarks>
    /// <para>线程安全的获取操作</para>
    /// <para>返回的是数组副本，不会影响内部状态</para>
    /// <para>排序规则：优先级相同时，数字优先级越大排在前面</para>
    /// <para>使用延迟排序策略，只在需要时才进行排序</para>
    /// </remarks>
    public EventHandlerInfo[] GetSortedHandlers()
    {
        lock (_lock)
        {
            if (!_needsSort) return _handlers.ToArray();
            _handlers.Sort((a, b) =>
            {
                var priorityCompare = a.Priority.CompareTo(b.Priority);
                return priorityCompare != 0 ? priorityCompare : b.NumericPriority.CompareTo(a.NumericPriority);
            });
            _needsSort = false;

            return _handlers.ToArray();
        }
    }

    /// <summary>
    /// 获取处理程序数量
    /// </summary>
    /// <value>当前列表中的处理程序数量</value>
    /// <remarks>线程安全的属性访问</remarks>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _handlers.Count;
            }
        }
    }

    /// <summary>
    /// 清空所有处理程序
    /// </summary>
    /// <remarks>
    /// <para>线程安全的清空操作</para>
    /// <para>清空后重置排序标记</para>
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _handlers.Clear();
            _needsSort = false;
        }
    }

    /// <summary>
    /// 获取所有处理程序的详细信息
    /// </summary>
    /// <returns>所有事件处理程序信息的数组副本</returns>
    /// <remarks>
    /// <para>返回未排序的原始列表副本</para>
    /// <para>主要用于调试和分析目的</para>
    /// </remarks>
    public EventHandlerInfo[] GetAllHandlers()
    {
        lock (_lock)
        {
            return _handlers.ToArray();
        }
    }

    /// <summary>
    /// 获取调试信息字符串
    /// </summary>
    /// <returns>包含所有处理程序信息的格式化字符串</returns>
    /// <remarks>
    /// <para>返回按优先级排序的处理程序列表</para>
    /// <para>显示原始方法名和类型名，便于调试</para>
    /// <para>包含优先级和接收取消事件的设置信息</para>
    /// </remarks>
    public string GetDebugInfo()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            var sortedHandlers = GetSortedHandlers();

            for (var i = 0; i < sortedHandlers.Length; i++)
            {
                var handler = sortedHandlers[i];
                // 使用DisplayMethodName和DisplayDeclaringType显示原始方法名
                sb.AppendLine(
                    $"  [{i + 1}] {handler.DisplayDeclaringType?.Name}.{handler.DisplayMethodName} - Priority: {handler.Priority}({handler.NumericPriority}), ReceiveCanceled: {handler.ReceiveCanceled}");
            }

            return sb.ToString();
        }
    }
}