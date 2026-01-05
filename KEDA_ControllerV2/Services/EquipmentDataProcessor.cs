using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class EquipmentDataProcessor : IEquipmentDataProcessor
{
    private readonly IVirtualPointCalculator _virtualPointCalculator;
    private readonly IPointExpressionConverter _pointExpressionConverter;
    private readonly JsonSerializerOptions _jsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public EquipmentDataProcessor(IPointExpressionConverter pointExpressionConverter, IVirtualPointCalculator virtualPointCalculator)
    {
        _pointExpressionConverter = pointExpressionConverter;
        _virtualPointCalculator = virtualPointCalculator;
    }

    public ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, ProtocolDto protocol, CancellationToken token)
    {
        var equipmentJsonDataMap = new ConcurrentDictionary<string, string>();
        foreach (var equipmentResult in protocolResult.EquipmentResults)
        {
            if (string.IsNullOrEmpty(equipmentResult.EquipmentId) || equipmentResult.PointResults == null || equipmentResult.PointResults.Count == 0) continue; //如果设备设备结果的设备id为空 或 设备结果的点结果列表为空 或 设备结果的点结果列表数量是0 跳过当前设备结果

            string timeStr = protocolResult.Time;
            var dt = DateTime.ParseExact(timeStr, "yyyy-MM-dd HH:mm:ss.fff", null);
            long timestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

            //设备结果转换， 有两个固定点，设备id 和 时间戳
            var forwardEquipmentResult = new ConcurrentDictionary<string, object?>();
            forwardEquipmentResult["EquipmentId"] = equipmentResult.EquipmentId;
            forwardEquipmentResult["timestamp"] = timestamp;

            var equipment = protocol.Equipments.FirstOrDefault(d => d.Id == equipmentResult.EquipmentId); //从协议中找到对应设备

            if (equipment == null) continue; //如果对应设备为空， 跳过当前设备结果

            // 收集虚拟点
            var virtualPoints = new ConcurrentBag<ParameterDto>();

            // 处理设备结果
            ProcessEquipmentResult(equipmentResult, equipment, virtualPoints, forwardEquipmentResult);
            // 处理虚拟点并发布处理结果
            SerializeEquipmentData(equipmentResult, forwardEquipmentResult, virtualPoints, equipmentJsonDataMap);
        }
        return equipmentJsonDataMap;
    }

    private void ProcessEquipmentResult(EquipmentResult equipmentResult, EquipmentDto equipment, ConcurrentBag<ParameterDto> virtualPoints, ConcurrentDictionary<string, object?> forwardEquipmentResult)
    {
        foreach (var pointResult in equipmentResult.PointResults)
        {
            if (!IsValidPointResult(pointResult)) continue; // 点结果或点结果标签为空， 跳过当前设备结果
            var point = equipment.Parameters.FirstOrDefault(p => p.Label == pointResult.Label); //从设备中找到与设备结果标签一样的点，目的找到配置中该点的转换条件或虚拟点
            if (point == null) continue;

            if (point.Address == "VirtualPoint")
            {
                virtualPoints.Add(point);
                continue;
            }

            var finalValue = _pointExpressionConverter.Convert(point, pointResult.Value);

            if (!string.IsNullOrEmpty(pointResult?.Label))
                forwardEquipmentResult[pointResult.Label] = finalValue;
        }
    }

    private static bool IsValidPointResult(PointResult pointResult) => pointResult != null && !string.IsNullOrWhiteSpace(pointResult.Label);

    private void SerializeEquipmentData(EquipmentResult equipmentResult, ConcurrentDictionary<string, object?> forwardEquipmentResult, ConcurrentBag<ParameterDto> virtualPoints, ConcurrentDictionary<string, string> dataEquipmentId)
    {
        if (forwardEquipmentResult.Count > 2)
        {
            // 处理虚拟点
            _virtualPointCalculator.Calculate(virtualPoints, forwardEquipmentResult);

            var data = JsonSerializer.Serialize(forwardEquipmentResult, _jsonOptions);

            dataEquipmentId[equipmentResult.EquipmentId] = data;
        }
    }
}