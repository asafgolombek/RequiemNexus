using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Phase 20: Fire-and-forget POSTs to a per-campaign Discord incoming webhook for session presence.
/// Uses a short-lived <see cref="HttpClient"/> per dispatch (low frequency; avoids extra package references).
/// </summary>
public sealed class DiscordWebhookSessionNotifier(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<DiscordWebhookSessionNotifier> logger) : ISessionDiscordNotifier
{
    private static readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(8);

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<DiscordWebhookSessionNotifier> _logger = logger;

    /// <inheritdoc />
    public void NotifySessionStarted(int chronicleId, string storytellerDisplayName)
    {
        StartSafe(() => PostEmbedAsync(
            chronicleId,
            "Session live",
            $"{DiscordWebhookEmbedText.SanitizeDisplayName(storytellerDisplayName)} started the chronicle session."));
    }

    /// <inheritdoc />
    public void NotifySessionEnded(int chronicleId, string storytellerDisplayName)
    {
        StartSafe(() => PostEmbedAsync(
            chronicleId,
            "Session ended",
            $"{DiscordWebhookEmbedText.SanitizeDisplayName(storytellerDisplayName)} closed the chronicle session."));
    }

    /// <inheritdoc />
    public void NotifyPlayerJoined(int chronicleId, string playerDisplayName)
    {
        StartSafe(() => PostEmbedAsync(
            chronicleId,
            "Player joined",
            $"{DiscordWebhookEmbedText.SanitizeDisplayName(playerDisplayName)} joined the session."));
    }

    /// <inheritdoc />
    public void NotifyPlayerLeft(int chronicleId, string playerUserId)
    {
        StartSafe(() => PostPlayerLeftAsync(chronicleId, playerUserId));
    }

    private void StartSafe(Func<Task> work)
    {
        _ = RunSafeAsync(work);
    }

    private async Task RunSafeAsync(Func<Task> work)
    {
        try
        {
            await work().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discord webhook background task failed");
        }
    }

    private async Task PostPlayerLeftAsync(int campaignId, string playerUserId)
    {
        string? webhookUrl;
        string? displayName;
        await using (ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false))
        {
            webhookUrl = await db.Campaigns.AsNoTracking()
                .Where(c => c.Id == campaignId)
                .Select(c => c.DiscordWebhookUrl)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                return;
            }

            displayName = await db.Users.AsNoTracking()
                .Where(u => u.Id == playerUserId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        await PostEmbedWithWebhookAsync(
            webhookUrl,
            campaignId,
            "Player left",
            $"{DiscordWebhookEmbedText.SanitizeDisplayName(displayName ?? "a player")} left the session.").ConfigureAwait(false);
    }

    private async Task PostEmbedAsync(int campaignId, string title, string description)
    {
        string? webhookUrl;
        await using (ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false))
        {
            webhookUrl = await db.Campaigns.AsNoTracking()
                .Where(c => c.Id == campaignId)
                .Select(c => c.DiscordWebhookUrl)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        await PostEmbedWithWebhookAsync(webhookUrl, campaignId, title, description).ConfigureAwait(false);
    }

    private async Task PostEmbedWithWebhookAsync(string webhookUrl, int campaignId, string title, string description)
    {
        using var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }, disposeHandler: true) { Timeout = _httpTimeout };
        var payload = new DiscordWebhookPayload
        {
            Embeds =
            [
                new DiscordEmbed
                {
                    Title = DiscordWebhookEmbedText.Truncate(title, 256),
                    Description = DiscordWebhookEmbedText.Truncate(description, 4096),
                    Color = 0x5865F2,
                },
            ],
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(webhookUrl, payload).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Discord webhook returned {StatusCode} for campaign {CampaignId}",
                (int)response.StatusCode,
                campaignId);
        }
    }

    private sealed class DiscordWebhookPayload
    {
        [JsonPropertyName("embeds")]
        public required DiscordEmbed[] Embeds { get; init; }
    }

    private sealed class DiscordEmbed
    {
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [JsonPropertyName("description")]
        public required string Description { get; init; }

        [JsonPropertyName("color")]
        public int Color { get; init; }
    }
}
