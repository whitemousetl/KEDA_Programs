using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model;

public class ProtocolResult//协议读取结果
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N"); // 唯一标识
    public string Time { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // 存储为字符串
    public string ProtocolId { get; set; } = string.Empty;//协议id
    public string ProtocolType { get; set; } = string.Empty; //协议类型
    public List<DeviceResult> DeviceResults { get; set; } = []; // 设备采集结果
    public bool ReadIsSuccess { get; set; } = false;//当次是否读取成功
    public string ErrorMsg { get; set; } = string.Empty; //错误信息
    public long ElapsedMs { get; set; } = 0;  //耗时，毫秒
    public int TotalDevices { get; set; } = 0;//总设备数
    public int SuccessDevices { get; set; } = 0;//成功设备数
    public int FailedDevices { get; set; } = 0;//失败设备数
    public int TotalPoints { get; set; } = 0;//总点数
    public int SuccessPoints { get; set; } = 0;//成功点数
    public int FailedPoints { get; set; } = 0;//失败点数
    public string StartTime { get; set; } = string.Empty;//开始时间
    public string EndTime { get; set; } = string.Empty;//结束时间
    public Dictionary<string, object> Metadata { get; set; } = []; // 额外元数据
}

public class DeviceResult // 设备读取结果
{
    public string EquipmentId { get; set; } = string.Empty; // 设备ID
    public string EquipmentName { get; set; } = string.Empty; // 设备名称
    public List<PointResult> PointResults { get; set; } = []; // 采集点结果
    public bool ReadIsSuccess { get; set; } = false;//当次是否读取成功
    public string ErrorMsg { get; set; } = string.Empty; //错误信息
    public long ElapsedMs { get; set; } = 0;  //耗时，毫秒
    public int TotalPoints { get; set; } = 0;//总点数
    public int SuccessPoints { get; set; } = 0;//成功点数
    public int FailedPoints { get; set; } = 0;//失败点数
    public string StartTime { get; set; } = string.Empty;//开始时间
    public string EndTime { get; set; } = string.Empty;//结束时间
    public Dictionary<string, object> Metadata { get; set; } = []; // 设备额外信息
}

public class PointResult//采集点读取结果
{
    public DataType DataType { get; set; } // 数据类型
    public string Label { get; set; } = string.Empty; //标签，采集点名称
    public string Address { get; set; } = string.Empty;//地址
    public object? Value { get; set; } = null;//结果
    public bool ReadIsSuccess { get; set; } = false;//当次是否读取成功
    public string ErrorMsg { get; set; } = string.Empty; //错误信息
    public long ElapsedMs { get; set; } = 0;  //耗时，毫秒
    public Dictionary<string, object> Metadata { get; set; } = []; // 点额外信息
}