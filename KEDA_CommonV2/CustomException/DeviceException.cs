namespace KEDA_CommonV2.CustomException;

public class DeviceException : Exception
{
    public DeviceException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}