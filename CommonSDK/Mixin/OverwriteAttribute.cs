// CommonSDK/Mixin/OverwriteAttribute.cs

namespace CommonSDK.Mixin
{
    /// <summary>
    /// 完全覆写目标方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OverwriteAttribute : Attribute
    {
        /// <summary>
        /// 目标方法名
        /// </summary>
        public string TargetMethod { get; }

        /// <summary>
        /// 覆写优先级，数字越小优先级越高
        /// </summary>
        public int Priority { get; set; } = 1000;

        /// <summary>
        /// 目标方法的签名（用于重载方法区分）
        /// </summary>
        public Type[] Signature { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// 是否保留原方法（通过 __originalMethod 调用）
        /// </summary>
        public bool PreserveOriginal { get; set; } = false;

        /// <summary>
        /// 覆写条件
        /// </summary>
        public string Condition { get; set; } = "";

        public OverwriteAttribute(string targetMethod)
        {
            TargetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
        }
    }
}