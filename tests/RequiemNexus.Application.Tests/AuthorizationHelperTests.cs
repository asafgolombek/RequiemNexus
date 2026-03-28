using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class AuthorizationHelperTests
{
    private sealed class TestDbFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly ApplicationDbContext _ctx;

        public TestDbFactory(ApplicationDbContext ctx) => _ctx = ctx;

        public ApplicationDbContext CreateDbContext() => _ctx;
    }

    [Fact]
    public async Task IsStorytellerAsync_IsST_ReturnsTrue()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "st",
                UserName = "st",
                NormalizedUserName = "ST",
                Email = "st@test",
                NormalizedEmail = "ST@TEST",
                EmailConfirmed = true,
            });
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
        await ctx.SaveChangesAsync();

        var helper = new AuthorizationHelper(new TestDbFactory(ctx), NullLogger<AuthorizationHelper>.Instance);

        Assert.True(await helper.IsStorytellerAsync(1, "st"));
    }

    [Fact]
    public async Task IsStorytellerAsync_IsNotST_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "st",
                UserName = "st",
                NormalizedUserName = "ST",
                Email = "st@test",
                NormalizedEmail = "ST@TEST",
                EmailConfirmed = true,
            });
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
        await ctx.SaveChangesAsync();

        var helper = new AuthorizationHelper(new TestDbFactory(ctx), NullLogger<AuthorizationHelper>.Instance);

        Assert.False(await helper.IsStorytellerAsync(1, "other"));
    }

    [Fact]
    public async Task IsStorytellerAsync_CampaignNotFound_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        var helper = new AuthorizationHelper(new TestDbFactory(ctx), NullLogger<AuthorizationHelper>.Instance);

        Assert.False(await helper.IsStorytellerAsync(999, "st"));
    }
}
