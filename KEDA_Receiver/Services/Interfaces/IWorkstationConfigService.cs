using KEDA_Share.Entity;

namespace KEDA_Receiver.Services.Interfaces;

public interface IWorkstationConfigService
{
    Task<IResult> HandleAsync(Workstation? ws);
}
