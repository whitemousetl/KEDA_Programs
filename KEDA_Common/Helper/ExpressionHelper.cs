using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Helper;
public static class ExpressionHelper
{
    // 计算表达式，x为变量
    public static double Eval(string expr, double x)
    {
        if (!string.IsNullOrEmpty(expr))
        {
            var interpreter = new Interpreter();
            var result = Convert.ToDouble(interpreter.SetVariable("x", x).Eval(expr));
            return Math.Round(result, 2); // 保留两位小数
        }
        else
            return Math.Round(x, 2);
    }

    public static double HexToDecimal(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string str = value.ToString() ?? "";
        // 支持0x前缀或纯十六进制字符串
        if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            str = str[2..];
        if (int.TryParse(str, System.Globalization.NumberStyles.HexNumber, null, out int result))
            return result;
        Console.WriteLine($"成功转换，值是{result}");
        throw new FormatException($"无法将值 '{value}' 解析为十六进制数");
    }

    public static string DecimalToHex(object? value, bool withPrefix = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        string str = value.ToString() ?? "";
        if (!long.TryParse(str, out long dec))
            throw new FormatException($"无法将值 '{value}' 解析为十进制数");
        var hex = dec.ToString("X");
        var result = withPrefix ? "0x" + hex : hex;
        Console.WriteLine($"[DEC2HEX] 输入: {value} => 十六进制: {result}");
        return result;
    }

    // 反解一元一次表达式，支持 x*a+b 或 x/a+b
    public static double InverseEval(string expr, double y)
    {
        // 只支持 x*a+b 或 x/a+b 形式
        // 例如: x*0.02+1  => x = (y-1)/0.02
        //      x/0.02+1  => x = (y-1)*0.02
        //      x+2       => x = y-2
        //      x*0.01    => x = y/0.01
        //      x/0.01    => x = y*0.01
        try
        {
            expr = expr.Replace(" ", "");
            if (expr.StartsWith("x*"))
            {
                var parts = expr.Substring(2).Split('+');
                double a = double.Parse(parts[0]);
                double b = parts.Length > 1 ? double.Parse(parts[1]) : 0;
                return (y - b) / a;
            }
            else if (expr.StartsWith("x/"))
            {
                var parts = expr.Substring(2).Split('+');
                double a = double.Parse(parts[0]);
                double b = parts.Length > 1 ? double.Parse(parts[1]) : 0;
                return (y - b) * a;
            }
            else if (expr.StartsWith("x+"))
            {
                double b = double.Parse(expr.Substring(2));
                return y - b;
            }
            else if (expr.StartsWith("x-"))
            {
                double b = double.Parse(expr.Substring(2));
                return y + b;
            }
            else if (expr == "x")
            {
                return y;
            }
        }
        catch (FormatException)
        {
            throw new NotSupportedException("只支持简单一元一次表达式反解");
        }
        throw new NotSupportedException("只支持简单一元一次表达式反解");
    }

    public static bool IsNumericType(object value)
    {
        if (value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return true;

        // 支持 JsonElement 数值类型
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return true;

        // 支持可解析为数值的字符串
        if (value is string s && double.TryParse(s, out _))
            return true;

        return false;
    }
}
