using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Model.MqttResponses;
public class EquipmentsStatusResponse
{
    public string WorkstationId {  get; set; } = string.Empty;
    public List<EquipmentResult> EquipmentResults { get; set; } = [];
}
