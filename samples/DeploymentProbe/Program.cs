using System.Reflection;

namespace DeploymentProbe;

public class Program
{
    public const string ServiceName = "DeploymentProbe";

    public static async Task Main(string[] args)
    {
        await using var app = BuildApp(args);
        await app.RunAsync();
    }

    public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();
        configure?.Invoke(builder);
        var app = builder.Build();
        var environment = app.Environment.EnvironmentName;
        var version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
        app.MapGet("/version", () => Results.Ok(new { service = ServiceName, environment, version }));

        app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation(
            "Service started: {ServiceName} {Environment} {Version}", ServiceName, environment, version));
        app.Lifetime.ApplicationStopped.Register(() => app.Logger.LogInformation(
            "Service stopped: {ServiceName} {Environment} {Version}", ServiceName, environment, version));
        return app;
    }
}
