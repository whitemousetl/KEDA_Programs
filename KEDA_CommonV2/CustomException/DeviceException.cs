namespace KEDA_CommonV2.CustomException;

public class EquipmentException : Exception
{
    public EquipmentException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}