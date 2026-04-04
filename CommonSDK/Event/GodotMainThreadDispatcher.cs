using System.Threading;
using Godot;

namespace CommonSDK.Event;

public static class GodotMainThreadDispatcher
{
    private static int _mainThreadId;

    public static void CaptureCurrentThread()
    {
        var currentThreadId = System.Environment.CurrentManagedThreadId;
        Interlocked.CompareExchange(ref _mainThreadId, currentThreadId, 0);
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref _mainThreadId, 0);
    }

    public static Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var mainThreadId = Volatile.Read(ref _mainThreadId);
        if (mainThreadId == 0 || System.Environment.CurrentManagedThreadId == mainThreadId)
        {
            action();
            return Task.CompletedTask;
        }

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Callable.From(() =>
        {
            try
            {
                action();
                completionSource.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
            }
        }).CallDeferred();

        return completionSource.Task;
    }
}
