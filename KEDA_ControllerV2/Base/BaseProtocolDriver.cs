using HslCommunication.Core;
using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.DLT;
using HslCommunication.ModBus;
using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;

namespace KEDA_Controller.Base;

/// <summary>
/// HSL通信库协议驱动的抽象基类，提供通用的读写逻辑
/// </summary>
/// <typeparam name="T">设备通信类型</typeparam>
public abstract class BaseProtocolDriver<T> : IProtocolDriver where T : class
{
    protected T? _conn; // 协议连接对象
    protected readonly string _protocolName; // 协议名称

    // 读取函数映射表
    private static readonly ConcurrentDictionary<DataType, Func<BaseProtocolDriver<T>, ParameterDto, CancellationToken, Task<(bool IsSuccess, object? Content, string Message)>>> _readFuncs =
        new()
        {
            [DataType.Bool] = async (driver, point, token) => await driver.ReadBoolAsync(point.Address),
            [DataType.UShort] = async (driver, point, token) => await driver.ReadUInt16Async(point.Address),
            [DataType.Short] = async (driver, point, token) => await driver.ReadInt16Async(point.Address),
            [DataType.UInt] = async (driver, point, token) => await driver.ReadUInt32Async(point.Address),
            [DataType.Int] = async (driver, point, token) => await driver.ReadInt32Async(point.Address),
            [DataType.Long] = async (driver, point, token) => await driver.ReadInt64Async(point.Address),
            [DataType.ULong] = async (driver, point, token) => await driver.ReadUInt64Async(point.Address),
            [DataType.Float] = async (driver, point, token) => await driver.ReadFloatAsync(point.Address),
            [DataType.Double] = async (driver, point, token) => await driver.ReadDoubleAsync(point.Address),
            [DataType.String] = async (driver, point, token) => await driver.ReadStringAsync(point.Address, point.Length),
        };

    // 写入函数映射表
    private static readonly ConcurrentDictionary<DataType, Func<BaseProtocolDriver<T>, ParameterDto, CancellationToken, Task<bool>>> _writeFuncs =
        new()
        {
            [DataType.Bool] = async (driver, point, token) =>
                bool.TryParse(point.Value, out var value) && await driver.WriteBoolAsync(point.Address, value),
            [DataType.Short] = async (driver, point, token) =>
                short.TryParse(point.Value, out var value) && await driver.WriteInt16Async(point.Address, value),
            [DataType.UShort] = async (driver, point, token) =>
                ushort.TryParse(point.Value, out var value) && await driver.WriteUInt16Async(point.Address, value),
            [DataType.Int] = async (driver, point, token) =>
                int.TryParse(point.Value, out var value) && await driver.WriteInt32Async(point.Address, value),
            [DataType.UInt] = async (driver, point, token) =>
                uint.TryParse(point.Value, out var value) && await driver.WriteUInt32Async(point.Address, value),
            [DataType.Long] = async (driver, point, token) =>
                long.TryParse(point.Value, out var value) && await driver.WriteInt64Async(point.Address, value),
            [DataType.ULong] = async (driver, point, token) =>
                ulong.TryParse(point.Value, out var value) && await driver.WriteUInt64Async(point.Address, value),
            [DataType.Float] = async (driver, point, token) =>
                float.TryParse(point.Value, out var value) && await driver.WriteFloatAsync(point.Address, value),
            [DataType.Double] = async (driver, point, token) =>
                double.TryParse(point.Value, out var value) && await driver.WriteDoubleAsync(point.Address, value),
            [DataType.String] = async (driver, point, token) =>
                await driver.WriteStringAsync(point.Address, point.Value, point.Length)
        };

    protected BaseProtocolDriver()
    {
        _protocolName = GetProtocolName();
    }

    #region 读方法

