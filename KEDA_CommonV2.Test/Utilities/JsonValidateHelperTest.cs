using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace KEDA_CommonV2.Test.Utilities;
public class JsonValidateHelperTest
{
    // 测试类定义
    public class Address
    {
        public string City { get; set; } = string.Empty;
        public int Zip { get; set; }
    }

    private readonly ITestOutputHelper _output;

    public enum OptionalTestStatus
    {
        None = 0,
        Success = 1,
        Failed = 2
    }

    public JsonValidateHelperTest(ITestOutputHelper output)
    {
        _output = output;
    }

    #region EnsurePropertyExistsAndTypeIsRight

    #region 正常路径（Happy Path）
    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验字符串_返回字符串()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "User的", "name", JsonValueKind.String);

        Assert.Equal("Alice", result);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验数字_返回数字()
    {
        var json = """{"age": 18}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<int>(root, "User的", "age", JsonValueKind.Number);

        Assert.Equal(18, result);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验布尔值true_返回布尔值true()
    {
        var json = """{"isActive": true}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(root, "User的", "isActive", JsonValueKind.True);

        Assert.True(result);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验布尔值false_返回布尔值false()
    {
        var json = """{"isActive": false}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(root, "User的", "isActive", JsonValueKind.True);

        Assert.False(result);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验对象_返回对象()
    {
        var json = """{"Address": {"City": "Beijing", "Zip": 100000}}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<Address>(root, "User的", "Address", JsonValueKind.Object);

        Assert.NotNull(result);
        Assert.Equal("Beijing", result.City);
        Assert.Equal(100000, result.Zip);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_校验数组_返回数组()
    {
        var json = """{"tags": ["admin", "user"]}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<string>>(root, "User的", "tags", JsonValueKind.Array);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("admin", result[0]);
        Assert.Equal("user", result[1]);
    }

    [Theory]
    [InlineData("longValue", 9223372036854775807L)]
    [InlineData("doubleValue", 3.14)]
    [InlineData("floatValue", 1.5f)]
    [InlineData("shortValue", (short)123)]
    [InlineData("uintValue", (uint)123)]
    [InlineData("ulongValue", (ulong)123)]
    [InlineData("ushortValue", (ushort)456)]
    [InlineData("intValue", 789)]
    public void ValidateOptionalFields_支持多种数字类型_正确反序列化(string fieldName, object expectedValue)
    {
        var json = $@"{{""{fieldName}"": {JsonSerializer.Serialize(expectedValue)}}}";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        switch (expectedValue)
        {
            case long longValue:
                Assert.Equal(longValue, JsonValidateHelper.ValidateOptionalFields<long>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case double doubleValue:
                Assert.Equal(doubleValue, JsonValidateHelper.ValidateOptionalFields<double>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case float floatValue:
                Assert.Equal(floatValue, JsonValidateHelper.ValidateOptionalFields<float>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case short shortValue:
                Assert.Equal(shortValue, JsonValidateHelper.ValidateOptionalFields<short>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case uint uintValue:
                Assert.Equal(uintValue, JsonValidateHelper.ValidateOptionalFields<uint>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case ulong ulongValue:
                Assert.Equal(ulongValue, JsonValidateHelper.ValidateOptionalFields<ulong>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case ushort ushortValue:
                Assert.Equal(ushortValue, JsonValidateHelper.ValidateOptionalFields<ushort>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            case int intValue:
                Assert.Equal(intValue, JsonValidateHelper.ValidateOptionalFields<int>(root, "Test的", fieldName, JsonValueKind.Number));
                break;
            default:
                throw new InvalidOperationException("Unsupported numeric type");
        }
    }
    #endregion

    #region 参数校验异常测试（ArgumentException）
    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数prop为Null_抛出参数异常()
    {
        var json = "null";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "field", JsonValueKind.String));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数prop为Undefined_抛出参数异常()
    {
        var prop = default(JsonElement);

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(prop, "Test的", "field", JsonValueKind.String));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数name为null_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", null!, JsonValueKind.String));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数name为空_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", string.Empty, JsonValueKind.String));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void EnsurePropertyExistsAndTypeIsRight_参数name为空白或制表符或换行_抛出参数异常(string whitespace)
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", whitespace, JsonValueKind.String));
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数namePrefix为null_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, null!, "field", JsonValueKind.String));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数namePrefix为空_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, string.Empty, "field", JsonValueKind.String));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void EnsurePropertyExistsAndTypeIsRight_参数namePrefix为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, whitespace, "field", JsonValueKind.String));
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数expectedKind为Null_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "field", JsonValueKind.Null));
        Assert.Contains("expectedKind", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数expectedKind为Undefined_抛出参数异常()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "field", JsonValueKind.Undefined));
        Assert.Contains("expectedKind", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_参数T为不支持类型_抛出NotSupportedException()
    {
        var json = """{"field": "value"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<DateTime>(root, "Test的", "field", JsonValueKind.String));
        Assert.Contains("ValidateEnsurePropertyExistsAndTypeIsRightParams方法不支持类型", ex.Message);
    }
    #endregion

    #region JSON 数据异常测试（JsonException）
    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_字段不存在_抛出JsonException()
    {
        var json = """{"other": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "User的", "name", JsonValueKind.String));
        Assert.Contains("User的name字段缺少", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_字段为null_抛出JsonException()
    {
        var json = """{"name": null}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "User的", "name", JsonValueKind.String));
        Assert.Contains("User的name", ex.Message);
        Assert.Contains("JsonElement类型不能为JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_字段类型不匹配_抛出JsonException()
    {
        var json = """{"age": "not a number"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<int>(root, "User的", "age", JsonValueKind.Number));
        Assert.Contains("User的age", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_布尔期望但实际为数字_抛出JsonException()
    {
        var json = """{"flag": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(root, "Test的", "flag", JsonValueKind.True));

        Assert.Contains("字段必须为布尔类型", ex.Message);
    }

    #endregion

    #region 边界情况测试
    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_特殊字符字段_正常运行()
    {
        var json = @"{""user-name"": ""Alice"", ""user_id"": 123}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result1 = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "user-name", JsonValueKind.String);
        var result2 = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<int>(root, "Test的", "user_id", JsonValueKind.Number);

        Assert.Equal("Alice", result1);
        Assert.Equal(123, result2);
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_字段名区分大小写()
    {
        var json = @"{""UserName"": ""Alice""}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "UserName", JsonValueKind.String);
        Assert.Equal("Alice", result);

        Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Test的", "username", JsonValueKind.String));
    }

    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_访问嵌套对象字段()
    {
        var json = """
    {
        "user": {
            "name": "Alice",
            "address": { "city": "Beijing" }
        }
    }
    """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var user = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<JsonElement>(root, "Root的", "user", JsonValueKind.Object);
        var name = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(user, "User的", "name", JsonValueKind.String);
        var address = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<JsonElement>(user, "User的", "address", JsonValueKind.Object);
        var city = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(address, "Address的", "city", JsonValueKind.String);

        Assert.Equal("Alice", name);
        Assert.Equal("Beijing", city);
    }
    #endregion

    #region 实际使用场景测试
    [Fact]
    public void EnsurePropertyExistsAndTypeIsRight_Parameter实际应用_正常运行()
    {
        var json = """
    {
        "Label": "Parameter001",
        "DataType": 0,
        "StationNo": "",
        "Address": "x=1;0",
        "Length": 25,
        "DefaultValue": "",
        "Cycle": 1000,
        "PositiveExpression": "",
        "MinValue": "",
        "MaxValue": "",
        "DataFormat": 0,
        "AddressStartWithZero": true,
        "InstrumentType": 0
    }
    """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var label = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Parameter的", "Label", JsonValueKind.String);
        var address = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, "Parameter的", "Address", JsonValueKind.String);

        Assert.Equal("Parameter001", label);
        Assert.Equal("x=1;0", address);
    }
    #endregion

    #endregion

    #region EnsurePropertyExistsAndEnumIsRight

    public enum EnumTestStatus
    {
        None = 0,
        Success = 1,
        Failed = 2
    }

    #region 正常路径（Happy Path）
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_数字对应枚举值_返回枚举()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", "status");

        Assert.Equal(EnumTestStatus.Success, result);
    }
    #endregion

    #region 参数校验异常测试（ArgumentException）
    // prop为Null
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数prop为Null_抛出参数异常()
    {
        var json = "null";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", "status"));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Null", ex.Message);
    }

    // prop为Undefined
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数prop为Undefined_抛出参数异常()
    {
        var prop = default(JsonElement);

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(prop, "Test的", "status"));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Undefined", ex.Message);
    }

    // name为null
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数name为null_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", null!));
        _output.WriteLine(ex.Message);
    }

    // name为空
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数name为空_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", string.Empty));
    }

    // name为空白
    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void EnsurePropertyExistsAndEnumIsRight_参数name为空白或制表符或换行_抛出参数异常(string whitespace)
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", whitespace));
    }

    // namePrefix为null
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数namePrefix为null_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, null!, "status"));
        _output.WriteLine(ex.Message);
    }

    // namePrefix为空
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_参数namePrefix为空_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, string.Empty, "status"));
    }

    // namePrefix为空白
    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void EnsurePropertyExistsAndEnumIsRight_参数namePrefix为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, whitespace, "status"));
    }
    #endregion

    #region JSON 数据异常测试（JsonException）
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_字段为超出Int32范围的数字_抛出JsonException()
    {
        var json = """{"status": 9999999999}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<OptionalTestStatus>(root, "Test的", "status"));

        Assert.Contains("无法转换为数字", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    // 字段类型不是数字
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_字段类型不是数字_抛出JsonException()
    {
        var json = """{"status": "Success"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", "status"));
        Assert.Contains("Test的status", ex.Message);
        Assert.Contains("字段必须为数字", ex.Message);
    }

    // 字段不存在
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_字段不存在_抛出JsonException()
    {
        var json = """{"other": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", "status"));
        Assert.Contains("Test的status字段缺少", ex.Message);
    }

    // 数字超出枚举范围
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_数字超出枚举范围_抛出JsonException()
    {
        var json = """{"status": 99}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Test的", "status"));
        Assert.Contains("Test的status", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
    }
    #endregion

    #region 实际使用场景测试
    [Fact]
    public void EnsurePropertyExistsAndEnumIsRight_Parameter实际应用_正常运行()
    {
        var json = """
        {
            "Status": 2,
            "Label": "Parameter001"
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EnumTestStatus>(root, "Parameter的", "Status");

        Assert.Equal(EnumTestStatus.Failed, status);
    }
    #endregion

    #endregion

    #region ValidateOptionalFields

    #region 正常路径（Happy Path）
    [Fact]
    public void ValidateOptionalFields_字段存在且类型正确_返回值()
    {
        var json = """{"name": "Alice", "age": 18, "isActive": true, "tags": ["a", "b"]}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Alice", JsonValidateHelper.ValidateOptionalFields<string>(root, "User的", "name", JsonValueKind.String));
        Assert.Equal(18, JsonValidateHelper.ValidateOptionalFields<int>(root, "User的", "age", JsonValueKind.Number));
        Assert.True(JsonValidateHelper.ValidateOptionalFields<bool>(root, "User的", "isActive", JsonValueKind.True));
        var tags = JsonValidateHelper.ValidateOptionalFields<List<string>>(root, "User的", "tags", JsonValueKind.Array);
        Assert.NotNull(tags);
        Assert.Equal(2, tags!.Count);
    }
    #endregion

    #region 字段不存在或为null返回default
    [Fact]
    public void ValidateOptionalFields_字段不存在_返回default()
    {
        var json = """{"other": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Null(JsonValidateHelper.ValidateOptionalFields<string>(root, "User的", "name", JsonValueKind.String));
        Assert.Equal(0, JsonValidateHelper.ValidateOptionalFields<int>(root, "User的", "age", JsonValueKind.Number));
    }

    [Fact]
    public void ValidateOptionalFields_字段为null_返回default()
    {
        var json = """{"name": null, "age": null}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 字段为null时，作为可选字段应返回default
        Assert.Null(JsonValidateHelper.ValidateOptionalFields<string>(root, "User的", "name", JsonValueKind.String));
        Assert.Equal(0, JsonValidateHelper.ValidateOptionalFields<int>(root, "User的", "age", JsonValueKind.Number));
    }
    #endregion

    #region 参数校验异常测试（ArgumentException）
    [Fact]
    public void ValidateOptionalFields_参数root为Null_抛出参数异常()
    {
        var json = "null";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "name", JsonValueKind.String));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数root为Undefined_抛出参数异常()
    {
        var root = default(JsonElement);

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "name", JsonValueKind.String));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数name为null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", null!, JsonValueKind.String));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数name为空_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", string.Empty, JsonValueKind.String));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ValidateOptionalFields_参数name为空白或制表符或换行_抛出参数异常(string whitespace)
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", whitespace, JsonValueKind.String));
    }

    [Fact]
    public void ValidateOptionalFields_参数namePrefix为null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, null!, "name", JsonValueKind.String));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数namePrefix为空_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, string.Empty, "name", JsonValueKind.String));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ValidateOptionalFields_参数namePrefix为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, whitespace, "name", JsonValueKind.String));
    }

    [Fact]
    public void ValidateOptionalFields_参数expectedKind为Null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "name", JsonValueKind.Null));
        Assert.Contains("expectedKind", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数expectedKind为Undefined_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "name", JsonValueKind.Undefined));
        Assert.Contains("expectedKind", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_参数T为不支持类型_抛出NotSupportedException()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonValidateHelper.ValidateOptionalFields<DateTime>(root, "Test的", "name", JsonValueKind.String));
        Assert.Contains("ValidateEnsurePropertyExistsAndTypeIsRightParams方法不支持类型", ex.Message);
    }
    #endregion

    #region JSON 数据异常测试（JsonException）
    // 字段存在但类型不匹配
    [Fact]
    public void ValidateOptionalFields_字段存在但类型不匹配_抛出JsonException()
    {
        var json = """{"age": "not a number"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.ValidateOptionalFields<int>(root, "User的", "age", JsonValueKind.Number));
        Assert.Contains("User的age", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
    }
    #endregion

    #region 边界情况测试
    [Fact]
    public void ValidateOptionalFields_特殊字符字段_正常运行()
    {
        var json = @"{""user-name"": ""Alice"", ""user_id"": 123}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result1 = JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "user-name", JsonValueKind.String);
        var result2 = JsonValidateHelper.ValidateOptionalFields<int>(root, "Test的", "user_id", JsonValueKind.Number);

        Assert.Equal("Alice", result1);
        Assert.Equal(123, result2);
    }

    [Fact]
    public void ValidateOptionalFields_字段名区分大小写()
    {
        var json = @"{""UserName"": ""Alice""}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "UserName", JsonValueKind.String);
        Assert.Equal("Alice", result);

        Assert.Null(JsonValidateHelper.ValidateOptionalFields<string>(root, "Test的", "username", JsonValueKind.String));
    }

    [Fact]
    public void ValidateOptionalFields_访问嵌套对象字段()
    {
        var json = """
        {
            "user": {
                "name": "Alice",
                "address": { "city": "Beijing" }
            }
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var user = JsonValidateHelper.ValidateOptionalFields<JsonElement>(root, "Root的", "user", JsonValueKind.Object);
        Assert.Equal(JsonValueKind.Object, user.ValueKind);

        var name = JsonValidateHelper.ValidateOptionalFields<string>(user, "User的", "name", JsonValueKind.String);
        var address = JsonValidateHelper.ValidateOptionalFields<JsonElement>(user, "User的", "address", JsonValueKind.Object);
        var city = JsonValidateHelper.ValidateOptionalFields<string>(address, "Address的", "city", JsonValueKind.String);

        Assert.Equal("Alice", name);
        Assert.Equal("Beijing", city);
    }
    #endregion

    #region 实际使用场景测试
    [Fact]
    public void ValidateOptionalFields_Parameter实际应用_正常运行()
    {
        var json = """
        {
            "Label": "Parameter001",
            "DataType": 0,
            "StationNo": "",
            "Address": "x=1;0",
            "Length": 25,
            "DefaultValue": "",
            "Cycle": 1000,
            "PositiveExpression": "",
            "MinValue": "",
            "MaxValue": "",
            "DataFormat": 0,
            "AddressStartWithZero": true,
            "InstrumentType": 0
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var label = JsonValidateHelper.ValidateOptionalFields<string>(root, "Parameter的", "Label", JsonValueKind.String);
        var address = JsonValidateHelper.ValidateOptionalFields<string>(root, "Parameter的", "Address", JsonValueKind.String);
        var notExist = JsonValidateHelper.ValidateOptionalFields<string>(root, "Parameter的", "NotExist", JsonValueKind.String);

        Assert.Equal("Parameter001", label);
        Assert.Equal("x=1;0", address);
        Assert.Null(notExist);
    }
    #endregion

    #endregion

    #region ValidateOptionalFields_批量校验重载

    #region 正常路径（Happy Path）
    [Fact]
    public void VoidValidateOptionalFields_所有字段存在且类型正确_不抛异常()
    {
        var json = """{"name": "Alice", "age": 18, "isActive": true, "tags": ["a", "b"]}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 不抛异常即为通过
        JsonValidateHelper.ValidateOptionalFields(
            root, "User的",
            ("name", JsonValueKind.String),
            ("age", JsonValueKind.Number),
            ("isActive", JsonValueKind.True),
            ("tags", JsonValueKind.Array)
        );
    }
    #endregion

    #region 字段不存在或为null_不抛异常
    [Fact]
    public void VoidValidateOptionalFields_部分字段不存在或为null_不抛异常()
    {
        var json = """{"name": "Alice", "age": null}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // age不存在或为null不会抛异常
        JsonValidateHelper.ValidateOptionalFields(
            root, "User的",
            ("name", JsonValueKind.String),
            ("age", JsonValueKind.Number),
            ("notExist", JsonValueKind.String)
        );
    }
    #endregion

    #region 参数校验异常测试（ArgumentException）
    [Fact]
    public void VoidValidateOptionalFields_参数root为Null_抛出参数异常()
    {
        var json = "null";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("name", JsonValueKind.String)));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void VoidValidateOptionalFields_参数root为Undefined_抛出参数异常()
    {
        var root = default(JsonElement);

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("name", JsonValueKind.String)));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void VoidValidateOptionalFields_参数namePrefix为null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, null!, ("name", JsonValueKind.String)));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void VoidValidateOptionalFields_参数namePrefix为空_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, string.Empty, ("name", JsonValueKind.String)));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void VoidValidateOptionalFields_参数namePrefix为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, whitespace, ("name", JsonValueKind.String)));
    }

    [Fact]
    public void VoidValidateOptionalFields_参数fields为null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", null!));
    }

    [Fact]
    public void VoidValidateOptionalFields_参数fields中Name为null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", (null!, JsonValueKind.String)));
    }

    [Fact]
    public void VoidValidateOptionalFields_参数fields中Name为空_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", (string.Empty, JsonValueKind.String)));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void VoidValidateOptionalFields_参数fields中Name为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", (whitespace, JsonValueKind.String)));
    }

    [Fact]
    public void VoidValidateOptionalFields_参数fields中Kind为Null_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("name", JsonValueKind.Null)));
    }

    [Fact]
    public void VoidValidateOptionalFields_参数fields中Kind为Undefined_抛出参数异常()
    {
        var json = """{"name": "Alice"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("name", JsonValueKind.Undefined)));
    }
    #endregion

    #region JSON 数据异常测试（JsonException）
    // 字段存在但类型不匹配
    [Fact]
    public void VoidValidateOptionalFields_字段存在但类型不匹配_抛出JsonException()
    {
        var json = """{"age": "not a number"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "User的", ("age", JsonValueKind.Number)));
        Assert.Contains("User的age", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_期望对象但实际为字符串_抛出JsonException()
    {
        var json = """{"address": "not object"}""";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "User的", ("address", JsonValueKind.Object)));

        Assert.Contains("字段必须为对象", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_期望字符串但实际为数字_抛出JsonException()
    {
        var json = """{"name": 123}""";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "User的", ("name", JsonValueKind.String)));

        Assert.Contains("字段必须为字符串", ex.Message);
    }

    [Fact]
    public void ValidateOptionalFields_期望数组但实际为字符串_抛出JsonException()
    {
        var json = """{"tags": "not array"}""";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.ValidateOptionalFields(root, "User的", ("tags", JsonValueKind.Array)));

        Assert.Contains("字段必须为数组", ex.Message);
    }
    #endregion

    #region 边界情况测试
    [Fact]
    public void VoidValidateOptionalFields_特殊字符字段_正常运行()
    {
        var json = @"{""user-name"": ""Alice"", ""user_id"": 123}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        JsonValidateHelper.ValidateOptionalFields(root, "Test的",
            ("user-name", JsonValueKind.String),
            ("user_id", JsonValueKind.Number)
        );
    }

    [Fact]
    public void VoidValidateOptionalFields_字段名区分大小写()
    {
        var json = @"{""UserName"": ""Alice""}";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("UserName", JsonValueKind.String));
        // 不存在的字段不会抛异常
        JsonValidateHelper.ValidateOptionalFields(root, "Test的", ("username", JsonValueKind.String));
    }

    [Fact]
    public void VoidValidateOptionalFields_访问嵌套对象字段()
    {
        var json = """
        {
            "user": {
                "name": "Alice",
                "address": { "city": "Beijing" }
            }
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        JsonValidateHelper.ValidateOptionalFields(root, "Root的", ("user", JsonValueKind.Object));
        var user = root.GetProperty("user");
        JsonValidateHelper.ValidateOptionalFields(user, "User的", ("name", JsonValueKind.String), ("address", JsonValueKind.Object));
        var address = user.GetProperty("address");
        JsonValidateHelper.ValidateOptionalFields(address, "Address的", ("city", JsonValueKind.String));
    }
    #endregion

    #region 实际使用场景测试
    [Fact]
    public void VoidValidateOptionalFields_Parameter实际应用_正常运行()
    {
        var json = """
        {
            "Label": "Parameter001",
            "DataType": 0,
            "StationNo": "",
            "Address": "x=1;0",
            "Length": 25,
            "DefaultValue": "",
            "Cycle": 1000,
            "PositiveExpression": "",
            "MinValue": "",
            "MaxValue": "",
            "DataFormat": 0,
            "AddressStartWithZero": true,
            "InstrumentType": 0
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        JsonValidateHelper.ValidateOptionalFields(root, "Parameter的",
            ("Label", JsonValueKind.String),
            ("DataType", JsonValueKind.Number),
            ("StationNo", JsonValueKind.String),
            ("Address", JsonValueKind.String),
            ("Length", JsonValueKind.Number),
            ("DefaultValue", JsonValueKind.String),
            ("Cycle", JsonValueKind.Number),
            ("PositiveExpression", JsonValueKind.String),
            ("MinValue", JsonValueKind.String),
            ("MaxValue", JsonValueKind.String),
            ("DataFormat", JsonValueKind.Number),
            ("AddressStartWithZero", JsonValueKind.True),
            ("InstrumentType", JsonValueKind.Number)
        );
    }
    #endregion

    #endregion

    #region GetOptionalEnum

    #region 正常路径（Happy Path）
    [Fact]
    public void GetOptionalEnum_字段存在且为有效枚举值_返回枚举()
    {
        var json = """{"status": 2}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status");

        Assert.True(result.HasValue);
        Assert.Equal(OptionalTestStatus.Failed, result.Value);
    }

    [Fact]
    public void GetOptionalEnum_字段不存在_返回null()
    {
        var json = """{"other": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status");

        Assert.Null(result);
    }

    [Fact]
    public void GetOptionalEnum_字段为null_返回null()
    {
        var json = """{"status": null}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status");

        Assert.Null(result);
    }
    #endregion

    #region 参数校验异常测试（ArgumentException）
    [Fact]
    public void GetOptionalEnum_参数root为Null_抛出参数异常()
    {
        var json = "null";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement; // ValueKind = Null

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status"));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Null", ex.Message);
    }

    [Fact]
    public void GetOptionalEnum_参数root为Undefined_抛出参数异常()
    {
        var root = default(JsonElement); // ValueKind = Undefined

        var ex = Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status"));
        Assert.Contains("prop", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JsonElement类型的参数prop不能为JsonValueKind.Undefined", ex.Message);
    }

    [Fact]
    public void GetOptionalEnum_参数name为null_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", null!));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void GetOptionalEnum_参数name为空_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", string.Empty));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void GetOptionalEnum_参数name为空白或制表符或换行_抛出参数异常(string whitespace)
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", whitespace));
    }

    [Fact]
    public void GetOptionalEnum_参数namePrefix为null_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, null!, "status"));
        _output.WriteLine(ex.Message);
    }

    [Fact]
    public void GetOptionalEnum_参数namePrefix为空_抛出参数异常()
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, string.Empty, "status"));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void GetOptionalEnum_参数namePrefix为空白或制表符_抛出参数异常(string whitespace)
    {
        var json = """{"status": 1}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Throws<ArgumentException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, whitespace, "status"));
    }
    #endregion

    #region JSON 数据异常测试（JsonException）
    [Fact]
    public void GetOptionalEnum_字段为超出Int32范围的数字_抛出JsonException()
    {
        var json = """{"status": 9999999999}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status"));

        Assert.Contains("无法转换为数字", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Fact]
    public void GetOptionalEnum_字段存在但类型不是数字_抛出JsonException()
    {
        var json = """{"status": "Success"}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status"));
        Assert.Contains("Test的status", ex.Message);
        Assert.Contains("必须为数字类型", ex.Message);
    }

    [Fact]
    public void GetOptionalEnum_字段存在但值超出枚举范围_抛出JsonException()
    {
        var json = """{"status": 99}""";
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ex = Assert.Throws<JsonException>(() =>
            JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Test的", "status"));
        Assert.Contains("Test的status", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
    }
    #endregion

    #region 实际使用场景测试
    [Fact]
    public void GetOptionalEnum_Parameter实际应用_正常运行()
    {
        var json = """
        {
            "Status": 1,
            "Label": "Parameter001"
        }
        """;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Parameter的", "Status");
        var notExist = JsonValidateHelper.GetOptionalEnum<OptionalTestStatus>(root, "Parameter的", "NotExist");

        Assert.True(status.HasValue);
        Assert.Equal(OptionalTestStatus.Success, status.Value);
        Assert.Null(notExist);
    }
    #endregion

    #endregion

}