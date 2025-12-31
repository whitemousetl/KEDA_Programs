using KEDA_Common.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Interfaces;
public interface IWriteTaskLogService
{
    Task AddLogAsync(WriteTaskLog log);
}
