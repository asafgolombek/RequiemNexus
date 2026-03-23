using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Client-side service for interacting with the real-time SessionHub.
/// Injected into Blazor components to handle broadcasts and invoke hub methods.
/// </summary>
public class SessionClientService(NavigationManager navManager, ToastService toastService) : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private int? _currentChronicleId;
    private int? _currentCharacterId;
    private string? _currentUserId;
    private CancellationTokenSource? _stopCts;
    private bool? _isSessionActiveCache;

    public event Action<IEnumerable<PlayerPresenceDto>>? PresenceUpdated;

    public event Action<DiceRollResultDto>? DiceRollReceived;

    public event Action<IEnumerable<DiceRollResultDto>>? RollHistoryReceived;

    public event Action<CharacterUpdateDto>? CharacterUpdated;

    public event Action<int, string>? BloodlineApproved;

    public event Action<IEnumerable<InitiativeEntryDto>>? InitiativeUpdated;

    public event Action<ConditionNotificationDto>? ConditionNotificationReceived;

    public event Action<ChronicleUpdateDto>? ChronicleUpdated;

    public event Action<SocialManeuverUpdateDto>? SocialManeuverUpdated;

    public event Action<RelationshipUpdateDto>? RelationshipUpdated;

    public event Action? SessionStarted;

    public event Action<string>? SessionEnded;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Connects to the session hub and invokes JoinSession. Returns a structured result so pages can show inline guidance
    /// (Blazor Server often lacks HttpContext; an empty cookie header usually breaks negotiate).
    /// </summary>
    public async Task<SessionHubConnectResult> StartAsync(int chronicleId, int? characterId, string userId, string? cookieHeader = null)
    {
        bool cookieMissing = string.IsNullOrWhiteSpace(cookieHeader);

        // Cancel any pending delayed stop
        if (_stopCts != null)
        {
            await _stopCts.CancelAsync();
            _stopCts.Dispose();
            _stopCts = null;
        }

        if (_hubConnection != null && _currentChronicleId == chronicleId && IsConnected)
        {
            _currentCharacterId = characterId;
            return await TryJoinSessionAsync(chronicleId, characterId);
        }

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _currentChronicleId = chronicleId;
        _currentCharacterId = characterId;
        _currentUserId = userId;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navManager.ToAbsoluteUri("/hubs/session"), options =>
            {
                if (!cookieMissing)
                {
                    options.Headers["Cookie"] = cookieHeader!;
                }
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHubHandlers();

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            await DisposeHubConnectionAsync();
            if (cookieMissing)
            {
                return SessionHubConnectResult.FailedMissingCookie;
            }

            return MapNegotiateFailure(ex);
        }

        SessionHubConnectResult joinResult = await TryJoinSessionAsync(chronicleId, characterId);
        if (joinResult != SessionHubConnectResult.Connected)
        {
            await DisposeHubConnectionAsync();
        }

        return joinResult;
    }

    /// <summary>
    /// Hydrates the presence bar with current session state (e.g. from REST API on page load).
    /// Use when connecting to show existing players before the first hub broadcast.
    /// </summary>
    public void SetPresence(IEnumerable<PlayerPresenceDto> players)
    {
        PresenceUpdated?.Invoke(players);
    }

    /// <summary>
    /// Efficiently checks if a session is active, using a local cache if available.
    /// </summary>
    public async Task<bool> GetSessionActiveAsync(int chronicleId, ISessionService sessionService)
    {
        if (_isSessionActiveCache.HasValue && _currentChronicleId == chronicleId)
        {
            return _isSessionActiveCache.Value;
        }

        var state = await sessionService.GetSessionStateAsync(chronicleId);
        _isSessionActiveCache = state != null;
        return _isSessionActiveCache.Value;
    }

    /// <summary>
    /// Triggers a delayed stop (2 minutes) to allow for internal navigation.
    /// Prefer explicit use from "Leave Session" on the campaign; avoid pairing with page disposal in Blazor —
    /// dispose order vs. the next page's <c>StartAsync</c> can schedule a teardown after reconnect.
    /// </summary>
    public async Task StopAsync()
    {
        // Cancel any existing stop request
        if (_stopCts != null)
        {
            await _stopCts.CancelAsync();
            _stopCts.Dispose();
        }

        _stopCts = new CancellationTokenSource();
        var token = _stopCts.Token;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Wait 2 minutes before actually stopping the connection
                    await Task.Delay(TimeSpan.FromMinutes(2), token);

                    if (!token.IsCancellationRequested)
                    {
                        await PerformImmediateStopAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Stop was cancelled by a new StartAsync call
                }
            },
            token);
    }

    public async Task RollDiceAsync(
        int chronicleId,
        int? characterId,
        int pool,
        string description,
        bool tenAgain,
        bool nineAgain,
        bool eightAgain,
        bool isRote)
    {
        await SafeInvokeAsync("RollDice", chronicleId, characterId, pool, description, tenAgain, nineAgain, eightAgain, isRote);
    }

    public async Task StartSessionAsync()
    {
        if (_currentChronicleId.HasValue)
        {
            await SafeInvokeAsync("StartSession", _currentChronicleId.Value);
        }
    }

    public async Task EndSessionAsync()
    {
        if (_currentChronicleId.HasValue)
        {
            await SafeInvokeAsync("EndSession", _currentChronicleId.Value);
        }
    }

    public async Task UpdateInitiativeAsync(IEnumerable<InitiativeEntryDto> entries)
    {
        if (_currentChronicleId.HasValue)
        {
            await SafeInvokeAsync("UpdateInitiative", _currentChronicleId.Value, entries);
        }
    }

    public async Task SendHeartbeatAsync()
    {
        if (_currentChronicleId.HasValue)
        {
            await SafeInvokeAsync("Heartbeat", _currentChronicleId.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_stopCts != null)
        {
            await _stopCts.CancelAsync();
            _stopCts.Dispose();
        }

        if (_hubConnection != null)
        {
            // When the circuit disposes (site close), we should stop immediately
            await _hubConnection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Re-adds this client to the chronicle group and Redis presence after reconnect or session start.
    /// </summary>
    private async Task RejoinCurrentSessionAsync()
    {
        if (!_currentChronicleId.HasValue)
        {
            return;
        }

        await SafeInvokeAsync("JoinSession", _currentChronicleId.Value, _currentCharacterId ?? (object?)null);
    }

    private void RegisterHubHandlers()
    {
        if (_hubConnection == null)
        {
            return;
        }

        _hubConnection.On("SessionStarted", async () =>
        {
            _isSessionActiveCache = true;
            SessionStarted?.Invoke();
            await RejoinCurrentSessionAsync();
        });

        _hubConnection.On<string>("SessionEnded", reason =>
        {
            _isSessionActiveCache = false;
            SessionEnded?.Invoke(reason);
        });

        _hubConnection.On<IEnumerable<PlayerPresenceDto>>("ReceivePresenceUpdate", players => PresenceUpdated?.Invoke(players));

        _hubConnection.On<DiceRollResultDto>("ReceiveDiceRoll", roll =>
        {
            // Only show toast if it's someone else's roll
            if (roll.RolledByUserId != _currentUserId)
            {
                toastService.Show(
                    "Dice Roll",
                    $"{roll.PlayerName} rolled {roll.Successes} success(es) on {roll.PoolDescription}",
                    roll.IsDramaticFailure ? ToastType.Error : roll.IsExceptionalSuccess ? ToastType.Success : ToastType.Info);
            }

            DiceRollReceived?.Invoke(roll);
        });

        _hubConnection.On<IEnumerable<DiceRollResultDto>>("ReceiveRollHistory", history => RollHistoryReceived?.Invoke(history));
        _hubConnection.On<CharacterUpdateDto>("ReceiveCharacterUpdate", patch => CharacterUpdated?.Invoke(patch));
        _hubConnection.On<int, string>("ReceiveBloodlineApproved", (characterId, bloodlineName) => BloodlineApproved?.Invoke(characterId, bloodlineName));
        _hubConnection.On<IEnumerable<InitiativeEntryDto>>("ReceiveInitiativeUpdate", entries => InitiativeUpdated?.Invoke(entries));
        _hubConnection.On<ConditionNotificationDto>("ReceiveConditionNotification", n => ConditionNotificationReceived?.Invoke(n));
        _hubConnection.On<ChronicleUpdateDto>("ReceiveChronicleUpdate", patch => ChronicleUpdated?.Invoke(patch));
        _hubConnection.On<SocialManeuverUpdateDto>("ReceiveSocialManeuverUpdate", update => SocialManeuverUpdated?.Invoke(update));
        _hubConnection.On<RelationshipUpdateDto>("ReceiveRelationshipUpdate", update =>
        {
            RelationshipUpdated?.Invoke(update);
            if (!string.IsNullOrEmpty(update.Summary))
            {
                toastService.Show("Relationships", update.Summary, ToastType.Info);
            }
        });

        _hubConnection.Reconnected += async _ =>
        {
            await RejoinCurrentSessionAsync();
        };

        _hubConnection.Closed += async _ =>
        {
            _isSessionActiveCache = null;
            await Task.CompletedTask;
        };
    }

    private async Task<SessionHubConnectResult> TryJoinSessionAsync(int chronicleId, int? characterId)
    {
        if (!IsConnected || _hubConnection == null)
        {
            return SessionHubConnectResult.FailedOther;
        }

        try
        {
            await _hubConnection.InvokeCoreAsync("JoinSession", [chronicleId, characterId]);
            return SessionHubConnectResult.Connected;
        }
        catch (Exception ex)
        {
            return MapJoinFailure(ex);
        }
    }

    private SessionHubConnectResult MapJoinFailure(Exception ex)
    {
        if (IsRateLimited(ex))
        {
            return SessionHubConnectResult.RateLimited;
        }

        if (ex is HubException hx)
        {
            if (hx.Message.Contains("Not a member", StringComparison.OrdinalIgnoreCase))
            {
                return SessionHubConnectResult.ForbiddenNotMember;
            }

            return SessionHubConnectResult.FailedOther;
        }

        return SessionHubConnectResult.FailedOther;
    }

    private SessionHubConnectResult MapNegotiateFailure(Exception ex)
    {
        if (IsRateLimited(ex))
        {
            return SessionHubConnectResult.RateLimited;
        }

        HttpStatusCode? status = GetHttpStatusCode(ex);
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return SessionHubConnectResult.FailedNegotiate;
        }

        return SessionHubConnectResult.FailedOther;
    }

    private HttpStatusCode? GetHttpStatusCode(Exception ex)
    {
        for (Exception? walk = ex; walk != null; walk = walk.InnerException)
        {
            if (walk is HttpRequestException hre && hre.StatusCode.HasValue)
            {
                return hre.StatusCode.Value;
            }
        }

        return null;
    }

    private bool IsRateLimited(Exception ex)
    {
        for (Exception? walk = ex; walk != null; walk = walk.InnerException)
        {
            if (walk is HttpRequestException hre && hre.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            if (walk.Message.Contains("429", StringComparison.OrdinalIgnoreCase)
                || walk.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task DisposeHubConnectionAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    private async Task SafeInvokeAsync(string methodName, params object?[] args)
    {
        if (!IsConnected || _hubConnection == null)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeCoreAsync(methodName, args);
        }
        catch (Exception ex)
        {
            if (IsRateLimited(ex))
            {
                toastService.Show("Slow Down", "You are sending messages too quickly. The Masquerade requires patience.", ToastType.Warning);
            }
            else if (ex is HubException)
            {
                toastService.Show("Action Denied", ex.Message, ToastType.Error);
            }
            else
            {
                toastService.Show("Link Failure", "The real-time link encountered an error.", ToastType.Error);
                Console.WriteLine($"Hub Invocation Error ({methodName}): {ex.Message}");
            }
        }
    }

    private async Task PerformImmediateStopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _currentChronicleId = null;
        _currentCharacterId = null;
        _isSessionActiveCache = null;
    }
}
