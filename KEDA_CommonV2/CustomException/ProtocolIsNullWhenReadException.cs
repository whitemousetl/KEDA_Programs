namespace KEDA_CommonV2.CustomException;

public class ProtocolIsNullWhenReadException : Exception
{
    public ProtocolIsNullWhenReadException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}