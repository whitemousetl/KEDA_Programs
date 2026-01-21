using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Services;

public interface IEquipmentDataProcessor
{
    ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, List<EquipmentDto> equipments, CancellationToken token);
}