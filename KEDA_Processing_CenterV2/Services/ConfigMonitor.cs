using KEDA_CommonV2.Interfaces;
using KEDA_Processing_CenterV2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Processing_CenterV2.Services;
public class ConfigMonitor : IConfigMonitor
{
    private readonly ILogger<ConfigMonitor> _logger;
    private string _lastConfigTime = DateTime.MinValue.ToString();
    private readonly IMqttSubscribeManager _mqttSubscribeManager;
    private readonly IWorkstationConfigProvider _workstationConfigProvider;

    public ConfigMonitor(ILogger<ConfigMonitor> logger, IWorkstationConfigProvider workstationConfigProvider, IMqttSubscribeManager mqttSubscribeManager)
    {
        _logger = logger;
        _workstationConfigProvider = workstationConfigProvider;
        _mqttSubscribeManager = mqttSubscribeManager;
    }

    public async Task MonitorAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // 减少日志频率

                // 定期检查配置更新
                await CheckForConfigurationUpdates(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // 正常取消，不记录错误
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台数据处理服务运行异常");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // 出错后等待
            }
        }
    }

    private async Task CheckForConfigurationUpdates(CancellationToken stoppingToken)
    {
        try
        {
            // 获取最新配置
            var latestConfig = await _workstationConfigProvider.GetLatestWorkstationConfigEntityAsync(stoppingToken);
            if (latestConfig == null)
            {
                _logger.LogWarning("未获取到最新协议配置");
                return;
            }

            //如果是时间的最小值，赋值为第一次查询到的时间
            if (_lastConfigTime == DateTime.MinValue.ToString())
            {
                _lastConfigTime = latestConfig.SaveTimeLocal;
                return;
            }

            // 判断配置是否变化
            if (_workstationConfigProvider.IsConfigChanged(latestConfig, _lastConfigTime))
            {
                _logger.LogInformation("检测到协议配置变更，正在重新初始化订阅...");
                _lastConfigTime = latestConfig.SaveTimeLocal;

                // 持续重试订阅直到成功或取消
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (await _mqttSubscribeManager.InitializeConfigurationAndSubscriptions(stoppingToken))
                        break;
                    _logger.LogError("配置变更后初始化订阅失败，10秒后重试...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配置热更新时发生异常");
        }
    }
}
