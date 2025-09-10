namespace CommonSDK.ModGateway;

/// <summary>
/// 模组接口
/// <para>定义模组的基本生命周期方法</para>
/// <para>所有游戏模组必须实现此接口以便被模组系统加载和管理</para>
/// </summary>
public interface IMod
{
    /// <summary>
    /// 异步初始化模组
    /// <para>在模组加载时调用，用于初始化资源和设置</para>
    /// <para>此阶段应避免与其他模组交互，因为它们可能尚未初始化</para>
    /// </summary>
    Task InitAsync();
    
    /// <summary>
    /// 异步启动模组
    /// <para>在所有模组初始化完成后调用</para>
    /// <para>此阶段可以安全地与其他模组交互</para>
    /// <para>⚠️ 注意：AddChild 等 Godot API 调用需要在主线程执行</para>
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// 模组循环更新
    /// <para>在游戏主循环中定期调用</para>
    /// <para>用于处理模组的持续性逻辑和更新</para>
    /// </summary>
    void Loop(double delta);

    /// <summary>
    /// 模组物理循环更新
    /// <para>在游戏主循环中定期调用</para>
    /// <para>用于处理模组的物理相关逻辑和更新</para>
    /// </summary>
    void PhysicsLoop(double delta);
}