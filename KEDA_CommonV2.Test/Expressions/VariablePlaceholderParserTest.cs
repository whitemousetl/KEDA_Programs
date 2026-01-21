using KEDA_CommonV2.Expressions;

namespace KEDA_CommonV2.Test.Expressions;

public class VariablePlaceholderParserTest
{
    #region Parse - 正常解析测试

    [Fact]
    public void Parse_SingleVariable_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "{Temperature}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("Temperature", variables[0]);
        Assert.Equal("Temperature", normalized);
    }

    [Fact]
    public void Parse_MultipleVariables_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "{Temperature} + {Humidity}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.Equal("Temperature", variables[0]);
        Assert.Equal("Humidity", variables[1]);
        Assert.Equal("Temperature + Humidity", normalized);
    }

    [Theory]
    [InlineData("{x} * 2", new[] { "x" }, "x * 2")]
    [InlineData("{a} + {b} * {c}", new[] { "a", "b", "c" }, "a + b * c")]
    [InlineData("({x} + {y}) / 2", new[] { "x", "y" }, "(x + y) / 2")]
    public void Parse_VariousExpressions_ReturnsCorrectResult(string expression, string[] expectedVars, string expectedNormalized)
    {
        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(expectedVars.Length, variables.Count);
        for (int i = 0; i < expectedVars.Length; i++)
        {
            Assert.Equal(expectedVars[i], variables[i]);
        }
        Assert.Equal(expectedNormalized, normalized);
    }

    #endregion

    #region Parse - 重复变量测试

    [Fact]
    public void Parse_DuplicateVariable_ReturnsUniqueVariables()
    {
        // Arrange
        var expression = "{x} + {x}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("x + x", normalized);
    }

    [Fact]
    public void Parse_MultipleDuplicates_ReturnsUniqueVariablesInOrder()
    {
        // Arrange
        var expression = "{a} + {b} + {a} + {c} + {b}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(3, variables.Count);
        Assert.Equal("a", variables[0]);
        Assert.Equal("b", variables[1]);
        Assert.Equal("c", variables[2]);
        Assert.Equal("a + b + a + c + b", normalized);
    }

    #endregion

    #region Parse - 无变量测试

    [Fact]
    public void Parse_NoVariables_ReturnsEmptyListAndOriginalExpression()
    {
        // Arrange
        var expression = "1 + 2 * 3";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Empty(variables);
        Assert.Equal("1 + 2 * 3", normalized);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyListAndEmptyString()
    {
        // Arrange
        var expression = "";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Empty(variables);
        Assert.Equal("", normalized);
    }

    #endregion

    #region Parse - 空/无效占位符测试

    [Fact]
    public void Parse_EmptyPlaceholder_IgnoresEmptyVariable()
    {
        // Arrange
        var expression = "{} + {x}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("{} + x", normalized);  // 空占位符保持不变
    }

    [Theory]
    [InlineData("{ }")]
    [InlineData("{  }")]
    [InlineData("{\t}")]
    [InlineData("{\n}")]
    public void Parse_WhitespacePlaceholder_IgnoresWhitespaceVariable(string expression)
    {
        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Empty(variables);
        Assert.Equal(expression, normalized);  // 空白占位符保持不变
    }

    [Fact]
    public void Parse_MixedEmptyAndValidPlaceholders_OnlyExtractsValidVariables()
    {
        // Arrange
        var expression = "{} + {x} + { } + {y}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.Equal("x", variables[0]);
        Assert.Equal("y", variables[1]);
        Assert.Equal("{} + x + { } + y", normalized);
    }

    #endregion

    #region Parse - 不完整占位符测试

    [Fact]
    public void Parse_OnlyOpeningBrace_ReturnsEmptyListAndOriginalExpression()
    {
        // Arrange
        var expression = "{Temperature";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Empty(variables);
        Assert.Equal("{Temperature", normalized);
    }

    [Fact]
    public void Parse_OnlyClosingBrace_ReturnsEmptyListAndOriginalExpression()
    {
        // Arrange
        var expression = "Temperature}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Empty(variables);
        Assert.Equal("Temperature}", normalized);
    }

    [Fact]
    public void Parse_MismatchedBraces_HandlesCorrectly()
    {
        // Arrange - 第一个 { 没有对应的 }
        var expression = "{x + {y}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert - 只能解析出 "x + {y" 作为第一个变量名（因为找到的是最近的 }）
        // 实际行为：找到 { 在位置0，找到 } 在位置7，提取 "x + {y"
        Assert.Single(variables);
        Assert.Equal("x + {y", variables[0]);
    }

    [Fact]
    public void Parse_IncompleteAtEnd_StopsAtIncomplete()
    {
        // Arrange
        var expression = "{x} + {y";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("x + {y", normalized);
    }

    #endregion

    #region Parse - 连续占位符测试

    [Fact]
    public void Parse_ConsecutivePlaceholders_ExtractsAllVariables()
    {
        // Arrange
        var expression = "{a}{b}{c}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(3, variables.Count);
        Assert.Equal("a", variables[0]);
        Assert.Equal("b", variables[1]);
        Assert.Equal("c", variables[2]);
        Assert.Equal("abc", normalized);
    }

    #endregion

    #region Parse - 边界位置测试

    [Fact]
    public void Parse_VariableAtStart_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "{x} + 1";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("x + 1", normalized);
    }

    [Fact]
    public void Parse_VariableAtEnd_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "1 + {x}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("1 + x", normalized);
    }

    [Fact]
    public void Parse_OnlyVariable_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "{x}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("x", variables[0]);
        Assert.Equal("x", normalized);
    }

    #endregion

    #region Parse - 特殊变量名测试

    [Theory]
    [InlineData("{var1}", "var1")]
    [InlineData("{my_var}", "my_var")]
    [InlineData("{MyVariable123}", "MyVariable123")]
    [InlineData("{_privateVar}", "_privateVar")]
    public void Parse_VariableNamesWithSpecialCharacters_ReturnsCorrectResult(string expression, string expectedVar)
    {
        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal(expectedVar, variables[0]);
        Assert.Equal(expectedVar, normalized);
    }

    [Fact]
    public void Parse_ChineseVariableName_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "{温度} + {湿度}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.Equal("温度", variables[0]);
        Assert.Equal("湿度", variables[1]);
        Assert.Equal("温度 + 湿度", normalized);
    }

    [Fact]
    public void Parse_VariableNameWithSpaces_ExtractsAsIs()
    {
        // Arrange - 变量名包含空格（非空白，是有效字符）
        var expression = "{var name}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("var name", variables[0]);
        Assert.Equal("var name", normalized);
    }

    #endregion

    #region Parse - 嵌套括号测试

    [Fact]
    public void Parse_NestedBraces_ExtractsInnerContent()
    {
        // Arrange
        var expression = "{{x}}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        // 第一个 { 在位置0，第一个 } 在位置2，提取 "{x"
        Assert.Single(variables);
        Assert.Equal("{x", variables[0]);
        Assert.Equal("{x}", normalized);  // 替换 "{{x}" 为 "{x"，但原字符串没有 "{{x}"
    }

    #endregion

    #region Parse - 实际应用场景测试

    [Fact]
    public void Parse_SensorDataExpression_ReturnsCorrectResult()
    {
        // Arrange - 传感器数据表达式
        var expression = "{Sensor1} * 0.02 + {Offset}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.Equal("Sensor1", variables[0]);
        Assert.Equal("Offset", variables[1]);
        Assert.Equal("Sensor1 * 0.02 + Offset", normalized);
    }

    [Fact]
    public void Parse_TemperatureConversionExpression_ReturnsCorrectResult()
    {
        // Arrange - 温度转换表达式
        var expression = "({Celsius} * 1.8) + 32";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Single(variables);
        Assert.Equal("Celsius", variables[0]);
        Assert.Equal("(Celsius * 1.8) + 32", normalized);
    }

    [Fact]
    public void Parse_ComplexFormula_ReturnsCorrectResult()
    {
        // Arrange - 复杂公式
        var expression = "({A} + {B}) / 2 * {Scale} + {Offset}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(4, variables.Count);
        Assert.Equal("A", variables[0]);
        Assert.Equal("B", variables[1]);
        Assert.Equal("Scale", variables[2]);
        Assert.Equal("Offset", variables[3]);
        Assert.Equal("(A + B) / 2 * Scale + Offset", normalized);
    }

    #endregion

    #region Parse - 大量变量测试

    [Fact]
    public void Parse_ManyVariables_ReturnsAllVariables()
    {
        // Arrange
        var expression = "{v1} + {v2} + {v3} + {v4} + {v5} + {v6} + {v7} + {v8} + {v9} + {v10}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(10, variables.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"v{i + 1}", variables[i]);
        }
        Assert.Equal("v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8 + v9 + v10", normalized);
    }

    #endregion

    #region Parse - 特殊符号在表达式中测试

    [Theory]
    [InlineData("{x} > {y}", new[] { "x", "y" }, "x > y")]
    [InlineData("{x} < {y}", new[] { "x", "y" }, "x < y")]
    [InlineData("{x} == {y}", new[] { "x", "y" }, "x == y")]
    [InlineData("{x} && {y}", new[] { "x", "y" }, "x && y")]
    [InlineData("{x} || {y}", new[] { "x", "y" }, "x || y")]
    public void Parse_LogicalExpressions_ReturnsCorrectResult(string expression, string[] expectedVars, string expectedNormalized)
    {
        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.Equal(expectedVars.Length, variables.Count);
        for (int i = 0; i < expectedVars.Length; i++)
        {
            Assert.Equal(expectedVars[i], variables[i]);
        }
        Assert.Equal(expectedNormalized, normalized);
    }

    #endregion

    #region Parse - 返回值类型验证测试

    [Fact]
    public void Parse_ReturnsListType_NotOtherCollectionType()
    {
        // Arrange
        var expression = "{x} + {y}";

        // Act
        var (variables, normalized) = VariablePlaceholderParser.Parse(expression);

        // Assert
        Assert.IsType<List<string>>(variables);
        Assert.IsType<string>(normalized);
    }

    [Fact]
    public void Parse_ReturnedListIsMutable()
    {
        // Arrange
        var expression = "{x}";

        // Act
        var (variables, _) = VariablePlaceholderParser.Parse(expression);

        // Assert - 返回的列表应该可以修改
        variables.Add("y");
        Assert.Equal(2, variables.Count);
    }

    #endregion
}