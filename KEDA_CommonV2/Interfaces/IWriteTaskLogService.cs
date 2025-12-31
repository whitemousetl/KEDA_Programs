using KEDA_CommonV2.Entity;

namespace KEDA_CommonV2.Interfaces;

public interface IWriteTaskLogService
{
    Task AddLogAsync(WriteTaskLog log);
}