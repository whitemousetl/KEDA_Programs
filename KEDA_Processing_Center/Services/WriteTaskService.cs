using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Processing_Center.Interfaces;
using SqlSugar;

namespace KEDA_Processing_Center.Services;
/// <summary>
/// 相同任务id直接返回
/// 查询所有未执行的写任务，3分钟之内如果设备id，Label，源值完全相同，则放弃本次任务，不存到数据库
/// </summary>
public class WriteTaskService : IWriteTaskService
{
    private readonly ILogger<WriteTaskService> _logger;
    private readonly SqlSugarClient _db;

    public WriteTaskService(ILogger<WriteTaskService> logger, SqlSugarClient db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IResult> HandleAsync(List<WritePointData> writePoints)
    {
        #region 校验，相同任务id直接返回
        foreach (var writePoint in writePoints)
        {
            if (writePoint == null)
            {
                var msg = "数据项为null";
                _logger.LogWarning(msg);
                return Results.Ok(ApiResponse<string>.Fial(msg));
            }
            if (string.IsNullOrWhiteSpace(writePoint.NodeId))
            {
                var msg = "数据NodeId为空";
                _logger.LogWarning(msg);
                return Results.Ok(ApiResponse<string>.Fial(msg));
            }
            if (string.IsNullOrWhiteSpace(writePoint.Value))
            {
                var msg = $"数据Value为空，NodeId={writePoint.NodeId}";
                _logger.LogWarning(msg);
                return Results.Ok(ApiResponse<string>.Fial(msg));
            }
            if (string.IsNullOrWhiteSpace(writePoint.TaskId))
            {
                var msg = $"数据TaskId为空，NodeId={writePoint.NodeId}";
                _logger.LogWarning(msg);
                return Results.Ok(ApiResponse<string>.Fial(msg));
            }
            else
            {
                if (await IsExistsWriteTaskByTaskIdAsync(writePoint.TaskId))
                {
                    var msg = $"taskid:[{writePoint.TaskId}] nodeid:[{writePoint.NodeId}] value:[{writePoint.Value}] tasktype:[{writePoint.TaskType}], 相同的任务已存在数据库，请检查";
                    _logger.LogWarning(msg);
                    return Results.Ok(ApiResponse<string>.Fial(msg));
                }
            }
            if (string.IsNullOrWhiteSpace(writePoint.TaskType))
            {
                var msg = $"数据TaskType为空，NodeId={writePoint.NodeId}";
                _logger.LogWarning(msg);
                return Results.Ok(ApiResponse<string>.Fial(msg));
            }
        } 
        #endregion

        try
        {
            List<WritePointEntity> points = [];
            foreach (var writePoint in writePoints)
            {
                var (DeviceId, Label) = ParseNodeId(writePoint.NodeId);

                if (string.IsNullOrWhiteSpace(DeviceId) || string.IsNullOrWhiteSpace(Label)) continue;

                if (Enum.TryParse(writePoint.TaskType, out TaskType taskType))
                {
                    var point = new WritePointEntity
                    {
                        TaskId = writePoint.TaskId,
                        TaskType = taskType,
                        NodeId = writePoint.NodeId,
                        OrigrinalValue = writePoint.Value,
                        DeviceId = DeviceId,
                        Label = Label
                    };
                    points.Add(point);
                }
            }

            // 查询所有未执行的写任务，3分钟之内如果设备id，Label，源值完全相同，则放弃本次任务，不存到数据库
            var notExecutedTasks = await GetNotExecutedWriteTasksAsync();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            foreach (var point in points)
            {
                var duplicate = notExecutedTasks.FirstOrDefault(t =>
                    t.DeviceId == point.DeviceId &&
                    t.Label == point.Label &&
                    t.OrigrinalValue == point.OrigrinalValue &&
                    (now - t.ReceivedTimestamp) < 180_000);

                if (duplicate != null)
                {
                    var msg = $"DeviceId:[{point.DeviceId}] Label:[{point.Label}] Value:[{point.OrigrinalValue}]，3分钟内已存在完全相同未执行任务，已放弃本次任务";
                    _logger.LogWarning(msg);
                    return Results.Ok(ApiResponse<string>.Fial(msg));
                }
            }

            var result = await SaveWriteTaskToSqliteAsync(points);

            if (result)
                return Results.Ok(ApiResponse<string>.Success("写任务已成功保存到数据库"));
            else
                return Results.Ok(ApiResponse<string>.Fial("写任务保存到数据库失败,请检查"));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<string>.FromException(ex));
        }
    }

