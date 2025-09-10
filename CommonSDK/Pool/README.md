# PoolManager - 对象池管理系统

一个为 Godot C# 项目设计的高效对象池管理系统，通过对象复用减少内存分配和垃圾回收，提升游戏性能。

## ✨ 特性

- 🔄 **对象复用** - 通过对象池复用减少内存分配开销
- ⚡ **高性能** - 避免频繁的实例化和销毁操作
- 🎯 **灵活管理** - 支持多种类型对象的独立池管理
- 🌍 **全局访问** - 单例模式确保全局统一管理
- 🔧 **自动扩展** - 对象池为空时自动创建新对象
- 📊 **日志监控** - 完整的日志记录便于调试和监控
- 🎮 **Godot集成** - 完美集成Godot节点系统

## 🚀 快速开始

### 1. 基本使用

```csharp
public partial class GameManager : Node
{
    public override void _Ready()
    {
        var poolManager = PoolManager.Instance;
        
        // 加载预制体
        var bulletPrefab = GD.Load<PackedScene>("res://Bullet.tscn");
        var enemyPrefab = GD.Load<PackedScene>("res://Enemy.tscn");
        var effectPrefab = GD.Load<PackedScene>("res://Effect.tscn");
        
        // 初始化对象池
        poolManager.InitializePool("Bullet", bulletPrefab, 100);  // 子弹池，初始100个
        poolManager.InitializePool("Enemy", enemyPrefab, 20);     // 敌人池，初始20个
        poolManager.InitializePool("Effect", effectPrefab, 50);  // 特效池，初始50个
    }
}
```

### 2. 对象生成和回收

```csharp
public partial class Player : CharacterBody2D
{
    public void Shoot()
    {
        var poolManager = PoolManager.Instance;
        
        // 从对象池生成子弹
        var bullet = poolManager.Spawn("Bullet", GetParent()) as Bullet;
        if (bullet != null)
        {
            bullet.GlobalPosition = GlobalPosition;
            bullet.Initialize(); // 重置子弹状态
        }
    }
}

public partial class Bullet : Area2D
{
    private float _lifetime = 5.0f;
    private float _speed = 500.0f;
    
    public void Initialize()
    {
        // 重置子弹状态
        _lifetime = 5.0f;
        Visible = true;
        
        // 设置生命周期定时器
        var timer = GetTree().CreateTimer(_lifetime);
        timer.Timeout += () => ReturnToPool();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 子弹移动逻辑
        Position += Vector2.Up * _speed * (float)delta;
    }
    
    private void OnBodyEntered(Node2D body)
    {
        // 碰撞检测
        if (body.IsInGroup("enemies"))
        {
            // 处理碰撞
            ReturnToPool();
        }
    }
    
    private void ReturnToPool()
    {
        // 回收到对象池
        PoolManager.Instance.Despawn("Bullet", this);
    }
}
```

## 📖 详细功能指南

### 对象池初始化

```csharp
public partial class PoolInitializer : Node
{
    public override void _Ready()
    {
        InitializeGamePools();
    }
    
    private void InitializeGamePools()
    {
        var poolManager = PoolManager.Instance;
        
        // 武器和道具池
        poolManager.InitializePool("Sword", GD.Load<PackedScene>("res://weapons/Sword.tscn"), 10);
        poolManager.InitializePool("Shield", GD.Load<PackedScene>("res://weapons/Shield.tscn"), 5);
        poolManager.InitializePool("HealthPotion", GD.Load<PackedScene>("res://items/HealthPotion.tscn"), 30);
        
        // 敌人池
        poolManager.InitializePool("Goblin", GD.Load<PackedScene>("res://enemies/Goblin.tscn"), 15);
        poolManager.InitializePool("Orc", GD.Load<PackedScene>("res://enemies/Orc.tscn"), 10);
        poolManager.InitializePool("Dragon", GD.Load<PackedScene>("res://enemies/Dragon.tscn"), 3);
        
        // 特效和UI池
        poolManager.InitializePool("Explosion", GD.Load<PackedScene>("res://effects/Explosion.tscn"), 25);
        poolManager.InitializePool("DamageText", GD.Load<PackedScene>("res://ui/DamageText.tscn"), 40);
        poolManager.InitializePool("ParticleSystem", GD.Load<PackedScene>("res://effects/Particles.tscn"), 20);
    }
}
```

