using CollectorService.CustomException;
using CollectorService.Models;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CollectorService.Protocols;
public class ApiDriver : IProtocolDriver
{
    private string _protocolName = "Api";

    private readonly HttpClient _conn = new();

    public Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task<DeviceResult> ReadAsync(Protocol protocol, Device device, CancellationToken token)
    {
        var deviceResult = new DeviceResult
        {
            PointResults = [],
        };

        try
        {
            var response = await _conn.GetAsync(protocol.IPAddress, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            foreach(var point in device.Points)
            {
                var address = point.Address;
                if (string.IsNullOrWhiteSpace(address)) continue;
                if (address.Contains('[') && address.Contains(']'))
                {
                    var dotIndex = address.IndexOf('.');
                    if (dotIndex < 0)
                    {
                        var (arrName, idx) = ParseArrayPath(address);
                        if (arrName == null || idx < 0)
                            throw new InvalidOperationException("地址格式错误，必须为数组类型如 TilelifterList[2]");

                        if (!doc.RootElement.TryGetProperty(arrName, out var arrElem) || arrElem.ValueKind != JsonValueKind.Array)
                            throw new InvalidOperationException($"JSON中未找到数组 {arrName}");

                        if (arrElem.GetArrayLength() <= idx)
                            throw new IndexOutOfRangeException($"数组 {arrName} 长度不足，索引 {idx} 越界");

                        var objElem = arrElem[idx];
                        foreach (var prop in objElem.EnumerateObject())
                        {
                            var (result, type) = GetValueAndType(prop.Value, point.DataType);
                            deviceResult.PointResults.Add(new PointResult
                            {
                                Label = prop.Name,
                                Result = result,
                                DataType = type
                            });
                        }
                    }
                    else
                    {
                        var arrPath = address[..dotIndex];
                        var propName = address[(dotIndex + 1)..];
                        var (arrName, idx) = ParseArrayPath(arrPath);
                        if (arrName == null || idx < 0)
                            throw new InvalidOperationException("地址格式错误，必须为数组类型如 TilelifterList[2].do_work");

                        if (!doc.RootElement.TryGetProperty(arrName, out var arrElem) || arrElem.ValueKind != JsonValueKind.Array)
                            throw new InvalidOperationException($"JSON中未找到数组 {arrName}");

                        if (arrElem.GetArrayLength() <= idx)
                            throw new IndexOutOfRangeException($"数组 {arrName} 长度不足，索引 {idx} 越界");

                        var objElem = arrElem[idx];
                        if (!objElem.TryGetProperty(propName, out var propValue))
                            throw new InvalidOperationException($"数组元素中未找到属性 {propName}");

                        var (result, type) = GetValueAndType(propValue, point.DataType);
                        deviceResult.PointResults.Add(new PointResult
                        {
                            Label = propName,
                            Result = result,
                            DataType = type
                        });
                    }
                }
                else
                {
                    if (!doc.RootElement.TryGetProperty(address, out var objElem))
                        throw new InvalidOperationException($"JSON中未找到对象 {address}");

                    if (objElem.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in objElem.EnumerateObject())
                        {
                            var (result, type) = GetValueAndType(prop.Value, point.DataType);
                            deviceResult.PointResults.Add(new PointResult
                            {
                                Label = prop.Name,
                                Result = result,
                                DataType = type
                            });
                        }
                    }
                    else if (objElem.ValueKind == JsonValueKind.Array)
                    {
                        int idx = 0;
                        foreach (var item in objElem.EnumerateArray())
                        {
                            foreach (var prop in item.EnumerateObject())
                            {
                                var (result, type) = GetValueAndType(prop.Value, point.DataType);
                                deviceResult.PointResults.Add(new PointResult
                                {
                                    Label = $"{address}[{idx}].{prop.Name}",
                                    Result = result,
                                    DataType = type
                                });
                            }
                            idx++;
                        }
                    }
                    else
                    {
                        deviceResult.PointResults.Add(new PointResult
                        {
                            Label = address,
                            Result = objElem.ToString(),
                            DataType = DataType.String
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is PointFailedException)
                throw;
            throw new PointException($"{_protocolName}协议读取采集点失败", ex);
        }

        deviceResult.DevId = device.EquipmentID;

        return deviceResult;
    }

    private (object? value, DataType type) GetValueAndType(JsonElement elem, string pointDataType)
    {
        if (!Enum.TryParse<DataType>(pointDataType, true, out var dataType))
            dataType = DataType.String;

        object? value = dataType switch
        {
            DataType.Bool => elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False ? elem.GetBoolean() : Convert.ToBoolean(elem.ToString()),
            DataType.UShort => Convert.ToUInt16(elem.ToString()),
            DataType.Short => Convert.ToInt16(elem.ToString()),
            DataType.UInt => Convert.ToUInt32(elem.ToString()),
            DataType.Int => Convert.ToInt32(elem.ToString()),
            DataType.Long => Convert.ToInt64(elem.ToString()),
            DataType.ULong => Convert.ToUInt64(elem.ToString()),
            DataType.Float => Convert.ToSingle(elem.ToString()),
            DataType.Double => Convert.ToDouble(elem.ToString()),
            DataType.String => elem.ToString(),
            _ => elem.ToString()
        };

        return (value, dataType);
    }

    private (string? arrName, int idx) ParseArrayPath(string path)
    {
        var leftBracket = path.IndexOf('[');
        var rightBracket = path.IndexOf(']');
        if (leftBracket < 0 || rightBracket < 0 || rightBracket <= leftBracket)
            return (null, -1);

        var arrName = path[..leftBracket];
        var idxStr = path[(leftBracket + 1)..rightBracket];
        if (!int.TryParse(idxStr, out var idx))
            return (null, -1);

        return (arrName, idx);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
