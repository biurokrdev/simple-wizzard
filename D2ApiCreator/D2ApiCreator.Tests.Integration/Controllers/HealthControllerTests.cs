using System.Net;
using FluentAssertions;
using Xunit;

namespace D2ApiCreator.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for Health Controller
/// </summary>
public class HealthControllerTests : IClassFixture<Common.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(Common.CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

