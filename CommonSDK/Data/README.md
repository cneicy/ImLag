# DataManager - 数据管理系统

一个为 Godot C# 项目设计的完整数据管理系统，支持多存档槽位、数据加密、自动保存和事件驱动的数据管理功能。

## ✨ 特性

- 💾 **多存档槽位** - 支持创建、删除、切换多个游戏存档
- 🔐 **数据加密** - 使用AES加密保护存档数据安全
- ⚡ **自动保存** - 定时自动保存和窗口关闭时保存
- 📡 **事件驱动** - 完整的事件系统追踪数据变化
- 🛡️ **错误处理** - 完善的异常处理和恢复机制
- 🔧 **类型安全** - 强类型数据访问和转换
- 🌍 **全局管理** - 单例模式确保数据一致性

## 🚀 快速开始

### 1. 基本数据操作

```csharp
// 获取数据管理器实例
var dataManager = DataManager.Instance;

// 设置游戏数据
dataManager.SetData("playerName", "勇者小明");
dataManager.SetData("level", 25);
dataManager.SetData("gold", 1500);
dataManager.SetData("inventory", new List<string> { "剑", "盾", "药水" });

// 读取游戏数据
var playerName = dataManager.GetData<string>("playerName", "未知玩家");
var level = dataManager.GetData<int>("level", 1);
var gold = dataManager.GetData<int>("gold", 0);
var inventory = dataManager.GetData<List<string>>("inventory", new List<string>());

GD.Print($"玩家: {playerName}, 等级: {level}, 金币: {gold}");
```

### 2. 存档槽位管理

```csharp
// 创建新存档槽位
dataManager.CreateNewSlot("存档1");
dataManager.CreateNewSlot("存档2");
dataManager.CreateNewSlot("存档3");

// 获取所有存档槽位
var slots = dataManager.GetAllSlots();
foreach (var slot in slots)
{
    GD.Print($"存档槽位: {slot}");
}

// 切换到指定存档槽位
dataManager.SwitchSlot("存档2");

// 删除存档槽位
dataManager.DeleteSlot("存档3");
```

### 3. 事件监听

```csharp
[EventBusSubscriber]
public partial class GameManager : Node
{
    // 监听数据更新
    [EventSubscribe]
    public void OnPlayerDataUpdated(PlayerDataUpdatedEvent evt)
    {
        GD.Print($"数据更新: {evt.Key} = {evt.NewValue}");
        
        // 特殊处理重要数据变化
        if (evt.Key == "level")
        {
            ShowLevelUpEffect();
        }
    }
    
    // 监听存档切换
    [EventSubscribe]
    public void OnSlotChanged(PlayerSlotChangedEvent evt)
    {
        GD.Print($"存档切换: {evt.OldSlot} -> {evt.NewSlot}");
        RefreshUI();
    }
    
    // 监听数据错误
    [EventSubscribe]
    public void OnDataError(PlayerDataErrorEvent evt)
    {
        GD.PrintErr($"数据错误: {evt.ErrorMessage}");
        ShowErrorDialog(evt.ErrorMessage);
    }
}
```

## 📖 详细功能指南

### 数据类型支持

支持所有基本类型和复杂对象：

```csharp
// 基本类型
dataManager.SetData("playerLevel", 50);
dataManager.SetData("playerName", "英雄");
dataManager.SetData("gameCompleted", true);

// 复杂对象
var playerStats = new Dictionary<string, int>
{
    {"strength", 25},
    {"agility", 18},
    {"intelligence", 30}
};
dataManager.SetData("playerStats", playerStats);

// 自定义类（需要支持JSON序列化）
public class PlayerSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public int GraphicsQuality { get; set; } = 2;
    public bool FullScreen { get; set; } = false;
}

var settings = new PlayerSettings();
dataManager.SetData("gameSettings", settings);
```

### 立即保存 vs 自动保存

```csharp
// 普通保存 - 5分钟后自动保存
dataManager.SetData("score", 1000);

// 立即保存 - 重要数据立即写入磁盘
dataManager.SetData("checkpoint", "boss_defeated", true);

// 手动强制保存
dataManager.ForceSave();
```

### 加密控制

```csharp
// 启用加密（默认）
dataManager.EncryptionEnabled = true;

// 禁用加密（开发调试时）
dataManager.EncryptionEnabled = false;

// 重新保存以应用加密设置
dataManager.ForceSave();
```

## 📋 完整示例 - RPG游戏存档系统

