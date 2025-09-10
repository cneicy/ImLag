# FindUtils - 节点查找工具

一个为 Godot C# 项目设计的强大节点查找系统，提供类似 Unity 的查找功能，让节点查找变得简单高效。

## ✨ 特性

- 🔍 **类型查找** - 按类型查找单个或多个节点对象
- 📝 **名称查找** - 按名称进行精确或模糊匹配查找
- 👥 **组查找** - 基于 Godot 组系统的高效查找
- 🌳 **层级查找** - 支持向上、向下和同级查找
- 🎯 **Unity风格** - 提供类似Unity的组件查找方法
- ⚡ **高性能** - 优化的递归算法和缓存机制
- 🔧 **灵活配置** - 支持包含/排除非激活节点等选项

## 🚀 快速开始

### 1. 基本类型查找

```csharp
public partial class GameManager : Node
{
    public override void _Ready()
    {
        // 查找单个对象
        var player = this.FindObjectOfType<Player>();
        var enemy = this.FindObjectOfType<Enemy>();
        var ui = this.FindObjectOfType<UIManager>();
        
        // 查找所有对象
        var allEnemies = this.FindObjectsOfType<Enemy>();
        var allPickups = this.FindObjectsOfType<Pickup>();
        
        GD.Print($"找到 {allEnemies.Length} 个敌人");
        GD.Print($"找到 {allPickups.Length} 个道具");
    }
}
```

### 2. 组件查找 (Unity风格)

```csharp
public partial class Player : CharacterBody2D
{
    private HealthComponent _health;
    private WeaponComponent _weapon;
    
    public override void _Ready()
    {
        // 在自己身上查找组件
        _health = this.GetComponent<HealthComponent>();
        
        // 在子节点中查找组件
        _weapon = this.GetComponentInChildren<WeaponComponent>();
        
        // 在父节点中查找组件
        var gameManager = this.GetComponentInParent<GameManager>();
        
        // 查找多个组件
        var allWeapons = this.GetComponentsInChildren<WeaponComponent>();
    }
}
```

### 3. 名称查找

```csharp
public partial class UIManager : Control
{
    public override void _Ready()
    {
        // 在直接子节点中查找
        var healthBar = this.Find("HealthBar");
        var inventoryPanel = this.Find("InventoryPanel");
        
        // 在所有子节点中递归查找
        var settingsButton = this.FindInChildren("SettingsButton");
        var allButtons = this.FindAllInChildren("Button", exactMatch: false);
        
        GD.Print($"找到 {allButtons.Length} 个包含'Button'的节点");
    }
}
```

## 📖 详细功能指南

### 类型查找功能

```csharp
public partial class SceneManager : Node
{
    public void ManageScene()
    {
        // === 单对象查找 ===
        
        // 查找第一个玩家
        var player = this.FindObjectOfType<Player>();
        if (player != null)
        {
            GD.Print($"找到玩家: {player.Name}");
        }
        
        // 包含非激活对象的查找
        var hiddenEnemy = this.FindObjectOfType<Enemy>(includeInactive: true);
        
        // === 多对象查找 ===
        
        // 查找所有敌人
        var enemies = this.FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            enemy.StartAI();
        }
        
        // 查找所有UI元素
        var uiElements = this.FindObjectsOfType<Control>();
        GD.Print($"场景中有 {uiElements.Length} 个UI元素");
        
        // === 存在性检查 ===
        
        // 检查是否存在指定类型
        if (this.HasObjectOfType<Boss>())
        {
            GD.Print("场景中有Boss");
            StartBossBattle();
        }
        
        // 统计对象数量
        var enemyCount = this.CountObjectsOfType<Enemy>();
        GD.Print($"当前敌人数量: {enemyCount}");
    }
    
    private void StartBossBattle() { /* Boss战斗逻辑 */ }
}
```

### 层级查找功能

```csharp
public partial class CharacterSystem : Node
{
    public void SetupCharacter()
    {
        // === 向下查找（在子节点中） ===
        
        // 查找单个子组件
        var healthComponent = this.GetComponentInChildren<HealthComponent>();
        var weaponSystem = this.GetComponentInChildren<WeaponSystem>();
        
        // 查找多个子组件
        var allAnimators = this.GetComponentsInChildren<AnimationPlayer>();
        var allColliders = this.GetComponentsInChildren<CollisionShape2D>();
        
        // === 向上查找（在父节点中） ===
        
        // 查找父级管理器
        var gameManager = this.GetComponentInParent<GameManager>();
        var sceneController = this.GetComponentInParent<SceneController>();
        
        // 查找所有父级组件
        var allParentManagers = this.GetComponentsInParent<IManager>();
        
        // === 自身组件查找 ===
        
        // 获取当前节点的组件
        var rigidbody = this.GetComponent<RigidBody2D>();
        var sprite = this.GetComponent<Sprite2D>();
        
        if (healthComponent != null)
        {
            healthComponent.MaxHealth = 100;
        }
    }
}
```

