# Logger - 日志记录系统

一个为 Godot C# 项目设计的完整日志记录系统，支持多级别日志、文件输出、自动压缩和线程安全操作。

## ✨ 特性

- 📊 **多级别日志** - Debug、Info、Warn、Error、Fatal 五种日志级别
- 💾 **文件输出** - 同时输出到控制台和本地日志文件
- 🗜️ **自动压缩** - 自动压缩历史日志文件，节省存储空间
- 🏷️ **上下文标记** - 为不同模块提供独立的上下文标识
- 🧵 **线程安全** - 完全的线程安全设计，支持并发日志记录
- ⚡ **自动刷新** - 实时写入文件，确保日志不丢失
- 🔧 **零配置** - 开箱即用，无需额外配置

## 🚀 快速开始

### 1. 基本使用

```csharp
// 创建日志记录器
var logger = new LogHelper("GameManager");

// 记录不同级别的日志
logger.LogInfo("游戏开始初始化");
logger.LogWarn("配置文件不存在，使用默认配置");
logger.LogError("加载资源失败");
logger.LogFatal("致命错误，游戏即将退出");

// 使用通用日志方法
logger.Log(LogType.Debug, "调试信息");
```

### 2. 在游戏系统中使用

```csharp
public partial class Player : CharacterBody2D
{
    private readonly LogHelper _logger = new("Player");
    
    public override void _Ready()
    {
        _logger.LogInfo("玩家节点初始化完成");
    }
    
    public void TakeDamage(int damage)
    {
        _logger.LogInfo($"玩家受到 {damage} 点伤害");
        
        if (Health <= 0)
        {
            _logger.LogWarn("玩家生命值归零");
            Die();
        }
    }
    
    private void Die()
    {
        _logger.LogError("玩家死亡");
        // 处理死亡逻辑
    }
}
```

## 📖 详细功能指南

### 日志级别说明

| 级别 | 用途 | 示例场景 |
|------|------|----------|
| **Debug** | 调试信息 | 变量状态、函数调用跟踪 |
| **Info** | 一般信息 | 系统启动、配置加载、正常操作 |
| **Warn** | 警告信息 | 配置缺失、性能问题、可恢复错误 |
| **Error** | 错误信息 | 操作失败、异常情况、功能故障 |
| **Fatal** | 致命错误 | 系统崩溃、无法恢复的严重错误 |

### 上下文管理

为不同的游戏模块创建独立的日志上下文：

```csharp
public partial class GameManager : Node
{
    private readonly LogHelper _logger = new("GameManager");
    
    public override void _Ready()
    {
        _logger.LogInfo("游戏管理器初始化");
    }
}

public partial class AudioManager : Node
{
    private readonly LogHelper _logger = new("AudioManager");
    
    public void PlaySound(string soundName)
    {
        _logger.LogInfo($"播放音效: {soundName}");
    }
}

public partial class NetworkManager : Node
{
    private readonly LogHelper _logger = new("NetworkManager");
    
    public void ConnectToServer()
    {
        _logger.LogInfo("正在连接服务器...");
    }
}
```

## 📋 完整示例 - 游戏日志系统

