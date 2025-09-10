# EventBus - 事件系统

一个为 Godot C# 项目设计的高性能、类型安全的事件总线系统，支持优先级处理、自动注册、异步事件处理和完整的生命周期管理。

## ✨ 核心特性

- 🔒 **类型安全** - 基于泛型的强类型事件系统，编译时检查事件类型
- ⚡ **高性能** - 优化的事件分发机制，支持细粒度优先级排序
- 🤖 **智能注册** - 自动发现和注册事件处理程序，支持Godot节点生命周期管理
- 🎯 **优先级系统** - 支持枚举和数字双重优先级，精确控制执行顺序
- 🔄 **同步/异步** - 统一的异步处理架构，同时支持同步和异步事件处理
- 🧵 **线程安全** - 完全的线程安全设计，支持多线程环境
- 📱 **零依赖** - 仅依赖 Godot 引擎和 .NET 标准库
- 🔍 **调试友好** - 丰富的调试信息、统计功能和原始方法名显示
- ⚙️ **事件控制** - 支持事件取消、结果设置和阶段管理
- 🔄 **向后兼容** - 完整的向后兼容支持，平滑迁移

## 🚀 快速开始

### 1. 定义事件

```csharp
// 基础事件 - 所有事件都必须继承 EventBase
public class PlayerHealthChangedEvent : EventBase
{
    public int PlayerId { get; set; }
    public int OldHealth { get; set; }
    public int NewHealth { get; set; }
    public float HealthPercentage => NewHealth / 100f;
}

// 可取消事件 - 支持取消操作
[Cancelable]
public class PlayerMoveEvent : EventBase
{
    public Vector2 OldPosition { get; set; }
    public Vector2 NewPosition { get; set; }
    public float MoveSpeed { get; set; }
}

// 有结果事件 - 支持设置处理结果
[HasResult]
public class ItemUseEvent : EventBase
{
    public Item Item { get; set; }
    public Player Player { get; set; }
}
```

### 2. 创建事件订阅者

```csharp
// 标记类为 EventBus 订阅者
[EventBusSubscriber]
public partial class GameManager : Node
{
    // 最高优先级异步处理程序
    [EventSubscribe(EventPriority.HIGHEST)]
    public async Task OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
    {
        GD.Print($"[HIGHEST] 玩家 {evt.PlayerId} 血量变化: {evt.OldHealth} -> {evt.NewHealth}");
        
        if (evt.NewHealth <= 0)
        {
            await HandlePlayerDeath(evt.PlayerId);
        }
    }
    
    // 高优先级，接收已取消的事件
    [EventSubscribe(EventPriority.HIGH, receiveCanceled: true)]
    public void OnPlayerMove(PlayerMoveEvent evt)
    {
        GD.Print($"[HIGH] 玩家移动监控: {evt.OldPosition} -> {evt.NewPosition}");
        
        // 验证移动是否合法
        if (!IsValidMove(evt.NewPosition))
        {
            evt.SetCanceled(true);
            GD.Print("移动被取消 - 位置无效");
        }
    }
    
    // 默认优先级同步处理程序
    [EventSubscribe]
    public void OnItemUse(ItemUseEvent evt)
    {
        if (evt.Item.CanUse(evt.Player))
        {
            evt.SetResult(EventResult.ALLOW);
            GD.Print($"物品使用被允许: {evt.Item.Name}");
        }
        else
        {
            evt.SetResult(EventResult.DENY);
            GD.Print($"物品使用被拒绝: {evt.Item.Name}");
        }
    }
    
    // 监控优先级 - 最后执行，用于日志和统计
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public void MonitorAllEvents(EventBase evt)
    {
        GD.Print($"[MONITOR] 事件处理完成: {evt.GetType().Name} (ID: {evt.EventId})");
        RecordEventStatistics(evt);
    }
    
    private async Task HandlePlayerDeath(int playerId)
    {
        GD.Print($"处理玩家 {playerId} 死亡...");
        await Task.Delay(1000); // 死亡动画
        // 重生逻辑
    }
    
    private bool IsValidMove(Vector2 position)
    {
        // 检查移动位置是否合法
        return position.X >= 0 && position.Y >= 0;
    }
    
    private void RecordEventStatistics(EventBase evt)
    {
        // 记录事件统计信息
    }
}
```

### 3. 触发事件

