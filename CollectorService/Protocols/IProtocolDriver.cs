using CollectorService.Models;
using KEDA_Share.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Protocols;
public interface IProtocolDriver : IDisposable
{
    Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token);
}