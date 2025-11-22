using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
[SugarIndex("idx_ProtocolData_SaveTime", nameof(SaveTime), OrderByType.Desc)]
public class ProtocolData//协议读取数据，持久化存储在数据库中，表
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string ProtocolID { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = [];
    public DateTime SaveTime { get; set; }
}
