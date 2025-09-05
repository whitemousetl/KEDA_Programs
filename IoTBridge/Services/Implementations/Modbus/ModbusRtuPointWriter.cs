using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Serilog;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuPointWriter : IModbusRtuPointWriter
{
    public async Task<(bool isSuccess, string? message)> WriteAsync(ModbusRtu modbusRtu, WriteMapItem writePoint)
    {
        try
        {
            modbusRtu.ReceiveTimeOut = writePoint.ReceiveTimeOut;
            modbusRtu.AddressStartWithZero = writePoint.ZeroBasedAddressing;
            modbusRtu.DataFormat = writePoint.DataFormat;
            modbusRtu.Station = writePoint.SlaveAddress;

            switch (writePoint.DataType)
            {
                case DataType.Bool:
                    if (writePoint.Value is bool boolVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, boolVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望bool，实际为{writePoint.Value?.GetType().Name}");
                case DataType.Short:
                    if (writePoint.Value is short shortVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, shortVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望short，实际为{writePoint.Value?.GetType().Name}");
                case DataType.UShort:
                    if (writePoint.Value is ushort ushortVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, ushortVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望ushort，实际为{writePoint.Value?.GetType().Name}");
                case DataType.Int:
                    if (writePoint.Value is int intVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, intVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望int，实际为{writePoint.Value?.GetType().Name}");
                case DataType.UInt:
                    if (writePoint.Value is uint uintVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, uintVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望uint，实际为{writePoint.Value?.GetType().Name}");
                case DataType.Float:
                    if (writePoint.Value is float floatVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, floatVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望float，实际为{writePoint.Value?.GetType().Name}");
                case DataType.Double:
                    if (writePoint.Value is double doubleVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, doubleVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望double，实际为{writePoint.Value?.GetType().Name}");
                case DataType.String:
                    if (writePoint.Value is string strVal)
                    {
                        var res = await modbusRtu.WriteAsync(writePoint.Address, strVal);
                        if (!res.IsSuccess)
                        {
                            Log.Error($"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                            return (false, $"[写入] ModbusRtu写入失败，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}，信息：{res.Message}");
                        }
                        else return (true, null);
                    }
                    else
                        throw new InvalidCastException($"Value类型错误，期望string，实际为{writePoint.Value?.GetType().Name}");
                default:
                    throw new NotSupportedException($"不支持的数据类型: {writePoint.DataType}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[写入] ModbusRtu写入异常，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}");
            return (false, ex + $"[写入] ModbusRtu写入异常，地址:{writePoint.Address}，类型:{writePoint.DataType}，从站地址{writePoint.SlaveAddress}");
        }
       
    }
}
