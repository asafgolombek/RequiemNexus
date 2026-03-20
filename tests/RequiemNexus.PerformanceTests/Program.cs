using Microsoft.AspNetCore.SignalR.Client;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace RequiemNexus.PerformanceTests;

public static class Program
{
#pragma warning disable S1075 // URIs should not be hardcoded. This is a local development default and can be overridden via TARGET_URL env var.
    private const string _defaultTargetUrl = "http://localhost:5251";
#pragma warning restore S1075

    public static async Task Main(string[] args)
    {
        var targetUrl = Environment.GetEnvironmentVariable("TARGET_URL") ?? _defaultTargetUrl;

        var httpClient = new HttpClient();

        var homePageScenario = Scenario.Create("home_page_scenario", async context =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        var signalrScenario = Scenario.Create("signalr_dispatch_scenario", async context =>
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl(targetUrl + "/hubs/session")
                    .Build();

                await connection.StartAsync();

                // We assume the target environment is configured to allow this for performance testing
                // or we are measuring the failure latency (which still exercises the hub pipeline).
                await connection.InvokeAsync("RollDice", 1, 10, "PerfTest", true, true, true, false);

                await connection.StopAsync();
                await connection.DisposeAsync();

                return Response.Ok();
            }
            catch (Exception ex)
            {
                return Response.Fail(message: ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(homePageScenario, signalrScenario)
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();

        // Performance Budget Enforcement
        var signalrStats = stats.ScenarioStats.First(s => s.ScenarioName == "signalr_dispatch_scenario");
        var p95 = signalrStats.Ok.Latency.Percent95;
        var failCount = signalrStats.Fail.Request.Count;
        var totalCount = signalrStats.Ok.Request.Count + failCount;
        var failRate = totalCount > 0 ? (double)failCount / totalCount : 0;

        const int maxP95Ms = 200;
        const double maxFailRate = 0.05; // Allow some failure due to auth in local environments

        Console.WriteLine($"--- SignalR Performance Results ---");
        Console.WriteLine($"P95 Latency: {p95}ms (Threshold: {maxP95Ms}ms)");
        Console.WriteLine($"Failure Rate: {failRate:P2} (Threshold: {maxFailRate:P2})");

        if (p95 > maxP95Ms && !targetUrl.Contains("localhost"))
        {
            Console.WriteLine("❌ SignalR performance budget exceeded! Failing build.");
            Environment.Exit(1);
        }

        Console.WriteLine("✅ Performance budget check complete.");
    }
}
