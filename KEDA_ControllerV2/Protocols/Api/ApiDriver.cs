using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Text.Json;

namespace KEDA_ControllerV2.Protocols.Api;
[ProtocolType(ProtocolType.Api)]
public class ApiDriver : IProtocolDriver
{
    private readonly string _protocolName = "Api"; // 协议名称

    public async Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token)
    {
        try
        {
            var apiProtocol = new ApiProtocolDto();
            using var client = CreateConnection(protocol, out apiProtocol, token);

            var result = new ProtocolResult
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ProtocolId = apiProtocol.Id,
                ProtocolType = apiProtocol.ProtocolType.ToString(),
                EquipmentResults = [],
                StartTime = string.Empty,
                EndTime = string.Empty,
            };

            var protocolStartTime = DateTime.Now;
            result.StartTime = protocolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var request = new HttpRequestMessage(apiProtocol.RequestMethod
            switch
            {
                RequestMethod.Get => HttpMethod.Get,
                RequestMethod.Post => HttpMethod.Post,
                RequestMethod.Put => HttpMethod.Put,
                RequestMethod.Delete => HttpMethod.Delete,
                _ => HttpMethod.Get
            }, apiProtocol.AccessApiString);

            var response = await client.SendAsync(request, token);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(token);

            foreach (var equipment in apiProtocol.Equipments)
            {
                var equipmentResult = new EquipmentResult
                {
                    EquipmentId = equipment.Id,
                    EquipmentName = equipment.Name,
                    PointResults = []
                };

                var startTime = DateTime.Now;
                equipmentResult.StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

                foreach (var point in equipment.Parameters)
                {
                    var address = point.Address;
                    if (string.IsNullOrWhiteSpace(address)) continue;

                    var pointResult = BuildPointResult(point, content);
                    equipmentResult.PointResults.Add(pointResult);
                }

                var endTime = DateTime.Now;
                equipmentResult.EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                equipmentResult.ElapsedMs = (long)(endTime - startTime).TotalMilliseconds;

                equipmentResult.TotalPoints = equipmentResult.PointResults.Count;
                equipmentResult.SuccessPoints = equipmentResult.PointResults.Count(p => p.ReadIsSuccess);
                equipmentResult.FailedPoints = equipmentResult.PointResults.Count(p => !p.ReadIsSuccess);
                equipmentResult.ReadIsSuccess = equipmentResult.PointResults.All(p => p.ReadIsSuccess);
                equipmentResult.ErrorMsg = equipmentResult.PointResults.FirstOrDefault(p => !p.ReadIsSuccess)?.ErrorMsg ?? string.Empty;

                result.EquipmentResults.Add(equipmentResult);
            }

            var protocolEndTime = DateTime.Now;
            result.EndTime = protocolEndTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            result.ElapsedMs = (long)(protocolEndTime - protocolStartTime).TotalMilliseconds;

            result.TotalEquipments = result.EquipmentResults.Count;
            result.SuccessEquipments = result.EquipmentResults.Count(d => d.ReadIsSuccess);
            result.FailedEquipments = result.EquipmentResults.Count(d => !d.ReadIsSuccess);

            result.TotalPoints = result.EquipmentResults.Sum(d => d.TotalPoints);
            result.SuccessPoints = result.EquipmentResults.Sum(d => d.SuccessPoints);
            result.FailedPoints = result.EquipmentResults.Sum(d => d.FailedPoints);

            result.ReadIsSuccess = result.EquipmentResults.All(d => d.ReadIsSuccess);
            result.ErrorMsg = result.EquipmentResults.FirstOrDefault(d => !d.ReadIsSuccess)?.ErrorMsg ?? string.Empty;

            return result;
        }
        catch (HttpRequestException ex)
        {
            // 构造失败的 ProtocolResult
            var result = new ProtocolResult
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ProtocolId = protocol.Id,
                ProtocolType = protocol.ProtocolType.ToString(),
                EquipmentResults = [],
                ReadIsSuccess = false,
                ErrorMsg = $"HTTP请求失败: {ex.Message}",
                StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ElapsedMs = 0
            };

            // 按照 UpdatePointResultIfEquipmentError 的逻辑，补全每个设备的所有点为失败
            if (protocol.Equals != null)
            {
                foreach (var equipment in protocol.Equipments)
                {
                    var equipmentResult = new EquipmentResult
                    {
                        EquipmentId = equipment.Id,
                        EquipmentName = equipment.Name,
                        ReadIsSuccess = false,
                        ErrorMsg = $"HTTP请求失败: {ex.Message}",
                        PointResults = [],
                        TotalPoints = 0,
                        SuccessPoints = 0,
                        FailedPoints = 0,
                        StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        ElapsedMs = 0
                    };

                    if (equipment.Parameters != null)
                    {
                        foreach (var point in equipment.Parameters)
                        {
                            equipmentResult.PointResults.Add(new PointResult
                            {
                                Address = point.Address,
                                Label = point.Label,
                                DataType = point.DataType,
                                ReadIsSuccess = false,
                                Value = null,
                                ErrorMsg = equipmentResult.ErrorMsg,
                                ElapsedMs = 0
                            });
                        }
                    }

                    // 补全统计字段
                    equipmentResult.TotalPoints = equipmentResult.PointResults.Count;
                    equipmentResult.SuccessPoints = equipmentResult.PointResults.Count(p => p.ReadIsSuccess);
                    equipmentResult.FailedPoints = equipmentResult.TotalPoints - equipmentResult.SuccessPoints;

                    result.EquipmentResults.Add(equipmentResult);
                }
            }

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

    public static object? GetValueFromJson(string json, string address, out string label, out string finalAddress)
    {
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        label = address;
        finalAddress = address;

        var parts = address.Split('.');
        foreach (var part in parts)
        {
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(part, @"(\w+)\[(\d+)\]");
            if (arrayMatch.Success)
            {
                var prop = arrayMatch.Groups[1].Value;
                var idx = int.Parse(arrayMatch.Groups[2].Value);
                element = element.GetProperty(prop);
                element = element[idx];
                label = $"{prop}[{idx}]";
                finalAddress = $"{prop}[{idx}]";
            }
            else
            {
                element = element.GetProperty(part);
                label = part;
                finalAddress = part;
            }
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => element.ToString()
        };
    }

    // 用法
    public static PointResult BuildPointResult(ParameterDto point, string json)
    {
        var value = GetValueFromJson(json, point.Address, out var label, out var address);
        return new PointResult
        {
            DataType = point.DataType,
            Label = point.Label,
            Address = address,
            Value = value,
            ReadIsSuccess = value != null,
            ErrorMsg = value == null ? "未找到对应数据" : string.Empty
        };
    }

    public HttpClient CreateConnection(ProtocolDto protocol, out ApiProtocolDto apiProtocol, CancellationToken token)
    {
        if (protocol is ApiProtocolDto p)
        {
            var conn = new HttpClient();
            apiProtocol = p;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 ApiProtocol，无法进行操作。");
    }

    public string GetProtocolName() => _protocolName;

    #region 不用实现
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }


    public Task<PointResult?> ReadAsync(ProtocolDto protocol, string equipmentId, ParameterDto point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    #endregion
}
