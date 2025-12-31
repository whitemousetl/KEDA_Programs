namespace KEDA_CommonV2.CustomException;

public class ProtocolDefaultException : Exception
{
    public ProtocolDefaultException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}