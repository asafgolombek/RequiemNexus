using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("RequiemNexus.Data")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddCascadingAuthenticationState();
// We temporarily bypass cookies/JWT for a second until we set up OIDC
// Just wire the basic Identity for now
builder.Services.AddAuthentication();
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
