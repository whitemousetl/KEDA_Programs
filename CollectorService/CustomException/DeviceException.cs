using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.CustomException;
public class DeviceException : Exception
{
    public DeviceException(string message, Exception? inner = null) : base(message, inner) { }
}
