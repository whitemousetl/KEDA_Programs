using KEDA_Common.Entity;
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
using SqlSugar;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace KEDA_Processing_Center;

public class Program
{
    public static void Main(string[] args)
    {
        var projectNmae = Assembly.GetExecutingAssembly().GetName().Name;

        var builder = WebApplication.CreateBuilder(args);

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

        builder.Services.AddAuthorization();


        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", Serilog.Events.LogEventLevel.Fatal)
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "Logs", $"log-{projectNmae}-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        });

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var kestrelConfig = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelConfig);
        });

        #region 初始化Sqlite数据库
        var connectionString = builder.Configuration.GetConnectionString("WorkstationDb");

        // 只在应用启动时执行一次
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode;";
            var currentMode = cmd.ExecuteScalar()?.ToString();
            if (currentMode != "wal" && currentMode != "WAL")
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();
            }
        }

        // 表初始化
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        db.CodeFirst.InitTables<WorkstationConfig>();
        db.CodeFirst.InitTables<ProtocolConfig>(); 
        db.CodeFirst.InitTables<WritePointEntity>();
        db.CodeFirst.InitTables<ProtocolData>();
        #endregion

        //注入SqlSugarClient
        builder.Services.AddTransient(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("WorkstationDb") ?? "Data Source=workstation.db";
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connStr,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            });
        });

        builder.Services.AddScoped<IValidator<Workstation>, WorkstationValidator>();
        builder.Services.AddScoped<IValidator<Protocol>, ProtocolValidator>();
        builder.Services.AddScoped<IValidator<Device>, DeviceValidator>();
        builder.Services.AddScoped<IValidator<Point>, PointValidator>();

        //注入工作站配置服务
        builder.Services.AddScoped<IWorkstationConfigService, WorkstationConfigService>();

        //注入写入任务服务
        builder.Services.AddScoped<IWriteTaskService, WriteTaskService>();

        builder.Services.AddScoped<IProtocolConfigProvider, ProtocolConfigProvider>();
        builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>();
        builder.Services.AddSingleton<IMqttSubscribeService, MqttSubscribeService>();

        builder.Services.AddHostedService<DataProcessingWorker>();

        var app = builder.Build();

        //app.UseAuthentication();
        app.UseAuthorization();

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

        //扩展方法
        app.MapProcessingCenterApis();

        app.Run();
    }
}
