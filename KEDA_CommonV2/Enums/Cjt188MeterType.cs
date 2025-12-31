using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Enums;
public enum Cjt188MeterType : byte
{
    // 水表类
    ColdWater = 0x10,           // 冷水水表
    DomesticHotWater = 0x11,    // 生活热水水表
    DrinkingWater = 0x12,       // 直饮水水表
    ReclaimedWater = 0x13,      // 中水水表

    // 热量表类
    HeatMeter = 0x20,           // 热量表（热量）
    CoolingMeter = 0x21,        // 热量表（冷量）

    // 其他
    GasMeter = 0x30,            // 燃气表
    ElectricityMeter = 0x40,     // 电度表

    // 未知或未定义（可选）
    Unknown = 0x00
}
