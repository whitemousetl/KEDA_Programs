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

namespace KEDA_CommonV2.Test.Converters;
//public class ProtocolJsonConverterTest
//{
//    private static JsonSerializerOptions _options =  JsonOptionsProvider.WorkstationOptions;

//    #region Read方法有效输入
//    //协议转换 输入网口协议json 预期返回LanProtocolDto
//    [Fact]
//    public void Read_ValidLanProtocolJson_ReturnsLanProtocolDto()
//    {
//        var json = """
//            {
//                "Id": "1564sdfdsf48ee-sfds",
//                "InterfaceType": 0,
//                "ProtocolType": 0,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600
//            }
//            """;

//        // Act
//        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(InterfaceType.LAN, result.InterfaceType);
//        Assert.Equal(ProtocolType.ModbusTcpNet, result.ProtocolType);
//        Assert.Equal(1000, result.CollectCycle);
//        Assert.Equal(5000, result.ReceiveTimeOut);
//        Assert.Equal(3000, result.ConnectTimeOut);
//        Assert.Equal("test", result.Remark);

//        Assert.IsType<LanProtocolDto>(result);

//        if (result is LanProtocolDto lanProtocolDto)
//        {
//            Assert.Equal("192.168.12.22", lanProtocolDto.IpAddress);
//            Assert.Equal(9600, lanProtocolDto.ProtocolPort);
//        }
//    }

//    //协议转换 输入串口协议json 预期返回SerialProtocolDto
//    [Fact]
//    public void Read_ValidSerialProtocolJson_ReturnsSerialProtocolDto()
//    {
//        var json = """
//            {
//                "Id": "1564sdfdsf48ee-sfds",
//                "InterfaceType": 1,
//                "ProtocolType": 100,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "SerialPortName": "COM1",
//                "BaudRate": 9600,
//                "DataBits": 7,
//                "Parity": 0,
//                "StopBits": 0
//            }
//            """;

//        // Act
//        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(InterfaceType.COM, result.InterfaceType);
//        Assert.Equal(ProtocolType.ModbusRtu, result.ProtocolType);
//        Assert.Equal(1000, result.CollectCycle);
//        Assert.Equal(5000, result.ReceiveTimeOut);
//        Assert.Equal(3000, result.ConnectTimeOut);
//        Assert.Equal("test", result.Remark);

//        Assert.IsType<SerialProtocolDto>(result);

//        if (result is SerialProtocolDto serialProtocolDto)
//        {
//            Assert.Equal("COM1", serialProtocolDto.SerialPortName);
//            Assert.Equal(BaudRateType.B9600, serialProtocolDto.BaudRate);
//            Assert.Equal(DataBitsType.D7, serialProtocolDto.DataBits);
//            Assert.Equal(Parity.None, serialProtocolDto.Parity);
//            Assert.Equal(StopBits.None, serialProtocolDto.StopBits);
//        }
//    }

//    //协议转换 输入接口json 预期返回ApiProtocolDto
//    [Fact]
//    public void Read_ValidApiProtocolJson_ReturnsApiProtocolDto()
//    {
//        var json = """
//            {
//                "Id": "1564sdfdsf48ee-sfds",
//                "InterfaceType": 2,
//                "ProtocolType": 200,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "RequestMethod": 0,
//                "AccessApiString": "https://s5xikogysg.feishu.cn/wiki/DtZawPgVhinGcvkq7uecCos3nAd"
//            }
//            """;

//        // Act
//        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(InterfaceType.API, result.InterfaceType);
//        Assert.Equal(ProtocolType.Api, result.ProtocolType);
//        Assert.Equal(1000, result.CollectCycle);
//        Assert.Equal(5000, result.ReceiveTimeOut);
//        Assert.Equal(3000, result.ConnectTimeOut);
//        Assert.Equal("test", result.Remark);

//        Assert.IsType<ApiProtocolDto>(result);

//        if (result is ApiProtocolDto apiProtocolDto)
//        {
//            Assert.Equal(RequestMethod.Get, apiProtocolDto.RequestMethod);
//            Assert.Equal("https://s5xikogysg.feishu.cn/wiki/DtZawPgVhinGcvkq7uecCos3nAd", apiProtocolDto.AccessApiString);
//        }
//    }

