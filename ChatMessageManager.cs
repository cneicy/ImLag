using System.Text.Json;
using System.Text.Encodings.Web;

namespace ImLag;

public class ChatMessageManager
{
    public List<string> Messages = [];
    private const string MessagesFile = "Messages.json";
    private readonly Random _random = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ChatMessageManager()
    {
        LoadMessages();
    }

    public void LoadMessages()
    {
        try
        {
            if (File.Exists(MessagesFile))
            {
                var json = File.ReadAllText(MessagesFile);
                var loadedMessages = JsonSerializer.Deserialize<List<string>>(json);
                if (loadedMessages != null)
                {
                    Messages = loadedMessages;
                    Console.WriteLine($"已載入 {Messages.Count} 條死亡訊息。");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入訊息檔案時出錯: {ex.Message}");
        }

        LoadDefaultMessages();
        SaveMessages();
    }

    private void LoadDefaultMessages()
    {
        Messages =
        [
            "網卡", "手抖", "高延遲", "滑鼠出問題了", "瓶頸期", "手凍僵了", "被陰了",
            "卡輸入法了", "day0了", "掉幀了", "手汗手滑", "腱鞘炎犯了", "吞子彈了",
            "timing俠", "唉，資本", "剛打瓦回來不適應", "靈敏度有問題", "誰把我鍵位改了",
            "感冒了沒反應", "拆消音器去了", "校園網是這樣的", "狀態不行", "滑鼠撞鍵盤上了",
            "復健", "螢幕太小", "鍵盤壞了", "顯示器延遲高", "對面鎖了", "他靜音"
        ];
        Console.WriteLine("已載入預設死亡訊息。");
    }

    public void SaveMessages()
    {
        try
        {
            var json = JsonSerializer.Serialize(Messages, _jsonOptions);
            File.WriteAllText(MessagesFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"儲存訊息檔案時出錯: {ex.Message}");
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
            Console.WriteLine("  訊息清單為空。請按A新增。");
            return;
        }

        for (var i = 0; i < Messages.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {Messages[i]}");
        }
    }
}