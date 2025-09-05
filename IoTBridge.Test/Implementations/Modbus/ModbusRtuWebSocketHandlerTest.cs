using FluentAssertions;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Moq;
using Newtonsoft.Json;

namespace IoTBridge.Test.Implementations.Modbus;

public class ModbusRtuWebSocketHandlerTest
{
    [Fact] // 1. 空参数
    public async Task HandleRequestAsync_EmptyJson_ReturnsError()
    {
        var serviceMock = new Mock<IModbusRtuService>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();
        var handler = new ModbusRtuWebSocketHandler(serviceMock.Object, notifierMock.Object);

        var result = await handler.HandleRequestAsync("");

        var response = JsonConvert.DeserializeObject<ModbusRtuResponse>(result);
        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("请求参数为空");
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 2. 参数解析失败
    public async Task HandleRequestAsync_InvalidJson_ReturnsError()
    {
        var serviceMock = new Mock<IModbusRtuService>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();
        var handler = new ModbusRtuWebSocketHandler(serviceMock.Object, notifierMock.Object);

        var result = await handler.HandleRequestAsync("{invalid json}");

        var response = JsonConvert.DeserializeObject<ModbusRtuResponse>(result);
        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().StartWith("参数解析失败:");
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 3. 正常业务调用（成功）
    public async Task HandleRequestAsync_ValidJson_ReturnsServiceResult()
    {
        var serviceMock = new Mock<IModbusRtuService>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, []);
        var expected = new ModbusRtuResponse { ProtocolStatus = ProtocolStatus.AllDeviceSuccess, DeviceResponses = [] };
        serviceMock.Setup(s => s.ReadAsync(It.IsAny<ModbusRtuParams>())).ReturnsAsync(expected);
        var handler = new ModbusRtuWebSocketHandler(serviceMock.Object, notifierMock.Object);

        var json = JsonConvert.SerializeObject(param);
        var result = await handler.HandleRequestAsync(json);

        var response = JsonConvert.DeserializeObject<ModbusRtuResponse>(result);
        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceSuccess);
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 4. 正常业务调用（失败）
    public async Task HandleRequestAsync_ValidJson_ReturnsServiceFailResult()
    {
        var serviceMock = new Mock<IModbusRtuService>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, []);
        var expected = new ModbusRtuResponse { ProtocolStatus = ProtocolStatus.AllDeviceFailture, ErrorMessage = "业务失败", DeviceResponses = [] };
        serviceMock.Setup(s => s.ReadAsync(It.IsAny<ModbusRtuParams>())).ReturnsAsync(expected);
        var handler = new ModbusRtuWebSocketHandler(serviceMock.Object, notifierMock.Object);

        var json = JsonConvert.SerializeObject(param);
        var result = await handler.HandleRequestAsync(json);

        var response = JsonConvert.DeserializeObject<ModbusRtuResponse>(result);
        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Be("业务失败");
        response.DeviceResponses.Should().BeEmpty();
    }

    [Fact] // 5. 业务服务抛出异常
    public async Task HandleRequestAsync_ServiceThrowsException_ReturnsError()
    {
        var serviceMock = new Mock<IModbusRtuService>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();
        var param = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, []);
        var exceptionMessage = "服务异常";

        serviceMock.Setup(s => s.ReadAsync(It.IsAny<ModbusRtuParams>()))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var handler = new ModbusRtuWebSocketHandler(serviceMock.Object, notifierMock.Object);
        var json = JsonConvert.SerializeObject(param);
        var result = await handler.HandleRequestAsync(json);

        var response = JsonConvert.DeserializeObject<ModbusRtuResponse>(result);
        response.ProtocolStatus.Should().Be(ProtocolStatus.AllDeviceFailture);
        response.ErrorMessage.Should().Contain("服务异常");
        response.DeviceResponses.Should().BeEmpty();
    }
}