```csharp
// 玩家数据结构
public class PlayerData
{
    public string Name { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int Mana { get; set; } = 50;
    public Vector2 Position { get; set; }
    public List<string> Inventory { get; set; } = new();
    public Dictionary<string, bool> Achievements { get; set; } = new();
}

// 游戏设置数据
public class GameSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public float SfxVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 1.0f;
    public int GraphicsQuality { get; set; } = 2;
    public bool FullScreen { get; set; } = false;
    public string Language { get; set; } = "zh-CN";
}

// 存档管理器
[EventBusSubscriber]
public partial class SaveGameManager : Node
{
    private DataManager _dataManager;
    private PlayerData _playerData;
    private GameSettings _gameSettings;
    
    public override void _Ready()
    {
        _dataManager = DataManager.Instance;
        LoadGameData();
    }
    
    // 加载游戏数据
    private void LoadGameData()
    {
        // 加载玩家数据
        _playerData = _dataManager.GetData("playerData", new PlayerData());
        
        // 加载游戏设置
        _gameSettings = _dataManager.GetData("gameSettings", new GameSettings());
        
        GD.Print($"已加载玩家数据: {_playerData.Name}, 等级 {_playerData.Level}");
    }
    
    // 保存玩家数据
    public void SavePlayerData(PlayerData playerData)
    {
        _playerData = playerData;
        _dataManager.SetData("playerData", playerData, true); // 立即保存
    }
    
    // 保存游戏设置
    public void SaveGameSettings(GameSettings settings)
    {
        _gameSettings = settings;
        _dataManager.SetData("gameSettings", settings);
    }
    
    // 创建新游戏
    public void NewGame(string playerName)
    {
        var newPlayerData = new PlayerData
        {
            Name = playerName,
            Level = 1,
            Experience = 0,
            Health = 100,
            Mana = 50,
            Position = Vector2.Zero,
            Inventory = new List<string> { "新手剑", "生命药水" }
        };
        
        SavePlayerData(newPlayerData);
        GD.Print($"新游戏开始: {playerName}");
    }
    
    // 升级玩家
    public void LevelUpPlayer()
    {
        _playerData.Level++;
        _playerData.Health += 10;
        _playerData.Mana += 5;
        
        SavePlayerData(_playerData);
        GD.Print($"玩家升级到 {_playerData.Level} 级!");
    }
    
    // 添加物品到背包
    public void AddItemToInventory(string item)
    {
        _playerData.Inventory.Add(item);
        SavePlayerData(_playerData);
    }
    
    // 解锁成就
    public void UnlockAchievement(string achievementId)
    {
        if (!_playerData.Achievements.ContainsKey(achievementId))
        {
            _playerData.Achievements[achievementId] = true;
            SavePlayerData(_playerData);
            GD.Print($"解锁成就: {achievementId}");
        }
    }
    
    // 事件处理
    [EventSubscribe]
    public void OnDataLoaded(PlayerDataLoadedEvent evt)
    {
        GD.Print($"存档 '{evt.SlotName}' 加载完成");
        LoadGameData(); // 重新加载数据
    }
    
    [EventSubscribe]
    public void OnSlotChanged(PlayerSlotChangedEvent evt)
    {
        GD.Print($"切换存档: {evt.OldSlot} -> {evt.NewSlot}");
        LoadGameData(); // 加载新存档的数据
    }
    
    [EventSubscribe]
    public void OnDataError(PlayerDataErrorEvent evt)
    {
        GD.PrintErr($"存档错误: {evt.ErrorMessage}");
        // 可以显示错误对话框或回退到备份存档
    }
    
    // 公开访问接口
    public PlayerData GetPlayerData() => _playerData;
    public GameSettings GetGameSettings() => _gameSettings;
    public string GetCurrentSlot() => _dataManager.CurrentSlot;
    public List<string> GetAllSaveSlots() => _dataManager.GetAllSlots();
}

// 存档选择UI
public partial class SaveSlotUI : Control
{
    private SaveGameManager _saveManager;
    private VBoxContainer _slotContainer;
    
    public override void _Ready()
    {
        _saveManager = GetNode<SaveGameManager>("../SaveGameManager");
        _slotContainer = GetNode<VBoxContainer>("SlotContainer");
        RefreshSlotList();
    }
    
    private void RefreshSlotList()
    {
        // 清空现有按钮
        foreach (Node child in _slotContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // 为每个存档槽位创建按钮
        var slots = _saveManager.GetAllSaveSlots();
        foreach (var slot in slots)
        {
            var button = new Button { Text = $"存档: {slot}" };
            button.Pressed += () => LoadSlot(slot);
            _slotContainer.AddChild(button);
        }
        
        // 添加新建存档按钮
        var newButton = new Button { Text = "新建存档" };
        newButton.Pressed += CreateNewSlot;
        _slotContainer.AddChild(newButton);
    }
    
    private void LoadSlot(string slotName)
    {
        DataManager.Instance.SwitchSlot(slotName);
        GD.Print($"加载存档: {slotName}");
    }
    
    private void CreateNewSlot()
    {
        var slotName = $"存档_{DateTime.Now:yyyyMMdd_HHmmss}";
        DataManager.Instance.CreateNewSlot(slotName);
        DataManager.Instance.SwitchSlot(slotName);
        _saveManager.NewGame("新玩家");
        RefreshSlotList();
    }
}
```

