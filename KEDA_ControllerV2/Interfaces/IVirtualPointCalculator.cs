using KEDA_CommonV2.Model.Workstations;

namespace KEDA_ControllerV2.Interfaces;

public interface IVirtualPointCalculator
{
    void Calculate(IEnumerable<ParameterDto> virtualPoints, IDictionary<string, object?> equipmentData);
}