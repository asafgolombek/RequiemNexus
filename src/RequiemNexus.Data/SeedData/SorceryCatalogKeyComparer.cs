using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Compares catalog entries by tradition + ritual name for idempotent DB alignment.
/// </summary>
public sealed class SorceryCatalogKeyComparer : IEqualityComparer<(string Name, SorceryType Type)>
{
    /// <inheritdoc />
    public bool Equals((string Name, SorceryType Type) x, (string Name, SorceryType Type) y) =>
        x.Type == y.Type && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public int GetHashCode((string Name, SorceryType Type) obj) =>
        HashCode.Combine(obj.Type, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
}
