using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;

namespace KEDA_ControllerV2.Interfaces;

public interface IPointExpressionConverter
{
    object? Convert(ParameterDto point, object? value);
}