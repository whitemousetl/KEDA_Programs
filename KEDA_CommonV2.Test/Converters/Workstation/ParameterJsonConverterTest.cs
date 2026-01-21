using HslCommunication.Core;
using KEDA_CommonV2.Converters.Workstation;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using System.Text.Json;
using KEDA_CommonV2.Utilities;

namespace KEDA_CommonV2.Test.Converters.Workstation;

public class ParameterJsonConverterTest
{
    private readonly JsonSerializerOptions _options = JsonOptionsProvider.WorkstationJsonOptions;

    #region 完整JSON解析测试

    [Fact]
    public void Read_AllFieldsProvided_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "StationNo": "001",
            "DataType": 1,
            "Address": "D100",
            "Length": 10,
            "DefaultValue": "0",
            "Cycle": 1000,
            "PositiveExpression": "x*2+1",
            "MinValue": "0",
            "MaxValue": "100",
            "DataFormat": 0,
            "AddressStartWithZero": true,
            "InstrumentType": 16,
            "Value": "25.5"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Temperature", result.Label);
        Assert.Equal("001", result.StationNo);
        Assert.Equal((DataType)1, result.DataType);
        Assert.Equal("D100", result.Address);
        Assert.Equal((ushort)10, result.Length);
        Assert.Equal("0", result.DefaultValue);
        Assert.Equal(1000, result.Cycle);
        Assert.Equal("x*2+1", result.PositiveExpression);
        Assert.Equal("0", result.MinValue);
        Assert.Equal("100", result.MaxValue);
        Assert.Equal((DataFormat)0, result.DataFormat);
        Assert.True(result.AddressStartWithZero);
        Assert.Equal((InstrumentType)16, result.InstrumentType);
        Assert.Equal("25.5", result.Value);
    }

    [Fact]
    public void Read_AllFieldsProvided_ReturnsCorrectDtoWithIsMonitor()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "StationNo": "001",
            "DataType": 1,
            "IsMonitor": true,
            "Address": "D100",
            "Length": 10,
            "DefaultValue": "0",
            "Cycle": 1000,
            "PositiveExpression": "x*2+1",
            "MinValue": "0",
            "MaxValue": "100",
            "DataFormat": 0,
            "AddressStartWithZero": true,
            "InstrumentType": 16,
            "Value": "25.5"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Temperature", result.Label);
        Assert.True(result.IsMonitor);
        Assert.Equal("001", result.StationNo);
        Assert.Equal((DataType)1, result.DataType);
        Assert.Equal("D100", result.Address);
        Assert.Equal((ushort)10, result.Length);
        Assert.Equal("0", result.DefaultValue);
        Assert.Equal(1000, result.Cycle);
        Assert.Equal("x*2+1", result.PositiveExpression);
        Assert.Equal("0", result.MinValue);
        Assert.Equal("100", result.MaxValue);
        Assert.Equal((DataFormat)0, result.DataFormat);
        Assert.True(result.AddressStartWithZero);
        Assert.Equal((InstrumentType)16, result.InstrumentType);
        Assert.Equal("25.5", result.Value);
    }

    [Fact]
    public void Read_OnlyRequiredFields_ReturnsCorrectDto()
    {
        // Arrange
        var json = """
        {
            "Label": "Pressure",
            "Address": "D200"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pressure", result.Label);
        Assert.Equal("D200", result.Address);
        Assert.Equal(string.Empty, result.StationNo);
        Assert.Null(result.DataType);
        Assert.Equal((ushort)0, result.Length);
        Assert.Equal(string.Empty, result.DefaultValue);
        Assert.Equal(0, result.Cycle);
        Assert.Equal(string.Empty, result.PositiveExpression);
        Assert.Equal(string.Empty, result.MinValue);
        Assert.Equal(string.Empty, result.MaxValue);
        Assert.Null(result.DataFormat);
        Assert.Null(result.AddressStartWithZero);
        Assert.Null(result.InstrumentType);
        Assert.Equal(string.Empty, result.Value);
    }

    #endregion

    #region 必填字段缺失测试

    [Fact]
    public void Read_MissingLabel_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Address": "D100"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Label", ex.Message);
        Assert.Contains("缺少", ex.Message);
        Assert.Contains("Parameter", ex.Message);
    }

    [Fact]
    public void Read_MissingAddress_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Address", ex.Message);
        Assert.Contains("缺少", ex.Message);
        Assert.Contains("Parameter", ex.Message);
    }

    [Fact]
    public void Read_NullLabel_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": null,
            "Address": "D100"
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
    }

    [Fact]
    public void Read_NullAddress_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": null
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
    }

    #endregion

    #region 类型错误测试

    [Fact]
    public void Read_LabelIsNumber_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": 123,
            "Address": "D100"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Label", ex.Message);
    }

    [Fact]
    public void Read_AddressIsNumber_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": 100
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Address", ex.Message);
    }

    [Fact]
    public void Read_LengthIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "Length": "10"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Length", ex.Message);
    }

    [Fact]
    public void Read_CycleIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "Cycle": "1000"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("Cycle", ex.Message);
    }

    [Fact]
    public void Read_AddressStartWithZeroIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "AddressStartWithZero": "true"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("AddressStartWithZero", ex.Message);
    }

    #endregion

    #region 枚举字段测试

    [Fact]
    public void Read_DataTypeIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "DataType": "Int16"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("DataType", ex.Message);
    }

    [Fact]
    public void Read_DataTypeInvalidValue_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "DataType": 99999
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("DataType", ex.Message);
    }

    [Fact]
    public void Read_DataFormatIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "DataFormat": "ABCD"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("DataFormat", ex.Message);
    }

    [Fact]
    public void Read_InstrumentTypeIsString_ThrowsJsonException()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "InstrumentType": "WaterMeter"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ParameterDto>(json, _options));
        Assert.Contains("InstrumentType", ex.Message);
    }

    [Fact]
    public void Read_NullEnumFields_ReturnsNull()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "DataType": null,
            "DataFormat": null,
            "InstrumentType": null
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DataType);
        Assert.Null(result.DataFormat);
        Assert.Null(result.InstrumentType);
    }

    #endregion

    #region 可选字段为null测试

    [Fact]
    public void Read_OptionalStringFieldsAreNull_ReturnsEmptyStrings()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "StationNo": null,
            "DefaultValue": null,
            "PositiveExpression": null,
            "MinValue": null,
            "MaxValue": null,
            "Value": null
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.StationNo);
        Assert.Equal(string.Empty, result.DefaultValue);
        Assert.Equal(string.Empty, result.PositiveExpression);
        Assert.Equal(string.Empty, result.MinValue);
        Assert.Equal(string.Empty, result.MaxValue);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Read_OptionalNumericFieldsAreNull_ReturnsDefaults()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "Length": null,
            "Cycle": null,
            "AddressStartWithZero": null
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((ushort)0, result.Length);
        Assert.Equal(0, result.Cycle);
        Assert.Null(result.AddressStartWithZero);
    }

    #endregion

    #region 边界值测试

    [Fact]
    public void Read_EmptyStringFields_ReturnsEmptyStrings()
    {
        // Arrange
        var json = """
        {
            "Label": "",
            "Address": "",
            "StationNo": "",
            "Value": ""
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Label);
        Assert.Equal(string.Empty, result.Address);
        Assert.Equal(string.Empty, result.StationNo);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Read_ZeroNumericValues_ReturnsZeros()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "Length": 0,
            "Cycle": 0
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((ushort)0, result.Length);
        Assert.Equal(0, result.Cycle);
    }

    [Fact]
    public void Read_AddressStartWithZeroFalse_ReturnsFalse()
    {
        // Arrange
        var json = """
        {
            "Label": "Temperature",
            "Address": "D100",
            "AddressStartWithZero": false
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ParameterDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AddressStartWithZero);
    }

    #endregion

    #region Write方法测试

    [Fact]
    public void Write_ThrowsNotImplementedException()
    {
        // Arrange
        var dto = new ParameterDto
        {
            Label = "Temperature",
            Address = "D100"
        };

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            JsonSerializer.Serialize(dto, _options));
    }

    #endregion
}
