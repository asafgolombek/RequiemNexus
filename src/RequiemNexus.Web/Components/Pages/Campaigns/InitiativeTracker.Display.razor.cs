using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private bool CanStEditLaunchedNpcEncounter() =>
        _encounter != null
        && !_encounter.IsDraft
        && _encounter.ResolvedAt == null
        && (_encounter.IsActive || _encounter.IsPaused);

    private bool ShowPlayerHealth(InitiativeEntry entry) =>
        entry.Character != null && _isSt;

    private string GetDamageClass(Character character, int index)
    {
        if (string.IsNullOrEmpty(character.HealthDamage) || index >= character.HealthDamage.Length)
        {
            return string.Empty;
        }

        return character.HealthDamage[index] switch
        {
            '/' => "bashing",
            'X' => "lethal",
            '*' => "aggravated",
            _ => string.Empty,
        };
    }
}
