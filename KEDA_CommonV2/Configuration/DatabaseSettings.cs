namespace KEDA_CommonV2.Configuration;

public class DatabaseSettings
{
    public string QuestDb { get; set; } = string.Empty;
    public string ConfigTableName { get; set; } = string.Empty;
    public string WriteLogTableName { get; set; } = string.Empty;
}