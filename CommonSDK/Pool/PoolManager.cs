using CommonSDK.Logger;
using Godot;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // 可能返回 null 引用。

namespace CommonSDK.Pool;

/// <summary>
/// 对象池管理器
/// <para>用于管理和复用游戏对象，减少内存分配和回收的开销</para>
/// </summary>
public partial class PoolManager : Singleton<PoolManager>
{
    private readonly LogHelper _logger = new("Pool");
    private readonly Dictionary<string, Queue<Node>> _pools = new();
    private readonly Dictionary<string, PackedScene> _prefabs = new();
    
    /// <summary>
    /// 初始化对象池
    /// <para>创建一个新的对象池并填充初始对象</para>
    /// </summary>
    /// <param name="poolKey">对象池的唯一标识符</param>
    /// <param name="prefab">用于生成对象的预制体</param>
    /// <param name="initialSize">对象池的初始大小</param>
    public void InitializePool(string poolKey, PackedScene prefab, int initialSize)
    {
        if (_pools.ContainsKey(poolKey))
        {
            _logger.LogWarn($"Pool {poolKey} 已存在，跳过初始化");
            return;
        }

        var queue = new Queue<Node>();
        for (var i = 0; i < initialSize; i++)
        {
            var obj = prefab.Instantiate<Node>();
            obj.ProcessMode = ProcessModeEnum.Disabled;
            queue.Enqueue(obj);
        }

        _pools.Add(poolKey, queue);
        _prefabs.Add(poolKey, prefab);
        _logger.LogInfo($"Pool {poolKey} 初始化完成，大小: {initialSize}");
    }
    
    /// <summary>
    /// 从对象池中生成一个对象
    /// <para>如果对象池为空，则创建一个新的对象</para>
    /// </summary>
    /// <param name="poolKey">对象池的唯一标识符</param>
    /// <param name="parent">可选的父节点，生成的对象将被添加为其子节点</param>
    /// <returns>生成的对象，如果失败则返回null</returns>
    public Node Spawn(string poolKey, Node? parent = null)
    {
        if (!_pools.TryGetValue(poolKey, out var queue))
        {
            _logger.LogError($"对象池 {poolKey} 未初始化");
            return null;
        }

        var obj = queue.Count > 0 ? queue.Dequeue() : CreateNewObject(poolKey);

        if (obj == null)
        {
            _logger.LogError($"从对象池 {poolKey} 获取对象失败");
            return null;
        }

        obj.ProcessMode = ProcessModeEnum.Inherit;

        parent?.AddChild(obj);

        return obj;
    }
    
    /// <summary>
    /// 回收对象并将其放回对象池
    /// </summary>
    /// <param name="poolKey">对象池的唯一标识符</param>
    /// <param name="obj">要回收的对象</param>
    public void Despawn(string poolKey, Node obj)
    {
        if (!_pools.ContainsKey(poolKey))
        {
            _logger.LogError($"对象池 {poolKey} 不存在，无法回收");
            return;
        }

        obj.ProcessMode = Node.ProcessModeEnum.Disabled;
        obj.GetParent()?.RemoveChild(obj);
        _pools[poolKey].Enqueue(obj);
    }

    /// <summary>
    /// 创建新的对象
    /// <para>如果对象池中没有可用对象，则使用预制体生成新对象</para>
    /// </summary>
    /// <param name="poolKey">对象池的唯一标识符</param>
    /// <returns>新创建的对象，如果失败则返回null</returns>
    private Node CreateNewObject(string poolKey)
    {
        if (_prefabs.TryGetValue(poolKey, out var prefab))
        {
            var obj = prefab.Instantiate<Node>();
            obj.ProcessMode = Node.ProcessModeEnum.Disabled;
            return obj;
        }

        _logger.LogError($"对象池 {poolKey} 无对应预制体");
        return null;
    }

    /// <summary>
    /// 单例初始化完成时调用
    /// <para>用于记录初始化日志</para>
    /// </summary>
    protected override void OnSingletonReady()
    {
        _logger.LogInfo("PoolManager 单例初始化完成");
    }
}