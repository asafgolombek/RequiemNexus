using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

public interface IAdvancementService
{
    bool TryUpgradeCoreTrait(Character character, string traitName, int currentRating, int newRating);
    void UpdateCoreTrait(Character character, string traitName, int newRating);
}
