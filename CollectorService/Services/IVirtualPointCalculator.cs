using KEDA_CommonV2.Model.Workstations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Services;

public interface IVirtualPointCalculator
{
    void Calculate(IEnumerable<ParameterDto> virtualPoints, IDictionary<string, object?> equipmentData);
}
