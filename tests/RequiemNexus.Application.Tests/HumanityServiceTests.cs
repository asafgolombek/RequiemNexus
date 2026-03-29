using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Domain.Events;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class HumanityServiceTests
{
    private static ApplicationDbContext CreateEmptyContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void GetEffectiveMaxHumanity_NoCruac_Returns10()
    {
        using var ctx = CreateEmptyContext();
        var character = new Character();
        var service = CreateHumanityService(ctx, Mock.Of<IAuthorizationHelper>(), Mock.Of<IDomainEventDispatcher>());

        Assert.Equal(10, service.GetEffectiveMaxHumanity(character));
    }

    [Fact]
    public void GetEffectiveMaxHumanity_CruacDot3_Returns7()
    {
        using var ctx = CreateEmptyContext();
        var cruc = new Discipline { Id = 1, Name = "Crúac" };
        var character = new Character();
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 1, Rating = 3, Discipline = cruc });

        var service = CreateHumanityService(ctx, Mock.Of<IAuthorizationHelper>(), Mock.Of<IDomainEventDispatcher>());

        Assert.Equal(7, service.GetEffectiveMaxHumanity(character));
    }

    [Fact]
    public async Task EvaluateStains_BelowThreshold_NoEvent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "u1",
                UserName = "u1",
                NormalizedUserName = "U1",
                Email = "u1@test",
                NormalizedEmail = "U1@TEST",
                EmailConfirmed = true,
            });
        ctx.Characters.Add(
            new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                Humanity = 7,
                HumanityStains = 1,
            });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "u1", It.IsAny<string>())).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var service = CreateHumanityService(ctx, auth.Object, dispatcher.Object);

        await service.EvaluateStainsAsync(1, "u1");

        dispatcher.Verify(d => d.Dispatch(It.IsAny<DegenerationCheckRequiredEvent>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateStains_AtThreshold_DispatchesEvent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "u1",
                UserName = "u1",
                NormalizedUserName = "U1",
                Email = "u1@test",
                NormalizedEmail = "U1@TEST",
                EmailConfirmed = true,
            });
        ctx.Characters.Add(
            new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                Humanity = 7,
                HumanityStains = 7,
            });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "u1", It.IsAny<string>())).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var service = CreateHumanityService(ctx, auth.Object, dispatcher.Object);

        await service.EvaluateStainsAsync(1, "u1");

        dispatcher.Verify(
            d => d.Dispatch(It.Is<DegenerationCheckRequiredEvent>(e =>
                e.CharacterId == 1 && e.Reason == DegenerationReason.StainsThreshold)),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateStains_AboveThresholdTwice_DispatchesEventTwice()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ApplicationDbContext(options);
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "u1",
                UserName = "u1",
                NormalizedUserName = "U1",
                Email = "u1@test",
                NormalizedEmail = "U1@TEST",
                EmailConfirmed = true,
            });
        ctx.Characters.Add(
            new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                Humanity = 5,
                HumanityStains = 6,
            });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "u1", It.IsAny<string>())).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var service = CreateHumanityService(ctx, auth.Object, dispatcher.Object);

        await service.EvaluateStainsAsync(1, "u1");
        await service.EvaluateStainsAsync(1, "u1");

        dispatcher.Verify(
            d => d.Dispatch(It.Is<DegenerationCheckRequiredEvent>(e =>
                e.CharacterId == 1 && e.Reason == DegenerationReason.StainsThreshold)),
            Times.Exactly(2));
    }

    private static HumanityService CreateHumanityService(
        ApplicationDbContext ctx,
        IAuthorizationHelper auth,
        IDomainEventDispatcher dispatcher)
    {
        return new HumanityService(
            ctx,
            auth,
            dispatcher,
            Mock.Of<IDiceService>(),
            Mock.Of<ISessionService>(),
            Mock.Of<IConditionService>(),
            Mock.Of<ILogger<HumanityService>>());
    }
}
