using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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

public class CorpusImportedEvent : EventBase
{
    public int AddedCount { get; init; }
    public int SkippedCount { get; init; }
    public string Source { get; init; } = string.Empty;
}

public class CorpusExportedEvent : EventBase
{
    public int ExportedCount { get; init; }
    public string Path { get; init; } = string.Empty;
}

public class CorpusOperationFailedEvent : EventBase
{
    public string Message { get; init; } = string.Empty;
}

public record CorpusImportResult(int AddedCount, int SkippedCount);

public class ChatMessageManager
{
    private readonly List<string> _messages = [];
    private readonly Random _random = new();
    private static readonly HttpClient HttpClient = new();
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

    public CorpusImportResult ImportMessages(IEnumerable<string> messages, string source = "")
    {
        var addedCount = 0;
        var skippedCount = 0;

        foreach (var rawMessage in messages)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                skippedCount++;
                continue;
            }

            var trimmedMessage = rawMessage.Trim();
            if (_messages.Contains(trimmedMessage))
            {
                skippedCount++;
                continue;
            }

            _messages.Add(trimmedMessage);
            addedCount++;
        }

        if (addedCount > 0)
        {
            SaveMessages();
        }

        EventBus.TriggerEvent(new CorpusImportedEvent
        {
            AddedCount = addedCount,
            SkippedCount = skippedCount,
            Source = source
        });
        return new CorpusImportResult(addedCount, skippedCount);
    }

    public CorpusImportResult ImportMessagesFromFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("Import file not found.", path);
            }

            var content = File.ReadAllText(path);
            return ImportMessages(ParseMessages(content), Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new CorpusOperationFailedEvent
            {
                Message = LocalizationManager.T("corpus.error.import_file_failed", ex.Message)
            });
            return new CorpusImportResult(0, 0);
        }
    }

    public async Task<CorpusImportResult> ImportMessagesFromUrlAsync(string url)
    {
        try
        {
            if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException(LocalizationManager.T("corpus.error.invalid_url"));
            }

            var content = await HttpClient.GetStringAsync(uri);
            return ImportMessages(ParseMessages(content), uri.Host);
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new CorpusOperationFailedEvent
            {
                Message = LocalizationManager.T("corpus.error.import_url_failed", ex.Message)
            });
            return new CorpusImportResult(0, 0);
        }
    }

    public bool ExportMessages(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(LocalizationManager.T("corpus.error.invalid_export_path"));
            }

            File.WriteAllLines(path, _messages);
            EventBus.TriggerEvent(new CorpusExportedEvent
            {
                ExportedCount = _messages.Count,
                Path = path
            });
            return true;
        }
        catch (Exception ex)
        {
            EventBus.TriggerEvent(new CorpusOperationFailedEvent
            {
                Message = LocalizationManager.T("corpus.error.export_failed", ex.Message)
            });
            return false;
        }
    }

    private static List<string> ParseMessages(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var trimmedContent = content.Trim();
        if (trimmedContent.StartsWith("[") && trimmedContent.EndsWith("]"))
        {
            try
            {
                var jsonMessages = JsonSerializer.Deserialize<List<string>>(trimmedContent);
                if (jsonMessages is { Count: > 0 })
                {
                    return jsonMessages
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Select(message => message.Trim())
                        .ToList();
                }
            }
            catch (JsonException)
            {
                // Ignore and fall back to line parsing.
            }
        }

        return trimmedContent
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
