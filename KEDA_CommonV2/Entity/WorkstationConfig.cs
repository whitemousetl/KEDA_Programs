namespace KEDA_CommonV2.Entity;

public class WorkstationConfig
{
    public string ConfigJson { get; set; } = string.Empty;
    public DateTime SaveTime { get; set; }
    public string SaveTimeLocal { get; set; } = string.Empty;
}