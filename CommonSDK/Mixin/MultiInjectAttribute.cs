// CommonSDK/Mixin/MultiInjectAttribute.cs

namespace CommonSDK.Mixin
{
    /// <summary>
    /// 多重注入 - 在同一个方法的多个位置注入相同代码
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MultiInjectAttribute : Attribute
    {
        /// <summary>
        /// 目标方法名
        /// </summary>
        public string TargetMethod { get; }

        /// <summary>
        /// 所有注入点
        /// </summary>
        public At[] InjectionPoints { get; }

        /// <summary>
        /// 注入优先级
        /// </summary>
        public int Priority { get; set; } = 1000;

        /// <summary>
        /// 目标方法签名
        /// </summary>
        public Type[] Signature { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// 是否允许部分注入失败
        /// </summary>
        public bool AllowPartialFailure { get; set; } = false;

        /// <summary>
        /// 每个注入点的条件（与 InjectionPoints 数组对应）
        /// </summary>
        public string[] Conditions { get; set; } = Array.Empty<string>();

        public MultiInjectAttribute(string targetMethod, params At[] injectionPoints)
        {
            TargetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
            InjectionPoints = injectionPoints ?? throw new ArgumentNullException(nameof(injectionPoints));

            if (injectionPoints.Length == 0)
                throw new ArgumentException("至少需要一个注入点", nameof(injectionPoints));
        }
    }
}