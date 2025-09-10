using CommonSDK.Logger;
using Godot;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // 可能返回 null 引用。

namespace CommonSDK.Utils;

/// <summary>
/// Node查找扩展方法
/// <para>为Node类提供类似Unity的查找功能</para>
/// </summary>
public static class FindUtils
{
    private static readonly LogHelper Logger = new("FindUtils");

    #region FindObjectOfType 系列方法
    
    /// <summary>
    /// 查找场景树中第一个指定类型的对象
    /// </summary>
    /// <typeparam name="T">要查找的对象类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的对象，如果没有则返回null</returns>
    public static T FindObjectOfType<T>(this Node node, bool includeInactive = false) where T : Node
    {
        var root = GetSceneRoot(node);
        return root == null ? null : FindObjectOfTypeRecursive<T>(root, includeInactive);
    }

    /// <summary>
    /// 查找场景树中第一个指定类型的对象
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="type">要查找的对象类型</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的对象，如果没有则返回null</returns>
    public static Node FindObjectOfType(this Node node, Type type, bool includeInactive = false)
    {
        var root = GetSceneRoot(node);
        return root == null ? null : FindObjectOfTypeRecursive(root, type, includeInactive);
    }

    /// <summary>
    /// 查找场景树中所有指定类型的对象
    /// </summary>
    /// <typeparam name="T">要查找的对象类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的对象数组</returns>
    public static T[] FindObjectsOfType<T>(this Node node, bool includeInactive = false) where T : Node
    {
        var root = GetSceneRoot(node);
        if (root == null) return new T[0];

        var results = new List<T>();
        FindObjectsOfTypeRecursive<T>(root, results, includeInactive);
        return results.ToArray();
    }

    /// <summary>
    /// 查找场景树中所有指定类型的对象
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="type">要查找的对象类型</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的对象数组</returns>
    public static Node[] FindObjectsOfType(this Node node, Type type, bool includeInactive = false)
    {
        var root = GetSceneRoot(node);
        if (root == null) return new Node[0];

        var results = new List<Node>();
        FindObjectsOfTypeRecursive(root, type, results, includeInactive);
        return results.ToArray();
    }

    #endregion

    #region 查找子节点方法 (类似Unity的Transform.Find)

    /// <summary>
    /// 在当前节点及其子节点中查找第一个指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="node">起始查找节点</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的组件，如果没有则返回null</returns>
    public static T GetComponentInChildren<T>(this Node node, bool includeInactive = false) where T : Node
    {
        return node == null ? null : FindObjectOfTypeRecursive<T>(node, includeInactive);
    }

    /// <summary>
    /// 在当前节点及其子节点中查找所有指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="node">起始查找节点</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的组件数组</returns>
    public static T[] GetComponentsInChildren<T>(this Node node, bool includeInactive = false) where T : Node
    {
        if (node == null) return new T[0];

        var results = new List<T>();
        FindObjectsOfTypeRecursive<T>(node, results, includeInactive);
        return results.ToArray();
    }

    /// <summary>
    /// 在当前节点的父级及其祖先中查找第一个指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="node">起始查找节点</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的组件，如果没有则返回null</returns>
    public static T GetComponentInParent<T>(this Node node, bool includeInactive = false) where T : Node
    {
        var current = node;
        while (current != null)
        {
            if ((!includeInactive && !IsNodeActive(current)) == false)
            {
                if (current is T result)
                    return result;
                
                if (HasScriptOfType<T>(current))
                    return current as T;
            }
            current = current.GetParent();
        }
        return null;
    }

    /// <summary>
    /// 在当前节点的父级及其祖先中查找所有指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="node">起始查找节点</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的组件数组</returns>
    public static T[] GetComponentsInParent<T>(this Node node, bool includeInactive = false) where T : Node
    {
        var results = new List<T>();
        var current = node;
        
        while (current != null)
        {
            if (includeInactive || IsNodeActive(current))
            {
                if (current is T directResult)
                {
                    results.Add(directResult);
                }
                else if (HasScriptOfType<T>(current))
                {
                    if (current is T scriptResult)
                    {
                        results.Add(scriptResult);
                    }
                }
            }
            current = current.GetParent();
        }
        
        return results.ToArray();
    }

    /// <summary>
    /// 获取当前节点上的指定类型组件（不查找子节点）
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="node">目标节点</param>
    /// <returns>找到的组件，如果没有则返回null</returns>
    public static T GetComponent<T>(this Node node) where T : Node
    {
        if (node == null) return null;
        
        if (node is T directResult)
            return directResult;
            
        if (HasScriptOfType<T>(node))
            return node as T;
            
        return null;
    }

    #endregion

    #region 按名称查找方法

