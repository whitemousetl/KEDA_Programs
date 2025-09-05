using FluentAssertions;
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

namespace IoTBridge.Test.Implementations.Modbus;
public class ModbusRtuCoordinatorTest
{
    [Fact]//仅读
    public async Task ReadWithWritePrioritizeAsync_NoWriteTask_OnlyRead()
    {
        var readerMock = new Mock<IModbusRtuPointReader>();
        var writerMock = new Mock<IModbusRtuPointWriter>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();

        notifierMock.Setup(n => n.TryQueue(out It.Ref<WriteMapItem?>.IsAny)).Returns(false);
        var expectedRead = new ReadValue<string> { IsSuccess = true, Address = "100", Value = "ok" };
        readerMock.Setup(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>())).ReturnsAsync(expectedRead);

        var coordinator = new ModbusRtuCoordinator(readerMock.Object, writerMock.Object, notifierMock.Object);
        var result = await coordinator.ReadWithWritePrioritizeAsync(new ModbusRtu(), new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "100", null));

        readerMock.Verify(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>()), Times.Once);
        writerMock.Verify(w => w.WriteAsync(It.IsAny<ModbusRtu>(), It.IsAny<WriteMapItem>()), Times.Never);
        result.Should().BeSameAs(expectedRead);
    }

    [Fact]//先写后读
    public async Task ReadWithWritePrioritizeAsync_MultiWriteTask_AllWriteThenRead()
    {
        var readerMock = new Mock<IModbusRtuPointReader>();
        var writerMock = new Mock<IModbusRtuPointWriter>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();

        var writeItems = new[] {
        new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "200", true),
        new WriteMapItem(2, true, DataFormat.ABCD, 1000, DataType.Int, "201", 123)};
        int callCount = 0;
        notifierMock.Setup(n => n.TryQueue(out It.Ref<WriteMapItem?>.IsAny))
            .Returns((out WriteMapItem? item) =>
            {
                if (callCount < writeItems.Length)
                {
                    item = writeItems[callCount++];
                    return true;
                }
                item = null;
                return false;
            });

        writerMock.Setup(w => w.WriteAsync(It.IsAny<ModbusRtu>(), It.IsAny<WriteMapItem>()))
            .ReturnsAsync((true, null));
        var expectedRead = new ReadValue<string> { IsSuccess = true, Address = "100", Value = "ok" };
        readerMock.Setup(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>())).ReturnsAsync(expectedRead);

        var coordinator = new ModbusRtuCoordinator(readerMock.Object, writerMock.Object, notifierMock.Object);
        var result = await coordinator.ReadWithWritePrioritizeAsync(new ModbusRtu(), new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "100", null));

        writerMock.Verify(w => w.WriteAsync(It.IsAny<ModbusRtu>(), It.IsAny<WriteMapItem>()), Times.Exactly(writeItems.Length));
        readerMock.Verify(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>()), Times.Once);
        result.Should().BeSameAs(expectedRead);
    }

    [Fact]//写入失败但读取正常
    public async Task ReadWithWritePrioritizeAsync_WriteTaskThrowsException_ReturnsReadValue()
    {
        var readerMock = new Mock<IModbusRtuPointReader>();
        var writerMock = new Mock<IModbusRtuPointWriter>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();

        var writeItem = new WriteMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "200", true);
        int callCount = 0;
        notifierMock.Setup(n => n.TryQueue(out It.Ref<WriteMapItem?>.IsAny))
            .Returns((out WriteMapItem? item) =>
            {
                if (callCount == 0)
                {
                    item = writeItem;
                    callCount++;
                    return true;
                }
                item = null;
                return false;
            });

        writerMock.Setup(w => w.WriteAsync(It.IsAny<ModbusRtu>(), It.IsAny<WriteMapItem>()))
            .ThrowsAsync(new TimeoutException("写入超时"));

        // 修正：设置读操作返回正常结果
        var expectedRead = new ReadValue<string> { IsSuccess = true, Address = "100", Value = "ok" };
        readerMock.Setup(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>())).ReturnsAsync(expectedRead);

        var coordinator = new ModbusRtuCoordinator(readerMock.Object, writerMock.Object, notifierMock.Object);
        var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", null);
        var result = await coordinator.ReadWithWritePrioritizeAsync(new ModbusRtu(), point);

        // 验证写异常不影响读结果
        result.Should().BeSameAs(expectedRead);
        result.IsSuccess.Should().BeTrue();
        result.Address.Should().Be("100");
    }

    [Fact]
    public async Task ReadWithWritePrioritizeAsync_ReadTaskThrowsException_ReturnsFailReadValue()
    {
        var readerMock = new Mock<IModbusRtuPointReader>();
        var writerMock = new Mock<IModbusRtuPointWriter>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();

        notifierMock.Setup(n => n.TryQueue(out It.Ref<WriteMapItem?>.IsAny)).Returns(false);
        readerMock.Setup(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>()))
            .ThrowsAsync(new TimeoutException("读超时"));

        var coordinator = new ModbusRtuCoordinator(readerMock.Object, writerMock.Object, notifierMock.Object);
        var point = new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", null);
        var result = await coordinator.ReadWithWritePrioritizeAsync(new ModbusRtu(), point);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("读超时");
        result.Address.Should().Be("100");
    }

    [Fact]
    public async Task ReadWithWritePrioritizeAsync_WriteTaskIsNull_SkipWrite()
    {
        var readerMock = new Mock<IModbusRtuPointReader>();
        var writerMock = new Mock<IModbusRtuPointWriter>();
        var notifierMock = new Mock<IModbusRtuWriteNotifier>();

        int callCount = 0;
        notifierMock.Setup(n => n.TryQueue(out It.Ref<WriteMapItem?>.IsAny))
            .Returns((out WriteMapItem? item) =>
            {
                if (callCount == 0)
                {
                    item = null;
                    callCount++;
                    return true;
                }
                item = null;
                return false;
            });

        var expectedRead = new ReadValue<string> { IsSuccess = true, Address = "100", Value = "ok" };
        readerMock.Setup(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>())).ReturnsAsync(expectedRead);

        var coordinator = new ModbusRtuCoordinator(readerMock.Object, writerMock.Object, notifierMock.Object);
        var result = await coordinator.ReadWithWritePrioritizeAsync(new ModbusRtu(), new ReadMapItem(1, true, DataFormat.ABCD, 1000, DataType.String, "100", null));

        writerMock.Verify(w => w.WriteAsync(It.IsAny<ModbusRtu>(), It.IsAny<WriteMapItem>()), Times.Never);
        readerMock.Verify(r => r.ReadAsync(It.IsAny<ModbusRtu>(), It.IsAny<ReadMapItem>()), Times.Once);
        result.Should().BeSameAs(expectedRead);
    }
}
