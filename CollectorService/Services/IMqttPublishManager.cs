using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Services;

public interface IMqttPublishManager
{
    Task ProcessDataAsync(ProtocolResult protocolResult, List<EquipmentDto> equipments, CancellationToken token);

    Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token);
}
