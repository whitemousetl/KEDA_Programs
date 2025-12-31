using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Services;
public class WriteTaskLogService : IWriteTaskLogService
{
    private readonly SqlSugarClient _db;
    private readonly ILogger<WriteTaskLogService> _logger;

    public WriteTaskLogService(ILogger<WriteTaskLogService> logger, SqlSugarClient db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task AddLogAsync(WriteTaskLog log)
    {
        try
        {
            await _db.Insertable(log).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写任务日志插入失败");
        }
    }

    // 可扩展查询、统计等方法
}
