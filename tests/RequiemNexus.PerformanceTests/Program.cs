using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace RequiemNexus.PerformanceTests;

public static class Program
{
#pragma warning disable S1075 // URIs should not be hardcoded. This is a local development default and can be overridden via TARGET_URL env var.
    private const string _defaultTargetUrl = "http://localhost:5000";
#pragma warning restore S1075


    public static void Main(string[] args)
    {
        var targetUrl = Environment.GetEnvironmentVariable("TARGET_URL") ?? _defaultTargetUrl;

        var httpClient = new HttpClient();

        var scenario = Scenario.Create("home_page_scenario", async context =>
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

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();
    }
}

