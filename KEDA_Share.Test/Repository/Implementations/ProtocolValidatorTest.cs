using FluentAssertions;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Test.Repository.Implementations;
public class ProtocolValidatorTest
{
    private readonly Mock<IValidator<Device>> _deviceValidatorMock;
    private readonly ProtocolValidator _validator;

    public ProtocolValidatorTest()
    {
        _deviceValidatorMock = new Mock<IValidator<Device>>();
        _validator = new ProtocolValidator(_deviceValidatorMock.Object);
    }

    [Fact]
    public void Validate_ShouldNotCheckStationNo_WhenProtocolTypeIsOther()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "IEC104", // 非目标关键字
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        // 断言不会因为StationNo为空而报错
        result.ErrorMessage.Should().NotContain("站号StationNo为空");
    }


    [Fact]
    public void Validate_ShouleReturnInvalid_WhenProtocolTypeIsEmptyString()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("[协议]存在协议类型ProtocolType为空的协议，请检查");
    }

    [Fact]
    public void Validate_ShouleReturnInvalid_WhenProtocolTypeIsNotExist()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("[协议]存在协议类型ProtocolType为空的协议，请检查");
    }

    [Theory]
    [InlineData("modbus")]
    [InlineData("MODBUS")]
    [InlineData("Maaabbb")]
    public void Validate_ShouleReturnInvalid_WhenProtocolTypeIsSimilarModbus(string protocolType)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = protocolType,
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"[协议]该协议{protocol.ProtocolType}暂未完成，请检查");
    }

    [Theory]
    [InlineData("DLT6452007OverTcp")]
    [InlineData("DLT6452007Serial")] 
    [InlineData("CJT188OverTcp_2004")] 
    public void Validate_ShouldReturnInvalid_WhenDLT6452007StationNoIsEmpty(string protocolType)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = protocolType,
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("站号StationNo为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenModbusStationNoIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain($"[Modbus系列]{protocol.ProtocolType}的Format为空");
    }

    [Theory]
    [InlineData("Modbussxxx")]
    [InlineData("DLT6452007")]
    public void Validate_ShouldReturnInvalid_WhenSimilarProtoType(string protocolType)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = protocolType,
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain($"[协议]该协议{protocol.ProtocolType}暂未完成，请检查");
    }

    [Theory]
    [InlineData("65536")]
    [InlineData("0")]
    public void Validate_ShouldReturnValid_WhenProtocolPortIs65535(string port)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = port,
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"[网口协议]{protocol.ProtocolType}的端口号ProtocolPort格式不正确，请检查");
    }

    [Theory]
    [InlineData("65535")]
    [InlineData("1")]
    public void Validate_ShouldReturnValid_WhenProtocolPortIs1(string port)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "DLT6452007OverTcp",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = port,
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("5")]
    [InlineData("8")]
    public void Validate_ShouldReturnValid_WhenDataBitsIs5(string dataBits)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "DLT6452007Serial",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = dataBits,
            StopBits = "One",
            Parity = "None",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("4")]
    [InlineData("9")]
    public void Validate_ShouldReturnInvalid_WhenDataBitsIs4(string dataBits)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = dataBits,
            StopBits = "One",
            Parity = "None",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };
        var validator = new ProtocolValidator(new DeviceValidator(new PointValidator()));
        var result = validator.Validate(protocol);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("数据位DataBits格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenProtocolIsNull()
    {
        var result = _validator.Validate(null);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("[协议]协议为空，请检查");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenInterfaceIsUnsupported()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "USB",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("接口类型是不支持的类型");
    }

    [Theory]
    [InlineData("", "LAN", "Modbus", "100", "100", "100", "[协议]存在id为空的协议，请检查")]
    [InlineData("P1", "", "Modbus", "100", "100", "100", "[协议]存在接口类型Interface为空的协议，请检查")]
    [InlineData("P1", "LAN", "", "100", "100", "100", "[协议]存在协议类型ProtocolType为空的协议，请检查")]
    [InlineData("P1", "LAN", "Modbus", "", "100", "100", "[协议]存在通讯延时CollectCycle为空的协议，请检查")]
    [InlineData("P1", "LAN", "Modbus", "100", "", "100", "[协议]存在接收超时ReceiveTimeOut为空的协议，请检查")]
    [InlineData("P1", "LAN", "Modbus", "100", "100", "", "[协议]存在连接超时ConnectTimeOut为空的协议，请检查")]
    public void Validate_ShouldReturnInvalid_WhenRequiredFieldIsEmpty(
        string protocolId, string iface, string type, string cycle, string timeout, string connectTimeout, string expectedMsg)
    {
        var protocol = new Protocol
        {
            ProtocolID = protocolId,
            Interface = iface,
            ProtocolType = type,
            CollectCycle = cycle,
            ReceiveTimeOut = timeout,
            ConnectTimeOut = connectTimeout
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expectedMsg);
    }

    [Theory]
    [InlineData("P1", "COM", "Modbus", "100", "100", "102", "", "9600", "8", "One", "None", "串口号PortName为空")]
    [InlineData("P1", "COM", "Modbus", "100", "100", "102", "COM1", "", "8", "One", "None", "波特率BaudRate为空")]
    [InlineData("P1", "COM", "Modbus", "100", "100", "102", "COM1", "9600", "", "One", "None", "数据位DataBits为空")]
    [InlineData("P1", "COM", "Modbus", "100", "100", "102", "COM1", "9600", "8", "", "None", "停止位StopBits为空")]
    [InlineData("P1", "COM", "Modbus", "100", "100", "102", "COM1", "9600", "8", "One", "", "校验位Parity为空")]
    public void Validate_ShouldReturnInvalid_WhenComRequiredFieldIsEmpty(
    string protocolId, string iface, string type, string cycle, string timeout, string connectTimeout,
    string portName, string baudRate, string dataBits, string stopBits, string parity, string expectedMsg)
    {
        var protocol = new Protocol
        {
            ProtocolID = protocolId,
            Interface = iface,
            ProtocolType = type,
            CollectCycle = cycle,
            ReceiveTimeOut = timeout,
            ConnectTimeOut = connectTimeout,
            PortName = portName,
            BaudRate = baudRate,
            DataBits = dataBits,
            StopBits = stopBits,
            Parity = parity
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expectedMsg);
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenLanIpIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "",
            ProtocolPort = "502"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ip地址IPAddress为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenLanIpIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "invalid_ip",
            ProtocolPort = "502"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ip地址IPAddress格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenLanPortIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = ""
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("端口号ProtocolPort为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenLanPortIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "70000"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("端口号ProtocolPort格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenLanIsCorrect()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "CDAB",
            AddressStartWithZero = "True",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };

        _deviceValidatorMock.Setup(x => x.Validate(It.IsAny<Device>())).Returns(new ValidationResult { IsValid = true });

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenComPortNameIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "",
            BaudRate = "9600",
            DataBits = "8",
            StopBits = "One",
            Parity = "None"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("串口号PortName为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenComBaudRateIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "99999",
            DataBits = "8",
            StopBits = "One",
            Parity = "None"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("波特率BaudRate格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenComDataBitsIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = "9",
            StopBits = "One",
            Parity = "None"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("数据位DataBits格式不正确");
    }


    [Fact]
    public void Validate_ShouldReturnInvalid_WhenComStopBitsIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = "8",
            StopBits = "Invalid",
            Parity = "None"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("停止位StopBits格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenComParityIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = "8",
            StopBits = "One",
            Parity = "Invalid"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("校验位Parity格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenComIsCorrect()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "COM",
            ProtocolType = "ModbusRtu",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            PortName = "COM1",
            BaudRate = "9600",
            DataBits = "8",
            StopBits = "One",
            Parity = "None",
            Format = "CDAB",
            AddressStartWithZero = "true",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };

        _deviceValidatorMock.Setup(x => x.Validate(It.IsAny<Device>())).Returns(new ValidationResult { IsValid = true });

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenModbusFormatIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "",
            AddressStartWithZero = "true"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Format为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenModbusFormatIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "Invalid",
            AddressStartWithZero = "true"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Format格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenModbusAddressStartWithZeroIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = ""
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("AddressStartWithZero为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenModbusAddressStartWithZeroIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "invalid"
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("AddressStartWithZero格式不正确");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenModbusIsCorrect()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "true",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };

        _deviceValidatorMock.Setup(x => x.Validate(It.IsAny<Device>())).Returns(new ValidationResult { IsValid = true });

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDevicesIsNull()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "true",
            Devices = null!
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("设备列表(Devices)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDevicesIsEmpty()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "true",
            Devices = []
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("设备列表(Devices)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenAnyDeviceIsInvalid()
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "true",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = "1", Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };

        _deviceValidatorMock.Setup(x => x.Validate(It.IsAny<Device>())).Returns(new ValidationResult { IsValid = false, ErrorMessage = "设备无效" });

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("设备无效");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldReturnInvalid_WhenModbusDLT645CJTDeviceStationNoIsNullOrEmpty(string? stationNo)
    {
        var protocol = new Protocol
        {
            ProtocolID = "P1",
            Interface = "LAN",
            ProtocolType = "Modbus",
            CollectCycle = "100",
            ReceiveTimeOut = "100",
            ConnectTimeOut = "100",
            IPAddress = "192.168.1.1",
            ProtocolPort = "502",
            Format = "ABCD",
            AddressStartWithZero = "true",
            Devices = [new Device { EquipmentID = "D1", EquipmentName = "设备A", StationNo = stationNo!, Points = [new Point { Label = "p1", DataType = "Int", Address = "100" }] }]
        };

        var result = _validator.Validate(protocol);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"[协议]{protocol.Devices[0].EquipmentID}的站号StationNo为空，设备名称是{protocol.Devices[0].EquipmentName}");
    }
}
