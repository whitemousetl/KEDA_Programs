using KEDA_CommonV2.Model;

namespace KEDA_ControllerV2.Interfaces;

public interface IWriteTaskManager
{
    Task StartConsumerAsync(CancellationToken token);

    Task TriggerWriteTaskAsync(WriteTask writeTask, CancellationToken token);
}