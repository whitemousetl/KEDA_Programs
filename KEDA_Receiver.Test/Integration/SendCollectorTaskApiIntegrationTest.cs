using FluentAssertions;
using KEDA_Share.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace KEDA_Receiver.Test.Integration;

public class SendCollectorTaskApiIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _webAppFactory;
    private readonly HttpClient _client;

    public SendCollectorTaskApiIntegrationTest(WebApplicationFactory<Program> webAppFactory)
    {
        _webAppFactory = webAppFactory;
        _client = webAppFactory.CreateClient();
    }

    [Fact]
    public async Task SendCollectorTask_ReturnFailed_WhenWorkstaitonIsNull()
    {
        var response = await _client.PostAsync("/send_collector_task", null);

        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result.Message.Should().Contain("工作站为空"); // 根据实际返回信息断言
    }
}