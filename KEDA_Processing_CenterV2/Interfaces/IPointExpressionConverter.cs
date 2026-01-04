using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IPointExpressionConverter
{
    object? Convert(ParameterDto point, object? value);
}
