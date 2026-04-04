using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class ReferenceDataCacheFlushTests
{
    [Fact]
    public async Task FlushAsync_Reloads_Without_Error_On_Empty_Database()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ref-cache-flush-{Guid.NewGuid():N}")
            .Options;
        await using var ctx = new ApplicationDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var cache = new ReferenceDataCache();
        await cache.LoadFromDatabaseAsync(ctx);
        Assert.True(cache.IsInitialized);

        await cache.FlushAsync(ctx);
        Assert.True(cache.IsInitialized);
    }
}
