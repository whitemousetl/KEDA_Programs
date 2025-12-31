namespace KEDA_CommonV2.CustomException;

public class ProtocolWhenConnFailedException : Exception
{
    public ProtocolWhenConnFailedException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}