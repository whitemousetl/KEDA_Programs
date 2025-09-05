using FluentAssertions;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Moq;
using System.IO.Ports;

namespace IoTBridge.Test.Implementations.Modbus;

public class ModbusRtuServiceTest
{
    [Fact] // 1. 参数校验：参数为null
    public async Task ReadAsync_ParamsNull_ReturnsAllDeviceFailture()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(null);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("ModbusRtu参数或设备列表为空");
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 2. 参数校验：设备列表为空
    public async Task ReadAsync_DevicesEmpty_ReturnsAllDeviceFailture()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, []);
        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("ModbusRtu参数或设备列表为空");
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 3. 连接失败
    public async Task ReadAsync_ConnectionFailed_ReturnsAllDeviceFailture()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var param = new ModbusRtuParams(
            Operation.Read,
            "COM1",
            9600,
            8,
            StopBits.One,
            Parity.None,
            [
            new ModbusRtuDeviceParams("D1",null,null),
            new ModbusRtuDeviceParams("D2",null,null)
            ]);
        connMgrMock.Setup(m => m.GetConnection(param)).Returns((null, "串口打开失败", false));
        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("串口打开失败");
        response.DeviceResponses.Should().HaveCount(2);
        response.DeviceResponses.All(d => !d.IsSuccess && !d.IsOnline).Should().BeTrue();
    }

    [Fact] // 4. 设备全部成功
    public async Task ReadAsync_AllDeviceSuccess_ReturnsAllDeviceSuccess()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, StopBits.One, Parity.None, [
            new ModbusRtuDeviceParams ("D1", null,null),
            new ModbusRtuDeviceParams ("D2",  null,null)
        ]);
        var fakeConn = new ModbusRtu();
        connMgrMock.Setup(m => m.GetConnection(param)).Returns((fakeConn, null, true));

        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[0], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D1", IsSuccess = true, IsOnline = true });

        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[1], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D2", IsSuccess = true, IsOnline = true });

        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceSuccess);
        response.DeviceResponses.Should().HaveCount(2);
        response.DeviceResponses.All(d => d.IsSuccess).Should().BeTrue();
    }

    [Fact] // 5. 设备部分成功
    public async Task ReadAsync_PartialDeviceSuccess_ReturnsPartialDeviceSuccess()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, StopBits.One, Parity.None, [
           new ModbusRtuDeviceParams ("D1", null,null),
            new ModbusRtuDeviceParams ("D2", null,null)
        ]);
        var fakeConn = new ModbusRtu();
        connMgrMock.Setup(m => m.GetConnection(param)).Returns((fakeConn, null, true));
        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[0], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D1", IsSuccess = true, IsOnline = true });
        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[1], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D2", IsSuccess = false, IsOnline = false });
        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.PartialDeviceSuccess);
        response.DeviceResponses.Should().HaveCount(2);
        response.DeviceResponses.Count(d => d.IsSuccess).Should().Be(1);
        response.DeviceResponses.Count(d => !d.IsSuccess).Should().Be(1);
    }

    [Fact] // 6. 设备全部失败
    public async Task ReadAsync_AllDeviceFailture_ReturnsAllDeviceFailture()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, StopBits.One, Parity.None, [
            new ModbusRtuDeviceParams ("D1",  null,null),
            new ModbusRtuDeviceParams ("D2", null,null)
        ]);
        var fakeConn = new ModbusRtu();
        connMgrMock.Setup(m => m.GetConnection(param)).Returns((fakeConn, null, true));

        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[0], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D1", IsSuccess = false, IsOnline = false });

        deviceReaderMock.Setup(m => m.ReadDeviceAsync(param.Devices[1], fakeConn)).ReturnsAsync(new ModbusRtuDeviceResponse { DeviceId = "D2", IsSuccess = false, IsOnline = false });

        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.DeviceResponses.Should().HaveCount(2);
        response.DeviceResponses.All(d => !d.IsSuccess).Should().BeTrue();
    }

    [Fact] // 7. 异常处理
    public async Task ReadAsync_ThrowsException_ReturnsAllDeviceFailture()
    {
        var connMgrMock = new Mock<IModbusRtuConnectionManager>();
        var deviceReaderMock = new Mock<IModbusRtuDeviceReader>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, StopBits.One, Parity.None, [
               new ModbusRtuDeviceParams ("D1", null,null),
            new ModbusRtuDeviceParams ("D2",  null,null)
        ]);
        connMgrMock.Setup(m => m.GetConnection(param)).Throws(new InvalidOperationException("Test Exception"));
        var service = new ModbusRtuService(connMgrMock.Object, deviceReaderMock.Object);

        var response = await service.ReadAsync(param);

        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("Test Exception");
        response.DeviceResponses.Should().HaveCount(2);
        response.DeviceResponses.All(d => !d.IsSuccess && !d.IsOnline).Should().BeTrue();
        response.DeviceResponses.All(d => d.Message.Contains("Test Exception")).Should().BeTrue();
    }
}