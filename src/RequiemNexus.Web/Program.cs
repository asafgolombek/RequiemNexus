using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Web.Components;
using RequiemNexus.Web.Hubs;
using Serilog;
using StackExchange.Redis;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

bool isMigrateOnly = args.Contains("--migrate-only");

var builder = WebApplication.CreateBuilder(args);

// Playwright / WebApplicationFactory run the app from the test output directory; load the Web project's static web asset manifest so Blazor and scoped CSS resolve.
if (builder.Environment.IsEnvironment("Testing"))
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = sentryDsn;
        o.Debug = builder.Environment.IsDevelopment();
        o.TracesSampleRate = 1.0;
    });
}

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Console()
                 .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddMeter("RequiemNexus.RealTime")
               .AddMeter("RequiemNexus.Relationships")
               .AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddOtlpExporter();
    });

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// In production, the DB password is injected as DB__Password by ECS from Secrets Manager
// (see ComputeStack.cs) to avoid embedding it in the CloudFormation template. Merge it here.
var dbPassword = builder.Configuration["DB:Password"];
if (!string.IsNullOrEmpty(dbPassword))
{
    var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { Password = dbPassword };
    connectionString = csb.ConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("RequiemNexus.Data"));

    // All schema changes are captured in migrations. This warning fires when EF Core's
    // internal model metadata drifts from the snapshot (e.g. annotation changes with no
    // DDL impact). It is safe to demote to a log entry rather than a startup exception.
    options.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
});

// Blazor Server: concurrent renders on the same circuit share one Scoped DbContext, which
// causes "A second operation was started" exceptions. IDbContextFactory lets services create
// a short-lived context per operation for read-only queries, avoiding the concurrency issue.
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(connectionString, b => b.MigrationsAssembly("RequiemNexus.Data"));
        options.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
    },
    ServiceLifetime.Scoped);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = "Requiem Nexus";
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>() ?? new HashSet<string> { "https://localhost:5001", "https://localhost:7154" }; // Defaults for generic dev
    options.TimestampDriftTolerance = 300000;
});

// Real-Time Services
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Allow startup when Redis is temporarily unavailable; multiplexer will retry in the background.
if (!redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
{
    redisConnectionString += ",abortConnect=false";
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<RealTimeMetrics>();
builder.Services.AddSingleton<RelationshipWebMetrics>();
builder.Services.AddSingleton<ISessionStateRepository, SessionStateRepository>();
builder.Services.AddScoped<ISessionAuthorizationService, SessionAuthorizationService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddSingleton<ISessionPublisher, SessionPublisher>();
builder.Services.AddScoped<RequiemNexus.Web.Services.SessionClientService>();

builder.Services.AddSingleton<IHubFilter>(new RequiemNexus.Web.Hubs.Filters.RateLimitingFilter(30));
var signalrBuilder = builder.Services.AddSignalR();

if (!builder.Environment.IsEnvironment("Testing"))
{
    signalrBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("RequiemNexus");
    });
}

// Application Services
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAuthorizationHelper, RequiemNexus.Application.Services.AuthorizationHelper>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICampaignService, RequiemNexus.Application.Services.CampaignService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IBeatLedgerService, RequiemNexus.Application.Services.BeatLedgerService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IConditionService, RequiemNexus.Application.Services.ConditionService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IStorytellerGlimpseService, RequiemNexus.Application.Services.StorytellerGlimpseService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterService, RequiemNexus.Application.Services.CharacterManagementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IClanService, RequiemNexus.Application.Services.ClanService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IMeritService, RequiemNexus.Application.Services.MeritService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IDisciplineService, RequiemNexus.Application.Services.DisciplineService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAdvancementService, RequiemNexus.Application.Services.AdvancementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAuditLogService, RequiemNexus.Application.Services.AuditLogService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IUserDataExportService, RequiemNexus.Application.Services.UserDataExportService>();
builder.Services.AddHostedService<RequiemNexus.Web.Services.AccountDeletionCleanupService>();
builder.Services.AddHostedService<RequiemNexus.Web.BackgroundServices.SessionTerminationService>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.IExperienceCostRules, RequiemNexus.Domain.ExperienceCostRules>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.ICharacterCreationRules, RequiemNexus.Domain.CharacterCreationRules>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.IConditionRules, RequiemNexus.Domain.ConditionRules>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.IDiceService, RequiemNexus.Domain.Services.DiceService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterExportService, RequiemNexus.Application.Services.CharacterExportService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IEncounterService, RequiemNexus.Application.Services.EncounterService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IEncounterPrepService, RequiemNexus.Application.Services.EncounterPrepService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.INpcCombatService, RequiemNexus.Application.Services.NpcCombatService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IEncounterTemplateService, RequiemNexus.Application.Services.EncounterTemplateService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IKindredLineageService, RequiemNexus.Application.Services.KindredLineageService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IBloodBondService, RequiemNexus.Application.Services.BloodBondService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IPredatoryAuraService, RequiemNexus.Application.Services.PredatoryAuraService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IGhoulManagementService, RequiemNexus.Application.Services.GhoulManagementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IPerceptionRollService, RequiemNexus.Application.Services.PerceptionRollService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICityFactionService, RequiemNexus.Application.Services.CityFactionService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IChronicleNpcService, RequiemNexus.Application.Services.ChronicleNpcService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ISocialManeuveringService, RequiemNexus.Application.Services.SocialManeuveringService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IFeedingTerritoryService, RequiemNexus.Application.Services.FeedingTerritoryService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IFactionRelationshipService, RequiemNexus.Application.Services.FactionRelationshipService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.INpcStatBlockService, RequiemNexus.Application.Services.NpcStatBlockService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterNoteService, RequiemNexus.Application.Services.CharacterNoteService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterAssetService, RequiemNexus.Application.Services.CharacterAssetService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAssetProcurementService, RequiemNexus.Application.Services.AssetProcurementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IBloodlineService, RequiemNexus.Application.Services.BloodlineService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICovenantService, RequiemNexus.Application.Services.CovenantService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ISorceryService, RequiemNexus.Application.Services.SorceryService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICoilService, RequiemNexus.Application.Services.CoilService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterMeritService, RequiemNexus.Application.Services.CharacterMeritService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterDisciplineService, RequiemNexus.Application.Services.CharacterDisciplineService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IDevotionService, RequiemNexus.Application.Services.DevotionService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IModifierService, RequiemNexus.Application.Services.ModifierService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IDerivedStatService, RequiemNexus.Application.Services.DerivedStatService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ITraitResolver, RequiemNexus.Application.Services.TraitResolver>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IDiceMacroService, RequiemNexus.Application.Services.DiceMacroService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IPublicRollService, RequiemNexus.Application.Services.PublicRollService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewDisciplineService, RequiemNexus.Application.Services.HomebrewDisciplineService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewMeritService, RequiemNexus.Application.Services.HomebrewMeritService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewClanService, RequiemNexus.Application.Services.HomebrewClanService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewPackService, RequiemNexus.Application.Services.HomebrewPackService>();