//    //协议转换 输入数据库json 预期返回DatabaseProtocolDto
//    [Fact]
//    public void Read_ValidDatabaseProtocolJson_ReturnsDatabaseProtocolDto()
//    {
//        var json = """
//            {
//                "Id": "1564sdfdsf48ee-sfds",
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "Account": "keda",
//                "Password": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        // Act
//        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(InterfaceType.DATABASE, result.InterfaceType);
//        Assert.Equal(ProtocolType.MySQL, result.ProtocolType);
//        Assert.Equal(1000, result.CollectCycle);
//        Assert.Equal(5000, result.ReceiveTimeOut);
//        Assert.Equal(3000, result.ConnectTimeOut);
//        Assert.Equal("test", result.Remark);

//        Assert.IsType<DatabaseProtocolDto>(result);

//        if (result is DatabaseProtocolDto dbProtocolDto)
//        {
//            Assert.Equal("192.168.12.22", dbProtocolDto.IpAddress);
//            Assert.Equal(9600, dbProtocolDto.ProtocolPort);
//            Assert.Equal("keda", dbProtocolDto.Account);
//            Assert.Equal("root", dbProtocolDto.Password);
//            Assert.Equal("collector", dbProtocolDto.DatabaseName);
//            Assert.Equal("select * from collector where name = 'keda'", dbProtocolDto.QuerySqlString);
//        }
//    }
//    #endregion

//    #region InterfaceType和ProtocolType超出范围
//    // InterfaceType超出范围，预期抛出异常，信息包含"InterfaceType值100超出有效范围"
//    [Fact]
//    public void Read_InterfaceTypeOutOfRange_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 100,
//            "ProtocolType": 0,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("InterfaceType值100超出有效范围", ex.Message);
//    }

//    // ProtocolType超出范围，预期抛出异常，信息包含"ProtocolType值999超出有效范围"
//    [Fact]
//    public void Read_ProtocolTypeOutOfRange_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 0,
//            "ProtocolType": 999,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("ProtocolType值999超出有效范围", ex.Message);
//    }
//    #endregion

//    #region 四种接口类型不支持的协议类型
//    // LAN接口类型不支持Api协议类型
//    [Fact]
//    public void Read_LanInterfaceTypeWithApiProtocolType_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 0,
//            "ProtocolType": 200,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("接口类型LAN下不支持协议类型Api", ex.Message);
//    }

//    // COM接口类型不支持ModbusTcpNet协议类型
//    [Fact]
//    public void Read_ComInterfaceTypeWithModbusTcpNetProtocolType_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 1,
//            "ProtocolType": 0,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("接口类型COM下不支持协议类型ModbusTcpNet", ex.Message);
//    }

//    // API接口类型不支持MySQL协议类型
//    [Fact]
//    public void Read_ApiInterfaceTypeWithMySQLProtocolType_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 2,
//            "ProtocolType": 300,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("接口类型API下不支持协议类型MySQL", ex.Message);
//    }

//    // DATABASE接口类型不支持ModbusRtu协议类型
//    [Fact]
//    public void Read_DatabaseInterfaceTypeWithModbusRtuProtocolType_ThrowsJsonException()
//    {
//        var json = """
//        {
//            "Id": "testid",
//            "InterfaceType": 3,
//            "ProtocolType": 100,
//            "CollectCycle": 1000,
//            "ReceiveTimeOut": 5000,
//            "ConnectTimeOut": 3000,
//            "Remark": "test"
//        }
//        """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
//        Assert.Contains("接口类型DATABASE下不支持协议类型ModbusRtu", ex.Message);
//    }

//    #endregion

//    #region Id校验,类型是字符串
//    //Id为字符串"ssswwww"合法数据,InterfaceType为非法，返回JsonException，信息 InterfaceType值30超出有效范围
//    [Fact]
//    public void Read_IdIsValidString_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssswwww",
//                "InterfaceType": 30,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType值30超出有效范围", ex.Message);
//    }

//    //Id为字符串""，返回JsonException，信息 Id字段不能为空或空白
//    [Fact]
//    public void Read_IdIsEmpty_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "",
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段不能为空或空白", ex.Message);
//    }

//    //缺失Id，返回JsonException，信息 缺少Id字段
//    [Fact]
//    public void Read_MissingId_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("缺少Id字段", ex.Message);
//    }

//    //Id为Object类型，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsObject_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": { "Type": 0 },
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.Object, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }

//    //Id为Array类型，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsArray_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": [ { "Type": 0 } ],
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.Array, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }

//    //Id是Number类型，非字符串，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsNumber_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": 22,
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.Number, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }

