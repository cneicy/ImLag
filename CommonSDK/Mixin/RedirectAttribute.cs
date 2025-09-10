// CommonSDK/Mixin/RedirectAttribute.cs

namespace CommonSDK.Mixin
{
    /// <summary>
    /// 重定向方法调用到另一个方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RedirectAttribute : Attribute
    {
        /// <summary>
        /// 目标方法名（包含重定向调用的方法）
        /// </summary>
        public string TargetMethod { get; }

        /// <summary>
        /// 要被重定向的方法调用
        /// </summary>
        public string CallMethod { get; }

        /// <summary>
        /// 重定向优先级
        /// </summary>
        public int Priority { get; set; } = 1000;

        /// <summary>
        /// 目标方法的签名
        /// </summary>
        public Type[] TargetSignature { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// 被调用方法的签名
        /// </summary>
        public Type[] CallSignature { get; set; } = Array.Empty<Type>();

        public RedirectAttribute(string targetMethod, string callMethod)
        {
            TargetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
            CallMethod = callMethod ?? throw new ArgumentNullException(nameof(callMethod));
        }
    }
}