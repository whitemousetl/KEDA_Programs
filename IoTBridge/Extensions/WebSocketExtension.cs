using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Implementations.Modbus;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;

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

                var scheduler = context.RequestServices.GetRequiredService<IModbusRtuScheduler>();
                var queue = context.RequestServices.GetRequiredService<IModbusQueue>();

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close by client", CancellationToken.None);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var config = JsonConvert.DeserializeObject<ModbusRtuConfig>(json);

                    if (config.Operation == Operation.Read)
                    {
                        foreach (var dev in config.Devices)
                        {
                            queue.EnqueueRead(dev.ReadPoints);
                        }
                    }
                    else if (config.Operation == Operation.Write)
                    {
                        foreach (var dev in config.Devices)
                        {
                            queue.EnqueueWrite(dev.WritePoints);
                        }
                    }
                    else
                    {

                    }

                    scheduler.InitProvider(config.PortConfig);
                    await scheduler.ScheduleAsync();

                    
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });
    }
}