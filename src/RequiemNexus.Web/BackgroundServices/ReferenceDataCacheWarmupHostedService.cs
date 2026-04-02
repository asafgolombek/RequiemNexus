using RequiemNexus.Application.Services;
using RequiemNexus.Data;

namespace RequiemNexus.Web.BackgroundServices;

/// <summary>
/// Warms <see cref="ReferenceDataCache"/> once at host startup so reference-catalog reads avoid per-request database round-trips.
/// </summary>
internal sealed class ReferenceDataCacheWarmupHostedService(
    IServiceScopeFactory scopeFactory,
    ReferenceDataCache cache,
    ILogger<ReferenceDataCacheWarmupHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await cache.LoadFromDatabaseAsync(db, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Reference data cache warmed: {ClanCount} clans, {DisciplineCount} disciplines, {MeritCount} merits, {RiteCount} rites, {CoilCount} coils.",
            cache.ReferenceClans.Count,
            cache.ReferenceDisciplines.Count,
            cache.ReferenceMerits.Count,
            cache.SorceryRiteDefinitions.Count,
            cache.CoilDefinitions.Count);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
