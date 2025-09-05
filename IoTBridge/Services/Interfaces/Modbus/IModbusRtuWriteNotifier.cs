using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuWriteNotifier
{
    void EnqueueWrite(WriteMapItem wirteParams);

    bool TryQueue(out WriteMapItem? wirteParams);
}