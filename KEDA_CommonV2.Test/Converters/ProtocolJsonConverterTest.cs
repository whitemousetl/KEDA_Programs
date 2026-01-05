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
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, JsonOptionsProvider.WorkstationOptions);

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
                "ProtocolType": 20,
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
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, JsonOptionsProvider.WorkstationOptions);

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
                "ProtocolType": 24,
                "CollectCycle": 1000,
                "ReceiveTimeOut": 5000,
                "ConnectTimeOut": 3000,
                "Remark": "test",
                "RequestMethod": 0,
                "AccessApiString": "https://s5xikogysg.feishu.cn/wiki/DtZawPgVhinGcvkq7uecCos3nAd"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, JsonOptionsProvider.WorkstationOptions);

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
                "ProtocolType": 25,
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

        // Act
        var result = JsonSerializer.Deserialize<ProtocolDto>(json, JsonOptionsProvider.WorkstationOptions);

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
            Assert.Equal("keda", dbProtocolDto.DatabaseAccount);
            Assert.Equal("root", dbProtocolDto.DatabasePassword);
            Assert.Equal("collector", dbProtocolDto.DatabaseName);
            Assert.Equal("select * from collector where name = 'keda'", dbProtocolDto.QuerySqlString);
        }
    }
    #endregion

    [Fact]
    public void Read_MissingInterfaceType_ThrowsJsonException()
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
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProtocolDto>(json, JsonOptionsProvider.WorkstationOptions));
    }
}
