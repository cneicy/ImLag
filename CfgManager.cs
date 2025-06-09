using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

// ReSharper disable InconsistentNaming

namespace ImLag;

[SuppressMessage("Interoperability", "CA1416:驗證平台相容性")]
public class CfgManager
{
    private readonly ChatMessageManager _chatManager;
    private readonly ConfigManager _configManager;

    public string SteamPath { get; private set; } = string.Empty;
    public string CS2Path => _configManager.Config.CS2Path ?? string.Empty;
    public string CfgPath { get; private set; } = string.Empty;
    public int TotalCfgFiles => _configManager.Config.TotalCfgFiles;
    private readonly Random _random = new();
    public List<string> BindKeys { get; set; }

    public CfgManager(ChatMessageManager chatManager, ConfigManager configManager)
    {
        _chatManager = chatManager;
        _configManager = configManager;

        if (string.IsNullOrEmpty(CS2Path))
        {
            FindCS2Path();
        }
        else
        {
            UpdateCfgPath();
        }

        BindKeys = _configManager.Config.BindKeys;
    }

    public void FindCS2Path()
    {
        try
        {
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                SteamPath = regKey?.GetValue("SteamPath") as string ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(SteamPath))
            {
                var libraryFoldersPath = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
                List<string> steamLibraries = [SteamPath];

                if (File.Exists(libraryFoldersPath))
                {
                    var lines = File.ReadAllLines(libraryFoldersPath);
                    steamLibraries.AddRange(from line in lines
                        select line.Trim()
                        into trimmedLine
                        where trimmedLine.StartsWith("\"path\"")
                        select trimmedLine.Split('\"')[3]
                        into path
                        where Directory.Exists(path)
                        select path);
                }

                string[] possibleRelativePaths =
                {
                    Path.Combine("steamapps", "common", "Counter-Strike Global Offensive"),
                    Path.Combine("steamapps", "common", "Counter-Strike 2")
                };

                foreach (var libPath in steamLibraries.Distinct())
                {
                    foreach (var relativePath in possibleRelativePaths)
                    {
                        var potentialCs2Path = Path.Combine(libPath, relativePath);
                        if (!Directory.Exists(potentialCs2Path) ||
                            !File.Exists(Path.Combine(potentialCs2Path, "game", "csgo", "pak01_dir.vpk")))
                            continue; // 檢查特徵檔案
                        _configManager.Config.CS2Path = potentialCs2Path;
                        _configManager.SaveConfig();
                        UpdateCfgPath();
                        Console.WriteLine($"自動找到CS2路徑: {CS2Path}");
                        Console.WriteLine($"CFG檔案將被寫入: {CfgPath}");
                        return;
                    }
                }
            }

            Console.WriteLine("未能自動找到CS2路徑。請使用選單選項 (S) 手動設定。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"自動尋找CS2路徑時出錯: {ex.Message}");
        }
    }

