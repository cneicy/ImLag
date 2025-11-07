using System;
using System.Threading;
using System.Threading.Tasks;
using CommonSDK.Event;

namespace ImLag.GUI.Scripts.Core;

public class ChatMessageSentEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}

public class ChatErrorOccurredEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}

public class ChatMessageSender(ConfigManager configManager)
{
    private readonly ConfigManager _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));


    public async Task SendMessageAsync(string message)
    {
        try
        {
            if (!_configManager.Config.SkipWindowCheck && !CSWindowChecker.IsCS2Active())
            {
                EventBus.TriggerEvent(new ChatErrorOccurredEvent("未找到CS2活动窗口"));
                return;
            }
                
            KeySimulator.ReleaseAllKeys();
            await Task.Delay(100);
                
            await ClipboardService.SetTextAsync(message);
            await Task.Delay(50);
                
            if (_configManager.Config.ForceMode)
            {
                for (var i = 0; i < 3; i++)
                {
                    OpenChatBox();
                    await Task.Delay(_configManager.Config.KeyDelay);
                }
            }
            else
            {
                OpenChatBox();
            }

            await Task.Delay(_configManager.Config.KeyDelay * 2);
                
            ClearChatInput();
            await Task.Delay(_configManager.Config.KeyDelay);

            PasteFromClipboard();
            await Task.Delay(_configManager.Config.KeyDelay);
                
            SendEnterKey();

            EventBus.TriggerEvent(new ChatMessageSentEvent(message));
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new ChatErrorOccurredEvent($"发送消息失败: {ex.Message}"));
        }
    }

    private void OpenChatBox()
    {
        var chatKey = _configManager.Config.ChatKey.ToLower();
            
        switch (chatKey)
        {
            case "enter":
                SimulateKey(0x0D);
                break;
            case "y":
            case "u":
            default:
                if (chatKey.Length == 1 && char.IsLetterOrDigit(chatKey[0]))
                {
                    KeySimulator.SimulateKeyPress(chatKey[0]);
                }
                break;
        }
    }

    private void ClearChatInput()
    {
        KeySimulator.SimulateKeyDown((char)0x11); // Ctrl
        Thread.Sleep(50);
        KeySimulator.SimulateKeyPress((char)0x41); // A
        Thread.Sleep(50);
        KeySimulator.SimulateKeyUp((char)0x11); // 释放 Ctrl
        Thread.Sleep(_configManager.Config.KeyDelay);
            
        KeySimulator.SimulateKeyPress((char)0x2E); // Delete
        Thread.Sleep(_configManager.Config.KeyDelay);
    }

    private void PasteFromClipboard()
    {
        KeySimulator.SimulateKeyDown((char)0x11); // Ctrl
        Thread.Sleep(50);
        KeySimulator.SimulateKeyPress((char)0x56); // V
        Thread.Sleep(50);
        KeySimulator.SimulateKeyUp((char)0x11); // 释放 Ctrl
    }

    private void SendEnterKey()
    {
        KeySimulator.SimulateKeyPress((char)0x0D); // Enter
    }

    private void SimulateKey(byte keyCode)
    {
        KeySimulator.SimulateKeyPress((char)keyCode);
    }
}