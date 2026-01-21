using KEDA_CommonV2.Converters;
using KEDA_CommonV2.Expressions;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Utilities;
using KEDA_ControllerV2.Interfaces;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class PointExpressionConverter : IPointExpressionConverter
{
    private readonly ILogger<PointExpressionConverter> _logger;

    public PointExpressionConverter(ILogger<PointExpressionConverter> logger)
    {
        _logger = logger;
    }

    public object? Convert(ParameterDto point, object? value)
    {
        try
        {
            if (string.IsNullOrEmpty(point.PositiveExpression))
                return value;

            return point.PositiveExpression.ToUpperInvariant() switch
            {
                "HEX2DEC" => NumberBaseConverter.HexToDecimal(value), //工具静态类，十六进制转十进制
                "DEC2HEX" => NumberBaseConverter.DecimalToHex(value, false), //工具静态类，十进制转十六进制
                _ => EvaluateExpression(point.PositiveExpression, value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "点位值转换失败:  {Expression}, 原值: {Value}", point.PositiveExpression, value);
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

        try
        {
            return SingleVariableExpressionEvaluator.Evaluate(expression, numericValue);
        }
        catch (Exception)
        {
            // 表达式计算失败，返回四舍五入后的原值作为降级处理
            return SingleVariableExpressionEvaluator.RoundToTwoDecimals(numericValue);
        }
    }
}