```csharp
public partial class Player : CharacterBody2D
{
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float MoveSpeed { get; set; } = 300f;
    
    private int _health = 100;
    private int _playerId = 1;
    
    public void TakeDamage(int damage)
    {
        int oldHealth = _health;
        _health = Mathf.Max(0, _health - damage);
        
        // 同步触发血量变化事件
        EventBus.TriggerEvent(new PlayerHealthChangedEvent 
        { 
            PlayerId = _playerId,
            OldHealth = oldHealth, 
            NewHealth = _health 
        });
    }
    
    public async Task<bool> TryMoveToAsync(Vector2 newPosition)
    {
        var moveEvent = new PlayerMoveEvent
        {
            OldPosition = Position,
            NewPosition = newPosition,
            MoveSpeed = MoveSpeed
        };
        
        // 异步触发移动事件
        bool handled = await EventBus.TriggerEventAsync(moveEvent);
        
        // 检查移动是否被取消
        if (moveEvent.IsCanceled)
        {
            GD.Print("移动被取消");
            return false;
        }
        
        // 执行移动
        Position = newPosition;
        return true;
    }
    
    public bool UseItem(Item item)
    {
        var useEvent = new ItemUseEvent
        {
            Item = item,
            Player = this
        };
        
        EventBus.TriggerEvent(useEvent);
        
        // 根据事件结果决定是否使用物品
        return useEvent.Result switch
        {
            EventResult.ALLOW => true,
            EventResult.DENY => false,
            EventResult.DEFAULT => item.DefaultCanUse(this)
        };
    }
}
```

## 📖 详细使用指南

### 优先级系统详解

EventBus 支持两种优先级系统，可以灵活组合使用：

#### 枚举优先级（推荐）

```csharp
[EventBusSubscriber]
public partial class PriorityDemo : Node
{
    // 最高优先级 - 权限验证
    [EventSubscribe(EventPriority.HIGHEST)]
    public void ValidatePermission(ActionEvent evt)
    {
        if (!HasPermission(evt.Action))
        {
            evt.SetCanceled(true);
        }
    }
    
    // 高优先级 - 核心业务逻辑
    [EventSubscribe(EventPriority.HIGH)]
    public void ProcessAction(ActionEvent evt)
    {
        // 执行核心逻辑
    }
    
    // 普通优先级 - UI更新
    [EventSubscribe(EventPriority.NORMAL)]
    public void UpdateUI(ActionEvent evt)
    {
        // 更新界面
    }
    
    // 低优先级 - 音效播放
    [EventSubscribe(EventPriority.LOW)]
    public void PlaySound(ActionEvent evt)
    {
        // 播放音效
    }
    
    // 监控优先级 - 日志记录
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public void LogAction(ActionEvent evt)
    {
        // 记录所有操作，包括被取消的
    }
}
```

#### 数字优先级（细粒度控制）

```csharp
[EventBusSubscriber]
public partial class NumericPriorityDemo : Node
{
    // 在HIGH优先级内，数字越大越先执行
    [EventSubscribe(EventPriority.HIGH)]
    public void FirstHighHandler(SomeEvent evt) { } // NumericPriority = 0
    
    // 自定义数字优先级
    public override void _Ready()
    {
        EventBus.RegisterEvent<SomeEvent>(SecondHighHandler, "SecondHighHandler", GetType(), EventPriority.HIGH);
        
        // 或使用向后兼容的数字注册
        EventBus.RegisterEvent<SomeEvent>(ThirdHandler, priority: 100); // 转换为 HIGHEST
        EventBus.RegisterEvent<SomeEvent>(FourthHandler, priority: 50);  // 转换为 HIGH
    }
    
    private void SecondHighHandler(SomeEvent evt) { }
    private void ThirdHandler(SomeEvent evt) { }
    private void FourthHandler(SomeEvent evt) { }
}
```

### 静态事件处理程序

支持静态方法作为全局事件处理程序：

```csharp
[EventBusSubscriber]
public static class GlobalEventHandlers
{
    // 全局错误处理
    [EventSubscribe(EventPriority.HIGHEST)]
    public static void HandleGlobalError(ErrorEvent evt)
    {
        GD.PrintErr($"全局错误: {evt.Message}");
        LogError(evt);
    }
    
    // 全局性能监控
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public static async Task MonitorPerformance(EventBase evt)
    {
        var processingTime = DateTime.UtcNow - evt.EventTime;
        if (processingTime.TotalMilliseconds > 100)
        {
            GD.PrintErr($"事件处理耗时过长: {evt.GetType().Name} - {processingTime.TotalMilliseconds}ms");
        }
        
        await RecordPerformanceMetrics(evt, processingTime);
    }
    
    // 全局调试日志
    [EventSubscribe(EventPriority.MONITOR)]
    public static void DebugLogger(EventBase evt)
    {
        if (OS.IsDebugBuild())
        {
            GD.Print($"[DEBUG] {evt.GetType().Name}: {evt.GetEventDebugInfo()}");
        }
    }
    
    private static void LogError(ErrorEvent evt)
    {
        // 错误日志逻辑
    }
    
    private static async Task RecordPerformanceMetrics(EventBase evt, TimeSpan processingTime)
    {
        // 性能监控逻辑
    }
}
```

### 手动注册 vs 自动注册

#### 自动注册（推荐用于Godot节点）

```csharp
[EventBusSubscriber]
public partial class PlayerController : CharacterBody2D
{
    // Godot节点会自动注册，无需手动调用

    [EventSubscribe(EventPriority.HIGH)]
    public void OnPlayerInput(InputEvent evt)
    {
        ProcessInput(evt);
    }
    
    // 节点销毁时会自动清理事件处理程序
    private void ProcessInput(InputEvent evt) { }
}
```

