# VariantUtils - Variant类型转换工具

一个专为 Godot C# 项目设计的 Variant 类型转换工具，提供 C# 类型与 Godot Variant 类型之间的无缝转换。

## ✨ 特性

- 🔄 **双向转换** - C# 类型与 Variant 类型的相互转换
- 📊 **全类型支持** - 支持基本类型、容器类型、Node类型等
- 🏗️ **泛型支持** - 完整支持泛型集合和字典
- ⚡ **高性能** - 优化的转换算法和类型检查
- 🛡️ **类型安全** - 严格的类型检查和异常处理
- 🔧 **易于使用** - 简单直观的API设计

## 🚀 快速开始

### 1. 基本类型转换

```csharp
public partial class TypeConversionExample : Node
{
    public override void _Ready()
    {
        // === C# 到 Variant 转换 ===
        
        // 基本类型
        var intVariant = VariantUtils.CSharpToVariant(42);
        var stringVariant = VariantUtils.CSharpToVariant("Hello World");
        var boolVariant = VariantUtils.CSharpToVariant(true);
        var floatVariant = VariantUtils.CSharpToVariant(3.14f);
        
        // Godot 特定类型
        var vectorVariant = VariantUtils.CSharpToVariant(new Vector2(10, 20));
        var colorVariant = VariantUtils.CSharpToVariant(Colors.Red);
        
        // === Variant 到 C# 转换 ===
        
        var intValue = VariantUtils.VariantToCSharp<int>(intVariant);
        var stringValue = VariantUtils.VariantToCSharp<string>(stringVariant);
        var boolValue = VariantUtils.VariantToCSharp<bool>(boolVariant);
        var vectorValue = VariantUtils.VariantToCSharp<Vector2>(vectorVariant);
        
        GD.Print($"转换结果 - Int: {intValue}, String: {stringValue}, Bool: {boolValue}");
    }
}
```

### 2. 集合类型转换

```csharp
public partial class CollectionExample : Node
{
    public override void _Ready()
    {
        // === 数组转换 ===
        
        // C# 数组到 Variant
        int[] intArray = { 1, 2, 3, 4, 5 };
        var arrayVariant = VariantUtils.CSharpToVariant(intArray);
        
        // Variant 到 C# 数组
        var convertedArray = VariantUtils.VariantToCSharp<int[]>(arrayVariant);
        
        // === List 转换 ===
        
        // C# List 到 Variant
        var stringList = new List<string> { "apple", "banana", "cherry" };
        var listVariant = VariantUtils.CSharpToVariant(stringList);
        
        // Variant 到 C# List
        var convertedList = VariantUtils.VariantToCSharp<List<string>>(listVariant);
        
        // === 复杂类型列表 ===
        
        var vectorList = new List<Vector2>
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6)
        };
        
        var vectorListVariant = VariantUtils.CSharpToVariant(vectorList);
        var convertedVectorList = VariantUtils.VariantToCSharp<List<Vector2>>(vectorListVariant);
        
        GD.Print($"数组长度: {convertedArray.Length}, 列表数量: {convertedList.Count}");
    }
}
```

### 3. 字典转换

```csharp
public partial class DictionaryExample : Node
{
    public override void _Ready()
    {
        // === 简单字典转换 ===
        
        var playerStats = new Dictionary<string, int>
        {
            ["health"] = 100,
            ["mana"] = 50,
            ["experience"] = 1250
        };
        
        var statsVariant = VariantUtils.CSharpToVariant(playerStats);
        var convertedStats = VariantUtils.VariantToCSharp<Dictionary<string, int>>(statsVariant);
        
        // === 复杂字典转换 ===
        
        var gameConfig = new Dictionary<string, object>
        {
            ["playerName"] = "Hero",
            ["level"] = 5,
            ["position"] = new Vector2(100, 200),
            ["isAlive"] = true
        };
        
        // 注意：混合类型字典需要特殊处理
        var configVariant = ConvertMixedDictionary(gameConfig);
        
        GD.Print($"玩家血量: {convertedStats["health"]}");
    }
    
    private Variant ConvertMixedDictionary(Dictionary<string, object> dict)
    {
        var godotDict = new Godot.Collections.Dictionary();
        
        foreach (var kvp in dict)
        {
            godotDict[kvp.Key] = VariantUtils.CSharpToVariant(kvp.Value);
        }
        
        return Variant.CreateFrom(godotDict);
    }
}
```

## 📖 详细功能指南

### 游戏数据序列化

