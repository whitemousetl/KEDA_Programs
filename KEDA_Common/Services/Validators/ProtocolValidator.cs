using HslCommunication.Core;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using System.IO.Ports;
using System.Net;

namespace KEDA_Common.Services.Validators;
public class ProtocolValidator : IValidator<Protocol>
{
    private readonly int[] _validBaudRates =
   [
       110, 300, 600, 1200, 2400, 4800, 9600,
        14400, 19200, 38400, 57600, 115200,
        128000, 256000
    ];

    private readonly IValidator<Device> _deviceValidator;

    public ProtocolValidator(IValidator<Device> deviceValidator)
    {
        _deviceValidator = deviceValidator;
    }

    public ValidationResult Validate(Protocol? protocol)
    {
        var result = new ValidationResult() { IsValid = true };

        if (protocol == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "[协议]协议为空，请检查";
            return result;
        }

        var requiredFields = new List<(string value, string errorMsg)>
        {
            (protocol.ProtocolID, "[协议]存在id为空的协议，请检查"),
            (protocol.Interface, "[协议]存在接口类型Interface为空的协议，请检查"),
            (protocol.ProtocolType, "[协议]存在协议类型ProtocolType为空的协议，请检查"),
            (protocol.CollectCycle, "[协议]存在通讯延时CollectCycle为空的协议，请检查"),
            (protocol.ReceiveTimeOut, "[协议]存在接收超时ReceiveTimeOut为空的协议，请检查"),
            (protocol.ConnectTimeOut, "[协议]存在连接超时ConnectTimeOut为空的协议，请检查")
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

        if (!Enum.TryParse<ProtocolType>(protocol.ProtocolType, false, out _))
        {
            result.IsValid = false;
            result.ErrorMessage = $"[协议]该协议{protocol.ProtocolType}暂未完成，请检查";
            return result;
        }

        var res = protocol.Interface switch
        {
            "LAN" => ValidateLan(protocol),
            "COM" => ValidateCom(protocol),
            _ => new ValidationResult { IsValid = false, ErrorMessage = "接口类型是不支持的类型" }
        };

        if (!res.IsValid) return res;

        if (!string.IsNullOrWhiteSpace(protocol.ProtocolType) && protocol.ProtocolType.Contains("Modbus", StringComparison.OrdinalIgnoreCase))
        {
            var validateModbusRes = ValidateModbus(protocol);
            if (!validateModbusRes.IsValid) return validateModbusRes;
        }

        if (protocol.Devices == null || protocol.Devices.Count == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = "[协议]设备列表(Devices)为空，请检查";
            return result;
        }

        foreach (var dev in protocol.Devices)
        {
            if (!string.IsNullOrWhiteSpace(protocol.ProtocolType) &&
                (protocol.ProtocolType.Contains("Modbus", StringComparison.OrdinalIgnoreCase) ||
                protocol.ProtocolType.Contains("DLT6452007", StringComparison.OrdinalIgnoreCase) ||
                protocol.ProtocolType.Contains("CJT188OverTcp", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrWhiteSpace(dev.StationNo))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"[协议]{dev.EquipmentID}的站号StationNo为空，设备名称是{dev.EquipmentName}";
                    return result;
                }
            }

            var validateDeviceRes = _deviceValidator.Validate(dev);
            if (!validateDeviceRes.IsValid) return validateDeviceRes;
        }

        return result;
    }

    private static ValidationResult ValidateModbus(Protocol protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol.Format))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[Modbus系列]{protocol.ProtocolType}的Format为空" };

        if (!Enum.TryParse<DataFormat>(protocol.Format, out _) || int.TryParse(protocol.Format, out _))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[Modbus系列]{protocol.ProtocolType}的Format格式不正确" };

        if (string.IsNullOrWhiteSpace(protocol.AddressStartWithZero))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[Modbus系列]{protocol.ProtocolType}的AddressStartWithZero为空" };

        if (!bool.TryParse(protocol.AddressStartWithZero, out _))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[Modbus系列]{protocol.ProtocolType}的AddressStartWithZero格式不正确" };
        return new ValidationResult { IsValid = true };
    }

    private static ValidationResult ValidateLan(Protocol protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol.IPAddress))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[网口协议]{protocol.ProtocolType}的ip地址IPAddress为空，请检查" };

        if (!IPAddress.TryParse(protocol.IPAddress, out _))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[网口协议]{protocol.ProtocolType}的ip地址IPAddress格式不正确，请检查" };

        if (string.IsNullOrWhiteSpace(protocol.ProtocolPort))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[网口协议]{protocol.ProtocolType}的端口号ProtocolPort为空，请检查" };

        if (!uint.TryParse(protocol.ProtocolPort, out var port) || port > 65535 || port < 1)
            return new ValidationResult { IsValid = false, ErrorMessage = $"[网口协议]{protocol.ProtocolType}的端口号ProtocolPort格式不正确，请检查" };

        return new ValidationResult { IsValid = true };
    }

    private ValidationResult ValidateCom(Protocol protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol.PortName))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的串口号PortName为空，请检查" };

        if (string.IsNullOrWhiteSpace(protocol.BaudRate))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的波特率BaudRate为空，请检查" };

        if (!int.TryParse(protocol.BaudRate, out int baudRate) || !_validBaudRates.Contains(baudRate))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的波特率BaudRate格式不正确，请检查" };

        if (string.IsNullOrWhiteSpace(protocol.DataBits))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的数据位DataBits为空，请检查" };

        if (!int.TryParse(protocol.DataBits, out int dataBits) || dataBits < 5 || dataBits > 8)
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的数据位DataBits格式不正确，请检查" };

        if (string.IsNullOrWhiteSpace(protocol.StopBits))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的停止位StopBits为空，请检查" };

        if (!Enum.TryParse<StopBits>(protocol.StopBits, out _) || int.TryParse(protocol.StopBits, out _))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的停止位StopBits格式不正确，请检查" };

        if (string.IsNullOrWhiteSpace(protocol.Parity))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的校验位Parity为空，请检查" };

        if (Enum.TryParse<Parity>(protocol.Parity, out _) || int.TryParse(protocol.Parity, out _))
            return new ValidationResult { IsValid = false, ErrorMessage = $"[串口协议]{protocol.ProtocolType}的校验位Parity格式不正确，请检查" };

        return new ValidationResult { IsValid = true };
    }
}