#### 手动注册（用于非节点类）

```csharp
public class GameStatistics
{
    public GameStatistics()
    {
        // 手动注册事件处理程序
        EventBus.RegisterEvent<PlayerHealthChangedEvent>(OnHealthChanged, EventPriority.LOW);
        EventBus.RegisterEvent<PlayerLevelUpEvent>(OnLevelUp, EventPriority.NORMAL);
        
        // 异步处理程序
        EventBus.RegisterEvent<GameEndEvent>(OnGameEndAsync, EventPriority.HIGH);
        
        // 向后兼容的数字优先级
        EventBus.RegisterEvent<ScoreChangedEvent>(OnScoreChanged, priority: 10);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        UpdateHealthStatistics(evt);
    }
    
    private void OnLevelUp(PlayerLevelUpEvent evt)
    {
        RecordLevelUpTime(evt);
    }
    
    private async Task OnGameEndAsync(GameEndEvent evt)
    {
        await SaveStatistics();
        await UploadToCloud();
    }
    
    private void OnScoreChanged(ScoreChangedEvent evt)
    {
        TrackHighScore(evt);
    }
    
    // 在对象销毁时清理
    public void Dispose()
    {
        EventBus.UnregisterAllEventsForObject(this);
    }
    
    private void UpdateHealthStatistics(PlayerHealthChangedEvent evt) { }
    private void RecordLevelUpTime(PlayerLevelUpEvent evt) { }
    private async Task SaveStatistics() { }
    private async Task UploadToCloud() { }
    private void TrackHighScore(ScoreChangedEvent evt) { }
}
```

### 事件控制高级功能

#### 可取消事件

```csharp
[Cancelable]
public class PlayerAttackEvent : EventBase
{
    public Player Attacker { get; set; }
    public Vector2 AttackDirection { get; set; }
    public int Damage { get; set; }
}

[EventBusSubscriber]
public partial class CombatSystem : Node
{
    // 高优先级验证攻击
    [EventSubscribe(EventPriority.HIGH)]
    public void ValidateAttack(PlayerAttackEvent evt)
    {
        if (evt.Attacker.IsFrozen || evt.Attacker.Stamina < 10)
        {
            evt.SetCanceled(true);
            GD.Print("攻击被取消 - 玩家状态不允许");
        }
    }
    
    // 普通优先级处理攻击（跳过已取消的攻击）
    [EventSubscribe(EventPriority.NORMAL)]
    public void ProcessAttack(PlayerAttackEvent evt)
    {
        // 这个方法不会处理被取消的攻击
        ExecuteAttack(evt);
    }
    
    // 监控优先级记录所有攻击尝试（包括被取消的）
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public void LogAttackAttempt(PlayerAttackEvent evt)
    {
        string status = evt.IsCanceled ? "已取消" : "已执行";
        GD.Print($"攻击尝试记录: {evt.Attacker.Name} - {status}");
    }
    
    private void ExecuteAttack(PlayerAttackEvent evt) { }
}
```

#### 有结果事件

```csharp
[HasResult]
public class PermissionCheckEvent : EventBase
{
    public string Action { get; set; }
    public Player Player { get; set; }
}

[EventBusSubscriber]
public partial class PermissionSystem : Node
{
    [EventSubscribe(EventPriority.HIGHEST)]
    public void CheckAdminPermission(PermissionCheckEvent evt)
    {
        if (evt.Player.IsAdmin)
        {
            evt.SetResult(EventResult.ALLOW);
            return;
        }
    }
    
    [EventSubscribe(EventPriority.HIGH)]
    public void CheckVIPPermission(PermissionCheckEvent evt)
    {
        // 如果已经有结果，可以选择是否覆盖
        if (evt.Result != EventResult.DEFAULT) return;
        
        if (evt.Player.IsVIP && evt.Action == "SpecialAction")
        {
            evt.SetResult(EventResult.ALLOW);
        }
    }
    
    [EventSubscribe(EventPriority.NORMAL)]
    public void CheckDefaultPermission(PermissionCheckEvent evt)
    {
        if (evt.Result != EventResult.DEFAULT) return;
        
        // 默认拒绝特殊操作
        if (evt.Action == "SpecialAction")
        {
            evt.SetResult(EventResult.DENY);
        }
    }
}

// 使用示例
public bool HasPermission(string action, Player player)
{
    var permissionEvent = new PermissionCheckEvent
    {
        Action = action,
        Player = player
    };
    
    EventBus.TriggerEvent(permissionEvent);
    
    return permissionEvent.Result switch
    {
        EventResult.ALLOW => true,
        EventResult.DENY => false,
        EventResult.DEFAULT => DefaultPermissionCheck(action, player)
    };
}
```

## 🔧 高级功能

### 调试和监控

