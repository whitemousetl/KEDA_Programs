using KEDA_Common.Entity;
using KEDA_Common.Helper;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Common.Services;
using KEDA_Common.Services.Validators;
using KEDA_Processing_Center.Extensions;
using KEDA_Processing_Center.Interfaces;
using KEDA_Processing_Center.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using SqlSugar;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace KEDA_Processing_Center;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args); //webapi建造者

        #region 身份认证
        //var jwtKey = "b7f8c2e1a9d4f6e3c5b8a1d2e4f7c9b0"; // 32字符
        //var tokenExpireMinutes = 30;

        //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //    .AddJwtBearer(options =>
        //    {
        //        options.TokenValidationParameters = new()
        //        {
        //            ValidateIssuer = false,
        //            ValidateAudience = false,
        //            ValidateLifetime = true,
        //            ValidateIssuerSigningKey = true,
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        //        };
        //    }); 
        #endregion

        builder.Services.AddAuthorization();

        #region 配置Serilog日志
        // 自动获取本机IP
        var localIp = GetLocalIp();
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithProperty("LocalIp", localIp);
        });
        #endregion

        #region 配置默认服务器Kestrel
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var kestrelConfig = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelConfig);
        });
        #endregion

        #region 配置并初始化MySql数据库
        var connectionString = builder.Configuration.GetConnectionString("WorkstationDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("未配置数据库连接字符串（WorkstationDb）。请检查 appsettings.json 或环境变量。");
        DbInitializer.EnsureDatabaseAndTables(connectionString, DbType.MySql);
        #endregion

        #region 依赖注入
        builder.Services.AddSingleton<ISqlSugarClientFactory, SqlSugarClientFactory>();
        builder.Services.AddSingleton<IValidator<Workstation>, WorkstationValidator>();
        builder.Services.AddSingleton<IValidator<Protocol>, ProtocolValidator>();
        builder.Services.AddSingleton<IValidator<Device>, DeviceValidator>();
        builder.Services.AddSingleton<IValidator<Point>, PointValidator>();
        builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>();
        builder.Services.AddSingleton<IMqttSubscribeService, MqttSubscribeService>();
        builder.Services.AddScoped<IDeviceNotificationService, DeviceNotificationService>();
        builder.Services.AddScoped<IWorkstationConfigService, WorkstationConfigService>();
        builder.Services.AddScoped<IProtocolConfigProvider, ProtocolConfigProvider>();
        builder.Services.AddHostedService<DataProcessingWorker>();
        #endregion

        var app = builder.Build();

        //app.UseAuthentication();
        app.UseAuthorization();

        #region 身份认证
        //// 登录接口
        //app.MapPost("/login", (string username, string password) =>
        //{
        //    if (username == "admin" && password == "123456")
        //    {
        //        var claims = new[]
        //        {
        //            new Claim(ClaimTypes.Name, username),
        //            new Claim("userid", "1001")
        //        };
        //        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        //        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        //            claims: claims,
        //            expires: DateTime.Now.AddMinutes(tokenExpireMinutes),
        //            signingCredentials: creds);

        //        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        //        return Results.Ok(new { token = tokenString });
        //    }
        //    return Results.Unauthorized();
        //});

        //// 刷新 Token 接口
        //app.MapPost("/refresh-token", (ClaimsPrincipal user) =>
        //{
        //    var username = user.Identity?.Name ?? "unknown";
        //    var userid = user.FindFirst("userid")?.Value ?? "unknown";
        //    var claims = new[]
        //    {
        //        new Claim(ClaimTypes.Name, username),
        //        new Claim("userid", userid)
        //    };
        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        //        claims: claims,
        //        expires: DateTime.Now.AddMinutes(tokenExpireMinutes),
        //        signingCredentials: creds);

        //    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        //    return Results.Ok(new { token = tokenString });
        //}).RequireAuthorization();

        //// 受保护接口
        //app.MapPost("/plc/write", (ClaimsPrincipal user, ILogger<Program> logger) =>
        //{
        //    var username = user.Identity?.Name ?? "unknown";
        //    var userid = user.FindFirst("userid")?.Value ?? "unknown";
        //    var path = "/plc/write";
        //    logger.LogInformation("用户 {User}({UserId}) 调用了接口 {Path}，时间：{Time}", username, userid, path, DateTimeOffset.Now);

        //    // 处理PLC写任务
        //    return Results.Ok("写入成功");
        //}).RequireAuthorization(); 
        #endregion

        //接口扩展方法
        //app.MapProcessingCenterApis();

        app.Run();
    }

    private static string GetLocalIp()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
            ?.ToString() ?? "unknown";
    }
}
