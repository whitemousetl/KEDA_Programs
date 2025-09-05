using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Serilog;
using System.Net;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuCoordinator : IModbusRtuCoordinator
{
    private readonly IModbusRtuPointReader _reader;
    private readonly IModbusRtuPointWriter _writer;
    private readonly IModbusRtuWriteNotifier _notifier;

    public ModbusRtuCoordinator(IModbusRtuPointReader reader, IModbusRtuPointWriter writer, IModbusRtuWriteNotifier notifier)
    {
        _reader = reader;
        _writer = writer;
        _notifier = notifier;
    }

    public async Task<ReadValueBase> ReadWithWritePrioritizeAsync(ModbusRtu modbusRtu, ReadMapItem point)
    {
        // 写任务异常只记录日志，不影响后续流程
        while (_notifier.TryQueue(out WriteMapItem? writeItem) && writeItem != null)
        {
            try
            {
                await _writer.WriteAsync(modbusRtu, writeItem);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[调度] ModbusRtuCoordinator写入异常，地址:{writeItem.Address}，类型:{writeItem.DataType}，从站地址{writeItem.SlaveAddress}");
                // 写异常不影响后续流程
            }
        }

        // 读任务异常会影响返回结果
        try
        {
            var readResult = await _reader.ReadAsync(modbusRtu, point);
            return readResult;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[调度] ModbusRtuCoordinator读取异常，地址:{point.Address}，类型:{point.DataType}，从站地址{point.SlaveAddress}");
            return ExceptionHandle(point.DataType, ex.Message, point.Address);
        }
    }

    private static ReadValueBase ExceptionHandle(DataType dataType, string msg, string address)
    {
        return dataType switch
        {
            DataType.Bool => new ReadValue<bool[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.UShort => new ReadValue<ushort[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.Short => new ReadValue<short[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.UInt => new ReadValue<uint[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.Int => new ReadValue<int[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.Float => new ReadValue<float[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.Double => new ReadValue<double[]> { Address = address, IsSuccess = false, Message = msg, Value = default },
            DataType.String => new ReadValue<string> { Address = address, IsSuccess = false, Message = msg, Value = default },
            _ => new ReadValue<string> { Address = address, IsSuccess = false, Message = msg, Value = default },
        };
    }
}