```csharp
public partial class EventBusDebugger : Node
{
    public override void _Ready()
    {
        // 获取系统统计信息
        int eventTypeCount = EventBus.GetRegisteredEventTypeCount();
        int instanceCount = EventBus.GetRegisteredInstanceCount();
        
        GD.Print($"EventBus 统计:");
        GD.Print($"  已注册事件类型: {eventTypeCount}");
        GD.Print($"  已注册实例: {instanceCount}");
        
        // 获取特定事件的详细信息
        string healthEventInfo = EventBus.GetEventTypeDebugInfo<PlayerHealthChangedEvent>();
        GD.Print($"血量事件详情:\n{healthEventInfo}");
        
        // 获取所有事件统计
        var allStats = EventBus.GetEventStatistics();
        foreach (var stat in allStats)
        {
            GD.Print($"  {stat.Key.Name}: {stat.Value} 个处理程序");
        }
        
        // 检查特定实例是否已注册
        var player = GetNode<Player>("Player");
        if (EventBus.IsInstanceRegistered(player))
        {
            GD.Print("Player 节点已注册到 EventBus");
        }
        
        // 创建测试事件并检查其信息
        var testEvent = new PlayerHealthChangedEvent 
        { 
            PlayerId = 1, 
            OldHealth = 100, 
            NewHealth = 80 
        };
        
        // 触发事件前获取订阅者信息
        var subscribers = EventBus.GetEventSubscribers<PlayerHealthChangedEvent>();
        GD.Print($"血量事件订阅者数量: {subscribers.Length}");
        
        foreach (var subscriber in subscribers)
        {
            GD.Print($"  订阅者: {subscriber.DisplayDeclaringType.Name}.{subscriber.DisplayMethodName}");
            GD.Print($"    优先级: {subscriber.Priority}({subscriber.NumericPriority})");
            GD.Print($"    接收已取消: {subscriber.ReceiveCanceled}");
        }
        
        // 触发事件并检查处理信息
        EventBus.TriggerEvent(testEvent);
        
        // 获取事件的完整调试信息
        string eventDebugInfo = testEvent.GetEventDebugInfo();
        GD.Print($"事件处理详情:\n{eventDebugInfo}");
    }
}
```

### 性能监控和优化

```csharp
[EventBusSubscriber]
public static class PerformanceMonitor
{
    private static readonly Dictionary<Type, List<TimeSpan>> _processingTimes = new();
    private static readonly object _lockObject = new();
    
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public static void MonitorEventPerformance(EventBase evt)
    {
        var processingTime = DateTime.UtcNow - evt.EventTime;
        var eventType = evt.GetType();
        
        lock (_lockObject)
        {
            if (!_processingTimes.ContainsKey(eventType))
            {
                _processingTimes[eventType] = new List<TimeSpan>();
            }
            
            _processingTimes[eventType].Add(processingTime);
            
            // 保持最近100次记录
            if (_processingTimes[eventType].Count > 100)
            {
                _processingTimes[eventType].RemoveAt(0);
            }
        }
        
        // 警告慢事件
        if (processingTime.TotalMilliseconds > 50)
        {
            GD.PrintErr($"[PERFORMANCE] 慢事件检测: {eventType.Name} - {processingTime.TotalMilliseconds:F2}ms");
            
            // 输出当前处理程序信息
            if (evt.CurrentHandler != null)
            {
                GD.PrintErr($"  当前处理程序: {evt.CurrentHandler.DisplayDeclaringType.Name}.{evt.CurrentHandler.DisplayMethodName}");
            }
        }
    }
    
    public static void PrintPerformanceReport()
    {
        lock (_lockObject)
        {
            GD.Print("=== EventBus 性能报告 ===");
            
            foreach (var kvp in _processingTimes)
            {
                var eventType = kvp.Key;
                var times = kvp.Value;
                
                if (times.Count == 0) continue;
                
                var avgTime = times.Average(t => t.TotalMilliseconds);
                var maxTime = times.Max(t => t.TotalMilliseconds);
                var minTime = times.Min(t => t.TotalMilliseconds);
                
                GD.Print($"事件: {eventType.Name}");
                GD.Print($"  样本数: {times.Count}");
                GD.Print($"  平均耗时: {avgTime:F2}ms");
                GD.Print($"  最大耗时: {maxTime:F2}ms");
                GD.Print($"  最小耗时: {minTime:F2}ms");
                GD.Print($"  订阅者数: {EventBus.GetEventSubscribers(eventType).Length}");
                GD.Print("");
            }
        }
    }
}
```

### 自定义事件处理程序工厂

