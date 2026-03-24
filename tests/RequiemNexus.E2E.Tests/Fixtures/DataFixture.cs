namespace RequiemNexus.E2E.Tests.Fixtures;

/// <summary>
/// Well-known E2E data — the host seeds a player/ST user, campaign, and character via <see cref="E2eTestDataSeed"/>.
/// </summary>
public static class DataFixture
{
    /// <inheritdoc cref="E2eTestDataSeed.PlayerEmail"/>
    public static string TestUserEmail => E2eTestDataSeed.PlayerEmail;

    /// <inheritdoc cref="E2eTestDataSeed.Password"/>
    public static string TestUserPassword => E2eTestDataSeed.Password;
}
