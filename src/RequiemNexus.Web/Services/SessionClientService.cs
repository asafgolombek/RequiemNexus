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

    public event Action<ChronicleUpdateDto>? ChronicleUpdated;

    public event Action? SessionStarted;

    public event Action<string>? SessionEnded;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartAsync(int chronicleId, int? characterId, string userId, string? cookieHeader = null)
    {
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
            await SafeInvokeAsync("JoinSession", chronicleId, characterId);
            return;
        }

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _currentChronicleId = chronicleId;
        _currentCharacterId = characterId;
        _currentUserId = userId;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navManager.ToAbsoluteUri("/hubs/session"), options =>
            {
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    options.Headers["Cookie"] = cookieHeader;
                }
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On("SessionStarted", async () =>
        {
            _isSessionActiveCache = true;
            SessionStarted?.Invoke();
            if (_currentChronicleId.HasValue)
            {
                await SafeInvokeAsync("JoinSession", _currentChronicleId.Value, _currentCharacterId ?? (object?)null);
            }
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
                    "🩸 Dice Roll",
                    $"{roll.PlayerName} rolled {roll.Successes} success(es) on {roll.PoolDescription}",
                    roll.IsDramaticFailure ? ToastType.Error : roll.IsExceptionalSuccess ? ToastType.Success : ToastType.Info);
            }

            DiceRollReceived?.Invoke(roll);
        });

        _hubConnection.On<IEnumerable<DiceRollResultDto>>("ReceiveRollHistory", history => RollHistoryReceived?.Invoke(history));
        _hubConnection.On<CharacterUpdateDto>("ReceiveCharacterUpdate", patch => CharacterUpdated?.Invoke(patch));
        _hubConnection.On<int, string>("ReceiveBloodlineApproved", (characterId, bloodlineName) => BloodlineApproved?.Invoke(characterId, bloodlineName));
        _hubConnection.On<IEnumerable<InitiativeEntryDto>>("ReceiveInitiativeUpdate", entries => InitiativeUpdated?.Invoke(entries));
        _hubConnection.On<ChronicleUpdateDto>("ReceiveChronicleUpdate", patch => ChronicleUpdated?.Invoke(patch));

        try
        {
            await _hubConnection.StartAsync();
            await SafeInvokeAsync("JoinSession", chronicleId, characterId);
        }
        catch (Exception ex)
        {
            toastService.Show("⚠️ Connection Error", "Unable to establish real-time link.", ToastType.Error);
            Console.WriteLine($"Hub Start Error: {ex.Message}");
        }
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
            if (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
            {
                toastService.Show("⏳ Slow Down", "You are sending messages too quickly. The Masquerade requires patience.", ToastType.Warning);
            }
            else if (ex is HubException)
            {
                toastService.Show("🚫 Action Denied", ex.Message, ToastType.Error);
            }
            else
            {
                toastService.Show("⚠️ Link Failure", "The real-time link encountered an error.", ToastType.Error);
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