```csharp
public static class CustomEventHandlerFactory
{
    // 创建条件处理程序
    public static void RegisterConditionalHandler<TEvent>(
        Func<TEvent, bool> condition,
        Action<TEvent> handler,
        EventPriority priority = EventPriority.NORMAL) where TEvent : EventBase
    {
        EventBus.RegisterEvent<TEvent>(evt =>
        {
            if (condition(evt))
            {
                handler(evt);
            }
        }, priority);
    }
    
    // 创建一次性处理程序
    public static void RegisterOnceHandler<TEvent>(
        Action<TEvent> handler,
        EventPriority priority = EventPriority.NORMAL) where TEvent : EventBase
    {
        Action<TEvent> onceHandler = null;
        onceHandler = evt =>
        {
            handler(evt);
            EventBus.UnregisterEvent(onceHandler);
        };
        
        EventBus.RegisterEvent(onceHandler, priority);
    }
    
    // 创建延迟处理程序
    public static void RegisterDelayedHandler<TEvent>(
        Action<TEvent> handler,
        TimeSpan delay,
        EventPriority priority = EventPriority.NORMAL) where TEvent : EventBase
    {
        EventBus.RegisterEvent<TEvent>(async evt =>
        {
            await Task.Delay(delay);
            handler(evt);
        }, priority);
    }
}

// 使用示例
public void SetupCustomHandlers()
{
    // 条件处理程序 - 只处理特定玩家的事件
    CustomEventHandlerFactory.RegisterConditionalHandler<PlayerHealthChangedEvent>(
        evt => evt.PlayerId == 1,
        evt => GD.Print($"主角血量变化: {evt.NewHealth}"),
        EventPriority.HIGH
    );
    
    // 一次性处理程序 - 只处理第一次游戏开始
    CustomEventHandlerFactory.RegisterOnceHandler<GameStartEvent>(
        evt => GD.Print("欢迎第一次游戏!"),
        EventPriority.NORMAL
    );
    
    // 延迟处理程序 - 延迟1秒显示伤害数字
    CustomEventHandlerFactory.RegisterDelayedHandler<PlayerHealthChangedEvent>(
        evt => ShowDamageNumber(evt.OldHealth - evt.NewHealth),
        TimeSpan.FromSeconds(1),
        EventPriority.LOW
    );
}
```

## 📋 完整示例：RPG战斗系统

