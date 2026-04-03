using System.Text.Json;
using System.Text.Json.Serialization;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for deserializing <see cref="Domain.Models.PassiveModifier"/> lists from seed JSON.
/// </summary>
internal static class PassiveModifierJsonSerializerOptions
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
