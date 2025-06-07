using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using TextCopy;
using WindowsInput;
using WindowsInput.Native;

// ReSharper disable InconsistentNaming

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace ImLag;

[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
internal static partial class Program
{
    public const string Version = "2.0.1";
    private const string Author = "Eicy";
    private const string Name = "ImLag";
    private const string UpdateLog = "优化CFG模式，自动创建/更新autoexec.cfg，并提供执行说明。";

    private static GameStateListener? _gsl;
    private static ChatMessageManager _chatManager;
    private static ConfigManager _configManager;
    private static CfgManager _cfgManager;
    private static readonly InputSimulator _inputSimulator = new();
    private static bool _useInputSimulator;

    private static void Main()
    {
        Console.Title = $"{Name} v{Version} by {Author}";

        if (!IsRunningAsAdministrator())
        {
            Console.WriteLine("警告：程序不是以管理员身份运行。");
            Console.WriteLine("如果遇到按键发送或CFG文件写入问题，请尝试以管理员身份重新运行程序。");
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();
            Console.Clear();
        }

        _configManager = new ConfigManager();
        _configManager.LoadConfig();

        _chatManager = new ChatMessageManager();
        _chatManager.LoadMessages();

        _cfgManager = new CfgManager(_chatManager, _configManager);

        if (string.IsNullOrWhiteSpace(_configManager.Config.UserPlayerName))
        {
            SetupPlayerName();
        }

        _gsl = new GameStateListener(4000);
        if (!_gsl.GenerateGSIConfigFile("ImLag"))
        {
            Console.WriteLine("无法生成GSI配置文件。");
        }

        _gsl.PlayerDied += OnPlayerDied;

        Console.WriteLine(!_gsl.Start() ? "GameStateListener启动失败。请尝试以管理员身份运行程序。" : "正在监听CS2游戏事件 (GSI)...");

        Console.WriteLine($"=== {Name} v{Version} by {Author} ===");
        if (_configManager.Config.UseCfgMode)
        {
            Console.WriteLine("当前运行模式: CFG 模式 (通过游戏内按键发送消息)");
            Console.WriteLine("在此模式下，程序本身不直接发送消息，而是配置游戏内的CFG文件。");
            Console.WriteLine($"请确保已按 'G'键生成CFG文件并将绑定键 '{_cfgManager.BindKeys}' 用于在游戏中发送消息。");
        }
        else
        {
            Console.WriteLine("当前运行模式: 聊天模式 (通过GSI检测死亡并模拟按键发送消息)");
            Console.WriteLine($"当前用户玩家名: {_configManager.Config.UserPlayerName}");
            Console.WriteLine($"当前监听模式: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")}");
            Console.WriteLine($"窗口检测: {(_configManager.Config.SkipWindowCheck ? "已禁用" : "已启用")}");
            Console.WriteLine($"强制发送模式: {(_configManager.Config.ForceMode ? "已启用" : "已禁用")}");
            Console.WriteLine($"按键延迟: {_configManager.Config.KeyDelay}毫秒");
            Console.WriteLine($"按键模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
        }

        Console.WriteLine();
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
                    Console.WriteLine("\n当前消息列表：");
                    _chatManager.DisplayMessages();
                    break;
                case ConsoleKey.C:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeChatKey();
                    else
                        Console.WriteLine("\nCFG模式下无需配置聊天按键，请使用绑定键。");
                    break;
                case ConsoleKey.P:
                    ChangePlayerName();
                    break;
                case ConsoleKey.M:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleMonitorMode();
                    else
                        Console.WriteLine("\nCFG模式下此设置无效，消息发送由游戏内按键触发。");
                    break;
                case ConsoleKey.W:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleWindowCheck();
                    else
                        Console.WriteLine("\nCFG模式下此设置无效。");
                    break;
                case ConsoleKey.F:
                    if (!_configManager.Config.UseCfgMode)
                        ToggleForceMode();
                    else
                        Console.WriteLine("\nCFG模式下无需强制发送模式。");
                    break;
                case ConsoleKey.K:
                    if (!_configManager.Config.UseCfgMode)
                        ChangeKeyDelay();
                    else
                        Console.WriteLine("\nCFG模式下按键延迟无效。");
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
                        Console.WriteLine("\n请先切换到CFG模式 (按T键)。");
                    break;
                case ConsoleKey.S:
                    if (_configManager.Config.UseCfgMode)
                        SetCS2Path();
                    else
                        Console.WriteLine("\n请先切换到CFG模式 (按T键)。");
                    break;
                case ConsoleKey.G:
                    if (_configManager.Config.UseCfgMode)
                        GenerateCfgFiles();
                    else
                        Console.WriteLine("\n请先切换到CFG模式 (按T键)。");
                    break;
                default:
                    Console.WriteLine("\n无效按键，请参照下方说明操作。");
                    Init();
                    break;
            }
        } while (keyInfo.Key != ConsoleKey.Escape);

        _configManager.SaveConfig();
        _chatManager.SaveMessages();
        Console.WriteLine("程序已退出。");
    }

    private static void ToggleOperationalMode()
    {
        _configManager.Config.UseCfgMode = !_configManager.Config.UseCfgMode;
        _configManager.SaveConfig();

        Console.WriteLine($"\n已切换到 {(_configManager.Config.UseCfgMode ? "CFG模式" : "聊天模式 (GSI)")}");

        if (_configManager.Config.UseCfgMode)
        {
            if (_gsl is { Running: true })
            {
                _gsl.Stop();
                Console.WriteLine("GSI监听已停止。");
            }

            Console.WriteLine("CFG模式特点：");
            Console.WriteLine("1. 在游戏内通过按键发送预设的随机消息。");
            Console.WriteLine("2. 需要先生成CFG文件 (按G键)。");
            Console.WriteLine("3. 消息发送更可靠，无惧输入法或窗口焦点问题。");
            if (string.IsNullOrEmpty(_configManager.Config.CS2Path))
            {
                Console.WriteLine("\n检测到CS2路径未设置，正在尝试自动查找...");
                _cfgManager.FindCS2Path();
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
                    Console.WriteLine("无法生成GSI配置文件。");
                }

                _gsl.PlayerDied += OnPlayerDied;
            }

            if (!_gsl.Running)
            {
                Console.WriteLine(_gsl.Start() ? "GSI监听已启动。" : "GameStateListener启动失败。请尝试以管理员身份运行程序。");
            }

            Console.WriteLine("聊天模式 (GSI)特点：");
            Console.WriteLine("1. 通过CS2 GSI自动检测玩家死亡事件。");
            Console.WriteLine("2. 程序自动模拟按键发送聊天消息。");
        }

        Init();
    }


    private static void ChangeBindKey()
    {
        while (true)
        {
            Console.WriteLine($"\n当前绑定键: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine("1. 添加绑定键");
            Console.WriteLine("2. 删除绑定键");
            Console.WriteLine("3. 返回");
            Console.Write("请选择操作 (1-3): ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    Console.Write("请输入要添加的绑定键 (单个字母或数字): ");
                    var input = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.AddBindKey(input);
                    break;
                case "2":
                    if (_cfgManager.BindKeys.Count <= 1)
                    {
                        Console.WriteLine("至少需要保留一个绑定键。");
                        break;
                    }

                    Console.Write("请输入要删除的绑定键: ");
                    var keyToRemove = Console.ReadLine()?.Trim().ToLower() ?? "";
                    _cfgManager.RemoveBindKey(keyToRemove);
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("无效选择。");
                    break;
            }
        }
    }

    private static void SetCS2Path()
    {
        Console.WriteLine($"\n当前CS2路径: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未设置")}");
        Console.WriteLine(
            @"请输入CS2游戏根目录路径 (例如: C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive)");
        Console.Write("新路径: ");
        var path = Console.ReadLine()?.Trim() ?? "";
        _cfgManager.SetCS2Path(path);
    }

    private static void UpdateCfgCount()
    {
        _cfgManager.SetTotalCfgFiles(_chatManager.Messages.Count);
    }

    private static void GenerateCfgFiles()
    {
        Console.WriteLine("\n=== 生成CFG文件 ===");
        if (!_cfgManager.GenerateConfigFiles()) return;
        Console.WriteLine("\nCFG文件已生成。");
        Console.WriteLine("是否要自动更新/创建 autoexec.cfg 文件以应用绑定? (y/n)");
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
            Console.WriteLine("\n已跳过更新 autoexec.cfg 文件。");
            Console.WriteLine("你需要手动将以下内容添加到你的 autoexec.cfg 文件中:");
            Console.WriteLine($"bind \"{string.Join(", ", _cfgManager.BindKeys)}\" \"exec random_say_selector\"");
            Console.WriteLine($"路径: {_cfgManager.CfgPath}\\autoexec.cfg");
            Console.WriteLine("\n同时，确保autoexec.cfg会被游戏执行 (例如，通过启动项 +exec autoexec)。");
        }
    }

    private static void ToggleInputMethod()
    {
        _useInputSimulator = !_useInputSimulator;
        Console.WriteLine($"\n按键模拟方式已切换为: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
        _configManager.SaveConfig();

        if (!_useInputSimulator)
        {
            Console.WriteLine("已切换到原生 Win32 API 模式，这可能更兼容某些系统配置。");
        }
        else
        {
            Console.WriteLine("已切换到 InputSimulator 模式，这通常更稳定(存疑)。");
            Console.WriteLine("如果遇到权限错误，请以管理员身份运行程序。");
        }
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

        Console.WriteLine($"\n强制发送模式已{(_configManager.Config.ForceMode ? "启用" : "禁用")}");
        if (_configManager.Config.ForceMode)
        {
            Console.WriteLine("警告：强制模式会多次尝试打开聊天框，可能导致重复发送！");
            Console.WriteLine("建议：仅在消息经常发送失败时启用此选项。");
        }
        else
        {
            Console.WriteLine("已恢复正常发送模式。");
        }
    }

    private static void ChangeKeyDelay()
    {
        Console.WriteLine($"\n当前按键延迟: {_configManager.Config.KeyDelay}毫秒");
        Console.Write("请输入新的按键延迟时间 (30-500毫秒): ");

        if (int.TryParse(Console.ReadLine(), out var delay) && delay is >= 30 and <= 500)
        {
            _configManager.Config.KeyDelay = delay;
            _configManager.SaveConfig();
            Console.WriteLine($"按键延迟已更新为: {delay}毫秒");
        }
        else
        {
            Console.WriteLine("无效输入，延迟时间应该在30-500毫秒之间。");
        }
    }

    private static void ToggleMonitorMode()
    {
        _configManager.Config.OnlySelfDeath = !_configManager.Config.OnlySelfDeath;
        _configManager.SaveConfig();

        Console.WriteLine($"\n监听模式已切换为: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")}");
        Console.WriteLine($"现在{(_configManager.Config.OnlySelfDeath ? "只有你死亡时" : "任何玩家死亡时")}都会在聊天模式下发送消息。");
    }

    private static void ToggleWindowCheck()
    {
        _configManager.Config.SkipWindowCheck = !_configManager.Config.SkipWindowCheck;
        _configManager.SaveConfig();

        Console.WriteLine($"\n窗口检测已{(_configManager.Config.SkipWindowCheck ? "禁用" : "启用")}");
        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("警告：禁用窗口检测后，即使CS2不是活动窗口也会尝试发送消息！");
            Console.WriteLine("建议：仅在聊天模式下，使用5E对战平台且窗口标题检测失败时启用此选项。");
        }
        else
        {
            Console.WriteLine("已恢复窗口检测，聊天模式下只有CS2窗口活动时才会发送消息。");
        }
    }

    private static void ShowVersionInfo()
    {
        Console.WriteLine($"\n=== {Name} v{Version} ===");
        Console.WriteLine($"作者: {Author}");
        Console.WriteLine("GitHub: https://github.com/cneicy/ImLag");
        Console.WriteLine($"更新日志: {UpdateLog}");
    }

    private static void SetupPlayerName()
    {
        Console.WriteLine("\n=== 首次启动设置 (聊天模式GSI需要) ===");
        Console.WriteLine("请输入你的CS2游戏内玩家名:");
        Console.WriteLine("注意：必须与游戏内显示的玩家名完全一致（包括大小写）");
        Console.WriteLine("如果你主要使用CFG模式，此项可以留空。");
        Console.WriteLine();

        while (true)
        {
            Console.Write("请输入你的玩家名 (直接按Enter跳过): ");
            var playerName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                _configManager.Config.UserPlayerName = string.Empty;
                _configManager.SaveConfig();
                Console.WriteLine("玩家名未设置。");
                break;
            }

            _configManager.Config.UserPlayerName = playerName.Trim();
            _configManager.SaveConfig();
            Console.WriteLine($"玩家名设置成功: {playerName}");
            Console.WriteLine();
            break;
        }
    }

    private static void ChangePlayerName()
    {
        Console.WriteLine(
            $"\n当前玩家名 (聊天模式GSI需要): {(_configManager.Config.UserPlayerName == "" ? "未设置" : _configManager.Config.UserPlayerName)}");
        Console.Write("请输入新的玩家名 (直接按Enter取消, 输入 '-' 清空): ");
        var playerName = Console.ReadLine();

        if (string.IsNullOrEmpty(playerName))
        {
            Console.WriteLine("已取消修改。");
            return;
        }

        if (playerName == "-")
        {
            _configManager.Config.UserPlayerName = string.Empty;
            Console.WriteLine("玩家名已清空。");
        }
        else
        {
            _configManager.Config.UserPlayerName = playerName.Trim();
            Console.WriteLine($"玩家名已更新: {playerName}");
        }

        _configManager.SaveConfig();
    }

    private static void Init()
    {
        Console.Clear(); // 清除控制台，确保显示最新信息

        Console.WriteLine("=== ImLag - 当前配置 & 可用操作 ===");
        Console.WriteLine();

        if (_configManager.Config.UseCfgMode)
        {
            // CFG 模式部分
            Console.WriteLine("**当前模式: CFG 模式**");
            Console.WriteLine("  描述: 通过游戏内按键发送消息。");
            Console.WriteLine();
            Console.WriteLine("配置信息:");
            Console.WriteLine($"  绑定键: {string.Join(", ", _cfgManager.BindKeys)}");
            Console.WriteLine($"  CFG 文件数量: {_cfgManager.TotalCfgFiles}");
            Console.WriteLine($"  CS2 路径: {(_cfgManager.CS2Path != "" ? _cfgManager.CS2Path : "未设置，请按 S 键配置")}");
            Console.WriteLine();
            Console.WriteLine($"消息数量: {_chatManager.Messages.Count}");
            UpdateCfgCount();
            Console.WriteLine();
            Console.WriteLine("CFG 模式控制按键:");
            Console.WriteLine("  T  - 切换到聊天模式 (GSI)");
            Console.WriteLine("  A  - 添加新消息 (用于 CFG 生成)");
            Console.WriteLine("  D  - 删除消息");
            Console.WriteLine("  L  - 显示当前消息列表");
            Console.WriteLine("  B  - 修改绑定键");
            Console.WriteLine("  S  - 设置 CS2 游戏路径");
            Console.WriteLine("  G  - 生成或重新生成 CFG 文件");
        }
        else
        {
            // 聊天模式部分
            var chatKeyDescription = _configManager.Config.ChatKey switch
            {
                "y" => "全局聊天",
                "u" => "队内聊天",
                "enter" => "回车键",
                _ => $"自定义 ({_configManager.Config.ChatKey})"
            };
            Console.WriteLine("**当前模式: 聊天模式 (GSI)**");
            Console.WriteLine("  描述: 通过 GSI 自动检测死亡并发送消息。");
            Console.WriteLine();
            Console.WriteLine("配置信息:");
            Console.WriteLine($"  聊天按键: {_configManager.Config.ChatKey} ({chatKeyDescription})");
            Console.WriteLine(
                $"  监听模式: {(_configManager.Config.OnlySelfDeath ? "仅监听自己死亡" : "监听所有玩家死亡")} (玩家名: {(_configManager.Config.UserPlayerName == "" ? "未设置" : _configManager.Config.UserPlayerName)})");
            Console.WriteLine($"  窗口检测: {(_configManager.Config.SkipWindowCheck ? "已禁用" : "已启用")}");
            Console.WriteLine($"  强制发送: {(_configManager.Config.ForceMode ? "已启用" : "已禁用")}");
            Console.WriteLine($"  按键延迟: {_configManager.Config.KeyDelay} 毫秒");
            Console.WriteLine($"  模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");
            Console.WriteLine();
            Console.WriteLine($"消息数量:{_chatManager.Messages.Count}");
            Console.WriteLine();
            Console.WriteLine("聊天模式控制按键:");
            Console.WriteLine("  T  - 切换到 CFG 模式");
            Console.WriteLine("  A  - 添加新消息");
            Console.WriteLine("  D  - 删除消息");
            Console.WriteLine("  L  - 显示当前消息列表");
            Console.WriteLine("  C  - 更改聊天按键设置");
            Console.WriteLine("  P  - 更新玩家名 (用于 '仅监听自己死亡' 模式)");
            Console.WriteLine("  M  - 切换监听模式");
            Console.WriteLine("  W  - 切换窗口检测");
            Console.WriteLine("  F  - 切换强制发送模式");
            Console.WriteLine("  K  - 调整按键延迟");
            Console.WriteLine("  I  - 切换按键模拟方式");
        }

        // 通用控制部分
        Console.WriteLine();
        Console.WriteLine("通用控制按键:");
        Console.WriteLine("  V  - 显示版本信息");
        Console.WriteLine("  ESC - 退出程序");
        Console.WriteLine("========================================");
        Console.Write("请按键选择操作: ");
    }

    private static void OnPlayerDied(PlayerDied gameEvent)
    {
        Console.WriteLine($"\n[GSI] 检测到玩家死亡: {gameEvent.Player.Name}");

        switch (_configManager.Config.OnlySelfDeath)
        {
            case true when gameEvent.Player.Name != _configManager.Config.UserPlayerName:
                Console.WriteLine("[GSI] 当前为仅监听自己死亡模式，跳过发送消息。");
                return;
            case true when string.IsNullOrEmpty(_configManager.Config.UserPlayerName):
                Console.WriteLine("[GSI] 当前为仅监听自己死亡模式，但未设置玩家名，将监听所有死亡事件。");
                break;
        }

        Console.WriteLine(gameEvent.Player.Name == _configManager.Config.UserPlayerName ||
                          string.IsNullOrEmpty(_configManager.Config.UserPlayerName)
            ? "[GSI] 检测到你死了！"
            : $"[GSI] 检测到队友 {gameEvent.Player.Name} 死亡！");

        if (!_configManager.Config.SkipWindowCheck && !IsCS2Active())
        {
            Console.WriteLine("[GSI] CS2不是当前活动窗口，跳过发送消息。");
            return;
        }

        if (_configManager.Config.SkipWindowCheck)
        {
            Console.WriteLine("[GSI] 已跳过窗口检测，尝试发送消息。");
        }

        if (_configManager.Config.UseCfgMode)
        {
            var randomKey = _cfgManager.GetRandomBindKey();
            SimulateBindKey(randomKey);
            Console.WriteLine($"[GSI] 已模拟按下随机绑定键: {randomKey}");
        }
        else
        {
            var randomMessage = _chatManager.GetRandomMessage();
            if (!string.IsNullOrEmpty(randomMessage))
            {
                SendChatMessage(randomMessage);
                Console.WriteLine($"[GSI] 已尝试发送聊天消息: {randomMessage}");
            }
            else
            {
                Console.WriteLine("[GSI] 消息列表为空，请添加消息。");
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
        else
        {
            SendKeyNative((byte)VkKeyScan(bindKey[0]));
        }
    }

    private static void SendChatMessage(string message)
    {
        try
        {
            Console.WriteLine($"[GSI] 准备发送消息: {message}");
            Console.WriteLine($"[GSI] 使用按键模拟方式: {(_useInputSimulator ? "InputSimulator" : "原生 Win32 API")}");

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

            Thread.Sleep(_configManager.Config.KeyDelay * 2); // 等待聊天框打开
            ClearChatInput();
            Thread.Sleep(_configManager.Config.KeyDelay);

            for (var retry = 0; retry < 3; retry++) // 粘贴重试
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
                        Console.WriteLine($"[GSI] 粘贴失败，正在重试 ({retry + 1}/3)... 错误: {ex.Message}");
                        if (_useInputSimulator && ex.Message.Contains("not sent successfully"))
                        {
                            Console.WriteLine("[GSI] InputSimulator 遇到权限问题，自动切换到原生 Win32 API...");
                            _useInputSimulator = false;
                        }

                        Thread.Sleep(_configManager.Config.KeyDelay);
                    }
                    else throw;
                }
            }

            Thread.Sleep(_configManager.Config.KeyDelay);
            SendEnterKey();
            Console.WriteLine("[GSI] 消息发送完成。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GSI] 发送聊天消息时出错: {ex.Message}");
            if (ex.Message.Contains("not sent successfully"))
            {
                Console.WriteLine("\n[GSI] 解决建议：");
                Console.WriteLine("1. 以管理员身份重新运行此程序。");
                Console.WriteLine("2. 按 I 键切换到原生 Win32 API 模式。");
                Console.WriteLine("3. 确保CS2游戏没有以更高权限运行。");
                _useInputSimulator = false;
                Console.WriteLine("[GSI] 已自动切换到原生 Win32 API 模式。");
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
                Console.WriteLine($"[GSI] InputSimulator 打开聊天框失败: {ex.Message}, 尝试原生API...");
                _useInputSimulator = false;
                OpenChatBox();
            }
        }
        else
        {
            if (_configManager.Config.ChatKey == "enter") SendKeyNative(0x0D); // Enter
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
                Console.WriteLine($"[GSI] InputSimulator 清除输入失败: {ex.Message}, 尝试原生API...");
                _useInputSimulator = false;
                ClearChatInput();
            }
        }
        else
        {
            SelectAllTextNative();
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
            SendKeyNative(0x2E); // Delete
            Thread.Sleep(_configManager.Config.KeyDelay / 2);
        }
    }

    private static void PasteFromClipboard()
    {
        if (_useInputSimulator) _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
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
                Console.WriteLine($"[GSI] InputSimulator 发送回车失败: {ex.Message}, 尝试原生API...");
                _useInputSimulator = false;
                SendKeyNative(0x0D);
            }
        }
        else SendKeyNative(0x0D); // Enter
    }

    private static void ReleaseAllKeys()
    {
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
                Console.WriteLine($"[GSI] InputSimulator 释放按键失败: {ex.Message}, 尝试原生API...");
                _useInputSimulator = false;
                ReleaseAllKeysNative();
            }
        }
        else ReleaseAllKeysNative();

        Thread.Sleep(_configManager.Config.KeyDelay);
    }

    private static void ReleaseAllKeysNative()
    {
        byte[] keysToRelease =
            [0x57, 0x41, 0x53, 0x44, 0x20, 0x10, 0x11, 0x12, 0x01, 0x02]; // W,A,S,D,Space,Shift,Ctrl,Alt,LMB,RMB
        foreach (var key in keysToRelease) keybd_event(key, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void SelectAllTextNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, 0, UIntPtr.Zero); // A down
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x41, 0, KeyeventfKeyup, UIntPtr.Zero); // A up
        Thread.Sleep(_configManager.Config.KeyDelay / 2);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero); // Ctrl up
    }

    private static void PasteFromClipboardNative()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
        Thread.Sleep(50);
        keybd_event(0x56, 0, 0, UIntPtr.Zero); // V down
        Thread.Sleep(20);
        keybd_event(0x56, 0, KeyeventfKeyup, UIntPtr.Zero); // V up
        Thread.Sleep(50);
        keybd_event(0x11, 0, KeyeventfKeyup, UIntPtr.Zero); // Ctrl up
    }

    private static void SendKeyNative(byte keyCode)
    {
        keybd_event(keyCode, 0, 0, UIntPtr.Zero); // Key down
        Thread.Sleep(20);
        keybd_event(keyCode, 0, KeyeventfKeyup, UIntPtr.Zero); // Key up
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
        Console.Write("\n请输入新的死亡消息 (按Enter确认): ");
        var newMessage = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(newMessage))
        {
            _chatManager.AddMessage(newMessage);
            _chatManager.SaveMessages();
            Console.WriteLine($"已添加消息: {newMessage}");
        }
        else
        {
            Console.WriteLine("消息不能为空。");
        }

        UpdateCfgCount();
    }

    private static void DeleteMessage()
    {
        Console.WriteLine("\n当前消息列表：");
        _chatManager.DisplayMessages();
        if (_chatManager.GetAllMessages().Count == 0) return;

        Console.Write("请输入要删除的消息编号: ");
        if (int.TryParse(Console.ReadLine(), out var index))
        {
            if (_chatManager.RemoveMessage(index - 1))
            {
                _chatManager.SaveMessages();
                Console.WriteLine("消息已删除。");
            }
            else
            {
                Console.WriteLine("无效的消息编号。");
            }
        }
        else
        {
            Console.WriteLine("请输入有效的数字。");
        }

        UpdateCfgCount();
    }

    private static void ChangeChatKey()
    {
        Console.WriteLine("\n当前聊天按键设置：");
        Console.WriteLine($"聊天按键: {_configManager.Config.ChatKey}");
        Console.WriteLine();
        Console.WriteLine("请选择聊天按键：");
        Console.WriteLine("1. Y - 全局聊天");
        Console.WriteLine("2. U - 队内聊天");
        Console.WriteLine("3. Enter - 回车键 (通常用于打开聊天框后直接发送)");
        Console.WriteLine("4. 自定义按键 (单个字符)");
        Console.Write("请输入选择 (1-4): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                _configManager.Config.ChatKey = "y";
                Console.WriteLine("已设置为全局聊天 (Y键)");
                break;
            case "2":
                _configManager.Config.ChatKey = "u";
                Console.WriteLine("已设置为队内聊天 (U键)");
                break;
            case "3":
                _configManager.Config.ChatKey = "enter";
                Console.WriteLine("已设置为回车键");
                break;
            case "4":
                Console.Write("请输入自定义按键 (单个字符): ");
                var customKey = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(customKey) && customKey.Length == 1 &&
                    char.IsLetterOrDigit(customKey[0]))
                {
                    _configManager.Config.ChatKey = customKey.ToLower();
                    Console.WriteLine($"已设置为自定义按键: {customKey.ToUpper()}");
                }
                else
                {
                    Console.WriteLine("无效输入，必须是单个字母或数字。");
                    return;
                }

                break;
            default:
                Console.WriteLine("无效选择。");
                return;
        }

        _configManager.SaveConfig();
        Console.WriteLine("聊天按键设置已保存。");
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
    private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("psapi.dll", SetLastError = true)]
    private static unsafe partial uint GetModuleFileNameExW(nint hProcess, nint hModule, char* lpFilename, uint nSize);

    [LibraryImport("kernel32.dll")]
    private static partial uint CloseHandle(nint hObject);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;

    private static unsafe bool IsCS2Active()
    {
        if (GetWindowThreadProcessId(GetForegroundWindow(), out var pid) == 0) {
            return false;
        }
        var hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
        if (hProc == 0) {
            return false;
        }
        var sProcPath = stackalloc char[32767];
        if (GetModuleFileNameExW(hProc, nint.Zero, sProcPath, 32767) == 0) {
            return false;
        }
        _ = CloseHandle(hProc);
        return Path.GetFileName(new string(sProcPath)).Equals("cs2.exe", StringComparison.InvariantCultureIgnoreCase);
    }
}
