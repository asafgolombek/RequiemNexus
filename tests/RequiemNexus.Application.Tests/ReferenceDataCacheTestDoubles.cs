using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Test doubles for <see cref="IReferenceDataCache"/> when the full host warmup pipeline is not used.
/// </summary>
internal static class ReferenceDataCacheTestDoubles
{
    /// <summary>
    /// Returns a cache that reports initialized with empty reference lists so services fall back to EF for catalog rows.
    /// </summary>
    public static IReferenceDataCache EmptyButInitialized()
    {
        var mock = new Mock<IReferenceDataCache>();
        mock.SetupGet(c => c.IsInitialized).Returns(true);
        mock.SetupGet(c => c.ReferenceClans).Returns([]);
        mock.SetupGet(c => c.ReferenceDisciplines).Returns([]);
        mock.SetupGet(c => c.ReferenceMerits).Returns([]);
        mock.SetupGet(c => c.CovenantDefinitions).Returns([]);
        mock.SetupGet(c => c.SorceryRiteDefinitions).Returns([]);
        mock.SetupGet(c => c.ScaleDefinitions).Returns([]);
        mock.SetupGet(c => c.CoilDefinitions).Returns([]);
        mock.SetupGet(c => c.BloodlineDefinitions).Returns([]);
        mock.SetupGet(c => c.CovenantDefinitionMerits).Returns([]);
        mock.SetupGet(c => c.DevotionDefinitions).Returns([]);
        mock.Setup(c => c.LoadFromDatabaseAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    /// <summary>
    /// Builds a real <see cref="ReferenceDataCache"/> populated from the current <paramref name="context"/> snapshot.
    /// </summary>
    public static async Task<IReferenceDataCache> WarmFromAsync(ApplicationDbContext context)
    {
        var cache = new ReferenceDataCache();
        await cache.LoadFromDatabaseAsync(context).ConfigureAwait(false);
        return cache;
    }
}