```csharp
public partial class GameDataManager : Node
{
    /// <summary>
    /// 玩家数据结构
    /// </summary>
    public class PlayerData
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public Vector2 Position { get; set; }
        public List<string> Inventory { get; set; } = new();
        public Dictionary<string, int> Stats { get; set; } = new();
    }
    
    public void SavePlayerData(PlayerData playerData)
    {
        try
        {
            // 转换玩家数据为可序列化的字典
            var dataDict = new Dictionary<string, object>
            {
                ["name"] = playerData.Name,
                ["level"] = playerData.Level,
                ["position"] = playerData.Position,
                ["inventory"] = playerData.Inventory,
                ["stats"] = playerData.Stats
            };
            
            // 转换为 Variant 以便存储
            var saveData = new Godot.Collections.Dictionary();
            foreach (var kvp in dataDict)
            {
                saveData[kvp.Key] = VariantUtils.CSharpToVariant(kvp.Value);
            }
            
            // 保存到文件
            var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);
            saveGame.StoreVar(Variant.CreateFrom(saveData));
            saveGame.Close();
            
            GD.Print("游戏数据保存成功");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"保存游戏数据失败: {ex.Message}");
        }
    }
    
    public PlayerData LoadPlayerData()
    {
        try
        {
            if (!FileAccess.FileExists("user://savegame.save"))
            {
                GD.Print("存档文件不存在，创建新游戏数据");
                return CreateDefaultPlayerData();
            }
            
            var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);
            var saveDataVariant = saveGame.GetVar();
            saveGame.Close();
            
            var saveData = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(saveDataVariant);
            
            var playerData = new PlayerData
            {
                Name = VariantUtils.VariantToCSharp<string>(saveData["name"]),
                Level = VariantUtils.VariantToCSharp<int>(saveData["level"]),
                Position = VariantUtils.VariantToCSharp<Vector2>(saveData["position"]),
                Inventory = VariantUtils.VariantToCSharp<List<string>>(saveData["inventory"]),
                Stats = VariantUtils.VariantToCSharp<Dictionary<string, int>>(saveData["stats"])
            };
            
            GD.Print("游戏数据加载成功");
            return playerData;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"加载游戏数据失败: {ex.Message}");
            return CreateDefaultPlayerData();
        }
    }
    
    private PlayerData CreateDefaultPlayerData()
    {
        return new PlayerData
        {
            Name = "新玩家",
            Level = 1,
            Position = Vector2.Zero,
            Inventory = new List<string> { "木剑", "生命药水" },
            Stats = new Dictionary<string, int>
            {
                ["health"] = 100,
                ["mana"] = 50,
                ["strength"] = 10,
                ["agility"] = 8
            }
        };
    }
}
```

### 网络数据传输

```csharp
public partial class NetworkManager : Node
{
    /// <summary>
    /// 网络消息结构
    /// </summary>
    public class NetworkMessage
    {
        public string Type { get; set; }
        public int PlayerId { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    public void SendPlayerUpdate(int playerId, Vector2 position, int health)
    {
        var message = new NetworkMessage
        {
            Type = "player_update",
            PlayerId = playerId,
            Data = new Dictionary<string, object>
            {
                ["position"] = position,
                ["health"] = health,
                ["timestamp"] = Time.GetUnixTimeFromSystem()
            }
        };
        
        var networkData = SerializeMessage(message);
        // 发送网络数据...
        
        GD.Print($"发送玩家更新: ID={playerId}, 位置={position}");
    }
    
    public void SendInventoryUpdate(int playerId, List<string> inventory)
    {
        var message = new NetworkMessage
        {
            Type = "inventory_update",
            PlayerId = playerId,
            Data = new Dictionary<string, object>
            {
                ["inventory"] = inventory,
                ["count"] = inventory.Count
            }
        };
        
        var networkData = SerializeMessage(message);
        // 发送网络数据...
    }
    
    private Variant SerializeMessage(NetworkMessage message)
    {
        var messageDict = new Dictionary<string, object>
        {
            ["type"] = message.Type,
            ["player_id"] = message.PlayerId,
            ["data"] = message.Data
        };
        
        var godotDict = new Godot.Collections.Dictionary();
        foreach (var kvp in messageDict)
        {
            godotDict[kvp.Key] = VariantUtils.CSharpToVariant(kvp.Value);
        }
        
        return Variant.CreateFrom(godotDict);
    }
    
    public NetworkMessage DeserializeMessage(Variant networkData)
    {
        try
        {
            var messageDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(networkData);
            
            var message = new NetworkMessage
            {
                Type = VariantUtils.VariantToCSharp<string>(messageDict["type"]),
                PlayerId = VariantUtils.VariantToCSharp<int>(messageDict["player_id"])
            };
            
            var dataDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(messageDict["data"]);
            foreach (Variant key in dataDict.Keys)
            {
                var keyStr = key.AsString();
                message.Data[keyStr] = ConvertVariantToObject(dataDict[key]);
            }
            
            return message;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"反序列化网络消息失败: {ex.Message}");
            return null;
        }
    }
    
    private object ConvertVariantToObject(Variant variant)
    {
        return variant.VariantType switch
        {
            Variant.Type.Bool => variant.AsBool(),
            Variant.Type.Int => variant.AsInt32(),
            Variant.Type.Float => variant.AsSingle(),
            Variant.Type.String => variant.AsString(),
            Variant.Type.Vector2 => variant.AsVector2(),
            Variant.Type.Vector3 => variant.AsVector3(),
            Variant.Type.Array => VariantUtils.VariantToCSharp<List<object>>(variant),
            _ => variant.AsString() // 默认转为字符串
        };
    }
}
```

