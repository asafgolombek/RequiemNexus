using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Events.Handlers;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Verifies <see cref="DegenerationCheckRequiredEventHandler"/> pushes <see cref="ChronicleUpdateDto"/> for Storyteller Glimpse
/// (Phase 17 / P4 — Necromancy breaking point and other degeneration triggers share this path).
/// </summary>
public sealed class DegenerationCheckRequiredChronicleBroadcastTests
{
    [Fact]
    public async Task Handle_BroadcastsChronicleDegenerationAlert_WhenCharacterInCampaign()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        try
        {
            var sessionMock = new Mock<ISessionService>();
            sessionMock
                .Setup(s => s.BroadcastChronicleUpdateAsync(It.IsAny<ChronicleUpdateDto>()))
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));
            services.AddSingleton(sessionMock.Object);
            services.AddSingleton<ILogger<DegenerationCheckRequiredEventHandler>>(
                NullLogger<DegenerationCheckRequiredEventHandler>.Instance);
            services.AddScoped<DegenerationCheckRequiredEventHandler>();
            services.AddScoped<IDomainEventHandler<DegenerationCheckRequiredEvent>>(sp =>
                sp.GetRequiredService<DegenerationCheckRequiredEventHandler>());
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            await using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            ApplicationDbContext ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ctx.Database.EnsureCreatedAsync();

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
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "u1" });
            ctx.Characters.Add(
                new Character
                {
                    Id = 1,
                    ApplicationUserId = "u1",
                    Name = "Mort",
                    CampaignId = 1,
                    Humanity = 8,
                    CurrentVitae = 5,
                    MaxVitae = 10,
                    CurrentWillpower = 4,
                    MaxWillpower = 5,
                    BloodPotency = 2,
                });
            ctx.CharacterAttributes.Add(
                new CharacterAttribute
                {
                    CharacterId = 1,
                    Name = nameof(AttributeId.Resolve),
                    Rating = 4,
                    Category = TraitCategory.Mental,
                });
            await ctx.SaveChangesAsync();

            IDomainEventDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            dispatcher.Dispatch(new DegenerationCheckRequiredEvent(1, DegenerationReason.NecromancyActivation));

            sessionMock.Verify(
                s => s.BroadcastChronicleUpdateAsync(It.Is<ChronicleUpdateDto>(d =>
                    d.ChronicleId == 1
                    && d.DegenerationCheckRequired != null
                    && d.DegenerationCheckRequired.CharacterId == 1
                    && d.DegenerationCheckRequired.CharacterName == "Mort"
                    && d.DegenerationCheckRequired.Humanity == 8
                    && d.DegenerationCheckRequired.ResolveRating == 4)),
                Times.Once);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Handle_DoesNotBroadcast_WhenCharacterNotInCampaign()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        try
        {
            var sessionMock = new Mock<ISessionService>();
            sessionMock
                .Setup(s => s.BroadcastChronicleUpdateAsync(It.IsAny<ChronicleUpdateDto>()))
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));
            services.AddSingleton(sessionMock.Object);
            services.AddSingleton<ILogger<DegenerationCheckRequiredEventHandler>>(
                NullLogger<DegenerationCheckRequiredEventHandler>.Instance);
            services.AddScoped<DegenerationCheckRequiredEventHandler>();
            services.AddScoped<IDomainEventHandler<DegenerationCheckRequiredEvent>>(sp =>
                sp.GetRequiredService<DegenerationCheckRequiredEventHandler>());
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            await using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            ApplicationDbContext ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ctx.Database.EnsureCreatedAsync();

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
                    Name = "Solo",
                    CampaignId = null,
                    Humanity = 8,
                    CurrentVitae = 5,
                    MaxVitae = 10,
                    CurrentWillpower = 4,
                    MaxWillpower = 5,
                    BloodPotency = 2,
                });
            ctx.CharacterAttributes.Add(
                new CharacterAttribute
                {
                    CharacterId = 1,
                    Name = nameof(AttributeId.Resolve),
                    Rating = 3,
                    Category = TraitCategory.Mental,
                });
            await ctx.SaveChangesAsync();

            IDomainEventDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            dispatcher.Dispatch(new DegenerationCheckRequiredEvent(1, DegenerationReason.NecromancyActivation));

            sessionMock.Verify(
                s => s.BroadcastChronicleUpdateAsync(It.IsAny<ChronicleUpdateDto>()),
                Times.Never);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