    public virtual async Task<PointResult?> ReadAsync(ProtocolDto protocol, string equipmentId, ParameterDto point, CancellationToken token)
    {
        try
        {
            if (_conn == null)
            {
                _conn = CreateConnection(protocol, token);
                await OnConnectionInitializedAsync(token);
            }

            // 应用点位配置
            ApplyPointConfiguration(point);

            var result = await ReadPointAsync(point, token);
            return result;
        }
        catch (Exception ex) when (
            ex is ProtocolWhenConnFailedException ||
            ex is ProtocolIsNullWhenReadException ||
            ex is NotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);
        }
    }

    protected virtual async Task<PointResult> ReadPointAsync(ParameterDto point, CancellationToken token)
    {
        var result = new PointResult
        {
            Address = point.Address,
            Label = point.Label,
            DataType = point.DataType
        };

        if (_conn == null)
            throw new ProtocolIsNullWhenReadException($"{_protocolName}协议为空，请检查",
                new Exception($"{_protocolName}协议为空，请检查"));

        if (_readFuncs.TryGetValue(point.DataType, out var func))
        {
            var (isSuccess, content, message) = await func(this, point, token);
            result.ReadIsSuccess = isSuccess;
            result.Value = isSuccess ? content : null;
            result.ErrorMsg = isSuccess ? string.Empty : message;
        }
        else
        {
            throw new NotSupportedException($"{_protocolName}协议不支持的数据类型: {point.DataType}");
        }

        return result;
    }

    #endregion 读方法

    #region 写方法

    public virtual async Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token)
    {
        var protocol = ExtractProtocolFromWriteTask(writeTask) ?? throw new InvalidOperationException($"写任务协议类型不匹配，无法进行 {_protocolName} 写操作。");
        try
        {
            if (_conn == null)
            {
                _conn = CreateConnection(protocol, token);
                await OnConnectionInitializedAsync(token);
            }

            var points = GetPointsFromProtocol(protocol);
            if (points == null || !points.Any())
                return false;

            foreach (var wp in points)
            {
                // 应用点位配置
                ApplyPointConfiguration(wp);

                return await WritePointAsync(wp, token);
            }
        }
        catch (Exception ex) when (
            ex is ProtocolWhenConnFailedException ||
            ex is ProtocolIsNullWhenWriteException ||
            ex is NotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);
        }

        return false;
    }

    private async Task<bool> WritePointAsync(ParameterDto point, CancellationToken token)
    {
        if (_conn == null)
            throw new ProtocolIsNullWhenWriteException($"{_protocolName}协议为空，请检查",
                new Exception($"{_protocolName}协议为空，请检查"));

        if (_writeFuncs.TryGetValue(point.DataType, out var func))
            return await func(this, point, token);

        throw new NotSupportedException($"{_protocolName}协议不支持的数据类型: {point.DataType}");
    }

    #endregion 写方法

    #region 点位配置应用

    /// <summary>
    /// 应用点位配置（站号、字节序、仪表类型等）
    /// </summary>
    protected virtual void ApplyPointConfiguration(ParameterDto point)
    {
        // 设置站号
        SetStationNoIfNeed(point.StationNo);

        // 设置字节序
        SetDataFormatNoIfNeed(point.DataFormat);

        // 设置地址起始位（如果协议支持）
        SetAddressStartWithZeroIfNeed(point.AddressStartWithZero);

        // 设置仪表类型（CJT188专用）
        SetInstrumentTypeIfNeed((byte)point.InstrumentType);
    }

    /// <summary>
    /// 设置站号
    /// </summary>
    protected virtual void SetStationNoIfNeed(string stationNo)
    {
        var station = string.IsNullOrEmpty(stationNo) ? "1" : stationNo;

        switch (_conn)
        {
            case ModbusTcpNet modbus:
                modbus.Station = byte.TryParse(station, out var modbusStation) ? modbusStation : (byte)1;
                break;

            case ModbusRtuOverTcp modbusRtuOverTcp:
                modbusRtuOverTcp.Station = byte.TryParse(station, out var rtuStation) ? rtuStation : (byte)1;
                break;

            case ModbusRtu modbusRtu:
                modbusRtu.Station = byte.TryParse(station, out var rtuStation2) ? rtuStation2 : (byte)1;
                break;

            case DLT645OverTcp dlt645OverTcp:
                dlt645OverTcp.Station = station;
                break;

            case DLT645 dlt645:
                dlt645.Station = station;
                break;

            case CJT188OverTcp cjt188OverTcp:
                cjt188OverTcp.Station = station;
                break;

            case CJT188 cjt188:
                cjt188.Station = station;
                break;
        }
    }

    /// <summary>
    /// 设置字节序
    /// </summary>
    protected virtual void SetDataFormatNoIfNeed(DataFormat format)
    {
        switch (_conn)
        {
            case ModbusTcpNet modbus:
                modbus.DataFormat = format;
                break;

            case ModbusRtuOverTcp modbusRtuOverTcp:
                modbusRtuOverTcp.DataFormat = format;
                break;

            case ModbusRtu modbusRtu:
                modbusRtu.DataFormat = format;
                break;
        }
    }

    /// <summary>
    /// 设置地址起始位（部分协议支持）
    /// </summary>
    protected virtual void SetAddressStartWithZeroIfNeed(bool addressStartWithZero)
    {
        switch (_conn)
        {
            case ModbusTcpNet modbus:
                modbus.AddressStartWithZero = addressStartWithZero;
                break;

            case ModbusRtuOverTcp modbusRtuOverTcp:
                modbusRtuOverTcp.AddressStartWithZero = addressStartWithZero;
                break;

            case ModbusRtu modbusRtu:
                modbusRtu.AddressStartWithZero = addressStartWithZero;
                break;
        }
    }

    /// <summary>
    /// 设置仪表类型（CJT188专用）
    /// </summary>
    protected virtual void SetInstrumentTypeIfNeed(byte instrumentType)
    {
        if (instrumentType == 0) return; // 0表示未设置

        switch (_conn)
        {
            case CJT188OverTcp cjt188OverTcp:
                cjt188OverTcp.InstrumentType = instrumentType;
                break;

            case CJT188 cjt188:
                cjt188.InstrumentType = instrumentType;
                break;
        }
    }

    #endregion 点位配置应用

    #region 抽象方法 - 子类必须实现

    /// <summary>
    /// 创建连接对象
    /// </summary>
    protected abstract T CreateConnection(ProtocolDto protocol, CancellationToken token);

    /// <summary>
    /// 非逐个采集点读取的协议需要实现的方法
    /// </summary>
    public virtual Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接初始化
    /// </summary>
    protected abstract Task OnConnectionInitializedAsync(CancellationToken token);

    /// <summary>
    /// 从写任务中提取协议对象
    /// </summary>
    protected abstract ProtocolDto? ExtractProtocolFromWriteTask(WriteTask writeTask);

    /// <summary>
    /// 从协议中获取点位列表
    /// </summary>
    protected abstract IEnumerable<ParameterDto>? GetPointsFromProtocol(ProtocolDto protocol);

    #endregion 抽象方法 - 子类必须实现

    #region 抽象读写方法 - 子类必须实现具体的HSL通信调用

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadBoolAsync(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadInt16Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadUInt16Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadInt32Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadUInt32Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadInt64Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadUInt64Async(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadFloatAsync(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadDoubleAsync(string address);

    protected abstract Task<(bool IsSuccess, object? Content, string Message)> ReadStringAsync(string address, ushort length);

    protected abstract Task<bool> WriteBoolAsync(string address, bool value);

    protected abstract Task<bool> WriteInt16Async(string address, short value);

    protected abstract Task<bool> WriteUInt16Async(string address, ushort value);

    protected abstract Task<bool> WriteInt32Async(string address, int value);

    protected abstract Task<bool> WriteUInt32Async(string address, uint value);

    protected abstract Task<bool> WriteInt64Async(string address, long value);

    protected abstract Task<bool> WriteUInt64Async(string address, ulong value);

    protected abstract Task<bool> WriteFloatAsync(string address, float value);

    protected abstract Task<bool> WriteDoubleAsync(string address, double value);

    protected abstract Task<bool> WriteStringAsync(string address, string value, ushort length);

    #endregion 抽象读写方法 - 子类必须实现具体的HSL通信调用

    #region IDisposable

    public virtual void Dispose()
    {
        DisposeConnection();
        _conn = null;
        GC.SuppressFinalize(this);
    }

    protected abstract void DisposeConnection();

    #endregion IDisposable

    #region 工具方法

    public string GetProtocolName()
    {
        var typeName = typeof(T).Name;
        if (typeName.EndsWith("ProtocolDriver"))
            return typeName[..^"ProtocolDriver".Length];
        if (typeName.EndsWith("Driver"))
            return typeName[..^"Driver".Length];
        return typeName;
    }
    #endregion 工具方法
}