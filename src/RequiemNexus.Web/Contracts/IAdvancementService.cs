using RequiemNexus.Data.Models;
using RequiemNexus.Domain;

namespace RequiemNexus.Web.Contracts;

public interface IAdvancementService
{
    bool TryUpgradeCoreTrait(Character character, AttributeId id, int currentRating, int newRating);

    bool TryUpgradeCoreTrait(Character character, SkillId id, int currentRating, int newRating);

    void UpdateCoreTrait(Character character, AttributeId id, int newRating);

    void UpdateCoreTrait(Character character, SkillId id, int newRating);

    void RecalculateDerivedStats(Character character);
}
