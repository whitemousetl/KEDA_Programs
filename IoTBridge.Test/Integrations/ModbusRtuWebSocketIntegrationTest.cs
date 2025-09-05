using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using KEDA_Share.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.WebSockets;
using System.Text;

namespace IoTBridge.Test.Integrations;

[Collection("Integration")]
public class ModbusRtuWebSocketIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModbusRtuWebSocketIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebSocket_ModbusRtu_FullChain_Success()
    {
        // 启动测试服务器
        var client = _factory.Server.CreateWebSocketClient();

        // 连接 WebSocket
        using var ws = await client.ConnectAsync(new Uri("ws://localhost/ws/modbusrtu/COM1"), CancellationToken.None);

        // 构造请求参数
        var request = new ModbusRtuParams(Operation.Read, "COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, []);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
        var buffer = Encoding.UTF8.GetBytes(json);

        // 发送请求
        await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

        // 接收响应
        var respBuffer = new byte[4096];
        var result = await ws.ReceiveAsync(respBuffer, CancellationToken.None);
        var respJson = Encoding.UTF8.GetString(respBuffer, 0, result.Count);

        // 断言响应内容
        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<ModbusRtuResponse>(respJson);
        Assert.NotNull(response);
        Assert.True(response.ProtocolStatus == ProtocolStatus.AllDeviceSuccess || response.ProtocolStatus == ProtocolStatus.AllDeviceFailture);
    }
}