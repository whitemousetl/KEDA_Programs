using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IMqttSubscribeManager
{
    Task InitialAsync(CancellationToken stoppingToken);
    Task<bool> InitializeConfigurationAndSubscriptions(CancellationToken stoppingToken);
}
