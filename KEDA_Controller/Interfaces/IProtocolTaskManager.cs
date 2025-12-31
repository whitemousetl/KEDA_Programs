using KEDA_Common.Entity;
using KEDA_Common.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Interfaces;
public interface IProtocolTaskManager
{
    Task StopAllAsync(CancellationToken token);
    Task StartAllAsync(ProtocolConfig config, CancellationToken token);
    Task StopProtocolAsync(string protocolId,CancellationToken token);
    Task RestartProtocolAsync(string protocolId, WorkstationEntity protocol, CancellationToken token);
    ConcurrentDictionary<string, IProtocolDriver> GetDrivers();
}
