using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace RequiemNexus.Web.Components;

/// <summary>
/// Base class for all interactive Requiem Nexus pages.
/// Resolves <see cref="CurrentUserId"/> from the authentication state and
/// provides <see cref="Loading"/> / <see cref="Busy"/> state helpers.
/// Override <see cref="LoadAsync"/> instead of <see cref="ComponentBase.OnInitializedAsync"/>.
/// </summary>
public abstract class RequiemNexusComponentBase : ComponentBase
{
    /// <summary>Gets the authentication state provider (injected).</summary>
    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Gets the navigation manager (injected).</summary>
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets the currently authenticated user's ID, or <c>null</c> if not authenticated.</summary>
    protected string? CurrentUserId { get; private set; }

    /// <summary>Gets or sets a value indicating whether the page is performing initial data load.</summary>
    protected bool Loading { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a user-initiated operation is in progress.</summary>
    protected bool Busy { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        CurrentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(CurrentUserId))
        {
            await LoadAsync();
        }

        Loading = false;
    }

    /// <summary>
    /// Override to perform page-specific data loading after authentication is resolved.
    /// Only called when the user is authenticated (<see cref="CurrentUserId"/> is non-null).
    /// </summary>
    protected virtual Task LoadAsync() => Task.CompletedTask;
}
