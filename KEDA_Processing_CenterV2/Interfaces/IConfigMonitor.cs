using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IConfigMonitor
{
    Task MonitorAsync(CancellationToken stoppingToken);
}
