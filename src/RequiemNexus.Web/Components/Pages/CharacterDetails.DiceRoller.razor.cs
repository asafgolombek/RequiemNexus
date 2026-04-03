// Blazor partial: standard dice roller, repair/devotion rolls, and trait-reference toast for CharacterDetails.
using System.Text.Json;
using System.Text.Json.Serialization;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private void OpenReference(string traitName)
    {
        Logger.LogDebug("Trait reference toast shown for {TraitName}.", traitName);
        ToastService.Show(
            "Trait reference",
            $"“{traitName}” — use Vampire: The Requiem 2nd Edition for full trait rules. An in-sheet rules browser is not part of the Grimoire yet.",
            ToastType.Info);
    }

    private void OpenRoller(string traitName)
    {
        _rollerFixedDicePool = null;
        ClearRiteExtendedRollerContext();
        _rollerTraitName = traitName;
        _rollerBaseDice = GetTraitValue(traitName);
        _isRollerOpen = true;
    }

    private int GetTraitValue(string traitName)
    {
        if (_character == null)
        {
            return 0;
        }

        var name = traitName.Replace(" ", string.Empty);
        if (TraitMetadata.IsAttribute(name))
        {
            return _character.GetAttributeRating(name);
        }

        return _character.GetSkillRating(name);
    }

    private void OpenRepairRoller(CharacterAsset ca)
    {
        if (_character == null)
        {
            return;
        }

        _rollerTraitName = $"Repair {ca.Asset?.Name} (Wits + Crafts)";
        _rollerBaseDice = _character.GetAttributeRating(AttributeId.Wits) + _character.GetSkillRating(SkillId.Crafts);
        _rollerFixedDicePool = null;
        ClearRiteExtendedRollerContext();
        _isRollerOpen = true;
    }

    private async Task OpenDevotionRollerAsync(CharacterDevotion cd)
    {
        if (_character == null || cd.DevotionDefinition?.PoolDefinitionJson == null)
        {
            return;
        }

        _rollerFixedDicePool = null;
        ClearRiteExtendedRollerContext();
        _rollerTraitName = cd.DevotionDefinition.Name;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(cd.DevotionDefinition.PoolDefinitionJson, options);
            _rollerFixedDicePool = pool != null ? await TraitResolver.ResolvePoolAsync(_character, pool) : 0;
            _rollerBaseDice = 0;
            _isRollerOpen = true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to resolve devotion pool for {DevotionName} on character {CharacterId}; opening roller with 0 dice",
                cd.DevotionDefinition.Name,
                Id);
            _rollerBaseDice = 0;
            _isRollerOpen = true;
        }
    }

    private void ClearRiteExtendedRollerContext()
    {
        _rollerRiteMaxRolls = null;
        _rollerRiteTargetSuccesses = null;
        _rollerRiteMinutesPerRoll = null;
        _rollerRiteRitualDisciplineDots = null;
        _rollerRiteSorceryType = null;
    }

    private Task OnDiceRollerVisibilityChangedAsync(bool visible)
    {
        _isRollerOpen = visible;
        if (!visible)
        {
            ClearRiteExtendedRollerContext();
        }

        return Task.CompletedTask;
    }
}
