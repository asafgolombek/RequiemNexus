# Masquerade audit checklist (mutations and sensitive reads)

Use this when adding or changing **minimal APIs**, **Blazor handlers** that invoke Application services, **SignalR hub methods**, or **new Application service** entry points.

The four Masquerade steps (from `AGENTS.md`):

1. Extract the authenticated user id from the security context (`ClaimsPrincipal`, `Context.UserIdentifier` on the hub).
2. Load the target entity (or verify session membership from Redis for ephemeral session state).
3. **Verify ownership or campaign role** — reject with `403` / `HubException` / `Results.Forbid()` if unauthorized.
4. Proceed only after authorization is confirmed.

## Minimal APIs (`Program.cs` and future endpoint maps)

- [ ] Endpoint uses `.RequireAuthorization()` unless it is intentionally anonymous (document why).
- [ ] Anonymous endpoints use rate limiting where abuse matters (see `public-rolls` policy).
- [ ] Handler does not implement business rules; it delegates to Application services.
- [ ] `UnauthorizedAccessException` from services maps to `403 Forbid`, not `400`.
- [ ] No trust in client-supplied user id; always use server-side claims.

## Blazor pages and components

- [ ] Presentation does not decide “can user edit X?” beyond UI affordance; **Application** services enforce access via `IAuthorizationHelper` / storyteller checks.
- [ ] Every call that mutates character, campaign, encounter, or session data goes through a service method that performs Masquerade steps.

## `SessionHub`

- [ ] Hub method contains **no** business logic and **no** direct Redis/DB access — delegate to `ISessionService` / `ISessionAuthorizationService` / other Application services.
- [ ] Dice results are produced only by server-side `DiceService` (or equivalent); the hub never accepts a client-supplied roll outcome.
- [ ] Chronicle membership and character-in-chronicle checks mirror REST session rules (`ISessionAuthorizationService`).

## Application services

- [ ] Mutating methods call `RequireCharacterOwnerAsync`, `RequireCharacterAccessAsync`, `RequireStorytellerAsync`, or equivalent before writes.
- [ ] Failures are logged with structured context; avoid logging secrets or full PII.
- [ ] Expected auth failures use `UnauthorizedAccessException` (or `Result.Failure` in Domain) consistently.

## Reference implementation

- [AuthorizationHelper.cs](../src/RequiemNexus.Application/Services/AuthorizationHelper.cs)
- [SessionHub.cs](../src/RequiemNexus.Web/Hubs/SessionHub.cs)
