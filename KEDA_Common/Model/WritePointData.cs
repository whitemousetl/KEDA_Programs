using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Model;
public class WritePointData//写入接口的参数
{
    public string NodeId { get; set; } = string.Empty;//设备id_Label

    public string Value { get; set; } = string.Empty;//写入值
    public string TaskId { get; set; } = string.Empty;//任务id
    public string TaskType { get; set; } = string.Empty;//任务类型
}