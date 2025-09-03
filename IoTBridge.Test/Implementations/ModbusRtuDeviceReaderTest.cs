using HslCommunication.Core;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTBridge.Test.Implementations;
public class ModbusRtuDeviceReaderTest
{
    [Fact]
    public async Task ReadDeviceAsync_ReturnsExpectedDeviceResponse()
    {
        // Arrange
        var pointReaderMock = new Mock<IModbusRtuPointReader>();
        var modbusRtu = new ModbusRtu(); // 可用mock或真实对象，因不会被直接调用硬件
        var deviceParams = new ModbusRtuDeviceParams
        {
            DeviceId = "D1",
            SlaveAddress = 2,
            ZeroBasedAddressing = true,
            DataFormat = DataFormat.ABCD,
            ReadMap = new ReadMapItem[]
            {
                new (DataType.Int, "100", null),
                new (DataType.Float, "200", null)
            },
        };

        // Mock每个点的读取结果
        pointReaderMock
            .Setup(x => x.ReadAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<int[]> { IsSuccess = true, Address = "100", Value = new[] { 123 } });

        pointReaderMock
            .Setup(x => x.ReadAsync(modbusRtu, deviceParams.ReadMap[1]))
            .ReturnsAsync(new ReadValue<float[]> { IsSuccess = false, Address = "200", Message = "设备故障" });

        var deviceReader = new ModbusRtuDeviceReader(pointReaderMock.Object);

        // Act
        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        // Assert
        response.DeviceId.Should().Be("D1");
        response.Values.Should().HaveCount(2);

        var intRes = response.Values[0] as ReadValue<int[]>;
        intRes.Should().NotBeNull();
        intRes.IsSuccess.Should().BeTrue();
        intRes.Address.Should().Be("100");
        intRes.Value.Should().Equal(new[] { 123 });

        var floatRes = response.Values[1] as ReadValue<float[]>;
        floatRes.Should().NotBeNull();
        floatRes.IsSuccess.Should().BeFalse();
        floatRes.Address.Should().Be("200");
        floatRes.Message.Should().Be("设备故障");
    }
}
