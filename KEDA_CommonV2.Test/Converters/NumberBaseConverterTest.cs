using KEDA_CommonV2.Converters;

namespace KEDA_CommonV2.Test.Converters;
public class NumberBaseConverterTest
{
    #region HexToDecimal
    // 十六进制转换成十进制 有效输入 预期返回值
    [Theory]
    [InlineData("FF", 255)]
    [InlineData("0xFF", 255)]
    [InlineData("ff", 255)]
    [InlineData("0xff", 255)]
    [InlineData("10", 16)]
    [InlineData("0x10", 16)]
    public void HexToDecimal_ValidInput_ReturnsExpected(string input, double expected)
    {
        var result = NumberBaseConverter.HexToDecimal(input);
        Assert.Equal(expected, result);
    }

    // 十六进制转换成十进制 有效输入但是带空格 预期返回值
    [Theory]
    [InlineData(" 0xFF ", 255)]
    [InlineData(" ff ", 255)]
    [InlineData(" 10", 16)]
    [InlineData("0x10 ", 16)]
    public void HexToDecimal_InputWithWhitespace_ReturnExpected(string input, double expected)
    {
        var result = NumberBaseConverter.HexToDecimal(input);
        Assert.Equal(expected, result);
    }

    // 十六进制转换成十进制 null 预期返回参数空异常
    [Fact]
    public void HexToDecimal_NullInput_ThrowsAgrumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NumberBaseConverter.HexToDecimal(null));
    }

    // 十六进制转换成十进制 无效输入 预期返回格式异常
    [Theory]
    [InlineData("notnumber")]
    [InlineData("")]
    public void HexToDecimal_InValidInput_ReturnsExcepted(object? input)
    {
        Assert.Throws<FormatException>(() => NumberBaseConverter.HexToDecimal(input));
    }
    #endregion

    #region DecimalToHex
    //十进制转换成十六进制 有效输入 预期返回值 并 带0x前缀
    [Theory]
    [InlineData(255, "0xFF")]
    [InlineData(17, "0x11")]
    public void DecimalToHex_ValidInput_ReturnsExpectedWithPreFix(long input, string expected)
    {
        var result = NumberBaseConverter.DecimalToHex(input, true);
        Assert.Equal(expected, result);
    }

    //十进制转换成十六进制 有效输入 预期返回值 并 不带0x前缀
    [Theory]
    [InlineData(255, "FF")]
    [InlineData(17, "11")]
    public void DecimalToHex_ValidInput_ReturnsExpectedWithoutPreFix(long input, string expected)
    {
        var result = NumberBaseConverter.DecimalToHex(input, false);
        Assert.Equal(expected, result);
    }

    //十进制转换成十六进制 null 预期返回参数空异常
    [Fact]
    public void DecimalToHex_NullInput_ThrowsAgrumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NumberBaseConverter.DecimalToHex(null));
    }

    //十进制转换成十六进制 无效输入 预期返回格式异常
    [Theory]
    [InlineData("notnumber")]
    [InlineData("")]
    public void DecimalToHex_InvalidInput_ThrowsFormatException(object input)
    {
        Assert.Throws<FormatException>(() => NumberBaseConverter.DecimalToHex(input));
    } 
    #endregion
}