### 高级对象管理

```csharp
// 可重用对象接口
public interface IPoolable
{
    void OnSpawn();   // 从池中取出时调用
    void OnDespawn(); // 返回池中时调用
}

// 实现可重用接口的敌人类
public partial class Enemy : CharacterBody2D, IPoolable
{
    [Export] public int Health = 100;
    [Export] public float Speed = 150.0f;
    
    private int _maxHealth;
    
    public override void _Ready()
    {
        _maxHealth = Health;
    }
    
    public void OnSpawn()
    {
        // 从对象池取出时重置状态
        Health = _maxHealth;
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        
        // 重置位置和动画
        Scale = Vector2.One;
        Modulate = Colors.White;
        
        // 启动AI
        StartAI();
    }
    
    public void OnDespawn()
    {
        // 返回对象池时清理状态
        StopAI();
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        
        // 断开所有信号连接
        DisconnectAllSignals();
    }
    
    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // 播放死亡动画后回收
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", Colors.Transparent, 0.5f);
        tween.TweenCallback(ReturnToPool);
    }
    
    private void ReturnToPool()
    {
        OnDespawn();
        PoolManager.Instance.Despawn("Enemy", this);
    }
    
    private void StartAI() { /* AI逻辑 */ }
    private void StopAI() { /* 停止AI */ }
    private void DisconnectAllSignals() { /* 断开信号 */ }
}
```

## 📋 完整示例 - 射击游戏对象池系统

