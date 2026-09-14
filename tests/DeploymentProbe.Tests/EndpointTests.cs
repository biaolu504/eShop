using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace DeploymentProbe.Tests;

[TestClass]
public class EndpointTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task HealthReturnsHealthyJson()
    {
        await using var app = Program.BuildApp([], builder => builder.WebHost.UseTestServer());
        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/healthz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("healthy", body.RootElement.GetProperty("status").GetString());
        await app.StopAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    [DataRow("Production")]
    [DataRow("Development")]
    public async Task VersionReturnsServiceEnvironmentAndAssemblyVersion(string environment)
    {
        await using var app = Program.BuildApp(["--environment", environment],
            builder => builder.WebHost.UseTestServer());
        await app.StartAsync(TestContext.CancellationToken);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/version", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("DeploymentProbe", body.RootElement.GetProperty("service").GetString());
        Assert.AreEqual(environment, body.RootElement.GetProperty("environment").GetString());
        var expectedVersion = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.IsFalse(string.IsNullOrWhiteSpace(expectedVersion));
        Assert.AreEqual(expectedVersion, body.RootElement.GetProperty("version").GetString());
        await app.StopAsync(TestContext.CancellationToken);
    }
}
