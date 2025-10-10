using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Implementations;
public class WorkstationProvider : IWorkstationProvider
{
    private readonly IWorkstationRepository _repo;
    private readonly ILogger<WorkstationProvider> _logger;
    private Workstation? _workstation;

    public WorkstationProvider(IWorkstationRepository repo, ILogger<WorkstationProvider> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Workstation? Current => Volatile.Read(ref _workstation);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var ws = await _repo.GetLatestByTimestampAsync(cancellationToken);
                var current = Volatile.Read(ref _workstation);

                if (ws != null)
                {
                    if (current == null || ws.Timestamp != current.Timestamp)
                    {
                        Interlocked.Exchange(ref _workstation, ws);
                        string message = $"工作站配置已更新, 时间: {ws.Time}";
                        _logger.LogInformation("工作站配置已更新, 时间: {Time}", ws.Time);
                    }
                }
                else
                    _logger.LogWarning("未获取到 Workstation, 15秒后重试...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 Workstation 时发生异常, 15秒后重试...");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        }
    }
}