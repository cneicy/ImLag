namespace CommonSDK.Event;

/// <summary>
/// 不支持的操作异常（C# 内置异常的替代）
/// </summary>
/// <remarks>
/// <para>当尝试对不支持的事件执行某些操作时抛出此异常</para>
/// <para>例如，对不可取消的事件调用取消方法</para>
/// </remarks>
public class UnsupportedOperationException : InvalidOperationException
{
    /// <summary>
    /// 初始化 <see cref="UnsupportedOperationException"/> 类的新实例
    /// </summary>
    public UnsupportedOperationException() : base()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="UnsupportedOperationException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述错误的消息</param>
    public UnsupportedOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="UnsupportedOperationException"/> 类的新实例
    /// </summary>
    /// <param name="message">解释异常原因的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个 null 引用</param>
    public UnsupportedOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}