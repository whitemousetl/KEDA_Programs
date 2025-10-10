using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.CustomException;
public class PointException : Exception
{
    public PointException(string message, Exception? inner = null) : base(message, inner) { }
}