### 组查找功能

```csharp
public partial class GroupManager : Node
{
    public void ManageGroups()
    {
        // === 单对象组查找 ===
        
        // 查找第一个玩家组成员
        var player = this.FindWithGroup("players");
        
        // 查找指定类型的组成员
        var mainEnemy = this.FindWithGroup<Enemy>("bosses");
        
        // === 多对象组查找 ===
        
        // 查找所有敌人组成员
        var allEnemies = this.FindObjectsWithGroup("enemies");
        foreach (var enemy in allEnemies)
        {
            if (enemy is Enemy enemyScript)
            {
                enemyScript.StartCombat();
            }
        }
        
        // 查找指定类型的组成员
        var allPickups = this.FindObjectsWithGroup<Pickup>("collectibles");
        GD.Print($"场景中有 {allPickups.Length} 个可收集物品");
        
        // === 组操作示例 ===
        SpawnEnemyWave();
    }
    
    private void SpawnEnemyWave()
    {
        // 找到所有敌人生成点
        var spawnPoints = this.FindObjectsWithGroup("enemy_spawns");
        
        foreach (var spawnPoint in spawnPoints)
        {
            // 在生成点创建敌人
            var enemyScene = GD.Load<PackedScene>("res://Enemy.tscn");
            var enemy = enemyScene.Instantiate<Enemy>();
            
            spawnPoint.AddChild(enemy);
            enemy.GlobalPosition = spawnPoint.GlobalPosition;
            
            // 将新敌人加入敌人组
            enemy.AddToGroup("enemies");
        }
    }
}
```

## 📋 完整示例 - 游戏管理系统

