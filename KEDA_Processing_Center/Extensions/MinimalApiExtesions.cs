using KEDA_Common.Model;
using KEDA_Processing_Center.Interfaces;
using KEDA_Processing_Center.Services;

namespace KEDA_Processing_Center.Extensions;

public static class MinimalApiExtesions
{
    public static void MapProcessingCenterApis(this WebApplication app)
    {
        #region 测试用

        app.MapGet("/test", () => new { message = "Hello Receiver!" });

        #endregion 测试用

        app.MapPost("/send_collector_task", (IWorkstationConfigService service, Workstation? ws) => service.HandleAsync(ws));
        app.MapPost("/WritePoints", (IWriteTaskService service, List<WritePointData> points) => service.HandleAsync(points));
    }
}