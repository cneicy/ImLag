using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonSDK.Event;

namespace ImLag.GUI.Scripts.Core;

public class MessageAddedEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}
public class MessageRemovedEvent(string msg) : EventBase
{
    public string Message { get; set; } = msg;
}

public class ChatMessageManager
{
    private readonly List<string> _messages = [];
    private readonly Random _random = new();
    private const string MessagesTxtFile = "Messages.txt";

    public int MessageCount => _messages.Count;

    public void LoadMessages()
    {
        try
        {
            if (File.Exists(MessagesTxtFile))
            {
                LoadFromTxtFile();
            }
            else
            {
                LoadDefaultMessages();
                SaveMessages();
            }
        }
        catch (Exception)
        {
            LoadDefaultMessages();
        }
    }

    private void LoadFromTxtFile()
    {
        var lines = File.ReadAllLines(MessagesTxtFile);
        _messages.Clear();
        _messages.AddRange(lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()));
    }

    private void LoadDefaultMessages()
    {
        _messages.Clear();
        _messages.AddRange([
            "网卡",
            "手抖",
            "高延迟",
            "鼠标出问题了",
            "瓶颈期",
            "手冻僵了",
            "被阴了",
            "卡输入法了",
            "day0了",
            "掉帧了",
            "手汗手滑",
            "腱鞘炎犯了",
            "吞子弹了",
            "timing侠",
            "唉，资本",
            "刚打瓦回来不适应",
            "灵敏度有问题",
            "谁把我键位改了",
            "感冒了没反应",
            "拆消音器去了",
            "校园网是这样的",
            "状态不行",
            "鼠标撞键盘上了",
            "复健",
            "屏幕太小",
            "键盘坏了",
            "显示器延迟高",
            "对面锁了",
            "他静音"
        ]);

        SaveToTxtFile();
    }

    public void SaveMessages()
    {
        try
        {
            SaveToTxtFile();
        }
        catch (Exception)
        {
           // ignored 
        }
    }

    private void SaveToTxtFile()
    {
        try
        {
            File.WriteAllLines(MessagesTxtFile, _messages);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public bool AddMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        var trimmedMessage = message.Trim();
        if (_messages.Contains(trimmedMessage)) return false;

        _messages.Add(trimmedMessage);
        SaveMessages();
        EventBus.TriggerEvent(new MessageAddedEvent(trimmedMessage));
        return true;
    }

    public bool RemoveMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        var removed = _messages.Remove(message.Trim());
        if (!removed) return removed;
        SaveMessages();
        EventBus.TriggerEvent(new MessageRemovedEvent(message));
        return removed;
    }

    public string GetRandomMessage()
    {
        return _messages.Count == 0 ? string.Empty : _messages[_random.Next(_messages.Count)];
    }

    public List<string> GetAllMessages()
    {
        return [.._messages];
    }
}