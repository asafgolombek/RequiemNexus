using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

public class AdvancementService(IBeatLedgerService ledger) : IAdvancementService
{
    private readonly IBeatLedgerService _ledger = ledger;

    /// <inheritdoc />
    public async Task<bool> TryUpgradeCoreTrait(
        Character character,
        AttributeId id,
        int currentRating,
        int newRating,
        string? actingUserId = null)
    {
        string traitName = id.ToString();
        CharacterAttribute? trait = character.Attributes.FirstOrDefault(a => a.Name == traitName);
        bool success = TryUpgradeTrait(character, trait, newRating);

        if (success)
        {
            await _ledger.RecordXpSpendAsync(
                character.Id,
                character.CampaignId,
                trait!.CalculateUpgradeCost(newRating),
                XpExpense.Attribute,
                $"Upgraded {id} to {newRating}",
                actingUserId);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> TryUpgradeCoreTrait(
        Character character,
        SkillId id,
        int currentRating,
        int newRating,
        string? actingUserId = null)
    {
        string traitName = id.ToString();
        CharacterSkill? trait = character.Skills.FirstOrDefault(s => s.Name == traitName);
        bool success = TryUpgradeTrait(character, trait, newRating);

        if (success)
        {
            await _ledger.RecordXpSpendAsync(
                character.Id,
                character.CampaignId,
                trait!.CalculateUpgradeCost(newRating),
                XpExpense.Skill,
                $"Upgraded {id} to {newRating}",
                actingUserId);
        }

        return success;
    }

    public void UpdateCoreTrait(Character character, AttributeId id, int newRating)
    {
        string traitName = id.ToString();
        CharacterAttribute? trait = character.Attributes.FirstOrDefault(a => a.Name == traitName);
        if (trait != null)
        {
            // Set directly — Upgrade() is not used here because creation/editing bypasses XP cost.
            trait.Rating = newRating;
        }
    }

    public void UpdateCoreTrait(Character character, SkillId id, int newRating)
    {
        string traitName = id.ToString();
        CharacterSkill? trait = character.Skills.FirstOrDefault(s => s.Name == traitName);
        if (trait != null)
        {
            // Set directly — Upgrade() is not used here because creation/editing bypasses XP cost.
            trait.Rating = newRating;
        }
    }

    public void RecalculateDerivedStats(Character character)
    {
        int stamina = character.GetAttributeRating(AttributeId.Stamina);
        int resolve = character.GetAttributeRating(AttributeId.Resolve);
        int composure = character.GetAttributeRating(AttributeId.Composure);

        int newMaxHealth = character.Size + stamina;
        int newMaxWillpower = resolve + composure;

        int healthDiff = newMaxHealth - character.MaxHealth;
        character.MaxHealth = newMaxHealth;
        character.CurrentHealth = Math.Clamp(character.CurrentHealth + healthDiff, 0, newMaxHealth);

        int willpowerDiff = newMaxWillpower - character.MaxWillpower;
        character.MaxWillpower = newMaxWillpower;
        character.CurrentWillpower = Math.Clamp(character.CurrentWillpower + willpowerDiff, 0, newMaxWillpower);
    }

    private static bool TryUpgradeTrait(Character character, IRatedTrait? trait, int newRating)
    {
        if (trait == null || newRating <= trait.Rating || newRating > 5)
        {
            return false;
        }

        int totalCost = trait.CalculateUpgradeCost(newRating);

        if (character.ExperiencePoints >= totalCost)
        {
            character.ExperiencePoints -= totalCost;
            trait.Upgrade(newRating, new ExperienceCostRules());
            return true;
        }

        return false;
    }
}
