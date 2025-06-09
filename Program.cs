using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using TextCopy;
using WindowsInput;
using WindowsInput.Native;

// ReSharper disable InconsistentNaming

namespace ImLag;

[SuppressMessage("Interoperability", "CA1416:驗證平台相容性")]
internal static partial class Program
{
    public const string Version = "2.0.4";
    private const string Author = "Eicy";
    private const string Name = "ImLag";
    private const string UpdateLog = "優化CS檢測模式、新增WinFormSendKeys模擬模式。";

    private static GameStateListener? _gsl;
    private static ChatMessageManager _chatManager;
    private static ConfigManager _configManager;
    private static CfgManager _cfgManager;
    private static readonly InputSimulator _inputSimulator = new();
    private static bool _useInputSimulator;
    private static bool _useSendInput;
    private static bool _useWinFormSendKeys;

    [STAThread]
    private static void Main()
    {
        Console.Title = $"{Name} v{Version} by {Author}";

        if (!IsRunningAsAdministrator())
        {
            Console.WriteLine("警告：請以系統管理員身分執行以確保按鍵發送和CFG寫入正常。");
            Console.WriteLine("按任意鍵繼續...");
            Console.ReadKey();
            Console.Clear();
        }

        _configManager = new ConfigManager();
        _configManager.LoadConfig();

        _chatManager = new ChatMessageManager();
        _chatManager.LoadMessages();

        _cfgManager = new CfgManager(_chatManager, _configManager);

        _useInputSimulator = _configManager.Config.KeySimulationMethod == 1;
        _useSendInput = _configManager.Config.KeySimulationMethod == 2;
        _useWinFormSendKeys = _configManager.Config.KeySimulationMethod == 3;
        

        if (string.IsNullOrWhiteSpace(_configManager.Config.UserPlayerName))
        {
            SetupPlayerName();
        }

        _gsl = new GameStateListener(4000);
        if (!_gsl.GenerateGSIConfigFile("ImLag"))
        {
            Console.WriteLine("無法產生GSI設定檔。");
        }

        _gsl.PlayerDied += OnPlayerDied;

        Console.WriteLine(!_gsl.Start() ? "GSI啟動失敗，請以系統管理員身分執行。" : "正在監聽CS2遊戲事件 (GSI)...");

        Console.WriteLine($"=== {Name} v{Version} by {Author} ===");
        Init();
        ConsoleKeyInfo keyInfo;
        do
        {
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }

            keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                    AddNewMessage();
                    break;
                case ConsoleKey.D:
                    DeleteMessage();
                    break;
                case ConsoleKey.L:
                    Console.WriteLine("\n訊息清單：");
                    _chatManager.DisplayMessages();
                    break;
                case ConsoleKey.C:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeChatKey();
                    else
                        Console.WriteLine("\nCFG模式無需設定聊天按鍵，請用綁定鍵。");
                    break;
                case ConsoleKey.P:
                    ChangePlayerName();
                    break;
                case ConsoleKey.M:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleMonitorMode();
                    else
                        Console.WriteLine("\nCFG模式下監聽模式無效。");
                    break;
                case ConsoleKey.W:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleWindowCheck();
                    else
                        Console.WriteLine("\nCFG模式下視窗檢測無效。");
                    break;
                case ConsoleKey.F:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleForceMode();
                    else
                        Console.WriteLine("\nCFG模式無需強制發送模式。");
                    break;
                case ConsoleKey.K:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeKeyDelay();
                    else
                        Console.WriteLine("\nCFG模式下按鍵延遲無效。");
                    break;
                case ConsoleKey.V:
                    ShowVersionInfo();
                    break;
                case ConsoleKey.I:
                    ToggleInputMethod();
                    break;
                case ConsoleKey.T:
                    ToggleOperationalMode();
                    break;
                case ConsoleKey.B:
                    if (_configManager.Config.UseCfgMode)
                        ChangeBindKey();
                    else
                        Console.WriteLine("\n請先切換到CFG模式 (T)。");
                    break;
                case ConsoleKey.S:
                    if (_configManager.Config.UseCfgMode)
                        SetCS2Path();
                    else
                        Console.WriteLine("\n請先切換到CFG模式 (T)。");
                    break;
                case ConsoleKey.G:
                    if (_configManager.Config.UseCfgMode)
                        GenerateCfgFiles();
                    else
                        Console.WriteLine("\n請先切換到CFG模式 (T)。");
                    break;
                default:
                    Console.WriteLine("\n無效按鍵，請查看下方提示。");
                    Init();
                    break;
            }
        } while (keyInfo.Key != ConsoleKey.Escape);
        _configManager.SaveConfig();
        _chatManager.SaveMessages();
        Console.WriteLine("程式已結束。");
    }
    

    private static void ToggleOperationalMode()
    {
        _configManager.Config.UseCfgMode = !_configManager.Config.UseCfgMode;
        _configManager.SaveConfig();
        Console.Clear();
        Console.WriteLine($"\n已切換到 {(_configManager.Config.UseCfgMode ? "CFG模式" : "聊天模式")}");

        if (_configManager.Config.UseCfgMode)
        {
            Console.WriteLine("CFG模式：透過遊戲內按鍵發送訊息，需產生CFG檔案 (G)。");
            if (string.IsNullOrEmpty(_configManager.Config.CS2Path))
            {
                Console.WriteLine("\nCS2路徑未設定，嘗試自動尋找...");
                _cfgManager.FindCS2Path();
            }

            if (_gsl == null)
            {
                _gsl = new GameStateListener(4000);
                if (!_gsl.GenerateGSIConfigFile("ImLag"))
                {
                    Console.WriteLine("無法產生GSI設定檔。");
                }

                _gsl.PlayerDied += OnPlayerDied;
            }

            if (!_gsl.Running)
            {
                Console.WriteLine(_gsl.Start() ? "GSI監聽啟動。" : "GSI啟動失敗，請以系統管理員身分執行。");
            }

            _cfgManager.ShowCfgInstructions();
        }
        else
        {
            if (_gsl == null)
            {
                _gsl = new GameStateListener(4000);
                if (!_gsl.GenerateGSIConfigFile("ImLag"))
                {
                    Console.WriteLine("無法產生GSI設定檔。");
                }

                _gsl.PlayerDied += OnPlayerDied;
            }

            if (!_gsl.Running)
            {
                Console.WriteLine(_gsl.Start() ? "GSI監聽啟動。" : "GSI啟動失敗，請以系統管理員身分執行。");
            }

            Console.WriteLine("聊天模式：GSI檢測死亡，自動發送訊息。");
        }

        Init();
    }

    private static void ChangeBindKey()
    {
        while (true)
        {
            Console.WriteLine($"\n綁定鍵: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine("1. 新增綁定鍵");
            Console.WriteLine("2. 刪除綁定鍵");
            Console.WriteLine("3. 返回");
            Console.Write("選擇 (1-3): ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    Console.Write("輸入綁定鍵 (字母/數字): ");
                    var input = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.AddBindKey(input);
                    break;
                case "2":
                    if (_cfgManager.BindKeys.Count <= 1)
                    {
                        Console.WriteLine("需保留至少一個綁定鍵。");
                        break;
                    }

                    Console.Write("輸入要刪除的綁定鍵: ");
                    var keyToRemove = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.RemoveBindKey(keyToRemove);
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("無效選擇。");
                    break;
            }
        }
    }

    private static void SetCS2Path()
    {
        Console.WriteLine($"\nCS2路徑: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未設定")}");
        Console.Write(
            @"輸入CS2根目錄路徑 (如: C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive): ");
        var path = Console.ReadLine()?.Trim() ?? "";
        _cfgManager.SetCS2Path(path);
    }

    private static void UpdateCfgCount()
    {
        _cfgManager.SetTotalCfgFiles(_chatManager.Messages.Count);
    }

    private static void GenerateCfgFiles()
    {
        Console.WriteLine("\n=== 產生CFG檔案 ===");
        if (!_cfgManager.GenerateConfigFiles()) return;
        Console.WriteLine("CFG檔案已產生。");
        Console.WriteLine("是否更新 autoexec.cfg 以套用綁定? (y/n)");
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.Y)
        {
            if (_cfgManager.UpdateAutoexecFile())
            {
                _cfgManager.ShowCfgInstructions();
            }
        }
        else
        {
            Console.WriteLine("\n已跳過更新 autoexec.cfg。");
            Console.WriteLine("請手動新增以下內容到 autoexec.cfg:");
            Console.WriteLine($"bind \"{string.Join(", ", _cfgManager.BindKeys)}\" \"exec random_say_selector\"");
            Console.WriteLine($"路徑: {_cfgManager.CfgPath}\\autoexec.cfg");
        }
    }

    private static void ToggleInputMethod()
    {
        var currentMethod = _configManager.Config.KeySimulationMethod;
        var nextMethod = (currentMethod + 1) % 4;

        _configManager.Config.KeySimulationMethod = nextMethod;
        _configManager.SaveConfig();
        
        _useInputSimulator = nextMethod == 1;
        _useSendInput = nextMethod == 2;
        _useWinFormSendKeys = nextMethod == 3;

        Console.WriteLine(nextMethod switch
        {
            0 => "\n模擬方式切換為: keybd_event",
            1 => "\n模擬方式切換為: InputSimulator",
            2 => "\n模擬方式切換為: SendInput",
            3 => "\n模擬方式切換為: WinForm SendKeys",
            _ => "\n模擬方式切換為: keybd_event"
        });
        
        if (_useInputSimulator)
            Console.WriteLine("InputSimulator 模式，可能需要系統管理員權限。");
        else if (_useSendInput)
            Console.WriteLine("SendInput 模式，更現代且可靠。");
        else if (_useWinFormSendKeys)
            Console.WriteLine("WinForm SendKeys 模式，依賴使用中視窗。");
        else
            Console.WriteLine("keybd_event 模式，相容性較佳。");
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static void ToggleForceMode()
    {
        _configManager.Config.ForceMode = !_configManager.Config.ForceMode;
        _configManager.SaveConfig();

        Console.WriteLine($"\n強制發送模式已{(_configManager.Config.ForceMode ? "啟用" : "停用")}");
        if (_configManager.Config.ForceMode)
        {
            Console.WriteLine("警告：強制模式可能導致重複發送，僅在發送失敗時啟用。");
        }
    }

    private static void ChangeKeyDelay()
    {
        Console.WriteLine($"\n目前按鍵延遲: {_configManager.Config.KeyDelay}ms");
        Console.Write("輸入新延遲 (30-500ms): ");

        if (int.TryParse(Console.ReadLine(), out var delay) && delay is >= 30 and <= 500)
        {
            _configManager.Config.KeyDelay = delay;
            _configManager.SaveConfig();
            Console.WriteLine($"延遲更新為: {delay}ms");
        }
        else
        {
            Console.WriteLine("無效輸入，需在30-500ms之間。");
        }
    }

    private static void ToggleMonitorMode()
    {
        _configManager.Config.OnlySelfDeath = !_configManager.Config.OnlySelfDeath;
        _configManager.SaveConfig();

        Console.WriteLine($"\n監聽模式切換為: {(_configManager.Config.OnlySelfDeath ? "僅自己死亡" : "所有玩家死亡")}");
    }

    private static void ToggleWindowCheck()
    {
        _configManager.Config.SkipWindowCheck = !_configManager.Config.SkipWindowCheck;
        _configManager.SaveConfig();

        Console.WriteLine($"\n視窗檢測已{(_configManager.Config.SkipWindowCheck ? "停用" : "啟用")}");
        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("警告：停用視窗檢測可能在非CS2視窗發送訊息。");
        }
    }

    private static void ShowVersionInfo()
    {
        Console.WriteLine($"\n=== {Name} v{Version} ===");
        Console.WriteLine($"作者: {Author}");
        Console.WriteLine("GitHub: https://github.com/cneicy/ImLag");
        Console.WriteLine($"更新: {UpdateLog}");
    }

    private static void SetupPlayerName()
    {
        Console.WriteLine("\n=== 首次設定 ===");
        Console.WriteLine("請輸入CS2遊戲內玩家名稱（需完全一致）：");

        while (true)
        {
            Console.Write("玩家名稱: ");
            var playerName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                Console.WriteLine("玩家名稱不能為空。");
                continue;
            }

            _configManager.Config.UserPlayerName = playerName.Trim();
            _configManager.SaveConfig();
            Console.WriteLine($"玩家名稱設定: {playerName}");
            break;
        }
    }

    private static void ChangePlayerName()
    {
        Console.WriteLine(
            $"\n目前玩家名稱: {(_configManager.Config.UserPlayerName == "" ? "未設定" : _configManager.Config.UserPlayerName)}");
        Console.Write("輸入新玩家名稱 (Enter取消, '-'清空): ");
        var playerName = Console.ReadLine();

        if (string.IsNullOrEmpty(playerName))
        {
            Console.WriteLine("已取消。");
            return;
        }

        if (playerName == "-")
        {
            _configManager.Config.UserPlayerName = string.Empty;
            Console.WriteLine("玩家名稱已清空。");
        }
        else
        {
            _configManager.Config.UserPlayerName = playerName.Trim();
            Console.WriteLine($"玩家名稱更新: {playerName}");
        }

        _configManager.SaveConfig();
    }

    private static void Init()
    {
        Console.Clear();
        Console.WriteLine($"=== ImLag v{Version} ===");
        Console.WriteLine();

        if (_configManager.Config.UseCfgMode)
        {
            Console.WriteLine("**CFG模式** (按鍵發送訊息)");
            Console.WriteLine($"綁定鍵: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine($"CFG數量: {_cfgManager.TotalCfgFiles}");
            Console.WriteLine($"CS2路徑: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未設定 (S)")}");
            Console.WriteLine($"訊息數: {_chatManager.Messages.Count}");
            var simulationMethod = _useInputSimulator ? "InputSimulator" :
                _useSendInput ? "SendInput" :
                _useWinFormSendKeys ? "WinForm SendKeys" : "keybd_event";
            Console.WriteLine($"模擬: {simulationMethod}");
            UpdateCfgCount();
            Console.WriteLine("\n操作: T-切換聊天模式 | A-加訊息 | D-刪訊息 | L-列訊息");
            Console.WriteLine("      B-改綁定鍵 | S-設CS2路徑 | G-產生CFG");
        }
        else
        {
            var chatKeyDescription = _configManager.Config.ChatKey switch
            {
                "y" => "全域",
                "u" => "團隊",
                "enter" => "回車",
                _ => $"自訂 ({_configManager.Config.ChatKey})"
            };
            Console.WriteLine("**聊天模式** (GSI自動發送)");
            Console.WriteLine($"聊天鍵: {chatKeyDescription}");
            Console.WriteLine(
                $"監聽: {(_configManager.Config.OnlySelfDeath ? "僅自己" : "所有")} (玩家: {_configManager.Config.UserPlayerName})");
            Console.WriteLine($"視窗檢測: {(_configManager.Config.SkipWindowCheck ? "停用" : "啟用")}");
            Console.WriteLine($"強制發送: {(_configManager.Config.ForceMode ? "啟用" : "停用")}");
            Console.WriteLine($"延遲: {_configManager.Config.KeyDelay}ms");
            var simulationMethod = _useInputSimulator ? "InputSimulator" :
                _useSendInput ? "SendInput" :
                _useWinFormSendKeys ? "WinForm SendKeys" : "keybd_event";
            Console.WriteLine($"模擬: {simulationMethod}");
            Console.WriteLine($"訊息數: {_chatManager.Messages.Count}");
            Console.WriteLine("\n操作: T-切換CFG模式 | A-加訊息 | D-刪訊息 | L-列訊息 | C-改聊天鍵");
            Console.WriteLine("      P-改玩家名 | M-切換監聽 | W-切換視窗檢測 | F-強制發送 | K-改延遲 | I-切換模擬");
        }

        Console.WriteLine("\n通用: V-版本 | ESC-離開");
        Console.Write("選擇操作: ");
    }

    private static void OnPlayerDied(PlayerDied gameEvent)
    {
        Console.WriteLine($"\n[GSI] 玩家死亡: {gameEvent.Player.Name}");

        if (_configManager.Config.OnlySelfDeath && gameEvent.Player.Name != _configManager.Config.UserPlayerName)
        {
            Console.WriteLine("[GSI] 僅監聽自己死亡，跳過。");
            return;
        }

        if (_configManager.Config.OnlySelfDeath && string.IsNullOrEmpty(_configManager.Config.UserPlayerName))
        {
            Console.WriteLine("[GSI] 未設定玩家名稱，監聽所有死亡。");
        }

        Console.WriteLine(gameEvent.Player.Name == _configManager.Config.UserPlayerName ||
                          string.IsNullOrEmpty(_configManager.Config.UserPlayerName)
            ? "[GSI] 你死了！"
            : $"[GSI] 隊友 {gameEvent.Player.Name} 死亡！");

        if (!_configManager.Config.SkipWindowCheck && !IsCS2Active())
        {
            Console.WriteLine("[GSI] CS2非使用中視窗，跳過。");
            return;
        }

        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("[GSI] 跳過視窗檢測，發送訊息。");
        }

        if (_configManager.Config.UseCfgMode)
        {
            var randomKey = _cfgManager.GetRandomBindKey();
            SimulateBindKey(randomKey);
            Console.WriteLine($"[GSI] 模擬按鍵: {randomKey}");
        }
        else
        {
            var randomMessage = _chatManager.GetRandomMessage();
            if (!string.IsNullOrEmpty(randomMessage))
            {
                SendChatMessage(randomMessage);
                Console.WriteLine($"[GSI] 發送訊息: {randomMessage}");
            }
            else
            {
                Console.WriteLine("[GSI] 訊息清單為空。");
            }
        }
    }

    private static void SimulateBindKey(string bindKey)
    {
        if (string.IsNullOrEmpty(bindKey)) return;
        if (_useInputSimulator)
        {
            var vk = GetVirtualKeyFromChar(bindKey[0]);
            if (vk != VirtualKeyCode.NONAME)
                _inputSimulator.Keyboard.KeyPress(vk);
        }
        else if (_useSendInput)
        {
            SendKeySendInput((byte)VkKeyScan(bindKey[0]));
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait(bindKey);
        }
        else
        {
            SendKeyNative((byte)VkKeyScan(bindKey[0]));
        }
    }

    private static void SendChatMessage(string message)
    {
        try
        {
            Console.WriteLine($"[GSI] 準備發送: {message}");
            Console.WriteLine(
                $"[GSI] 模擬方式: {(_useInputSimulator ? "InputSimulator" : _useSendInput ? "SendInput" : _useWinFormSendKeys ? "WinFormSendKeys" : "keybd_event")}");

            ClipboardService.SetText(message);
            ReleaseAllKeys();
            Thread.Sleep(300);

            if (_configManager.Config.ForceMode)
            {
                for (var i = 0; i < 3; i++)
                {
                    OpenChatBox();
                    Thread.Sleep(_configManager.Config.KeyDelay);
                }
            }
            else
            {
                OpenChatBox();
            }

            Thread.Sleep(_configManager.Config.KeyDelay * 2);
            ClearChatInput();
            Thread.Sleep(_configManager.Config.KeyDelay);

            for (var retry = 0; retry < 3; retry++)
            {
                try
                {
                    PasteFromClipboard();
                    break;
                }
                catch (Exception ex)
                {
                    if (retry < 2)
                    {
                        Console.WriteLine($"[GSI] 貼上失敗，重試 ({retry + 1}/3)... 錯誤: {ex.Message}");
                        if (_useInputSimulator && ex.Message.Contains("not sent successfully"))
                        {
                            Console.WriteLine("[GSI] InputSimulator權限問題，切換到SendInput...");
                            _useInputSimulator = false;
                            _useSendInput = true;
                        }
                        else if (_useSendInput && ex.Message.Contains("not sent successfully"))
                        {
                            Console.WriteLine("[GSI] SendInput權限問題，切換到keybd_event...");
                            _useSendInput = false;
                        }

                        Thread.Sleep(_configManager.Config.KeyDelay);
                    }
                    else throw;
                }
            }

            Thread.Sleep(_configManager.Config.KeyDelay);
            SendEnterKey();
            Console.WriteLine("[GSI] 發送完成。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GSI] 發送出錯: {ex.Message}");
            if (ex.Message.Contains("not sent successfully"))
            {
                Console.WriteLine("\n[GSI] 解決建議：");
                Console.WriteLine("1. 以系統管理員身分執行。");
                Console.WriteLine("2. 按I切換模擬方式。");
                Console.WriteLine("3. 確保CS2無更高權限。");
                if (_useInputSimulator)
                {
                    _useInputSimulator = false;
                    _useSendInput = true;
                    Console.WriteLine("[GSI] 切換到SendInput。");
                }
                else if (_useSendInput)
                {
                    _useSendInput = false;
                    Console.WriteLine("[GSI] 切換到keybd_event。");
                }
            }
        }
    }

    private static void OpenChatBox()
    {
        if (_useInputSimulator)
        {
            try
            {
                if (_configManager.Config.ChatKey == "enter") _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                else
                {
                    var virtualKey = GetVirtualKeyFromChar(_configManager.Config.ChatKey.ToLower()[0]);
                    if (virtualKey != VirtualKeyCode.NONAME) _inputSimulator.Keyboard.KeyPress(virtualKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator失敗: {ex.Message}, 嘗試SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                OpenChatBox();
            }
        }
        else if (_useSendInput)
        {
            if (_configManager.Config.ChatKey == "enter") SendKeySendInput(0x0D);
            else SendKeySendInput((byte)VkKeyScan(_configManager.Config.ChatKey.ToLower()[0]));
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait(_configManager.Config.ChatKey == "enter" ? "{ENTER}" : _configManager.Config.ChatKey);
        }
        else
        {
            if (_configManager.Config.ChatKey == "enter") SendKeyNative(0x0D);
            else SendKeyNative((byte)VkKeyScan(_configManager.Config.ChatKey.ToLower()[0]));
        }
    }

    private static void ClearChatInput()
    {
        if (_useInputSimulator)
        {
            try
            {
                _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
                _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
                Thread.Sleep(_configManager.Config.KeyDelay / 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator清除失敗: {ex.Message}, 嘗試SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                ClearChatInput();
            }
        }
        else if (_useSendInput)
        {
            SelectAllTextSendInput();
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeySendInput(0x2E);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("^a");
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeys.SendWait("{DEL}");
        }
        else
        {
            SelectAllTextNative();
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeyNative(0x2E);
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
        }
    }

    private static void PasteFromClipboard()
    {
        if (_useInputSimulator) _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
        else if (_useSendInput) PasteFromClipboardSendInput();
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("^v");
        }
        else PasteFromClipboardNative();
    }

    private static void SendEnterKey()
    {
        if (_useInputSimulator)
        {
            try
            {
                _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator回車失敗: {ex.Message}, 嘗試SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                SendKeySendInput(0x0D);
            }
        }
        else if (_useSendInput)
        {
            SendKeySendInput(0x0D);
        }
        else if (_useWinFormSendKeys)
        {
            SendKeys.SendWait("{ENTER}");
        }
        else
        {
            SendKeyNative(0x0D);
        }
    }

    private static void ReleaseAllKeys()
    {
        if (_useWinFormSendKeys) return;
        if (_useInputSimulator)
        {
            try
            {
                VirtualKeyCode[] keysToRelease =
                [
                    VirtualKeyCode.VK_W, VirtualKeyCode.VK_A, VirtualKeyCode.VK_S, VirtualKeyCode.VK_D,
                    VirtualKeyCode.SPACE, VirtualKeyCode.LSHIFT, VirtualKeyCode.RSHIFT,
                    VirtualKeyCode.LCONTROL, VirtualKeyCode.RCONTROL, VirtualKeyCode.LMENU, VirtualKeyCode.RMENU,
                    VirtualKeyCode.LBUTTON, VirtualKeyCode.RBUTTON
                ];
                foreach (var key in keysToRelease) _inputSimulator.Keyboard.KeyUp(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GSI] InputSimulator釋放失敗: {ex.Message}, 嘗試SendInput...");
                _useInputSimulator = false;
                _useSendInput = true;
                ReleaseAllKeysSendInput();
            }
        }
        else if (_useSendInput)
        {
            ReleaseAllKeysSendInput();
        }
        else
        {
            ReleaseAllKeysNative();
        }

        Thread.Sleep(_configManager.Config.KeyDelay);
    }

    private static void ReleaseAllKeysNative()
    {
        byte[] keysToRelease = [0x57, 0x41, 0x53, 0x44, 0x20, 0x10, 0x11, 0x12, 0x01, 0x02];
        foreach (var key in keysToRelease) keybd_event(key, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void SelectAllTextNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, 0, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, KeyeventfKeyup, UIntPtr.Zero);
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void PasteFromClipboardNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(0x56, 0, 0, UIntPtr.Zero);
        Thread.Sleep(20);
        keybd_event(0x56, 0, KeyeventfKeyup, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void SendKeyNative(byte keyCode)
    {
        keybd_event(keyCode, 0, 0, UIntPtr.Zero);
        Thread.Sleep(20);
        keybd_event(keyCode, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private static void SendKeySendInput(byte keyCode)
    {
        INPUT[] inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (result != inputs.Length)
        {
            throw new Exception($"SendInput failed with error code: {Marshal.GetLastWin32Error()}");
        }
        Thread.Sleep(20);
    }

    private static void ReleaseAllKeysSendInput()
    {
        byte[] keysToRelease = [0x57, 0x41, 0x53, 0x44, 0x20, 0x10, 0x11, 0x12, 0x01, 0x02];
        foreach (var key in keysToRelease)
        {
            INPUT[] inputs = new INPUT[1]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    private static void SelectAllTextSendInput()
    {
        INPUT[] inputs = new INPUT[4];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x41,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x41,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
    }

    private static void PasteFromClipboardSendInput()
    {
        INPUT[] inputs = new INPUT[4];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x56,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x56,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(50);
    }

    private static VirtualKeyCode GetVirtualKeyFromChar(char character)
    {
        return character switch
        {
            'a' => VirtualKeyCode.VK_A, 'b' => VirtualKeyCode.VK_B, 'c' => VirtualKeyCode.VK_C,
            'd' => VirtualKeyCode.VK_D, 'e' => VirtualKeyCode.VK_E, 'f' => VirtualKeyCode.VK_F,
            'g' => VirtualKeyCode.VK_G, 'h' => VirtualKeyCode.VK_H, 'i' => VirtualKeyCode.VK_I,
            'j' => VirtualKeyCode.VK_J, 'k' => VirtualKeyCode.VK_K, 'l' => VirtualKeyCode.VK_L,
            'm' => VirtualKeyCode.VK_M, 'n' => VirtualKeyCode.VK_N, 'o' => VirtualKeyCode.VK_O,
            'p' => VirtualKeyCode.VK_P, 'q' => VirtualKeyCode.VK_Q, 'r' => VirtualKeyCode.VK_R,
            's' => VirtualKeyCode.VK_S, 't' => VirtualKeyCode.VK_T, 'u' => VirtualKeyCode.VK_U,
            'v' => VirtualKeyCode.VK_V, 'w' => VirtualKeyCode.VK_W, 'x' => VirtualKeyCode.VK_X,
            'y' => VirtualKeyCode.VK_Y, 'z' => VirtualKeyCode.VK_Z,
            '0' => VirtualKeyCode.VK_0, '1' => VirtualKeyCode.VK_1, '2' => VirtualKeyCode.VK_2,
            '3' => VirtualKeyCode.VK_3, '4' => VirtualKeyCode.VK_4, '5' => VirtualKeyCode.VK_5,
            '6' => VirtualKeyCode.VK_6, '7' => VirtualKeyCode.VK_7, '8' => VirtualKeyCode.VK_8,
            '9' => VirtualKeyCode.VK_9,
            _ => VirtualKeyCode.NONAME
        };
    }

    private static void AddNewMessage()
    {
        Console.Write("\n輸入新死亡訊息 (Enter確認): ");
        var newMessage = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(newMessage))
        {
            _chatManager.AddMessage(newMessage);
            _chatManager.SaveMessages();
            Console.WriteLine($"已新增: {newMessage}");
        }
        else
        {
            Console.WriteLine("訊息不能為空。");
        }

        UpdateCfgCount();
    }

    private static void DeleteMessage()
    {
        Console.WriteLine("\n訊息清單：");
        _chatManager.DisplayMessages();
        if (_chatManager.GetAllMessages().Count == 0) return;

        Console.Write("輸入要刪除的訊息編號: ");
        if (int.TryParse(Console.ReadLine(), out var index))
        {
            if (_chatManager.RemoveMessage(index - 1))
            {
                _chatManager.SaveMessages();
                Console.WriteLine("訊息已刪除。");
            }
            else
            {
                Console.WriteLine("無效編號。");
            }
        }
        else
        {
            Console.WriteLine("請輸入數字。");
        }

        UpdateCfgCount();
    }

    private static void ChangeChatKey()
    {
        Console.WriteLine($"\n目前聊天鍵: {_configManager.Config.ChatKey}");
        Console.WriteLine("1. Y-全域 | 2. U-團隊 | 3. Enter | 4. 自訂");
        Console.Write("選擇 (1-4): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                _configManager.Config.ChatKey = "y";
                Console.WriteLine("設為全域聊天 (Y)");
                break;
            case "2":
                _configManager.Config.ChatKey = "u";
                Console.WriteLine("設為團隊聊天 (U)");
                break;
            case "3":
                _configManager.Config.ChatKey = "enter";
                Console.WriteLine("設為回車鍵");
                break;
            case "4":
                Console.Write("輸入自訂鍵 (單字元): ");
                var customKey = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(customKey) && customKey.Length == 1 &&
                    char.IsLetterOrDigit(customKey[0]))
                {
                    _configManager.Config.ChatKey = customKey.ToLower();
                    Console.WriteLine($"設為: {customKey.ToUpper()}");
                }
                else
                {
                    Console.WriteLine("無效輸入，需單字母/數字。");
                    return;
                }

                break;
            default:
                Console.WriteLine("無效選擇。");
                return;
        }

        _configManager.SaveConfig();
        Console.WriteLine("聊天鍵已儲存。");
    }

    [LibraryImport("user32.dll")]
    private static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern short VkKeyScan(char ch);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    private const uint KeyeventfKeyup = 0x0002;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId);

    [LibraryImport("psapi.dll", SetLastError = true)]
    private static unsafe partial uint GetModuleFileNameExW(nint hProcess, nint hModule, char* lpFilename, uint nSize);

    [LibraryImport("kernel32.dll")]
    private static partial uint CloseHandle(nint hObject);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;

    private static unsafe bool IsCS2Active()
    {
        if (GetWindowThreadProcessId(GetForegroundWindow(), out var pid) == 0)
        {
            return false;
        }

        var hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
        if (hProc == 0)
        {
            return false;
        }

        var sProcPath = stackalloc char[32767];
        if (GetModuleFileNameExW(hProc, nint.Zero, sProcPath, 32767) == 0)
        {
            return false;
        }

        _ = CloseHandle(hProc);
        return Path.GetFileName(new string(sProcPath)).Equals("cs2.exe", StringComparison.InvariantCultureIgnoreCase);
    }
}