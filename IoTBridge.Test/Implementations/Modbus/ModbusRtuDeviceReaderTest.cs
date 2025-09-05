using FluentAssertions;
using HslCommunication.Core;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Moq;

namespace IoTBridge.Test.Implementations.Modbus;
public class ModbusRtuDeviceReaderTest
{
    [Fact]
    public async Task ReadDeviceAsync_ReturnsRightModbusRtuDeviceResponse()
    {
        // Arrange
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu(); // 可用mock或真实对象，因不会被直接调用硬件
        var deviceParams = new ModbusRtuDeviceParams(
            "D1",
            [
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "200", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "300", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "400", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "500", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "600", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "700", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "800", 10),
                ],
            null);


        // Mock每个点的读取结果
        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<bool[]> { IsSuccess = true, Address = "100", Value = [true] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[1]))
            .ReturnsAsync(new ReadValue<ushort[]> { IsSuccess = true, Address = "200", Value = [123] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[2]))
            .ReturnsAsync(new ReadValue<short[]> { IsSuccess = true, Address = "300", Value = [-123] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[3]))
            .ReturnsAsync(new ReadValue<uint[]> { IsSuccess = true, Address = "400", Value = [1_000_000] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[4]))
            .ReturnsAsync(new ReadValue<int[]> { IsSuccess = true, Address = "500", Value = [-1_000_000] });


        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[5]))
            .ReturnsAsync(new ReadValue<float[]> { IsSuccess = true, Address = "600", Value = [7.665f] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[6]))
            .ReturnsAsync(new ReadValue<double[]> { IsSuccess = true, Address = "700", Value = [9.364134564] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[7]))
            .ReturnsAsync(new ReadValue<string> { IsSuccess = true, Address = "700", Value = "ljs" });

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        // Act
        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        // Assert
        response.DeviceId.Should().Be("D1");
        response.Values.Should().HaveCount(8);
        response.IsOnline.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();

        var boolRes = response.Values[0] as ReadValue<bool[]>;
        boolRes.Should().NotBeNull();
        boolRes.IsSuccess.Should().BeTrue();
        boolRes.Address.Should().Be("100");
        boolRes.Value.Should().Equal([true]);

        var ushortRes = response.Values[1] as ReadValue<ushort[]>;
        ushortRes.Should().NotBeNull();
        ushortRes.IsSuccess.Should().BeTrue();
        ushortRes.Address.Should().Be("200");
        ushortRes.Value.Should().Equal([123]);

        var shortRes = response.Values[2] as ReadValue<short[]>;
        shortRes.Should().NotBeNull();
        shortRes.IsSuccess.Should().BeTrue();
        shortRes.Address.Should().Be("300");
        shortRes.Value.Should().Equal([-123]);

        var uintRes = response.Values[3] as ReadValue<uint[]>;
        uintRes.Should().NotBeNull();
        uintRes.IsSuccess.Should().BeTrue();
        uintRes.Address.Should().Be("400");
        uintRes.Value.Should().Equal([1_000_000]);

        var intRes = response.Values[4] as ReadValue<int[]>;
        intRes.Should().NotBeNull();
        intRes.IsSuccess.Should().BeTrue();
        intRes.Address.Should().Be("500");
        intRes.Value.Should().Equal([-1_000_000]);


        var floatRes = response.Values[5] as ReadValue<float[]>;
        floatRes.Should().NotBeNull();
        floatRes.IsSuccess.Should().BeTrue();
        floatRes.Address.Should().Be("600");
        floatRes.Value.Should().Equal([7.665f]);

        var doubleRes = response.Values[6] as ReadValue<double[]>;
        doubleRes.Should().NotBeNull();
        doubleRes.IsSuccess.Should().BeTrue();
        doubleRes.Address.Should().Be("700");
        doubleRes.Value.Should().Equal([9.364134564]);

        var stringRes = response.Values[7] as ReadValue<string>;
        stringRes.Should().NotBeNull();
        stringRes.IsSuccess.Should().BeTrue();
        stringRes.Address.Should().Be("700");
        stringRes.Value.Should().Be("ljs");
    }

    [Fact]
    public async Task ReadDeviceAsync_PointReadFailOrException_AggregatesFailure()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu();
        var deviceParams = new ModbusRtuDeviceParams(
            "D2",
            [
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "100", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "200", null)
            ],
            null);

        // 第一个点正常，第二个点抛异常
        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<int[]> { IsSuccess = true, Address = "100", Value = [1] });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[1]))
            .ReturnsAsync(new ReadValue<float[]>
            {
                IsSuccess = false,
                Address = "200",
                Message = "通讯超时"
            });

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be("D2");
        response.Values.Should().HaveCount(2);
        response.IsSuccess.Should().BeFalse();
        response.IsOnline.Should().BeTrue();
        response.Message.Should().Be("设备采集点部分异常，请检查");

        var intRes = response.Values[0] as ReadValue<int[]>;
        intRes.Should().NotBeNull();
        intRes.IsSuccess.Should().BeTrue();
        intRes.Value.Should().Equal([1]);

        var floatRes = response.Values[1] as ReadValue<float[]>;
        floatRes.Should().NotBeNull();
        floatRes.IsSuccess.Should().BeFalse();
        floatRes.Message.Should().Contain("通讯超时");
    }

    [Fact]
    public async Task ReadDeviceAsync_PointReadAllFail_ReturnDeviceOffline()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu();
        var deviceParams = new ModbusRtuDeviceParams(
            "D2",
            [
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "100", null),
                new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "200", null)
            ],
            null);

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<int[]>
            {
                IsSuccess = false,
                Address = "100",
                Message = "通讯超时"
            });

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[1]))
            .ReturnsAsync(new ReadValue<float[]>
            {
                IsSuccess = false,
                Address = "200",
                Message = "通讯超时"
            });

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be("D2");
        response.Values.Should().HaveCount(2);
        response.IsSuccess.Should().BeFalse();
        response.IsOnline.Should().BeFalse();
        response.Message.Should().Be("设备异常或离线，请检查");

        var intRes = response.Values[0] as ReadValue<int[]>;
        intRes.Should().NotBeNull();
        intRes.IsSuccess.Should().BeFalse();
        intRes.Message.Should().Contain("通讯超时");

        var floatRes = response.Values[1] as ReadValue<float[]>;
        floatRes.Should().NotBeNull();
        floatRes.IsSuccess.Should().BeFalse();
        floatRes.Message.Should().Contain("通讯超时");
    }

    [Fact]
    public async Task ReadDeviceAsync_EmptyReadMap_ReturnsEmptyValues()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu();
        var deviceParams = new ModbusRtuDeviceParams(
            "D3",
            [],
            null);

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be("D3");
        response.IsOnline.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("设备的读取参数列表为空");
        response.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadDeviceAsync_NullReadMap_ReturnsEmptyValues()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();

        var modbusRtu = new ModbusRtu();
        var deviceParams = new ModbusRtuDeviceParams(
            "D3",
            null,
            null);

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be("D3");
        response.IsOnline.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("设备的读取参数列表为空");
        response.Values.Should().BeEmpty();
    }

    [Fact]// 验证DeviceId为空时行为
    public async Task ReadDeviceAsync_DeviceParamsBoundaryValues()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu();
        var deviceParams = new ModbusRtuDeviceParams(
            "", // DeviceId为空
            [new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "1", null)],
            null);

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<bool[]> { IsSuccess = true, Address = "1", Value = [false] });

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be(""); // 验证DeviceId为空时行为
        response.Values.Should().HaveCount(1);
        response.IsOnline.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
        response.Values[0].Address.Should().Be("1");
    }

    [Fact]
    public async Task ReadDeviceAsync_UnsupportedDataType_ReturnsErrorValue()
    {
        var coordinatorMock = new Mock<IModbusRtuCoordinator>();
        var modbusRtu = new ModbusRtu();
        var unsupportedType = (DataType)999;
        var deviceParams = new ModbusRtuDeviceParams(
            "D5", 
            [new ReadMapItem(1, true, DataFormat.ABCD, 1000, unsupportedType, "999", null)],
            null);

        coordinatorMock
            .Setup(x => x.ReadWithWritePrioritizeAsync(modbusRtu, deviceParams.ReadMap[0]))
            .ReturnsAsync(new ReadValue<string>
            {
                IsSuccess = false,
                Address = "999",
                Message = "不支持的数据类类型，请检查"
            });

        var deviceReader = new ModbusRtuDeviceReader(coordinatorMock.Object);

        var response = await deviceReader.ReadDeviceAsync(deviceParams, modbusRtu);

        response.DeviceId.Should().Be("D5");
        response.Values.Should().HaveCount(1);
        response.IsOnline.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();

        var res = response.Values[0] as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("不支持的数据类类型，请检查");
        res.Address.Should().Be("999");
    }
}
