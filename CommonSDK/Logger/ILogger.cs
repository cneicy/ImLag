namespace CommonSDK.Logger;

/// <summary>
/// 日志记录接口
/// <para>定义了日志记录的基本方法和类型</para>
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="type">日志类型</param>
    /// <param name="msg">日志消息</param>
    void Log(LogType type, string msg);

    /// <summary>
    /// 记录信息日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    void LogInfo(string msg);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    void LogWarn(string msg);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    void LogError(string msg);

    /// <summary>
    /// 记录致命日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    void LogFatal(string msg);
}