```csharp
// 游戏主控制器
public partial class ShootingGame : Node
{
    private PackedScene _bulletScene;
    private PackedScene _enemyScene;
    private PackedScene _explosionScene;
    private PackedScene _powerUpScene;
    
    public override void _Ready()
    {
        LoadScenes();
        InitializePools();
    }
    
    private void LoadScenes()
    {
        _bulletScene = GD.Load<PackedScene>("res://Bullet.tscn");
        _enemyScene = GD.Load<PackedScene>("res://Enemy.tscn");
        _explosionScene = GD.Load<PackedScene>("res://Explosion.tscn");
        _powerUpScene = GD.Load<PackedScene>("res://PowerUp.tscn");
    }
    
    private void InitializePools()
    {
        var poolManager = PoolManager.Instance;
        
        // 根据游戏需求调整池大小
        poolManager.InitializePool("PlayerBullet", _bulletScene, 200);    // 玩家子弹池
        poolManager.InitializePool("EnemyBullet", _bulletScene, 300);     // 敌人子弹池
        poolManager.InitializePool("Enemy", _enemyScene, 50);             // 敌人池
        poolManager.InitializePool("Explosion", _explosionScene, 30);     // 爆炸特效池
        poolManager.InitializePool("PowerUp", _powerUpScene, 10);         // 道具池
    }
}

// 玩家控制器
public partial class Player : CharacterBody2D
{
    [Export] public float FireRate = 0.1f; // 射击间隔
    
    private float _fireTimer = 0f;
    
    public override void _Process(double delta)
    {
        _fireTimer -= (float)delta;
        
        if (Input.IsActionPressed("shoot") && _fireTimer <= 0f)
        {
            Shoot();
            _fireTimer = FireRate;
        }
    }
    
    private void Shoot()
    {
        var bullet = PoolManager.Instance.Spawn("PlayerBullet", GetParent()) as Bullet;
        if (bullet != null)
        {
            bullet.GlobalPosition = GlobalPosition + Vector2.Up * 20;
            bullet.Setup(Vector2.Up, 600f, true); // 向上，速度600，玩家子弹
        }
    }
    
    public void TakeDamage()
    {
        // 玩家受伤时生成爆炸特效
        var explosion = PoolManager.Instance.Spawn("Explosion", GetParent()) as Explosion;
        if (explosion != null)
        {
            explosion.GlobalPosition = GlobalPosition;
            explosion.Play();
        }
    }
}

// 子弹类
public partial class Bullet : Area2D, IPoolable
{
    private Vector2 _direction;
    private float _speed;
    private bool _isPlayerBullet;
    private float _lifetime = 10f;
    private float _currentLifetime;
    
    public void Setup(Vector2 direction, float speed, bool isPlayerBullet)
    {
        _direction = direction.Normalized();
        _speed = speed;
        _isPlayerBullet = isPlayerBullet;
        _currentLifetime = _lifetime;
        
        // 设置碰撞检测组
        if (_isPlayerBullet)
        {
            CollisionLayer = 2; // 玩家子弹层
            CollisionMask = 4;  // 敌人层
        }
        else
        {
            CollisionLayer = 8; // 敌人子弹层
            CollisionMask = 1;  // 玩家层
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 移动子弹
        GlobalPosition += _direction * _speed * (float)delta;
        
        // 检查生命周期
        _currentLifetime -= (float)delta;
        if (_currentLifetime <= 0)
        {
            ReturnToPool();
        }
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (_isPlayerBullet && body.IsInGroup("enemies"))
        {
            // 玩家子弹击中敌人
            if (body is Enemy enemy)
            {
                enemy.TakeDamage(25);
            }
            ReturnToPool();
        }
        else if (!_isPlayerBullet && body.IsInGroup("player"))
        {
            // 敌人子弹击中玩家
            if (body is Player player)
            {
                player.TakeDamage();
            }
            ReturnToPool();
        }
    }
    
    public void OnSpawn()
    {
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        _currentLifetime = _lifetime;
    }
    
    public void OnDespawn()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
    
    private void ReturnToPool()
    {
        string poolKey = _isPlayerBullet ? "PlayerBullet" : "EnemyBullet";
        PoolManager.Instance.Despawn(poolKey, this);
    }
}

// 敌人生成器
public partial class EnemySpawner : Node
{
    [Export] public float SpawnRate = 2.0f;
    [Export] public int MaxEnemies = 20;
    
    private float _spawnTimer = 0f;
    private int _currentEnemyCount = 0;
    
    public override void _Process(double delta)
    {
        _spawnTimer -= (float)delta;
        
        if (_spawnTimer <= 0f && _currentEnemyCount < MaxEnemies)
        {
            SpawnEnemy();
            _spawnTimer = SpawnRate;
        }
    }
    
    private void SpawnEnemy()
    {
        var enemy = PoolManager.Instance.Spawn("Enemy", GetParent()) as Enemy;
        if (enemy != null)
        {
            // 随机生成位置
            var spawnX = GD.RandRange(-400, 400);
            enemy.GlobalPosition = new Vector2(spawnX, -100);
            
            // 连接敌人死亡信号
            enemy.EnemyDied += OnEnemyDied;
            
            _currentEnemyCount++;
        }
    }
    
    private void OnEnemyDied()
    {
        _currentEnemyCount--;
        
        // 有概率生成道具
        if (GD.Randf() < 0.3f) // 30%概率
        {
            SpawnPowerUp();
        }
    }
    
    private void SpawnPowerUp()
    {
        var powerUp = PoolManager.Instance.Spawn("PowerUp", GetParent()) as PowerUp;
        if (powerUp != null)
        {
            var spawnX = GD.RandRange(-300, 300);
            powerUp.GlobalPosition = new Vector2(spawnX, GD.RandRange(-50, 50));
        }
    }
}

// 特效管理器
public partial class EffectManager : Node
{
    /// <summary>
    /// 在指定位置播放爆炸特效
    /// </summary>
    public void PlayExplosion(Vector2 position, float scale = 1.0f)
    {
        var explosion = PoolManager.Instance.Spawn("Explosion", this) as Explosion;
        if (explosion != null)
        {
            explosion.GlobalPosition = position;
            explosion.Scale = Vector2.One * scale;
            explosion.Play();
        }
    }
    
    /// <summary>
    /// 显示伤害数字
    /// </summary>
    public void ShowDamageText(Vector2 position, int damage)
    {
        var damageText = PoolManager.Instance.Spawn("DamageText", this) as DamageText;
        if (damageText != null)
        {
            damageText.GlobalPosition = position;
            damageText.ShowDamage(damage);
        }
    }
}

// 爆炸特效类
public partial class Explosion : AnimatedSprite2D, IPoolable
{
    [Signal]
    public delegate void AnimationFinishedEventHandler();
    
    public void Play()
    {
        Play("explosion");
        AnimationFinished += OnAnimationFinished;
    }
    
    private void OnAnimationFinished()
    {
        AnimationFinished -= OnAnimationFinished;
        ReturnToPool();
    }
    
    public void OnSpawn()
    {
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
    }
    
    public void OnDespawn()
    {
        Stop();
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
    
    private void ReturnToPool()
    {
        PoolManager.Instance.Despawn("Explosion", this);
    }
}
```

