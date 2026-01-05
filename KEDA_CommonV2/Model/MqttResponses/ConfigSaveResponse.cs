namespace KEDA_CommonV2.Model.MqttResponses;
public class ConfigSaveResponse
{
    public string WorkstationId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}
