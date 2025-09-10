namespace CommonSDK.ModGateway;

/// <summary>
/// 🎯 线程安全任务管理器接口
/// <para>提供在主线程执行操作的抽象接口</para>
/// </summary>
public interface IThreadSafeTaskManager
{
    /// <summary>
    /// 在主线程异步执行操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>表示异步操作的任务</returns>
    Task CallDeferredAsync(Action action);

    /// <summary>
    /// 在主线程异步执行操作并返回结果
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>包含操作结果的任务</returns>
    Task<TResult> CallDeferredAsync<TResult>(Func<TResult> func);
}