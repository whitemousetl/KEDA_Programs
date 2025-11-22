using KEDA_Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Interfaces;
public interface IValidator<T>
{
    ValidationResult Validate(T? ws);
}
