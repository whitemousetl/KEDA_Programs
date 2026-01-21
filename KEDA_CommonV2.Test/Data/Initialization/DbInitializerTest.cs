using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Data.Initialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Data.Initialization;

public class DbInitializerTest
{
    #region EnsureQuestDbTablesAsync - 参数校验测试
    [Fact]
    public async Task EnsureQuestDbTablesAsync_NullDbSettings_ThrowsArgumentNullException()
    {
        // Arrange
        DatabaseSettings? dbSettings = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureQuestDbTablesAsync_InvalidConnectionString_ThrowsInvalidOperationException(string? connectionString)
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = connectionString!,
            ConfigTableName = "ValidTable",
            WriteLogTableName = "ValidLogTable"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings));

        Assert.Contains("未配置数据库连接字符串", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureQuestDbTablesAsync_InvalidConfigTableName_ThrowsArgumentException(string? tableName)
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Database=qdb",
            ConfigTableName = tableName!,
            WriteLogTableName = "ValidLogTable"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings));

        Assert.Contains("表名不能为空", ex.Message);
        Assert.Equal(nameof(dbSettings.ConfigTableName), ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureQuestDbTablesAsync_InvalidWriteLogTableName_ThrowsArgumentException(string? tableName)
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Database=qdb",
            ConfigTableName = "ValidConfigTable",
            WriteLogTableName = tableName!
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings));

        Assert.Contains("表名不能为空", ex.Message);
        Assert.Equal(nameof(dbSettings.WriteLogTableName), ex.ParamName);
    }
    #endregion

    #region 表名安全校验测试（SQL注入防护）
    [Theory]
    [InlineData("table; DROP TABLE users;--")]
    [InlineData("table' OR '1'='1")]
    [InlineData("123InvalidStart")]
    [InlineData("table-name")]
    [InlineData("table.name")]
    [InlineData("table name")]
    [InlineData("表名")]
    public async Task EnsureQuestDbTablesAsync_ConfigTableNameWithIllegalCharacters_ThrowsArgumentException(string illegalTableName)
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Database=qdb",
            ConfigTableName = illegalTableName,
            WriteLogTableName = "ValidLogTable"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings));

        Assert.Contains("包含非法字符", ex.Message);
    }

    [Theory]
    [InlineData("table; DROP TABLE users;--")]
    [InlineData("table' OR '1'='1")]
    [InlineData("123InvalidStart")]
    public async Task EnsureQuestDbTablesAsync_WriteLogTableNameWithIllegalCharacters_ThrowsArgumentException(string illegalTableName)
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Database=qdb",
            ConfigTableName = "ValidConfigTable",
            WriteLogTableName = illegalTableName
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings));

        Assert.Contains("包含非法字符", ex.Message);
    }

    [Theory]
    [InlineData("ValidTable")]
    [InlineData("valid_table")]
    [InlineData("_privateTable")]
    [InlineData("Table123")]
    [InlineData("UPPER_CASE_TABLE")]
    [InlineData("mixedCase_Table_123")]
    public void ValidTableName_ShouldPassRegexValidation(string validTableName)
    {
        // Arrange - 使用反射测试私有正则
        var regexMethod = typeof(DbInitializer)
            .GetMethod("SafeTableNameRegex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var regex = (System.Text.RegularExpressions.Regex)regexMethod!.Invoke(null, null)!;

        // Act & Assert
        Assert.True(regex.IsMatch(validTableName), $"表名 '{validTableName}' 应该通过校验");
    }

    [Theory]
    [InlineData("123start")]
    [InlineData("has-dash")]
    [InlineData("has.dot")]
    [InlineData("has space")]
    [InlineData("has;semicolon")]
    [InlineData("")]
    public void InvalidTableName_ShouldFailRegexValidation(string invalidTableName)
    {
        // Arrange
        var regexMethod = typeof(DbInitializer)
            .GetMethod("SafeTableNameRegex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var regex = (System.Text.RegularExpressions.Regex)regexMethod!.Invoke(null, null)!;

        // Act & Assert
        Assert.False(regex.IsMatch(invalidTableName), $"表名 '{invalidTableName}' 不应通过校验");
    }
    #endregion

    #region CancellationToken 测试
    [Fact]
    public async Task EnsureQuestDbTablesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Database=qdb",
            ConfigTableName = "ConfigTable",
            WriteLogTableName = "WriteLogTable"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消

        // Act & Assert
        // 注意：由于连接失败可能先于取消检查，此测试可能抛出不同异常
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DbInitializer.EnsureQuestDbTablesAsync(dbSettings, cts.Token));
    }
    #endregion

    #region 集成测试（需要实际数据库连接）

    /// 集成测试：需要运行中的 QuestDB 实例
    /// 跳过条件：无法连接到数据库时自动跳过
    /// </summary>
    //[Fact(Skip = "集成测试 - 需要 QuestDB 实例运行")]
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EnsureQuestDbTablesAsync_WithValidSettings_CreatesTablesSuccessfully()
    {
        // Arrange
        var dbSettings = new DatabaseSettings
        {
            QuestDb = "Host=localhost;Port=8812;Username=admin;Password=quest",
            ConfigTableName = "WorkstationConfig",
            WriteLogTableName = "WriteTaskLog"
        };

        try
        {
            // Act
            await DbInitializer.EnsureQuestDbTablesAsync(dbSettings);

            // Assert - 再次调用不应抛出异常（幂等性）
            await DbInitializer.EnsureQuestDbTablesAsync(dbSettings);
        }
        finally
        {
            // Cleanup - 删除测试表
            // await CleanupTestTables(dbSettings);
        }
    }
    #endregion
}
