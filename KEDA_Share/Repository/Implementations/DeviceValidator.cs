using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Implementations;
public class DeviceValidator : IValidator<Device>
{
    private readonly IValidator<Point> _pointValidator;

    public DeviceValidator(IValidator<Point> pointValidator)
    {
        _pointValidator = pointValidator;
    }
    public ValidationResult Validate(Device? device)
    {
        var result = new ValidationResult() { IsValid = true };

        if (device == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "[设备]存在设备对象为空，请检查";
            return result;
        }

        var requiredFields = new List<(string value, string errorMsg)>
        {
            ( device.EquipmentID, "[设备]存在设备ID(EquipmentID)为空，请检查" ),
            ( device.EquipmentName, $"[设备]存在设备名称(EquipmentName)为空，请检查,设备id是{device.EquipmentID}" ),
        };

        foreach (var (value, errorMsg) in requiredFields)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.IsValid = false;
                result.ErrorMessage = errorMsg;
                return result;
            }
        }

        if (device.Points == null || device.Points.Count == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = $"[设备]测点列表(Points)为空，请检查,设备id是{device.EquipmentID}";
            return result;
        }

        foreach(var point in device.Points)
        {
            var pointValidateRes = _pointValidator.Validate(point);
            if (!pointValidateRes.IsValid) return pointValidateRes;
        }

        return result;
    }
}
