using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace KEDA_CommonV2.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// 从 KEDA_CommonV2 类库中加载共享配置
    /// </summary>
    /// <param name="builder">配置构建器</param>
    /// <param name="environmentName">环境名称（可选），如 Development、Production</param>
    /// <returns></returns>
    public static IConfigurationBuilder AddSharedConfiguration(
        this IConfigurationBuilder builder,
        string? environmentName = null)
    {
        // 获取 KEDA_CommonV2 程序集
        var assembly = typeof(ConfigurationExtensions).Assembly;

        // 加载基础共享配置
        var baseResourceName = "KEDA_CommonV2.Configuration.appsettings.Shared.json";
        LoadEmbeddedConfiguration(builder, assembly, baseResourceName, "基础共享配置");

        // 如果指定了环境，尝试加载环境特定配置
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            var envResourceName = $"KEDA_CommonV2.Configuration.appsettings.Shared.{environmentName}. json";
            LoadEmbeddedConfiguration(builder, assembly, envResourceName, $"{environmentName} 环境共享配置", optional: true);
        }

        return builder;
    }

    private static void LoadEmbeddedConfiguration(
        IConfigurationBuilder builder,
        Assembly assembly,
        string resourceName,
        string description,
        bool optional = false)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            if (!optional)
            {
                throw new FileNotFoundException(
                    $"无法找到嵌入资源:  {resourceName}。" +
                    $"请确保文件存在并在 . csproj 中配置为 EmbeddedResource。");
            }
            return;
        }

        // 需要创建一个新的 MemoryStream，因为嵌入资源流可能不支持 Seek
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        builder.AddJsonStream(memoryStream);
        Console.WriteLine($"✓ 已加载 {description}:  {resourceName}");
    }
}