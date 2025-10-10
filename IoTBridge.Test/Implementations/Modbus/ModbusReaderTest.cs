using HslCommunication;
using HslCommunication.Core;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Implementations.Modbus;
using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTBridge.Test.Implementations.Modbus;
public class ModbusReaderTest
{
    //[Fact]
    //public async Task ReadPointAsync_Array_CallsReadFuncWithCorrectParameters()
    //{
    //    //Arrange
    //    var reader = new ModbusReader();
    //    var point = new ModbusReadPoint (1, true, DataFormat.ABCD, 1000, DataType.Bool, "100", null);
    //    var mockResult = new OperateResult<int[]>() { IsSuccess = true, ErrorCode = 1000, Content = [2] };

    //    Func<string, ushort, Task<OperateResult<int[]>>> readFunc =
    //        (address, length) =>
    //        {
    //            return Task.FromResult(mockResult);
    //        };

    //    await reader.ReadPointAsync(readFunc, point);
    //}
}
