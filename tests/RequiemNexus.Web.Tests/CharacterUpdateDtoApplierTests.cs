using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Tests;

public class CharacterUpdateDtoApplierTests
{
    [Fact]
    public void ApplyToCharacter_CopiesScalarsIncludingProgression()
    {
        var character = new Character
        {
            Id = 1,
            CurrentHealth = 1,
            Beats = 0,
            ExperiencePoints = 5,
            TotalExperiencePoints = 10,
        };

        var patch = new CharacterUpdateDto(
            CharacterId: 1,
            CurrentHealth: 2,
            MaxHealth: 7,
            CurrentWillpower: 3,
            MaxWillpower: 4,
            CurrentVitae: 5,
            MaxVitae: 10,
            Humanity: 6,
            Armor: 2,
            ActiveConditions: [],
            HealthDamage: "●",
            Beats: 3,
            ExperiencePoints: 7,
            TotalExperiencePoints: 12);

        CharacterUpdateDtoApplier.ApplyToCharacter(character, patch);

        Assert.Equal(2, character.CurrentHealth);
        Assert.Equal(7, character.MaxHealth);
        Assert.Equal(3, character.CurrentWillpower);
        Assert.Equal(4, character.MaxWillpower);
        Assert.Equal(5, character.CurrentVitae);
        Assert.Equal(10, character.MaxVitae);
        Assert.Equal(6, character.Humanity);
        Assert.Equal("●", character.HealthDamage);
        Assert.Equal(3, character.Beats);
        Assert.Equal(7, character.ExperiencePoints);
        Assert.Equal(12, character.TotalExperiencePoints);
    }
}
