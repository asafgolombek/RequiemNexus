using Xunit;

namespace RequiemNexus.E2E.Tests.Fixtures;

/// <summary>
/// One shared in-process host for all E2E tests — avoids Kestrel port collisions when xUnit runs test classes in parallel.
/// </summary>
[CollectionDefinition("E2e")]
public sealed class E2eCollectionDefinition : ICollectionFixture<AppFixture>
{
}
