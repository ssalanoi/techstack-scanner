using Api.Data.Entities;
using Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Api.Tests;

public class OutdatedDependencyServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly OutdatedDependencyService _service;

    public OutdatedDependencyServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _service = new OutdatedDependencyService(
            NullLogger<OutdatedDependencyService>.Instance,
            _httpClientFactoryMock.Object);
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.5.0", "1.6.0", true)]
    [InlineData("1.5.2", "1.5.3", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("^1.0.0", "2.0.0", true)]
    [InlineData("~1.5.0", "1.6.0", true)]
    [InlineData(">=1.0.0", "2.0.0", true)]
    [InlineData("==1.0.0", "1.0.0", false)]
    [InlineData("v1.0.0", "1.0.1", true)]
    public void IsVersionOutdated_ComparesVersionsCorrectly(string current, string latest, bool expectedOutdated)
    {
        var result = _service.IsVersionOutdated(current, latest);
        Assert.Equal(expectedOutdated, result);
    }

    [Fact]
    public void IsVersionOutdated_HandlesMajorVersionDifference()
    {
        Assert.True(_service.IsVersionOutdated("1.9.9", "2.0.0"));
        Assert.False(_service.IsVersionOutdated("2.0.0", "1.9.9"));
    }

    [Fact]
    public void IsVersionOutdated_HandlesMinorVersionDifference()
    {
        Assert.True(_service.IsVersionOutdated("1.5.9", "1.6.0"));
        Assert.False(_service.IsVersionOutdated("1.6.0", "1.5.9"));
    }

    [Fact]
    public void IsVersionOutdated_HandlesPatchVersionDifference()
    {
        Assert.True(_service.IsVersionOutdated("1.5.2", "1.5.3"));
        Assert.False(_service.IsVersionOutdated("1.5.3", "1.5.2"));
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_UpdatesNpmPackages()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "react",
                Version = "17.0.0",
                Detector = "npm",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var npmResponse = new { version = "18.2.0" };
        var responseContent = new StringContent(
            JsonSerializer.Serialize(npmResponse),
            Encoding.UTF8,
            "application/json");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("registry.npmjs.org")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.True(finding.IsOutdated);
        Assert.Equal("18.2.0", finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_UpdatesNuGetPackages()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "Serilog",
                Version = "2.0.0",
                Detector = "nuget",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var nugetResponse = new { versions = new[] { "2.0.0", "2.5.0", "3.0.0" } };
        var responseContent = new StringContent(
            JsonSerializer.Serialize(nugetResponse),
            Encoding.UTF8,
            "application/json");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api.nuget.org")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.True(finding.IsOutdated);
        Assert.Equal("3.0.0", finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_UpdatesPyPiPackages()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "fastapi",
                Version = "==0.100.0",
                Detector = "pip",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var pypiResponse = new { info = new { version = "0.115.0" } };
        var responseContent = new StringContent(
            JsonSerializer.Serialize(pypiResponse),
            Encoding.UTF8,
            "application/json");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("pypi.org")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.True(finding.IsOutdated);
        Assert.Equal("0.115.0", finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_UpdatesRubyGemsPackages()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "rails",
                Version = "6.0.0",
                Detector = "gem",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var gemResponse = new { version = "7.1.0" };
        var responseContent = new StringContent(
            JsonSerializer.Serialize(gemResponse),
            Encoding.UTF8,
            "application/json");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("rubygems.org")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.True(finding.IsOutdated);
        Assert.Equal("7.1.0", finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_HandlesHttpErrors()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "nonexistent-package",
                Version = "1.0.0",
                Detector = "npm",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert - Should not throw and should not update the finding
        var finding = findings[0];
        Assert.False(finding.IsOutdated);
        Assert.Null(finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_SkipsFindingsWithoutVersion()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "some-package",
                Version = null,
                Detector = "npm",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.False(finding.IsOutdated);
        Assert.Null(finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_SkipsUnsupportedDetectors()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "some-package",
                Version = "1.0.0",
                Detector = "unknown",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.False(finding.IsOutdated);
        Assert.Null(finding.LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_ProcessesMultipleFindingsInParallel()
    {
        // Arrange - Simplified to just test that we can process multiple findings
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "react",
                Version = "17.0.0",
                Detector = "npm",
                IsOutdated = false,
                LatestVersion = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "nonexistent",
                Version = "1.0.0",
                Detector = "unknown",  // Unsupported detector
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var url = request.RequestUri?.ToString() ?? "";
                if (url.Contains("react"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(new { version = "18.2.0" }), Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert - First finding should be updated, second should be skipped
        Assert.True(findings[0].IsOutdated);
        Assert.Equal("18.2.0", findings[0].LatestVersion);
        Assert.False(findings[1].IsOutdated);  // Unsupported detector
        Assert.Null(findings[1].LatestVersion);
    }

    [Fact]
    public async Task CheckAndUpdateOutdatedAsync_MarksAsNotOutdatedWhenVersionsMatch()
    {
        // Arrange
        var findings = new List<TechnologyFinding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScanId = Guid.NewGuid(),
                Name = "react",
                Version = "18.2.0",
                Detector = "npm",
                IsOutdated = false,
                LatestVersion = null
            }
        };

        var npmResponse = new { version = "18.2.0" };
        var responseContent = new StringContent(
            JsonSerializer.Serialize(npmResponse),
            Encoding.UTF8,
            "application/json");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _service.CheckAndUpdateOutdatedAsync(findings, CancellationToken.None);

        // Assert
        var finding = findings[0];
        Assert.False(finding.IsOutdated);
        Assert.Equal("18.2.0", finding.LatestVersion);
    }
}