## 📋 完整示例 - 游戏配置系统

```csharp
// 游戏配置管理器
public partial class GameConfigManager : Node
{
    /// <summary>
    /// 游戏配置数据结构
    /// </summary>
    public class GameConfig
    {
        public VideoSettings Video { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
        public ControlSettings Controls { get; set; } = new();
        public GameplaySettings Gameplay { get; set; } = new();
    }
    
    public class VideoSettings
    {
        public Vector2I Resolution { get; set; } = new(1920, 1080);
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = true;
        public int MaxFPS { get; set; } = 60;
        public float Brightness { get; set; } = 1.0f;
    }
    
    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SFXVolume { get; set; } = 1.0f;
        public bool Muted { get; set; } = false;
    }
    
    public class ControlSettings
    {
        public Dictionary<string, string> KeyBindings { get; set; } = new();
        public float MouseSensitivity { get; set; } = 1.0f;
        public bool InvertYAxis { get; set; } = false;
    }
    
    public class GameplaySettings
    {
        public string Difficulty { get; set; } = "Normal";
        public bool AutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 300; // 秒
        public List<string> EnabledMods { get; set; } = new();
    }
    
    private const string ConfigPath = "user://game_config.cfg";
    private GameConfig _currentConfig;
    
    public override void _Ready()
    {
        LoadConfig();
        ApplyConfig();
    }
    
    /// <summary>
    /// 加载游戏配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            if (!FileAccess.FileExists(ConfigPath))
            {
                GD.Print("配置文件不存在，创建默认配置");
                _currentConfig = CreateDefaultConfig();
                SaveConfig();
                return;
            }
            
            var configFile = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
            var configVariant = configFile.GetVar();
            configFile.Close();
            
            _currentConfig = DeserializeConfig(configVariant);
            GD.Print("游戏配置加载成功");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"加载配置失败: {ex.Message}");
            _currentConfig = CreateDefaultConfig();
        }
    }
    
    /// <summary>
    /// 保存游戏配置
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            var configVariant = SerializeConfig(_currentConfig);
            
            var configFile = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
            configFile.StoreVar(configVariant);
            configFile.Close();
            
            GD.Print("游戏配置保存成功");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"保存配置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 应用配置到游戏
    /// </summary>
    private void ApplyConfig()
    {
        // 应用视频设置
        DisplayServer.WindowSetSize(_currentConfig.Video.Resolution);
        DisplayServer.WindowSetMode(_currentConfig.Video.Fullscreen 
            ? DisplayServer.WindowMode.Fullscreen 
            : DisplayServer.WindowMode.Windowed);
        
        Engine.MaxFps = _currentConfig.Video.MaxFPS;
        
        // 应用音频设置
        var masterBus = AudioServer.GetBusIndex("Master");
        var musicBus = AudioServer.GetBusIndex("Music");
        var sfxBus = AudioServer.GetBusIndex("SFX");
        
        AudioServer.SetBusVolumeDb(masterBus, LinearToDb(_currentConfig.Audio.MasterVolume));
        AudioServer.SetBusVolumeDb(musicBus, LinearToDb(_currentConfig.Audio.MusicVolume));
        AudioServer.SetBusVolumeDb(sfxBus, LinearToDb(_currentConfig.Audio.SFXVolume));
        
        AudioServer.SetBusMute(masterBus, _currentConfig.Audio.Muted);
        
        GD.Print("配置应用完成");
    }
    
    /// <summary>
    /// 序列化配置
    /// </summary>
    private Variant SerializeConfig(GameConfig config)
    {
        var configDict = new Dictionary<string, object>
        {
            ["video"] = new Dictionary<string, object>
            {
                ["resolution"] = config.Video.Resolution,
                ["fullscreen"] = config.Video.Fullscreen,
                ["vsync"] = config.Video.VSync,
                ["max_fps"] = config.Video.MaxFPS,
                ["brightness"] = config.Video.Brightness
            },
            ["audio"] = new Dictionary<string, object>
            {
                ["master_volume"] = config.Audio.MasterVolume,
                ["music_volume"] = config.Audio.MusicVolume,
                ["sfx_volume"] = config.Audio.SFXVolume,
                ["muted"] = config.Audio.Muted
            },
            ["controls"] = new Dictionary<string, object>
            {
                ["key_bindings"] = config.Controls.KeyBindings,
                ["mouse_sensitivity"] = config.Controls.MouseSensitivity,
                ["invert_y_axis"] = config.Controls.InvertYAxis
            },
            ["gameplay"] = new Dictionary<string, object>
            {
                ["difficulty"] = config.Gameplay.Difficulty,
                ["auto_save"] = config.Gameplay.AutoSave,
                ["auto_save_interval"] = config.Gameplay.AutoSaveInterval,
                ["enabled_mods"] = config.Gameplay.EnabledMods
            }
        };
        
        return ConvertNestedDictionary(configDict);
    }
    
    /// <summary>
    /// 反序列化配置
    /// </summary>
    private GameConfig DeserializeConfig(Variant configVariant)
    {
        var configDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(configVariant);
        
        var config = new GameConfig();
        
        // 视频设置
        if (configDict.TryGetValue("video", out var videoVariant))
        {
            var videoDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(videoVariant);
            config.Video.Resolution = VariantUtils.VariantToCSharp<Vector2I>(videoDict["resolution"]);
            config.Video.Fullscreen = VariantUtils.VariantToCSharp<bool>(videoDict["fullscreen"]);
            config.Video.VSync = VariantUtils.VariantToCSharp<bool>(videoDict["vsync"]);
            config.Video.MaxFPS = VariantUtils.VariantToCSharp<int>(videoDict["max_fps"]);
            config.Video.Brightness = VariantUtils.VariantToCSharp<float>(videoDict["brightness"]);
        }
        
        // 音频设置
        if (configDict.TryGetValue("audio", out var audioVariant))
        {
            var audioDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(audioVariant);
            config.Audio.MasterVolume = VariantUtils.VariantToCSharp<float>(audioDict["master_volume"]);
            config.Audio.MusicVolume = VariantUtils.VariantToCSharp<float>(audioDict["music_volume"]);
            config.Audio.SFXVolume = VariantUtils.VariantToCSharp<float>(audioDict["sfx_volume"]);
            config.Audio.Muted = VariantUtils.VariantToCSharp<bool>(audioDict["muted"]);
        }
        
        // 控制设置
        if (configDict.TryGetValue("controls", out var controlsVariant))
        {
            var controlsDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(controlsVariant);
            config.Controls.KeyBindings = VariantUtils.VariantToCSharp<Dictionary<string, string>>(controlsDict["key_bindings"]);
            config.Controls.MouseSensitivity = VariantUtils.VariantToCSharp<float>(controlsDict["mouse_sensitivity"]);
            config.Controls.InvertYAxis = VariantUtils.VariantToCSharp<bool>(controlsDict["invert_y_axis"]);
        }
        
        // 游戏玩法设置
        if (configDict.TryGetValue("gameplay", out var gameplayVariant))
        {
            var gameplayDict = VariantUtils.VariantToCSharp<Godot.Collections.Dictionary>(gameplayVariant);
            config.Gameplay.Difficulty = VariantUtils.VariantToCSharp<string>(gameplayDict["difficulty"]);
            config.Gameplay.AutoSave = VariantUtils.VariantToCSharp<bool>(gameplayDict["auto_save"]);
            config.Gameplay.AutoSaveInterval = VariantUtils.VariantToCSharp<int>(gameplayDict["auto_save_interval"]);
            config.Gameplay.EnabledMods = VariantUtils.VariantToCSharp<List<string>>(gameplayDict["enabled_mods"]);
        }
        
        return config;
    }
    
    /// <summary>
    /// 转换嵌套字典为Variant
    /// </summary>
    private Variant ConvertNestedDictionary(Dictionary<string, object> dict)
    {
        var godotDict = new Godot.Collections.Dictionary();
        
        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                godotDict[kvp.Key] = ConvertNestedDictionary(nestedDict);
            }
            else
            {
                godotDict[kvp.Key] = VariantUtils.CSharpToVariant(kvp.Value);
            }
        }
        
        return Variant.CreateFrom(godotDict);
    }
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    private GameConfig CreateDefaultConfig()
    {
        return new GameConfig
        {
            Video = new VideoSettings
            {
                Resolution = new Vector2I(1920, 1080),
                Fullscreen = false,
                VSync = true,
                MaxFPS = 60,
                Brightness = 1.0f
            },
            Audio = new AudioSettings
            {
                MasterVolume = 1.0f,
                MusicVolume = 0.8f,
                SFXVolume = 1.0f,
                Muted = false
            },
            Controls = new ControlSettings
            {
                KeyBindings = new Dictionary<string, string>
                {
                    ["move_up"] = "W",
                    ["move_down"] = "S",
                    ["move_left"] = "A",
                    ["move_right"] = "D",
                    ["jump"] = "Space",
                    ["attack"] = "Mouse1",
                    ["interact"] = "E"
                },
                MouseSensitivity = 1.0f,
                InvertYAxis = false
            },
            Gameplay = new GameplaySettings
            {
                Difficulty = "Normal",
                AutoSave = true,
                AutoSaveInterval = 300,
                EnabledMods = new List<string>()
            }
        };
    }
    
    // 获取和设置配置的公共方法
    public GameConfig GetConfig() => _currentConfig;
    
    public void UpdateVideoSettings(VideoSettings videoSettings)
    {
        _currentConfig.Video = videoSettings;
        ApplyConfig();
        SaveConfig();
    }
    
    public void UpdateAudioSettings(AudioSettings audioSettings)
    {
        _currentConfig.Audio = audioSettings;
        ApplyConfig();
        SaveConfig();
    }
    
    private float LinearToDb(float linear)
    {
        return linear > 0 ? Mathf.LinearToDb(linear) : -80.0f;
    }
}
```

