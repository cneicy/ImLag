using Godot;
using Godot.Collections;
#pragma warning disable CS8603 // 可能返回 null 引用。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8605 // 取消装箱可能为 null 的值。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。

namespace CommonSDK.Utils;

public static class VariantUtils
{
    /// <summary>
    /// 将 Variant 转换为对应的 C# 类型
    /// </summary>
    public static T VariantToCSharp<T>(Variant variant)
    {
        if (variant.VariantType == Variant.Type.Nil) throw new ArgumentNullException(nameof(variant));
        
        var targetType = typeof(T);
        
        try
        {
            // 基本类型
            if (targetType == typeof(bool)) return (T)(object)variant.AsBool();
            if (targetType == typeof(int)) return (T)(object)variant.AsInt32();
            if (targetType == typeof(float)) return (T)(object)variant.AsSingle();
            if (targetType == typeof(string)) return (T)(object)variant.AsString();
            if (targetType == typeof(Vector2)) return (T)(object)variant.AsVector2();
            if (targetType == typeof(Vector3)) return (T)(object)variant.AsVector3();
            if (targetType == typeof(Color)) return (T)(object)variant.AsColor();

            // Godot容器类型
            if (targetType == typeof(Godot.Collections.Array)) return (T)(object)variant.AsGodotArray();
            if (targetType == typeof(Dictionary)) return (T)(object)variant.AsGodotDictionary();

            if (targetType.IsArray) return ConvertToCSharpArray<T>(variant);
            if (IsGenericDictionary(targetType)) return ConvertToCSharpDictionary<T>(variant);

            // 处理Node类型
            if (typeof(Node).IsAssignableFrom(targetType))
                return (T)(object)variant.AsGodotObject();

            // 泛型数组（如List<int>, List<string>等）
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertToCSharpList<T>(variant);

            throw new NotSupportedException($"不支持的目标类型: {targetType.Name}");
        }
        catch (InvalidCastException ex)
        {
            throw new InvalidCastException(
                $"未能将Variant类型 {variant.VariantType} 转换到 {targetType.Name}", ex);
        }
    }

    /// <summary>
    /// 将 C# 类型转换为 Variant
    /// </summary>
    public static Variant CSharpToVariant(object value)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (value == null) return default;
        
        var type = value.GetType();