```csharp
// 游戏管理器 - 展示各种查找功能的使用
public partial class GameController : Node
{
    // 缓存重要的游戏对象
    private Player _player;
    private UIManager _uiManager;
    private AudioManager _audioManager;
    private List<Enemy> _enemies = new();
    
    public override void _Ready()
    {
        InitializeGameObjects();
        SetupGameSystems();
        StartGameLoop();
    }
    
    /// <summary>
    /// 初始化游戏对象引用
    /// </summary>
    private void InitializeGameObjects()
    {
        // 查找核心游戏对象
        _player = this.FindObjectOfType<Player>();
        if (_player == null)
        {
            GD.PrintErr("未找到玩家对象！");
            return;
        }
        
        _uiManager = this.FindObjectOfType<UIManager>();
        _audioManager = this.FindObjectOfType<AudioManager>();
        
        // 查找所有敌人
        var enemyArray = this.FindObjectsOfType<Enemy>();
        _enemies.AddRange(enemyArray);
        
        GD.Print($"游戏初始化完成 - 玩家: {_player.Name}, 敌人数量: {_enemies.Count}");
    }
    
    /// <summary>
    /// 设置游戏系统
    /// </summary>
    private void SetupGameSystems()
    {
        // 设置UI系统
        if (_uiManager != null)
        {
            _uiManager.ShowHUD();
            _uiManager.UpdatePlayerInfo(_player);
        }
        
        // 设置音频系统
        _audioManager?.PlayBackgroundMusic("game_theme");
        
        // 设置敌人AI
        foreach (var enemy in _enemies)
        {
            enemy.SetTarget(_player);
            enemy.StartAI();
        }
        
        // 查找并设置环境对象
        SetupEnvironment();
    }
    
    /// <summary>
    /// 设置游戏环境
    /// </summary>
    private void SetupEnvironment()
    {
        // 查找所有可交互对象
        var interactables = this.FindObjectsOfType<IInteractable>();
        GD.Print($"找到 {interactables.Length} 个可交互对象");
        
        // 查找所有道具
        var pickups = this.FindObjectsWithGroup<Pickup>("collectibles");
        foreach (var pickup in pickups)
        {
            pickup.SetPlayer(_player);
        }
        
        // 查找所有陷阱
        var traps = this.FindObjectsWithGroup("traps");
        foreach (var trap in traps)
        {
            if (trap.HasMethod("SetTarget"))
            {
                trap.Call("SetTarget", _player);
            }
        }
        
        // 查找存档点
        var checkpoints = this.FindObjectsOfType<Checkpoint>();
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.OnPlayerReached += OnCheckpointReached;
        }
    }
    
    /// <summary>
    /// 开始游戏循环
    /// </summary>
    private void StartGameLoop()
    {
        // 设置游戏状态
        GameState.Current = GameState.Playing;
        
        // 启动各种管理器
        var managers = this.FindObjectsOfType<IGameManager>();
        foreach (var manager in managers)
        {
            manager.StartManager();
        }
        
        GD.Print("游戏开始！");
    }
    
    /// <summary>
    /// 玩家死亡处理
    /// </summary>
    public void OnPlayerDied()
    {
        GD.Print("玩家死亡，重新加载关卡");
        
        // 停止所有敌人
        var allEnemies = this.FindObjectsOfType<Enemy>(includeInactive: true);
        foreach (var enemy in allEnemies)
        {
            enemy.StopAI();
        }
        
        // 重置环境
        ResetEnvironment();
        
        // 重新生成玩家
        RespawnPlayer();
    }
    
    /// <summary>
    /// 重置游戏环境
    /// </summary>
    private void ResetEnvironment()
    {
        // 重置所有可重置的对象
        var resetables = this.FindObjectsOfType<IResetable>();
        foreach (var resetable in resetables)
        {
            resetable.Reset();
        }
        
        // 重新激活所有道具
        var pickups = this.FindObjectsWithGroup("collectibles");
        foreach (var pickup in pickups)
        {
            if (!pickup.Visible)
            {
                pickup.Visible = true;
                pickup.ProcessMode = Node.ProcessModeEnum.Inherit;
            }
        }
    }
    
    /// <summary>
    /// 重生玩家
    /// </summary>
    private void RespawnPlayer()
    {
        // 找到最近的检查点
        var activeCheckpoint = this.FindObjectsOfType<Checkpoint>()
            .Where(cp => cp.IsActive)
            .OrderByDescending(cp => cp.ActivationTime)
            .FirstOrDefault();
        
        if (activeCheckpoint != null)
        {
            _player.GlobalPosition = activeCheckpoint.GlobalPosition;
            _player.Respawn();
            GD.Print($"玩家在检查点 {activeCheckpoint.Name} 重生");
        }
        else
        {
            // 回到起始位置
            var startPoint = this.FindWithGroup("start_point");
            if (startPoint != null)
            {
                _player.GlobalPosition = startPoint.GlobalPosition;
            }
            _player.Respawn();
            GD.Print("玩家在起始点重生");
        }
    }
    
    /// <summary>
    /// 检查点到达事件
    /// </summary>
    private void OnCheckpointReached(Checkpoint checkpoint)
    {
        GD.Print($"到达检查点: {checkpoint.Name}");
        _audioManager?.PlaySFX("checkpoint");
    }
    
    /// <summary>
    /// 生成新的敌人波次
    /// </summary>
    public void SpawnEnemyWave(int waveNumber)
    {
        var spawnPoints = this.FindObjectsWithGroup("enemy_spawns");
        var enemyPrefab = GD.Load<PackedScene>("res://enemies/Enemy.tscn");
        
        int enemiesToSpawn = waveNumber * 3; // 每波增加3个敌人
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            var randomSpawnPoint = spawnPoints[GD.RandRange(0, spawnPoints.Length - 1)];
            var enemy = enemyPrefab.Instantiate<Enemy>();
            
            randomSpawnPoint.AddChild(enemy);
            enemy.GlobalPosition = randomSpawnPoint.GlobalPosition;
            enemy.SetTarget(_player);
            enemy.AddToGroup("enemies");
            
            _enemies.Add(enemy);
        }
        
        GD.Print($"第 {waveNumber} 波敌人生成完成，共 {enemiesToSpawn} 个敌人");
    }
    
    /// <summary>
    /// 清理已死亡的敌人
    /// </summary>
    public void CleanupDeadEnemies()
    {
        var deadEnemies = _enemies.Where(e => !IsInstanceValid(e) || e.Health <= 0).ToList();
        
        foreach (var deadEnemy in deadEnemies)
        {
            _enemies.Remove(deadEnemy);
            deadEnemy?.QueueFree();
        }
        
        if (deadEnemies.Count > 0)
        {
            GD.Print($"清理了 {deadEnemies.Count} 个死亡敌人");
        }
    }
    
    /// <summary>
    /// 获取游戏统计信息
    /// </summary>
    public void PrintGameStats()
    {
        var totalEnemies = this.CountObjectsOfType<Enemy>();
        var totalPickups = this.FindObjectsWithGroup("collectibles").Length;
        var totalNPCs = this.CountObjectsOfType<NPC>();
        
        GD.Print("=== 游戏统计 ===");
        GD.Print($"敌人数量: {totalEnemies}");
        GD.Print($"道具数量: {totalPickups}");
        GD.Print($"NPC数量: {totalNPCs}");
        GD.Print($"玩家位置: {_player?.GlobalPosition}");
    }
}

// 示例接口定义
public interface IInteractable
{
    void Interact(Player player);
}

public interface IGameManager
{
    void StartManager();
    void StopManager();
}

public interface IResetable
{
    void Reset();
}

// 示例组件类
public partial class HealthComponent : Node
{
    [Export] public int MaxHealth = 100;
    public int CurrentHealth { get; private set; }
    
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }
}

public partial class WeaponComponent : Node
{
    [Export] public int Damage = 25;
    [Export] public float FireRate = 0.5f;
}

public partial class Pickup : Area2D
{
    private Player _player;
    
    public void SetPlayer(Player player)
    {
        _player = player;
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (body == _player)
        {
            Collect();
        }
    }
    
    private void Collect()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        // 收集逻辑
    }
}

public partial class Checkpoint : Area2D
{
    [Signal]
    public delegate void PlayerReachedEventHandler(Checkpoint checkpoint);
    
    public bool IsActive { get; private set; }
    public double ActivationTime { get; private set; }
    
    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            IsActive = true;
            ActivationTime = Time.GetUnixTimeFromSystem();
            EmitSignal(SignalName.PlayerReached, this);
        }
    }
}
```

