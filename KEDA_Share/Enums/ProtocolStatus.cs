using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Enums;
public enum ProtocolStatus
{
    AllDeviceSuccess,
    PartialDeviceSuccess,
    AllDeviceFailture,
    WriteListEntered,
    WriteListEnteredException,
}
