using HslCommunication.Core;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Processing_Center.Interfaces;
using SqlSugar;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text.Json;

namespace KEDA_Processing_Center.Services;

public class WorkstationConfigService : IWorkstationConfigService
{
    private readonly IValidator<Workstation> _validator;
    private readonly ISqlSugarClientFactory _dbFactory;
    private readonly JsonSerializerOptions options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public WorkstationConfigService(IValidator<Workstation> validator, ISqlSugarClientFactory dbFactory)
    {
        _validator = validator;
        _dbFactory = dbFactory;
    }

    public async Task<IResult> HandleAsync(Workstation? ws)
    {
        var res = _validator.Validate(ws);

        if (!res.IsValid) return Results.Ok(ApiResponse<string>.Fial(res.ErrorMessage ?? "服务器无返回错误信息"));

        // 1. 转换为 ProtocolEntity 列表
        var protocolEntities = new ConcurrentBag<ProtocolEntity>();
        foreach (var proto in ws!.Protocols)
        {
            var protocolEntity = new ProtocolEntity
            {
                ProtocolID = proto.ProtocolID,
                Interface = Enum.TryParse<ProtocolInterface>(proto.Interface, out var iface) ? iface : ProtocolInterface.LAN,
                ProtocolType = Enum.TryParse<ProtocolType>(proto.ProtocolType, out var ptype) ? ptype : ProtocolType.Modbus,
                IPAddress = proto.IPAddress,
                Gateway = proto.Gateway,
                ProtocolPort = int.TryParse(proto.ProtocolPort, out var port) ? port : 502,
                PortName = proto.PortName,
                AddressStartWithZero = !bool.TryParse(proto.AddressStartWithZero, out var asz) || asz,
                InstrumentType = byte.TryParse(proto.InstrumentType, out var type) ? type : (byte)0,
                BaudRate = int.TryParse(proto.BaudRate, out var baud) ? baud : 9600,
                DataBits = int.TryParse(proto.DataBits, out var bits) ? bits : 8,
                StopBits = Enum.TryParse<StopBits>(proto.StopBits, out var stopBits) ? stopBits : StopBits.One,
                Parity = Enum.TryParse<Parity>(proto.Parity, out var parity) ? parity : Parity.None,
                Remark = proto.Remark,
                CollectCycle = int.TryParse(proto.CollectCycle, out var cycle) ? cycle : 50000,
                ReceiveTimeOut = int.TryParse(proto.ReceiveTimeOut, out var rto) ? rto : 5000,
                ConnectTimeOut = int.TryParse(proto.ConnectTimeOut, out var cto) ? cto : 5000,
                Devices = proto.Devices.Select(d => new DeviceEntity
                {
                    EquipmentId = d.EquipmentID,
                    Points = d.Points.Select(p => new PointEntity
                    {
                        Label = p.Label,
                        StationNo = d.StationNo,
                        DataType = Enum.TryParse<DataType>(p.DataType, out var dt) ? dt : DataType.String,
                        Address = p.Address,
                        Length = ushort.TryParse(p.Length, out var length) ? length : (ushort)0,
                        Format = Enum.TryParse<DataFormat>(proto.Format, out var df) ? df : DataFormat.CDAB,
                        Change = p.Change,
                    }).ToList()
                }).ToList()
            };

            protocolEntities.Add(protocolEntity);
        }

        // 统一时间
        var now = DateTime.Now;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);//毫秒

        // 2. 序列化为 JSON
        var protocolJson = JsonSerializer.Serialize(protocolEntities, options);
        var wsJson = JsonSerializer.Serialize(ws, options);

        // 3. 事务保存
        using var db = _dbFactory.CreateClient();
        var result = await db.Ado.UseTranAsync(async () =>
        {
            var wsConfig = new WorkstationConfig
            {
                ConfigJson = wsJson,
                SaveTime = now
            };
            await db.Insertable(wsConfig).ExecuteCommandAsync();

            var protocolConfig = new ProtocolConfig
            {
                ConfigJson = protocolJson,
                SaveTime = now
            };
            await db.Insertable(protocolConfig).ExecuteCommandAsync();
        });

        if (result.IsSuccess)
            return Results.Ok(ApiResponse<string>.Success($"保存 Workstation 成功，EdgeID: {ws!.EdgeName}"));
        else
            return Results.Ok(ApiResponse<string>.Fial($"保存失败：{result.ErrorMessage}"));
    }
}