```csharp
// 游戏主控制器
public partial class GameController : Node
{
    private readonly LogHelper _logger = new("GameController");
    private PlayerManager _playerManager;
    private LevelManager _levelManager;
    
    public override void _Ready()
    {
        _logger.LogInfo("=== 游戏启动 ===");
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        try
        {
            _logger.LogInfo("开始初始化游戏系统");
            
            // 初始化各个管理器
            InitializeManagers();
            LoadGameConfiguration();
            SetupEventHandlers();
            
            _logger.LogInfo("游戏初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogFatal($"游戏初始化失败: {ex.Message}");
            GetTree().Quit();
        }
    }
    
    private void InitializeManagers()
    {
        _logger.LogInfo("初始化游戏管理器");
        
        _playerManager = GetNode<PlayerManager>("PlayerManager");
        _levelManager = GetNode<LevelManager>("LevelManager");
        
        _logger.LogInfo("管理器初始化完成");
    }
    
    private void LoadGameConfiguration()
    {
        _logger.LogInfo("加载游戏配置");
        
        try
        {
            // 模拟配置加载
            var configPath = "user://config.json";
            if (!FileAccess.FileExists(configPath))
            {
                _logger.LogWarn("配置文件不存在，创建默认配置");
                CreateDefaultConfig();
            }
            else
            {
                _logger.LogInfo("配置文件加载成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"配置加载失败: {ex.Message}");
        }
    }
    
    private void CreateDefaultConfig()
    {
        // 创建默认配置逻辑
        _logger.LogInfo("默认配置创建完成");
    }
    
    private void SetupEventHandlers()
    {
        _logger.LogInfo("设置事件处理器");
        
        GetTree().AutoAcceptQuit = false;
        GetTree().QuitOnGoBack = false;
        
        _logger.LogInfo("事件处理器设置完成");
    }
    
    public override void _Notification(int what)
    {
        switch (what)
        {
            case NotificationWMCloseRequest:
                _logger.LogInfo("收到窗口关闭请求");
                HandleGameExit();
                break;
                
            case NotificationWMGoBackRequest:
                _logger.LogInfo("收到返回请求");
                break;
        }
    }
    
    private void HandleGameExit()
    {
        _logger.LogInfo("开始游戏退出流程");
        
        try
        {
            // 保存游戏状态
            SaveGameState();
            _logger.LogInfo("游戏状态保存完成");
            
            // 清理资源
            CleanupResources();
            _logger.LogInfo("资源清理完成");
            
            _logger.LogInfo("=== 游戏正常退出 ===");
        }
        catch (Exception ex)
        {
            _logger.LogError($"退出过程中发生错误: {ex.Message}");
        }
        finally
        {
            GetTree().Quit();
        }
    }
    
    private void SaveGameState()
    {
        _logger.LogInfo("保存游戏状态");
        // 保存逻辑
    }
    
    private void CleanupResources()
    {
        _logger.LogInfo("清理游戏资源");
        // 清理逻辑
    }
}

// 玩家管理器
public partial class PlayerManager : Node
{
    private readonly LogHelper _logger = new("PlayerManager");
    private Player _player;
    
    public override void _Ready()
    {
        _logger.LogInfo("玩家管理器初始化");
        CreatePlayer();
    }
    
    private void CreatePlayer()
    {
        try
        {
            _logger.LogInfo("创建玩家角色");
            
            var playerScene = GD.Load<PackedScene>("res://Player.tscn");
            _player = playerScene.Instantiate<Player>();
            AddChild(_player);
            
            _logger.LogInfo($"玩家角色创建成功，位置: {_player.Position}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"创建玩家失败: {ex.Message}");
        }
    }
    
    public void HandlePlayerDeath()
    {
        _logger.LogError("玩家死亡事件");
        
        // 记录死亡统计
        var deathCount = GetDeathCount() + 1;
        SetDeathCount(deathCount);
        _logger.LogInfo($"玩家死亡次数: {deathCount}");
        
        // 重生逻辑
        RespawnPlayer();
    }
    
    private void RespawnPlayer()
    {
        _logger.LogInfo("玩家重生");
        // 重生逻辑
    }
    
    private int GetDeathCount() => 0; // 示例方法
    private void SetDeathCount(int count) { } // 示例方法
}

// 关卡管理器
public partial class LevelManager : Node
{
    private readonly LogHelper _logger = new("LevelManager");
    private int _currentLevel = 1;
    
    public override void _Ready()
    {
        _logger.LogInfo("关卡管理器初始化");
        LoadLevel(_currentLevel);
    }
    
    public void LoadLevel(int levelId)
    {
        _logger.LogInfo($"开始加载关卡: Level_{levelId}");
        
        try
        {
            // 清理当前关卡
            CleanupCurrentLevel();
            
            // 加载新关卡
            var levelPath = $"res://Levels/Level_{levelId}.tscn";
            if (!FileAccess.FileExists(levelPath))
            {
                _logger.LogError($"关卡文件不存在: {levelPath}");
                return;
            }
            
            var levelScene = GD.Load<PackedScene>(levelPath);
            var level = levelScene.Instantiate();
            AddChild(level);
            
            _currentLevel = levelId;
            _logger.LogInfo($"关卡 Level_{levelId} 加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError($"关卡加载失败: {ex.Message}");
        }
    }
    
    private void CleanupCurrentLevel()
    {
        _logger.LogInfo("清理当前关卡");
        
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }
    }
    
    public void OnLevelComplete()
    {
        _logger.LogInfo($"关卡 Level_{_currentLevel} 完成");
        
        // 记录完成时间
        var completionTime = Time.GetUnixTimeFromSystem();
        _logger.LogInfo($"完成时间: {DateTime.FromBinary((long)completionTime)}");
        
        // 加载下一关
        LoadLevel(_currentLevel + 1);
    }
}

// 错误处理示例
public partial class ErrorHandler : Node
{
    private readonly LogHelper _logger = new("ErrorHandler");
    
    public override void _Ready()
    {
        // 捕获未处理的异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger.LogFatal($"未处理的异常: {exception?.Message}");
        _logger.LogFatal($"堆栈跟踪: {exception?.StackTrace}");
        
        // 保存错误报告
        SaveErrorReport(exception);
    }
    
    private void SaveErrorReport(Exception exception)
    {
        try
        {
            var errorReport = $"错误时间: {DateTime.Now}\n" +
                             $"错误信息: {exception?.Message}\n" +
                             $"堆栈跟踪: {exception?.StackTrace}";
            
            var errorPath = $"user://error_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            using var file = FileAccess.Open(errorPath, FileAccess.ModeFlags.Write);
            file?.StoreString(errorReport);
            
            _logger.LogInfo($"错误报告已保存: {errorPath}");
        }
        catch
        {
            _logger.LogError("保存错误报告失败");
        }
    }
}
```

