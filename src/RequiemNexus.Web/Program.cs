using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Components;
using Serilog;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Console()
                 .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("RequiemNexus.Data")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = "Requiem Nexus";
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>() ?? new HashSet<string> { "https://localhost:5001", "https://localhost:7154" }; // Defaults for generic dev
    options.TimestampDriftTolerance = 300000;
});

// Application Services
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterService, RequiemNexus.Application.Services.CharacterManagementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IClanService, RequiemNexus.Application.Services.ClanService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IMeritService, RequiemNexus.Application.Services.MeritService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IDisciplineService, RequiemNexus.Application.Services.DisciplineService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAdvancementService, RequiemNexus.Application.Services.AdvancementService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IAuditLogService, RequiemNexus.Application.Services.AuditLogService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.IUserDataExportService, RequiemNexus.Application.Services.UserDataExportService>();
builder.Services.AddHostedService<RequiemNexus.Web.Services.AccountDeletionCleanupService>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.IExperienceCostRules, RequiemNexus.Domain.ExperienceCostRules>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.ICharacterCreationRules, RequiemNexus.Domain.CharacterCreationRules>();
builder.Services.AddSingleton<RequiemNexus.Domain.Contracts.IDiceService, RequiemNexus.Domain.Services.DiceService>();
builder.Services.AddScoped<RequiemNexus.Application.Contracts.ICharacterExportService, RequiemNexus.Application.Services.CharacterExportService>();

builder.Services.AddSingleton<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore, RequiemNexus.Web.Services.DatabaseTicketStore>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore>((options, store) =>
    {
        options.SessionStore = store;
        options.ExpireTimeSpan = TimeSpan.FromDays(14); // Remember Me duration
        options.SlidingExpiration = true;
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
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<RequiemNexus.Web.Services.SmtpEmailSender>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.SmtpEmailSender>());
builder.Services.AddScoped<RequiemNexus.Web.Services.IRequiemEmailService>(sp => sp.GetRequiredService<RequiemNexus.Web.Services.SmtpEmailSender>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.InitializeAsync(context);

    if (app.Environment.IsDevelopment())
    {
        await TestDbInitializer.InitializeAsync(context);
    }
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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
