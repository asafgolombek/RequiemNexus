using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
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
    private string? _currentUserId;

    public event Action<IEnumerable<PlayerPresenceDto>>? PresenceUpdated;

    public event Action<DiceRollResultDto>? DiceRollReceived;

    public event Action<IEnumerable<DiceRollResultDto>>? RollHistoryReceived;

    public event Action<CharacterUpdateDto>? CharacterUpdated;

    public event Action<IEnumerable<InitiativeEntryDto>>? InitiativeUpdated;

    public event Action<ChronicleUpdateDto>? ChronicleUpdated;

    public event Action? SessionStarted;

    public event Action<string>? SessionEnded;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartAsync(int chronicleId, int? characterId, string userId)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _currentChronicleId = chronicleId;
        _currentUserId = userId;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navManager.ToAbsoluteUri("/hubs/session"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On("SessionStarted", () => SessionStarted?.Invoke());
        _hubConnection.On<string>("SessionEnded", reason => SessionEnded?.Invoke(reason));
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
        _hubConnection.On<IEnumerable<InitiativeEntryDto>>("ReceiveInitiativeUpdate", entries => InitiativeUpdated?.Invoke(entries));
        _hubConnection.On<ChronicleUpdateDto>("ReceiveChronicleUpdate", patch => ChronicleUpdated?.Invoke(patch));

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("JoinSession", chronicleId, characterId);
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task RollDiceAsync(int pool, string description, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote)
    {
        if (_hubConnection != null && _currentChronicleId.HasValue)
        {
            await _hubConnection.InvokeAsync("RollDice", _currentChronicleId.Value, pool, description, tenAgain, nineAgain, eightAgain, isRote);
        }
    }

    public async Task StartSessionAsync()
    {
        if (_hubConnection != null && _currentChronicleId.HasValue)
        {
            await _hubConnection.InvokeAsync("StartSession", _currentChronicleId.Value);
        }
    }

    public async Task EndSessionAsync()
    {
        if (_hubConnection != null && _currentChronicleId.HasValue)
        {
            await _hubConnection.InvokeAsync("EndSession", _currentChronicleId.Value);
        }
    }

    public async Task UpdateInitiativeAsync(IEnumerable<InitiativeEntryDto> entries)
    {
        if (_hubConnection != null && _currentChronicleId.HasValue)
        {
            await _hubConnection.InvokeAsync("UpdateInitiative", _currentChronicleId.Value, entries);
        }
    }

    public async Task SendHeartbeatAsync()
    {
        if (_hubConnection != null && _currentChronicleId.HasValue)
        {
            await _hubConnection.InvokeAsync("Heartbeat", _currentChronicleId.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
