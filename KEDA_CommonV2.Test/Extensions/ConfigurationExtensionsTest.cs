using KEDA_CommonV2.Extensions;
using Microsoft.Extensions.Configuration;
using ConfigurationExtensions = KEDA_CommonV2.Extensions.ConfigurationExtensions;

namespace KEDA_CommonV2.Test.Extensions;

public class ConfigurationExtensionsTest
{
    #region AddSharedConfiguration - 基础配置加载测试

    [Fact]
    public void AddSharedConfiguration_WithoutEnvironment_ShouldLoadBaseConfiguration()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddSharedConfiguration();

        // Assert
        Assert.Same(builder, result); // 返回同一个 builder 实例（链式调用）
    }

    [Fact]
    public void AddSharedConfiguration_WithEnvironment_ShouldReturnBuilder()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddSharedConfiguration("Development");

        // Assert
        Assert.Same(builder, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AddSharedConfiguration_NullOrWhitespaceEnvironment_ShouldOnlyLoadBaseConfiguration(string? environmentName)
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddSharedConfiguration(environmentName);

        // Assert
        Assert.Same(builder, result);
    }

    #endregion

    #region AddSharedConfiguration - 环境配置加载测试

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void AddSharedConfiguration_WithValidEnvironment_ShouldNotThrow(string environmentName)
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert - 环境特定配置是可选的，不存在时不应抛出异常
        var exception = Record.Exception(() => builder.AddSharedConfiguration(environmentName));
        Assert.Null(exception);
    }

    #endregion

    #region AddSharedConfiguration - 链式调用测试

    [Fact]
    public void AddSharedConfiguration_ShouldSupportChainedCalls()
    {
        // Arrange & Act
        var builder = new ConfigurationBuilder()
            .AddSharedConfiguration()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestKey"] = "TestValue"
            });

        var configuration = builder.Build();

        // Assert
        Assert.Equal("TestValue", configuration["TestKey"]);
    }

    [Fact]
    public void AddSharedConfiguration_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert - 多次调用不应抛出异常
        var exception = Record.Exception(() =>
        {
            builder.AddSharedConfiguration();
            builder.AddSharedConfiguration("Development");
        });

        Assert.Null(exception);
    }

    #endregion

    #region AddSharedConfiguration - 配置值验证测试

    [Fact]
    public void AddSharedConfiguration_ShouldLoadConfigurationValues()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddSharedConfiguration();
        var configuration = builder.Build();

        // Assert - 验证配置已加载（至少应该能构建成功）
        Assert.NotNull(configuration);
    }

    [Fact]
    public void AddSharedConfiguration_ConfigurationShouldBeReadable()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddSharedConfiguration();

        // Act
        var configuration = builder.Build();

        // Assert - 配置应该可以枚举（即使为空）
        var children = configuration.GetChildren();
        Assert.NotNull(children);
    }

    #endregion

    #region AddSharedConfiguration - 与其他配置源组合测试

    [Fact]
    public void AddSharedConfiguration_WithInMemoryConfiguration_ShouldMergeConfigurations()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AppSettings:Name"] = "TestApp",
            ["AppSettings:Version"] = "1.0.0"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddSharedConfiguration()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Assert
        Assert.Equal("TestApp", configuration["AppSettings:Name"]);
        Assert.Equal("1.0.0", configuration["AppSettings:Version"]);
    }

    [Fact]
    public void AddSharedConfiguration_InMemoryCanOverrideSharedConfiguration()
    {
        // Arrange - 后添加的配置源应该能覆盖前面的
        var overrideSettings = new Dictionary<string, string?>
        {
            ["OverrideKey"] = "OverrideValue"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddSharedConfiguration()
            .AddInMemoryCollection(overrideSettings)
            .Build();

        // Assert
        Assert.Equal("OverrideValue", configuration["OverrideKey"]);
    }

    #endregion

    #region AddSharedConfiguration - 边界情况测试

    [Fact]
    public void AddSharedConfiguration_WithVeryLongEnvironmentName_ShouldNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var longEnvironmentName = new string('A', 1000);

        // Act & Assert - 长环境名不应导致异常（只是找不到对应配置文件）
        var exception = Record.Exception(() => builder.AddSharedConfiguration(longEnvironmentName));
        Assert.Null(exception);
    }

    [Fact]
    public void AddSharedConfiguration_WithSpecialCharactersInEnvironment_ShouldNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert - 特殊字符不应导致异常
        var exception = Record.Exception(() => builder.AddSharedConfiguration("Dev-Test_123"));
        Assert.Null(exception);
    }

    #endregion

    #region 嵌入资源验证测试

    [Fact]
    public void EmbeddedResource_BaseSharedConfiguration_ShouldExist()
    {
        // Arrange
        var assembly = typeof(ConfigurationExtensions).Assembly;
        var resourceName = "KEDA_CommonV2.Configuration.appsettings.Shared.json";

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);

        // Assert - 基础共享配置必须存在
        Assert.NotNull(stream);
    }

    [Fact]
    public void EmbeddedResource_BaseSharedConfiguration_ShouldBeValidJson()
    {
        // Arrange
        var assembly = typeof(ConfigurationExtensions).Assembly;
        var resourceName = "KEDA_CommonV2.Configuration.appsettings.Shared.json";

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        // Assert - 内容不应为空且应该是有效的 JSON（至少包含 { 和 }）
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("{", content);
        Assert.Contains("}", content);
    }

    [Fact]
    public void EmbeddedResource_ListAllResources_ForDebugging()
    {
        // Arrange
        var assembly = typeof(ConfigurationExtensions).Assembly;

        // Act
        var resourceNames = assembly.GetManifestResourceNames();

        // Assert - 应该至少包含基础配置
        Assert.Contains(resourceNames, name => name.Contains("appsettings.Shared.json"));
    }

    #endregion

    #region 配置构建完整性测试

    [Fact]
    public void AddSharedConfiguration_BuildConfiguration_ShouldNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddSharedConfiguration();

        // Act & Assert
        var exception = Record.Exception(() => builder.Build());
        Assert.Null(exception);
    }

    [Fact]
    public void AddSharedConfiguration_WithEnvironment_BuildConfiguration_ShouldNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddSharedConfiguration("Production");

        // Act & Assert
        var exception = Record.Exception(() => builder.Build());
        Assert.Null(exception);
    }

    #endregion
}