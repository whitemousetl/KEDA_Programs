using DynamicExpresso;
using KEDA_CommonV2.Expressions;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_Processing_CenterV2.Interfaces;

namespace KEDA_Processing_CenterV2.Services;
public class VirtualPointCalculator : IVirtualPointCalculator
{
    private readonly ILogger<VirtualPointCalculator> _logger;

    public VirtualPointCalculator(ILogger<VirtualPointCalculator> logger)
    {
        _logger = logger;
    }

    public void Calculate(IEnumerable<ParameterDto> virtualPoints, IDictionary<string, object?> deviceData)
    {
        foreach (var point in virtualPoints)
        {
            if (string.IsNullOrWhiteSpace(point.PositiveExpression))
                continue;

            try
            {
                var result = EvaluateExpression(point.PositiveExpression, deviceData);
                deviceData[point.Label] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "虚拟点计算失败: {Label}, 表达式: {Expression}", point.Label, point.PositiveExpression);
                deviceData[point.Label] = null;
            }
        }
    }

    private static object? EvaluateExpression(string expression, IDictionary<string, object?> deviceData)
    {
        var variables = VariablePlaceholderParser.ExtractVariableNames(expression);
        var normalizedExpression = VariablePlaceholderParser.ReplacePlaceholders(expression, variables);

        var interpreter = new Interpreter();
        foreach (var varName in variables)
        {
            var value = deviceData.TryGetValue(varName, out var val) ? val ?? 0 : 0;
            interpreter.SetVariable(varName, value);
        }

        return interpreter.Eval(normalizedExpression);
    }
}
