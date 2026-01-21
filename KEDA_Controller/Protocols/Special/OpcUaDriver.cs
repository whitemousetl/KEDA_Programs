//using KEDA_Common.Entity;
//using KEDA_Common.Enums;
//using KEDA_Common.Interfaces;
//using KEDA_Common.Model;
//using KEDA_Controller.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace KEDA_Controller.Protocols.Special;
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
