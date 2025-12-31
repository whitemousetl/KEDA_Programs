namespace KEDA_CommonV2.CustomException;

public class PointException : Exception
{
    public PointException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}