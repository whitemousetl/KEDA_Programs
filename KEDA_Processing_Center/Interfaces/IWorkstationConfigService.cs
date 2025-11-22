using KEDA_Common.Model;

namespace KEDA_Processing_Center.Interfaces;

public interface IWorkstationConfigService
{
    Task<IResult> HandleAsync(Workstation? ws);
}
