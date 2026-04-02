using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Seeding;
using RequiemNexus.Web.Components;
using RequiemNexus.Web.Extensions;
using RequiemNexus.Web.Hubs;
using Serilog;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

bool isMigrateOnly = args.Contains("--migrate-only");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Playwright / WebApplicationFactory run the app from the test output directory; load the Web project's static web asset manifest so Blazor and scoped CSS resolve.
if (builder.Environment.IsEnvironment("Testing"))
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

builder.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.AddIdentityAndAuthServices();
builder.AddRequiemRateLimiting();

WebApplication app = builder.Build();

bool runMigrations = isMigrateOnly || !builder.Environment.IsEnvironment("Testing");

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    IEnumerable<ISeeder> seeders = scope.ServiceProvider.GetServices<ISeeder>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DbInitializer.InitializeAsync(context, roleManager, app.Logger, seeders, runMigrations);
    }

    if (app.Environment.IsDevelopment())
    {
        await TestDbInitializer.InitializeAsync(context);
    }
}

if (isMigrateOnly)
{
    Serilog.Log.Information("Migration complete. Exiting due to --migrate-only flag.");
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.MapPost("/api/characters/{characterId:int}/perception-roll", async (
        int characterId,
        bool useAwareness,
        int penaltyDice,
        RequiemNexus.Application.Contracts.IPerceptionRollService perceptionService,
        System.Security.Claims.ClaimsPrincipal user) =>
    {
        string? userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await perceptionService.RollPerceptionAsync(characterId, useAwareness, penaltyDice, userId);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization();

app.MapGet("/api/sessions/{chronicleId:int}/state", async (
    int chronicleId,
    RequiemNexus.Application.RealTime.ISessionService sessionService,
    RequiemNexus.Application.RealTime.ISessionAuthorizationService authService,
    System.Security.Claims.ClaimsPrincipal user) =>
{
    string? userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId) || !await authService.IsMemberAsync(userId, chronicleId))
    {
        return Results.Forbid();
    }

    var state = await sessionService.GetSessionStateAsync(chronicleId);
    return state != null ? Results.Ok(state) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/rolls/{slug}", async (string slug, RequiemNexus.Application.Contracts.IPublicRollService rollService) =>
{
    var roll = await rollService.GetRollBySlugAsync(slug);
    return roll != null ? Results.Ok(roll) : Results.NotFound();
}).AllowAnonymous().RequireRateLimiting("public-rolls");

app.MapHub<SessionHub>("/hubs/session").RequireRateLimiting("signalr");

app.MapStaticAssets();
app.MapPost("/Account/Logout", async (
    SignInManager<ApplicationUser> signInManager,
    [Microsoft.AspNetCore.Mvc.FromForm] string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect(returnUrl ?? "/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
