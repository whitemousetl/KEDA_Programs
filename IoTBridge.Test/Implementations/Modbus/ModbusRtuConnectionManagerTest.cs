using FluentAssertions;
using HslCommunication;
using HslCommunication.Core.Device.Fakes;
using HslCommunication.ModBus;
using HslCommunication.ModBus.Fakes;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Implementations.Modbus;
using KEDA_Share.Enums;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IoTBridge.Test.Implementations.Modbus;
[Collection("ModbusShimTests")]
public class ModbusRtuConnectionManagerTest
{
    [Fact]//1.	首次连接成功
    public void GetConnection_FirstCall_CreateAndOpenConnection()
    {
        using (ShimsContext.Create())
        {
            ShimDeviceSerialPort.AllInstances.Open = (instance) => new OperateResult { IsSuccess = true };

            // 拦截 DeviceSerialPort.Close
            ShimDeviceSerialPort.AllInstances.Close = (instance) => { };

            // 拦截 DeviceSerialPort.IsOpen
            ShimDeviceSerialPort.AllInstances.IsOpen = (instance) => false;

            var manager = new ModbusRtuConnectionManager();
            var param = new ModbusRtuParams(Operation.Read, "COM99", 9600, 8, StopBits.One, Parity.None,  []);
            var (conn, msg, isSuccess) = manager.GetConnection(param);

            conn.Should().NotBeNull();
            isSuccess.Should().BeTrue();
            msg.Should().BeNull();
            conn.PortName.Should().Be("COM99");
        }
    }

    [Fact]//2.	打开失败
    public async Task GetConnection_OpenFailed_ReturnsError()
    {
        using (ShimsContext.Create())
        {
            ShimDeviceSerialPort.AllInstances.Open = (instance) => new OperateResult { IsSuccess = false, Message = "Open failed" };
            ShimDeviceSerialPort.AllInstances.Close = (instance) => { };
            ShimDeviceSerialPort.AllInstances.IsOpen = (instance) => false;

            var manager = new ModbusRtuConnectionManager();
            var param = new ModbusRtuParams(Operation.Read, "COM99", 9600, 8, StopBits.One, Parity.None,  []);
            var (conn, msg, isSuccess) = manager.GetConnection(param);

            conn.Should().NotBeNull();
            isSuccess.Should().BeFalse();
            msg.Should().Be("Open failed");
        }
    }

    [Fact]//3.	参数变更时重连
    public async Task GetConnection_ParamsChanged_ShouldReconnect()
    {
        using (ShimsContext.Create())
        {
            int closeCount = 0;
            int openCount = 0;
            ShimDeviceSerialPort.AllInstances.Open = (instance) =>
            {
                openCount++;
                return new OperateResult { IsSuccess = true };
            };
            ShimDeviceSerialPort.AllInstances.Close = (instance) => { closeCount++; };
            ShimDeviceSerialPort.AllInstances.IsOpen = (instance) => false;

            var manager = new ModbusRtuConnectionManager();
            var param1 = new ModbusRtuParams(Operation.Read, "COM99", 9600, 8, StopBits.One, Parity.None,  []);
            var param2 = new ModbusRtuParams(Operation.Read, "COM100", 9600, 8, StopBits.One, Parity.None,  []);

            var res1 = manager.GetConnection(param1);
            var res2 = manager.GetConnection(param2);

            closeCount.Should().Be(1); // 参数变更时应关闭一次
            openCount.Should().Be(2);

            res1.conn.PortName.Should().Be("COM99");
            res1.isSuccess.Should().BeTrue();

            res2.conn.PortName.Should().Be("COM100");
            res2.isSuccess.Should().BeTrue();
        }
    }

    [Fact]//4.	已打开时复用连接
    public async Task GetConnection_AlreadyOpen_ShouldNotReconnect()
    {
        using (ShimsContext.Create())
        {
            int openCount = 0;
            int closeCount = 0;
            ShimDeviceSerialPort.AllInstances.Open = (instance) => { openCount++; return new OperateResult { IsSuccess = true }; };
            ShimDeviceSerialPort.AllInstances.Close = (instance) => { closeCount++; };
            ShimDeviceSerialPort.AllInstances.IsOpen = (instance) => true;

            var manager = new ModbusRtuConnectionManager();
            var param = new ModbusRtuParams(Operation.Read, "COM99", 9600, 8, StopBits.One, Parity.None,  []);

            var res1 = manager.GetConnection(param);
            var res2 = manager.GetConnection(param);

            openCount.Should().Be(1); // 已打开时不应再调用Open
            closeCount.Should().Be(0);

            res1.conn.PortName.Should().Be("COM99");
            res1.isSuccess.Should().BeTrue();

            res2.conn.PortName.Should().Be("COM99");
            res2.isSuccess.Should().BeTrue();
        }
    }

    [Fact]//5. 异常处理
    public async Task GetConnection_OpenThrowsException_ShouldReturnError()
    {
        using (ShimsContext.Create())
        {
            ShimDeviceSerialPort.AllInstances.Open = (instance) => throw new InvalidOperationException("Test Exception");
            ShimDeviceSerialPort.AllInstances.Close = (instance) => { };
            ShimDeviceSerialPort.AllInstances.IsOpen = (instance) => false;

            var manager = new ModbusRtuConnectionManager();
            var param = new ModbusRtuParams(Operation.Read, "COM99", 9600, 8, StopBits.One, Parity.None,  []);
            var (conn, msg, isSuccess) = manager.GetConnection(param);

            conn.Should().NotBeNull();
            isSuccess.Should().BeFalse();
            msg.Should().Be("Test Exception");
        }
    }
}
