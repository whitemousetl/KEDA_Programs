using FluentAssertions;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using Moq;
using System.Collections.Generic;

namespace KEDA_Share.Test.Repository.Implementations;
public class WorkstationValidatorTest
{
    private readonly Mock<IValidator<Protocol>> _protocolValidatorMock;
    private readonly WorkstationValidator _validator;

    public WorkstationValidatorTest()
    {
        _protocolValidatorMock = new Mock<IValidator<Protocol>>();
        _validator = new WorkstationValidator(_protocolValidatorMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldReturnInvalid_WhenWorkstaitonNameIsNullOrEmpty(string? edgeName)
    {
        var ws = new Workstation
        {
            EdgeName = edgeName!,
            Protocols = [
                 new Protocol
                {
                    ProtocolID = "P1",
                    Interface = "LAN",
                    ProtocolType = "Modbus",
                    CollectCycle = "100",
                    ReceiveTimeOut = "100",
                    ConnectTimeOut = "100",
                    IPAddress = "192.168.1.1",
                    ProtocolPort = "502",
                    Format = "ABCD",
                    AddressStartWithZero = "true",
                    Devices = [
                        new Device
                        {
                            EquipmentID = "D1",
                            EquipmentName = "设备A",
                            StationNo = "1",
                            Points = [
                                new Point { Label = "p1", DataType = "Int", Address = "100" }
                            ]
                        }
                    ]
                }
             ]
        };

        var result = _validator.Validate(ws);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"传入的工作站名字{nameof(ws.EdgeName)}为空，请检查");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenAllDataIsCorrect()
    {
        var pointValidator = new PointValidator();
        var deviceValidator = new DeviceValidator(pointValidator);
        var protocolValidator = new ProtocolValidator(deviceValidator);
        var workstationValidator = new WorkstationValidator(protocolValidator);

        var workstation = new Workstation
        {
            EdgeName = "www",
            Protocols = [
                new Protocol
                {
                    ProtocolID = "P1",
                    Interface = "LAN",
                    ProtocolType = "Modbus",
                    CollectCycle = "100",
                    ReceiveTimeOut = "100",
                    ConnectTimeOut = "100",
                    IPAddress = "192.168.1.1",
                    ProtocolPort = "502",
                    Format = "ABCD",
                    AddressStartWithZero = "true",
                    Devices = [
                        new Device
                        {
                            EquipmentID = "D1",
                            EquipmentName = "设备A",
                            StationNo = "1",
                            Points = [
                                new Point { Label = "p1", DataType = "Int", Address = "100" }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = workstationValidator.Validate(workstation);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenWorkstationIsNull()
    {
        var result = _validator.Validate(null);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("传入的工作站为空，请检查");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenProtocolsIsNull()
    {
        var ws = new Workstation
        {
            EdgeName = "www",
            Protocols = null!
        };

        var result = _validator.Validate(ws);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("协议数量Protocols为0");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenProtocolsIsEmpty()
    {
        var ws = new Workstation
        {
            EdgeName = "www",
            Protocols = []
        };

        var result = _validator.Validate(ws);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("协议数量Protocols为0");
    }

    [Fact]
    public void Validate_ShouldReturnInvalid_WhenAnyProtocolIsInvalid()
    {
        var ws = new Workstation
        {
            EdgeName = "www",
            Protocols = [new Protocol()]
        };

        _protocolValidatorMock
            .Setup(x => x.Validate(It.IsAny<Protocol>()))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = "协议无效" });

        var result = _validator.Validate(ws);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("协议无效");
    }

    [Fact]
    public void Validate_ShouldReturnValid_WhenAllProtocolsAreValid()
    {
        var ws = new Workstation
        {
            EdgeName = "www",
            Protocols = [new Protocol(), new Protocol()]
        };

        _protocolValidatorMock
            .Setup(x => x.Validate(It.IsAny<Protocol>()))
            .Returns(new ValidationResult { IsValid = true });

        var result = _validator.Validate(ws);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
    }
}
