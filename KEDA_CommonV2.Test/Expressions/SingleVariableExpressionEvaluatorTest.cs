using DynamicExpresso.Exceptions;
using KEDA_CommonV2.Expressions;

namespace KEDA_CommonV2.Test.Expressions;

public class SingleVariableExpressionEvaluatorTest
{
    #region Evaluate正常计算测试

    [Theory]
    [InlineData("x*2", 5, 10)]
    [InlineData("x+1", 5, 6)]
    [InlineData("x-1", 5, 4)]
    [InlineData("x/2", 10, 5)]
    [InlineData("x*0.02+1", 100, 3)]
    [InlineData("x/100", 500, 5)]
    [InlineData("x", 5, 5)]
    public void Evaluate_ValidExpression_ReturnsCorrectResult(string expression, double x, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate(expression, x);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("x*2+x*3", 2, 10)]       // 多个 x
    [InlineData("(x+1)*2", 3, 8)]         // 带括号
    [InlineData("x*x", 3, 9)]             // x 的平方
    [InlineData("-x", 5, -5)]             // 负号
    [InlineData("x*-1", 5, -5)]           // 乘以负数
    public void Evaluate_ComplexExpression_ReturnsCorrectResult(string expression, double x, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate(expression, x);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Evaluate空表达式测试

    [Theory]
    [InlineData(null, 5.554)]
    public void Evaluate_NullOrEmptyExpression_ThrowsArgumentNullException(string? expression, double x)
    {
        // Act
        Assert.Throws<ArgumentNullException>(() => SingleVariableExpressionEvaluator.Evaluate(expression!, x));
    }

    [Theory]
    [InlineData("", 5.554)]
    [InlineData(" ", 5.554)]
    [InlineData("\t", 5.554)]
    public void Evaluate_NullOrEmptyExpression_ThrowsArgumentException(string? expression, double x)
    {
        // Act
        Assert.Throws<ArgumentException>(() => SingleVariableExpressionEvaluator.Evaluate(expression!, x));
    }

    #endregion

    #region Evaluate四舍五入测试

    [Theory]
    [InlineData("x/3", 10, 3.33)]         // 保留两位小数
    [InlineData("x/3", 1, 0.33)]
    [InlineData("x*0.001", 1, 0)]         // 结果过小
    [InlineData("x+0.005", 1, 1.01)]      // 四舍五入进位 (AwayFromZero)
    [InlineData("x+0.004", 1, 1)]         // 不进位
    [InlineData("x-0.005", 1, 1)]      // 负向四舍五入
    [InlineData("x-0.006", 1, 0.99)]      // 负向四舍五入
    public void Evaluate_ResultRounding_ReturnsTwoDecimalPlaces(string expression, double x, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate(expression, x);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Evaluate边界值测试

    [Fact]
    public void Evaluate_XIsZero_ReturnsCorrectResult()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate("x*100+5", 0);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void Evaluate_XIsNegative_ReturnsCorrectResult()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate("x*2", -5);

        // Assert
        Assert.Equal(-10, result);
    }

    [Fact]
    public void Evaluate_XIsVeryLarge_ReturnsCorrectResult()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate("x/1000000", 1000000000);

        // Assert
        Assert.Equal(1000, result);
    }

    [Fact]
    public void Evaluate_XIsVerySmall_ReturnsCorrectResult()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate("x*1000000", 0.000001);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Evaluate_ExpressionWithSpaces_ReturnsCorrectResult()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate("x * 2 + 1", 5);

