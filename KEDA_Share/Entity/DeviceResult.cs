using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Entity;
public class DeviceResult
{
    public string? DevId {  get; set; }
    public List<PointResult>? PointResults { get; set; }
}

public class PointResult
{
    public string? Label { get; set; }
    public object? Result { get; set; }
    public DataType DataType { get; set; }
}