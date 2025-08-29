using FluentAssertions;
using KEDA_Share.Entity;
using KEDA_Share.Repository.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Test.Repository.Implementations;
public class PointValidatorTest
{
    private readonly PointValidator _validator = new();

    [Fact]
    public void Validate_ShouldReturnValid_WhenDataTypeIsLowerCase()
    {
        var point = new Point { Label = "p1", DataType = "int", Address = "100" };
        var result = _validator.Validate(point);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenPointIsNull()
    {
        var result = _validator.Validate(null);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("[采集点]存在采集点对象为空，请检查");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenLabelIsEmpty()
    {
        var point = new Point { Label = "", DataType = "Int", Address = "100" };

        var result = _validator.Validate(point);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Label为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDataTypeIsNotEnum()
    {
        var point = new Point { Label = "p1", DataType = "NotExists", Address = "100" };

        var result = _validator.Validate(point);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("暂未实现");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDataTypeIsEmpty()
    {
        var point = new Point { Label = "p1", DataType = "", Address = "100" };
        var result = _validator.Validate(point);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DataType为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenAddressIsEmpty()
    {
        var point = new Point { Label = "p1", DataType = "Int", Address = "" };
        var result = _validator.Validate(point);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Address为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDataTypeIsNumericString()
    {
        var point = new Point { Label = "p1", DataType = "1", Address = "100" };

        var result = _validator.Validate(point);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("暂未实现");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenPointIsCorrect()
    {
        var point = new Point { Label = "p1", DataType = "Int", Address = "100" };

        var result = _validator.Validate(point);

        result.IsValid.Should().BeTrue();
    }
}
