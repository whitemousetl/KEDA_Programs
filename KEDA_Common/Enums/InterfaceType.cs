using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Enums;
/// <summary>
/// 接口类型
/// </summary>
public enum InterfaceType
{
    /// <summary>
    /// LAN
    /// </summary>
    [Description("LAN")]
    LAN = 0,

    /// <summary>
    /// COM
    /// </summary>
    [Description("COM")]
    COM = 1,

    /// <summary>
    /// API
    /// </summary>
    [Description("API")]
    API = 2,

    /// <summary>
    /// DATABASE
    /// </summary>
    [Description("DATABASE")]
    DATABASE = 3,
}
