using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace RequiemNexus.PerformanceTests;

public class Program
{
    public static void Main(string[] args)
    {
        var targetUrl = Environment.GetEnvironmentVariable("TARGET_URL") ?? "http://localhost:5000";
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

