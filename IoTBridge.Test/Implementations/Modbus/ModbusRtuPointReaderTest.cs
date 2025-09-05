using FluentAssertions;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.Device;
using HslCommunication.Core.Device.Fakes;
using HslCommunication.Core.Net.Fakes;
using HslCommunication.ModBus;
using HslCommunication.ModBus.Fakes;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Implementations.Modbus;
using KEDA_Share.Enums;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;

namespace IoTBridge.Test.Implementations.Modbus;
[Collection("ModbusShimTests")]
public class ModbusRtuPointReaderTest
{
    #region bool类型
    [Fact]
    public async Task ReadAsync_BoolType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };

            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { /* 可忽略 */ };

            ShimDeviceCommunication.AllInstances.ReadBoolAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<bool[]> { IsSuccess = true, Content = [true], Message = null, ErrorCode = 1000 });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", null);
            var reader = new ModbusRtuPointReader();

            // Act
            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            //Assert
            var res = result as ReadValue<bool[]>;
            res.Should().NotBeNull();

            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("100");
            res.Value.Should().Equal([true]);
        }
    }

    [Fact]
    public async Task ReadAsync_BoolType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                 DataFormatSetDataFormat = (instance) => { },
                 AddressStartWithZeroSetBoolean = (instance) => { },
                 StationSetByte = (instance) => { },
            };

            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { /* 可忽略 */ };

            ShimDeviceCommunication.AllInstances.ReadBoolAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<bool[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "101", null);
            var reader = new ModbusRtuPointReader();

            // Act
            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            // Assert
            var res = result as ReadValue<bool[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("101");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_BoolType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };

            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { /* 可忽略 */ };

            // 关键：直接抛出异常
            ShimDeviceCommunication.AllInstances.ReadBoolAsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "102", null);
            var reader = new ModbusRtuPointReader();

            // Act
            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            // Assert
            var res = result as ReadValue<bool[]>;
            res.Should().NotBeNull();

            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("102");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region ushort类型
    [Fact]
    public async Task ReadAsync_UShortType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadUInt16AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<ushort[]> { IsSuccess = true, Content = [120] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "100", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<ushort[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("100");
            res.Value.Should().Equal([120]);
        }
    }

    [Fact]
    public async Task ReadAsync_UShortType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadUInt16AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<ushort[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "101", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<ushort[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("101");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_UShortType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };

            ShimDeviceCommunication.AllInstances.ReadUInt16AsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "102", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<ushort[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("102");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region short类型
    [Fact]
    public async Task ReadAsync_ShortType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadInt16AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<short[]> { IsSuccess = true, Content = [123] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "200", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<short[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("200");
            res.Value.Should().Equal([123]);
        }
    }

    [Fact]
    public async Task ReadAsync_ShortType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadInt16AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<short[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "201", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<short[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("201");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_ShortType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadInt16AsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "202", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<short[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("202");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region uint类型
    [Fact]
    public async Task ReadAsync_UIntType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadUInt32AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<uint[]> { IsSuccess = true, Content = [123456] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "300", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<uint[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("300");
            res.Value.Should().Equal([123456]);
        }
    }

    [Fact]
    public async Task ReadAsync_UIntType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadUInt32AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<uint[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "301", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<uint[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("301");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_UIntType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadUInt32AsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "302", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<uint[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("302");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region int类型
    [Fact]
    public async Task ReadAsync_IntType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadInt32AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<int[]> { IsSuccess = true, Content = [654321] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "400", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<int[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("400");
            res.Value.Should().Equal([654321]);
        }
    }

    [Fact]
    public async Task ReadAsync_IntType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadInt32AsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<int[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "401", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<int[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("401");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_IntType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadInt32AsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "402", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<int[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("402");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region float类型
    [Fact]
    public async Task ReadAsync_FloatType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadFloatAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<float[]> { IsSuccess = true, Content = [1.23f] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "500", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<float[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("500");
            res.Value.Should().Equal([1.23f]);
        }
    }

    [Fact]
    public async Task ReadAsync_FloatType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadFloatAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<float[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "501", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<float[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("501");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_FloatType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadFloatAsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "502", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<float[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("502");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region double类型
    [Fact]
    public async Task ReadAsync_DoubleType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadDoubleAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<double[]> { IsSuccess = true, Content = [2.34] });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "600", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<double[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("600");
            res.Value.Should().Equal([2.34]);
        }
    }

    [Fact]
    public async Task ReadAsync_DoubleType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadDoubleAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<double[]> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "601", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<double[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("601");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_DoubleType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimModbusRtu.AllInstances.ReadDoubleAsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "602", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<double[]>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("602");
            res.Value.Should().BeNull();
        }
    }
    #endregion

    #region string类型
    [Fact]
    public async Task ReadAsync_StringType_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadStringAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<string> { IsSuccess = true, Content = "Hello" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "700", 5);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<string>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Message.Should().BeNull();
            res.Address.Should().Be("700");
            res.Value.Should().Be("Hello");
        }
    }

    [Fact]
    public async Task ReadAsync_StringType_ReturnFail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadStringAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<string> { IsSuccess = false, Message = "设备故障" });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "701", 5);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<string>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("设备故障");
            res.Address.Should().Be("701");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_StringType_TimeOutException()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadStringAsyncStringUInt16 = (instance, address, length) =>
            {
                throw new TimeoutException("通讯超时");
            };

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "702", 5);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<string>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeFalse();
            res.Message.Should().Be("通讯超时");
            res.Address.Should().Be("702");
            res.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task ReadAsync_StringType_LengthNotSpecified_ReturnSuccess()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu
            {
                DataFormatSetDataFormat = (instance) => { },
                AddressStartWithZeroSetBoolean = (instance) => { },
                StationSetByte = (instance) => { },
            };
            ShimBinaryCommunication.AllInstances.ReceiveTimeOutSetInt32 = (instance, value) => { };
            ShimDeviceCommunication.AllInstances.ReadStringAsyncStringUInt16 = (instance, address, length) =>
                Task.FromResult(new OperateResult<string> { Content = "cyr zha l", IsSuccess = true });

            var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "703", null);
            var reader = new ModbusRtuPointReader();

            var result = await reader.ReadAsync(shimModbusRtu.Instance, point);

            var res = result as ReadValue<string>;
            res.Should().NotBeNull();
            res.IsSuccess.Should().BeTrue();
            res.Address.Should().Be("703");
            res.Value.Should().Be("cyr zha l");
        }
    }
    #endregion
}
