using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Data.Seeding;
using RequiemNexus.Web.Hubs;
using Serilog;
using StackExchange.Redis;

namespace RequiemNexus.Web.Extensions;

/// <summary>
/// Infrastructure service registrations: observability, database, Redis, SignalR, and real-time services.
/// </summary>
internal static class InfrastructureServiceExtensions
{
    internal static void AddInfrastructureServices(this WebApplicationBuilder builder)
    {
        string? sentryDsn = builder.Configuration["Sentry:Dsn"];
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

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        string? dbPassword = builder.Configuration["DB:Password"];
        if (!string.IsNullOrEmpty(dbPassword))
        {
            Npgsql.NpgsqlConnectionStringBuilder csb = new(connectionString) { Password = dbPassword };
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

        builder.Services.AddRequiemDataSeeders();

        builder.Services.AddFido2(options =>
        {
            options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
            options.ServerName = "Requiem Nexus";
            options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>()
                ?? new HashSet<string> { "https://localhost:5001", "https://localhost:7154" }; // Defaults for generic dev
            options.TimestampDriftTolerance = 300000;
        });

        string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

        // Allow startup when Redis is temporarily unavailable; multiplexer will retry in the background.
        if (!redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
        {
            redisConnectionString += ",abortConnect=false";
        }

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        builder.Services.AddSingleton<RealTimeMetrics>();
        builder.Services.AddSingleton<RelationshipWebMetrics>();
        builder.Services.AddSingleton<ISessionStateRepository, SessionStateRepository>();
        builder.Services.AddScoped<ISessionAuthorizationService, SessionAuthorizationService>();
        builder.Services.AddScoped<ISessionService, SessionService>();
        builder.Services.AddSingleton<ISessionPublisher, SessionPublisher>();
        builder.Services.AddScoped<RequiemNexus.Web.Services.SessionClientService>();
        builder.Services.AddScoped<RequiemNexus.Web.Services.ISessionEventBus>(sp =>
            sp.GetRequiredService<RequiemNexus.Web.Services.SessionClientService>());

        builder.Services.AddSingleton<IHubFilter>(new RequiemNexus.Web.Hubs.Filters.RateLimitingFilter(30));

        ISignalRServerBuilder signalrBuilder = builder.Services.AddSignalR();

        // Phase 20: initiative / hub payloads can exceed the default 32 KB cap; Redis backplane above supports multi-instance scale-out.
        signalrBuilder.AddHubOptions<SessionHub>(o => o.MaximumReceiveMessageSize = 64 * 1024);

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            signalrBuilder.AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("RequiemNexus");
            });
        }
    }
}
