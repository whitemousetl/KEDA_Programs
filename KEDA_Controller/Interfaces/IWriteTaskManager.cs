using KEDA_Common.Entity;
using KEDA_Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Interfaces;
public interface IWriteTaskManager
{
    Task StartConsumerAsync(CancellationToken token);
    Task InitSubscribeAsync(CancellationToken token);
    Task TriggerWriteTaskAsync(WritePointData writePointData, CancellationToken token);
}
