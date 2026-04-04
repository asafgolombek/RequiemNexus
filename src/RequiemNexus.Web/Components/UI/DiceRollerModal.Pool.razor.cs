using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.UI;

#pragma warning disable SA1201 // Partial mirrors feature grouping
#pragma warning disable SA1202

public partial class DiceRollerModal
{
    private async Task OnAssociatedTraitFromPanelAsync(string name)
    {
        _associatedTraitName = name;
        await RefreshResolverTotalAsync();
    }

    private Task OnModifierFromPanelAsync(int value)
    {
        _modifier = value;
        return Task.CompletedTask;
    }

    private Task OnTenAgainFromPanelAsync(bool value)
    {
        _tenAgain = value;
        return Task.CompletedTask;
    }

    private Task OnNineAgainFromPanelAsync(bool value)
    {
        _nineAgain = value;
        return Task.CompletedTask;
    }

    private Task OnEightAgainFromPanelAsync(bool value)
    {
        _eightAgain = value;
        return Task.CompletedTask;
    }

    private Task OnIsRoteFromPanelAsync(bool value)
    {
        _isRote = value;
        return Task.CompletedTask;
    }

    private async Task RefreshResolverTotalAsync()
    {
        _resolverTotal = null;
        if (Character == null || !IsVisible || FixedDicePool.HasValue)
        {
            return;
        }

        PoolDefinition? pool = SheetPoolBuilder.TryCreate(TraitName, _associatedTraitName);
        if (pool != null)
        {
            _resolverTotal = await TraitResolver.ResolvePoolAsync(Character, pool);
        }
    }

    private int AssociatedTraitDice
    {
        get
        {
            if (string.IsNullOrEmpty(_associatedTraitName) || Character == null)
            {
                return 0;
            }

            var propName = _associatedTraitName.Replace(" ", string.Empty);
            if (TraitMetadata.IsAttribute(propName))
            {
                return Character.GetAttributeRating(propName);
            }

            return Character.GetSkillRating(propName);
        }
    }

    private int TotalPool
    {
        get
        {
            if (IsRiteExtendedMode)
            {
                return Math.Max(0, FixedDicePool ?? 0);
            }

            int basePart = FixedDicePool ?? _resolverTotal ?? (BaseDice + AssociatedTraitDice);
            return Math.Max(0, basePart + _modifier);
        }
    }

    private bool IsChanceDiePool => TotalPool <= 0;

    private static bool IsDieShownAsSuccess(int dieFace, bool isChanceDie) =>
        isChanceDie ? dieFace == 10 : dieFace >= 8;
}
