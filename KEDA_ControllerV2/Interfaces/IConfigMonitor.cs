namespace KEDA_ControllerV2.Interfaces;

public interface IConfigMonitor
{
    Task MonitorAsync(CancellationToken stoppingToken);
}