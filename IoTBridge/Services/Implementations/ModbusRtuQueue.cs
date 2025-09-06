using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using System.Collections.Concurrent;

namespace IoTBridge.Services.Implementations;

public class ModbusRtuQueue : IModbusRtuQueue
{
    private readonly ConcurrentQueue<List<ModbusReadPoint>> _readQueue = new();
    private readonly ConcurrentQueue<List<ModbusWritePoint>> _writeQueue = new();

    public int WriteCount => _writeQueue.Count;

    public int ReadCount => _readQueue.Count;

    public void EnqueueRead(List<ModbusReadPoint> points)
    {
        _readQueue.Enqueue(points);
    }

    public void EnqueueWrite(List<ModbusWritePoint> points)
    {
        _writeQueue.Enqueue(points);
    }

    public bool TryDequeueRead(out List<ModbusReadPoint>? points)
    {
        return _readQueue.TryDequeue(out points);
    }

    public bool TryDequeueRead(out List<ModbusWritePoint>? points)
    {
        throw new NotImplementedException();
    }

    public bool TryDequeueWrite(out List<ModbusWritePoint>? points)
    {
        return _writeQueue.TryDequeue(out points);
    }

    public bool TryDequeueWrite(out List<ModbusReadPoint>? points)
    {
        throw new NotImplementedException();
    }
}
