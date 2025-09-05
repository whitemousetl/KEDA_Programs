using FluentAssertions;
using HslCommunication.Core;
using HslCommunication.Core.Device.Fakes;
using HslCommunication.ModBus.Fakes;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Implementations.Modbus;
using KEDA_Share.Enums;
using Microsoft.QualityTools.Testing.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTBridge.Test.Implementations.Modbus;
[Collection("ModbusShimTests")]
public class ModbusRtuPointWriterTest
{
    #region 写入成功
    [Fact]
    public async Task WriteAsync_BoolType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimModbusRtu.AllInstances.WriteAsyncStringBoolean = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", true);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_ShortType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimModbusRtu.AllInstances.WriteAsyncStringInt16 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "101", (short)123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_UShortType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimModbusRtu.AllInstances.WriteAsyncStringUInt16 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "102", (ushort)456);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_IntType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimDeviceCommunication.AllInstances.WriteAsyncStringInt32 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "103", 789);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_UIntType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimDeviceCommunication.AllInstances.WriteAsyncStringUInt32 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "104", (uint)123u);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_FloatType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimDeviceCommunication.AllInstances.WriteAsyncStringSingle = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "105", 1.23f);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_DoubleType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimDeviceCommunication.AllInstances.WriteAsyncStringDouble = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "106", 4.56);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }

    [Fact]
    public async Task WriteAsync_StringType_Success()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();

            ShimDeviceCommunication.AllInstances.WriteAsyncStringString = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = true });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "107", "test");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
        }
    }
    #endregion

    #region 写入失败
    [Fact]
    public async Task WriteAsync_BoolType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimModbusRtu.AllInstances.WriteAsyncStringBoolean = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "200", true);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_ShortType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimModbusRtu.AllInstances.WriteAsyncStringInt16 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "201", (short)123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_UShortType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimModbusRtu.AllInstances.WriteAsyncStringUInt16 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "202", (ushort)456);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_IntType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringInt32 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "203", 789);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_UIntType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringUInt32 = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "204", (uint)123u);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_FloatType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringSingle = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "205", 1.23f);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_DoubleType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringDouble = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "206", 4.56);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }

    [Fact]
    public async Task WriteAsync_StringType_Fail()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringString = (instance, address, value) =>
                Task.FromResult(new HslCommunication.OperateResult { IsSuccess = false, Message = "zhqlysljscyrzhacrl" });

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "207", "test");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("zhqlysljscyrzhacrl");
        }
    }
    #endregion

    #region 写入类型不匹配异常
    [Fact]
    public async Task WriteAsync_BoolType_ValueNotBool_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "300", "notBool");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望bool");
        }
    }

    [Fact]
    public async Task WriteAsync_ShortType_ValueNotShort_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Short, "301", 123.45);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望short");
        }
    }

    [Fact]
    public async Task WriteAsync_UShortType_ValueNotUShort_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UShort, "302", 123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望ushort");
        }
    }

    [Fact]
    public async Task WriteAsync_IntType_ValueNotInt_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "303", "notInt");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望int");
        }
    }

    [Fact]
    public async Task WriteAsync_UIntType_ValueNotUInt_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.UInt, "304", 123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望uint");
        }
    }

    [Fact]
    public async Task WriteAsync_FloatType_ValueNotFloat_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Float, "305", "notFloat");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望float");
        }
    }

    [Fact]
    public async Task WriteAsync_DoubleType_ValueNotDouble_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Double, "306", "notDouble");

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望double");
        }
    }

    [Fact]
    public async Task WriteAsync_StringType_ValueNotString_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "307", 123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望string");
        }
    }

    #endregion

    #region 不支持的数据类型异常
    [Fact]
    public async Task WriteAsync_UnsupportedDataType_ReturnsFalseWithNotSupportedMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            // 强制转换为 DataType 不支持的值
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, (DataType)999, "400", 123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("不支持的数据类型");
            msg.Should().Contain("NotSupportedException");
        }
    }

    #endregion

    #region 写入过程中发生异常
    [Fact]
    public async Task WriteAsync_BoolType_ThrowsTimeoutException_ReturnsFalseWithExceptionMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimModbusRtu.AllInstances.WriteAsyncStringBoolean = (instance, address, value) =>
                throw new TimeoutException("通讯超时");

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "500", true);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("TimeoutException");
            msg.Should().Contain("通讯超时");
        }
    }

    [Fact]
    public async Task WriteAsync_IntType_ThrowsIOException_ReturnsFalseWithExceptionMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            ShimDeviceCommunication.AllInstances.WriteAsyncStringInt32 = (instance, address, value) =>
                throw new System.IO.IOException("IO错误");

            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "501", 123);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("IOException");
            msg.Should().Contain("IO错误");
        }
    }

    #endregion

    #region Value为null类型不匹配

    [Fact]
    public async Task WriteAsync_BoolType_ValueNull_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "600", null);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望bool");
        }
    }

    [Fact]
    public async Task WriteAsync_IntType_ValueNull_ReturnsFalseWithInvalidCastMessage()
    {
        using (ShimsContext.Create())
        {
            var shimModbusRtu = new ShimModbusRtu();
            var writer = new ModbusRtuPointWriter();
            var point = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Int, "601", null);

            var (isSuccess, msg) = await writer.WriteAsync(shimModbusRtu.Instance, point);
            isSuccess.Should().BeFalse();
            msg.Should().Contain("Value类型错误，期望int");
        }
    }

    #endregion

}
