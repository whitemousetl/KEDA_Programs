using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_CommonV2.Model;

public class WriteTask
{
    public string UUID { get; set; } = string.Empty;
    public ProtocolDto Protocol { get; set; } = null!;
}