    private void UpdateCfgPath()
    {
        if (string.IsNullOrEmpty(CS2Path)) return;
        CfgPath = Path.Combine(CS2Path, "game", "csgo", "cfg");

        if (Directory.Exists(CfgPath)) return;
        try
        {
            Directory.CreateDirectory(CfgPath);
            Console.WriteLine($"已建立CFG目錄: {CfgPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"建立CFG目錄 '{CfgPath}' 失敗: {ex.Message}");
        }
    }

    public void SetCS2Path(string path)
    {
        if (Directory.Exists(path) && File.Exists(Path.Combine(path, "game", "csgo", "pak01_dir.vpk"))) // 簡單驗證
        {
            _configManager.Config.CS2Path = path;
            _configManager.SaveConfig();
            UpdateCfgPath();
            Console.WriteLine($"CS2路徑已設定為: {CS2Path}");
            Console.WriteLine($"CFG檔案將被寫入: {CfgPath}");
        }
        else
        {
            Console.WriteLine("無效的CS2路徑，目錄不存在或不是有效的CS2安裝目錄。");
        }
    }

    public void AddBindKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            var normalizedKey = key.ToLower();
            if (!BindKeys.Contains(normalizedKey))
            {
                BindKeys.Add(normalizedKey);
                _configManager.SaveConfig();
                Console.WriteLine($"已新增綁定鍵: {normalizedKey}");
                Console.WriteLine($"目前所有綁定鍵: {string.Join(", ", BindKeys)}");
            }
            else
            {
                Console.WriteLine("該按鍵已在綁定清單中。");
            }
        }
        else
        {
            Console.WriteLine("無效的按鍵，必須是單個字母或數字。");
        }
    }

    public void RemoveBindKey(string key)
    {
        if (BindKeys.Count <= 1)
        {
            Console.WriteLine("至少需要保留一個綁定鍵。");
            return;
        }

        var normalizedKey = key.ToLower();
        if (BindKeys.Remove(normalizedKey))
        {
            _configManager.SaveConfig();
            Console.WriteLine($"已移除綁定鍵: {normalizedKey}");
            Console.WriteLine($"目前所有綁定鍵: {string.Join(", ", BindKeys)}");
        }
        else
        {
            Console.WriteLine("未找到該綁定鍵。");
        }
    }

    public string GetRandomBindKey()
    {
        return BindKeys[_random.Next(BindKeys.Count)];
    }

    public void SetTotalCfgFiles(int count)
    {
        if (count is >= 1 and <= 200)
        {
            _configManager.Config.TotalCfgFiles = count;
            _configManager.SaveConfig();
            Console.WriteLine($"CFG檔案數量已更改為: {TotalCfgFiles}");
        }
        else
        {
            Console.WriteLine("無效的數量，CFG檔案數量應在1-200之間。");
        }
    }

    private string EscapeMessageForCfg(string message)
    {
        message = message.Replace("\"", "\"\"");
        message = message.Replace(";", "");
        return message;
    }

    public bool GenerateConfigFiles()
    {
        var messages = _chatManager.GetAllMessages();
        if (messages.Count == 0)
        {
            Console.WriteLine("\n訊息清單為空，請先按A新增訊息後再產生CFG。");
            return false;
        }

        if (string.IsNullOrEmpty(CS2Path) || !Directory.Exists(CS2Path))
        {
            Console.WriteLine("\nCS2路徑無效或未設定。請先按S設定正確的CS2路徑。");
            return false;
        }

        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            UpdateCfgPath();
            if (!Directory.Exists(CfgPath))
            {
                Console.WriteLine($"\nCFG目錄 '{CfgPath}' 不存在且無法建立。請檢查權限或手動建立。");
                return false;
            }
        }

        try
        {
            Random random = new();
            var shuffledMessages = messages.OrderBy(_ => random.Next()).ToList();
            var actualTotalFiles = Math.Min(TotalCfgFiles, shuffledMessages.Count);

            if (actualTotalFiles < TotalCfgFiles)
            {
                Console.WriteLine(
                    $"注意：訊息數量 ({shuffledMessages.Count}) 少於請求的CFG檔案數 ({TotalCfgFiles})。將只為每條訊息產生一個CFG，共 {actualTotalFiles} 個。");
            }

            for (var i = 0; i < actualTotalFiles; i++)
            {
                var filename = $"imlag_say_{i + 1}.cfg";
                var filePath = Path.Combine(CfgPath, filename);
                var messageToUse = EscapeMessageForCfg(shuffledMessages[i]);

                using (var writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"// ImLag Random Say CFG - File {i + 1}");
                    writer.WriteLine($"// Message: {shuffledMessages[i]}");
                    writer.WriteLine($"say \"{messageToUse}\"");
                }

                Console.WriteLine($"已產生: {filePath}");
            }

            for (var i = actualTotalFiles; i < 200; i++)
            {
                var oldFilename = $"imlag_say_{i + 1}.cfg";
                var oldFilePath = Path.Combine(CfgPath, oldFilename);
                if (!File.Exists(oldFilePath)) continue;
                File.Delete(oldFilePath);
                Console.WriteLine($"已刪除舊檔案: {oldFilePath}");
            }

            if (actualTotalFiles > 0)
            {
                GenerateSelectorFile(actualTotalFiles);
                Console.WriteLine($"\n成功產生 {actualTotalFiles} 個訊息CFG檔案和1個選擇器檔案。");
            }
            else
            {
                Console.WriteLine("\n沒有可用的訊息來產生CFG檔案。");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"產生CFG檔案時出錯: {ex.Message}");
            return false;
        }
    }

    private void GenerateSelectorFile(int numberOfMessageFiles)
    {
        if (numberOfMessageFiles == 0) return;

        var selectorFilePath = Path.Combine(CfgPath, "imlag_say_selector.cfg");
        using (var writer = new StreamWriter(selectorFilePath, false))
        {
            writer.WriteLine("// ImLag Random Say Selector CFG");
            writer.WriteLine($"// Cycles through {numberOfMessageFiles} message CFGs.");
            writer.WriteLine();

            for (int i = 1; i <= numberOfMessageFiles; i++)
            {
                int nextFileIndex = i % numberOfMessageFiles + 1;
                writer.WriteLine(
                    $"alias imlag_random_say_{i} \"exec imlag_say_{i}; alias imlag_do_say imlag_random_say_{nextFileIndex}\"");
            }

            writer.WriteLine();
            writer.WriteLine($"alias imlag_do_say imlag_random_say_1");
            writer.WriteLine($"imlag_do_say");
        }

        Console.WriteLine($"已產生選擇器檔案: {selectorFilePath}");
    }

    public bool UpdateAutoexecFile()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            Console.WriteLine("\nCFG路徑無效或未設定，無法更新autoexec.cfg。");
            return false;
        }

        string autoexecFilePath = Path.Combine(CfgPath, "autoexec.cfg");
        string imlagCommentStart = "// --- ImLag Auto-Bind Start ---";
        string imlagCommentEnd = "// --- ImLag Auto-Bind End ---";

        try
        {
            List<string> lines = [];
            bool autoexecExists = File.Exists(autoexecFilePath);

            if (autoexecExists)
            {
                lines.AddRange(File.ReadAllLines(autoexecFilePath));
                int startIndex = -1, endIndex = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == imlagCommentStart) startIndex = i;
                    if (lines[i].Trim() == imlagCommentEnd && startIndex != -1)
                    {
                        endIndex = i;
                        break;
                    }
                }

                if (startIndex != -1 && endIndex != -1)
                {
                    lines.RemoveRange(startIndex, endIndex - startIndex + 1);
                    Console.WriteLine("已從autoexec.cfg中移除舊的ImLag綁定。");
                }

                lines.RemoveAll(line => line.Contains("exec imlag_say_selector"));
            }
            else
            {
                Console.WriteLine($"autoexec.cfg 檔案不存在於 '{CfgPath}'，將建立一個新的。");
                lines.Add("// Counter-Strike 2 Autoexec Configuration File");
                lines.Add("// Generated by ImLag");
                lines.Add("");
            }

            lines.Add("");
            lines.Add(imlagCommentStart);
            lines.Add($"// This block is automatically managed by ImLag v{Program.Version}");
            foreach (var key in BindKeys)
            {
                lines.Add($"bind \"{key}\" \"exec imlag_say_selector\"");
                lines.Add($"echo \"ImLag: '{key}' bound to random message selector.\"");
            }

            lines.Add(imlagCommentEnd);
            lines.Add("");

            lines.RemoveAll(line => line.Trim().ToLower() == "host_writeconfig");
            lines.Add("host_writeconfig");

            File.WriteAllLines(autoexecFilePath, lines);
            Console.WriteLine($"已{(autoexecExists ? "更新" : "建立")} autoexec.cfg 檔案並新增/更新綁定。");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新/建立 autoexec.cfg 時出錯: {ex.Message}");
            return false;
        }
    }

    public void ShowCfgInstructions()
    {
        Console.WriteLine("\n=== CFG模式設定完成/說明 ===");
        if (string.IsNullOrEmpty(CS2Path))
        {
            Console.WriteLine("CS2路徑尚未設定，部分功能可能受限。請按S設定。");
            return;
        }

        Console.WriteLine($"CFG檔案已產生在: {CfgPath}");
        Console.WriteLine(
            $"1. 包含隨機訊息的CFG檔案: imlag_say_1.cfg ... imlag_say_{Math.Min(TotalCfgFiles, _chatManager.GetAllMessages().Count)}.cfg");
        Console.WriteLine($"2. 選擇器CFG檔案: imlag_say_selector.cfg");
        Console.WriteLine(
            $"3. autoexec.cfg 中應已新增綁定: bind \"{string.Join(", ", BindKeys)}\" \"exec imlag_say_selector\"");
        Console.WriteLine();
        Console.WriteLine("使用方法:");
        Console.WriteLine($"  - 在CS2遊戲中，按下您設定的綁定鍵 (目前為: '{string.Join(", ", BindKeys)}') 即可發送一條隨機訊息。");
        Console.WriteLine("  - 每次按下綁定鍵都會發送不同的訊息，循環播放。");
        Console.WriteLine();
        Console.WriteLine("重要提示 - 確保autoexec.cfg被執行:");
        Console.WriteLine("  如果您是首次設定或遇到問題，請確保CS2啟動時會執行autoexec.cfg。");
        Console.WriteLine("  方法1: 在Steam中，右鍵點擊CS2 -> '內容...' -> '一般' -> '啟動選項'，");
        Console.WriteLine("          在輸入框中新增(如果已有其他選項，用空格隔開): +exec autoexec.cfg");
        Console.WriteLine("  方法2: 在遊戲主控台中手動輸入 `exec autoexec.cfg` 來測試。");
        Console.WriteLine("          如果autoexec.cfg被正確執行，您應該能在主控台看到類似 \"ImLag: 'X' bound...\" 的訊息。");
        Console.WriteLine();
    }
}