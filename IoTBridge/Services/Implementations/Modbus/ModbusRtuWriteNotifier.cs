using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using System.Collections.Concurrent;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuWriteNotifier : IModbusRtuWriteNotifier
{
    //写操作队列
    private readonly ConcurrentQueue<WriteMapItem> _writeQueue = new();

    public void EnqueueWrite(WriteMapItem wirteParams)
    {
        _writeQueue.Enqueue(wirteParams);
    }

    public bool TryQueue(out WriteMapItem? wirteParams)
    {
        return _writeQueue.TryDequeue(out wirteParams);
    }
}