namespace KEDA_CommonV2.CustomException;

public class PointWhenReadFailedException : Exception
{
    public PointWhenReadFailedException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}