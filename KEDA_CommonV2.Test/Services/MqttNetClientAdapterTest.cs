using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Services;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace KEDA_CommonV2.Test.Services;

public class MqttNetClientAdapterTest
{
    [Fact(DisplayName = "底层 ApplicationMessageReceivedAsync 触发时应转发 MessageReceived")]
    public async Task MessageReceived_Event_Should_Be_Forwarded()
    {
        var mockClient = new Mock<IMqttClient>();
        Func<MqttApplicationMessageReceivedEventArgs, Task>? capturedHandler = null;

        // 把实际传入的委托保存到本地变量 capturedHandler 中
        mockClient.SetupAdd(c => c.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
            .Callback<Func<MqttApplicationMessageReceivedEventArgs, Task>>(h => capturedHandler = h);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        bool called = false;
        adapter.MessageReceived += e => { called = true; return Task.CompletedTask; };

        // 模拟底层事件被触发（参数可以为 null，因为我们只验证事件转发）
        Assert.NotNull(capturedHandler);
        await capturedHandler!(null!);

        Assert.True(called);
    }

    [Fact(DisplayName = "底层 ConnectedAsync 触发时应转发 Connected")]
    public async Task Connected_Event_Should_Be_Forwarded()
    {
        var mockClient = new Mock<IMqttClient>();
        Func<MqttClientConnectedEventArgs, Task>? capturedHandler = null;

        mockClient.SetupAdd(c => c.ConnectedAsync += It.IsAny<Func<MqttClientConnectedEventArgs, Task>>())
            .Callback<Func<MqttClientConnectedEventArgs, Task>>(h => capturedHandler = h);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        bool called = false;
        adapter.Connected += e => { called = true; return Task.CompletedTask; };

        Assert.NotNull(capturedHandler);
        await capturedHandler!(null!);

        Assert.True(called);
    }

    [Fact(DisplayName = "底层 DisconnectedAsync 触发时应转发 Disconnected")]
    public async Task Disconnected_Event_Should_Be_Forwarded()
    {
        var mockClient = new Mock<IMqttClient>();
        Func<MqttClientDisconnectedEventArgs, Task>? capturedHandler = null;

        mockClient.SetupAdd(c => c.DisconnectedAsync += It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>())
            .Callback<Func<MqttClientDisconnectedEventArgs, Task>>(h => capturedHandler = h);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        bool called = false;
        adapter.Disconnected += e => { called = true; return Task.CompletedTask; };

        Assert.NotNull(capturedHandler);
        await capturedHandler!(null!);

        Assert.True(called);
    }

    [Fact(DisplayName = "IsConnected 应反映底层客户端状态")]
    public void IsConnected_Should_Reflect_Client_IsConnected()
    {
        var mockClient = new Mock<IMqttClient>();
        mockClient.SetupGet(c => c.IsConnected).Returns(true);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        Assert.True(adapter.IsConnected);

        mockClient.SetupGet(c => c.IsConnected).Returns(false);
        Assert.False(adapter.IsConnected);
    }

    [Fact(DisplayName = "ConnectAsync 应调用底层 ConnectAsync")]
    public async Task ConnectAsync_Should_Call_Underlying_ConnectAsync()
    {
        var mockClient = new Mock<IMqttClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MqttClientConnectResult?)null)
            .Verifiable();

        var adapter = new MqttNetClientAdapter(mockClient.Object);
        // 必须指定通道，否则 Build() 会抛出 "A channel must be set."
        var options = new MqttClientOptionsBuilder()
            .WithClientId("test")
            .WithTcpServer("127.0.0.1") // <- 添加这一行
            .Build();
        await adapter.ConnectAsync(options, CancellationToken.None);

