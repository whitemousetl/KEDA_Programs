using FluentAssertions;
using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using KEDA_Share.Enums;
using Moq;

namespace IoTBridge.Test.Implementations;
public class ModbusRtuPointReaderTest
{
    #region bool类型
    [Fact]
    public async Task ReadAsync_BoolType_ReturnSuccess()
    {
        //Arrange
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadBoolAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<bool[]> { IsSuccess = true, Content = [true],  Message = null, ErrorCode = 1000 });

        var point = new ReadMapItem(DataType.Bool, "100", null);

        var reader = new ModbusRtuPointReader();

        //Act
        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        //Assert
        var res = result as ReadValue<bool[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("100");
        res.Value.Should().Equal([true]);
    }

    [Fact]
    public async Task ReadAsync_BoolType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadBoolAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<bool[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.Bool, "101", null);

        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<bool[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("101");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_BoolType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadBoolAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.Bool, "102", null);

        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<bool[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("102");
        res.Value.Should().BeNull();
    }
    #endregion

    #region ushort类型
    [Fact]
    public async Task ReadAsync_UShortType_ReturnSuccess()
    {
        //Arrange
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<ushort[]> { IsSuccess = true, Content = [120] });

        var point = new ReadMapItem(DataType.UShort, "100", null);

        var reader = new ModbusRtuPointReader();

        //Act
        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        //Assert
        var res = result as ReadValue<ushort[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("100");
        res.Value.Should().Equal([120]);
    }

    [Fact]
    public async Task ReadAsync_UShortType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<ushort[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.UShort, "101", null);

        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<ushort[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("101");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_UShortType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.UShort, "102", null);

        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<ushort[]>;
        res.Should().NotBeNull();

        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("102");
        res.Value.Should().BeNull();
    }
    #endregion

    #region short类型
    [Fact]
    public async Task ReadAsync_ShortType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<short[]> { IsSuccess = true, Content = [123] });

        var point = new ReadMapItem(DataType.Short, "200", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<short[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("200");
        res.Value.Should().Equal([123]);
    }

    [Fact]
    public async Task ReadAsync_ShortType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<short[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.Short, "201", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<short[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("201");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_ShortType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt16Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.Short, "202", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<short[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("202");
        res.Value.Should().BeNull();
    }
    #endregion

    #region uint类型
    [Fact]
    public async Task ReadAsync_UIntType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<uint[]> { IsSuccess = true, Content = [123456] });

        var point = new ReadMapItem(DataType.UInt, "300", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<uint[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("300");
        res.Value.Should().Equal([123456]);
    }

    [Fact]
    public async Task ReadAsync_UIntType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<uint[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.UInt, "301", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<uint[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("301");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_UIntType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadUInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.UInt, "302", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<uint[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("302");
        res.Value.Should().BeNull();
    }
    #endregion

    #region int类型
    [Fact]
    public async Task ReadAsync_IntType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<int[]> { IsSuccess = true, Content = [654321] });

        var point = new ReadMapItem(DataType.Int, "400", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<int[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("400");
        res.Value.Should().Equal([654321]);
    }

    [Fact]
    public async Task ReadAsync_IntType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<int[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.Int, "401", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<int[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("401");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_IntType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadInt32Async(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.Int, "402", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<int[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("402");
        res.Value.Should().BeNull();
    }
    #endregion

    #region float类型
    [Fact]
    public async Task ReadAsync_FloatType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadFloatAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<float[]> { IsSuccess = true, Content = [1.23f] });

        var point = new ReadMapItem(DataType.Float, "500", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<float[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("500");
        res.Value.Should().Equal([1.23f]);
    }

    [Fact]
    public async Task ReadAsync_FloatType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadFloatAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<float[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.Float, "501", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<float[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("501");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_FloatType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadFloatAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.Float, "502", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<float[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("502");
        res.Value.Should().BeNull();
    }
    #endregion

    #region double类型
    [Fact]
    public async Task ReadAsync_DoubleType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadDoubleAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<double[]> { IsSuccess = true, Content = [2.34] });

        var point = new ReadMapItem(DataType.Double, "600", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<double[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("600");
        res.Value.Should().Equal([2.34]);
    }

    [Fact]
    public async Task ReadAsync_DoubleType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadDoubleAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<double[]> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.Double, "601", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<double[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("601");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_DoubleType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadDoubleAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.Double, "602", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<double[]>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("602");
        res.Value.Should().BeNull();
    }
    #endregion

    #region string类型
    [Fact]
    public async Task ReadAsync_StringType_ReturnSuccess()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadStringAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<string> { IsSuccess = true, Content = "Hello" });

        var point = new ReadMapItem(DataType.String, "700", 5);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().BeNull();
        res.Address.Should().Be("700");
        res.Value.Should().Be("Hello");
    }

    [Fact]
    public async Task ReadAsync_StringType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadStringAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ReturnsAsync(new OperateResult<string> { IsSuccess = false, Message = "设备故障" });

        var point = new ReadMapItem(DataType.String, "701", 5);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("设备故障");
        res.Address.Should().Be("701");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_StringType_TimeOutException()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        modbusRtuMock
            .Setup(m => m.ReadStringAsync(It.IsAny<string>(), It.IsAny<ushort>()))
            .ThrowsAsync(new TimeoutException("通讯超时"));

        var point = new ReadMapItem(DataType.String, "702", 5);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("通讯超时");
        res.Address.Should().Be("702");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_StringType_LengthNotSpecified_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        // 不需要设置mock，因为不会调用ReadStringAsync

        var point = new ReadMapItem(DataType.String, "703", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("String类型必须指定length");
        res.Address.Should().Be("703");
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_UnsupportedDataType_ReturnFail()
    {
        var modbusRtuMock = new Mock<ModbusRtu>();
        // 不需要设置mock，因为不会调用任何Read方法

        // 强制转换一个不存在的枚举值
        var unsupportedType = (DataType)999;
        var point = new ReadMapItem(unsupportedType, "800", null);
        var reader = new ModbusRtuPointReader();

        var result = await reader.ReadAsync(modbusRtuMock.Object, point);

        var res = result as ReadValue<string>;
        res.Should().NotBeNull();
        res.IsSuccess.Should().BeFalse();
        res.Message.Should().Be("不支持的数据类类型，请检查");
        res.Address.Should().Be("800");
        res.Value.Should().BeNull();
    }
    #endregion
}