## 🔧 高级功能

### 数据验证和备份

```csharp
public partial class DataBackupManager : Node
{
    private DataManager _dataManager;
    
    public override void _Ready()
    {
        _dataManager = DataManager.Instance;
    }
    
    // 创建数据备份
    public void CreateBackup(string backupName = null)
    {
        if (backupName == null)
            backupName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            
        _dataManager.CreateNewSlot(backupName);
        // 复制当前数据到备份槽位
        var currentSlot = _dataManager.CurrentSlot;
        _dataManager.SwitchSlot(backupName);
        _dataManager.SwitchSlot(currentSlot);
    }
    
    // 验证数据完整性
    public bool ValidateData()
    {
        var playerData = _dataManager.GetData<PlayerData>("playerData");
        
        if (playerData == null) return false;
        if (string.IsNullOrEmpty(playerData.Name)) return false;
        if (playerData.Level < 1) return false;
        
        return true;
    }
}
```

### 数据迁移

```csharp
public partial class DataMigrationManager : Node
{
    // 从旧版本迁移数据
    public void MigrateFromVersion1()
    {
        var dataManager = DataManager.Instance;
        
        // 检查是否需要迁移
        var version = dataManager.GetData<int>("dataVersion", 1);
        if (version >= 2) return;
        
        // 执行迁移逻辑
        var oldPlayerName = dataManager.GetData<string>("player_name", "");
        if (!string.IsNullOrEmpty(oldPlayerName))
        {
            var playerData = new PlayerData { Name = oldPlayerName };
            dataManager.SetData("playerData", playerData);
        }
        
        // 更新版本号
        dataManager.SetData("dataVersion", 2, true);
        GD.Print("数据迁移完成");
    }
}
```

## 🎯 最佳实践

### 1. 数据结构设计

```csharp
// ✅ 好的设计 - 结构化数据
public class GameProgress
{
    public int Chapter { get; set; }
    public int Level { get; set; }
    public float CompletionPercent { get; set; }
    public List<string> UnlockedFeatures { get; set; } = new();
}

// ❌ 避免 - 分散的键值对
// dataManager.SetData("chapter", 3);
// dataManager.SetData("level", 15);  
// dataManager.SetData("completion", 0.75f);
```

### 2. 错误处理

```csharp
[EventBusSubscriber]
public partial class DataErrorHandler : Node
{
    [EventSubscribe]
    public void OnDataError(PlayerDataErrorEvent evt)
    {
        // 记录错误
        GD.PrintErr($"数据操作失败: {evt.ErrorMessage}");
        
        // 尝试恢复
        if (evt.ErrorMessage.Contains("解密失败"))
        {
            // 可能是加密密钥变化，尝试禁用加密重新加载
            DataManager.Instance.EncryptionEnabled = false;
        }
        
        // 通知用户
        ShowErrorNotification(evt.ErrorMessage);
    }
}
```

### 3. 性能优化

```csharp
// 批量数据更新
public void UpdatePlayerStats(int level, int exp, int health)
{
    var dataManager = DataManager.Instance;
    
    // 批量设置，最后一个立即保存
    dataManager.SetData("playerLevel", level, false);
    dataManager.SetData("playerExp", exp, false);
    dataManager.SetData("playerHealth", health, true); // 立即保存
}
```

## ⚠️ 注意事项

1. **数据类型** - 确保存储的对象可以被JSON序列化
2. **加密密钥** - 生产环境中应使用更安全的密钥管理
3. **文件权限** - 确保游戏有权限访问用户数据目录
4. **数据大小** - 避免存储过大的数据对象，影响加载性能
5. **版本兼容** - 考虑数据结构变化时的向后兼容性

## 🐛 故障排除

### 常见问题

```csharp
// 检查存档是否损坏
public bool CheckSaveIntegrity()
{
    try
    {
        var testData = DataManager.Instance.GetData<string>("test", "ok");
        return testData == "ok";
    }
    catch
    {
        return false;
    }
}

// 重置损坏的存档
public void ResetCorruptedSave()
{
    DataManager.Instance.ResetData();
    GD.Print("存档已重置");
}
```

---

**DataManager** - 让你的 Godot C# 项目拥有可靠而强大的数据管理系统！