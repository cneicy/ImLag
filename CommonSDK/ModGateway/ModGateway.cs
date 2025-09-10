namespace CommonSDK.ModGateway;

/// <summary>
/// 🎯 模组网关静态管理器
/// <para>提供全局的任务管理器访问</para>
/// </summary>
public static class ModGateway
{
    private static IThreadSafeTaskManager _taskManager;

    /// <summary>
    /// 设置任务管理器
    /// </summary>
    public static void SetTaskManager(IThreadSafeTaskManager taskManager)
    {
        _taskManager = taskManager;
    }

    /// <summary>
    /// 获取任务管理器
    /// </summary>
    public static IThreadSafeTaskManager GetTaskManager()
    {
        return _taskManager;
    }
}