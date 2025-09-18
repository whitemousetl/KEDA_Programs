using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using System.Drawing;
using System.Threading.Channels;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuScheduler : IModbusRtuScheduler
{
    private readonly IModbusRtuProviderFactory _providerFactory;
    private IModbusRtuProvider _provider = null!;
    private readonly ChannelReader<ModbusReadPoint[]> _readPointsReader;
    private readonly ChannelReader<ModbusWritePoint[]> _writePointsReader;

    public ModbusRtuScheduler(IModbusQueue queue, IModbusRtuProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
        _readPointsReader = queue.ReadPointsReader;
        _writePointsReader = queue.WritePointsReader;
    }

    public void InitProvider(SerialPortConfig config)
    {
        _provider = _providerFactory.Create(config);
    }

    public async Task ScheduleAsync()
    {
        // 并行等待，哪个通道先有数据就先处理
        var writeReadyTask = _writePointsReader.WaitToReadAsync().AsTask();
        var readReadyTask = _readPointsReader.WaitToReadAsync().AsTask();

        var completedTask = await Task.WhenAny(writeReadyTask, readReadyTask);

        if (completedTask == writeReadyTask && await writeReadyTask && _writePointsReader.TryRead(out var writeBatch))
        {
            // 写优先，drain所有写
            do
            {
                foreach (var point in writeBatch)
                    await HandleWriteAsync(point);

            }
            while (_writePointsReader.TryRead(out writeBatch));
        }

        if (completedTask == readReadyTask && await readReadyTask && _readPointsReader.TryRead(out var readBatch))
        {
            foreach (var point in readBatch)
            {
                // 每个读点之间快速探测是否来了写 → 允许写插队
                if (_writePointsReader.TryRead(out var writeNow))
                {
                    do
                    {
                        foreach (var wp in writeNow)
                            await HandleWriteAsync(wp);
                    }
                    while (_writePointsReader.TryRead(out writeNow));
                    // 写优先，回到循环重新select
                    continue;
                }
                await HandleReadAsync(point);
            }
        }
    }

    private async Task HandleWriteAsync(ModbusWritePoint point)
    {
        var address = point.Address;
        var value = point.Value;
        switch (point.DataType)
        {
            case DataType.Bool:
                if(value is bool b)
                    await _provider.WriteBoolAsync(address, b);
                break;
            case DataType.UShort:
                if(value is ushort u16)
                    await _provider.WriteUInt16Async(address, u16);
                break;
            case DataType.Short:
                if(value is short s16)
                    await _provider.WriteInt16Async(address, s16);
                break;
            case DataType.UInt:
                if(value is uint u32)
                    await _provider.WriteUInt32Async(address, u32);
                break;
            case DataType.Int:
                if(value is int i32)
                    await _provider.WriteInt32Async(address, i32);
                break;
            case DataType.Float:
                if(value is float f)
                    await _provider.WriteFloatAsync(address, f);
                break;
            case DataType.Double:
                if(value is double d)
                    await _provider.WriteDoubleAsync(address, d);
                break;
            default:
                break;
        }
    }

    private async Task HandleReadAsync(ModbusReadPoint point)
    {
        var address = point.Address;
        ushort length;
        if(point.Length.HasValue) length = point.Length.Value;
        else length = 1;

        switch (point.DataType)
        {
            case DataType.Bool:
                await _provider.ReadBoolAsync(address, length);
                break;
            case DataType.UShort:
                await _provider.ReadUInt16Async(address, length);
                break;
            case DataType.Short:
                await _provider.ReadInt16Async(address, length);
                break;
            case DataType.UInt:
                await _provider.ReadUInt32Async(address, length);
                break;
            case DataType.Int:
                await _provider.ReadInt32Async(address, length);
                break;
            case DataType.Float:
                await _provider.ReadFloatAsync(address, length);
                break;
            case DataType.Double:
                await _provider.ReadDoubleAsync(address, length);
                break;
            case DataType.String:
                await _provider.ReadStringAsync(address, length);
                break;
            default: 
                break;
        }
    }
}
