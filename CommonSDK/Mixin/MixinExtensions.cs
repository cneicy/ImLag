using System.Reflection;
#pragma warning disable CS8603 // 可能返回 null 引用。

namespace CommonSDK.Mixin;

/// <summary>
/// Mixin 系统的扩展方法
/// </summary>
public static class MixinExtensions
{
    /// <summary>
    /// 检查类型是否为 Mixin
    /// </summary>
    public static bool IsMixin(this Type type)
    {
        return type.GetCustomAttribute<MixinAttribute>() != null;
    }

    /// <summary>
    /// 获取 Mixin 的目标类型
    /// </summary>
    public static Type GetMixinTarget(this Type mixinType)
    {
        var attr = mixinType.GetCustomAttribute<MixinAttribute>();
        return attr?.TargetType;
    }

    /// <summary>
    /// 检查方法是否为注入方法
    /// </summary>
    public static bool IsInjectMethod(this MethodInfo method)
    {
        return method.GetCustomAttribute<InjectAttribute>() != null ||
               method.GetCustomAttribute<ConditionalInjectAttribute>() != null ||
               method.GetCustomAttribute<MultiInjectAttribute>() != null;
    }

    /// <summary>
    /// 检查方法是否为覆写方法
    /// </summary>
    public static bool IsOverwriteMethod(this MethodInfo method)
    {
        return method.GetCustomAttribute<OverwriteAttribute>() != null;
    }

    /// <summary>
    /// 检查方法是否为重定向方法
    /// </summary>
    public static bool IsRedirectMethod(this MethodInfo method)
    {
        return method.GetCustomAttribute<RedirectAttribute>() != null;
    }

    /// <summary>
    /// 获取方法的优先级
    /// </summary>
    public static int GetPriority(this MethodInfo method)
    {
        var injectAttr = method.GetCustomAttribute<InjectAttribute>();
        if (injectAttr != null) return injectAttr.Priority;

        var overwriteAttr = method.GetCustomAttribute<OverwriteAttribute>();
        if (overwriteAttr != null) return overwriteAttr.Priority;

        var redirectAttr = method.GetCustomAttribute<RedirectAttribute>();
        if (redirectAttr != null) return redirectAttr.Priority;

        var conditionalAttr = method.GetCustomAttribute<ConditionalInjectAttribute>();
        if (conditionalAttr != null) return conditionalAttr.Priority;

        var multiAttr = method.GetCustomAttribute<MultiInjectAttribute>();
        if (multiAttr != null) return multiAttr.Priority;

        return 1000; // 默认优先级
    }
}