namespace KEDA_CommonV2.Interfaces;

public interface IEquipmentDataStorageService
{
    /// <summary>
    /// 保存设备数据到数据库
    /// </summary>
    Task SaveEquipmentDataAsync(string equipmentId, string jsonData, CancellationToken token);

    Task EnsureAllTablesTtlUpdatedAsync();
}