using System.Text.Json.Serialization;

namespace CommonSDK.ModGateway;

/// <summary>
/// 模组元数据类
/// <para>用于存储模组的基本信息和配置</para>
/// </summary>
public class ModMetadata
{
    /// <summary>
    /// 模组唯一标识符
    /// <para>用于在系统中唯一标识此模组</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 模组ID（兼容性属性）
    /// </summary>
    public string ModId 
    { 
        get => Id; 
        set => Id = value; 
    }
    
    /// <summary>
    /// 模组名称
    /// <para>用户可见的模组名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 模组版本号
    /// <para>遵循语义化版本规范，默认为 "1.0.0"</para>
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// 🎯 模组作者数组（主属性）
    /// <para>可以包含多个作者，默认为空数组</para>
    /// </summary>
    public string[] Authors { get; set; } = [];
    
    /// <summary>
    /// 🎯 模组作者（兼容性属性 - JSON 序列化时使用 Author）
    /// </summary>
    [JsonPropertyName("Author")]
    public string[] Author 
    { 
        get => Authors; 
        set => Authors = value; 
    }
    
    /// <summary>
    /// 模组描述
    /// <para>简要说明模组的功能和用途，默认为空字符串</para>
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 模组加载顺序
    /// <para>用于定义模组加载的优先级，默认为0</para>
    /// </summary>
    public int LoadOrder { get; set; }
    
    /// <summary>
    /// 模组依赖项
    /// <para>列出此模组所依赖的其他模组，默认为空数组</para>
    /// </summary>
    public string[] Dependencies { get; set; } = [];
    
    /// <summary>
    /// 模组目录
    /// <para>用于存储模组文件的目录路径，标记为JsonIgnore以避免序列化</para>
    /// </summary>
    [JsonIgnore]
    public string Directory { get; set; } = string.Empty;
}