        // Assert
        Assert.Equal(11, result);
    }

    #endregion

    #region Evaluate语法错误异常测试

    [Theory]
    [InlineData("x*")]           // 不完整表达式
    [InlineData("x+")]           // 缺少操作数
    [InlineData("*x")]           // 无效开头
    [InlineData("x**1")]         // 无效的双乘号
    [InlineData("x//1")]         // 无效的双除号
    [InlineData("x(2)")]         // 无效语法
    public void Evaluate_InvalidSyntax_ThrowsArgumentException(string expression)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate(expression, 5));
        Assert.Contains("格式无效", ex.Message);
        Assert.Equal("expression", ex.ParamName);
    }

    [Theory]
    [InlineData("y*2")]          // 未知变量 y
    [InlineData("x+z")]          // 未知变量 z
    [InlineData("abc")]          // 完全未知标识符
    public void Evaluate_UnknownIdentifier_ThrowsArgumentException(string expression)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate(expression, 5));
        Assert.Contains("格式无效", ex.Message);
        Assert.Equal("expression", ex.ParamName);
    }

    #endregion

    #region Evaluate除零异常测试

    [Fact]
    public void Evaluate_DivideByZero_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate("1/x", 0));
        Assert.Contains("表达式 '1/x' 计算失败 (x=0): 结果超出有效范围（可能是除以零或数值溢", ex.Message);
        Assert.Equal("expression", ex.ParamName);
    }

    [Fact]
    public void Evaluate_DivideByZeroExpression_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate("x/(1-1)", 5));
        Assert.Contains("表达式 'x/(1-1)' 计算失败 (x=5): 结果超出有效范围（可能是除以零或数值溢", ex.Message);
    }

    #endregion

    #region Evaluate溢出异常测试

    [Fact]
    public void Evaluate_Overflow_ThrowsArgumentException()
    {
        // Arrange - double.MaxValue 的多次乘积会溢出为 Infinity
        var expression = "x*x*x*x*x*x*x*x*x*x";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate(expression, double.MaxValue));
        Assert.Contains("结果超出有效范围", ex.Message);
    }

    #endregion

    #region Evaluate线程安全测试

    [Fact]
    public async Task Evaluate_ConcurrentCalls_ReturnsCorrectResults()
    {
        // Arrange
        var tasks = new List<Task<double>>();
        var random = new Random(42);

        // Act - 并发执行多个计算
        for (int i = 0; i < 100; i++)
        {
            var x = random.NextDouble() * 100;
            var expectedResult = Math.Round(x * 2 + 1, 2, MidpointRounding.AwayFromZero);

            tasks.Add(Task.Run(() =>
            {
                var result = SingleVariableExpressionEvaluator.Evaluate("x*2+1", x);
                Assert.Equal(expectedResult, result);
                return result;
            }));
        }

        // Assert - 所有任务都应成功完成
        await Task.WhenAll(tasks);
        Assert.Equal(100, tasks.Count(t => t.IsCompletedSuccessfully));
    }

    #endregion

    #region Evaluate数学函数测试

    [Theory]
    [InlineData("Math.Abs(x)", -5, 5)]
    [InlineData("Math.Sqrt(x)", 16, 4)]
    [InlineData("Math.Pow(x, 2)", 3, 9)]
    [InlineData("Math.Max(x, 10)", 5, 10)]
    [InlineData("Math.Min(x, 10)", 5, 5)]
    public void Evaluate_MathFunctions_ReturnsCorrectResult(string expression, double x, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate(expression, x);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Evaluate实际应用场景测试

    [Fact]
    public void Evaluate_TemperatureConversion_ReturnsCorrectResult()
    {
        // Arrange - 摄氏度转华氏度: F = C * 1.8 + 32
        var expression = "x*1.8+32";
        var celsius = 100.0;

        // Act
        var fahrenheit = SingleVariableExpressionEvaluator.Evaluate(expression, celsius);

        // Assert
        Assert.Equal(212, fahrenheit);
    }

    [Fact]
    public void Evaluate_LinearScaling_ReturnsCorrectResult()
    {
        // Arrange - 线性缩放: 0-4095 -> 0-100
        var expression = "x/4095*100";
        var rawValue = 2047.5;

        // Act
        var scaledValue = SingleVariableExpressionEvaluator.Evaluate(expression, rawValue);

        // Assert
        Assert.Equal(50, scaledValue);
    }

    [Fact]
    public void Evaluate_OffsetAndGain_ReturnsCorrectResult()
    {
        // Arrange - 偏移量和增益: y = x * 0.02 + 1
        var expression = "x*0.02+1";
        var rawValue = 500.0;

        // Act
        var result = SingleVariableExpressionEvaluator.Evaluate(expression, rawValue);

        // Assert
        Assert.Equal(11, result);
    }

    #endregion

    #region Evaluate异常测试

    [Theory]
    [InlineData("0/x", 0)]              // 0/0 = NaN
    [InlineData("x-x", double.NaN)]     // NaN 参与运算
    [InlineData("x/x", double.NaN)]     // NaN/NaN = NaN
    public void Evaluate_ResultIsNaN_ThrowsArgumentException(string expression, double x)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleVariableExpressionEvaluator.Evaluate(expression, x));
        Assert.Contains("结果无效", ex.Message);
    }

    [Fact]
    public void Evaluate_ResultCannotConvertToDouble_ThrowsInvalidOperationException()
    {
        // 注意：这个测试可能无法通过，因为 DynamicExpresso 数学表达式通常返回数值类型
        // 如果无法构造出触发条件，说明这个 catch 块是死代码，应该移除

        // 可能的测试方式：使用返回字符串的表达式（如果 DynamicExpresso 支持）
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SingleVariableExpressionEvaluator.Evaluate("\"text\"", 5));
        Assert.Contains("结果无法转换为数值", ex.Message);
    }

    #endregion 

    #region RoundToTwoDecimals - 测试

    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(1.004, 1)]
    [InlineData(-1.005, -1.01)]
    [InlineData(0, 0)]
    [InlineData(123.456, 123.46)]
    public void RoundToTwoDecimals_ValidValue_ReturnsRoundedResult(double value, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.RoundToTwoDecimals(value);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RoundToTwoDecimals_ValueExceedsDecimalMax_FallsBackToDoubleRound()
    {
        // Arrange - 超出 decimal 范围的值
        var largeValue = (double)decimal.MaxValue * 2;

        // Act
        var result = SingleVariableExpressionEvaluator.RoundToTwoDecimals(largeValue);

        // Assert
        Assert.Equal(Math.Round(largeValue, 2, MidpointRounding.AwayFromZero), result);
    }

    [Fact]
    public void RoundToTwoDecimals_ValueBelowDecimalMin_FallsBackToDoubleRound()
    {
        // Arrange - 低于 decimal 范围的值
        var smallValue = (double)decimal.MinValue * 2;

        // Act
        var result = SingleVariableExpressionEvaluator.RoundToTwoDecimals(smallValue);

        // Assert
        Assert.Equal(Math.Round(smallValue, 2, MidpointRounding.AwayFromZero), result);
    }

    #endregion

    #region InverseEvaluate - 表达式 "x" 测试

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(-5.5)]
    [InlineData(100.99)]
    public void InverseEvaluate_ExpressionIsX_ReturnsY(double y)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate("x", y);

        // Assert
        Assert.Equal(y, result);
    }

    #endregion

    #region InverseEvaluate - 乘法表达式 "x*a" 和 "x*a+b" 测试

    [Theory]
    [InlineData("x*2", 10, 5)]           // y=10, x*2=10 → x=5
    [InlineData("x*0.5", 5, 10)]         // y=5, x*0.5=5 → x=10
    [InlineData("x*-2", -10, 5)]         // y=-10, x*-2=-10 → x=5
    [InlineData("x*10", 100, 10)]        // y=100, x*10=100 → x=10
    public void InverseEvaluate_MultiplyExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("x*2+3", 13, 5)]         // y=13, x*2+3=13 → x=5
    [InlineData("x*0.5+1", 6, 10)]       // y=6, x*0.5+1=6 → x=10
    [InlineData("x*10+5", 105, 10)]      // y=105, x*10+5=105 → x=10
    public void InverseEvaluate_MultiplyWithOffsetExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 除法表达式 "x/a" 和 "x/a+b" 测试

    [Theory]
    [InlineData("x/2", 5, 10)]           // y=5, x/2=5 → x=10
    [InlineData("x/0.5", 10, 5)]         // y=10, x/0.5=10 → x=5
    [InlineData("x/10", 10, 100)]        // y=10, x/10=10 → x=100
    public void InverseEvaluate_DivideExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("x/2+3", 8, 10)]         // y=8, x/2+3=8 → x=10
    [InlineData("x/10+5", 15, 100)]      // y=15, x/10+5=15 → x=100
    public void InverseEvaluate_DivideWithOffsetExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 加法表达式 "x+a" 测试

    [Theory]
    [InlineData("x+5", 15, 10)]          // y=15, x+5=15 → x=10
    [InlineData("x+0", 10, 10)]          // y=10, x+0=10 → x=10
    [InlineData("x+-5", 5, 10)]          // y=5, x+(-5)=5 → x=10
    [InlineData("x+0.5", 10.5, 10)]      // y=10.5, x+0.5=10.5 → x=10
    public void InverseEvaluate_AddExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 减法表达式 "x-a" 测试

    [Theory]
    [InlineData("x-5", 5, 10)]           // y=5, x-5=5 → x=10
    [InlineData("x-0", 10, 10)]          // y=10, x-0=10 → x=10
    [InlineData("x--5", 15, 10)]         // y=15, x-(-5)=15 → x=10
    [InlineData("x-0.5", 9.5, 10)]       // y=9.5, x-0.5=9.5 → x=10
    public void InverseEvaluate_SubtractExpression_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 带空格表达式测试

    [Theory]
    [InlineData(" x ", 10, 10)]
    [InlineData("x * 2", 10, 5)]
    [InlineData("x / 2", 5, 10)]
    [InlineData("x + 5", 15, 10)]
    [InlineData("x - 5", 5, 10)]
    [InlineData(" x * 2 + 3 ", 13, 5)]
    public void InverseEvaluate_ExpressionWithSpaces_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 现已支持的表达式测试

    [Theory]
    [InlineData("2*x", 10, 5)]           // x 不在开头 → 现已支持
    [InlineData("2+x", 12, 10)]          // x 不在开头 → 现已支持
    [InlineData("x*2*3", 60, 10)]        // 多个乘法 → 现已支持
    [InlineData("x*2-3", 7, 5)]          // 减法偏移 → 现已支持
    public void InverseEvaluate_VariousLinearFormats_ReturnsCorrectX(string expression, double y, double expected)
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region InverseEvaluate - 不支持的表达式格式测试

    [Theory]
    [InlineData("y")]                // 其他变量
    public void InverseEvaluate_InvalidExpression_ThrowsArgumentException(string expression)
    {
        // Act & Assert
        var ex = Assert.Throws<UnknownIdentifierException>(() =>
            SingleVariableExpressionEvaluator.InverseEvaluate(expression, 10));
        Assert.Contains("y", ex.Message);
    }

    [Theory]
    [InlineData("1/x")]              // 倒数 - f(0) 无效
    public void InverseEvaluate_NonLinearExpression_ThrowsInvalidOperationException(string expression)
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SingleVariableExpressionEvaluator.InverseEvaluate(expression, 10));
        Assert.Contains("表达式计算结果无效", ex.Message);
    }

    [Theory]
    [InlineData("x^2")]              // 幂运算 - 非一元一次方程
    //[InlineData("x*x")]              // 二次方程
    public void InverseEvaluate_NonLinearEquation_ThrowsInvalidOperationException(string expression)
    {
        // 注意：当前实现不会检测非线性方程，会返回错误结果
        // 如果需要严格验证，应添加线性检测逻辑

        // 当前行为：x^2 会被误当作线性方程处理
        // y=10 时，算出 x=10（错误，正确应为 √10）
        var ex = Assert.Throws<ParseException>(() => SingleVariableExpressionEvaluator.InverseEvaluate(expression, 10));
        Assert.Contains("Invalid Operation", ex.Message);
        //var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, 10);
        //Assert.NotEqual(Math.Sqrt(10), result, 5); // 说明结果是错的
    }

    [Theory]
    [InlineData("x*x")]              // 二次方程
    public void InverseEvaluate_NonLinearEquation_QuadraticEquation(string expression)
    {
        // 注意：当前实现不会检测非线性方程，会返回错误结果
        // 如果需要严格验证，应添加线性检测逻辑

        // 当前行为：x^2 会被误当作线性方程处理
        // y=10 时，算出 x=10（错误，正确应为 √10）
        var ex = Assert.Throws<ArgumentException>(() => SingleVariableExpressionEvaluator.InverseEvaluate(expression, 100));
        Assert.Contains("不是一元一次方程", ex.Message);
        //var result = SingleVariableExpressionEvaluator.InverseEvaluate(expression, 10);
        //Assert.NotEqual(Math.Sqrt(10), result, 5); // 说明结果是错的
    }
    #endregion

    #region InverseEvaluate - 边界值测试

    [Fact]
    public void InverseEvaluate_YIsZero_ReturnsCorrectX()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate("x*2+10", 0);

        // Assert - y=0, x*2+10=0 → x=-5
        Assert.Equal(-5, result);
    }

    [Fact]
    public void InverseEvaluate_YIsNegative_ReturnsCorrectX()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate("x*2", -10);

        // Assert - y=-10, x*2=-10 → x=-5
        Assert.Equal(-5, result);
    }

    [Fact]
    public void InverseEvaluate_YIsVeryLarge_ReturnsCorrectX()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate("x*0.001", 1000000);

        // Assert - y=1000000, x*0.001=1000000 → x=1000000000
        Assert.Equal(1000000000, result);
    }

    [Fact]
    public void InverseEvaluate_YIsVerySmall_ReturnsCorrectX()
    {
        // Act
        var result = SingleVariableExpressionEvaluator.InverseEvaluate("x*1000", 0.001);

        // Assert - y=0.001, x*1000=0.001 → x=0.000001
        Assert.Equal(0, result);
    }

    #endregion

    #region InverseEvaluate - 实际应用场景测试

    [Fact]
    public void InverseEvaluate_TemperatureConversion_ReturnsCorrectCelsius()
    {
        // Arrange - 华氏度转摄氏度: 已知 F = C * 1.8 + 32，求 C
        var expression = "x*1.8+32";
        var fahrenheit = 212.0;

        // Act
        var celsius = SingleVariableExpressionEvaluator.InverseEvaluate(expression, fahrenheit);

        // Assert - F=212, C*1.8+32=212 → C=100
        Assert.Equal(100, celsius);
    }

    [Fact]
    public void InverseEvaluate_LinearScaling_ReturnsCorrectRawValue()
    {
        // Arrange - 反向缩放: 已知 y = x * 0.02 + 1，求 x
        var expression = "x*0.02+1";
        var scaledValue = 11.0;

        // Act
        var rawValue = SingleVariableExpressionEvaluator.InverseEvaluate(expression, scaledValue);

        // Assert - y=11, x*0.02+1=11 → x=500
        Assert.Equal(500, rawValue);
    }

    [Fact]
    public void InverseEvaluate_SensorCalibration_ReturnsCorrectRawReading()
    {
        // Arrange - 传感器校准反算: 已知 显示值 = 原始值 / 100 + 5
        var expression = "x/100+5";
        var displayValue = 15.0;

        // Act
        var rawReading = SingleVariableExpressionEvaluator.InverseEvaluate(expression, displayValue);

        // Assert - y=15, x/100+5=15 → x=1000
        Assert.Equal(1000, rawReading);
    }

    #endregion

    #region InverseEvaluate - 与 Evaluate 互验测试

    [Theory]
    [InlineData("x*2", 5)]
    [InlineData("x*0.5+10", 20)]
    [InlineData("x/4", 100)]
    [InlineData("x/2+5", 50)]
    [InlineData("x+100", 50)]
    [InlineData("x-25", 75)]
    public void InverseEvaluate_ThenEvaluate_ReturnsOriginalY(string expression, double originalX)
    {
        // Arrange - 先用 Evaluate 计算 y
        var y = SingleVariableExpressionEvaluator.Evaluate(expression, originalX);

        // Act - 再用 InverseEvaluate 反算 x
        var calculatedX = SingleVariableExpressionEvaluator.InverseEvaluate(expression, y);

        // Assert - 反算出的 x 应该等于原始 x（考虑浮点精度）
        Assert.Equal(originalX, calculatedX, 10);
    }

    #endregion
}