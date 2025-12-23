using Api.Options;
using Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests;

public class ScanServiceTests
{
    private readonly ScanService _service;

    public ScanServiceTests()
    {
        _service = new ScanService(NullLogger<ScanService>.Instance, Microsoft.Extensions.Options.Options.Create(new ScanOptions { MaxDepth = 3 }));
    }

    [Fact]
    public void ParsePackageJson_FindsDependencies()
    {
        const string content = "{\n  \"dependencies\": { \"react\": \"18.2.0\" },\n  \"devDependencies\": { \"vite\": \"6.0.0\", \"typescript\": \"5.6.2\" }\n}";

        var results = _service.ParsePackageJson(content, "package.json", Guid.Empty);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Name == "react" && r.Version == "18.2.0" && r.Detector == "npm");
        Assert.Contains(results, r => r.Name == "vite" && r.Version == "6.0.0");
        Assert.Contains(results, r => r.Name == "typescript" && r.Version == "5.6.2");
    }

    [Fact]
    public void ParseCsProj_FindsPackageReferences()
    {
        const string content = """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Serilog" Version="3.0.1" />
    <PackageReference Include="FluentAssertions">
      <Version>7.0.0</Version>
    </PackageReference>
  </ItemGroup>
</Project>
""";

        var results = _service.ParseCsProj(content, "api.csproj", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "Serilog" && r.Version == "3.0.1" && r.Detector == "nuget");
        Assert.Contains(results, r => r.Name == "FluentAssertions" && r.Version == "7.0.0");
    }

    [Fact]
    public void ParseGlobalJson_FindsSdkVersion()
    {
        const string content = "{ \"sdk\": { \"version\": \"9.0.100\" } }";

        var results = _service.ParseGlobalJson(content, "global.json", Guid.Empty);

        Assert.Single(results);
        Assert.Equal(".NET SDK", results[0].Name);
        Assert.Equal("9.0.100", results[0].Version);
    }

    [Fact]
    public void ParseRequirementsTxt_ParsesPythonRequirements()
    {
        const string content = "fastapi==0.115.0\nuvicorn>=0.20.0\n# comment\npydantic~=2.7.0";

        var results = _service.ParseRequirementsTxt(content, "requirements.txt", Guid.Empty);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Name == "fastapi" && r.Version == "==0.115.0");
        Assert.Contains(results, r => r.Name == "uvicorn" && r.Version == ">=0.20.0");
        Assert.Contains(results, r => r.Name == "pydantic" && r.Version == "~=2.7.0");
    }

    [Fact]
    public void ParsePyProjectToml_HandlesDependenciesBlocks()
    {
        const string content = """
[project]
dependencies = [
  "fastapi==0.115.0",
  "uvicorn>=0.20.0"
]

[tool.poetry.dependencies]
pydantic = "^2.7"
""";

        var results = _service.ParsePyProjectToml(content, "pyproject.toml", Guid.Empty);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Name == "fastapi");
        Assert.Contains(results, r => r.Name == "uvicorn");
        Assert.Contains(results, r => r.Name == "pydantic" && r.Version == "^2.7");
    }

    [Fact]
    public void ParseGemfile_FindsGems()
    {
        const string content = "source 'https://rubygems.org'\ngem \"rails\", \"7.1.3\"\ngem 'puma', '6.4.2'";

        var results = _service.ParseGemfile(content, "Gemfile", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "rails" && r.Version == "7.1.3");
        Assert.Contains(results, r => r.Name == "puma" && r.Version == "6.4.2");
    }

    [Fact]
    public void ParseGemfileLock_FindsLockedVersions()
    {
        const string content = """
GEM
  specs:
    rails (7.1.3)
    puma (6.4.2)
""";

        var results = _service.ParseGemfileLock(content, "Gemfile.lock", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "rails" && r.Version == "7.1.3");
        Assert.Contains(results, r => r.Name == "puma" && r.Version == "6.4.2");
    }

    [Fact]
    public void ParseGoMod_FindsDependencies()
    {
        const string content = """
module github.com/example/project

go 1.21

require (
  github.com/gorilla/mux v1.8.1
  golang.org/x/text v0.14.0
)
""";

        var results = _service.ParseGoMod(content, "go.mod", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "github.com/gorilla/mux" && r.Version == "v1.8.1");
        Assert.Contains(results, r => r.Name == "golang.org/x/text" && r.Version == "v0.14.0");
    }

    [Fact]
    public void ParseGoSum_FindsEntries()
    {
        const string content = "github.com/gorilla/mux v1.8.1 h1:abcd\ngolang.org/x/text v0.14.0 h1:efgh";

        var results = _service.ParseGoSum(content, "go.sum", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "github.com/gorilla/mux" && r.Version == "v1.8.1");
        Assert.Contains(results, r => r.Name == "golang.org/x/text" && r.Version == "v0.14.0");
    }

    [Fact]
    public void ParsePomXml_FindsDependencies()
    {
        const string content = """
<project>
  <dependencies>
    <dependency>
      <groupId>org.springframework.boot</groupId>
      <artifactId>spring-boot-starter-web</artifactId>
      <version>3.3.0</version>
    </dependency>
  </dependencies>
</project>
""";

        var results = _service.ParsePomXml(content, "pom.xml", Guid.Empty);

        Assert.Single(results);
        Assert.Equal("org.springframework.boot:spring-boot-starter-web", results[0].Name);
        Assert.Equal("3.3.0", results[0].Version);
    }

    [Fact]
    public void ParseBuildGradle_FindsCoordinates()
    {
        const string content = """
plugins { id "java" }
dependencies {
  implementation "org.springframework.boot:spring-boot-starter-web:3.3.0"
  testImplementation group: 'junit', name: 'junit', version: '4.13.2'
}
""";

        var results = _service.ParseBuildGradle(content, "build.gradle", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "org.springframework.boot:spring-boot-starter-web" && r.Version == "3.3.0");
        Assert.Contains(results, r => r.Name == "junit:junit" && r.Version == "4.13.2");
    }

    [Fact]
    public void ParseDockerfile_FindsBaseImage()
    {
        const string content = "FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build\nFROM node:20-alpine";

        var results = _service.ParseDockerfile(content, "Dockerfile", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "mcr.microsoft.com/dotnet/sdk" && r.Version == "9.0");
        Assert.Contains(results, r => r.Name == "node" && r.Version == "20-alpine");
    }

    [Fact]
    public void ParseDockerCompose_FindsImages()
    {
        const string content = """
services:
  api:
    image: ghcr.io/example/api:1.0.0
  web:
    image: nginx:1.25-alpine
""";

        var results = _service.ParseDockerCompose(content, "docker-compose.yml", Guid.Empty);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "ghcr.io/example/api" && r.Version == "1.0.0");
        Assert.Contains(results, r => r.Name == "nginx" && r.Version == "1.25-alpine");
    }
}
