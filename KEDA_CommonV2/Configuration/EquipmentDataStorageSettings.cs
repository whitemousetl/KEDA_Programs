namespace KEDA_CommonV2.Configuration;

public class EquipmentDataStorageSettings
{
    /// <summary>
    /// 数据保留天数（默认3天）
    /// </summary>
    public int DataRetentionDays { get; set; } = 3;

    /// <summary>
    /// 清理任务执行间隔（小时，默认24小时）
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}