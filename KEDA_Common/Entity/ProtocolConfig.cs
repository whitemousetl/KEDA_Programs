using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
[SugarIndex("idx_ProtocolConfig_SaveTime", nameof(SaveTime), OrderByType.Desc)]
public class ProtocolConfig//协议配置，把ProtocolEntity转换成json,表
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ConfigJson { get; set; } = string.Empty;
    public DateTime SaveTime { get; set; }
}
