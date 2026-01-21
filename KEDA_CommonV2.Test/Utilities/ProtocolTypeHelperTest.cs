using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Utilities;

public class ProtocolTypeHelperTest
{
    #region 正常路径（Happy Path）

    public static IEnumerable<object[]> ValidLanPairs =>
    [
        [InterfaceType.LAN, ProtocolType.ModbusTcpNet],
        [InterfaceType.LAN, ProtocolType.ModbusRtuOverTcp],
        [InterfaceType.LAN, ProtocolType.OmronFinsNet],
        [InterfaceType.LAN, ProtocolType.OpcUa],
    ];

    public static IEnumerable<object[]> ValidComPairs =>
    [
        [InterfaceType.COM, ProtocolType.ModbusRtu],
        [InterfaceType.COM, ProtocolType.DLT6452007Serial],
        [InterfaceType.COM, ProtocolType.FxSerial],
    ];

    public static IEnumerable<object[]> ValidApiPairs =>
    [
        [InterfaceType.API, ProtocolType.Api],
    ];

    public static IEnumerable<object[]> ValidDatabasePairs =>
    [
        [InterfaceType.DATABASE, ProtocolType.MySQL],
    ];

    [Theory]
    [MemberData(nameof(ValidLanPairs))]
    [MemberData(nameof(ValidComPairs))]
    [MemberData(nameof(ValidApiPairs))]
    [MemberData(nameof(ValidDatabasePairs))]
    public void IsProtocolTypeValidForInterface_有效映射_返回True(InterfaceType interfaceType, ProtocolType protocolType)
    {
        Assert.True(ProtocolTypeHelper.IsProtocolTypeValidForInterface(interfaceType, protocolType));
    }

    #endregion

    #region 无效映射

    public static IEnumerable<object[]> InvalidPairs =>
    [
        [InterfaceType.COM, ProtocolType.ModbusTcpNet],
        [InterfaceType.LAN, ProtocolType.ModbusRtu],
        [InterfaceType.API, ProtocolType.MySQL],
        [InterfaceType.DATABASE, ProtocolType.Api],
        [InterfaceType.LAN, ProtocolType.Api],
        [InterfaceType.COM, ProtocolType.MySQL],
    ];

    [Theory]
    [MemberData(nameof(InvalidPairs))]
    public void IsProtocolTypeValidForInterface_无效映射_返回False(InterfaceType interfaceType, ProtocolType protocolType)
    {
        Assert.False(ProtocolTypeHelper.IsProtocolTypeValidForInterface(interfaceType, protocolType));
    }

    #endregion

    #region 边界情况

    [Fact]
    public void IsProtocolTypeValidForInterface_未定义枚举值_返回False()
    {
        // 强转未定义枚举值
        var invalidInterface = (InterfaceType)9999;
        var invalidProtocol = (ProtocolType)9999;

        Assert.False(ProtocolTypeHelper.IsProtocolTypeValidForInterface(invalidInterface, ProtocolType.ModbusTcpNet));
        Assert.False(ProtocolTypeHelper.IsProtocolTypeValidForInterface(InterfaceType.LAN, invalidProtocol));
        Assert.False(ProtocolTypeHelper.IsProtocolTypeValidForInterface(invalidInterface, invalidProtocol));
    }

    #endregion

    #region 全量校验（确保所有带标签的枚举都被正确识别）

    [Fact]
    public void IsProtocolTypeValidForInterface_所有带ProtocolInterfaceTypeAttribute的枚举_均能正确识别()
    {
        foreach (var field in typeof(ProtocolType).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<ProtocolInterfaceTypeAttribute>();
            if (attr == null)
                continue;

            var protocolType = (ProtocolType)field.GetValue(null)!;
            Assert.True(
                ProtocolTypeHelper.IsProtocolTypeValidForInterface(attr.InterfaceType, protocolType),
                $"{protocolType} 应映射到接口 {attr.InterfaceType}");
        }
    }

    #endregion
}
