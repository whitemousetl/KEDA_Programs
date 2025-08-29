using FluentAssertions;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Test.Repository.Implementations;
public class DeviceValidatorTest
{
    private readonly Mock<IValidator<Point>> _pointValidatorMock;
    private readonly DeviceValidator _validator;

    public DeviceValidatorTest()
    {
        _pointValidatorMock = new Mock<IValidator<Point>>();
        _validator = new DeviceValidator(_pointValidatorMock.Object);
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenDeviceIsNull()
    {
        var result = _validator.Validate(null);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("[设备]存在设备对象为空，请检查");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenEquipmentIDIsEmpty()
    {
        var device = new Device
        {
            EquipmentID = "",
            EquipmentName = "设备A",
            Points = [new Point()]
        };

        var result = _validator.Validate(device);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("设备ID(EquipmentID)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenEquipmentNameIsEmpty()
    {
        var device = new Device
        {
            EquipmentID = "D1",
            EquipmentName = "",
            Points = [new Point()]
        };

        var result = _validator.Validate(device);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("设备名称(EquipmentName)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenPointsIsNull()
    {
        var device = new Device
        {
            EquipmentID = "D1",
            EquipmentName = "设备A",
            Points = null!
        };

        var result = _validator.Validate(device);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("测点列表(Points)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenPointsIsEmpty()
    {
        var device = new Device
        {
            EquipmentID = "D1",
            EquipmentName = "设备A",
            Points = []
        };

        var result = _validator.Validate(device);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("测点列表(Points)为空");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenAnyPointIsInvalid()
    {
        var device = new Device
        {
            EquipmentID = "D1",
            EquipmentName = "设备A",
            Points = [new Point { Label = "p1" }]
        };

        _pointValidatorMock
            .Setup(x => x.Validate(It.IsAny<Point>()))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = "采集点无效" });

        var result = _validator.Validate(device);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("采集点无效");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenDeviceAndAllPointsAreValid()
    {
        var device = new Device
        {
            EquipmentID = "D1",
            EquipmentName = "设备A",
            Points = [new Point { Label = "p1" }, new Point { Label = "p2" }],
        };

        _pointValidatorMock.Setup(x => x.Validate(It.IsAny<Point>())).Returns(new ValidationResult { IsValid = true });

        var result = _validator.Validate(device);
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
    }
}
