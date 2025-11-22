using KEDA_Common.Entity;
using KEDA_Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Interfaces;
public interface IProtocolConfigProvider
{
    Task<ProtocolConfig?> GetLatestConfigAsync(CancellationToken token);
    bool IsConfigChanged(ProtocolConfig? latestConfig, DateTime lastConfigTime);
    Task<ProtocolEntity?> GetProtocolEntityByProtocolIdAsync(string protocolId, CancellationToken token);
    Task<ProtocolEntity?> GetProtocolEntityByDeviceIdAsync(string protocolId, CancellationToken token);
    Task<WorkstationConfig?> GetLatestWrokstationConfigAsync(CancellationToken token);
}