//    //Id是true，非字符串，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsTrue_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": true,
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.True, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }

//    //Id是false，非字符串，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsFalse_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": false,
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.False, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }

//    //Id为null，返回JsonException，信息 Id字段必须为字符串
//    [Fact]
//    public void Read_IdIsNull_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": null,
//                "InterfaceType": 3,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("Id");
//        Assert.Equal(JsonValueKind.Null, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("Id字段必须为字符串", ex.Message);
//    }
//    #endregion

//    #region InterfaceType校验,类型是数字
//    //InterfaceType是Number类型，合法枚举，但是协议类型错误，为字符串，返回JsonException，InterfaceType校验通过。信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsNumber_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "22",
//                "InterfaceType": 3,
//                "ProtocolType": "300",
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.Number, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //缺失InterfaceType，返回JsonException，信息 缺少InterfaceType字段
//    [Fact]
//    public void Read_MissingInterfaceType_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssswwvvv4444",
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("缺少InterfaceType字段", ex.Message);
//    }

//    //InterfaceType为Object类型，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsObject_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "333ssssddd",
//                "InterfaceType": { "Type": 0 },
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.Object, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType为Array类型，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsArray_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssssdff",
//                "InterfaceType": [ { "Type": 0 } ],
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.Array, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType为字符串""，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsEmpty_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "3333ddd",
//                "InterfaceType": "",
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType为字符串"ssswwww"合法数据,InterfaceType为非法，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsValidString_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssswwwwsswww333",
//                "InterfaceType": "ssswwww",
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType是true，非字符串，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsTrue_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "true",
//                "InterfaceType": true,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.True, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType是false，非字符串，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsFalse_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "false",
//                "InterfaceType": false,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.False, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType为null，返回JsonException，信息 InterfaceType字段必须为数字
//    [Fact]
//    public void Read_InterfaceTypeIsNull_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "sfdsfe",
//                "InterfaceType": null,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.Null, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType字段必须为数字", ex.Message);
//    }

//    //InterfaceType为100，不支持的接口类型，返回JsonException，信息 InterfaceType值100超出有效范围
//    [Fact]
//    public void Read_InterfaceTypeIsNotSupport_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "sfdsfe",
//                "InterfaceType": 100,
//                "ProtocolType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("InterfaceType");
//        Assert.Equal(JsonValueKind.Number, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType值100超出有效范围", ex.Message);
//    }
//    #endregion

//    #region ProtocolType校验,类型是数字
//    //ProtocolType是Number类型，合法枚举，但是协议类型错误，为字符串，返回JsonException，信息 接口类型LAN下不支持协议类型Api
//    [Fact]
//    public void Read_ProtocolTypeIsNumber_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "22",
//                "InterfaceType": 0,
//                "ProtocolType": 200,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.Number, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("接口类型LAN下不支持协议类型Api", ex.Message);
//    }

//    //缺失ProtocolType，返回JsonException，信息缺少 InterfaceType值300超出有效范围
//    [Fact]
//    public void Read_MissingProtocolTypeProtocolType_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssswwvvv4444",
//                "InterfaceType": 300,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("InterfaceType值300超出有效范围", ex.Message);
//    }

//    //ProtocolType为Object类型，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsObject_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "333ssssddd",
//                "InterfaceType": 0,
//                "ProtocolType": { "Type": 0 },
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.Object, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType为Array类型，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsArray_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssssdff",
//                "InterfaceType": 0,
//                "ProtocolType": [ { "Type": 0 } ],
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.Array, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType为字符串""，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsEmpty_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "3333ddd",
//                "InterfaceType": 0,
//                "ProtocolType": "",
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType为字符串"ssswwww"合法数据,ProtocolType为非法，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsValidString_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "ssswwwwsswww333",
//                "InterfaceType": 0,
//                "ProtocolType": "ssswwww",
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.String, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType是true，非字符串，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsTrue_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "true",
//                "InterfaceType": 0,
//                "ProtocolType": true,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.True, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType是false，非字符串，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsFalse_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "false",
//                "InterfaceType": 3,
//                "ProtocolType": false,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.False, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }

