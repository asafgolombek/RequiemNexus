using Microsoft.JSInterop;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Caches a human-readable command-palette shortcut label (⌘K vs Ctrl+K) from browser APIs once per scope.
/// </summary>
public sealed class PlatformShortcutHintService
{
    private string? _commandPaletteShortcutLabel;

    /// <summary>
    /// Returns a display label for the command palette shortcut, suitable for visible UI next to the trigger.
    /// </summary>
    /// <param name="js">Runtime used to read <c>navigator.userAgentData</c> / <c>userAgent</c>.</param>
    /// <returns><c>⌘K</c> on Apple platforms when detected, otherwise <c>Ctrl+K</c>.</returns>
    public async Task<string> GetCommandPaletteShortcutLabelAsync(IJSRuntime js)
    {
        if (_commandPaletteShortcutLabel != null)
        {
            return _commandPaletteShortcutLabel;
        }

        try
        {
            _commandPaletteShortcutLabel = await js.InvokeAsync<string>("getPaletteShortcutLabel");
        }
        catch
        {
            _commandPaletteShortcutLabel = "Ctrl+K";
        }

        if (string.IsNullOrWhiteSpace(_commandPaletteShortcutLabel))
        {
            _commandPaletteShortcutLabel = "Ctrl+K";
        }

        return _commandPaletteShortcutLabel;
    }
}
