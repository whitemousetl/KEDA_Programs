using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Entity;

public class WriteTaskLog
{
    public string UUID { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public string WriteTaskJson { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string TimeLocal { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Msg { get; set; } = string.Empty;
}