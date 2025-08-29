using KEDA_Receiver.Services;
using KEDA_Share.Entity;

namespace KEDA_Receiver.Extensions;

public static class MinimalApiExtesions
{
    public static void MapReceiverApis(this WebApplication app)
    {
        app.MapGet("/test", () => "Hello Receiver!");

        app.MapPost("/send_collector_task", (WorkstationConfigService service, Workstation? ws) => service.HandleAsync(ws));
    }
}
