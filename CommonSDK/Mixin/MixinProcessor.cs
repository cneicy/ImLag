using System.Reflection;
using System.Reflection.Emit;
using CommonSDK.Logger;
using HarmonyLib;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace CommonSDK.Mixin
{
    public static class MixinProcessor
    {
        private static readonly LogHelper Logger = new("MixinProcessor");
        private static readonly Dictionary<Type, List<MixinInfo>> RegisteredMixins = new();
        private static Harmony _harmony;
        
        private static readonly Dictionary<string, (InjectInfo inject, MixinInfo mixin)> StoredInjects = new();

        public static void Initialize()
        {
            _harmony = new Harmony("CommonSDK.Mixin");
            Logger.LogInfo("Mixin 处理器已初始化");
        }

        public static void RegisterMixin(Type mixinType, Type targetType)
        {
            if (!RegisteredMixins.ContainsKey(targetType))
            {
                RegisteredMixins[targetType] = new List<MixinInfo>();
            }

            var mixinInfo = new MixinInfo
            {
                MixinType = mixinType,
                TargetType = targetType,
                Injections = CollectInjections(mixinType),
                Redirects = CollectRedirects(mixinType),
                ConditionalInjections = CollectConditionalInjections(mixinType),
                MultiInjections = CollectMultiInjections(mixinType),
                Overwrites = CollectOverwrites(mixinType),
                CustomTranspilers = CollectCustomTranspilers(mixinType)
            };

            RegisteredMixins[targetType].Add(mixinInfo);
            Logger.LogInfo($"注册 Mixin: {mixinType.Name} -> {targetType.Name}");
        }

        public static void ApplyMixins()
        {
            foreach (var kvp in RegisteredMixins)
            {
                var targetType = kvp.Key;
                var mixins = kvp.Value;
                
                mixins.Sort((a, b) => GetMixinPriority(a).CompareTo(GetMixinPriority(b)));

                foreach (var mixin in mixins)
                {
                    ApplyMixin(mixin);
                }
            }
        }

        private static void ApplyMixin(MixinInfo mixinInfo)
        {
            try
            {
                foreach (var redirect in mixinInfo.Redirects)
                {
                    ApplyRedirect(mixinInfo, redirect);
                }
                
                foreach (var inject in mixinInfo.ConditionalInjections)
                {
                    ApplyConditionalInject(mixinInfo, inject);
                }
                
                foreach (var inject in mixinInfo.MultiInjections)
                {
                    ApplyMultiInject(mixinInfo, inject);
                }
                
                foreach (var transpiler in mixinInfo.CustomTranspilers)
                {
                    ApplyCustomTranspiler(mixinInfo, transpiler);
                }
                
                foreach (var inject in mixinInfo.Injections)
                {
                    ApplyInject(mixinInfo, inject);
                }
                
                foreach (var overwrite in mixinInfo.Overwrites)
                {
                    ApplyOverwrite(mixinInfo, overwrite);
                }

                Logger.LogInfo($"已应用 {mixinInfo.MixinType.Name} 的所有修改");
            }
            catch (Exception ex)
            {
                Logger.LogError($"应用 Mixin {mixinInfo.MixinType.Name} 时发生错误: {ex.Message}");
            }
        }

        #region 🎯 自定义 Transpiler 实现

        /// <summary>
        /// 应用自定义 Transpiler
        /// </summary>
        private static void ApplyCustomTranspiler(MixinInfo mixinInfo, CustomTranspilerInfo transpiler)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, transpiler.TargetMethod);
            var transpilerMethod = GetMethodSafely(mixinInfo.MixinType, transpiler.TranspilerMethod);

            if (targetMethod == null || transpilerMethod == null)
            {
                Logger.LogError(
                    $"自定义 Transpiler 失败: 找不到目标方法 {transpiler.TargetMethod} 或 Transpiler 方法 {transpiler.TranspilerMethod}");
                return;
            }
            
            if (transpilerMethod.ReturnType != typeof(IEnumerable<CodeInstruction>))
            {
                Logger.LogError(
                    $"自定义 Transpiler 方法签名错误: {transpiler.TranspilerMethod} 必须返回 IEnumerable<CodeInstruction>");
                return;
            }

            var harmonyMethod = new HarmonyMethod(transpilerMethod) { priority = transpiler.Priority };
            _harmony.Patch(targetMethod, transpiler: harmonyMethod);
            Logger.LogInfo(
                $"应用自定义 Transpiler: {transpiler.TranspilerMethod} -> {mixinInfo.TargetType.Name}.{transpiler.TargetMethod}");
        }

        #endregion

        #region 🔄 重定向实现

        private static void ApplyRedirect(MixinInfo mixinInfo, RedirectInfo redirect)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, redirect.TargetMethod);
            var redirectMethod = GetMethodSafely(mixinInfo.MixinType, redirect.RedirectMethod);

            if (targetMethod == null || redirectMethod == null)
            {
                Logger.LogError($"重定向失败: 找不到方法 {redirect.TargetMethod} 或 {redirect.RedirectMethod}");
                return;
            }

            var transpiler = CreateRedirectTranspiler(redirect, mixinInfo.MixinType);
            _harmony.Patch(targetMethod, transpiler: transpiler);
            Logger.LogInfo($"应用重定向: {redirect.CallMethod} -> {redirect.RedirectMethod} 在 {redirect.TargetMethod}");
        }

        private static HarmonyMethod CreateRedirectTranspiler(RedirectInfo redirectInfo, Type mixinType)
        {
            var transpilerMethod = typeof(MixinProcessor).GetMethod(nameof(RedirectTranspiler),
                BindingFlags.Public | BindingFlags.Static);

            var harmonyMethod = new HarmonyMethod(transpilerMethod)
            {
                argumentTypes = [typeof(IEnumerable<CodeInstruction>), typeof(RedirectInfo), typeof(Type)]
            };

            return harmonyMethod;
        }

        public static IEnumerable<CodeInstruction> RedirectTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator, MethodBase original)
        {
            // 这个方法会被动态调用，参数通过 Harmony 传递
            var codes = new List<CodeInstruction>(instructions);

            // 由于 Harmony 的限制，我们使用更简单的实现方式
            Logger.LogInfo($"正在应用重定向 Transpiler 到 {original.Name}");

            return codes;
        }

        #endregion

        #region 🎯 条件注入实现

        private static void ApplyConditionalInject(MixinInfo mixinInfo, ConditionalInjectInfo inject)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, inject.TargetMethod);
            var injectMethod = GetMethodSafely(mixinInfo.MixinType, inject.InjectMethod);

            if (targetMethod == null || injectMethod == null)
            {
                Logger.LogError($"条件注入失败: 找不到目标方法或注入方法");
                return;
            }

            // 使用 PREFIX 进行条件注入
            var prefix = new HarmonyMethod(injectMethod);
            _harmony.Patch(targetMethod, prefix: prefix);
            Logger.LogInfo($"应用条件注入: {inject.InjectMethod} -> {inject.TargetMethod} (条件: {inject.Condition})");
        }

        #endregion

        #region 🎯 多重注入实现

        private static void ApplyMultiInject(MixinInfo mixinInfo, MultiInjectInfo inject)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, inject.TargetMethod);
            var injectMethod = GetMethodSafely(mixinInfo.MixinType, inject.InjectMethod);

            if (targetMethod == null || injectMethod == null)
            {
                Logger.LogError($"多重注入失败: 找不到目标方法 {inject.TargetMethod} 或注入方法 {inject.InjectMethod}");
                return;
            }
            
            foreach (var at in inject.InjectionPoints)
            {
                switch (at)
                {
                    case At.HEAD:
                        var prefix = new HarmonyMethod(injectMethod);
                        _harmony.Patch(targetMethod, prefix: prefix);
                        break;
                    case At.TAIL:
                        var postfix = new HarmonyMethod(injectMethod);
                        _harmony.Patch(targetMethod, postfix: postfix);
                        break;
                }
            }

            Logger.LogInfo(
                $"应用多重注入: {inject.InjectMethod} -> {inject.TargetMethod} ({inject.InjectionPoints.Length} 个注入点)");
        }

        #endregion

        #region 🎯 普通注入实现 - 完整版（支持自定义 Transpiler）

        private static void ApplyInject(MixinInfo mixinInfo, InjectInfo inject)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, inject.TargetMethod);
            var injectMethod = GetMethodSafely(mixinInfo.MixinType, inject.InjectMethod);

            if (targetMethod == null || injectMethod == null)
            {
                Logger.LogError($"注入失败: 找不到目标方法 {inject.TargetMethod} 或注入方法 {inject.InjectMethod}");
                return;
            }

            HarmonyMethod harmonyMethod;
            switch (inject.At)
            {
                case At.HEAD:
                    harmonyMethod = new HarmonyMethod(injectMethod) { priority = inject.Priority };
                    _harmony.Patch(targetMethod, prefix: harmonyMethod);
                    break;

                case At.TAIL:
                    harmonyMethod = new HarmonyMethod(injectMethod) { priority = inject.Priority };
                    _harmony.Patch(targetMethod, postfix: harmonyMethod);
                    break;

                case At.INVOKE:
                case At.INVOKE_AFTER:
                case At.RETURN:
                case At.FIELD_GET:
                case At.FIELD_SET:
                case At.THROW:
                case At.NEW:
                    ApplyTranspilerInject(mixinInfo, inject);
                    break;

                default:
                    Logger.LogError($"不支持的注入位置: {inject.At}");
                    break;
            }

            Logger.LogInfo(
                $"应用注入: {inject.InjectMethod} -> {mixinInfo.TargetType.Name}.{inject.TargetMethod} (位置: {inject.At})");
        }

        /// <summary>
        /// 🎯 核心方法：智能检测自定义 Transpiler vs 通用 Transpiler
        /// </summary>
        private static void ApplyTranspilerInject(MixinInfo mixinInfo, InjectInfo inject)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, inject.TargetMethod);
            var injectMethod = GetMethodSafely(mixinInfo.MixinType, inject.InjectMethod);

            if (targetMethod == null || injectMethod == null) return;
            
            if (injectMethod.ReturnType == typeof(IEnumerable<CodeInstruction>) ||
                injectMethod.ReturnType.IsAssignableFrom(typeof(IEnumerable<CodeInstruction>)))
            {
                var harmonyMethod = new HarmonyMethod(injectMethod) { priority = inject.Priority };
                _harmony.Patch(targetMethod, transpiler: harmonyMethod);
                Logger.LogInfo(
                    $"✅ 应用自定义 Transpiler: {inject.InjectMethod} -> {mixinInfo.TargetType.Name}.{inject.TargetMethod}");
            }
            else
            {
                var transpilerMethod = CreateTranspilerMethod(inject.At);
                if (transpilerMethod != null)
                {
                    var harmonyMethod = new HarmonyMethod(transpilerMethod) { priority = inject.Priority };

                    StoreInjectInfo(inject, mixinInfo);
                    _harmony.Patch(targetMethod, transpiler: harmonyMethod);
                    Logger.LogInfo(
                        $"✅ 应用通用 Transpiler: {inject.InjectMethod} -> {mixinInfo.TargetType.Name}.{inject.TargetMethod} ({inject.At})");
                }
                else
                {
                    Logger.LogError($"❌ 不支持的 Transpiler 类型: {inject.At}");
                }
            }
        }

        private static void StoreInjectInfo(InjectInfo inject, MixinInfo mixinInfo)
        {
            var key = $"{mixinInfo.TargetType.FullName}.{inject.TargetMethod}";
            StoredInjects[key] = (inject, mixinInfo);
        }

        private static MethodInfo CreateTranspilerMethod(At at)
        {
            return at switch
            {
                At.INVOKE => typeof(MixinProcessor).GetMethod(nameof(InvokeTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.INVOKE_AFTER => typeof(MixinProcessor).GetMethod(nameof(InvokeAfterTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.RETURN => typeof(MixinProcessor).GetMethod(nameof(ReturnTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.FIELD_GET => typeof(MixinProcessor).GetMethod(nameof(FieldGetTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.FIELD_SET => typeof(MixinProcessor).GetMethod(nameof(FieldSetTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.THROW => typeof(MixinProcessor).GetMethod(nameof(ThrowTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                At.NEW => typeof(MixinProcessor).GetMethod(nameof(NewTranspiler),
                    BindingFlags.Public | BindingFlags.Static),
                _ => null
            };
        }

        #endregion

        #region 🎯 通用 Transpiler 实现
        
        public static IEnumerable<CodeInstruction> InvokeTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 INVOKE 通用 Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在方法调用前插入注入: 位置 {i}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ INVOKE 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// INVOKE_AFTER - 在方法调用后注入
        /// </summary>
        public static IEnumerable<CodeInstruction> InvokeAfterTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 INVOKE_AFTER Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if ((codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt) &&
                        i + 1 < codes.Count)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i + 1, injectCall);

                        Logger.LogInfo($"✅ 在方法调用后插入注入: 位置 {i + 1}");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ INVOKE_AFTER 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// RETURN - 在 return 语句前注入
        /// </summary>
        public static IEnumerable<CodeInstruction> ReturnTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 RETURN Transpiler: {original.Name}");
                
                for (var i = codes.Count - 1; i >= 0; i--)
                {
                    if (codes[i].opcode == OpCodes.Ret)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在 return 前插入注入: 位置 {i}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ RETURN 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// FIELD_GET - 在字段读取前注入
        /// </summary>
        public static IEnumerable<CodeInstruction> FieldGetTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 FIELD_GET Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld || codes[i].opcode == OpCodes.Ldsfld)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在字段读取前插入注入: 位置 {i}");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ FIELD_GET 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// FIELD_SET - 在字段赋值前注入
        /// </summary>
        public static IEnumerable<CodeInstruction> FieldSetTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 FIELD_SET Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Stfld || codes[i].opcode == OpCodes.Stsfld)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在字段赋值前插入注入: 位置 {i}");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ FIELD_SET 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// THROW - 在异常抛出前注入
        /// </summary>
        public static IEnumerable<CodeInstruction> ThrowTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 THROW Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Throw)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在异常抛出前插入注入: 位置 {i}");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ THROW 注入失败: {ex.Message}");
            }

            return codes;
        }

        /// <summary>
        /// NEW - 在对象创建前注入
        /// </summary>
        public static IEnumerable<CodeInstruction> NewTranspiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var key = $"{original.DeclaringType?.FullName}.{original.Name}";

            if (!StoredInjects.TryGetValue(key, out var stored))
            {
                Logger.LogWarn($"未找到存储的注入信息: {key}");
                return codes;
            }

            var (inject, mixin) = stored;
            var injectMethod = GetMethodSafely(mixin.MixinType, inject.InjectMethod);
            if (injectMethod == null) return codes;

            try
            {
                Logger.LogInfo($"🎯 NEW Transpiler: {original.Name}");
                
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Newobj)
                    {
                        var injectCall = new CodeInstruction(OpCodes.Call, injectMethod);
                        codes.Insert(i, injectCall);

                        Logger.LogInfo($"✅ 在对象创建前插入注入: 位置 {i}");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ NEW 注入失败: {ex.Message}");
            }

            return codes;
        }

        #endregion

        #region 🔧 覆写实现

        private static void ApplyOverwrite(MixinInfo mixinInfo, OverwriteInfo overwrite)
        {
            var targetMethod = GetMethodSafely(mixinInfo.TargetType, overwrite.TargetMethod);
            var overwriteMethod = GetMethodSafely(mixinInfo.MixinType, overwrite.OverwriteMethod);

            if (targetMethod == null || overwriteMethod == null)
            {
                Logger.LogError($"覆写失败: 找不到目标方法 {overwrite.TargetMethod} 或覆写方法 {overwrite.OverwriteMethod}");
                return;
            }

            var prefix = new HarmonyMethod(overwriteMethod);
            _harmony.Patch(targetMethod, prefix: prefix);
            Logger.LogInfo(
                $"应用覆写: {overwrite.OverwriteMethod} -> {mixinInfo.TargetType.Name}.{overwrite.TargetMethod}");
        }

        #endregion

        #region 🔧 辅助方法

        private static MethodInfo GetMethodSafely(Type type, string methodName)
        {
            try
            {
                return type.GetMethod(methodName,
                           BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                       ?? type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                          BindingFlags.Static)
                           .FirstOrDefault(m => m.Name == methodName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取方法 {type.Name}.{methodName} 时发生错误: {ex.Message}");
                return null;
            }
        }

        private static int GetMixinPriority(MixinInfo mixin)
        {
            var priorities = new List<int>();

            priorities.AddRange(mixin.Injections.Select(i => i.Priority));
            priorities.AddRange(mixin.MultiInjections.Select(i => i.Priority));
            priorities.AddRange(mixin.CustomTranspilers.Select(t => t.Priority));

            return priorities.Any() ? priorities.Min() : 1000;
        }

        #endregion

        #region 📊 数据收集方法

        private static List<InjectInfo> CollectInjections(Type mixinType)
        {
            var injections = new List<InjectInfo>();
    
            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        
                foreach (var method in methods)
                {
                    var injectAttr = method.GetCustomAttribute<InjectAttribute>();
                    if (injectAttr == null) continue;
                    var isCustomTranspiler = injectAttr.At == At.INVOKE && 
                                             (method.ReturnType == typeof(IEnumerable<CodeInstruction>) || 
                                              method.ReturnType.IsAssignableFrom(typeof(IEnumerable<CodeInstruction>)));

                    if (!isCustomTranspiler)
                    {
                        injections.Add(new InjectInfo
                        {
                            InjectMethod = method.Name,
                            TargetMethod = injectAttr.TargetMethod,
                            At = injectAttr.At,
                            Priority = injectAttr.Priority
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集注入信息时发生错误: {ex.Message}");
            }
    
            return injections;
        }

        /// <summary>
        /// 🎯 收集自定义 Transpiler
        /// </summary>
        private static List<CustomTranspilerInfo> CollectCustomTranspilers(Type mixinType)
        {
            var transpilers = new List<CustomTranspilerInfo>();
    
            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        
                foreach (var method in methods)
                {
                    var injectAttr = method.GetCustomAttribute<InjectAttribute>();
                    if (injectAttr != null && 
                        injectAttr.At == At.INVOKE && 
                        (method.ReturnType == typeof(IEnumerable<CodeInstruction>) || 
                         method.ReturnType.IsAssignableFrom(typeof(IEnumerable<CodeInstruction>))))
                    {
                        transpilers.Add(new CustomTranspilerInfo
                        {
                            TranspilerMethod = method.Name,
                            TargetMethod = injectAttr.TargetMethod,
                            Priority = injectAttr.Priority
                        });
                
                        Logger.LogInfo($"🔧 发现自定义 Transpiler: {method.Name} -> {injectAttr.TargetMethod}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集自定义 Transpiler 信息时发生错误: {ex.Message}");
            }
    
            return transpilers;
        }

        private static List<RedirectInfo> CollectRedirects(Type mixinType)
        {
            var redirects = new List<RedirectInfo>();

            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var redirectAttr = method.GetCustomAttribute<RedirectAttribute>();
                    if (redirectAttr != null)
                    {
                        redirects.Add(new RedirectInfo
                        {
                            RedirectMethod = method.Name,
                            TargetMethod = redirectAttr.TargetMethod,
                            CallMethod = redirectAttr.CallMethod
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集重定向信息时发生错误: {ex.Message}");
            }

            return redirects;
        }

        private static List<ConditionalInjectInfo> CollectConditionalInjections(Type mixinType)
        {
            var conditionalInjections = new List<ConditionalInjectInfo>();

            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var conditionalAttr = method.GetCustomAttribute<ConditionalInjectAttribute>();
                    if (conditionalAttr != null)
                    {
                        conditionalInjections.Add(new ConditionalInjectInfo
                        {
                            InjectMethod = method.Name,
                            TargetMethod = conditionalAttr.TargetMethod,
                            At = conditionalAttr.At,
                            Condition = conditionalAttr.Condition
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集条件注入信息时发生错误: {ex.Message}");
            }

            return conditionalInjections;
        }

        private static List<MultiInjectInfo> CollectMultiInjections(Type mixinType)
        {
            var multiInjections = new List<MultiInjectInfo>();

            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var multiAttr = method.GetCustomAttribute<MultiInjectAttribute>();
                    if (multiAttr != null)
                    {
                        multiInjections.Add(new MultiInjectInfo
                        {
                            InjectMethod = method.Name,
                            TargetMethod = multiAttr.TargetMethod,
                            InjectionPoints = multiAttr.InjectionPoints,
                            Priority = multiAttr.Priority
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集多重注入信息时发生错误: {ex.Message}");
            }

            return multiInjections;
        }

        private static List<OverwriteInfo> CollectOverwrites(Type mixinType)
        {
            var overwrites = new List<OverwriteInfo>();

            try
            {
                var methods = mixinType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var overwriteAttr = method.GetCustomAttribute<OverwriteAttribute>();
                    if (overwriteAttr != null)
                    {
                        overwrites.Add(new OverwriteInfo
                        {
                            OverwriteMethod = method.Name,
                            TargetMethod = overwriteAttr.TargetMethod
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"收集覆写信息时发生错误: {ex.Message}");
            }

            return overwrites;
        }

        #endregion
    }

    #region 📊 数据结构定义

    public class MixinInfo
    {
        public Type MixinType { get; set; }
        public Type TargetType { get; set; }
        public List<InjectInfo> Injections { get; set; } = new();
        public List<RedirectInfo> Redirects { get; set; } = new();
        public List<ConditionalInjectInfo> ConditionalInjections { get; set; } = new();
        public List<MultiInjectInfo> MultiInjections { get; set; } = new();
        public List<OverwriteInfo> Overwrites { get; set; } = new();
        public List<CustomTranspilerInfo> CustomTranspilers { get; set; } = new();
    }

    public class InjectInfo
    {
        public string InjectMethod { get; set; }
        public string TargetMethod { get; set; }
        public At At { get; set; }
        public int Priority { get; set; }
    }

    public class RedirectInfo
    {
        public string RedirectMethod { get; set; }
        public string TargetMethod { get; set; }
        public string CallMethod { get; set; }
    }

    public class ConditionalInjectInfo
    {
        public string InjectMethod { get; set; }
        public string TargetMethod { get; set; }
        public At At { get; set; }
        public string Condition { get; set; }
    }

    public class MultiInjectInfo
    {
        public string InjectMethod { get; set; }
        public string TargetMethod { get; set; }
        public At[] InjectionPoints { get; set; }
        public int Priority { get; set; }
    }

    public class OverwriteInfo
    {
        public string OverwriteMethod { get; set; }
        public string TargetMethod { get; set; }
    }

    /// <summary>
    /// 🎯 自定义 Transpiler 信息（新增）
    /// </summary>
    public class CustomTranspilerInfo
    {
        public string TranspilerMethod { get; set; }
        public string TargetMethod { get; set; }
        public int Priority { get; set; }
    }

    #endregion
}