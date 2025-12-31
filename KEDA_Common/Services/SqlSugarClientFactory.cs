using KEDA_Common.Interfaces;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Services;
public class SqlSugarClientFactory : ISqlSugarClientFactory
{
    private readonly string _connectionString;

    public SqlSugarClientFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WorkstationDb")
            ?? throw new InvalidOperationException("未配置数据库连接字符串");
    }

    public ISqlSugarClient CreateClient()
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = _connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }
}