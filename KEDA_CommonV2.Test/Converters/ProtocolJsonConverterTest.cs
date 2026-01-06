using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Converters;
public class ProtocolJsonConverterTest
{
    private static JsonSerializerOptions _options =  JsonOptionsProvider.WorkstationOptions;

    #region Read方法有效输入
    //协议转换 输入网口协议json 预期返回LanProtocolDto
    [Fact]
    public void Read_ValidLanProtocolJson_ReturnsLanProtocolDto()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": 0,
                "ProtocolType": 0,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InterfaceType.LAN, result.InterfaceType);
        Assert.Equal(ProtocolType.ModbusTcpNet, result.ProtocolType);
        Assert.Equal(1000, result.CollectCycle);
        Assert.Equal(5000, result.ReceiveTimeOut);
        Assert.Equal(3000, result.ConnectTimeOut);
        Assert.Equal("test", result.Remark);

        Assert.IsType<LanProtocolDto>(result);

        if (result is LanProtocolDto lanProtocolDto)
        {
            Assert.Equal("192.168.12.22", lanProtocolDto.IpAddress);
            Assert.Equal(9600, lanProtocolDto.ProtocolPort);
        }
    }

    //协议转换 输入串口协议json 预期返回SerialProtocolDto
    [Fact]
    public void Read_ValidSerialProtocolJson_ReturnsSerialProtocolDto()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": 1,
                "ProtocolType": 100,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "SerialPortName": "COM1",
                "BaudRate": 9600,
                "DataBits": 7,
                "Parity": 0,
                "StopBits": 0
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InterfaceType.COM, result.InterfaceType);
        Assert.Equal(ProtocolType.ModbusRtu, result.ProtocolType);
        Assert.Equal(1000, result.CollectCycle);
        Assert.Equal(5000, result.ReceiveTimeOut);
        Assert.Equal(3000, result.ConnectTimeOut);
        Assert.Equal("test", result.Remark);

        Assert.IsType<SerialProtocolDto>(result);

        if (result is SerialProtocolDto serialProtocolDto)
        {
            Assert.Equal("COM1", serialProtocolDto.SerialPortName);
            Assert.Equal(BaudRateType.B9600, serialProtocolDto.BaudRate);
            Assert.Equal(DataBitsType.D7, serialProtocolDto.DataBits);
            Assert.Equal(Parity.None, serialProtocolDto.Parity);
            Assert.Equal(StopBits.None, serialProtocolDto.StopBits);
        }
    }

    //协议转换 输入接口json 预期返回ApiProtocolDto
    [Fact]
    public void Read_ValidApiProtocolJson_ReturnsApiProtocolDto()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": 2,
                "ProtocolType": 200,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "RequestMethod": 0,
                "AccessApiString": "https://s5xikogysg.feishu.cn/wiki/DtZawPgVhinGcvkq7uecCos3nAd"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InterfaceType.API, result.InterfaceType);
        Assert.Equal(ProtocolType.Api, result.ProtocolType);
        Assert.Equal(1000, result.CollectCycle);
        Assert.Equal(5000, result.ReceiveTimeOut);
        Assert.Equal(3000, result.ConnectTimeOut);
        Assert.Equal("test", result.Remark);

        Assert.IsType<ApiProtocolDto>(result);

        if (result is ApiProtocolDto apiProtocolDto)
        {
            Assert.Equal(RequestMethod.Get, apiProtocolDto.RequestMethod);
            Assert.Equal("https://s5xikogysg.feishu.cn/wiki/DtZawPgVhinGcvkq7uecCos3nAd", apiProtocolDto.AccessApiString);
        }
    }

    //协议转换 输入数据库json 预期返回DatabaseProtocolDto
    [Fact]
    public void Read_ValidDatabaseProtocolJson_ReturnsDatabaseProtocolDto()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "Account": "keda",
                "Password": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InterfaceType.DATABASE, result.InterfaceType);
        Assert.Equal(ProtocolType.MySQL, result.ProtocolType);
        Assert.Equal(1000, result.CollectCycle);
        Assert.Equal(5000, result.ReceiveTimeOut);
        Assert.Equal(3000, result.ConnectTimeOut);
        Assert.Equal("test", result.Remark);

        Assert.IsType<DatabaseProtocolDto>(result);

        if (result is DatabaseProtocolDto dbProtocolDto)
        {
            Assert.Equal("192.168.12.22", dbProtocolDto.IpAddress);
            Assert.Equal(9600, dbProtocolDto.ProtocolPort);
            Assert.Equal("keda", dbProtocolDto.Account);
            Assert.Equal("root", dbProtocolDto.Password);
            Assert.Equal("collector", dbProtocolDto.DatabaseName);
            Assert.Equal("select * from collector where name = 'keda'", dbProtocolDto.QuerySqlString);
        }
    }
    #endregion

    #region Id校验,类型是字符串
    //缺失Id，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_MissingId_ThrowsJsonException()
    {
        var json = """
            {
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id为Object类型，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsObject_ThrowsJsonException()
    {
        var json = """
            {
                "Id": { "Type": 0 },
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.Object, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id为Array类型，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsArray_ThrowsJsonException()
    {
        var json = """
            {
                "Id": [ { "Type": 0 } ],
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.Array, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id为字符串""，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsEmpty_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "",
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.String, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id为字符串"ssswwww"合法数据,InterfaceType为非法，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_IdIsValidString_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "ssswwww",
                "InterfaceType": 30,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.String, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("不支持的接口类型", ex.Message);
    }

    //Id是Number类型，非字符串，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsNumber_ThrowsJsonException()
    {
        var json = """
            {
                "Id": 22,
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.Number, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id是true，非字符串，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsTrue_ThrowsJsonException()
    {
        var json = """
            {
                "Id": true,
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.True, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id是false，非字符串，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsFalse_ThrowsJsonException()
    {
        var json = """
            {
                "Id": false,
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.False, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }

    //Id为null，返回JsonException，信息缺少或无效的Id字段
    [Fact]
    public void Read_IdIsNull_ThrowsJsonException()
    {
        var json = """
            {
                "Id": null,
                "InterfaceType": 3,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.Null, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的Id字段", ex.Message);
    }
    #endregion

    #region InterfaceType校验,类型是字符串
    //缺失InterfaceType，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_MissingInterfaceType_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "ssswwvvv4444",
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType为Object类型，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsObject_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "333ssssddd",
                "InterfaceType": { "Type": 0 },
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var prop = root.GetProperty("InterfaceType");
        Assert.Equal(JsonValueKind.Object, prop.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType为Array类型，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsArray_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "ssssdff",
                "InterfaceType": [ { "Type": 0 } ],
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var prop = root.GetProperty("InterfaceType");
        Assert.Equal(JsonValueKind.Array, prop.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType为字符串""，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsEmpty_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "3333ddd",
                "InterfaceType": "",
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var prop = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.String, prop.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType为字符串"ssswwww"合法数据,InterfaceType为非法，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsValidString_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "ssswwwwsswww333",
                "InterfaceType": "ssswwww",
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var prop = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.String, prop.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段，必须为数字", ex.Message);
    }

    //InterfaceType是Number类型，合法枚举，但是协议类型错误，为字符串，返回JsonException，信息缺少或无效的ProtocolType字段
    [Fact]
    public void Read_InterfaceTypeIsNumber_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "22",
                "InterfaceType": 3,
                "ProtocolType": "300",
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var prop = root.GetProperty("InterfaceType");
        Assert.Equal(JsonValueKind.Number, prop.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的ProtocolType字段", ex.Message);
    }

    //InterfaceType是true，非字符串，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsTrue_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "true",
                "InterfaceType": true,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.True, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType是false，非字符串，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsFalse_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "false",
                "InterfaceType": false,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.False, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    //InterfaceType为null，返回JsonException，信息缺少或无效的InterfaceType字段
    [Fact]
    public void Read_InterfaceTypeIsNull_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "sfdsfe",
                "InterfaceType": null,
                "ProtocolType": 300,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600,
                "DatabaseAccount": "keda",
                "DatabasePassword": "root",
                "DatabaseName": "collector",
                "QuerySqlString": "select * from collector where name = 'keda'"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var idProp = root.GetProperty("Id");
        Assert.Equal(JsonValueKind.Null, idProp.ValueKind);

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));

        Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    }

    
    #endregion

    //#region InterfaceType校验，类型是数字
    ////InterfaceType缺失
    //[Fact]
    //public void Read_MissingInterfaceType_ThrowsJsonException()
    //{
    //    var json = """
    //        {
    //            "Id": "1564sdfdsf48ee-sfds",
    //            "ProtocolType": 0,
    //            "CollectCycle": 1000,
    //            "ReceiveTimeOut": 5000,
    //            "ConnectTimeOut": 3000,
    //            "Remark": "test",
    //            "IpAddress": "192.168.12.22",
    //            "ProtocolPort": 9600
    //        }
    //        """;

    //    // Act
    //    var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //    Assert.Contains("缺少或无效的InterfaceType字段", ex.Message);
    //}

    ////InterfaceType是字符串""
    //[Fact]
    //public void Read_InterfaceTypeIsEmpty_ThrowsJsonException()
    //{
    //    var json = """
    //        {
    //            "Id": "1564sdfdsf48ee-sfds",
    //            "InterfaceType": "",
    //            "ProtocolType": 0,
    //            "CollectCycle": 1000,
    //            "ReceiveTimeOut": 5000,
    //            "ConnectTimeOut": 3000,
    //            "Remark": "test",
    //            "IpAddress": "192.168.12.22",
    //            "ProtocolPort": 9600
    //        }
    //        """;

    //    using var doc = JsonDocument.Parse(json);
    //    var root = doc.RootElement;
    //    var idProp = root.GetProperty("Id");
    //    Assert.Equal(JsonValueKind.String, idProp.ValueKind);

    //    // Act
    //    var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //}

    //[Fact]
    //public void Read_InterfaceTypeIsNull_ThrowsJsonException()
    //{
    //    var json = """
    //        {
    //            "Id": "1564sdfdsf48ee-sfds",
    //            "InterfaceType": null,
    //            "ProtocolType": 0,
    //            "CollectCycle": 1000,
    //            "ReceiveTimeOut": 5000,
    //            "ConnectTimeOut": 3000,
    //            "Remark": "test",
    //            "IpAddress": "192.168.12.22",
    //            "ProtocolPort": 9600
    //        }
    //        """;

    //    // Act
    //    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //}

    //[Fact]
    //public void Read_InterfaceTypeAsString_ThrowsJsonException()
    //{
    //    var json = """
    //        {
    //            "Id": "1564sdfdsf48ee-sfds",
    //            "InterfaceType": "0",
    //            "ProtocolType": 0,
    //            "CollectCycle": 1000,
    //            "ReceiveTimeOut": 5000,
    //            "ConnectTimeOut": 3000,
    //            "Remark": "test",
    //            "IpAddress": "192.168.12.22",
    //            "ProtocolPort": 9600
    //        }
    //        """;

    //    // Act
    //    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //}

    //[Fact]
    //public void Read_InterfaceTypeAsObject_ThrowsJsonException()
    //{
    //    var json = """
    //    {
    //        "Id": "1564sdfdsf48ee-sfds",
    //        "InterfaceType": { "Type": 0 },
    //        "ProtocolType": 0,
    //        "CollectCycle": 1000,
    //        "ReceiveTimeOut": 5000,
    //        "ConnectTimeOut": 3000,
    //        "Remark": "test",
    //        "IpAddress": "192.168.12.22",
    //        "ProtocolPort": 9600
    //    }
    //    """;

    //    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //}

    //[Fact]
    //public void Read_UnknownInterfaceType_ThrowsJsonException()
    //{
    //    var json = """
    //    {
    //        "Id": "1564sdfdsf48ee-sfds",
    //        "InterfaceType": 11,
    //        "ProtocolType": 0,
    //        "CollectCycle": 1000,
    //        "ReceiveTimeOut": 5000,
    //        "ConnectTimeOut": 3000,
    //        "Remark": "test",
    //        "IpAddress": "192.168.12.22",
    //        "ProtocolPort": 9600
    //    }
    //    """;

    //    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    //}
    //#endregion

    #region ProtocolType校验，类型是数字
    [Fact]
    public void Read_MissingProtocolType_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "ProtocolType": 0,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600
            }
            """;

        // Act
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    [Fact]
    public void Read_ProtocolTypeIsEmpty_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": "",
                "ProtocolType": 0,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600
            }
            """;

        // Act
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    [Fact]
    public void Read_ProtocolTypeIsNull_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": null,
                "ProtocolType": 0,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600
            }
            """;

        // Act
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    [Fact]
    public void Read_ProtocolTypeAsString_ThrowsJsonException()
    {
        var json = """
            {
                "Id": "1564sdfdsf48ee-sfds",
                "InterfaceType": "0",
                "ProtocolType": 0,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "IpAddress": "192.168.12.22",
                "ProtocolPort": 9600
            }
            """;

        // Act
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    [Fact]
    public void Read_ProtocolTypeTypeAsObject_ThrowsJsonException()
    {
        var json = """
        {
            "Id": "1564sdfdsf48ee-sfds",
            "InterfaceType": { "Type": 0 },
            "ProtocolType": 0,
            "CollectCycle": 1000,
            "ReceiveTimeOut": 5000,
            "ConnectTimeOut": 3000,
            "Remark": "test",
            "IpAddress": "192.168.12.22",
            "ProtocolPort": 9600
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }

    [Fact]
    public void Read_UnknownProtocolType_ThrowsJsonException()
    {
        var json = """
        {
            "Id": "1564sdfdsf48ee-sfds",
            "InterfaceType": 11,
            "ProtocolType": 0,
            "CollectCycle": 1000,
            "ReceiveTimeOut": 5000,
            "ConnectTimeOut": 3000,
            "Remark": "test",
            "IpAddress": "192.168.12.22",
            "ProtocolPort": 9600
        }
        """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, _options));
    }
    #endregion

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("123")]
    [InlineData("[1,2,3]")]
    public void Read_JsonIsNotObject_ThrowsJsonException(string input)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Read_EmptyJson_ThrowsJsonException(string input)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(input, _options));
    }

    [Fact]
    public void Read_NullJson_ThrowsArgumentNullException()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<ProtocolDto>(input!, _options));
    }

    [Fact]
    public void Read_ProtocolSubtypeFieldMissing_DeserializesWithDefaults()
    {

    }
}
