using Microsoft.Playwright;
using RequiemNexus.E2E.Tests.Helpers;

namespace RequiemNexus.E2E.Tests.Fixtures;

/// <summary>
/// Track 1 auth entry point — delegates to <see cref="E2eLoginHelper"/> against the seeded E2E user.
/// </summary>
public static class AuthFixture
{
    /// <summary>
    /// Signs in via the UI using seeded credentials (same as <see cref="E2eLoginHelper.LoginAsSeededPlayerAsync"/>).
    /// </summary>
    /// <param name="page">Playwright page bound to the E2E host.</param>
    /// <returns>A task that completes when the home dashboard is shown.</returns>
    public static Task LoginAsSeededPlayerAsync(IPage page) => E2eLoginHelper.LoginAsSeededPlayerAsync(page);
}
