using KEDA_CommonV2.Model;
using KEDA_Processing_CenterV2.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KEDA_Processing_CenterV2.Services;
public class DeviceDataProcessor : IDeviceDataProcessor
{
    private readonly IVirtualPointCalculator _virtualPointCalculator;
    private readonly IPointExpressionConverter _pointExpressionConverter;
    private readonly JsonSerializerOptions _jsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public DeviceDataProcessor(IPointExpressionConverter pointExpressionConverter, IVirtualPointCalculator virtualPointCalculator)
    {
        _pointExpressionConverter = pointExpressionConverter;
        _virtualPointCalculator = virtualPointCalculator;
    }

    public ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, Protocol protocol, CancellationToken token)
    {
        var deviceJsonDataMap = new ConcurrentDictionary<string, string>();
        foreach (var deviceResult in protocolResult.DeviceResults)
        {
            if (string.IsNullOrEmpty(deviceResult.EquipmentId) || deviceResult.PointResults == null || deviceResult.PointResults.Count == 0) continue; //如果设备设备结果的设备id为空 或 设备结果的点结果列表为空 或 设备结果的点结果列表数量是0 跳过当前设备结果

            string timeStr = protocolResult.Time;
            var dt = DateTime.ParseExact(timeStr, "yyyy-MM-dd HH:mm:ss.fff", null);
            long timestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

            //设备结果转换， 有两个固定点，设备id 和 时间戳
            var forwardDeviceResult = new ConcurrentDictionary<string, object?>();
            forwardDeviceResult["DeviceId"] = deviceResult.EquipmentId;
            forwardDeviceResult["timestamp"] = timestamp;

            var device = protocol.Devices.FirstOrDefault(d => d.EquipmentID == deviceResult.EquipmentId); //从协议中找到对应设备

            if (device == null) continue; //如果对应设备为空， 跳过当前设备结果

            // 收集虚拟点
            var virtualPoints = new ConcurrentBag<Point>();

            // 处理设备结果
            ProcessDeviceResult(deviceResult, device, virtualPoints, forwardDeviceResult);
            // 处理虚拟点并发布处理结果
            SerializeDeviceData(deviceResult, forwardDeviceResult, virtualPoints, deviceJsonDataMap);

        }
        return deviceJsonDataMap;
    }

    private void ProcessDeviceResult(DeviceResult deviceResult, Device device, ConcurrentBag<Point> virtualPoints, ConcurrentDictionary<string, object?> forwardDeviceResult)
    {
        foreach (var pointResult in deviceResult.PointResults)
        {
            if (!IsValidPointResult(pointResult)) continue; // 点结果或点结果标签为空， 跳过当前设备结果
            var point = device.Points.FirstOrDefault(p => p.Label == pointResult.Label); //从设备中找到与设备结果标签一样的点，目的找到配置中该点的转换条件或虚拟点
            if (point == null) continue;

            if (point.Address == "VirtualPoint")
            {
                virtualPoints.Add(point);
                continue;
            }

            var finalValue = _pointExpressionConverter.Convert(point, pointResult.Value);

            if (!string.IsNullOrEmpty(pointResult?.Label))
                forwardDeviceResult[pointResult.Label] = finalValue;
        }
    }

    private static bool IsValidPointResult(PointResult pointResult) => pointResult != null && !string.IsNullOrWhiteSpace(pointResult.Label);

    private void SerializeDeviceData(DeviceResult deviceResult, ConcurrentDictionary<string, object?> forwardDeviceResult, ConcurrentBag<Point> virtualPoints, ConcurrentDictionary<string, string> dataDevId)
    {
        if (forwardDeviceResult.Count > 2)
        {
            // 处理虚拟点
            _virtualPointCalculator.Calculate(virtualPoints, forwardDeviceResult);

            var data = JsonSerializer.Serialize(forwardDeviceResult, _jsonOptions);

            dataDevId[deviceResult.EquipmentId] = data;
        }
    }
}
