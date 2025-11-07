using System;
using System.Threading.Tasks;
using CommonSDK.Event;

namespace ImLag.GUI.Scripts.Core;

public class ClipboardErrorOccurredEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}
public static class ClipboardService
{
    public static async Task SetTextAsync(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return;
                
            await TextCopy.ClipboardService.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new ClipboardErrorOccurredEvent($"设置剪贴板时出错: {ex.Message}"));
        }
    }
}