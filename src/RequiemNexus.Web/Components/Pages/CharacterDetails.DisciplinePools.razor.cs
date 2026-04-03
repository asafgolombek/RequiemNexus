// Blazor partial: resolved discipline power dice pools for CharacterDetails.
using System.Text.Json;
using System.Text.Json.Serialization;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private static readonly JsonSerializerOptions _poolJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task ResolveDisciplinePowerPoolsAsync()
    {
        _disciplinePowerResolvedPools.Clear();
        if (_character == null)
        {
            return;
        }

        foreach (CharacterDiscipline cd in _character.Disciplines)
        {
            if (cd.Discipline?.Powers == null)
            {
                continue;
            }

            foreach (DisciplinePower p in cd.Discipline.Powers)
            {
                if (string.IsNullOrEmpty(p.PoolDefinitionJson))
                {
                    continue;
                }

                try
                {
                    PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(p.PoolDefinitionJson, _poolJsonOptions);
                    if (pool != null)
                    {
                        _disciplinePowerResolvedPools[p.Id] = await TraitResolver.ResolvePoolAsync(_character, pool);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        ex,
                        "Failed to resolve PoolDefinitionJson for discipline power {PowerId} on character {CharacterId}",
                        p.Id,
                        _character.Id);
                }
            }
        }
    }
}