//    //ProtocolType为null，返回JsonException，信息 ProtocolType字段必须为数字
//    [Fact]
//    public void Read_ProtocolTypeIsNull_ThrowsJsonException()
//    {
//        var json = """
//            {
//                "Id": "sfdsfe",
//                "InterfaceType": 3,
//                "ProtocolType": null,
//                "CollectCycle": 1000,
//                "ReceiveTimeOut": 5000,
//                "ConnectTimeOut": 3000,
//                "Remark": "test",
//                "IpAddress": "192.168.12.22",
//                "ProtocolPort": 9600,
//                "DatabaseAccount": "keda",
//                "DatabasePassword": "root",
//                "DatabaseName": "collector",
//                "QuerySqlString": "select * from collector where name = 'keda'"
//            }
//            """;

//        using var doc = JsonDocument.Parse(json);
//        var root = doc.RootElement;
//        var prop = root.GetProperty("ProtocolType");
//        Assert.Equal(JsonValueKind.Null, prop.ValueKind);

//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

//        Assert.Contains("ProtocolType字段必须为数字", ex.Message);
//    }
//    #endregion

//    #region Json整体问题测试用例
//    [Theory]
//    [InlineData("\"abc\"")]
//    [InlineData("123")]
//    [InlineData("[1,2,3]")]
//    public void Read_JsonIsNotObject_ThrowsJsonException(string input)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains("The JSON value could not be converted to KEDA_CommonV2.Model.Workstations.Protocols.ProtocolDto", ex.Message, StringComparison.OrdinalIgnoreCase);
//    }

//    [Theory]
//    [InlineData("")]
//    [InlineData(" ")]
//    public void Read_EmptyJson_ThrowsJsonException(string input)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains("The input does not contain any JSON tokens. Expected the input to start with a valid JSON token, when isFinalBlock is true. Path", ex.Message, StringComparison.OrdinalIgnoreCase);
//    }

//    [Fact]
//    public void Read_NullJson_ThrowsArgumentNullException()
//    {
//        string? input = null;
//        var ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<ProtocolDto>(input!, _options));
//        Assert.Contains("Value cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
//    }
//    #endregion

//    #region 四种接口类型对应协议子类校验
//    [Theory]
//    [InlineData("""
//    {
//        "Id": "id1",
//        "InterfaceType": 0,
//        "ProtocolType": 0,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "",
//        "ProtocolPort": 9600
//    }
//    """, "IpAddress不能为空")]
//    [InlineData("""
//    {
//        "Id": "id1",
//        "InterfaceType": 0,
//        "ProtocolType": 0,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "192.168.1.1",
//        "ProtocolPort": 0
//    }
//    """, "ProtocolPort必须是1-65535之间的有效端口号")]
//    [InlineData("""
//    {
//        "Id": "id1",
//        "InterfaceType": 0,
//        "ProtocolType": 0,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "192.168.1.1",
//        "ProtocolPort": 70000
//    }
//    """, "ProtocolPort必须是1-65535之间的有效端口号")]
//    public void Deserialize_LanProtocolDto_Invalid_Throws(string input, string expectedMsg)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains(expectedMsg, ex.Message);
//    }

//    [Theory]
//    [InlineData("""
//    {
//        "Id": "id2",
//        "InterfaceType": 1,
//        "ProtocolType": 100,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "SerialPortName": "",
//        "BaudRate": 9600,
//        "DataBits": 7,
//        "Parity": 0,
//        "StopBits": 0
//    }
//    """, "SerialPortName不能为空")]
//    [InlineData("""
//    {
//        "Id": "id2",
//        "InterfaceType": 1,
//        "ProtocolType": 100,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "SerialPortName": "COM1",
//        "BaudRate": 99999,
//        "DataBits": 7,
//        "Parity": 0,
//        "StopBits": 0
//    }
//    """, "BaudRate值99999超出有效范围")]
//    [InlineData("""
//    {
//        "Id": "id2",
//        "InterfaceType": 1,
//        "ProtocolType": 100,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "SerialPortName": "COM1",
//        "BaudRate": 9600,
//        "DataBits": 99,
//        "Parity": 0,
//        "StopBits": 0
//    }
//    """, "DataBits值99超出有效范围")]
//    [InlineData("""
//    {
//        "Id": "id2",
//        "InterfaceType": 1,
//        "ProtocolType": 100,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "SerialPortName": "COM1",
//        "BaudRate": 9600,
//        "DataBits": 7,
//        "Parity": 99,
//        "StopBits": 0
//    }
//    """, "Parity值99超出有效范围")]
//    [InlineData("""
//    {
//        "Id": "id2",
//        "InterfaceType": 1,
//        "ProtocolType": 100,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "SerialPortName": "COM1",
//        "BaudRate": 9600,
//        "DataBits": 7,
//        "Parity": 0,
//        "StopBits": 99
//    }
//    """, "StopBits值99超出有效范围")]
//    public void Deserialize_SerialProtocolDto_Invalid_Throws(string input, string expectedMsg)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains(expectedMsg, ex.Message);
//    }

