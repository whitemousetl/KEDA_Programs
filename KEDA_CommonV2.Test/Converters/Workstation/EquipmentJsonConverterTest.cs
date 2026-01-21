using KEDA_CommonV2.Converters.Workstation;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Converters.Workstation;

public class EquipmentJsonConverterTest
{
    private readonly JsonSerializerOptions _options = JsonOptionsProvider.WorkstationJsonOptions;

    #region 完整JSON解析测试

    [Fact]
    public void Read_AllFieldsProvided_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "Name": "测试设备",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": [
                {
                    "Label": "Temperature",
                    "Address": "D100"
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EQ001", result.Id);
        Assert.Equal("测试设备", result.Name);
        Assert.Equal(EquipmentType.Equipment, result.EquipmentType);
        Assert.Single(result.Parameters);
        Assert.Equal("Temperature", result.Parameters[0].Label);
        Assert.Equal("D100", result.Parameters[0].Address);
    }

    [Fact]
    public void Read_OnlyRequiredFields_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ002",
            "EquipmentType": 1,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EQ002", result.Id);
        Assert.Equal(string.Empty, result.Name);
        Assert.Equal(EquipmentType.Instrument, result.EquipmentType);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void Read_MultipleParameters_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ003",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": [
                { "Label": "Param1", "Address": "D100" },
                { "Label": "Param2", "Address": "D200" },
                { "Label": "Param3", "Address": "D300" }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Parameters.Count);
    }

    #endregion

    #region 必填字段缺失测试

    [Fact]
    public void Read_MissingId_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "EquipmentType": 0,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Id", ex.Message);
        Assert.Contains("缺少", ex.Message);
        Assert.Contains("Equipment", ex.Message);
    }

    [Fact]
    public void Read_MissingEquipmentType_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("EquipmentType", ex.Message);
        Assert.Contains("缺少", ex.Message);
        Assert.Contains("Equipment", ex.Message);
    }

    [Fact]
    public void Read_MissingParameters_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "IsCollect": true,
            "EquipmentType": 0
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Parameters", ex.Message);
        Assert.Contains("缺少", ex.Message);
        Assert.Contains("Equipment", ex.Message);
    }

    [Fact]
    public void Read_NullId_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": null,
            "EquipmentType": 0,
            "Parameters": []
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
    }

    [Fact]
    public void Read_NullEquipmentType_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": null,
            "Parameters": []
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
    }

    [Fact]
    public void Read_NullParameters_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": 0,
            "Parameters": null
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
    }

    #endregion

    #region 类型错误测试

    [Fact]
    public void Read_IdIsNumber_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": 123,
            "EquipmentType": 0,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Id", ex.Message);
    }

    [Fact]
    public void Read_ParametersIsNotArray_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": "not an array"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Parameters", ex.Message);
    }

    [Fact]
    public void Read_ParametersIsObject_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": { "Label": "Test" }
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Parameters", ex.Message);
    }

    [Fact]
    public void Read_NameIsNumber_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "Name": 123,
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("Name", ex.Message);
    }

    #endregion

    #region 枚举字段测试

    [Fact]
    public void Read_EquipmentTypeIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": "Equipment",
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("EquipmentType", ex.Message);
    }

    [Fact]
    public void Read_EquipmentTypeInvalidValue_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": 99998,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("EquipmentType", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
    }

    [Theory]
    [InlineData(0, EquipmentType.Equipment)]
    [InlineData(1, EquipmentType.Instrument)]
    public void Read_ValidEquipmentType_ReturnsCorrectEnum(int value, EquipmentType expected)
    {
        // Arrange
        var json = $$"""
        {
            "Id": "EQ001",
            "IsCollect": true,
            "EquipmentType": {{value}},
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result.EquipmentType);
    }

    #endregion

    #region 可选字段测试

    [Fact]
    public void Read_NameNotProvided_ReturnsEmptyString()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Name);
    }

    [Fact]
    public void Read_NameIsNull_ReturnsEmptyString()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "Name": null,
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Name);
    }

    [Fact]
    public void Read_NameIsEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ001",
            "Name": "",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Name);
    }

    #endregion

    #region 边界值测试

    [Fact]
    public void Read_EmptyId_ReturnsEmptyString()
    {
        // Arrange
        var json = """
        {
            "Id": "",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Id);
    }

    [Fact]
    public void Read_SpecialCharactersInId_ReturnsCorrectValue()
    {
        // Arrange
        var json = """
        {
            "Id": "EQ-001_测试/设备",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EQ-001_测试/设备", result.Id);
    }

    [Fact]
    public void Read_LongName_ReturnsCorrectValue()
    {
        // Arrange
        var longName = new string('A', 1000);
        var json = $$"""
        {
            "Id": "EQ001",
            "Name": "{{longName}}",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longName, result.Name);
    }

    #endregion

    #region 字段名大小写测试

    [Fact]
    public void Read_FieldNameCaseSensitive_ThrowsJsonException()
    {
        // Arrange - 使用小写字段名
        var json = """
        {
            "id": "EQ001",
            "equipmentType": 0,
            "parameters": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EquipmentDto>(json, _options));
        Assert.Contains("缺少", ex.Message);
    }

    #endregion

    #region Write方法测试

    [Fact]
    public void Write_ThrowsNotImplementedException()
    {
        // Arrange
        var dto = new EquipmentDto
        {
            Id = "EQ001",
            Name = "测试设备",
            EquipmentType = EquipmentType.Equipment,
            Parameters = []
        };

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            JsonSerializer.Serialize(dto, _options));
    }

    #endregion

    #region 实际应用场景测试

    [Fact]
    public void Read_ComplexEquipmentWithMultipleParameters_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Id": "PLC001",
            "Name": "主控PLC",
            "EquipmentType": 0,
            "IsCollect": true,
            "Parameters": [
                {
                    "Label": "Temperature",
                    "Address": "D100",
                    "DataType": 1,
                    "Length": 10
                },
                {
                    "Label": "Pressure",
                    "Address": "D200",
                    "DataType": 2,
                    "Cycle": 1000
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PLC001", result.Id);
        Assert.Equal("主控PLC", result.Name);
        Assert.Equal(EquipmentType.Equipment, result.EquipmentType);
        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("Temperature", result.Parameters[0].Label);
        Assert.Equal("Pressure", result.Parameters[1].Label);
    }

    [Fact]
    public void Read_InstrumentType_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Id": "METER001",
            "Name": "水表",
            "EquipmentType": 1,
            "IsCollect": true,
            "Parameters": [
                {
                    "Label": "Flow",
                    "Address": "M100"
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<EquipmentDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EquipmentType.Instrument, result.EquipmentType);
    }

    #endregion
}
