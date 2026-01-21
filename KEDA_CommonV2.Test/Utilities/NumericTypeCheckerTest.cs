using KEDA_CommonV2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Test.Utilities;

public class NumericTypeCheckerTest
{
    public static IEnumerable<object?[]> NumericPrimitiveValues =>
    [
        [(sbyte)-1],
        [(byte)255],
        [(short)-123],
        [(ushort)123],
        [-456],
        [789u],
        [long.MaxValue],
        [ulong.MaxValue],
        [3.1415f],
        [2.71828],
        [123.456m]
    ];

    [Theory]
    [MemberData(nameof(NumericPrimitiveValues))]
    public void IsNumeric_PrimitiveNumeric_ReturnsTrue(object value)
    {
        Assert.True(NumericTypeChecker.IsNumeric(value));
    }

    [Fact]
    public void IsNumeric_Null_ReturnsFalse()
    {
        Assert.False(NumericTypeChecker.IsNumeric(null));
    }

    [Fact]
    public void IsNumeric_JsonElementNumber_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("42");
        var element = doc.RootElement;
        Assert.True(NumericTypeChecker.IsNumeric(element));
    }

    [Fact]
    public void IsNumeric_JsonElementNonNumber_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse(@"""text""");
        var element = doc.RootElement;
        Assert.False(NumericTypeChecker.IsNumeric(element));
    }

    [Theory]
    [InlineData("123.45")]
    [InlineData("-0.001")]
    [InlineData("1e4")]
    public void IsNumeric_StringParsable_ReturnsTrue(string numericString)
    {
        Assert.True(NumericTypeChecker.IsNumeric(numericString));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("123abc")]
    public void IsNumeric_StringNotParsable_ReturnsFalse(string nonNumericString)
    {
        Assert.False(NumericTypeChecker.IsNumeric(nonNumericString));
    }

    [Fact]
    public void IsNumeric_Boolean_ReturnsFalse()
    {
        Assert.False(NumericTypeChecker.IsNumeric(true));
        Assert.False(NumericTypeChecker.IsNumeric(false));
    }

    [Fact]
    public void IsNumeric_Object_ReturnsFalse()
    {
        Assert.False(NumericTypeChecker.IsNumeric(new object()));
    }
}