## 🎯 最佳实践

### 1. 类型安全处理

```csharp
public partial class SafeConverter : Node
{
    public void SafeConversionExample()
    {
        // ✅ 始终进行空检查
        Variant data = GetSomeData();
        if (data.VariantType != Variant.Type.Nil)
        {
            try
            {
                var converted = VariantUtils.VariantToCSharp<int>(data);
                // 使用转换后的数据
            }
            catch (InvalidCastException ex)
            {
                GD.PrintErr($"类型转换失败: {ex.Message}");
            }
        }
        
        // ✅ 提供默认值
        var safeValue = TryConvertWithDefault<string>(data, "default_value");
    }
    
    private T TryConvertWithDefault<T>(Variant variant, T defaultValue)
    {
        try
        {
            return variant.VariantType == Variant.Type.Nil 
                ? defaultValue 
                : VariantUtils.VariantToCSharp<T>(variant);
        }
        catch
        {
            return defaultValue;
        }
    }
    
    private Variant GetSomeData() => default; // 示例方法
}
```

### 2. 性能优化

```csharp
public partial class OptimizedConverter : Node
{
    private readonly Dictionary<Type, Func<object, Variant>> _converterCache = new();
    
    public void OptimizedConversion()
    {
        // ✅ 批量转换
        var dataList = new List<object> { 1, "hello", true, 3.14f };
        var variants = dataList.Select(VariantUtils.CSharpToVariant).ToArray();
        
        // ✅ 重用转换器
        var converter = GetOrCreateConverter(typeof(int));
        var result = converter(42);
    }
    
    private Func<object, Variant> GetOrCreateConverter(Type type)
    {
        if (!_converterCache.TryGetValue(type, out var converter))
        {
            converter = obj => VariantUtils.CSharpToVariant(obj);
            _converterCache[type] = converter;
        }
        return converter;
    }
}
```

## ⚠️ 注意事项

1. **类型兼容性** - 确保转换的类型被 Variant 支持
2. **异常处理** - 转换可能失败，需要适当的错误处理
3. **性能考虑** - 大量转换时考虑缓存和批处理
4. **空值处理** - 处理 null 和 Nil 值的情况
5. **嵌套类型** - 复杂嵌套结构需要特殊处理

---

**VariantUtils** - 让你的 Godot C# 项目拥有强大的类型转换能力！