using KEDA_Common.Entity;
using MySqlConnector;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Helper;
public static class DbInitializer
{
    /// <summary>
    /// 初始化数据库表（CodeFirst 自动建表）
    /// </summary>
    public static void EnsureDatabaseAndTables(string connectionString, DbType dbType = DbType.MySql)
    {
        // 1. 解析数据库名
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;

        // 2. 构造不带 Database 的连接字符串，连接到 mysql 系统库
        builder.Database = "mysql";
        var sysConnStr = builder.ToString();

        // 3. 创建数据库（如果不存在）
        using (var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = sysConnStr,
            DbType = dbType,
            IsAutoCloseConnection = true
        }))
        {
            db.Ado.ExecuteCommand($"CREATE DATABASE IF NOT EXISTS `{dbName}` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;");
        }

        // 4. 用目标库连接，建表
        using (var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true
        }))
        {
            db.CodeFirst.InitTables<WorkstationConfig>();
            db.CodeFirst.InitTables<ProtocolConfig>();
            db.CodeFirst.InitTables<ProtocolData>();
            db.CodeFirst.InitTables<TotalEquipmentStatus>();
            db.CodeFirst.InitTables<WriteTaskLog>();
        }
    }
}