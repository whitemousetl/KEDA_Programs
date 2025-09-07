using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusQueue : IModbusQueue
{
    private readonly Channel<ModbusReadPoint[]> _readChannel = Channel.CreateUnbounded<ModbusReadPoint[]>();
    private readonly Channel<ModbusWritePoint[]> _writeChannel = Channel.CreateUnbounded<ModbusWritePoint[]>();

    public ChannelReader<ModbusReadPoint[]> ReadPointsReader => _readChannel.Reader;
    public ChannelReader<ModbusWritePoint[]> WritePointsReader => _writeChannel.Reader;

    public void EnqueueRead(ModbusReadPoint[] points) => _readChannel.Writer.TryWrite(points);
    public void EnqueueWrite(ModbusWritePoint[] points) => _writeChannel.Writer.TryWrite(points);
}
