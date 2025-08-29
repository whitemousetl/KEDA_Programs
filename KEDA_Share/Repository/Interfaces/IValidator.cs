using KEDA_Share.Entity;
using KEDA_Share.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Interfaces;
public interface IValidator<T>
{
    ValidationResult Validate(T? ws);
}
