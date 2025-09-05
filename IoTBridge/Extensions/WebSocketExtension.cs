using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using IoTBridge.Models.ProtocolParams;
using HslCommunication.ModBus;
using System.Data.Common;
using KEDA_Share.Enums;
using HslCommunication;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using IoTBridge.Models.ProtocolResponses;
using Newtonsoft.Json;

namespace IoTBridge.Extensions;

public static class WebSocketExtension
{
    public static void MapIotBridgeWebSockets(this WebApplication app)
    {
        app.Map("/ws/modbusrtu/{serialPort}", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var buffer = new byte[1024 * 4];

                // 注入新的处理服务
                var handler = context.RequestServices.GetRequiredService<IModbusRtuWebSocketHandler>();

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close by client", CancellationToken.None);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = await handler.HandleRequestAsync(json);

                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await webSocket.SendAsync(responseBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });
    }
}