using System.Text.Json;
using System.Text.Encodings.Web;

namespace ImLag;

public class ChatMessageManager
{
    public List<string> Messages = [];
    private const string MessagesFile = "Messages.txt";
    private readonly Random _random = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ChatMessageManager()
    {
    }

    public void LoadMessages()
    {
        try
        {
            if (File.Exists(MessagesFile))
            {
                var messages = File.ReadAllText(MessagesFile);
                var loadedMessages = messages.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                if (loadedMessages.Length > 0)
                {
                    Messages.AddRange(loadedMessages);
                    Console.WriteLine($"已加载 {Messages.Count} 条死亡消息。");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载消息文件时出错: {ex.Message}");
        }

        LoadDefaultMessages();
        SaveMessages();
    }

    private void LoadDefaultMessages()
    {
        Messages =
        [
            "网卡", "手抖", "高延迟", "鼠标出问题了", "瓶颈期", "手冻僵了", "被阴了",
            "卡输入法了", "day0了", "掉帧了", "手汗手滑", "腱鞘炎犯了", "吞子弹了",
            "timing侠", "唉，资本", "刚打瓦回来不适应", "灵敏度有问题", "谁把我键位改了",
            "感冒了没反应", "拆消音器去了", "校园网是这样的", "状态不行", "鼠标撞键盘上了",
            "复健", "屏幕太小", "键盘坏了", "显示器延迟高", "对面锁了", "他静音"
        ];
        Console.WriteLine("已加载默认死亡消息。");
    }

    public void SaveMessages()
    {
        try
        {
            File.WriteAllText(MessagesFile, string.Join('\n', Messages));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存消息文件时出错: {ex.Message}");
        }
    }

    public string GetRandomMessage()
    {
        if (Messages.Count == 0)
            return string.Empty;

        var index = _random.Next(Messages.Count);
        return Messages[index];
    }

    public List<string> GetAllMessages()
    {
        return [..Messages];
    }

    public void AddMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || Messages.Contains(message.Trim())) return;
        Messages.Add(message.Trim());
        SaveMessages();
    }

    public bool RemoveMessage(int index)
    {
        if (index < 0 || index >= Messages.Count) return false;
        Messages.RemoveAt(index);
        SaveMessages();
        return true;
    }

    public void DisplayMessages()
    {
        if (Messages.Count == 0)
        {
            Console.WriteLine("  消息列表为空。请按A添加。");
            return;
        }

        for (var i = 0; i < Messages.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {Messages[i]}");
        }
    }
}