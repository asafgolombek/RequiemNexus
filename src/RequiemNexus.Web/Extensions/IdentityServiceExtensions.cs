using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Extensions;

/// <summary>
/// Identity, authentication, and email service registrations.
/// </summary>
internal static class IdentityServiceExtensions
{
    internal static void AddIdentityAndAuthServices(this WebApplicationBuilder builder)
    {
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
            .AddEntityFrameworkStores<RequiemNexus.Data.ApplicationDbContext>()
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
    }
}
