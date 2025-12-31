using KEDA_CommonV2.Converters;
using KEDA_CommonV2.Expressions;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Utilities;
using KEDA_Processing_CenterV2.Interfaces;
using System.Text.Json;

namespace KEDA_Processing_CenterV2.Services;
public class PointExpressionConverter : IPointExpressionConverter
{
    private readonly ILogger<PointExpressionConverter> _logger;

    public PointExpressionConverter(ILogger<PointExpressionConverter> logger)
    {
        _logger = logger;
    }

    public object? Convert(Point point, object? value)
    {
        try
        {
            if (string.IsNullOrEmpty(point.Change))
                return value;

            return point.Change.ToUpperInvariant() switch
            {
                "HEX2DEC" => NumberBaseConverter.HexToDecimal(value), //工具静态类，十六进制转十进制
                "DEC2HEX" => NumberBaseConverter.DecimalToHex(value, false), //工具静态类，十进制转十六进制
                _ => EvaluateExpression(point.Change, value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "点位值转换失败:  {Expression}, 原值: {Value}", point.Change, value);
            return value;
        }
    }

    private static object? EvaluateExpression(string expression, object? value)
    {
        if (value == null || !NumericTypeChecker.IsNumeric(value)) //工具静态类，判断值是哪种类型：数值原生类型、JsonElement、可解析的数值字符串
            return value;

        double numericValue = value switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetDouble(),
            _ => System.Convert.ToDouble(value)
        };

        return SingleVariableExpressionEvaluator.Evaluate(expression, numericValue);//工具静态类，一元一次表达式计算
    }
}