using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Services.Validators;
public class PointValidator : IValidator<Point>
{
    public ValidationResult Validate(Point? point)
    {
        var result = new ValidationResult { IsValid = true };

        if (point == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "[采集点]存在采集点对象为空，请检查";
            return result;
        }

        var requiredFileds = new List<(string value, string errorMsg)>
        {
            ( point.Label, "[采集点]存在采集点Label为空，请检查" ),
            ( point.DataType, $"[采集点]存在采集点DataType为空，请检查,Label是{point.Label}" ),
            ( point.Address, $"[采集点]存在采集点Address为空，请检查,Label是{point.Label}" ),
        };

        foreach (var (value, errorMsg) in requiredFileds)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.IsValid = false;
                result.ErrorMessage = errorMsg;
                return result;
            }
        }

        if (!Enum.TryParse<DataType>(point.DataType, out _) || int.TryParse(point.DataType, out _))
        {
            result.IsValid = false;
            result.ErrorMessage = $"[采集点]数据类型[{point.DataType}]暂未实现,请假查，Label是{point.Label}";
            return result;
        }

        return result;
    }
}
