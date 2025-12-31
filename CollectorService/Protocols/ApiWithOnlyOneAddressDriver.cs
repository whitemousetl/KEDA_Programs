using System.Text.Json;
using CollectorService.CustomException;
using CollectorService.Models;
using KEDA_Share.Entity;
using KEDA_Share.Enums;

namespace CollectorService.Protocols;
public class ApiWithOnlyOneAddressDriver : IProtocolDriver
{
    private static readonly HttpClient _httpClient = new(); // 复用HttpClient
    private readonly string _protocolName = "ApiWithOnlyOneAddressDriver";

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
            var response = await _httpClient.GetAsync(protocol.IPAddress, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(json);

            var dict = new Dictionary<string, object?>();
            FlattenJson(doc.RootElement, "", dict);

            foreach (var kv in dict)
            {
                var (value, type) = GetValueAndType(kv.Value);
                deviceResult.PointResults.Add(new PointResult
                {
                    Label = kv.Key,
                    Result = value,
                    DataType = type
                });
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

    // 递归打平Json
    private void FlattenJson(JsonElement element, string prefix, Dictionary<string, object?> dict)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenJson(prop.Value, key, dict);
                }
                break;
            case JsonValueKind.Array:
                int idx = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{idx}]";
                    FlattenJson(item, key, dict);
                    idx++;
                }
                break;
            default:
                dict[prefix] = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.TryGetInt64(out var l) ? l :
                                           element.TryGetDouble(out var d) ? d : element.GetRawText(),
                    JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
                    _ => element.GetRawText()
                };
                break;
        }
    }

    // 推断DataType
    private (object? value, DataType type) GetValueAndType(object? value)
    {
        return value switch
        {
            bool => (value, DataType.Bool),
            long => (value, DataType.Long),
            int => (value, DataType.Int),
            double => (value, DataType.Double),
            string => (value, DataType.String),
            _ => (value?.ToString(), DataType.String)
        };
    }

    public void Dispose()
    {
        // 无需资源释放
        GC.SuppressFinalize(this);
    }
}



//using CollectorService.CustomException;
//using CollectorService.Models;
//using KEDA_Share.Entity;
//using KEDA_Share.Enums;
//using Org.BouncyCastle.Tls;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace CollectorService.Protocols;
//public class ApiWithOnlyOneAddressDriver : IProtocolDriver
//{

//    private string _protocolName = "ApiWithOnlyAddress";
//    public Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)//完成这个
//    {
//        throw new NotImplementedException();
//    }

//    public async Task<DeviceResult> ReadAsync(Protocol protocol, Device device, CancellationToken token)
//    {
//        var deviceResult = new DeviceResult
//        {
//            PointResults = [],
//        };

//        try
//        {
//            using var httpClient = new HttpClient();
//            var response = await httpClient.GetAsync(protocol.IPAddress, token);
//            response.EnsureSuccessStatusCode();

//            var json  = await response.Content.ReadAsStringAsync();
//            using var doc = JsonDocument.Parse(json);

//            var point = device.Points.FirstOrDefault() ?? throw new InvalidOperationException("设备未配置采集点");

//            var address = point.Address;
//            if (address.Contains('[') && address.Contains(']'))
//            {
//                var dotIndex = address.IndexOf('.');
//                if (dotIndex < 0)
//                {
//                    var (arrName, idx) = ParseArrayPath(address);
//                    if (arrName == null || idx < 0)
//                        throw new InvalidOperationException("地址格式错误，必须为数组类型如 TilelifterList[2]");

//                    if (!doc.RootElement.TryGetProperty(arrName, out var arrElem) || arrElem.ValueKind != JsonValueKind.Array)
//                        throw new InvalidOperationException($"JSON中未找到数组 {arrName}");

//                    if (arrElem.GetArrayLength() <= idx)
//                        throw new IndexOutOfRangeException($"数组 {arrName} 长度不足，索引 {idx} 越界");

//                    var objElem = arrElem[idx];
//                    foreach (var prop in objElem.EnumerateObject())
//                    {
//                        var (result, type) = GetValueAndType(prop.Value);
//                        deviceResult.PointResults.Add(new PointResult
//                        {
//                            Label = prop.Name,
//                            Result = result,
//                            DataType = type
//                        });
//                    }
//                }
//                else
//                {
//                    var arrPath = address[..dotIndex];
//                    var propName = address[(dotIndex + 1)..];
//                    var (arrName, idx) = ParseArrayPath(arrPath);
//                    if (arrName == null || idx < 0)
//                        throw new InvalidOperationException("地址格式错误，必须为数组类型如 TilelifterList[2].do_work");

//                    if (!doc.RootElement.TryGetProperty(arrName, out var arrElem) || arrElem.ValueKind != JsonValueKind.Array)
//                        throw new InvalidOperationException($"JSON中未找到数组 {arrName}");

//                    if (arrElem.GetArrayLength() <= idx)
//                        throw new IndexOutOfRangeException($"数组 {arrName} 长度不足，索引 {idx} 越界");

//                    var objElem = arrElem[idx];
//                    if (!objElem.TryGetProperty(propName, out var propValue))
//                        throw new InvalidOperationException($"数组元素中未找到属性 {propName}");

//                    var (result, type) = GetValueAndType(propValue);
//                    deviceResult.PointResults.Add(new PointResult
//                    {
//                        Label = propName,
//                        Result = result,
//                        DataType = type
//                    });
//                }
//            }
//            else
//            {
//                if (!doc.RootElement.TryGetProperty(address, out var objElem) || objElem.ValueKind != JsonValueKind.Object)
//                    throw new InvalidOperationException($"JSON中未找到对象 {address}");

//                foreach (var prop in objElem.EnumerateObject())
//                {
//                    var (result, type) = GetValueAndType(prop.Value);
//                    deviceResult.PointResults.Add(new PointResult
//                    {
//                        Label = prop.Name,
//                        Result = result,
//                        DataType = type
//                    });
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            if (ex is PointFailedException)
//                throw;
//            throw new PointException($"{_protocolName}协议读取采集点失败", ex);
//        }

//        return deviceResult;
//    }

//    private (object? value, DataType type) GetValueAndType(JsonElement elem)
//    {
//        return elem.ValueKind switch
//        {
//            JsonValueKind.String => (elem.GetString(), DataType.String),
//            JsonValueKind.Number => elem.TryGetInt64(out var l) ? (l, DataType.Long) :
//                                   elem.TryGetInt32(out var i) ? (i, DataType.Int) :
//                                   elem.TryGetDouble(out var d) ? (d, DataType.Double) :
//                                   (elem.GetDouble(), DataType.Double),
//            JsonValueKind.True or JsonValueKind.False => (elem.GetBoolean(), DataType.Bool),
//            _ => (elem.ToString(), DataType.String)
//        };
//    }

//    private (string? arrName, int idx) ParseArrayPath(string path)
//    {
//        var leftBracket = path.IndexOf('[');
//        var rightBracket = path.IndexOf(']');
//        if (leftBracket < 0 || rightBracket < 0 || rightBracket <= leftBracket)
//            return (null, -1);

//        var arrName = path[..leftBracket];
//        var idxStr = path[(leftBracket + 1)..rightBracket];
//        if (!int.TryParse(idxStr, out var idx))
//            return (null, -1);

//        return (arrName, idx);
//    }

//    public void Dispose()
//    {
//        // 无需资源释放
//        GC.SuppressFinalize(this);
//    }
//}