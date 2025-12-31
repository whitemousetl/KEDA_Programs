using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Interfaces;
public interface IProtocolDriver : IDisposable
{
    Task<ProtocolResult?> ReadAsync(WorkstationEntity protocol, string devId, PointEntity point, CancellationToken token);

    Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token);

    string GetProtocolName();
}
