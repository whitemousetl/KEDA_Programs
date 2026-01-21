using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace KEDA_CommonV2.Test.Converters.Workstation;

public class ProtocolJsonConverterTest
{
    private static JsonSerializerOptions _options = JsonOptionsProvider.WorkstationJsonOptions;
    #region LAN 协议正常反序列化测试

    [Fact]
    public void Read_ValidLanProtocol_ModbusTcpNet_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "lan-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
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
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<LanProtocolDto>(result);
        var lanProtocol = (LanProtocolDto)result;
        Assert.Equal("lan-001", lanProtocol.Id);
        Assert.Equal(InterfaceType.LAN, lanProtocol.InterfaceType);
        Assert.Equal(ProtocolType.ModbusTcpNet, lanProtocol.ProtocolType);
        Assert.Equal("192.168.1.100", lanProtocol.IpAddress);
        Assert.Equal(502, lanProtocol.ProtocolPort);
        Assert.Single(lanProtocol.Equipments);
    }

    [Fact]
    public void Read_ValidLanProtocol_WithOptionalFields_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "lan-002",
            "InterfaceType": 0,
            "ProtocolType": 2,
            "IpAddress": "192.168.1.101",
            "ProtocolPort": 9600,
            "Gateway": "192.168.1.1",
            "CollectCycle": 2000,
            "ReceiveTimeOut": 1000,
            "ConnectTimeOut": 800,
            "Account": "admin",
            "Password": "123456",
            "Remark": "测试协议",
            "AdditionalOptions": "option1=value1",
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 7,
                            "Address": "D100"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var lanProtocol = (LanProtocolDto)result;
        Assert.Equal("192.168.1.1", lanProtocol.Gateway);
        Assert.Equal(2000, lanProtocol.CollectCycle);
        Assert.Equal(1000, lanProtocol.ReceiveTimeOut);
        Assert.Equal(800, lanProtocol.ConnectTimeOut);
        Assert.Equal("admin", lanProtocol.Account);
        Assert.Equal("123456", lanProtocol.Password);
        Assert.Equal("测试协议", lanProtocol.Remark);
        Assert.Equal("option1=value1", lanProtocol.AdditionalOptions);
    }

    [Fact]
    public void Read_ValidLanProtocol_IEC104_NoParameterValidation_ShouldDeserializeCorrectly()
    {
        // IEC104 不需要任何参数校验
        var json = """
        {
            "Id": "lan-003",
            "InterfaceType": 0,
            "ProtocolType": 13,
            "IpAddress": "192.168.1.102",
            "ProtocolPort": 2404,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "信号1",
                            "Address": "1001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<LanProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.IEC104, result.ProtocolType);
        Assert.Equal(InterfaceType.LAN, result.InterfaceType);
        Assert.Equal("192.168.1.102", result.IpAddress);
        Assert.Equal(2404, result.ProtocolPort);
        Assert.Equal("eq-001", result.Equipments[0].Id);
        Assert.Equal(EquipmentType.Equipment, result.Equipments[0].EquipmentType);
        Assert.Equal("信号1", result.Equipments[0].Parameters[0].Label);
        Assert.Equal("1001", result.Equipments[0].Parameters[0].Address);
    }

    #endregion

    #region COM 协议正常反序列化测试

    [Fact]
    public void Read_ValidSerialProtocol_ModbusRtu_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
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
                    "Id": "eq-001",
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
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SerialProtocolDto>(result);
        var serialProtocol = (SerialProtocolDto)result;
        Assert.Equal("com-001", serialProtocol.Id);
        Assert.Equal(InterfaceType.COM, serialProtocol.InterfaceType);
        Assert.Equal(ProtocolType.ModbusRtu, serialProtocol.ProtocolType);
        Assert.Equal("COM1", serialProtocol.SerialPortName);
        Assert.Equal(BaudRateType.B9600, serialProtocol.BaudRate);
        Assert.Equal(DataBitsType.D8, serialProtocol.DataBits);
        Assert.Equal(Parity.None, serialProtocol.Parity);
        Assert.Equal(StopBits.One, serialProtocol.StopBits);
    }

    [Fact]
    public void Read_ValidSerialProtocol_CJT1882004Serial_WithInstrumentType_ShouldDeserializeCorrectly()
    {
        // CJT1882004Serial 需要 InstrumentType
        var json = """
        {
            "Id": "com-002",
            "InterfaceType": 1,
            "ProtocolType": 102,
            "SerialPortName": "COM2",
            "BaudRate": 2400,
            "DataBits": 8,
            "Parity": 2,
            "StopBits": 1,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 1,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "累计流量",
                            "StationNo": "123456789012",
                            "DataType": 7,
                            "InstrumentType": 16,
                            "Address": "1F90"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.CJT1882004Serial, result.ProtocolType);
        Assert.Equal(ProtocolType.CJT1882004Serial, result.ProtocolType);
    }

    #endregion

    #region API 协议正常反序列化测试

    [Fact]
    public void Read_ValidApiProtocol_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "api-001",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "AccessApiString": "http://localhost:8080/api/data",
            "RequestMethod": 0,
            "Equipments": [
                {
                    "Id": "eq-001",
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
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ApiProtocolDto>(result);
        var apiProtocol = (ApiProtocolDto)result;
        Assert.Equal("api-001", apiProtocol.Id);
        Assert.Equal(InterfaceType.API, apiProtocol.InterfaceType);
        Assert.Equal(ProtocolType.Api, apiProtocol.ProtocolType);
        Assert.Equal("http://localhost:8080/api/data", apiProtocol.AccessApiString);
        Assert.Equal(RequestMethod.Get, apiProtocol.RequestMethod);
    }

    [Fact]
    public void Read_ValidApiProtocol_WithGateway_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "api-002",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "AccessApiString": "http://localhost:8080/api/data",
            "RequestMethod": 1,
            "Gateway": "http://proxy.local:3128",
            "Equipments": [
                {
                    "Id": "eq-001",
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
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var apiProtocol = (ApiProtocolDto)result;
        Assert.Equal("http://proxy.local:3128", apiProtocol.Gateway);
        Assert.Equal(RequestMethod.Post, apiProtocol.RequestMethod);
    }

    #endregion

    #region DATABASE 协议正常反序列化测试

    [Fact]
    public void Read_ValidDatabaseProtocol_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "Equipments": [
                {
                    "Id": "eq-001",
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
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DatabaseProtocolDto>(result);
        var dbProtocol = (DatabaseProtocolDto)result;
        Assert.Equal("db-001", dbProtocol.Id);
        Assert.Equal(InterfaceType.DATABASE, dbProtocol.InterfaceType);
        Assert.Equal(ProtocolType.MySQL, dbProtocol.ProtocolType);
        Assert.Equal("SELECT * FROM sensors", dbProtocol.QuerySqlString);
    }

    [Fact]
    public void Read_ValidDatabaseProtocol_WithAllOptionalFields_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "db-002",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "IpAddress": "192.168.1.200",
            "ProtocolPort": 3306,
            "DatabaseName": "sensor_db",
            "DatabaseConnectString": "Server=192.168.1.200;Database=sensor_db;",
            "Gateway": "proxy.local",
            "Equipments": [
                {
                    "Id": "eq-001",
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
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var dbProtocol = (DatabaseProtocolDto)result;
        Assert.Equal("192.168.1.200", dbProtocol.IpAddress);
        Assert.Equal(3306, dbProtocol.ProtocolPort);
        Assert.Equal("sensor_db", dbProtocol.DatabaseName);
        Assert.Equal("Server=192.168.1.200;Database=sensor_db;", dbProtocol.DatabaseConnectString);
        Assert.Equal("proxy.local", dbProtocol.Gateway);
    }

    #endregion

    #region 必填字段缺失测试

    [Fact]
    public void Read_MissingId_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Id", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Protocol", exception.Message);
    }

    [Fact]
    public void Read_MissingInterfaceType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "ProtocolType": "ModbusTcpNet",
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("InterfaceType", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Protocol", exception.Message);
    }

    [Fact]
    public void Read_MissingProtocolType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("ProtocolType", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Protocol", exception.Message);
    }

    [Fact]
    public void Read_MissingEquipments_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Equipments", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusTcpNet", exception.Message);
    }

    [Fact]
    public void Read_LanProtocol_MissingIpAddress_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("IpAddress", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusTcpNet", exception.Message);
    }

    [Fact]
    public void Read_LanProtocol_MissingProtocolPort_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("ProtocolPort", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Protocol", exception.Message);
    }

    [Fact]
    public void Read_SerialProtocol_MissingSerialPortName_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 7,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("SerialPortName", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_SerialProtocol_MissingBaudRate_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("BaudRate", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_SerialProtocol_MissingDataBits_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("DataBits", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_SerialProtocol_MissingParity_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Parity", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_SerialProtocol_MissingStopBits_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 0,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("StopBits", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_ApiProtocol_MissingAccessApiString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "RequestMethod": 0,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("AccessApiString", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Api的", exception.Message);
    }

    [Fact]
    public void Read_ApiProtocol_MissingRequestMethod_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "AccessApiString": "http://localhost/api",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("RequestMethod", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("Api的", exception.Message);
    }

    [Fact]
    public void Read_DatabaseProtocol_MissingQuerySqlString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("QuerySqlString", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("MySQL的", exception.Message);
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
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("Id", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains("Protocol", ex.Message);
    }

    [Fact]
    public void Read_EquipmentsNotArray_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": "not-an-array"
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("Equipments", ex.Message);
        Assert.Contains("必须为数组", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_IpAddressNotString_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": 12345,
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("IpAddress", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains(ProtocolType.ModbusTcpNet.ToString(), ex.Message);
    }

    [Fact]
    public void Read_ProtocolPortNotNumber_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": "not-a-number",
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("ProtocolPort", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
        Assert.Contains("Protocol", ex.Message);
    }

    [Fact]
    public void Read_OptionalCollectCycleWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "CollectCycle": "not-a-number",
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("CollectCycle", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalReceiveTimeOutWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "ReceiveTimeOut": "not-a-number",
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("ReceiveTimeOut", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalConnectTimeOutWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "ConnectTimeOut": "not-a-number",
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("ConnectTimeOut", ex.Message);
        Assert.Contains("必须为数字", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalAccountWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Account": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Account", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalPasswordWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Password": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Password", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalRemarkWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Remark": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Remark", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    [Fact]
    public void Read_OptionalAdditionalOptionsWrongType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "AdditionalOptions": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("AdditionalOptions", ex.Message);
        Assert.Contains("必须为字符串", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
    }

    #endregion

    #region 枚举值无效测试

    [Fact]
    public void Read_InvalidInterfaceType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 887,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("InterfaceType", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains("Protocol", ex.Message);
    }

    [Fact]
    public void Read_InvalidProtocolType_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 9987,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("ProtocolType", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains("Protocol", ex.Message);
    }

    [Fact]
    public void Read_InvalidBaudRate_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 3456,
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("BaudRate", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains("ModbusRtu", ex.Message);
    }

    [Fact]
    public void Read_InvalidDataBits_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 88,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("DataBits", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains(ProtocolType.ModbusRtu.ToString(), ex.Message);
    }

    [Fact]
    public void Read_InvalidParity_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 887,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("Parity", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains(ProtocolType.ModbusRtu.ToString(), ex.Message);
    }

    [Fact]
    public void Read_InvalidStopBits_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 55,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("StopBits", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains(ProtocolType.ModbusRtu.ToString(), ex.Message);
    }

    [Fact]
    public void Read_InvalidRequestMethod_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "AccessApiString": "http://localhost/api",
            "RequestMethod": 99,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("RequestMethod", ex.Message);
        Assert.Contains("超出有效范围", ex.Message);
        Assert.Contains(ProtocolType.Api.ToString(), ex.Message);
    }

    #endregion

    #region 接口类型与协议类型不匹配测试

    [Fact]
    public void Read_LanInterfaceWithComProtocol_ShouldThrowJsonException()
    {
        // Arrange - ModbusRtu 是 COM 协议，不能用于 LAN
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 100,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型LAN下不支持协议类型ModbusRtu", ex.Message);
    }

    [Fact]
    public void Read_ComInterfaceWithLanProtocol_ShouldThrowJsonException()
    {
        // Arrange - ModbusTcpNet 是 LAN 协议，不能用于 COM
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 0,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型COM下不支持协议类型ModbusTcpNet", exception.Message);
    }

    [Fact]
    public void Read_ApiInterfaceWithLanProtocol_ShouldThrowJsonException()
    {
        // Arrange - ModbusTcpNet 是 LAN 协议，不能用于 API
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 2,
            "ProtocolType": 0,
            "AccessApiString": "http://localhost/api",
            "RequestMethod": 0,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型API下不支持协议类型ModbusTcpNet", exception.Message);
    }

    [Fact]
    public void Read_DatabaseInterfaceWithLanProtocol_ShouldThrowJsonException()
    {
        // Arrange - ModbusTcpNet 是 LAN 协议，不能用于 DATABASE
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 3,
            "ProtocolType": 0,
            "QuerySqlString": "SELECT * FROM sensors",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型DATABASE下不支持协议类型ModbusTcpNet", exception.Message);
    }

    #endregion

    #region 跨对象校验测试 - Parameters 字段校验

    [Fact]
    public void Read_ModbusTcpNet_MissingStationNo_ShouldThrowJsonException()
    {
        // ModbusTcpNet 需要 StationNo
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataFormat": 0,
                            "DataType": 7,
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("StationNo", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_ModbusTcpNet_MissingDataFormat_ShouldThrowJsonException()
    {
        // ModbusTcpNet 需要 DataFormat
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
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
                            "DataType": 7,
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("DataFormat", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_ModbusTcpNet_MissingDataType_ShouldThrowJsonException()
    {
        // ModbusTcpNet 需要 DataType
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
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
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("DataType", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_ModbusTcpNet_MissingAddressStartWithZero_ShouldThrowJsonException()
    {
        // ModbusTcpNet 需要 AddressStartWithZero
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
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
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("AddressStartWithZero", ex.Message);
        Assert.Contains("ModbusTcpNet", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_CJT1882004OverTcp_MissingInstrumentType_ShouldThrowJsonException()
    {
        // CJT1882004OverTcp 需要 InstrumentType
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 11,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "累计流量",
                            "StationNo": "123456789012",
                            "DataType": 7,
                            "Address": "1F90"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("InstrumentType", ex.Message);
        Assert.Contains("CJT1882004OverTcp", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_Equipment_MissingParametersList_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "IsCollect": true,
                    "EquipmentType": 0
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Parameters", ex.Message);
        Assert.Contains("Equipment", ex.Message);
        Assert.Contains("缺少", ex.Message);
    }

    [Fact]
    public void Read_Equipment_ParametersNotArray_ShouldThrowJsonException()
    {
        // Arrange
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": "not-an-array"
                }
            ]
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("Equipment的Parameters字段必须为数组", ex.Message);
    }

    #endregion

    #region 序列化测试

    [Fact]
    public void Write_LanProtocol_ShouldSerializeCorrectly()
    {
        // Arrange
        var protocol = new LanProtocolDto
        {
            Id = "lan-001",
            ProtocolType = ProtocolType.ModbusTcpNet,
            IpAddress = "192.168.1.100",
            ProtocolPort = 502,
            Gateway = "192.168.1.1",
            CollectCycle = 2000,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("\"Id\":\"lan-001\"", json);
        Assert.Contains("\"IpAddress\":\"192.168.1.100\"", json);
        Assert.Contains("\"ProtocolPort\":502", json);
        Assert.Contains("\"Gateway\":\"192.168.1.1\"", json);
    }

    [Fact]
    public void Write_SerialProtocol_ShouldSerializeCorrectly()
    {
        // Arrange
        var protocol = new SerialProtocolDto
        {
            Id = "com-001",
            ProtocolType = ProtocolType.ModbusRtu,
            SerialPortName = "COM1",
            BaudRate = BaudRateType.B9600,
            DataBits = DataBitsType.D8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("\"Id\":\"com-001\"", json);
        Assert.Contains("\"SerialPortName\":\"COM1\"", json);
    }

    [Fact]
    public void Write_ApiProtocol_ShouldSerializeCorrectly()
    {
        // Arrange
        var protocol = new ApiProtocolDto
        {
            Id = "api-001",
            ProtocolType = ProtocolType.Api,
            AccessApiString = "http://localhost/api",
            RequestMethod = RequestMethod.Get,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("\"Id\":\"api-001\"", json);
        Assert.Contains("\"AccessApiString\":\"http://localhost/api\"", json);
    }

    [Fact]
    public void Write_DatabaseProtocol_ShouldSerializeCorrectly()
    {
        // Arrange
        var protocol = new DatabaseProtocolDto
        {
            Id = "db-001",
            ProtocolType = ProtocolType.MySQL,
            QuerySqlString = "SELECT * FROM sensors",
            IpAddress = "192.168.1.200",
            ProtocolPort = 3306,
            DatabaseName = "sensor_db",
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("\"Id\":\"db-001\"", json);
        Assert.Contains("\"QuerySqlString\":\"SELECT * FROM sensors\"", json);
        Assert.Contains("\"DatabaseName\":\"sensor_db\"", json);
    }

    #endregion

    #region 多设备多参数测试

    [Fact]
    public void Read_MultipleEquipmentsWithMultipleParameters_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "lan-multi",
            "InterfaceType": 0,
            "ProtocolType": 0,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "Name": "设备1",
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
                        },
                        {
                            "Label": "湿度",
                            "StationNo": "1",
                            "DataFormat": 0,
                            "DataType": 4,
                            "AddressStartWithZero": true,
                            "Address": "40003"
                        }
                    ]
                },
                {
                    "Id": "eq-002",
                    "Name": "设备2",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "压力",
                            "StationNo": "2",
                            "DataFormat": 1,
                            "DataType": 1,
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Equipments.Count);
        Assert.Equal(2, result.Equipments[0].Parameters.Count);
        Assert.Single(result.Equipments[1].Parameters);
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void Read_EmptyEquipmentsList_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "Id": "lan-empty",
            "InterfaceType": 0,
            "ProtocolType": 11,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 2404,
            "Equipments": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Equipments);
    }

    [Fact]
    public void Read_EmptyParametersList_ShouldDeserializeCorrectly()
    {
        // Arrange - IEC104 不需要参数校验
        var json = """
        {
            "Id": "lan-empty-params",
            "InterfaceType": 0,
            "ProtocolType": 11,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 2404,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": []
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Equipments);
        Assert.Empty(result.Equipments[0].Parameters);
    }

    #endregion

    #region 补充测试 - 更多协议类型覆盖

    [Fact]
    public void Read_ValidLanProtocol_ModbusRtuOverTcp_ShouldDeserializeCorrectly()
    {
        // ModbusRtuOverTcp 需要 StationNo、DataFormat、DataType、AddressStartWithZero
        var json = """
        {
            "Id": "lan-rtuovertcp",
            "InterfaceType": 0,
            "ProtocolType": 1,
            "IpAddress": "192.168.1.100",
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
                            "DataType": 6,
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.ModbusRtuOverTcp, result.ProtocolType);
        Assert.Single(result.Equipments);
        Assert.Single(result.Equipments[0].Parameters);
    }

    [Fact]
    public void Read_ValidLanProtocol_SiemensS200Smart_OnlyRequireDataType_ShouldDeserializeCorrectly()
    {
        // SiemensS200Smart 只需要 DataType
        var json = """
        {
            "Id": "lan-siemens",
            "InterfaceType": 0,
            "ProtocolType": 4,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 102,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 6,
                            "Address": "DB1.DBD0"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.SiemensS200Smart, result.ProtocolType);
        Assert.Single(result.Equipments);
        Assert.Single(result.Equipments[0].Parameters);
        Assert.Contains("eq-001", result.Equipments[0].Id);
        Assert.Contains("温度", result.Equipments[0].Parameters[0].Label);
    }

    [Fact]
    public void Read_ValidLanProtocol_SiemensS1200_ShouldDeserializeCorrectly()
    {
        // SiemensS1200 只需要 DataType
        var json = """
        {
            "Id": "lan-siemens-1200",
            "InterfaceType": 0,
            "ProtocolType": 5,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 102,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 7,
                            "Address": "DB1.DBD0"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.SiemensS1200, result.ProtocolType);
    }

    [Fact]
    public void Read_ValidLanProtocol_DLT6452007OverTcp_RequireStationNoAndDataType_ShouldDeserializeCorrectly()
    {
        // DLT6452007OverTcp 需要 StationNo 和 DataType
        var json = """
        {
            "Id": "lan-dlt645",
            "InterfaceType": 0,
            "ProtocolType": 10,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "电量",
                            "StationNo": "123456789012",
                            "DataType": 5,
                            "Address": "00010000"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.DLT6452007OverTcp, result.ProtocolType);
        Assert.Equal("eq-001", result.Equipments[0].Id);
        Assert.Equal(EquipmentType.Equipment, result.Equipments[0].EquipmentType);
        Assert.Equal("电量", result.Equipments[0].Parameters[0].Label);
        Assert.Equal("123456789012", result.Equipments[0].Parameters[0].StationNo);
        Assert.Equal(DataType.ULong, result.Equipments[0].Parameters[0].DataType);
        Assert.Equal("00010000", result.Equipments[0].Parameters[0].Address);

    }

    [Fact]
    public void Read_ValidLanProtocol_OpcUa_NoValidation_ShouldDeserializeCorrectly()
    {
        // OpcUa 不需要任何参数校验
        var json = """
        {
            "Id": "lan-opcua",
            "InterfaceType": 0,
            "ProtocolType": 14,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 4840,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "节点值",
                            "Address": "ns=2;s=Demo.Static.Scalar.Float"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.OpcUa, result.ProtocolType);
        Assert.Equal("eq-001", result.Equipments[0].Id);
        Assert.Equal(EquipmentType.Equipment, result.Equipments[0].EquipmentType);
        Assert.Equal("节点值", result.Equipments[0].Parameters[0].Label);
        Assert.Equal("ns=2;s=Demo.Static.Scalar.Float", result.Equipments[0].Parameters[0].Address);
    }

    [Fact]
    public void Read_ValidLanProtocol_OmronCipNet_ShouldDeserializeCorrectly()
    {
        // OmronCipNet 只需要 DataType
        var json = """
        {
            "Id": "lan-omron-cip",
            "InterfaceType": 0,
            "ProtocolType": 3,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 44818,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 5,
                            "Address": "D100"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.OmronCipNet, result.ProtocolType);
        Assert.Equal("eq-001", result.Equipments[0].Id);
        Assert.Equal(EquipmentType.Equipment, result.Equipments[0].EquipmentType);
        Assert.Equal("温度", result.Equipments[0].Parameters[0].Label);
        Assert.Equal(DataType.ULong, result.Equipments[0].Parameters[0].DataType);
        Assert.Equal("D100", result.Equipments[0].Parameters[0].Address);
    }

    [Fact]
    public void Read_ValidLanProtocol_SiemensS1500_ShouldDeserializeCorrectly()
    {
        // SiemensS1500 只需要 DataType
        var json = """
        {
            "Id": "lan-siemens-1500",
            "InterfaceType": 0,
            "ProtocolType": 6,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 102,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 5,
                            "Address": "DB1.DBD0"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.SiemensS1500, result.ProtocolType);
        var protocol = (LanProtocolDto)result;
        Assert.Equal(102, protocol.ProtocolPort);
        Assert.Equal("192.168.1.100", protocol.IpAddress);
    }

    [Fact]
    public void Read_ValidLanProtocol_SiemensS300_ShouldDeserializeCorrectly()
    {
        var json = """
        {
            "Id": "lan-siemens-300",
            "InterfaceType": 0,
            "ProtocolType": 8,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 102,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 4,
                            "Address": "DB1.DBD0"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.SiemensS300, result.ProtocolType);
    }

    [Fact]
    public void Read_ValidLanProtocol_FxSerialOverTcp_ShouldDeserializeCorrectly()
    {
        var json = """
        {
            "Id": "lan-fx-overtcp",
            "InterfaceType": 0,
            "ProtocolType": 12,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 4,
                            "Address": "D100"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.FxSerialOverTcp, result.ProtocolType);
    }

    [Fact]
    public void Read_ValidLanProtocol_OmronFinsUdp_ShouldDeserializeCorrectly()
    {
        var json = """
        {
            "Id": "lan-omron-udp",
            "InterfaceType": 0,
            "ProtocolType": 15,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 9600,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 4,
                            "Address": "D100"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.OmronFinsUdp, result.ProtocolType);
    }

    [Fact]
    public void Read_ValidLanProtocol_FJ1000Jet_NoValidation_ShouldDeserializeCorrectly()
    {
        // 自由协议不需要参数校验
        var json = """
        {
            "Id": "lan-fj1000",
            "InterfaceType": 0,
            "ProtocolType": 16,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 8080,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "数据",
                            "Address": "data1"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.FJ1000Jet, result.ProtocolType);
    }

    #endregion

    #region 补充测试 - COM 协议类型覆盖

    [Fact]
    public void Read_ValidSerialProtocol_DLT6452007Serial_ShouldDeserializeCorrectly()
    {
        // DLT6452007Serial 需要 StationNo 和 DataType
        var json = """
        {
            "Id": "com-dlt645",
            "InterfaceType": 1,
            "ProtocolType": 101,
            "SerialPortName": "COM3",
            "BaudRate": 2400,
            "DataBits": 8,
            "Parity": 2,
            "StopBits": 1,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "电量",
                            "StationNo": "123456789012",
                            "DataType": 2,
                            "Address": "00010000"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.DLT6452007Serial, result.ProtocolType);
    }

    [Fact]
    public void Read_ValidSerialProtocol_FxSerial_ShouldDeserializeCorrectly()
    {
        // FxSerial 只需要 DataType
        var json = """
        {
            "Id": "com-fx",
            "InterfaceType": 1,
            "ProtocolType": 103,
            "SerialPortName": "COM4",
            "BaudRate": 9600,
            "DataBits": 7,
            "Parity": 2,
            "StopBits": 1,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "DataType": 7,
                            "Address": "D100"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProtocolType.FxSerial, result.ProtocolType);
    }

    [Fact]
    public void Read_SerialProtocol_CJT1882004Serial_MissingInstrumentType_ShouldThrowJsonException()
    {
        // CJT1882004Serial 需要 InstrumentType
        var json = """
        {
            "Id": "com-cjt188-fail",
            "InterfaceType": 1,
            "ProtocolType": 102,
            "SerialPortName": "COM2",
            "BaudRate": 2400,
            "DataBits": 8,
            "Parity": 2,
            "StopBits": 1,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "累计流量",
                            "StationNo": "123456789012",
                            "DataType": 3,
                            "Address": "1F90"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("InstrumentType", exception.Message);
        Assert.Contains("缺少", exception.Message);
        Assert.Contains("CJT1882004Serial", exception.Message);
    }

    #endregion

    #region 补充测试 - 序列化往返测试

    [Fact]
    public void ReadWrite_LanProtocol_RoundTrip_ShouldBeEqual()
    {
        // Arrange
        var original = new LanProtocolDto
        {
            Id = "lan-roundtrip",
            ProtocolType = ProtocolType.ModbusTcpNet,
            IpAddress = "192.168.1.100",
            ProtocolPort = 502,
            Gateway = "192.168.1.1",
            CollectCycle = 2000,
            ReceiveTimeOut = 1000,
            ConnectTimeOut = 500,
            Account = "admin",
            Password = "password",
            Remark = "测试",
            AdditionalOptions = "opt=1",
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(original, _options);
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var lanResult = Assert.IsType<LanProtocolDto>(result);
        Assert.Equal(original.Id, lanResult.Id);
        Assert.Equal(original.IpAddress, lanResult.IpAddress);
        Assert.Equal(original.ProtocolPort, lanResult.ProtocolPort);
        Assert.Equal(original.Gateway, lanResult.Gateway);
        Assert.Equal(original.CollectCycle, lanResult.CollectCycle);
    }

    [Fact]
    public void ReadWrite_SerialProtocol_RoundTrip_ShouldBeEqual()
    {
        // Arrange
        var original = new SerialProtocolDto
        {
            Id = "com-roundtrip",
            ProtocolType = ProtocolType.ModbusRtu,
            SerialPortName = "COM1",
            BaudRate = BaudRateType.B9600,
            DataBits = DataBitsType.D8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(original, _options);
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var serialResult = Assert.IsType<SerialProtocolDto>(result);
        Assert.Equal(original.SerialPortName, serialResult.SerialPortName);
        Assert.Equal(original.BaudRate, serialResult.BaudRate);
    }

    [Fact]
    public void ReadWrite_ApiProtocol_RoundTrip_ShouldBeEqual()
    {
        // Arrange
        var original = new ApiProtocolDto
        {
            Id = "api-roundtrip",
            ProtocolType = ProtocolType.Api,
            AccessApiString = "http://localhost/api",
            RequestMethod = RequestMethod.Post,
            Gateway = "http://proxy:8080",
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(original, _options);
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var apiResult = Assert.IsType<ApiProtocolDto>(result);
        Assert.Equal(original.AccessApiString, apiResult.AccessApiString);
        Assert.Equal(original.RequestMethod, apiResult.RequestMethod);
        Assert.Equal(original.Gateway, apiResult.Gateway);
    }

    [Fact]
    public void ReadWrite_DatabaseProtocol_RoundTrip_ShouldBeEqual()
    {
        // Arrange
        var original = new DatabaseProtocolDto
        {
            Id = "db-roundtrip",
            ProtocolType = ProtocolType.MySQL,
            QuerySqlString = "SELECT * FROM table1",
            IpAddress = "192.168.1.200",
            ProtocolPort = 3306,
            DatabaseName = "testdb",
            DatabaseConnectString = "Server=localhost;",
            Gateway = "proxy.local",
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(original, _options);
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        var dbResult = Assert.IsType<DatabaseProtocolDto>(result);
        Assert.Equal(original.QuerySqlString, dbResult.QuerySqlString);
        Assert.Equal(original.DatabaseName, dbResult.DatabaseName);
    }

    #endregion

    #region 补充测试 - DATABASE 可选字段类型错误

    [Fact]
    public void Read_DatabaseProtocol_IpAddressWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "IpAddress": 12345,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("MySQL", ex.Message);
        Assert.Contains("IpAddress", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    [Fact]
    public void Read_DatabaseProtocol_ProtocolPortWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "ProtocolPort": "not-a-number",
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("MySQL", ex.Message);
        Assert.Contains("ProtocolPort", ex.Message);
        Assert.Contains("数字", ex.Message);
    }

    [Fact]
    public void Read_DatabaseProtocol_DatabaseNameWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "DatabaseName": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("MySQL", ex.Message);
        Assert.Contains("DatabaseName", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    [Fact]
    public void Read_DatabaseProtocol_DatabaseConnectStringWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "DatabaseConnectString": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("MySQL", ex.Message);
        Assert.Contains("DatabaseConnectString", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    [Fact]
    public void Read_DatabaseProtocol_GatewayWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "db-001",
            "InterfaceType": 3,
            "ProtocolType": 300,
            "QuerySqlString": "SELECT * FROM sensors",
            "Gateway": 123,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("MySQL", ex.Message);
        Assert.Contains("Gateway", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    #endregion

    #region 补充测试 - LAN/API Gateway 可选字段类型错误

    [Fact]
    public void Read_LanProtocol_GatewayWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "lan-001",
            "InterfaceType": 0,
            "ProtocolType": 13,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 2404,
            "Gateway": 12345,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("IEC104", ex.Message);
        Assert.Contains("Gateway", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    [Fact]
    public void Read_ApiProtocol_GatewayWrongType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "api-001",
            "InterfaceType": 2,
            "ProtocolType": 200,
            "AccessApiString": "http://localhost/api",
            "RequestMethod": 0,
            "Gateway": 12345,
            "Equipments": []
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("Api", ex.Message);
        Assert.Contains("Gateway", ex.Message);
        Assert.Contains("字符串", ex.Message);
    }

    #endregion

    #region 补充测试 - 跨对象校验更多分支

    [Fact]
    public void Read_DLT6452007OverTcp_MissingStationNo_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "lan-dlt645-fail",
            "InterfaceType": 0,
            "ProtocolType": 10,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "电量",
                            "DataType": 5,
                            "Address": "00010000"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("StationNo", exception.Message);
        Assert.Contains("DLT6452007OverTcp", exception.Message);
        Assert.Contains("缺少", exception.Message);
    }

    [Fact]
    public void Read_SiemensS200Smart_MissingDataType_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "lan-siemens-fail",
            "InterfaceType": 0,
            "ProtocolType": 4,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 102,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 0,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "Address": "DB1.DBD0"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("DataType", exception.Message);
        Assert.Contains("SiemensS200Smart", exception.Message);
        Assert.Contains("缺少", exception.Message);
    }

    [Fact]
    public void Read_ModbusRtuOverTcp_MissingDataFormat_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "lan-rtuovertcp-fail",
            "InterfaceType": 0,
            "ProtocolType": 1,
            "IpAddress": "192.168.1.100",
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
                            "DataType": 7,
                            "AddressStartWithZero": true,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("DataFormat", exception.Message);
        Assert.Contains("ModbusRtuOverTcp", exception.Message);
        Assert.Contains("缺少", exception.Message);
    }

    [Fact]
    public void Read_ModbusRtu_MissingAddressStartWithZero_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "com-rtu-fail",
            "InterfaceType": 1,
            "ProtocolType": 100,
            "SerialPortName": "COM1",
            "BaudRate": 9600,
            "DataBits": 8,
            "Parity": 0,
            "StopBits": 1,
            "Equipments": [
                {
                    "Id": "eq-001",
                    "EquipmentType": 1,
                    "IsCollect": true,
                    "Parameters": [
                        {
                            "Label": "温度",
                            "StationNo": "1",
                            "DataFormat": 0,
                            "DataType": 2,
                            "Address": "40001"
                        }
                    ]
                }
            ]
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("AddressStartWithZero", exception.Message);
        Assert.Contains("ModbusRtu", exception.Message);
        Assert.Contains("缺少", exception.Message);
    }

    #endregion

    #region 补充测试 - 默认值测试

    [Fact]
    public void Read_LanProtocol_WithoutOptionalFields_ShouldUseDefaultValues()
    {
        var json = """
        {
            "Id": "lan-defaults",
            "InterfaceType": 0,
            "ProtocolType": 13,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 2404,
            "Equipments": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.CollectCycle); // 默认值
        Assert.Equal(500, result.ReceiveTimeOut); // 默认值
        Assert.Equal(500, result.ConnectTimeOut); // 默认值
        Assert.Equal(string.Empty, result.Account);
        Assert.Equal(string.Empty, result.Password);
        Assert.Equal(string.Empty, result.Remark);
        Assert.Equal(string.Empty, result.AdditionalOptions);
    }

    #endregion

    #region 补充测试 - 接口类型与协议类型更多不匹配场景

    [Fact]
    public void Read_LanInterfaceWithApiProtocol_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 200,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型LAN下不支持协议类型Api", exception.Message);
    }

    [Fact]
    public void Read_LanInterfaceWithDatabaseProtocol_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 0,
            "ProtocolType": 300,
            "IpAddress": "192.168.1.100",
            "ProtocolPort": 502,
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型LAN下不支持协议类型MySQL", exception.Message);
    }

    [Fact]
    public void Read_ComInterfaceWithApiProtocol_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 1,
            "ProtocolType": 200,
            "SerialPortName": "COM1",
            "BaudRate": "B9600",
            "DataBits": "D8",
            "Parity": "None",
            "StopBits": "One",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型COM下不支持协议类型Api", exception.Message);
    }

    [Fact]
    public void Read_ApiInterfaceWithComProtocol_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 2,
            "ProtocolType": 100,
            "AccessApiString": "http://localhost/api",
            "RequestMethod": "Get",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型API下不支持协议类型ModbusRtu", exception.Message);
    }

    [Fact]
    public void Read_DatabaseInterfaceWithComProtocol_ShouldThrowJsonException()
    {
        var json = """
        {
            "Id": "test-001",
            "InterfaceType": 3,
            "ProtocolType": 100,
            "QuerySqlString": "SELECT * FROM sensors",
            "Equipments": []
        }
        """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        Assert.Contains("接口类型DATABASE下不支持协议类型ModbusRtu", exception.Message);
    }

    #endregion

    #region 补充测试 - 序列化时使用实际类型

    [Fact]
    public void Write_LanProtocol_ShouldIncludeInterfaceType()
    {
        // Arrange
        var protocol = new LanProtocolDto
        {
            Id = "lan-serialize",
            ProtocolType = ProtocolType.ModbusTcpNet,
            IpAddress = "192.168.1.100",
            ProtocolPort = 502,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("""
            "InterfaceType":0
            """, json);
    }

    [Fact]
    public void Write_SerialProtocol_ShouldIncludeInterfaceType()
    {
        // Arrange
        var protocol = new SerialProtocolDto
        {
            Id = "com-serialize",
            ProtocolType = ProtocolType.ModbusRtu,
            SerialPortName = "COM1",
            BaudRate = BaudRateType.B9600,
            DataBits = DataBitsType.D8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("""
            "InterfaceType":1
            """, json);
    }

    [Fact]
    public void Write_ApiProtocol_ShouldIncludeInterfaceType()
    {
        // Arrange
        var protocol = new ApiProtocolDto
        {
            Id = "api-serialize",
            ProtocolType = ProtocolType.Api,
            AccessApiString = "http://localhost/api",
            RequestMethod = RequestMethod.Get,
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("""
            "InterfaceType":2
            """, json);
    }

    [Fact]
    public void Write_DatabaseProtocol_ShouldIncludeInterfaceType()
    {
        // Arrange
        var protocol = new DatabaseProtocolDto
        {
            Id = "db-serialize",
            ProtocolType = ProtocolType.MySQL,
            QuerySqlString = "SELECT 1",
            Equipments = []
        };

        // Act
        var json = JsonSerializer.Serialize<ProtocolDto>(protocol, _options);

        // Assert
        Assert.Contains("""
            "InterfaceType":3
            """, json);
    }

    #endregion
}