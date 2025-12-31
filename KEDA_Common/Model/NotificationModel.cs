using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Model;
public class NotificationModel //工作站设备状态通知更新模型
{
    public string edge_id { get; set; } = string.Empty;
    public string edge_name { get; set; } = string.Empty;
    public string ip { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string msg { get; set; } = string.Empty;
    public List<DeviceStatus> items { get; set; } = [];
    public string desc { get; set; } = string.Empty;
    public string time { get; set; } = string.Empty;
}

public class DeviceStatus
{
    public string equipment_id { get; set; } = string.Empty;
    public string equipment_name { get; set; } = string.Empty;
    public string equipment_ip { get; set; } = string.Empty;
    public string equipment_status { get; set; } = string.Empty;
    public string msg { get; set; } = string.Empty;
    public string desc { get; set; } = string.Empty;
    public string dev_type { get; set; } = string.Empty;
    public string time { get; set; } = string.Empty;
}
