using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_Common.Services;
public class ProtocolConfigProvider : IProtocolConfigProvider
{
    private readonly ILogger<ProtocolConfigProvider> _logger;
    private readonly ISqlSugarClientFactory _dbFactory;

    public ProtocolConfigProvider(ILogger<ProtocolConfigProvider> logger, ISqlSugarClientFactory dbFactory)
    {
        _logger = logger;
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// 拿最新的协议配置json，处理后的json，删除多余属性的json
    /// </summary>
    public async Task<ProtocolConfig?> GetLatestConfigAsync(CancellationToken token)
    {
        using var db = _dbFactory.CreateClient();
        try
        {
            return await db.Queryable<ProtocolConfig>()
                .OrderByDescending(x => x.SaveTime)
                .FirstAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"查询数据库最新的ProtocolConfig时发生异常，信息{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据ProtocoId找到ProtocolEntity
    /// </summary>
    public async Task<ProtocolEntity?> GetProtocolEntityByProtocolIdAsync(string protocolId, CancellationToken token)
    {
        using var db = _dbFactory.CreateClient();
        try
        {
            var config = await db.Queryable<ProtocolConfig>()
                .OrderByDescending(x => x.SaveTime)
                .FirstAsync(token);

            if (config == null || string.IsNullOrWhiteSpace(config.ConfigJson))
                return null;

            var workstationEntity = JsonSerializer.Deserialize<WorkstationEntity>(config.ConfigJson);
            return workstationEntity?.Protocols?.FirstOrDefault(p => p.ProtocolID == protocolId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"根据ProtocolId[{protocolId}]查询ProtocolEntity时发生异常，信息{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据DeviceId找到ProtocolEntity
    /// </summary>
    public async Task<ProtocolEntity?> GetProtocolEntityByDeviceIdAsync(string deviceId, CancellationToken token)
    {
        using var db = _dbFactory.CreateClient();
        try
        {
            var config = await db.Queryable<ProtocolConfig>()
                .OrderByDescending(x => x.SaveTime)
                .FirstAsync(token);

            if (config == null || string.IsNullOrWhiteSpace(config.ConfigJson))
                return null;

            var workstationEntity = JsonSerializer.Deserialize<WorkstationEntity>(config.ConfigJson);
            return workstationEntity?.Protocols?.FirstOrDefault(p => p.Devices.Any(d => d.EquipmentId == deviceId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"根据deviceId[{deviceId}]查询ProtocolEntity时发生异常，信息{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 拿原始的WorkstationConfig配置，有转换，清洗等属性的
    /// </summary>
    public async Task<WorkstationConfig?> GetLatestWrokstationConfigAsync(CancellationToken token)
    {
        using var db = _dbFactory.CreateClient();
        try
        {
            return await db.Queryable<WorkstationConfig>()
                .OrderByDescending(x => x.SaveTime)
                .FirstAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"查询数据库最新的ProtocolConfig时发生异常，信息{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 判断当前配置的时间是否最新
    /// </summary>
    public bool IsConfigChanged(ProtocolConfig? latestConfig, DateTime lastConfigTime)
    {
        return latestConfig != null && latestConfig.SaveTime != lastConfigTime;
    }
}
