using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using System.Drawing;
using System.Threading.Channels;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuScheduler : IModbusRtuScheduler
{
    private readonly IModbusReader _reader;
    private readonly IModbusWriter _writer;
    private readonly IModbusRtuProviderFactory _providerFactory;
    private IModbusRtuProvider _provider = null!;
    private readonly ChannelReader<ModbusReadPoint[]> _readPointsReader;
    private readonly ChannelReader<ModbusWritePoint[]> _writePointsReader;

    public ModbusRtuScheduler(IModbusQueue queue, IModbusReader reader, IModbusWriter writer, IModbusRtuProviderFactory providerFactory)
    {
        _reader = reader;
        _writer = writer;
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
        switch (point.DataType)
        {
            case DataType.Bool:
                await _writer.WritePointAsync<bool>(_provider.WriteBoolAsync, point);
                break;
            case DataType.UShort:
                await _writer.WritePointAsync<ushort>(_provider.WriteUInt16Async, point);
                break;
            case DataType.Short:
                await _writer.WritePointAsync<short>(_provider.WriteInt16Async, point);
                break;
            case DataType.UInt:
                await _writer.WritePointAsync<uint>(_provider.WriteUInt32Async, point);
                break;
            case DataType.Int:
                await _writer.WritePointAsync<int>(_provider.WriteInt32Async, point);
                break;
            case DataType.Float:
                await _writer.WritePointAsync<float>(_provider.WriteFloatAsync, point);
                break;
            case DataType.Double:
                await _writer.WritePointAsync<double>(_provider.WriteDoubleAsync, point);
                break;
            default:
                break;
        }
    }

    private async Task HandleReadAsync(ModbusReadPoint point)
    {
        switch (point.DataType)
        {
            case DataType.Bool:
                await _reader.ReadPointAsync(_provider.ReadBoolAsync, point);
                break;
            case DataType.UShort:
                await _reader.ReadPointAsync(_provider.ReadUInt16Async, point);
                break;
            case DataType.Short:
                await _reader.ReadPointAsync(_provider.ReadInt16Async, point);
                break;
            case DataType.UInt:
                await _reader.ReadPointAsync(_provider.ReadUInt32Async, point);
                break;
            case DataType.Int:
                await _reader.ReadPointAsync(_provider.ReadInt32Async, point);
                break;
            case DataType.Float:
                await _reader.ReadPointAsync(_provider.ReadFloatAsync, point);
                break;
            case DataType.Double:
                await _reader.ReadPointAsync(_provider.ReadDoubleAsync, point);
                break;
            case DataType.String:
                await _reader.ReadPointAsync(_provider.ReadStringAsync, point);
                break;
            default: 
                break;
        }
    }
}
