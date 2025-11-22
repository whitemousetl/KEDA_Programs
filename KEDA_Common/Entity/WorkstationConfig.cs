using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
[SugarIndex("idx_WorkstationConfig_SaveTime", nameof(SaveTime), OrderByType.Desc)]
public class WorkstationConfig//工作站配置，把工作站实体转换成json然后存在这个表中，表
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ConfigJson { get; set; } = string.Empty;
    public DateTime SaveTime { get; set; }
}

