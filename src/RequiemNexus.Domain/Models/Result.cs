namespace RequiemNexus.Domain.Models;

/// <summary>
/// Represents the outcome of an operation. Domain methods return Result for expected business failures.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <param name="IsSuccess">True when the operation succeeded.</param>
/// <param name="Value">The value when successful.</param>
/// <param name="Error">The error message when failed.</param>
public record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    /// <summary>Creates a successful result.</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result.</summary>
    public static Result<T> Failure(string error) => new(false, default, error);
}