        try
        {
            // 基本类型
            if (type == typeof(bool)) return Variant.CreateFrom((bool)value);
            if (type == typeof(int)) return Variant.CreateFrom((int)value);
            if (type == typeof(float)) return Variant.CreateFrom((float)value);
            if (type == typeof(string)) return Variant.CreateFrom((string)value);
            if (type == typeof(Vector2)) return Variant.CreateFrom((Vector2)value);
            if (type == typeof(Vector3)) return Variant.CreateFrom((Vector3)value);
            if (type == typeof(Color)) return Variant.CreateFrom((Color)value);

            // Godot容器类型
            if (type == typeof(Godot.Collections.Array)) return Variant.CreateFrom((Godot.Collections.Array)value);
            if (type == typeof(Dictionary)) return Variant.CreateFrom((Dictionary)value);

            // Node类型
            if (typeof(Node).IsAssignableFrom(type))
                return Variant.CreateFrom((GodotObject)value);

            // C#数组和列表
            if (type.IsArray)
                return ConvertArrayToVariant((System.Array)value);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertListToVariant(value, type);

            // C#字典
            if (IsGenericDictionary(type))
                return ConvertDictionaryToVariant(value, type);

            throw new NotSupportedException($"不支持的类型转换: {type.Name}");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"未能将类型 {type.Name} 转换到Variant", ex);
        }
    }
    
    #region 数组和列表转换
    
    private static T ConvertToCSharpArray<T>(Variant variant)
    {
        var elementType = typeof(T).GetElementType();
        var godotArray = variant.AsGodotArray();
        var result = System.Array.CreateInstance(elementType, godotArray.Count);

        for (var i = 0; i < godotArray.Count; i++)
        {
            var itemVar = godotArray[i];
            var item = VariantToCSharp(itemVar, elementType);
            result.SetValue(item, i);
        }

        return (T)(object)result;
    }
    
    private static T ConvertToCSharpList<T>(Variant variant)
    {
        var listType = typeof(T);
        var elementType = listType.GetGenericArguments()[0];
        var godotArray = variant.AsGodotArray();

        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");

        foreach (var itemVar in godotArray)
        {
            var item = VariantToCSharp(itemVar, elementType);
            addMethod.Invoke(list, [item]);
        }

        return (T)list;
    }
    
    private static Variant ConvertListToVariant(object list, Type listType)
    {
        // ReSharper disable once UnusedVariable
        var elementType = listType.GetGenericArguments()[0];
        var getCount = listType.GetProperty("Count");
        var getItem = listType.GetMethod("get_Item");

        var count = (int)getCount.GetValue(list);
        var result = new Godot.Collections.Array();

        for (var i = 0; i < count; i++)
        {
            var item = getItem.Invoke(list, [i]);
            result.Add(CSharpToVariant(item));
        }

        return Variant.CreateFrom(result);
    }
    
    private static Variant ConvertArrayToVariant(System.Array array)
    {
        var result = new Godot.Collections.Array();
        foreach (var item in array)
        {
            result.Add(CSharpToVariant(item));
        }
        return Variant.CreateFrom(result);
    }
    
    #endregion
    
    #region 字典转换
    
    private static bool IsGenericDictionary(Type type)
    {
        return type.IsGenericType && 
               type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.Dictionary<,>);
    }
    
    private static T ConvertToCSharpDictionary<T>(Variant variant)
    {
        var dictType = typeof(T);
        var typeArgs = dictType.GetGenericArguments();
        var keyType = typeArgs[0];
        var valueType = typeArgs[1];

        var godotDict = variant.AsGodotDictionary();
        var dictionary = Activator.CreateInstance(dictType);
        var addMethod = dictType.GetMethod("Add");

        foreach (Variant keyVar in godotDict.Keys)
        {
            var key = VariantToCSharp(keyVar, keyType);
            var value = VariantToCSharp(godotDict[keyVar], valueType);
            addMethod.Invoke(dictionary, [key, value]);
        }

        return (T)dictionary;
    }
    
    private static Variant ConvertDictionaryToVariant(object dict, Type dictType)
    {
        var typeArgs = dictType.GetGenericArguments();
        var keyType = typeArgs[0];

        if (!IsVariantCompatible(keyType))
            throw new InvalidOperationException($"字典键类型必须为string或Variant支持类型: {keyType.Name}");

        var result = new Dictionary();
        var getEnumerator = dictType.GetMethod("GetEnumerator");
        var moveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
        var currentProperty = typeof(System.Collections.IEnumerator).GetProperty("Current");

        var enumerator = getEnumerator.Invoke(dict, null);
        while ((bool)moveNext.Invoke(enumerator, null))
        {
            var current = currentProperty.GetValue(enumerator);
            var key = current.GetType().GetProperty("Key").GetValue(current);
            var value = current.GetType().GetProperty("Value").GetValue(current);

            var variantKey = key switch
            {
                string s => Variant.CreateFrom(s),
                bool b => Variant.CreateFrom(b),
                int i => Variant.CreateFrom(i),
                float f => Variant.CreateFrom(f),
                Vector2 vector2 => Variant.CreateFrom(vector2),
                Vector3 vector3 => Variant.CreateFrom(vector3),
                _ => throw new InvalidOperationException($"不支持转换到Variant的键类型: {key.GetType().Name}")
            };

            result[variantKey] = CSharpToVariant(value);
        }

        return Variant.CreateFrom(result);
    }
    
    private static bool IsVariantCompatible(Type type)
    {
        return type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(string) ||
               type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color) || 
               type == typeof(Godot.Collections.Array) || type == typeof(Dictionary) ||
               typeof(Node).IsAssignableFrom(type);
    }

    #endregion
    
    #region 类型辅助方法
    
    private static object VariantToCSharp(Variant variant, Type targetType)
    {
        var method = typeof(VariantUtils).GetMethod("VariantToCSharp").MakeGenericMethod(targetType);
        return method.Invoke(null, new object[] { variant });
    }
    
    #endregion
}