using KEDA_CommonV2.Model;

namespace KEDA_ControllerV2.Interfaces;

public interface IPointExpressionConverter
{
    object? Convert(Point point, object? value);
}