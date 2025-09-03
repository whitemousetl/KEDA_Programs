using KEDA_Receiver.Services.Interfaces;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Interfaces;

namespace KEDA_Receiver.Services.Implementtations;

public class WorkstationConfigService : IWorkstationConfigService
{
    private readonly IWorkstationRepository _workstationRepo;
    private readonly IValidator<Workstation> _validator;

    public WorkstationConfigService(IWorkstationRepository workstationRepo, IValidator<Workstation> validator)
    {
        _workstationRepo = workstationRepo;
        _validator = validator;
    }

    public async Task<IResult> HandleAsync(Workstation? ws)
    {
        var res = _validator.Validate(ws);

        if (!res.IsValid) return Results.Ok(ApiResponse<string>.Fial(res.ErrorMessage ?? "服务器无返回错误信息"));

        await _workstationRepo.AddAsync(ws!);

        return Results.Ok(ApiResponse<string>.Success($"保存 Workstation 成功，EdgeID: {ws!.EdgeName}"));
    }
}