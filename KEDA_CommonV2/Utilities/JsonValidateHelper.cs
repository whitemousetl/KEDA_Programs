using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Utilities;
public static class JsonValidateHelper
{
    /// <summary>
    /// 保证JSON中传入的字段存在
    /// </summary>
    public static void EnsurePropertyExists(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var _))
            throw new JsonException($"缺少{name}字段");
        return;
    }

    public static T EnsurePropertyTypeIsRight<T>(JsonElement prop, string name, JsonValueKind expectedKind)
    {
        if (expectedKind == JsonValueKind.Null)
            throw new JsonException($"{name}字段不能为null类型");

        if (expectedKind == JsonValueKind.True || expectedKind == JsonValueKind.False)
        {
            if (prop.ValueKind != JsonValueKind.True && prop.ValueKind != JsonValueKind.False)
                throw new JsonException($"{name}字段必须为布尔类型");
        }
        else if (prop.ValueKind != expectedKind)
        {
            string typeName = expectedKind switch
            {
                JsonValueKind.Object => "对象",
                JsonValueKind.String => "字符串",
                JsonValueKind.Number => "数字",
                JsonValueKind.Array => "数组",
                _ => "未知类型"
            };
            throw new JsonException($"{name}字段必须为{typeName}");
        }

        object? value = expectedKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => typeof(T) == typeof(int) ? prop.GetInt32() :
                                   typeof(T) == typeof(long) ? prop.GetInt64() :
                                   typeof(T) == typeof(double) ? prop.GetDouble() :
                                   throw new JsonException($"{name}字段为数字，但T类型不支持"),
            JsonValueKind.True or JsonValueKind.False => prop.GetBoolean(),
            JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Deserialize<T>(prop),
            _ => throw new JsonException($"{name}字段类型不支持")
        };

        if (value is null)
            throw new JsonException($"{name}字段反序列化失败或为null");

        return (T)value;
    }

    public static TEnum EnsureEnumIsRight<TEnum>(JsonElement prop, string name) where TEnum : struct, Enum
    {
        if (prop.ValueKind != JsonValueKind.Number)
            throw new JsonException($"{name}字段必须为数字");

        var value = prop.GetInt32();

        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new JsonException($"{name}值{value}超出有效范围");

        return (TEnum)(object)value;
    }

    public static TEnum? GetOptionalEnum<TEnum>(JsonElement root, string name) where TEnum : struct, Enum
    {
        if (root.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            if (prop.ValueKind != JsonValueKind.Number)
                throw new JsonException($"{name}字段存在时，必须为数字类型");

            var value = prop.GetInt32();

            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new JsonException($"{name}值{value}超出有效范围");

            return (TEnum)(object)value;
        }
        return null;
    }

    public static T? GetOptionalValue<T>(JsonElement root, string name, JsonValueKind expectedKind)
    {
        if (root.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            bool isValid = expectedKind switch
            {
                JsonValueKind.String => prop.ValueKind == JsonValueKind.String,
                JsonValueKind.Number => prop.ValueKind == JsonValueKind.Number,
                JsonValueKind.True or JsonValueKind.False => prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False,
                JsonValueKind.Array => prop.ValueKind == JsonValueKind.Array,
                JsonValueKind.Object => prop.ValueKind == JsonValueKind.Object,
                _ => false
            };

            if (!isValid)
            {
                string typeName = expectedKind switch
                {
                    JsonValueKind.String => "字符串类型",
                    JsonValueKind.Number => "数字类型",
                    JsonValueKind.True or JsonValueKind.False => "布尔类型",
                    JsonValueKind.Array => "数组类型",
                    JsonValueKind.Object => "对象类型",
                    _ => "指定类型"
                };
                throw new JsonException($"{name}字段存在时，必须为{typeName}");
            }

            // 类型转换
            object? value = expectedKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Number => typeof(T) == typeof(int) ? prop.GetInt32() :
                                       typeof(T) == typeof(long) ? prop.GetInt64() :
                                       typeof(T) == typeof(double) ? prop.GetDouble() :
                                       throw new JsonException($"{name}字段为数字，但T类型不支持"),
                JsonValueKind.True or JsonValueKind.False => prop.GetBoolean(),
                JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Deserialize<T>(prop),
                _ => null
            };

            return (T?)value;
        }
        return default;
    }
}
