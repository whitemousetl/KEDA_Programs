using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
[SugarTable("WriteTaskLog")]
[SugarIndex("idx_WriteTaskLog_Time", nameof(Time), OrderByType.Desc)]
public class WriteTaskLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string UUID { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string WriteTaskJson { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public bool IsSuccess { get; set; }
    public string Msg { get; set; } = string.Empty;
}
