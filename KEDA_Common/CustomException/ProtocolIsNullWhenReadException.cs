using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.CustomException;
public class ProtocolIsNullWhenReadException : Exception
{
    public ProtocolIsNullWhenReadException(string message, Exception? inner = null) : base(message, inner) { }
}
