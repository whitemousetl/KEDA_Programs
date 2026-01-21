using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Interfaces.IMqttServices;
using KEDA_CommonV2.Services.MqttServices;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Services;

public class MqttSubscribeServiceTest
{
    private static Mock<ISharedConfigHelper> CreateSharedMock(int reconnect = 1, int maxReconnect = 8, int messageTimeout = 30)
    {
        var shared = new Mock<ISharedConfigHelper>();
        var mqttSettings = new MqttSettings
        {
            Server = "127.0.0.1",
            Port = 1883,
            Username = "u",
            Password = "p",
            ReconnectDelaySeconds = reconnect,
            MaxReconnectDelaySeconds = maxReconnect,
            MessageTimeoutSeconds = messageTimeout,
            AutoReconnect = true
        };

        var topics = new MqttTopicSettings
        {
            WorkstationConfigSendPrefix = "workstation/config/send",
            ProtocolWritePrefix = "protocol/write"
        };

        shared.SetupGet(s => s.MqttSettings).Returns(mqttSettings);
        shared.SetupGet(s => s.MqttTopicSettings).Returns(topics);

        // Provide a MqttClientOptions to satisfy code paths that reference it
        shared.SetupGet(s => s.MqttClientOptions).Returns(new MqttClientOptionsBuilder().WithClientId("t").WithTcpServer("127.0.0.1").Build());

        return shared;
    }

    private static MqttSubscribeService CreateService(Mock<ISharedConfigHelper> sharedMock, Mock<IMqttClientAdapter> clientAdapterMock, ILogger<MqttSubscribeService>? logger = null)
    {
        var log = logger ?? Mock.Of<ILogger<MqttSubscribeService>>();
        return new MqttSubscribeService(log, sharedMock.Object, clientAdapterMock.Object);
    }

    [Fact(DisplayName = "构造函数：logger 为 null 抛出")]
    public void Ctor_NullLogger_Throws()
    {
        var shared = CreateSharedMock();
        var adapter = new Mock<IMqttClientAdapter>();
        Assert.Throws<ArgumentNullException>(() => new MqttSubscribeService(null!, shared.Object, adapter.Object));
    }


}