## 🎯 最佳实践

### 1. 合理的池大小设计

```csharp
// ✅ 根据游戏需求设计池大小
public void InitializePoolsSmart()
{
    var poolManager = PoolManager.Instance;
    
    // 高频对象 - 较大的池
    poolManager.InitializePool("Bullet", bulletScene, 200);      // 子弹频繁生成
    poolManager.InitializePool("Particle", particleScene, 100); // 粒子效果多
    
    // 中频对象 - 中等池
    poolManager.InitializePool("Enemy", enemyScene, 30);         // 敌人数量适中
    poolManager.InitializePool("Pickup", pickupScene, 20);      // 道具偶尔出现
    
    // 低频对象 - 较小池
    poolManager.InitializePool("Boss", bossScene, 3);           // Boss很少出现
    poolManager.InitializePool("Cutscene", cutsceneScene, 5);   // 过场动画不多
}
```

### 2. 实现IPoolable接口

```csharp
// ✅ 推荐：实现标准化的池对象接口
public partial class PoolableObject : Node2D, IPoolable
{
    public virtual void OnSpawn()
    {
        // 统一的生成逻辑
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        ResetState();
    }
    
    public virtual void OnDespawn()
    {
        // 统一的回收逻辑
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        CleanupState();
    }
    
    protected virtual void ResetState() { }
    protected virtual void CleanupState() { }
}
```

### 3. 避免内存泄漏

```csharp
public partial class SafePoolableObject : Node, IPoolable
{
    private List<Timer> _activeTimers = new();
    private List<Tween> _activeTweens = new();
    
    public void OnDespawn()
    {
        // 清理计时器
        foreach (var timer in _activeTimers)
        {
            timer?.QueueFree();
        }
        _activeTimers.Clear();
        
        // 清理补间动画
        foreach (var tween in _activeTweens)
        {
            tween?.Kill();
        }
        _activeTweens.Clear();
        
        // 断开信号连接
        DisconnectAllSignals();
    }
}
```

## ⚠️ 注意事项

1. **状态重置** - 确保对象返回池时完全重置状态
2. **内存管理** - 避免在池对象中持有大量引用
3. **信号连接** - 回收时记得断开所有信号连接
4. **池大小** - 根据实际需求设置合适的初始池大小
5. **线程安全** - 当前实现非线程安全，多线程使用需要额外同步

## 🔧 扩展功能

### 池统计和监控

```csharp
public partial class PoolMonitor : Node
{
    public void PrintPoolStats()
    {
        var poolManager = PoolManager.Instance;
        // 这里需要扩展PoolManager来暴露统计信息
        GD.Print("=== 对象池统计 ===");
        // GD.Print($"子弹池: 使用中 {activeCount}, 可用 {availableCount}");
    }
}
```

---

**PoolManager** - 让你的 Godot C# 项目拥有高效的对象复用能力，大幅提升游戏性能！