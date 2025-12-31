using KEDA_CommonV2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IMqttPublishManager
{
    Task ProcessDataAsync(ProtocolResult protocolResult, Protocol protocol, CancellationToken token);
    Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token);
}
