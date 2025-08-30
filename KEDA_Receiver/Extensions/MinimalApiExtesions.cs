using KEDA_Receiver.Services;
using KEDA_Share.Entity;
using MongoDB.Driver;

namespace KEDA_Receiver.Extensions;

public static class MinimalApiExtesions
{
    public static void MapReceiverApis(this WebApplication app)
    {
        #region 测试用
        app.MapGet("/test", () => new { message = "Hello Receiver!" });

        app.MapGet("/test-argument", () =>
        {
            throw new ArgumentException("参数错误示例");
        });

        app.MapGet("/test-invalid", () =>
        {
            throw new InvalidOperationException("操作无效示例");
        });

        app.MapGet("/test-unauthorized", () =>
        {
            throw new UnauthorizedAccessException("未授权示例");
        });

        app.MapGet("/test-unknown", () =>
        {
            throw new Exception("未知异常示例");
        });

        app.MapGet("/test-mongodb-exception", () =>
        {
            throw new MongoException("mongo异常示例");
        });
        #endregion

        app.MapPost("/send_collector_task", (WorkstationConfigService service, Workstation? ws) => service.HandleAsync(ws));

    }
}
