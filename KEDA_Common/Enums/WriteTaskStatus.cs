using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Enums;
public enum WriteTaskStatus
{
    NotExecuted = 100,
    Cancel = 102,
    Error = 103,
    Done = 104
}

