using KEDA_Share.Entity;
using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Models;
public class PointCollectTask
{
    public Protocol Protocol { get; set; } = default!;
    public Device Device { get; set; } = default!;
    public Point Point { get; set; } = default!;
    public object? Value { get; set; }
    public DataType DataType { get; set; }
}

