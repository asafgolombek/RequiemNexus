namespace RequiemNexus.Domain.Models;

/// <summary>
/// Sentinel type for <see cref="Result{T}"/> when an operation has no return value.
/// </summary>
public readonly record struct Unit
{
    /// <summary>Canonical success value for <c>Result&lt;Unit&gt;</c>.</summary>
    public static Unit Value => default;
}
