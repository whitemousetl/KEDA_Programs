namespace KEDA_CommonV2.CustomException;

public class ProtocolIsNullWhenWriteException : Exception
{
    public ProtocolIsNullWhenWriteException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}