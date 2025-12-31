using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Entity;
public class TotalEquipmentStatus
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string EquipmentId { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string EquipmentName { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(Length = 255)]
    public string Msg { get; set; } = string.Empty;

    [SugarColumn]
    public DateTime Time { get; set; }
}
