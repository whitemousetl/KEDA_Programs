using KEDA_Share.Model;

namespace KEDA_Share.Repository.Interfaces;

public interface IValidator<T>
{
    ValidationResult Validate(T? ws);
}