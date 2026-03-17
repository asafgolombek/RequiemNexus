# 🩸 Real-Time Subsystem (SignalR)

This directory contains the SignalR Hub and related infrastructure for the "Blood Communion" (Real-Time Play) features.

## 🌌 Why SignalR?

Requiem Nexus uses SignalR to provide a "live" experience for tabletop play. Unlike polling, SignalR allows the server to push updates instantly to all connected players, ensuring that dice rolls, initiative changes, and vital updates are synchronized across the coterie within our **200ms performance budget**.

### Redis Backplane
To support horizontal scaling (e.g., multiple ECS Fargate tasks), we use a Redis backplane. This ensures that a message sent by a Storyteller connected to Server A reaches a player connected to Server B.

### Strongly-Typed Hubs
We use `Hub<ISessionClient>` to ensure compile-time safety. This prevents "magic string" errors when calling client methods and provides a clear contract for frontend implementation.

## 🏗️ Architectural Pattern: The Thin Relay

Following the **Sacred Covenants** of Requiem Nexus, the `SessionHub` is a **Thin Relay**.

1.  **No Game Logic:** The hub does not calculate successes, apply damage, or manage turns. It delegates all operations to `ISessionService`.
2.  **No Direct Persistence:** The hub does not touch the Database or Redis directly. It uses `ISessionService` and `ISessionStateRepository`.
3.  **The Masquerade (Authorization):** Every hub method must be authorized via `ISessionAuthorizationService` before execution.

### ✅ The Right Way (Surgical Broadcast)
```csharp
public async Task RollDice(int chronicleId, int pool)
{
    // 1. Authorize
    if (!await _authService.IsMemberAsync(UserId, chronicleId)) throw new HubException("Forbidden");

    // 2. Delegate to Application Layer
    await _sessionService.RollDiceAsync(UserId, chronicleId, pool, ...);
}
```

### ❌ The Wrong Way (Logic Leakage)
```csharp
public async Task RollDice(int chronicleId, int pool)
{
    // WRONG: Business logic in the hub
    var result = new Random().Next(1, 11); 
    
    // WRONG: Direct persistence from the hub
    await _redis.ListLeftPushAsync($"rolls:{chronicleId}", result);
    
    // WRONG: Manual broadcasting without service orchestration
    await Clients.Group(chronicleId.ToString()).SendAsync("DiceRolled", result);
}
```

## 🧪 Testing

Real-time features are tested using `WebApplicationFactory` in `RequiemNexus.Application.Tests`. These tests verify the full stack from hub invocation to group broadcast without mocking the transport layer.

---

> *"The blood flows in real time. The code must be its heartbeat."*