```csharp
// === 事件定义 ===
[Cancelable, HasResult]
public class CombatActionEvent : EventBase
{
    public Character Attacker { get; set; }
    public Character Target { get; set; }
    public string ActionType { get; set; }
    public int BaseDamage { get; set; }
    public int FinalDamage { get; set; }
}

[Cancelable]
public class CharacterDeathEvent : EventBase
{
    public Character Character { get; set; }
    public Character Killer { get; set; }
}

public class ExperienceGainedEvent : EventBase
{
    public Character Character { get; set; }
    public int Experience { get; set; }
    public string Source { get; set; }
}

// === 战斗管理器 ===
[EventBusSubscriber]
public partial class CombatManager : Node
{
    // 最高优先级 - 验证攻击合法性
    [EventSubscribe(EventPriority.HIGHEST)]
    public void ValidateCombatAction(CombatActionEvent evt)
    {
        if (evt.Attacker.IsDead || evt.Target.IsDead)
        {
            evt.SetCanceled(true);
            evt.SetResult(EventResult.DENY);
            return;
        }
        
        if (!evt.Attacker.CanPerformAction(evt.ActionType))
        {
            evt.SetCanceled(true);
            evt.SetResult(EventResult.DENY);
            return;
        }
        
        evt.SetResult(EventResult.ALLOW);
    }
    
    // 高优先级 - 计算伤害
    [EventSubscribe(EventPriority.HIGH)]
    public void CalculateDamage(CombatActionEvent evt)
    {
        if (evt.Result == EventResult.DENY) return;
        
        int damage = evt.BaseDamage;
        
        // 应用攻击者属性
        damage = (int)(damage * evt.Attacker.AttackMultiplier);
        
        // 应用目标防御
        damage = Mathf.Max(1, damage - evt.Target.Defense);
        
        // 暴击检查
        if (UnityEngine.Random.Range(0f, 1f) < evt.Attacker.CriticalChance)
        {
            damage = (int)(damage * evt.Attacker.CriticalMultiplier);
            GD.Print($"暴击! 伤害: {damage}");
        }
        
        evt.FinalDamage = damage;
    }
    
    // 普通优先级 - 应用伤害
    [EventSubscribe(EventPriority.NORMAL)]
    public async Task ApplyDamage(CombatActionEvent evt)
    {
        if (evt.Result == EventResult.DENY) return;
        
        int oldHealth = evt.Target.Health;
        evt.Target.Health = Mathf.Max(0, evt.Target.Health - evt.FinalDamage);
        
        GD.Print($"{evt.Attacker.Name} 对 {evt.Target.Name} 造成 {evt.FinalDamage} 伤害");
        
        // 触发血量变化事件
        EventBus.TriggerEvent(new PlayerHealthChangedEvent
        {
            PlayerId = evt.Target.Id,
            OldHealth = oldHealth,
            NewHealth = evt.Target.Health
        });
        
        // 检查死亡
        if (evt.Target.Health <= 0)
        {
            var deathEvent = new CharacterDeathEvent
            {
                Character = evt.Target,
                Killer = evt.Attacker
            };
            
            await EventBus.TriggerEventAsync(deathEvent);
        }
    }
    
    // 监控优先级 - 记录战斗日志
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public void LogCombatAction(CombatActionEvent evt)
    {
        string status = evt.IsCanceled ? "失败" : "成功";
        string logEntry = $"[战斗日志] {evt.Attacker.Name} -> {evt.Target.Name} " +
                         $"({evt.ActionType}): {status} - 伤害: {evt.FinalDamage}";
        
        GD.Print(logEntry);
        SaveCombatLog(logEntry);
    }
    
    private void SaveCombatLog(string logEntry) { }
}

// === 经验系统 ===
[EventBusSubscriber]
public partial class ExperienceSystem : Node
{
    [EventSubscribe(EventPriority.HIGH)]
    public void OnCharacterDeath(CharacterDeathEvent evt)
    {
        if (evt.Killer != null && !evt.Killer.IsDead)
        {
            int expGain = CalculateExperienceGain(evt.Character, evt.Killer);
            
            EventBus.TriggerEvent(new ExperienceGainedEvent
            {
                Character = evt.Killer,
                Experience = expGain,
                Source = $"击败 {evt.Character.Name}"
            });
        }
    }
    
    [EventSubscribe(EventPriority.NORMAL)]
    public void OnExperienceGained(ExperienceGainedEvent evt)
    {
        evt.Character.Experience += evt.Experience;
        GD.Print($"{evt.Character.Name} 获得 {evt.Experience} 经验 ({evt.Source})");
        
        CheckLevelUp(evt.Character);
    }
    
    private int CalculateExperienceGain(Character defeated, Character killer)
    {
        int baseExp = defeated.Level * 100;
        float levelDiff = defeated.Level - killer.Level;
        float multiplier = Mathf.Max(0.1f, 1.0f + (levelDiff * 0.1f));
        
        return (int)(baseExp * multiplier);
    }
    
    private void CheckLevelUp(Character character)
    {
        int requiredExp = character.Level * 1000;
        
        if (character.Experience >= requiredExp)
        {
            character.Level++;
            character.Experience -= requiredExp;
            
            EventBus.TriggerEvent(new PlayerLevelUpEvent
            {
                OldLevel = character.Level - 1,
                NewLevel = character.Level,
                ExperienceGained = requiredExp
            });
        }
    }
}

// === UI系统 ===
[EventBusSubscriber]
public partial class CombatUI : Control
{
    private Label _combatLog;
    private ProgressBar _playerHealth;
    private Label _damageNumbers;
    
    public override void _Ready()
    {
        _combatLog = GetNode<Label>("CombatLog");
        _playerHealth = GetNode<ProgressBar>("PlayerHealth");
        _damageNumbers = GetNode<Label>("DamageNumbers");
    }
    
    [EventSubscribe(EventPriority.LOW)]
    public async Task ShowDamageNumber(CombatActionEvent evt)
    {
        if (evt.IsCanceled || evt.FinalDamage <= 0) return;
        
        var damageLabel = new Label();
        damageLabel.Text = $"-{evt.FinalDamage}";
        damageLabel.Modulate = Colors.Red;
        
        // 添加到场景并播放动画
        AddChild(damageLabel);
        
        var tween = CreateTween();
        tween.Parallel().TweenProperty(damageLabel, "position", 
            damageLabel.Position + Vector2.Up * 50, 1.0f);
        tween.Parallel().TweenProperty(damageLabel, "modulate:a", 0.0f, 1.0f);
        
        await ToSignal(tween, Tween.SignalName.Finished);
        damageLabel.QueueFree();
    }
    
    [EventSubscribe(EventPriority.LOW)]
    public void UpdateHealthBar(PlayerHealthChangedEvent evt)
    {
        if (evt.PlayerId == 1) // 主角
        {
            _playerHealth.Value = evt.HealthPercentage * 100;
            
            // 血量低时闪烁
            if (evt.HealthPercentage < 0.2f)
            {
                var tween = CreateTween();
                tween.SetLoops();
                tween.TweenProperty(_playerHealth, "modulate", Colors.Red, 0.5f);
                tween.TweenProperty(_playerHealth, "modulate", Colors.White, 0.5f);
            }
        }
    }
    
    [EventSubscribe(EventPriority.LOW)]
    public void UpdateCombatLog(CombatActionEvent evt)
    {
        string message = evt.IsCanceled 
            ? $"{evt.Attacker.Name} 的攻击失败了！"
            : $"{evt.Attacker.Name} 对 {evt.Target.Name} 造成了 {evt.FinalDamage} 伤害！";
            
        _combatLog.Text = message;
        
        // 淡出效果
        var tween = CreateTween();
        tween.TweenDelay(2.0f);
        tween.TweenProperty(_combatLog, "modulate:a", 0.0f, 1.0f);
        tween.TweenCallback(Callable.From(() => _combatLog.Text = ""));
        tween.TweenProperty(_combatLog, "modulate:a", 1.0f, 0.1f);
    }
}

// === 角色类 ===
public partial class Character : CharacterBody2D
{
    [Export] public string CharacterName { get; set; } = "";
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int Defense { get; set; } = 10;
    [Export] public float AttackMultiplier { get; set; } = 1.0f;
    [Export] public float CriticalChance { get; set; } = 0.1f;
    [Export] public float CriticalMultiplier { get; set; } = 2.0f;
    
    public int Id { get; set; }
    public int Health { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public bool IsDead => Health <= 0;
    public string Name => CharacterName;
    
    public override void _Ready()
    {
        Health = MaxHealth;
        Id = GetInstanceId().AsInt32();
    }
    
    public bool CanPerformAction(string actionType)
    {
        if (IsDead) return false;
        
        return actionType switch
        {
            "Attack" => true,
            "MagicAttack" => Level >= 3,
            "SpecialAttack" => Level >= 5,
            _ => false
        };
    }
    
    public async Task<bool> AttackAsync(Character target, string actionType = "Attack")
    {
        var combatEvent = new CombatActionEvent
        {
            Attacker = this,
            Target = target,
            ActionType = actionType,
            BaseDamage = CalculateBaseDamage(actionType)
        };
        
        bool handled = await EventBus.TriggerEventAsync(combatEvent);
        
        return handled && combatEvent.Result == EventResult.ALLOW;
    }
    
    private int CalculateBaseDamage(string actionType)
    {
        return actionType switch
        {
            "Attack" => 20 + Level * 5,
            "MagicAttack" => 30 + Level * 8,
            "SpecialAttack" => 50 + Level * 10,
            _ => 10
        };
    }
}
```

