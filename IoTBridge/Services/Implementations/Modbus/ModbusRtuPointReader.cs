using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Serilog;
using System.Collections;
using System.Net;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuPointReader : IModbusRtuPointReader
{
    public async Task<ReadValueBase> ReadAsync(ModbusRtu modbusRtu, ReadMapItem point)
    {
        try
        {
            ushort length;
            if(point.Length.HasValue) length = point.Length.Value;
            else length = 1;

            var result = point.DataType switch
            {
                DataType.Bool => await ExecuteAsync(modbusRtu.ReadBoolAsync, point.Address, length),
                DataType.UShort => await ExecuteAsync(modbusRtu.ReadUInt16Async, point.Address, length),
                DataType.Short => await ExecuteAsync(modbusRtu.ReadInt16Async, point.Address, length),
                DataType.UInt => await ExecuteAsync(modbusRtu.ReadUInt32Async, point.Address, length),
                DataType.Int => await ExecuteAsync(modbusRtu.ReadInt32Async, point.Address, length),
                DataType.Float => await ExecuteAsync(modbusRtu.ReadFloatAsync, point.Address, length),
                DataType.Double => await ExecuteAsync(modbusRtu.ReadDoubleAsync, point.Address, length),
                DataType.String => point.Length.HasValue
                ? await ExecuteAsync(modbusRtu.ReadStringAsync, point.Address, point.Length.Value)
                : new ReadValue<string>
                {
                    IsSuccess = false,
                    Message = "String类型必须指定length",
                    Address = point.Address
                },
                _ => new ReadValue<string>
                {
                    IsSuccess = false,
                    Message = "不支持的数据类类型，请检查",
                    Address = point.Address,
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Modbus读取异常，地址:{Address}，类型:{DataType}", point.Address, point.DataType);
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

    private static async Task<ReadValueBase> ExecuteAsync<T>(Func<string, ushort, Task<OperateResult<T>>> func, string address, ushort length)
    {
        var readResult = await func.Invoke(address, length);
        var result = HandleReadResultAsync(readResult, address);
        return result;
    }

    private static ReadValue<T> HandleReadResultAsync<T>(OperateResult<T> readResult, string address)
    {
        var result = new ReadValue<T>();
        result.IsSuccess = readResult.IsSuccess;
        if (result.IsSuccess)
            result.Value = readResult.Content;
        else
            result.Message = readResult.Message;

        result.Address = address;

        return result;
    }
}