//    [Theory]
//    [InlineData("""
//    {
//        "Id": "id3",
//        "InterfaceType": 2,
//        "ProtocolType": 200,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "RequestMethod": 0,
//        "AccessApiString": ""
//    }
//    """, "AccessApiString不能为空")]
//    [InlineData("""
//    {
//        "Id": "id3",
//        "InterfaceType": 2,
//        "ProtocolType": 200,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "RequestMethod": 99,
//        "AccessApiString": "http://api"
//    }
//    """, "RequestMethod值99超出有效范围")]
//    public void Deserialize_ApiProtocolDto_Invalid_Throws(string input, string expectedMsg)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains(expectedMsg, ex.Message);
//    }

//    [Theory]
//    [InlineData("""
//    {
//        "Id": "id4",
//        "InterfaceType": 3,
//        "ProtocolType": 300,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "",
//        "ProtocolPort": 0,
//        "DatabaseName": "",
//        "Account": "",
//        "Password": "",
//        "DatabaseConnectString": "",
//        "QuerySqlString": ""
//    }
//    """, "QuerySqlString不能为空")]
//    [InlineData("""
//    {
//        "Id": "id4",
//        "InterfaceType": 3,
//        "ProtocolType": 300,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "",
//        "ProtocolPort": 0,
//        "DatabaseName": "",
//        "Account": "",
//        "Password": "",
//        "DatabaseConnectString": "",
//        "QuerySqlString": "select 1"
//    }
//    """, "IpAddress, ProtocolPort, DatabaseName, Account, Password必须全部填写，ProtocolPort范围是1~65535")]
//    [InlineData("""
//    {
//        "Id": "id4",
//        "InterfaceType": 3,
//        "ProtocolType": 300,
//        "CollectCycle": 1000,
//        "ReceiveTimeOut": 5000,
//        "ConnectTimeOut": 3000,
//        "Remark": "test",
//        "IpAddress": "127.0.0.1",
//        "ProtocolPort": 0,
//        "DatabaseName": "",
//        "Account": "",
//        "Password": "",
//        "DatabaseConnectString": "",
//        "QuerySqlString": "select 1"
//    }
//    """, "IpAddress, ProtocolPort, DatabaseName, Account, Password必须全部填写")]
//    public void Deserialize_DatabaseProtocolDto_Invalid_Throws(string input, string expectedMsg)
//    {
//        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
//        Assert.Contains(expectedMsg, ex.Message);
//    } 
//    #endregion
//}

public class ProtocolJsonConverterTest
{
    private static JsonSerializerOptions _options = JsonOptionsProvider.WorkstationOptions;
    private readonly ITestOutputHelper _output;

