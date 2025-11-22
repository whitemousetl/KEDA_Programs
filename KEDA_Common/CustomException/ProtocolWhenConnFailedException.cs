using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.CustomException;
public class ProtocolWhenConnFailedException : Exception
{
    public ProtocolWhenConnFailedException(string message, Exception? inner = null) : base(message, inner) { }
}