## 🔨 最佳实践

### 1. 事件设计原则

```csharp
// ✅ 好的事件设计
[Cancelable, HasResult]
public class ItemPurchaseEvent : EventBase
{
    // 不可变属性，避免处理程序间的副作用
    public string ItemId { get; }
    public Player Player { get; }
    public int Price { get; }
    public DateTime PurchaseTime { get; }
    
    public ItemPurchaseEvent(string itemId, Player player, int price)
    {
        ItemId = itemId;
        Player = player;
        Price = price;
        PurchaseTime = DateTime.UtcNow;
    }
}

// ❌ 避免的设计
public class BadEvent : EventBase
{
    public object Data { get; set; } // 失去类型安全
    public string Type { get; set; } // 应该使用具体的事件类型
    public Dictionary<string, object> Properties { get; set; } // 过于通用
}
```

### 2. 优先级分配策略

```csharp
// 推荐的优先级分配
[EventBusSubscriber]
public partial class BestPracticeHandlers : Node
{
    // HIGHEST (0) - 权限验证、安全检查
    [EventSubscribe(EventPriority.HIGHEST)]
    public void ValidatePermissions(ActionEvent evt) { }
    
    // HIGH (1) - 核心业务逻辑
    [EventSubscribe(EventPriority.HIGH)]
    public void ProcessBusinessLogic(ActionEvent evt) { }
    
    // NORMAL (2) - 一般处理、默认行为
    [EventSubscribe(EventPriority.NORMAL)]
    public void HandleNormalOperation(ActionEvent evt) { }
    
    // LOW (3) - UI更新、音效、动画
    [EventSubscribe(EventPriority.LOW)]
    public void UpdateUI(ActionEvent evt) { }
    
    // LOWEST (4) - 清理、收尾工作
    [EventSubscribe(EventPriority.LOWEST)]
    public void Cleanup(ActionEvent evt) { }
    
    // MONITOR (5) - 日志、统计、监控
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public void LogAndMonitor(ActionEvent evt) { }
}
```

### 3. 错误处理策略

```csharp
[EventBusSubscriber]
public partial class ErrorHandlingExample : Node
{
    [EventSubscribe(EventPriority.NORMAL)]
    public async Task RiskyOperation(SomeEvent evt)
    {
        try
        {
            await PerformRiskyTask(evt);
        }
        catch (SpecificException ex)
        {
            // 处理特定异常
            GD.PrintErr($"特定错误处理: {ex.Message}");
            
            // 可以触发错误事件让其他系统处理
            EventBus.TriggerEvent(new ErrorEvent 
            { 
                OriginalEvent = evt, 
                Exception = ex,
                Severity = ErrorSeverity.High
            });
        }
        catch (Exception ex)
        {
            // 处理通用异常
            GD.PrintErr($"未知错误: {ex.Message}");
            
            // 记录详细错误信息
            LogError(evt, ex);
        }
    }
    
    private async Task PerformRiskyTask(SomeEvent evt) { }
    private void LogError(EventBase evt, Exception ex) { }
}
```

### 4. 性能优化建议

