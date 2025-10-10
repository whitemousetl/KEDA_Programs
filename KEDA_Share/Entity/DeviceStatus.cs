using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Entity;
public class DeviceStatus
{
    public string DeviceId { get; set; } = default!;
    public DevStatus Status { get; set; } = default!;     // 设备整体状态
    public string? UpdateTime { get; set; }
    public string Message {  get; set; } = default!;
    public List<PointStatus> PointStatuses { get; set; } = []; // 采集点状态列表
    /// <summary>
    /// 本次采集该设备耗费的时间（毫秒）
    /// </summary>
    public long? ElapsedMilliseconds { get; set; }
}

public class PointStatus
{
    public string Label { get; set; } = default!;      // 采集点唯一标识
    public PointReadResult Status { get; set; } = default!;     // 采集点状态
    public string? UpdateTime { get; set; }           // 状态更新时间
    public string Message { get; set; } = default!;

    /// <summary>
    /// 本次采集该点耗费的时间（毫秒）
    /// </summary>
    public long? ElapsedMilliseconds { get; set; }
}