        mockClient.Verify(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PublishAsync 应调用底层 PublishAsync 并返回结果")]
    public async Task PublishAsync_Should_Call_Underlying_And_Return_Result()
    {
        var mockClient = new Mock<IMqttClient>();
        var expectedResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]); // 如果无默认构造则可返回 null 或替换为合适实例
        mockClient.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult)
            .Verifiable();

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("t")
            .WithPayload("p")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await adapter.PublishAsync(message, CancellationToken.None);

        Assert.Equal(expectedResult, result);
        mockClient.Verify(c => c.PublishAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "DisconnectAsync 应调用底层 DisconnectAsync")]
    public async Task DisconnectAsync_Should_Call_Underlying()
    {
        var mockClient = new Mock<IMqttClient>();
        mockClient.Setup(c => c.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        await adapter.DisconnectAsync(CancellationToken.None);

        mockClient.Verify(c => c.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SubscribeAsync 应使用 AtLeastOnce QoS 调用底层 SubscribeAsync")]
    public async Task SubscribeAsync_Should_Call_With_AtLeastOnce_QoS()
    {
        var mockClient = new Mock<IMqttClient>();
        string? capturedTopic = null;
        MqttQualityOfServiceLevel? capturedQos = null;

        // 这里针对实例方法 SubscribeAsync(MqttClientSubscribeOptions, CancellationToken) 做 Mock
        mockClient.Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .Callback<MqttClientSubscribeOptions, CancellationToken>((opts, ct) =>
            {
                var tf = opts?.TopicFilters?.FirstOrDefault();
                capturedTopic = tf?.Topic;
                capturedQos = tf?.QualityOfServiceLevel;
            })
            .ReturnsAsync((MqttClientSubscribeResult?)null);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        await adapter.SubscribeAsync("my/topic", CancellationToken.None);

        Assert.Equal("my/topic", capturedTopic);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, capturedQos);
    }

    [Fact(DisplayName = "UnsubscribeAsync 应调用底层 UnsubscribeAsync")]
    public async Task UnsubscribeAsync_Should_Call_Underlying()
    {
        var mockClient = new Mock<IMqttClient>();
        string? capturedTopic = null;

        // 针对实例方法 UnsubscribeAsync(MqttClientUnsubscribeOptions, CancellationToken) 做 Mock
        mockClient.Setup(c => c.UnsubscribeAsync(It.IsAny<MqttClientUnsubscribeOptions>(), It.IsAny<CancellationToken>()))
            .Callback<MqttClientUnsubscribeOptions, CancellationToken>((opts, ct) =>
            {
                // 不同版本的 MQTTnet 上 TopicFilters 的类型可能不同（IEnumerable<string> / IList<string>），用 FirstOrDefault 兼容读取
                capturedTopic = opts?.TopicFilters?.FirstOrDefault();
            })
            .ReturnsAsync((MqttClientUnsubscribeResult?)null)
            .Verifiable();

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        await adapter.UnsubscribeAsync("my/topic", CancellationToken.None);

        Assert.Equal("my/topic", capturedTopic);
        mockClient.Verify(c => c.UnsubscribeAsync(It.IsAny<MqttClientUnsubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "DisposeAsync 应调用底层 Dispose")]
    public async Task DisposeAsync_Should_Call_Client_Dispose()
    {
        var mockClient = new Mock<IMqttClient>();
        mockClient.Setup(c => c.Dispose()).Verifiable();

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        await adapter.DisposeAsync();

        mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact(DisplayName = "多个 MessageReceived 订阅者应全部被调用")]
    public async Task MessageReceived_MultipleHandlers_AllInvoked()
    {
        var mockClient = new Mock<IMqttClient>();
        Func<MqttApplicationMessageReceivedEventArgs, Task>? capturedHandler = null;
        mockClient.SetupAdd(c => c.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
            .Callback<Func<MqttApplicationMessageReceivedEventArgs, Task>>(h => capturedHandler = h);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        int called1 = 0, called2 = 0;
        adapter.MessageReceived += _ => { called1++; return Task.CompletedTask; };
        adapter.MessageReceived += _ => { called2++; return Task.CompletedTask; };

        Assert.NotNull(capturedHandler);
        await capturedHandler!(null!);

        Assert.Equal(1, called1);
        Assert.Equal(1, called2);
    }

    [Fact(DisplayName = "当底层 PublishAsync 抛异常时，适配器应传播该异常")]
    public async Task PublishAsync_WhenUnderlyingThrows_ExceptionPropagated()
    {
        var mockClient = new Mock<IMqttClient>();
        var ex = new System.InvalidOperationException("publish fail");
        mockClient.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var adapter = new MqttNetClientAdapter(mockClient.Object);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("t")
            .WithPayload("p")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var thrown = await Assert.ThrowsAsync<System.InvalidOperationException>(() => adapter.PublishAsync(message, CancellationToken.None));
        Assert.Same(ex, thrown);
    }
}
