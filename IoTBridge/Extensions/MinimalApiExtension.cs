namespace IoTBridge.Extensions;

public static class MinimalApiExtension
{
    public static void MapIotBridgeApis(this WebApplication app)
    {
        app.MapGet("/test", () => new { message = "Hello IotBridge!" });
    }
}