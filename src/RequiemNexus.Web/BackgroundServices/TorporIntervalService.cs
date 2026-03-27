using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;

namespace RequiemNexus.Web.BackgroundServices;

/// <summary>
/// Periodically evaluates torpor starvation milestones for all characters in torpor.
/// </summary>
public sealed class TorporIntervalService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TorporIntervalService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TorporIntervalService> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        double intervalHours = _configuration.GetValue("Torpor:IntervalHours", 24.0);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIntervalPassAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during torpor interval pass.");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task RunIntervalPassAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ITorporService torporService = scope.ServiceProvider.GetRequiredService<ITorporService>();

        List<int> ids = await db.Characters
            .AsNoTracking()
            .Where(c => c.TorporSince != null)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (int id in ids)
        {
            await torporService.CheckStarvationIntervalAsync(id, cancellationToken);
        }
    }
}