## 🔧 高级功能

### 自定义日志实现

```csharp
// 实现自定义日志记录器
public class CustomLogger : ILogger
{
    private readonly string _context;
    
    public CustomLogger(string context)
    {
        _context = context;
    }
    
    public void Log(LogType type, string msg)
    {
        // 自定义日志逻辑
        var color = GetLogColor(type);
        var formattedMsg = $"[{_context}] {msg}";
        
        // 输出到不同目标
        ConsoleOutput(type, formattedMsg);
        FileOutput(type, formattedMsg);
        NetworkOutput(type, formattedMsg); // 发送到日志服务器
    }
    
    private Color GetLogColor(LogType type)
    {
        return type switch
        {
            LogType.Error => Colors.Red,
            LogType.Warn => Colors.Yellow,
            LogType.Info => Colors.White,
            LogType.Debug => Colors.Gray,
            LogType.Fatal => Colors.Purple,
            _ => Colors.White
        };
    }
    
    public void LogInfo(string msg) => Log(LogType.Info, msg);
    public void LogWarn(string msg) => Log(LogType.Warn, msg);
    public void LogError(string msg) => Log(LogType.Error, msg);
    public void LogFatal(string msg) => Log(LogType.Fatal, msg);
    
    private void ConsoleOutput(LogType type, string msg) { /* 控制台输出 */ }
    private void FileOutput(LogType type, string msg) { /* 文件输出 */ }
    private void NetworkOutput(LogType type, string msg) { /* 网络输出 */ }
}
```

### 条件日志记录

```csharp
public class ConditionalLogger : LogHelper
{
    private readonly LogType _minLevel;
    
    public ConditionalLogger(string context, LogType minLevel = LogType.Info) 
        : base(context)
    {
        _minLevel = minLevel;
    }
    
    public override void Log(LogType type, string msg)
    {
        // 只记录达到最小级别的日志
        if (type >= _minLevel)
        {
            base.Log(type, msg);
        }
    }
}

// 使用示例
var debugLogger = new ConditionalLogger("Debug", LogType.Debug);  // 记录所有级别
var releaseLogger = new ConditionalLogger("Release", LogType.Warn); // 只记录警告及以上
```

## 🎯 最佳实践

### 1. 合理的日志级别使用

```csharp
public partial class GameSystem : Node
{
    private readonly LogHelper _logger = new("GameSystem");
    
    public void ProcessGameLogic()
    {
        // ✅ 正确使用
        _logger.LogInfo("开始处理游戏逻辑");        // 重要流程
        _logger.LogWarn("内存使用率过高");           // 潜在问题
        _logger.LogError("保存游戏失败");           // 明确错误
        
        // ❌ 避免过度记录
        // _logger.LogInfo("变量i的值为10");         // 过于详细
        // _logger.LogError("用户点击了按钮");       // 错误的级别
    }
}
```

### 2. 结构化日志信息

```csharp
public void HandlePlayerAction(string action, int playerId)
{
    // ✅ 结构化信息
    _logger.LogInfo($"玩家动作 - ID:{playerId}, 动作:{action}, 时间:{DateTime.Now:HH:mm:ss}");
    
    // ❌ 模糊信息
    // _logger.LogInfo("玩家做了什么");
}
```

### 3. 异常日志记录

```csharp
public void LoadResource(string path)
{
    try
    {
        // 加载资源逻辑
    }
    catch (FileNotFoundException ex)
    {
        _logger.LogError($"资源文件不存在: {path}");
        _logger.LogError($"详细错误: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogFatal($"加载资源时发生未知错误: {ex.Message}");
        _logger.LogFatal($"堆栈跟踪: {ex.StackTrace}");
        throw; // 重新抛出致命错误
    }
}
```

## 🔧 配置和自定义

### 日志文件管理

日志系统会自动：
- 在 `用户数据目录/logs/` 下创建日志文件
- 按日期时间命名日志文件 (`log_yyyyMMdd_HHmmss.txt`)
- 压缩历史日志文件为 `.zip` 格式
- 程序退出时自动压缩当前日志文件

### 文件位置

```csharp
// Windows: %APPDATA%/Godot/app_userdata/[项目名]/logs/
// macOS: ~/Library/Application Support/Godot/app_userdata/[项目名]/logs/  
// Linux: ~/.local/share/godot/app_userdata/[项目名]/logs/
```

---

**Logger** - 让你的 Godot C# 项目拥有专业的日志记录能力！