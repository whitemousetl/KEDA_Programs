using KEDA_Common.Model;

namespace KEDA_Processing_Center.Interfaces;

public interface IWriteTaskService
{
    Task<IResult> HandleAsync(List<WritePointData> writePoints);
}
