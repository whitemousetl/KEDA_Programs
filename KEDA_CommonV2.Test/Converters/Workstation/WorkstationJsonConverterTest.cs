using KEDA_CommonV2.Converters.Workstation;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Converters.Workstation;

public class WorkstationJsonConverterTest
{
    private static JsonSerializerOptions _options = JsonOptionsProvider.WorkstationJsonOptions;

    #region 正常反序列化测试

    [Fact]
    public void Read_ValidWorkstation_WithAllFields_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": "边缘服务器1",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 0,
                    "ProtocolType": 13,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 2404,
                    "Equipments": []
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ws-001", result.Id);
        Assert.Equal("边缘服务器1", result.Name);
        Assert.Equal("192.168.1.100", result.IpAddress);
        Assert.Single(result.Protocols);
        Assert.Equal("lan-001", result.Protocols[0].Id);
    }

    [Fact]
    public void Read_ValidWorkstation_WithoutOptionalName_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-002",
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ws-002", result.Id);
        Assert.Equal(string.Empty, result.Name);
        Assert.Equal("192.168.1.100", result.IpAddress);
        Assert.Empty(result.Protocols);
    }

    [Fact]
    public void Read_ValidWorkstation_WithEmptyProtocols_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-003",
            "Name": "测试边缘",
            "IpAddress": "10.0.0.1",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ws-003", result.Id);
        Assert.Equal("测试边缘", result.Name);
        Assert.Equal("10.0.0.1", result.IpAddress);
        Assert.Empty(result.Protocols);
    }

    [Fact]
    public void Read_ValidWorkstation_WithMultipleProtocols_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-multi",
            "Name": "多协议边缘",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 0,
                    "ProtocolType": 13,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 2404,
                    "Equipments": []
                },
                {
                    "Id": "api-001",
                    "InterfaceType": 2,
                    "ProtocolType": 200,
                    "AccessApiString": "http://localhost:8080/api",
                    "RequestMethod": 0,
                    "Equipments": []
                },
                {
                    "Id": "db-001",
                    "InterfaceType": 3,
                    "ProtocolType": 300,
                    "QuerySqlString": "SELECT * FROM sensors",
                    "Equipments": []
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ws-multi", result.Id);
        Assert.Equal(3, result.Protocols.Count);
        Assert.IsType<LanProtocolDto>(result.Protocols[0]);
        Assert.IsType<ApiProtocolDto>(result.Protocols[1]);
        Assert.IsType<DatabaseProtocolDto>(result.Protocols[2]);
    }

    [Fact]
    public void Read_ValidWorkstation_WithCompleteProtocolAndEquipments_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-complete",
            "Name": "完整边缘",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 0,
                    "ProtocolType": 0,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 502,
                    "Equipments": [
                        {
                            "Id": "eq-001",
                            "EquipmentType": 0,
                            "IsCollect": true,
                            "Parameters": [
                                {
                                    "Label": "温度",
                                    "StationNo": "1",
                                    "DataFormat": 0,
                                    "DataType": 7,
                                    "AddressStartWithZero": true,
                                    "Address": "40001"
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Protocols);
        Assert.Single(result.Protocols[0].Equipments);
        Assert.Single(result.Protocols[0].Equipments[0].Parameters);
        Assert.Equal("温度", result.Protocols[0].Equipments[0].Parameters[0].Label);
    }

    #endregion

    #region 必填字段缺失测试

    [Fact]
    public void Read_MissingId_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Name": "边缘服务器",
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Id", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_MissingIpAddress_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": "边缘服务器",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("IpAddress", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_MissingProtocols_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": "边缘服务器",
            "IpAddress": "192.168.1.100"
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Protocols", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_MissingAllRequiredFields_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Name": "边缘服务器"
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        // 应该首先报告第一个缺失的必填字段
        Assert.Contains("Workstatoin", exception.Message);
    }

    #endregion

    #region 字段类型错误测试

    [Fact]
    public void Read_IdNotString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": 123,
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Id", exception.Message);
        Assert.Contains("必须为字符串", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_IpAddressNotString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": 12345,
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("IpAddress", exception.Message);
        Assert.Contains("必须为字符串", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_ProtocolsNotArray_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": "not-an-array"
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Protocols", exception.Message);
        Assert.Contains("必须为数组", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_ProtocolsAsNumber_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": 123
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Protocols", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_NameNotString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": 123,
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Name", exception.Message);
        Assert.Contains("必须为字符串", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_NameAsArray_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": ["a", "b"],
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Name", exception.Message);
        Assert.Contains("Workstatoin", exception.Message);
    }

    #endregion

    #region 协议内容校验测试

    [Fact]
    public void Read_ProtocolMissingRequiredField_ShouldThrowJsonException()
    {
        // Arrange - 协议缺少 Id
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "InterfaceType": 0,
                    "ProtocolType": 0,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 502,
                    "Equipments": []
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Id", exception.Message);
        Assert.Contains("Protocol", exception.Message);
    }

    [Fact]
    public void Read_ProtocolWithInvalidInterfaceType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 877,
                    "ProtocolType": 0,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 502,
                    "Equipments": []
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("InterfaceType", exception.Message);
        Assert.Contains("超出有效范围", exception.Message);
    }

    [Fact]
    public void Read_ProtocolTypeMismatch_ShouldThrowJsonException()
    {
        // Arrange - LAN 接口下使用 COM 协议
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 0,
                    "ProtocolType": 100,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 502,
                    "Equipments": []
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("接口类型LAN下不支持协议类型ModbusRtu", exception.Message);
    }

    #endregion

    #region 序列化测试

    [Fact]
    public void Write_Workstation_ShouldThrowNotImplementedException()
    {
        // Arrange
        var workstation = new WorkstationDto
        {
            Id = "ws-001",
            Name = "边缘服务器",
            IpAddress = "192.168.1.100",
            Protocols = []
        };

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(workstation, _options));
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void Read_EmptyJson_ShouldThrowJsonException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Workstatoin", exception.Message);
    }

    [Fact]
    public void Read_NullValues_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": null,
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkstationDto>(json, _options));
        Assert.Contains("Id", exception.Message);
    }

    [Fact]
    public void Read_EmptyStringId_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "",
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Id);
    }

    [Fact]
    public void Read_EmptyStringIpAddress_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.IpAddress);
    }

    [Fact]
    public void Read_NameAsNull_ShouldUseEmptyString()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "Name": null,
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Name);
    }

    [Fact]
    public void Read_ExtraFields_ShouldBeIgnored()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-001",
            "IpAddress": "192.168.1.100",
            "Protocols": [],
            "ExtraField1": "value1",
            "ExtraField2": 123
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ws-001", result.Id);
        Assert.Equal("192.168.1.100", result.IpAddress);
    }

    [Fact]
    public void Read_UnicodeCharacters_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "边缘-001",
            "Name": "测试边缘服务器🚀",
            "IpAddress": "192.168.1.100",
            "Protocols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("边缘-001", result.Id);
        Assert.Equal("测试边缘服务器🚀", result.Name);
    }

    #endregion

    #region 混合协议类型测试

    [Fact]
    public void Read_WorkstationWithAllProtocolTypes_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "ws-all-protocols",
            "Name": "全协议边缘",
            "IpAddress": "192.168.1.100",
            "Protocols": [
                {
                    "Id": "lan-001",
                    "InterfaceType": 0,
                    "ProtocolType": 0,
                    "IpAddress": "192.168.1.101",
                    "ProtocolPort": 502,
                    "Equipments": [
                        {
                            "Id": "eq-001",
                            "EquipmentType": 0,
                            "IsCollect": true,
                            "Parameters": [
                                {
                                    "Label": "温度",
                                    "StationNo": "1",
                                    "DataFormat": 0,
                                    "DataType": 7,
                                    "AddressStartWithZero": true,
                                    "Address": "40001"
                                }
                            ]
                        }
                    ]
                },
                {
                    "Id": "com-001",
                    "InterfaceType": 1,
                    "ProtocolType": 100,
                    "SerialPortName": "COM1",
                    "BaudRate": 9600,
                    "DataBits": 8,
                    "Parity": 0,
                    "StopBits": 1,
                    "Equipments": [
                        {
                            "Id": "eq-002",
                            "EquipmentType": 0,
                            "IsCollect": true,
                            "Parameters": [
                                {
                                    "Label": "电流",
                                    "StationNo": "1",
                                    "DataFormat": 0,
                                    "DataType": 7,
                                    "AddressStartWithZero": true,
                                    "Address": "40001"
                                }
                            ]
                        }
                    ]
                },
                {
                    "Id": "api-001",
                    "InterfaceType": 2,
                    "ProtocolType": 200,
                    "AccessApiString": "http://localhost:8080/api/data",
                    "RequestMethod": 0,
                    "Equipments": [
                        {
                            "Id": "eq-003",
                            "EquipmentType": 0,
                            "IsCollect": true,
                            "Parameters": [
                                {
                                    "Label": "状态",
                                    "Address": "$.status"
                                }
                            ]
                        }
                    ]
                },
                {
                    "Id": "db-001",
                    "InterfaceType": 3,
                    "ProtocolType": 300,
                    "QuerySqlString": "SELECT * FROM sensors",
                    "Equipments": [
                        {
                            "Id": "eq-004",
                            "EquipmentType": 0,
                            "IsCollect": true,
                            "Parameters": [
                                {
                                    "Label": "温度",
                                    "Address": "temperature"
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkstationDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Protocols.Count);
        Assert.IsType<LanProtocolDto>(result.Protocols[0]);
        Assert.IsType<SerialProtocolDto>(result.Protocols[1]);
        Assert.IsType<ApiProtocolDto>(result.Protocols[2]);
        Assert.IsType<DatabaseProtocolDto>(result.Protocols[3]);

        // 验证各协议详细信息
        var lanProtocol = (LanProtocolDto)result.Protocols[0];
        Assert.Equal(ProtocolType.ModbusTcpNet, lanProtocol.ProtocolType);
        Assert.Equal("192.168.1.101", lanProtocol.IpAddress);

        var serialProtocol = (SerialProtocolDto)result.Protocols[1];
        Assert.Equal(ProtocolType.ModbusRtu, serialProtocol.ProtocolType);
        Assert.Equal("COM1", serialProtocol.SerialPortName);

        var apiProtocol = (ApiProtocolDto)result.Protocols[2];
        Assert.Equal(ProtocolType.Api, apiProtocol.ProtocolType);
        Assert.Equal("http://localhost:8080/api/data", apiProtocol.AccessApiString);

        var dbProtocol = (DatabaseProtocolDto)result.Protocols[3];
        Assert.Equal(ProtocolType.MySQL, dbProtocol.ProtocolType);
        Assert.Equal("SELECT * FROM sensors", dbProtocol.QuerySqlString);
    }

    #endregion
}