using HslCommunication.Core;
using HslCommunication.Core.Device;
using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.DLT;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Melsec;
using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Base;
public abstract class HslSerialBaseProtocolDriver<T> : IProtocolDriver where T : DeviceSerialPort
{
    protected T? _conn;//协议连接对象
    private readonly string _protocolName;//协议名称
    private static readonly Dictionary<DataType, Func<HslSerialBaseProtocolDriver<T>, PointEntity, CancellationToken, Task<(bool IsSuccess, object? Content, string Message)>>> _readFuncs =
      new()
      {
          [DataType.Bool] = async (driver, point, token) => { var r = await driver._conn!.ReadBoolAsync(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.UShort] = async (driver, point, token) => { var r = await driver._conn!.ReadUInt16Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.Short] = async (driver, point, token) => { var r = await driver._conn!.ReadInt16Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.UInt] = async (driver, point, token) => { var r = await driver._conn!.ReadUInt32Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.Int] = async (driver, point, token) => { var r = await driver._conn!.ReadInt32Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.Long] = async (driver, point, token) => { var r = await driver._conn!.ReadInt64Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.ULong] = async (driver, point, token) => { var r = await driver._conn!.ReadUInt64Async(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.Float] = async (driver, point, token) => { var r = await driver._conn!.ReadFloatAsync(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.Double] = async (driver, point, token) => { var r = await driver._conn!.ReadDoubleAsync(point.Address); return (r.IsSuccess, r.Content, r.Message); },
          [DataType.String] = async (driver, point, token) => { var r = await driver._conn!.ReadStringAsync(point.Address, point.Length); return (r.IsSuccess, r.Content, r.Message); },
      };
    private static readonly Dictionary<DataType, Func<HslSerialBaseProtocolDriver<T>, WritePoint, CancellationToken, Task<bool>>> _writeFuncs =
    new()
    {
        [DataType.Bool] = async (driver, point, token) =>
        { return bool.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.Short] = async (driver, point, token) =>
        { return short.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.UShort] = async (driver, point, token) =>
        { return ushort.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.Int] = async (driver, point, token) =>
        { return int.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.UInt] = async (driver, point, token) =>
        { return uint.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.Long] = async (driver, point, token) =>
        { return long.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.ULong] = async (driver, point, token) =>
        { return ulong.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.Float] = async (driver, point, token) =>
        { return float.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.Double] = async (driver, point, token) =>
        { return double.TryParse(point.Value, out var value) && (await driver._conn!.WriteAsync(point.Address, value)).IsSuccess; },
        [DataType.String] = async (driver, point, token) =>
        {
            var res = await driver._conn!.WriteAsync(point.Address, point.Value, point.Length);
            return res.IsSuccess;
        }
    };

    protected HslSerialBaseProtocolDriver() => _protocolName = GetProtocolName();

    #region 读方法
    public virtual async Task<ProtocolResult?> ReadAsync(WorkstationEntity protocol, string devId, PointEntity point, CancellationToken token)//读取正常则正常返回，异常则抛出，让worker处理
    {
        try
        {
            if (_conn == null)
            {
                _conn = CreateConnection(protocol, token);//创建或获取协议类
                await OnConnectionInitializedAsync(token);//初始化协议对象
            }

            SetStationNoIfNeed(point.StationNo);//根据协议类型来决定是否需要为协议连接对象的站号赋值
            SetDataFormatNoIfNeed(point.Format);//设置字节序

            var result = await ReadPointAsync(point, token);//读取
            return result;
        }
        catch (Exception ex) when (
        ex is ProtocolWhenConnFailedException ||//连接plc失败异常
        ex is ProtocolIsNullWhenReadException ||//当读取时协议为空异常
        ex is NotSupportedException) //不支持的类型异常
        {
            // 直接抛出已知的自定义异常
            throw;
        }
        catch (Exception ex)
        {
            // 统一处理未知异常
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);//抛出默认异常
        }
    }

    protected virtual async Task<ProtocolResult> ReadPointAsync(PointEntity point, CancellationToken token)
    {
        var result = new ProtocolResult();
        result.Address = point.Address;
        result.Label = point.Label;
        result.DataType = point.DataType;

        if (_conn == null)
            throw new ProtocolIsNullWhenReadException($"{_protocolName}协议为空，请检查", new Exception($"{_protocolName}协议为空，请检查"));

        if (_readFuncs.TryGetValue(point.DataType, out var func))
        {
            var (isSuccess, content, message) = await func(this, point, token);
            result.ReadIsSuccess = isSuccess;//读取是否成功
            result.Value = isSuccess ? content : null;//正常读取，成功则给result的Value赋值，失败则赋值为null
            result.ErrorMsg = isSuccess ? string.Empty : message;//读取失败时，失败信息
        }
        else
            throw new NotSupportedException($"{_protocolName}协议不支持的数据类型: {point.DataType}");

        return result;
    }
    #endregion

    #region 写方法
    public virtual async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
    {
        //初始化_conn
        var protocol = new WorkstationEntity
        {
            ProtocolID = writeTask.ProtocolID,
            Interface = writeTask.Interface,
            ProtocolType = writeTask.ProtocolType,
            IPAddress = writeTask.IPAddress,
            Gateway = writeTask.Gateway,
            ProtocolPort = writeTask.ProtocolPort,
            PortName = writeTask.PortName,
            BaudRate = writeTask.BaudRate,
            DataBits = writeTask.DataBits,
            Parity = writeTask.Parity,
            StopBits = writeTask.StopBits,
            Remark = writeTask.Remark,
            CollectCycle = writeTask.CollectCycle,
            ReceiveTimeOut = writeTask.ReceiveTimeOut,
            ConnectTimeOut = writeTask.ConnectTimeOut,
        };

        try
        {
            if (_conn == null)
            {
                _conn = CreateConnection(protocol, token);
                await OnConnectionInitializedAsync(token);
            }

            if (writeTask.WriteDevice == null) return false;

            foreach(var wp in writeTask.WriteDevice.WritePoints)
            {
                SetStationNoIfNeed(wp.StationNo);//根据协议类型来决定是否需要为协议连接对象的站号赋值
                SetDataFormatNoIfNeed(wp.Format);//根据协议类型来决定是否需要为协议连接对象的站号赋值

                return await WritePointAsync(wp, token);
            }
        }
        catch (Exception ex) when (
        ex is ProtocolWhenConnFailedException ||//连接plc失败异常
        ex is ProtocolIsNullWhenWriteException ||//当写入时协议为空异常
        ex is NotSupportedException) //不支持的类型异常
        {
            // 直接抛出已知的自定义异常
            throw;
        }
        catch (Exception ex)
        {
            // 统一处理未知异常
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);//抛出默认异常
        }

        return false;
    }

    private async Task<bool> WritePointAsync(WritePoint point, CancellationToken token)
    {
        if (_conn == null)
            throw new ProtocolIsNullWhenWriteException($"{_protocolName}协议为空，请检查", new Exception($"{_protocolName}协议为空，请检查"));

        if (_writeFuncs.TryGetValue(point.DataType, out var func))
            return await func(this, point, token);
        throw new NotSupportedException($"{_protocolName}协议不支持的数据类型: {point.DataType}");
    }
    #endregion

    #region 读写公共方法，设置站号和字节序
    protected virtual void SetStationNoIfNeed(string stationNo)
    {
        var station = string.IsNullOrEmpty(stationNo) ? "1" : stationNo;

        switch (_conn)
        {
            case CJT188 cjt188:
                cjt188.Station = station;
                break;
            case DLT645 dlt645:
                dlt645.Station = station;
                break;
            case ModbusRtu modbusRtu:
                modbusRtu.Station = byte.TryParse(station, out var rtuStation) ? rtuStation : (byte)1;
                break;
        }
    }

    protected virtual void SetDataFormatNoIfNeed(DataFormat format)
    {
        switch (_conn)
        {
            case ModbusRtu modbusRtu:
                modbusRtu.DataFormat = format;
                break;
        }
    }
    #endregion

    #region 读写公共方法，创建协议对象和连接协议
    //子类实现：创建连接对象
    protected abstract T CreateConnection(WorkstationEntity protocol, CancellationToken token);//一般不抛出异常

    //子类可选实现：连接初始化后设置参数
    protected virtual async Task OnConnectionInitializedAsync(CancellationToken token)
    {
        if (_conn != null)
        {
            var connRes = _conn.Open();//连接plc
            if (!connRes.IsSuccess)
                throw new ProtocolWhenConnFailedException($"{_protocolName}协议连接plc时异常: {connRes.Message}", new Exception(connRes.Message));//抛出连接plc异常
        }

        await Task.CompletedTask;
    }
    #endregion

    public virtual void Dispose()
    {
        _conn?.Dispose();
        _conn = null;
        GC.SuppressFinalize(this);
    }

    //获取协议名称
    public string GetProtocolName()
    {
        var typeName = typeof(T).Name;
        if (typeName.EndsWith("ProtocolDriver"))
            return typeName[..^"ProtocolDriver".Length];
        if (typeName.EndsWith("Driver"))
            return typeName[..^"Driver".Length];
        return typeName;
    }
}
