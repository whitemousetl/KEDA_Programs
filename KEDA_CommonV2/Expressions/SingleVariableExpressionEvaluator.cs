using DynamicExpresso;

namespace KEDA_CommonV2.Expressions;

/// <summary>
/// 单变量数学表达式求值器
/// 支持: x*0.02+1, x/100, x+5 等
/// </summary>
public static class SingleVariableExpressionEvaluator
{
    /// <summary>
    /// 计算单变量表达式，x为变量
    /// </summary>
    public static double Evaluate(string expression, double x)
    {
        if (string.IsNullOrEmpty(expression))
            return Round(x);

        var interpreter = new Interpreter();
        var result = Convert.ToDouble(interpreter.SetVariable("x", x).Eval(expression));
        return Round(result);
    }

    /// <summary>
    /// 反解一元一次方程:  已知 y = f(x)，求 x
    /// 支持: x*a+b, x/a+b, x+b, x-b
    /// </summary>
    public static double InverseEvaluate(string expression, double y)
    {
        expression = expression.Replace(" ", "");

        return expression switch
        {
            var e when e == "x" => y,
            var e when e.StartsWith("x*") => ParseAndInvert(e[2..], y, (a, b) => (y - b) / a),
            var e when e.StartsWith("x/") => ParseAndInvert(e[2..], y, (a, b) => (y - b) * a),
            var e when e.StartsWith("x+") => y - double.Parse(e[2..]),
            var e when e.StartsWith("x-") => y + double.Parse(e[2..]),
            _ => throw new NotSupportedException($"不支持的表达式格式: {expression}")
        };
    }

    private static double ParseAndInvert(string part, double y, Func<double, double, double> inverter)
    {
        var parts = part.Split('+');
        double a = double.Parse(parts[0]);
        double b = parts.Length > 1 ? double.Parse(parts[1]) : 0;
        return inverter(a, b);
    }

    private static double Round(double value) => Math.Round(value, 2);
}