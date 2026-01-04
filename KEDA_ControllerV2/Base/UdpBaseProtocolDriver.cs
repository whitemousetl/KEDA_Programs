using HslCommunication.Core.Device;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_Controller.Base;

public abstract class UdpBaseProtocolDriver<T> : BaseProtocolDriver<T> where T : DeviceUdpNet
{
    protected override Task OnConnectionInitializedAsync(CancellationToken token)
    {
        // UDP通常不需要显式连接
        return Task.CompletedTask;
    }

    protected override ProtocolDto? ExtractProtocolFromWriteTask(WriteTask writeTask)
    {
        if (writeTask.Protocol is not LanProtocolDto lanProtocol)
            return null;

        return new LanProtocolDto
        {
            Id = lanProtocol.Id,
            ProtocolType = lanProtocol.ProtocolType,
            IpAddress = lanProtocol.IpAddress,
            Gateway = lanProtocol.Gateway,
            ProtocolPort = lanProtocol.ProtocolPort,
            Remark = lanProtocol.Remark,
            CollectCycle = lanProtocol.CollectCycle,
            ReceiveTimeOut = lanProtocol.ReceiveTimeOut,
            ConnectTimeOut = lanProtocol.ConnectTimeOut,
        };
    }

    protected override IEnumerable<ParameterDto>? GetPointsFromProtocol(ProtocolDto protocol)
    {
        return (protocol as LanProtocolDto)?.Equipments[0]?.Parameters;
    }

    protected override void DisposeConnection()
    {
        _conn?.Dispose();
    }

    #region HSL读写实现

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadBoolAsync(string address)
    {
        var r = await _conn!.ReadBoolAsync(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadInt16Async(string address)
    {
        var r = await _conn!.ReadInt16Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadUInt16Async(string address)
    {
        var r = await _conn!.ReadUInt16Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadInt32Async(string address)
    {
        var r = await _conn!.ReadInt32Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadUInt32Async(string address)
    {
        var r = await _conn!.ReadUInt32Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadInt64Async(string address)
    {
        var r = await _conn!.ReadInt64Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadUInt64Async(string address)
    {
        var r = await _conn!.ReadUInt64Async(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadFloatAsync(string address)
    {
        var r = await _conn!.ReadFloatAsync(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadDoubleAsync(string address)
    {
        var r = await _conn!.ReadDoubleAsync(address);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<(bool IsSuccess, object? Content, string Message)> ReadStringAsync(string address, ushort length)
    {
        var r = await _conn!.ReadStringAsync(address, length);
        return (r.IsSuccess, r.Content, r.Message);
    }

    protected override async Task<bool> WriteBoolAsync(string address, bool value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteInt16Async(string address, short value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteUInt16Async(string address, ushort value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteInt32Async(string address, int value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteUInt32Async(string address, uint value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteInt64Async(string address, long value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteUInt64Async(string address, ulong value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteFloatAsync(string address, float value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteDoubleAsync(string address, double value)
    {
        return (await _conn!.WriteAsync(address, value)).IsSuccess;
    }

    protected override async Task<bool> WriteStringAsync(string address, string value, ushort length)
    {
        var res = await _conn!.WriteAsync(address, value, length);
        return res.IsSuccess;
    }

    #endregion HSL读写实现
}