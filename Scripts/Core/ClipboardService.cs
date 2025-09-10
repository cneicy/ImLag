using System;
using System.Threading.Tasks;

namespace ImLag.GUI.Scripts.Core;

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
            throw new InvalidOperationException($"设置剪贴板时出错: {ex.Message}", ex);
        }
    }
}