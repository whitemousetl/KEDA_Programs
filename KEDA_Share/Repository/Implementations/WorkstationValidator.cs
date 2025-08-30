using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Interfaces;

namespace KEDA_Share.Repository.Implementations;

public class WorkstationValidator : IValidator<Workstation>
{
    private readonly IValidator<Protocol> _protocolValidator;

    public WorkstationValidator(IValidator<Protocol> protocolValidator)
    {
        _protocolValidator = protocolValidator;
    }

    public ValidationResult Validate(Workstation? ws)
    {
        var result = new ValidationResult() { IsValid = true };

        if (ws == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "传入的工作站为空，请检查";
            return result;
        }

        if (string.IsNullOrWhiteSpace(ws.EdgeName))
        {
            result.IsValid = false;
            result.ErrorMessage = $"传入的工作站名字{nameof(ws.EdgeName)}为空，请检查";
            return result;
        }

        if (ws.Protocols == null || ws.Protocols.Count == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = $"传入的工作站协议数量{nameof(ws.Protocols)}为0，请检查";
            return result;
        }

        foreach (var protocol in ws.Protocols)
        {
            var validateProtocolRes = _protocolValidator.Validate(protocol);
            if (!validateProtocolRes.IsValid) return validateProtocolRes;
        }

        return result;
    }
}