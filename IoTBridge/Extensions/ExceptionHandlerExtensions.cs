using KEDA_Share.Model;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace IoTBridge.Extensions;

public static class ExceptionHandlerExtensions
{
    public static void UseGlobalExceptionHanlder(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;

                if (exception == null)
                {
                    var msg = $"未知错误(Path={context.Request.Path})";
                    Log.Error(msg);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsJsonAsync(ApiResponse<string>.FromException(msg));
                    return;
                }

                var statusCode = StatusCodes.Status200OK;
                var clientMessage = "服务器内部错误，请联系管理员";

                switch (exception)
                {
                    case ArgumentException:
                    case InvalidOperationException:
                        clientMessage = exception.Message;
                        break;

                    case UnauthorizedAccessException:
                        clientMessage = "未授权访问";
                        break;
                }

                var extraMessage = $"([{clientMessage}]Path={context.Request.Path})";

                // 🔥 这里把额外信息写入日志
                Log.Error(exception, "全局异常捕获 {Extra}", extraMessage);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(ApiResponse<string>.FromException(exception, extraMessage));
            });
        });
    }
}