    public ProtocolJsonConverterTest(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 第一层：JSON 格式层 (JSON Format Layer)
    //目的：确保可以被解析器解析
    //1、验证输入是否是合法的 JSON 格式
    //2、是否为 null
    //3、是否为空字符串
    //4、是否为有效的 JSON 语法
    //5、根节点类型是否正确（对象、数组等）

    /// <summary>
    /// 输入为 null，预期抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_Null_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json!, _options));

        Assert.Contains("Value cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入为 空字符串 ，空白字符串（空格、制表符、换行符），预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("   \t\n  ")]
    public void Layer1_JsonFormat_WhitespaceString_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("The input does not contain any JSON tokens", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入为无效的 JSON 语法，预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("{")]                           // 未闭合的对象
    [InlineData("}")]                           // 只有闭合括号
    [InlineData("{{}")]                         // 括号不匹配
    [InlineData("{ \"Id\": }")]                 // 值缺失
    [InlineData("{ \"Id\" }")]                  // 缺少冒号
    [InlineData("{ Id: \"123\" }")]             // 键未使用引号
    [InlineData("{ 'Id': '123' }")]             // 使用单引号
    [InlineData("{ \"Id\": \"123\", }")]        // 末尾多余逗号
    [InlineData("{ \"Id\":  undefined }")]       // undefined 不是有效 JSON
    [InlineData("{ \"Id\": NaN }")]             // NaN 不是有效 JSON
    [InlineData("abc")]                         // 普通字符串（未加引号）
    [InlineData("{ \"Id\": \"123\" }}}")]       // 多余的闭合括号
    public void Layer1_JsonFormat_InvalidSyntax_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));
        _output.WriteLine(ex.ToString());
    }

    /// <summary>
    /// 输入的根节点是字符串类型，预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"\"")]
    [InlineData("\"123\"")]
    public void Layer1_JsonFormat_RootIsString_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("could not be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入的根节点是数字类型，预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("3.14")]
    [InlineData("1e10")]
    public void Layer1_JsonFormat_RootIsNumber_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("could not be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入的根节点是布尔类型，预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Layer1_JsonFormat_RootIsBoolean_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("could not be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入的根节点是数组类型，预期抛出 JsonException
    /// </summary>
    [Theory]
    [InlineData("[]")]
    [InlineData("[1, 2, 3]")]
    [InlineData("[{\"Id\": \"123\"}]")]
    public void Layer1_JsonFormat_RootIsArray_ThrowsJsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("could not be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入的根节点是 null，预期抛出异常或返回 null（取决于配置）
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_RootIsNull_ReturnsNull()
    {
        // Arrange
        string json = "null";

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 输入是合法的空对象，预期能够解析（但会在后续层失败）
    /// 这个测试证明 JSON 格式层通过了
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_EmptyObject_ParsesSuccessfully()
    {
        // Arrange
        string json = "{}";

        // Act & Assert
        // 应该能解析 JSON，但会在第二层（字段存在性）失败
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        // 确保不是格式错误，而是缺少字段
        Assert.Contains("缺少", ex.Message);
    }

    /// <summary>
    /// 输入包含注释（非标准 JSON），预期抛出 JsonException
    /// 注意：标准 JSON 不支持注释
    /// </summary>
    [Theory]
    [InlineData("{ /* comment */ \"Id\": \"123\" }")]
    [InlineData("{ // comment\n \"Id\": \"123\" }")]
    public void Layer1_JsonFormat_WithComments_ThrowsJsonException(string json)
    {
        // Act & Assert
        // 标准 JSON 不支持注释（除非 JsonSerializerOptions 启用了 ReadCommentHandling）
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    /// <summary>
    /// 输入包含 Unicode 转义序列，预期能够正确解析
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_WithUnicodeEscape_ParsesSuccessfully()
    {
        // Arrange
        string json = "{ \"Id\": \"\\u0074\\u0065\\u0073\\u0074\" }"; // "test" 的 Unicode 转义

        // Act & Assert
        // 应该能解析 JSON（会在后续层失败，因为缺少其他字段）
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        // 确保不是格式错误
        Assert.Contains("缺少", ex.Message);
    }

    /// <summary>
    /// 输入包含转义字符，预期能够正确解析
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_WithEscapedCharacters_ParsesSuccessfully()
    {
        // Arrange
        string json = "{ \"Id\": \"test\\nvalue\\t\\\"quoted\\\"\" }";

        // Act & Assert
        // 应该能解析 JSON
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        // 确保不是格式错误
        Assert.Contains("缺少", ex.Message);
    }

    /// <summary>
    /// 输入包含很深的嵌套结构，预期能够解析（或因深度限制失败）
    /// </summary>
    [Fact]
    public void Layer1_JsonFormat_DeeplyNested_ParsesOrThrowsDepthException()
    {
        // Arrange
        // 创建一个非常深的嵌套结构
        var nested = "{ \"a\": ";
        for (int i = 0; i < 100; i++)
        {
            nested += "{ \"b\": ";
        }
        nested += "1";
        for (int i = 0; i < 100; i++)
        {
            nested += " }";
        }
        nested += " }";

        // Act & Assert
        // 可能抛出 JsonException（深度超限）或能够解析
        try
        {
            JsonSerializer.Deserialize<ProtocolDto>(nested, _options);
        }
        catch (JsonException ex)
        {
            // 如果抛出异常，应该是深度相关的错误
            Assert.True(
                ex.Message.Contains("depth", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("nested", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            );
        }
    }
    #endregion

    #region 第二层：字段存在性层 (Field Existence Layer)
    //目的：确保所有必需的数据都存在
    //验证必需字段是否存在
    //字段是否缺失
    //字段是否为 null（如果不允许）
    #endregion
}