    /// <summary>
    /// 在当前节点的直接子节点中按名称查找
    /// </summary>
    /// <param name="node">父节点</param>
    /// <param name="name">子节点名称</param>
    /// <returns>找到的子节点，如果没有则返回null</returns>
    public static Node Find(this Node node, string name)
    {
        if (node == null || string.IsNullOrEmpty(name)) return null;

        foreach (var child in node.GetChildren())
        {
            if (child.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }

    /// <summary>
    /// 在当前节点及其所有子节点中递归查找指定名称的节点
    /// </summary>
    /// <param name="node">起始节点</param>
    /// <param name="name">要查找的节点名称</param>
    /// <param name="exactMatch">是否进行精确匹配</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的节点，如果没有则返回null</returns>
    public static Node FindInChildren(this Node node, string name, bool exactMatch = true, bool includeInactive = false)
    {
        return node == null ? null : FindByNameRecursive(node, name, exactMatch, includeInactive);
    }

    /// <summary>
    /// 在当前节点及其所有子节点中递归查找所有指定名称的节点
    /// </summary>
    /// <param name="node">起始节点</param>
    /// <param name="name">要查找的节点名称</param>
    /// <param name="exactMatch">是否进行精确匹配</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>找到的节点数组</returns>
    public static Node[] FindAllInChildren(this Node node, string name, bool exactMatch = true, bool includeInactive = false)
    {
        if (node == null) return new Node[0];

        var results = new List<Node>();
        FindAllByNameRecursive(node, name, exactMatch, includeInactive, results);
        return results.ToArray();
    }

    #endregion

    #region 组查找方法

    /// <summary>
    /// 根据组名查找第一个节点
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="group">组名</param>
    /// <returns>找到的节点，如果没有则返回null</returns>
    public static Node FindWithGroup(this Node node, string group)
    {
        var sceneTree = node.GetTree();
        return sceneTree?.GetFirstNodeInGroup(group);
    }

    /// <summary>
    /// 根据组名查找第一个指定类型的节点
    /// </summary>
    /// <typeparam name="T">要查找的节点类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="group">组名</param>
    /// <returns>找到的节点，如果没有则返回null</returns>
    public static T FindWithGroup<T>(this Node node, string group) where T : Node
    {
        return node.FindWithGroup(group) as T;
    }

    /// <summary>
    /// 根据组名查找所有节点
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="group">组名</param>
    /// <returns>找到的节点数组</returns>
    public static Node[] FindObjectsWithGroup(this Node node, string group)
    {
        var sceneTree = node.GetTree();
        var nodes = sceneTree?.GetNodesInGroup(group);
        return nodes?.Cast<Node>().ToArray() ?? new Node[0];
    }

    /// <summary>
    /// 根据组名查找所有指定类型的节点
    /// </summary>
    /// <typeparam name="T">要查找的节点类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="group">组名</param>
    /// <returns>找到的节点数组</returns>
    public static T[] FindObjectsWithGroup<T>(this Node node, string group) where T : Node
    {
        var nodes = node.FindObjectsWithGroup(group);
        return nodes.OfType<T>().ToArray();
    }

    #endregion

    #region 便捷查询方法

    /// <summary>
    /// 检查是否存在指定类型的对象
    /// </summary>
    /// <typeparam name="T">要检查的对象类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>如果存在则返回true，否则返回false</returns>
    public static bool HasObjectOfType<T>(this Node node, bool includeInactive = false) where T : Node
    {
        return node.FindObjectOfType<T>(includeInactive) != null;
    }

    /// <summary>
    /// 计算指定类型的对象数量
    /// </summary>
    /// <typeparam name="T">要计算的对象类型</typeparam>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <param name="includeInactive">是否包含未激活的节点</param>
    /// <returns>对象数量</returns>
    public static int CountObjectsOfType<T>(this Node node, bool includeInactive = false) where T : Node
    {
        return node.FindObjectsOfType<T>(includeInactive).Length;
    }

    /// <summary>
    /// 获取场景树中所有激活的节点
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <returns>激活的节点数组</returns>
    public static Node[] GetAllActiveNodes(this Node node)
    {
        return node.FindObjectsOfType<Node>(includeInactive: false);
    }

    /// <summary>
    /// 获取场景树中所有节点
    /// </summary>
    /// <param name="node">任意节点（用于获取场景树）</param>
    /// <returns>节点数组</returns>
    public static Node[] GetAllNodes(this Node node)
    {
        return node.FindObjectsOfType<Node>(includeInactive: true);
    }

    #endregion

    #region 辅助方法

    private static Node GetSceneRoot(Node node)
    {
        return node?.GetTree()?.Root;
    }

    private static T FindObjectOfTypeRecursive<T>(Node node, bool includeInactive) where T : Node
    {
        if (!includeInactive && !IsNodeActive(node))
        {
            return null;
        }

        if (node is T directResult)
        {
            return directResult;
        }

        if (HasScriptOfType<T>(node))
        {
            return node as T;
        }

        foreach (var child in node.GetChildren())
        {
            var found = FindObjectOfTypeRecursive<T>(child, includeInactive);
            if (found != null) return found;
        }

        return null;
    }

    private static Node FindObjectOfTypeRecursive(Node node, Type type, bool includeInactive)
    {
        if (!includeInactive && !IsNodeActive(node))
        {
            return null;
        }

        if (type.IsInstanceOfType(node) || HasScriptOfType(node, type))
        {
            return node;
        }

        foreach (var child in node.GetChildren())
        {
            var found = FindObjectOfTypeRecursive(child, type, includeInactive);
            if (found != null) return found;
        }

        return null;
    }

    private static void FindObjectsOfTypeRecursive<T>(Node node, List<T> results, bool includeInactive) where T : Node
    {
        if (!includeInactive && !IsNodeActive(node))
        {
            return;
        }

        if (node is T directResult)
        {
            results.Add(directResult);
        }
        else if (HasScriptOfType<T>(node))
        {
            if (node is T scriptResult)
            {
                results.Add(scriptResult);
            }
        }

        foreach (var child in node.GetChildren())
        {
            FindObjectsOfTypeRecursive<T>(child, results, includeInactive);
        }
    }

    private static void FindObjectsOfTypeRecursive(Node node, Type type, List<Node> results, bool includeInactive)
    {
        if (!includeInactive && !IsNodeActive(node))
        {
            return;
        }

        if (type.IsInstanceOfType(node) || HasScriptOfType(node, type))
        {
            results.Add(node);
        }

        foreach (var child in node.GetChildren())
        {
            FindObjectsOfTypeRecursive(child, type, results, includeInactive);
        }
    }

    private static Node FindByNameRecursive(Node node, string name, bool exactMatch, bool includeInactive)
    {
        var nodeName = node.Name.ToString();
        var nameMatches = exactMatch
            ? string.Equals(nodeName, name, StringComparison.OrdinalIgnoreCase)
            : nodeName.Contains(name, StringComparison.OrdinalIgnoreCase);

        if (nameMatches && (includeInactive || IsNodeActive(node)))
        {
            return node;
        }

        foreach (var child in node.GetChildren())
        {
            var found = FindByNameRecursive(child, name, exactMatch, includeInactive);
            if (found != null) return found;
        }

        return null;
    }

    private static void FindAllByNameRecursive(Node node, string name, bool exactMatch, bool includeInactive,
        List<Node> results)
    {
        var nodeName = node.Name.ToString();
        var nameMatches = exactMatch
            ? string.Equals(nodeName, name, StringComparison.OrdinalIgnoreCase)
            : nodeName.Contains(name, StringComparison.OrdinalIgnoreCase);

        if (nameMatches && (includeInactive || IsNodeActive(node)))
        {
            results.Add(node);
        }

        foreach (var child in node.GetChildren())
        {
            FindAllByNameRecursive(child, name, exactMatch, includeInactive, results);
        }
    }

    private static bool IsNodeActive(Node node)
    {
        if (node.ProcessMode == Node.ProcessModeEnum.Disabled)
            return false;

        return node switch
        {
            CanvasItem { Visible: false } or Node3D { Visible: false } => false,
            _ => true
        };
    }

    private static bool HasScriptOfType<T>(Node node) where T : Node
    {
        try
        {
            var nodeType = node.GetType();
            var targetType = typeof(T);
            if (targetType.IsAssignableFrom(nodeType))
            {
                return true;
            }

            var script = node.GetScript();
            if (script.VariantType != Variant.Type.Nil)
            {
                if (script.AsGodotObject() is Script scriptResource)
                {
                    var scriptPath = scriptResource.ResourcePath;
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(scriptPath);
                        if (fileName == targetType.Name)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"检查脚本类型时出错: {ex.Message}");
        }

        return false;
    }

    private static bool HasScriptOfType(Node node, Type targetType)
    {
        try
        {
            var nodeType = node.GetType();

            if (targetType.IsAssignableFrom(nodeType))
            {
                return true;
            }

            var script = node.GetScript();
            if (script.VariantType != Variant.Type.Nil)
            {
                if (script.AsGodotObject() is Script scriptResource)
                {
                    var scriptPath = scriptResource.ResourcePath;
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(scriptPath);
                        if (fileName == targetType.Name)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"检查脚本类型时出错: {ex.Message}");
        }

        return false;
    }

    #endregion
}