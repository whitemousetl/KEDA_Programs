using HslCommunication;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuProvider : IDisposable
{
    bool IsOpen { get; }

    void Open();
    void Close();
    void Reset();

    // 读方法
    Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length);
    Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length);
    Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length);
    Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length);
    Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length);
    Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length);
    Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length);
    Task<OperateResult<string>> ReadStringAsync(string address, ushort length);

    // 写方法
    Task<OperateResult> WriteBoolAsync(string address, bool value);
    Task<OperateResult> WriteInt16Async(string address, short value);
    Task<OperateResult> WriteUInt16Async(string address, ushort value);
    Task<OperateResult> WriteInt32Async(string address, int value);
    Task<OperateResult> WriteUInt32Async(string address, uint value);
    Task<OperateResult> WriteFloatAsync(string address, float value);
    Task<OperateResult> WriteDoubleAsync(string address, double value);
    Task<OperateResult> WriteStringAsync(string address, string value);
}
