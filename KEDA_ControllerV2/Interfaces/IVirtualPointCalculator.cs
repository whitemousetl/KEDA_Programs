using KEDA_CommonV2.Model;

namespace KEDA_ControllerV2.Interfaces;

public interface IVirtualPointCalculator
{
    void Calculate(IEnumerable<Point> virtualPoints, IDictionary<string, object?> deviceData);
}