using KEDA_Common.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
[SugarIndex("idx_WritePointEntity_ReceivedTime", nameof(ReceivedTime), OrderByType.Desc)]
[SugarIndex("idx_WritePointEntity_OperatedTime", nameof(OperatedTime), OrderByType.Desc)]
[SugarIndex("idx_WritePointEntity_ReceivedTimestamp", nameof(ReceivedTimestamp), OrderByType.Desc)]
public class WritePointEntity//写入点实体，接口参数，表
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;//设备id
    public string Label { get; set; } = string.Empty;//参数名
    public string TaskId { get; set; } = string.Empty;//任务id
    public TaskType TaskType { get; set; } = TaskType.Other;//任务类型：100：启动；200：停止；300：其他  目前是球磨控制专用，区分球磨的启动或停止任务，启动是指发送第一段球磨频率，而不是真的启动
    public string NodeId { get; set; } = string.Empty;//节点id：设备id_Label
    public WriteTaskStatus WriteTaskStatus { get; set; } = WriteTaskStatus.NotExecuted;//任务状态：100：未执行；102：取消；103：错误；104：已完成
    public string OrigrinalValue { get; set; } = string.Empty;//源值，发送过来的值有可能是转换后的
    public string WritedValue { get; set; } = string.Empty;//写值，真正写入的值
    public string Message { get; set; } = string.Empty;//写入操作信息
    public string OperatedTime { get; set; } = string.Empty;//最后操作时间
    public string ReceivedTime { get; set; } = _now.ToString("yyyy-MM-dd HH:mm:ss.fff");//接收时间
    public long ReceivedTimestamp { get; set; } = _now.ToUnixTimeMilliseconds();//接收时间戳

    private static DateTimeOffset GetNow() => DateTimeOffset.Now;
    private static readonly DateTimeOffset _now = GetNow();
}
