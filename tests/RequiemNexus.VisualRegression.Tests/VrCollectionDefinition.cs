using Xunit;

namespace RequiemNexus.VisualRegression.Tests;

/// <summary>
/// One shared host + browser for visual tests (avoids parallel Kestrel binds on the same port).
/// </summary>
[CollectionDefinition("Vr")]
public sealed class VrCollectionDefinition : ICollectionFixture<SnapshotFixture>
{
}
