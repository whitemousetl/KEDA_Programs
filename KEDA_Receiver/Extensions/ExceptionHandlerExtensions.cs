using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace KEDA_Receiver.Extensions;

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
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { Message = "未知错误" });
                    return;
                }

                var errorId = Guid.NewGuid().ToString("N");

                var statusCode = StatusCodes.Status500InternalServerError;
                var clientMessage = "服务器内部错误，请联系管理员";

                switch (exception)
                {
                    case ArgumentException:
                    case InvalidOperationException:
                        statusCode = StatusCodes.Status400BadRequest;
                        clientMessage = exception.Message; // 业务异常直接把 Message 给前端
                        break;

                    case UnauthorizedAccessException:
                        statusCode = StatusCodes.Status401Unauthorized;
                        clientMessage = "未授权访问";
                        break;
                }

                Log.Error(exception, $"请求处理异常 (ErrorId={errorId}, Path={context.Request.Path}, StatusCode={statusCode})");

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    ErrorId = errorId,     // 返回前端一个ID，方便查日志
                    StatusCode = statusCode,
                    Message = clientMessage,
                    Detail = exception is ArgumentException ? exception.Message : null // 只在业务异常时给前端细节
                });
            });
        });
    }
}
