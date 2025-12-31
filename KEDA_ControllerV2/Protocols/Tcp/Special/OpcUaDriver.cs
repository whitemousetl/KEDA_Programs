//using KEDA_CommonV2.Entity;
//using KEDA_CommonV2.Enums;
//using KEDA_CommonV2.Interfaces;
//using KEDA_CommonV2.Model;
//using KEDA_ControllerV2.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace KEDA_ControllerV2.Protocols.Special;
//[ProtocolType(ProtocolType.OPC)]
//[ProtocolType(ProtocolType.OPCUA)]
//public class OpcUaDriver : IProtocolDriver
//{
//    public Task<ProtocolResult?> ReadAsync(WorkstationEntity protocol, string devId, PointEntity point, CancellationToken token)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
//    {
//        throw new NotImplementedException();
//    }

//    public void Dispose()
//    {
//        throw new NotImplementedException();
//    }

//    public string GetProtocolName() => "OpcUa";
//}