    public async Task<bool> IsExistsWriteTaskByTaskIdAsync(string taskId)
    {
        return await _db.Queryable<WritePointEntity>()
            .AnyAsync(x => x.TaskId == taskId);
    }

    public async Task<List<WritePointEntity>> GetNotExecutedWriteTasksAsync()
    {
        return await _db.Queryable<WritePointEntity>()
            .Where(x => x.WriteTaskStatus == WriteTaskStatus.NotExecuted)
            .ToListAsync();
    }

    public async Task<bool> SaveWriteTaskToSqliteAsync(List<WritePointEntity> writePoints)
    {
        try
        {
            if (writePoints != null && writePoints.Count > 0)
            {
                foreach (var point in writePoints)
                {
                    if (point == null) continue;
                    if (point.TaskType == TaskType.Stop)
                    {
                        await _db.Updateable<WritePointEntity>()
                            .SetColumns(x => x.WriteTaskStatus == WriteTaskStatus.Done)
                            .SetColumns(x => x.OperatedTime == DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            .Where(x => x.DeviceId == point.DeviceId && x.Label == point.Label && x.WriteTaskStatus != WriteTaskStatus.Done)
                            .ExecuteCommandAsync();
                    }
                    else
                    {
                        await _db.Updateable<WritePointEntity>()
                            .SetColumns(x => x.WriteTaskStatus == WriteTaskStatus.Done)
                            .SetColumns(x => x.OperatedTime == DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            .Where(x => x.DeviceId == point.DeviceId && x.Label == point.Label && x.TaskType == point.TaskType && x.WriteTaskStatus != WriteTaskStatus.Done)
                            .ExecuteCommandAsync();
                    }
                }

                await _db.Insertable(writePoints).ExecuteCommandAsync();
                _logger.LogInformation("已批量保存 {Count} 条写任务到数据库 WriteTasks 表", writePoints.Count);
                return true;
            }
            else
            {
                _logger.LogWarning("写任务列表为空，未保存到数据库");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存写任务到Sqlite过程中发生了异常");
            return false;
        }
    }

    public static (string DeviceId, string Label) ParseNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return (string.Empty, string.Empty);

        var parts = nodeId.Split(';');
        if (parts.Length != 2)
            return (string.Empty, string.Empty);

        var sPart = parts[1];
        var eqIdx = sPart.IndexOf('=');
        if (eqIdx < 0 || eqIdx == sPart.Length - 1)
            return (string.Empty, string.Empty);

        var value = sPart[(eqIdx + 1)..];
        var underIdx = value.IndexOf('_');
        if (underIdx < 0)
            return (string.Empty, string.Empty);

        var deviceId = value[..underIdx];
        var label = value[(underIdx + 1)..];
        return (deviceId, label);
    }
}


//using KEDA_Common.Entity;
//using KEDA_Common.Model;
//using KEDA_Processing_Center.Interfaces;

//namespace KEDA_Processing_Center.Services;

//public class WriteTaskService : IWriteTaskService
//{
//    private readonly ILogger _logger;

//    public WriteTaskService(ILogger logger)
//    {
//        _logger = logger;
//    }

//    public async Task<IResult> HandleAsync(List<WritePointData> writePoints)
//    {
//        foreach (var writePoint in writePoints)
//        {
//            if (writePoint == null)
//            {
//                var msg = "数据项为null";
//                _logger.LogWarning(msg);
//                return Results.Ok(ApiResponse<string>.Fial(msg));
//            }
//            if (string.IsNullOrWhiteSpace(writePoint.NodeId))
//            {
//                var msg = "数据NodeId为空";
//                _logger.LogWarning(msg);
//                return Results.Ok(ApiResponse<string>.Fial(msg));
//            }
//            if (string.IsNullOrWhiteSpace(writePoint.Value))
//            {
//                var msg = $"数据Value为空，NodeId={writePoint.NodeId}";
//                _logger.LogWarning(msg);
//                return Results.Ok(ApiResponse<string>.Fial(msg));
//            }
//            if (string.IsNullOrWhiteSpace(writePoint.TaskId))
//            {
//                var msg = $"数据TaskId为空，NodeId={writePoint.NodeId}";
//                _logger.LogWarning(msg);
//                return Results.Ok(ApiResponse<string>.Fial(msg));
//            }
//            else
//            {
//                if (await _db.IsExistsWriteTaskByTaskIdAsync(_global.WriteTasksDb, writePoint.TaskId))
//                {
//                    var msg = $"taskid:[{writePoint.TaskId}] nodeid:[{writePoint.NodeId}] value:[{writePoint.Value}] tasktype:[{writePoint.TaskType}], 相同的任务已存在数据库，请检查";
//                    _logger.LogWarning(msg);
//                    return Results.Ok(ApiResponse<string>.Fial(msg));
//                }
//            }
//            if (string.IsNullOrWhiteSpace(writePoint.TaskType))
//            {
//                var msg = $"数据TaskType为空，NodeId={writePoint.NodeId}";
//                _logger.LogWarning(msg);
//                return Results.Ok(ApiResponse<string>.Fial(msg));
//            }
//        }

//        try
//        {
//            List<WritePointEntity> points = [];
//            foreach (var writePoint in writePoints)
//            {
//                var (DeviceId, Label) = ParseNodeId(writePoint.NodeId);

//                if (string.IsNullOrWhiteSpace(DeviceId) || string.IsNullOrWhiteSpace(Label)) continue;

//                if (Enum.TryParse(writePoint.TaskType, out TaskType taskType))
//                {
//                    var point = new WritePointEntity
//                    {
//                        TaskId = writePoint.TaskId,
//                        TaskType = taskType,
//                        NodeId = writePoint.NodeId,
//                        OrigrinalValue = writePoint.Value,
//                        DeviceId = DeviceId,
//                        Label = Label
//                    };
//                    points.Add(point);
//                }
//            }
//            #region 添加相同未执行任务两分钟间隔

//            //如果数据库中存在相同的任务，并且接收时间不超过2分钟，则放弃这次任务
//            // 查询所有未执行的写任务
//            var notExecutedTasks = await _db.GetNotExecutedWriteTasksAsync(_global.WriteTasksDb);
//            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

//            foreach (var point in points)
//            {
//                var duplicate = notExecutedTasks.FirstOrDefault(t =>
//                    t.DeviceId == point.DeviceId &&
//                    t.Label == point.Label &&
//                    t.OrigrinalValue == point.OrigrinalValue &&
//                    (now - t.ReceivedTimestamp) < 180_000);

//                if (duplicate != null)
//                {
//                    var msg = $"DeviceId:[{point.DeviceId}] Label:[{point.Label}] Value:[{point.OrigrinalValue}]，2分钟内已存在完全相同未执行任务，已放弃本次任务";
//                    _logger.LogWarning(msg);
//                    return Results.Ok(ApiResponse<string>.Fial(msg));
//                }
//            }
//            #endregion

//            var result = await _db.SaveWriteTaskToMongoDB(_global.WriteTasksDb, points);

//            if (result)
//                return Results.Ok(ApiResponse<string>.Success("写任务已成功保存到数据库"));
//            else
//                return Results.Ok(ApiResponse<string>.Fial("写任务保存到数据库失败,请检查"));
//        }
//        catch (Exception ex)
//        {
//            return Results.Ok(ApiResponse<string>.FromException(ex));
//        }
//    }


//    public static (string DeviceId, string Label) ParseNodeId(string nodeId)
//    {
//        if (string.IsNullOrWhiteSpace(nodeId))
//            return (string.Empty, string.Empty);

//        var parts = nodeId.Split(';');
//        if (parts.Length != 2)
//            return (string.Empty, string.Empty);

//        var sPart = parts[1];
//        var eqIdx = sPart.IndexOf('=');
//        if (eqIdx < 0 || eqIdx == sPart.Length - 1)
//            return (string.Empty, string.Empty);

//        var value = sPart[(eqIdx + 1)..];
//        var underIdx = value.IndexOf('_');
//        if (underIdx < 0)
//            return (string.Empty, string.Empty);

//        var deviceId = value[..underIdx];
//        var label = value[(underIdx + 1)..];
//        return (deviceId, label);
//    }
//}
