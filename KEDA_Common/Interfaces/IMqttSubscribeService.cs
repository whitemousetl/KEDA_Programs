using KEDA_Common.Entity;
using KEDA_Common.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Interfaces;
public interface IMqttSubscribeService : IAsyncDisposable
{
    //订阅protocol/write固定主题，如果收到WriteTaskEntity则反序列化，执行writeTaskFunc委托传入这个WriteTaskEntity把写任务入列
    //订阅workstation/ProtocolID动态主题，ProtocolID是变量,但也固定，因为ProtocolID从配置文件中获取，就是可能多个ProtocolID，比如十个。
    //如果收到ProtocolResult则反序列化，然后把ProtocolResult传入委托做数据清洗，转换，或其他操作，然后把清洗后的数据作为主题发布到MQTT
    //委托的动作：数据清洗，转换，或其他操作，然后把清洗后的数据作为主题发布到MQTT
    Task StartAsync<T>(ConcurrentDictionary<string, Func<T, CancellationToken, Task>> topicHandles, CancellationToken token);
}

