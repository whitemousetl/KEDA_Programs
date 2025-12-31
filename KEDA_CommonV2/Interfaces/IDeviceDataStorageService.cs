namespace KEDA_CommonV2.Interfaces;

public interface IDeviceDataStorageService
{
    /// <summary>
    /// 保存设备数据到数据库
    /// </summary>
    Task SaveDeviceDataAsync(string deviceId, string jsonData, CancellationToken token);

    Task EnsureAllTablesTtlUpdatedAsync();
}