builder.Services.AddSingleton<RequiemNexus.Web.Services.ToastService>();
builder.Services.AddScoped<RequiemNexus.Web.Services.ScreenReaderAnnouncer>();
builder.Services.AddSingleton<RequiemNexus.Web.Services.CommandPaletteService>();
builder.Services.AddScoped<RequiemNexus.Web.Services.PlatformShortcutHintService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore, RequiemNexus.Web.Services.DatabaseTicketStore>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// External OAuth providers — chained via a second AddAuthentication() call (no-op on defaults)
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "not-configured";
    })
    .AddDiscord(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Discord:ClientId"] ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Discord:ClientSecret"] ?? "not-configured";
    });

builder.Services.AddOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore>((options, store) =>
    {
        options.SessionStore = store;
        options.ExpireTimeSpan = TimeSpan.FromDays(14); // Remember Me duration
        options.SlidingExpiration = true;
        options.LoginPath = "/";
        options.ReturnUrlParameter = "returnUrl";
    });

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;

        // Account Lockout policies
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Explicit password rules for Production
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Relaxed password rules are development-only
        if (builder.Environment.IsDevelopment())
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        }
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<RequiemNexus.Web.Services.TestEmailSink>();
    builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.TestEmailSink>());
    builder.Services.AddScoped<RequiemNexus.Web.Services.IRequiemEmailService>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.TestEmailSink>());
}
else
{
    builder.Services.AddScoped<RequiemNexus.Web.Services.SmtpEmailSender>();
    builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.SmtpEmailSender>());
    builder.Services.AddScoped<RequiemNexus.Web.Services.IRequiemEmailService>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.SmtpEmailSender>());
}

if (builder.Environment.IsEnvironment("Testing"))
{
    // E2E hits the login route many times from one IP; keep policy names so endpoint metadata resolves, but do not throttle.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("forgot-password", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("register", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("account-recovery", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("signalr", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("public-rolls", _ => RateLimitPartition.GetNoLimiter("test"));
    });
}
else
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Login: 10 attempts per 15 minutes per IP
        options.AddSlidingWindowLimiter("login", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(15);
            opt.SegmentsPerWindow = 3;
            opt.PermitLimit = 10;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Password reset: 5 requests per hour per IP to prevent email flooding
        options.AddSlidingWindowLimiter("forgot-password", opt =>
        {
            opt.Window = TimeSpan.FromHours(1);
            opt.SegmentsPerWindow = 4;
            opt.PermitLimit = 5;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Registration: 10 attempts per hour per IP
        options.AddSlidingWindowLimiter("register", opt =>
        {
            opt.Window = TimeSpan.FromHours(1);
            opt.SegmentsPerWindow = 4;
            opt.PermitLimit = 10;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Account recovery: 3 attempts per hour per IP
        options.AddSlidingWindowLimiter("account-recovery", opt =>
        {
            opt.Window = TimeSpan.FromHours(1);
            opt.SegmentsPerWindow = 4;
            opt.PermitLimit = 3;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // SignalR Hub: 30 requests per minute per authenticated user (fallback: per client IP for negotiate).
        options.AddPolicy("signalr", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 30,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));

        // Public Rolls: 10 requests per minute
        options.AddFixedWindowLimiter("public-rolls", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 10;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
    });
}

WebApplication app = builder.Build();

bool runMigrations = isMigrateOnly || !builder.Environment.IsEnvironment("Testing");

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DbInitializer.InitializeAsync(context, roleManager, runMigrations);
    }

    if (app.Environment.IsDevelopment())
    {
        await TestDbInitializer.InitializeAsync(context);
    }
}

if (isMigrateOnly)
{
    Log.Information("Migration complete. Exiting due to --migrate-only flag.");
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

app.MapGet("/api/sessions/{chronicleId:int}/state", async (int chronicleId, ISessionService sessionService, ISessionAuthorizationService authService, System.Security.Claims.ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId) || !await authService.IsMemberAsync(userId, chronicleId))
    {
        return Results.Forbid();
    }

    var state = await sessionService.GetSessionStateAsync(chronicleId);
    return state != null ? Results.Ok(state) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/rolls/{slug}", async (string slug, IPublicRollService rollService) =>
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
