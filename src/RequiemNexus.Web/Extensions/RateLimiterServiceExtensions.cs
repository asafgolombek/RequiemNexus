using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RequiemNexus.Web.Extensions;

/// <summary>
/// Rate limiter policy registrations.
/// </summary>
internal static class RateLimiterServiceExtensions
{
    internal static void AddRequiemRateLimiting(this WebApplicationBuilder builder)
    {
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
    }
}
