using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Interfaces.IMqttServices;
using KEDA_CommonV2.Services.MqttServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System.Reflection;
using System.Text;

namespace KEDA_CommonV2.Test.Services;

public class MqttPublishServiceTest
{
    private static MqttClientOptions CreateOptions() =>
        new MqttClientOptionsBuilder()
            .WithClientId("unit-test-client")
            .WithTcpServer("127.0.0.1", 1883)
            .Build();

    [Theory(DisplayName = "PublishAsync: 无效 topic 应抛出 ArgumentException")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PublishAsync_InvalidTopic_Throws(string topic)
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var clientAdapter = new Mock<IMqttClientAdapter>();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.PublishAsync(topic!, "p", CancellationToken.None));
    }

    [Fact(DisplayName = "PublishAsync: topic 为 null 应抛出 ArgumentException")]
    public async Task PublishAsync_TopicIsNull_Throws()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var clientAdapter = new Mock<IMqttClientAdapter>();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.PublishAsync(null!, "p", CancellationToken.None));
    }

    [Theory(DisplayName = "PublishAsync: 无效 Payload 应抛出 ArgumentException")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PublishAsync_InvalidPayload_Throws(string payload)
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var clientAdapter = new Mock<IMqttClientAdapter>();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.PublishAsync("workstatoin/data/aa22", payload, CancellationToken.None));
    }

    [Fact(DisplayName = "PublishAsync: Payload 为 null 应抛出 ArgumentException")]
    public async Task PublishAsync_PayloadIsNull_Throws()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var clientAdapter = new Mock<IMqttClientAdapter>();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.PublishAsync("workstatoin/data/aa22", null!, CancellationToken.None));
    }

    [Fact(DisplayName = "PublishAsync: 已连接时应直接发布并返回成功")]
    public async Task PublishAsync_AlreadyConnected_PublishesSuccessfully()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = CreateOptions();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(true);

        MqttApplicationMessage? capturedMessage = null;
        var successResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]);

        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((m, ct) => capturedMessage = m)
            .ReturnsAsync(successResult);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        var ok = await svc.PublishAsync("topic/1", "payload", CancellationToken.None);

        Assert.True(ok);
        Assert.NotNull(capturedMessage);
        Assert.Equal("topic/1", capturedMessage!.Topic);
        Assert.Equal("payload", Encoding.UTF8.GetString(capturedMessage.Payload ?? Array.Empty<byte>()));
        clientAdapter.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PublishAsync: 已连接时发布返回非成功应返回 false")]
    public async Task PublishAsync_AlreadyConnected_PublishNonSuccess_ReturnsFalse()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        shared.SetupGet(s => s.MqttClientOptions).Returns(CreateOptions());

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(true);

        var failResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.TopicNameInvalid, "", [new MqttUserProperty("", "")]);
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        var ok = await svc.PublishAsync("topic/2", "p", CancellationToken.None);

        Assert.False(ok);
    }

    [Fact(DisplayName = "PublishAsync: 未连接时应调用 ConnectAsync 并发布")]
    public async Task PublishAsync_NotConnected_Should_Connect_Then_Publish()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = CreateOptions();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        var clientAdapter = new Mock<IMqttClientAdapter>();

        // property sequence: first call false, after connect returns true
        clientAdapter.SetupSequence(c => c.IsConnected)
            .Returns(false);

        // 当 ConnectAsync 被调用时，把 IsConnected 改为 true（确保后续 Publish 路径看到已连接）
        clientAdapter.Setup(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()))
            .Callback<MqttClientOptions, CancellationToken>((opt, ct) =>
            {
                clientAdapter.SetupGet(c => c.IsConnected).Returns(true);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var successResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]);
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult)
            .Verifiable();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        var ok = await svc.PublishAsync("topic/3", "p3", CancellationToken.None);

        Assert.True(ok);
        clientAdapter.Verify(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()), Times.Once);
        clientAdapter.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PublishAsync: 底层 Publish 抛异常时应捕获并返回 false")]
    public async Task PublishAsync_PublishThrows_ReturnsFalse()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        shared.SetupGet(s => s.MqttClientOptions).Returns(CreateOptions());

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(true);

        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("pub fail"));

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        var ok = await svc.PublishAsync("topic/4", "p", CancellationToken.None);

        Assert.False(ok);
    }

    [Fact(DisplayName = "PublishAsync: 已 Dispose 时应直接返回 false")]
    public async Task PublishAsync_AfterDispose_ReturnsFalse()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        shared.SetupGet(s => s.MqttClientOptions).Returns(CreateOptions());

        var clientAdapter = new Mock<IMqttClientAdapter>();
        // IsConnected false to avoid awaited disconnect in try
        clientAdapter.SetupGet(c => c.IsConnected).Returns(false);
        clientAdapter.Setup(c => c.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await svc.DisposeAsync();

        var ok = await svc.PublishAsync("topic/6", "p", CancellationToken.None);

        Assert.False(ok);
        // Dispose should call the (non-awaited) disconnect once in finally block
        clientAdapter.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "DisposeAsync: 如果已连接应调用 DisconnectAsync 两次（await + finally 非 await）")]
    public async Task DisposeAsync_WhenConnected_CallsDisconnectTwice()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        shared.SetupGet(s => s.MqttClientOptions).Returns(CreateOptions());

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(true);
        clientAdapter.Setup(c => c.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        await svc.DisposeAsync();

        // 第一次在 try 中 awaited，finally 中还会再调用一次（未 await）
        clientAdapter.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        // 再次 Dispose 不会重复调用（幂等）
        await svc.DisposeAsync();
        clientAdapter.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "PublishAsync: Connect 多次失败后应返回 false（覆盖未连接分支与 retry catch）")]
    public async Task PublishAsync_WhenConnectAlwaysFails_ReturnsFalse_And_Retries()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = new MqttClientOptionsBuilder().WithClientId("t").WithTcpServer("127.0.0.1").Build();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(false);

        // ConnectAsync 每次都抛异常，触发 retry 的 catch 分支
        clientAdapter.Setup(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connect fail"));

        // PublishAsync 不应该被调用因为连接始终失败
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("should not be called"));

        // 注入 maxRetries = 3，注入一个无等待的 delayFunc（立即完成）
        int maxRetries = 3;
        int delayCalls = 0;
        Func<TimeSpan, CancellationToken, Task> noWaitDelay = (ts, ct) =>
        {
            delayCalls++;
            return Task.CompletedTask;
        };

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object, maxRetries, noWaitDelay);

        var result = await svc.PublishAsync("topic/x", "payload", CancellationToken.None);

        Assert.False(result);
        // Connect 应被尝试 maxRetries 次
        clientAdapter.Verify(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
        // delayFunc 在前 (maxRetries - 1) 次异常后会被调用
        Assert.Equal(maxRetries - 1, delayCalls);
    }

    [Fact(DisplayName = "EnsureConnectedAsync: 当某次 Connect 抛出后 catch 分支应执行并等待（由注入 delay 覆盖）")]
    public async Task EnsureConnectedAsync_CatchBranch_Invoked_OnException()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = new MqttClientOptionsBuilder().WithClientId("t").WithTcpServer("127.0.0.1").Build();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(false);

        // 首次抛异常，第二次成功
        var seq = clientAdapter.SetupSequence(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()));
        seq.ThrowsAsync(new InvalidOperationException("first fail"));
        seq.Returns(Task.CompletedTask);

        bool delayCalled = false;
        Func<TimeSpan, CancellationToken, Task> recordDelay = (ts, ct) =>
        {
            delayCalled = true;
            return Task.CompletedTask;
        };

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object, maxRetries: 3, delayFunc: recordDelay);

        // PublishAsync 会尝试 EnsureConnectedAsync，并最终能够连接成功并发布
        var successResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]);
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var ok = await svc.PublishAsync("topic/y", "pl", CancellationToken.None);

        Assert.True(ok);
        Assert.True(delayCalled); // 确认 catch 分支里的等待逻辑被触发（记录）
        clientAdapter.Verify(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "PublishAsync: double-check 在锁内被其它线程设置为已连接时不应调用 Connect")]
    public async Task PublishAsync_DoubleCheck_NoConnect_When_BecomesConnectedInsideLock()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = CreateOptions();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        var clientAdapter = new Mock<IMqttClientAdapter>();

        // 模拟：第一次检查为 false，第二次检查（锁内）为 true => 不应调用 ConnectAsync
        clientAdapter.SetupSequence(c => c.IsConnected)
            .Returns(false) // outer check
            .Returns(true); // inner double-check

        // ConnectAsync 若被调用会失败测试（确保不会调用）
        clientAdapter.Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("should not be called"));

        var successResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]);
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        var ok = await svc.PublishAsync("topic/z", "payload", CancellationToken.None);

        Assert.True(ok);
        clientAdapter.Verify(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        clientAdapter.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PublishAsync: 如果 Connect 首次成功则不调用 delayFunc")]
    public async Task PublishAsync_NoDelayFunc_WhenConnectSucceeds_FirstAttempt()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        var options = CreateOptions();
        shared.SetupGet(s => s.MqttClientOptions).Returns(options);

        int delayCalls = 0;
        Func<TimeSpan, CancellationToken, Task> recordDelay = (ts, ct) =>
        {
            Interlocked.Increment(ref delayCalls);
            return Task.CompletedTask;
        };

        var clientAdapter = new Mock<IMqttClientAdapter>();
        clientAdapter.SetupGet(c => c.IsConnected).Returns(false);
        clientAdapter.Setup(c => c.ConnectAsync(options, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var successResult = new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, "", [new MqttUserProperty("", "")]);
        clientAdapter.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object, maxRetries: 3, delayFunc: recordDelay);

        var ok = await svc.PublishAsync("topic/no-delay", "payload", CancellationToken.None);

        Assert.True(ok);
        Assert.Equal(0, delayCalls);
    }

    [Fact(DisplayName = "PublishAsync: Publish 时 publishLock 已被 Dispose 应返回 false（覆盖 ObjectDisposedException catch 分支）")]
    public async Task PublishAsync_WhenPublishLockDisposed_ReturnsFalse()
    {
        var logger = Mock.Of<ILogger<MqttPublishService>>();
        var shared = new Mock<ISharedConfigHelper>();
        shared.SetupGet(s => s.MqttClientOptions).Returns(CreateOptions());

        var clientAdapter = new Mock<IMqttClientAdapter>();

        var svc = new MqttPublishService(logger, shared.Object, clientAdapter.Object);

        // 通过反射获取私有的 _publishLock 并 Dispose 它，保持 _isDisposed 为 false
        var field = typeof(MqttPublishService).GetField("_publishLock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var sem = (SemaphoreSlim)field.GetValue(svc)!;
        sem.Dispose(); // 直接释放 semaphore，触发 WaitAsync 抛 ObjectDisposedException

        var result = await svc.PublishAsync("topic/disposed", "payload", CancellationToken.None);

        Assert.False(result);
        // 确保 PublishAsync 在锁抛出后没有调用底层 Publish
        clientAdapter.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