## 🎯 最佳实践

### 1. 性能优化

```csharp
public partial class OptimizedFinder : Node
{
    // ✅ 缓存频繁查找的对象
    private static Player _cachedPlayer;
    private static UIManager _cachedUIManager;
    
    public static Player GetPlayer(Node context)
    {
        if (_cachedPlayer == null || !IsInstanceValid(_cachedPlayer))
        {
            _cachedPlayer = context.FindObjectOfType<Player>();
        }
        return _cachedPlayer;
    }
    
    // ✅ 使用组查找而不是类型查找（当适用时）
    public void FindEnemiesEfficiently()
    {
        // 更快：使用组查找
        var enemies = this.FindObjectsWithGroup<Enemy>("enemies");
        
        // 较慢：类型查找（需要遍历整个场景树）
        // var enemies = this.FindObjectsOfType<Enemy>();
    }
    
    // ✅ 批量操作
    public void BatchFindAndOperate()
    {
        var allEnemies = this.FindObjectsOfType<Enemy>();
        foreach (var enemy in allEnemies)
        {
            enemy.SetTarget(GetPlayer(this));
            enemy.StartAI();
        }
    }
}
```

### 2. 错误处理

```csharp
public partial class SafeFinder : Node
{
    public void SafeFindExample()
    {
        // ✅ 安全的查找模式
        var player = this.FindObjectOfType<Player>();
        if (player != null)
        {
            player.TakeDamage(10);
        }
        else
        {
            GD.PrintErr("未找到玩家对象");
        }
        
        // ✅ 空检查的扩展方法使用
        var enemies = this.FindObjectsOfType<Enemy>();
        if (enemies.Length > 0)
        {
            foreach (var enemy in enemies)
            {
                if (IsInstanceValid(enemy))
                {
                    enemy.StartCombat();
                }
            }
        }
    }
}
```

### 3. 合理的查找范围

```csharp
public partial class ScopedFinder : Node
{
    public void FindWithProperScope()
    {
        // ✅ 在合适的范围内查找
        
        // 只在UI层查找UI元素
        var uiRoot = GetNode("/root/UI");
        var healthBar = uiRoot.FindInChildren("HealthBar");
        
        // 只在游戏世界中查找游戏对象
        var gameWorld = GetNode("/root/GameWorld");
        var enemies = gameWorld.FindObjectsOfType<Enemy>();
        
        // 在特定父节点下查找子组件
        var weaponSystem = GetNode("WeaponSystem");
        var weapons = weaponSystem.GetComponentsInChildren<Weapon>();
    }
}
```

## ⚠️ 注意事项

1. **性能考虑** - 频繁查找会影响性能，应适当缓存结果
2. **生命周期** - 确保查找到的对象仍然有效（未被释放）
3. **查找范围** - 明确查找范围，避免不必要的全场景遍历
4. **空检查** - 始终检查查找结果是否为空
5. **组使用** - 合理使用Godot的组系统可以显著提升查找效率

---

**FindUtils** - 让你的 Godot C# 项目拥有强大而高效的节点查找能力！