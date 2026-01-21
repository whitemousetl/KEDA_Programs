using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Utilities;

public class SystemMsgTest
{
    #region GetLocalIp

    [Fact]
    public void GetLocalIp_正常环境_返回有效IPv4地址或unknown()
    {
        // Act
        var result = SystemMsg.GetLocalIp();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));

        if (result != "unknown")
        {
            // 验证返回的是有效的 IPv4 地址
            Assert.True(IPAddress.TryParse(result, out var ip));
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);
            Assert.False(IPAddress.IsLoopback(ip));
        }
    }

    [Fact]
    public void GetLocalIp_返回值不是回环地址()
    {
        // Act
        var result = SystemMsg.GetLocalIp();

        // Assert
        if (result != "unknown" && IPAddress.TryParse(result, out var ip))
        {
            Assert.False(IPAddress.IsLoopback(ip), "返回的IP不应该是回环地址");
        }
    }

    [Fact]
    public void GetLocalIp_返回值是IPv4而非IPv6()
    {
        // Act
        var result = SystemMsg.GetLocalIp();

        // Assert
        if (result != "unknown" && IPAddress.TryParse(result, out var ip))
        {
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);
        }
    }

    [Fact]
    public void GetLocalIp_多次调用_结果一致()
    {
        // Act
        var result1 = SystemMsg.GetLocalIp();
        var result2 = SystemMsg.GetLocalIp();

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetLocalIp_返回值格式正确()
    {
        // Act
        var result = SystemMsg.GetLocalIp();

        // Assert
        if (result != "unknown")
        {
            // IPv4 格式: x.x.x.x，每段 0-255
            var parts = result.Split('.');
            Assert.Equal(4, parts.Length);

            foreach (var part in parts)
            {
                Assert.True(int.TryParse(part, out var num));
                Assert.InRange(num, 0, 255);
            }
        }
    }

    #endregion
}
