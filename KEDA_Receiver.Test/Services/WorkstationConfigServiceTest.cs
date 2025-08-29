using FluentAssertions;
using KEDA_Receiver.Services;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Receiver.Test.Services;
public class WorkstationConfigServiceTest
{
    private readonly Mock<IWorkstationRepository> _repoMock = new();
    private readonly Mock<IValidator<Workstation>> _validatorMock = new();
    private readonly WorkstationConfigService _service;

    public WorkstationConfigServiceTest()
    {
        _service = new(_repoMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenWorkstationIsNull()
    {
        _validatorMock.Setup(x => x.Validate(null)).Returns(new ValidationResult { IsValid = false, ErrorMessage = "传入的工作站为空，请检查" });

        var result = await _service.HandleAsync(null);

        result.Should().NotBeNull();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Message.Should().Contain("传入的工作站为空，请检查");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenValidationFails()
    {
        var ws = new Workstation { EdgeName = "Edge1", Protocols = [] };
        _validatorMock.Setup(x => x.Validate(ws))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = "校验失败" });

        var result = await _service.HandleAsync(ws);

        result.Should().NotBeNull();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Message.Should().Contain("校验失败");
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryAndReturnSuccess_WhenValidationPasses()
    {
        var ws = new Workstation
        {
            EdgeName = "Edge1",
            Protocols = [new Protocol { ProtocolID = "P1", Interface = "LAN", ProtocolType = "Modbus" }]
        };
        _validatorMock.Setup(x => x.Validate(ws))
            .Returns(new ValidationResult { IsValid = true });
        _repoMock.Setup(x => x.AddAsync(ws, default)).Returns(Task.CompletedTask);

        var result = await _service.HandleAsync(ws);

        result.Should().NotBeNull();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult.Should().NotBeNull();
        okResult.Value.Should().NotBeNull();
        okResult.Value.Message.Should().Contain("保存 Workstation 成功");
        _repoMock.Verify(x => x.AddAsync(ws, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenRepositoryThrows()
    {
        var ws = new Workstation
        {
            EdgeName = "Edge1",
            Protocols = [new Protocol { ProtocolID = "P1", Interface = "LAN", ProtocolType = "Modbus" }]
        };
        _validatorMock.Setup(x => x.Validate(ws))
            .Returns(new ValidationResult { IsValid = true });
        _repoMock.Setup(x => x.AddAsync(ws, default)).ThrowsAsync(new Exception("数据库异常"));

        Func<Task> act = async () => await _service.HandleAsync(ws);

        await act.Should().ThrowAsync<Exception>().WithMessage("数据库异常");
    }
}
