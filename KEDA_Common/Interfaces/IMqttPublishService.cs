using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Interfaces;
public interface IMqttPublishService
{
    Task<bool> PublishAsync(string topic, byte[] payload, CancellationToken token);
    Task<bool> PublishAsync(string topic, string payload, CancellationToken token);
}