```csharp
// 优化的事件处理程序
[EventBusSubscriber]
public partial class PerformanceOptimizedHandler : Node
{
    // 缓存经常使用的组件引用
    private HealthBar _healthBar;
    private AudioStreamPlayer _audioPlayer;
    
    public override void _Ready()
    {
        _healthBar = GetNode<HealthBar>("UI/HealthBar");
        _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
    }
    
    [EventSubscribe(EventPriority.LOW)]
    public void OptimizedUIUpdate(PlayerHealthChangedEvent evt)
    {
        // 避免频繁的节点查找
        _healthBar.UpdateHealth(evt.HealthPercentage);
        
        // 避免不必要的计算
        if (evt.NewHealth < evt.OldHealth) // 只在受伤时播放音效
        {
            _audioPlayer.Play();
        }
    }
    
    // 对于CPU密集型操作，使用异步处理
    [EventSubscribe(EventPriority.LOW)]
    public async Task CpuIntensiveOperation(ComplexEvent evt)
    {
        // 将CPU密集型操作移到后台线程
        await Task.Run(() => ProcessComplexCalculation(evt));
        
        // 在主线程更新UI
        await Task.Delay(1); // 确保返回主线程
        UpdateUIWithResults();
    }
    
    private void ProcessComplexCalculation(ComplexEvent evt) { }
    private void UpdateUIWithResults() { }
}
```

## ⚠️ 注意事项和故障排除

### 常见问题

1. **事件没有被触发**
```csharp
// 检查列表
// 1. 类是否标记了 [EventBusSubscriber]
// 2. 方法是否标记了 [EventSubscribe]
// 3. 方法签名是否正确
// 4. 节点是否在场景树中

// 调试工具
public void DebugeventRegistration()
{
    var node = GetNode<SomeNode>("SomeNode");
    
    if (!EventBus.IsInstanceRegistered(node))
    {
        GD.PrintErr("节点未注册到EventBus");
        
        // 检查是否有 [EventBusSubscriber] 特性
        var hasAttribute = node.GetType().GetCustomAttribute<EventBusSubscriberAttribute>() != null;
        GD.Print($"有EventBusSubscriber特性: {hasAttribute}");
        
        // 手动注册
        EventBus.AutoRegister(node);
    }
}
```

2. **内存泄漏检测**
```csharp
public void CheckMemoryLeaks()
{
    int eventTypes = EventBus.GetRegisteredEventTypeCount();
    int instances = EventBus.GetRegisteredInstanceCount();
    
    GD.Print($"注册统计 - 事件类型: {eventTypes}, 实例: {instances}");
    
    if (instances > 1000)
    {
        GD.PrintErr("可能存在内存泄漏 - 实例数过多");
        
        // 获取详细统计
        var stats = EventBus.GetEventStatistics();
        foreach (var stat in stats.OrderByDescending(x => x.Value))
        {
            GD.Print($"  {stat.Key.Name}: {stat.Value} 个处理程序");
        }
    }
}
```

3. **性能问题诊断**
```csharp
[EventBusSubscriber]
public static class PerformanceDiagnostics
{
    [EventSubscribe(EventPriority.MONITOR)]
    public static void DiagnoseSlowEvents(EventBase evt)
    {
        var processingTime = DateTime.UtcNow - evt.EventTime;
        
        if (processingTime.TotalMilliseconds > 16) // 超过一帧的时间
        {
            GD.PrintErr($"慢事件检测: {evt.GetType().Name} - {processingTime.TotalMilliseconds:F2}ms");
            
            // 分析订阅者数量
            var subscribers = evt.GetSubscribers();
            GD.PrintErr($"  订阅者数量: {subscribers.Length}");
            
            foreach (var subscriber in subscribers)
            {
                GD.PrintErr($"    {subscriber.DisplayDeclaringType.Name}.{subscriber.DisplayMethodName}");
            }
        }
    }
}
```

### 调试工具

```csharp
// 全局事件调试器
[EventBusSubscriber]
public static class GlobalEventDebugger
{
    private static bool _debugEnabled = false;
    
    public static void EnableDebug(bool enabled)
    {
        _debugEnabled = enabled;
    }
    
    [EventSubscribe(EventPriority.MONITOR, receiveCanceled: true)]
    public static void DebugAllEvents(EventBase evt)
    {
        if (!_debugEnabled || !OS.IsDebugBuild()) return;
        
        var eventType = evt.GetType().Name;
        var eventId = evt.EventId.ToString()[..8]; // 前8位
        var status = evt.IsCanceled ? "[CANCELED]" : "[SUCCESS]";
        var result = evt.HasResult ? $" Result:{evt.Result}" : "";
        
        GD.Print($"[EVENT] {eventType} {eventId} {status}{result}");
        
        if (evt.CurrentHandler != null)
        {
            var handler = evt.CurrentHandler;
            GD.Print($"  Handler: {handler.DisplayDeclaringType.Name}.{handler.DisplayMethodName}");
            GD.Print($"  Priority: {handler.Priority}({handler.NumericPriority})");
        }
    }
}

// 在游戏开始时启用调试
public override void _Ready()
{
    GlobalEventDebugger.EnableDebug(true);
}
```