using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class VitaeServiceTests
{
    private static ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<IAuthorizationHelper> CreateAuthMock()
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return auth;
    }

    [Fact]
    public async Task SpendVitaeAsync_WhenVitaeReachesZero_DispatchesVitaeDepletedEvent()
    {
        using ApplicationDbContext ctx = CreateInMemoryContext(nameof(SpendVitaeAsync_WhenVitaeReachesZero_DispatchesVitaeDepletedEvent));
        ctx.Characters.Add(new Character
        {
            ApplicationUserId = "u1",
            Name = "V",
            MaxHealth = 5,
            CurrentHealth = 5,
            MaxWillpower = 3,
            CurrentWillpower = 3,
            MaxVitae = 5,
            CurrentVitae = 1,
        });
        await ctx.SaveChangesAsync();

        VitaeDepletedEvent? dispatched = null;
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.Dispatch(It.IsAny<VitaeDepletedEvent>()))
            .Callback((VitaeDepletedEvent e) => dispatched = e);

        var sut = new VitaeService(
            ctx,
            CreateAuthMock().Object,
            dispatcher.Object,
            new Mock<ILogger<VitaeService>>().Object);

        Result<int> result = await sut.SpendVitaeAsync(1, "u1", 1, "test");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.NotNull(dispatched);
        Assert.Equal(1, dispatched.CharacterId);
    }

    [Fact]
    public async Task SpendVitaeAsync_WhenVitaeStaysPositive_DoesNotDispatch()
    {
        using ApplicationDbContext ctx = CreateInMemoryContext(nameof(SpendVitaeAsync_WhenVitaeStaysPositive_DoesNotDispatch));
        ctx.Characters.Add(new Character
        {
            ApplicationUserId = "u1",
            Name = "V",
            MaxHealth = 5,
            CurrentHealth = 5,
            MaxWillpower = 3,
            CurrentWillpower = 3,
            MaxVitae = 5,
            CurrentVitae = 3,
        });
        await ctx.SaveChangesAsync();

        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new VitaeService(
            ctx,
            CreateAuthMock().Object,
            dispatcher.Object,
            new Mock<ILogger<VitaeService>>().Object);

        Result<int> result = await sut.SpendVitaeAsync(1, "u1", 1, "test");

        Assert.True(result.IsSuccess);
        dispatcher.Verify(
            d => d.Dispatch(It.IsAny<VitaeDepletedEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task SpendVitaeAsync_InsufficientVitae_ReturnsFailure()
    {
        using ApplicationDbContext ctx = CreateInMemoryContext(nameof(SpendVitaeAsync_InsufficientVitae_ReturnsFailure));
        ctx.Characters.Add(new Character
        {
            ApplicationUserId = "u1",
            Name = "V",
            MaxHealth = 5,
            CurrentHealth = 5,
            MaxWillpower = 3,
            CurrentWillpower = 3,
            MaxVitae = 5,
            CurrentVitae = 1,
        });
        await ctx.SaveChangesAsync();

        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new VitaeService(
            ctx,
            CreateAuthMock().Object,
            dispatcher.Object,
            new Mock<ILogger<VitaeService>>().Object);

        Result<int> result = await sut.SpendVitaeAsync(1, "u1", 2, "test");

        Assert.False(result.IsSuccess);
    }
}
