using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class CharacterCreationServiceTests
{
    private readonly CharacterCreationService _service = new();

    [Fact]
    public void Eligibility_Necromancy_Ventrue_Fails()
    {
        var ventrue = new Clan { Id = 1, Name = "Ventrue" };
        ventrue.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 10 });
        var character = new Character { ClanId = 1, Clan = ventrue };
        var necromancy = new Discipline { Id = 9, Name = "Necromancy", IsNecromancy = true };
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 9, Rating = 1 });
        var dict = new Dictionary<int, Discipline> { [9] = necromancy };

        var result = _service.ValidateCreationDisciplineEligibility(character, dict);

        Assert.False(result.IsSuccess);
        Assert.Contains("Necromancy", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Eligibility_Necromancy_Mekhet_Succeeds()
    {
        var mekhet = new Clan { Id = 1, Name = "Mekhet" };
        mekhet.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 10 });
        var character = new Character { ClanId = 1, Clan = mekhet };
        var necromancy = new Discipline { Id = 9, Name = "Necromancy", IsNecromancy = true };
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 9, Rating = 1 });
        var dict = new Dictionary<int, Discipline> { [9] = necromancy };

        var result = _service.ValidateCreationDisciplineEligibility(character, dict);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Eligibility_CovenantDiscipline_WithoutCovenant_Fails()
    {
        var clan = new Clan { Id = 1, Name = "Daeva" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        var character = new Character { ClanId = 1, Clan = clan };
        var theban = new Discipline
        {
            Id = 5,
            Name = "Theban Sorcery",
            IsCovenantDiscipline = true,
            CovenantId = 2,
        };
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 5, Rating = 1 });
        var dict = new Dictionary<int, Discipline> { [5] = theban };

        var result = _service.ValidateCreationDisciplineEligibility(character, dict);

        Assert.False(result.IsSuccess);
        Assert.Contains("Covenant", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Eligibility_BloodlineOnlyDiscipline_Fails()
    {
        var clan = new Clan { Id = 1, Name = "Gangrel" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        var character = new Character { ClanId = 1, Clan = clan };
        var bloodlineDisc = new Discipline
        {
            Id = 8,
            Name = "Bloodline Gift",
            IsBloodlineDiscipline = true,
            BloodlineId = 3,
            Bloodline = new BloodlineDefinition { Id = 3, Name = "Test Bloodline" },
        };
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 8, Rating = 1 });
        var dict = new Dictionary<int, Discipline> { [8] = bloodlineDisc };

        var result = _service.ValidateCreationDisciplineEligibility(character, dict);

        Assert.False(result.IsSuccess);
        Assert.Contains("bloodline", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Eligibility_UnknownDisciplineId_Fails()
    {
        var character = new Character();
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 404, Rating = 1 });
        var dict = new Dictionary<int, Discipline>();

        var result = _service.ValidateCreationDisciplineEligibility(character, dict);

        Assert.False(result.IsSuccess);
        Assert.Contains("Unknown discipline", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AllInClan_Succeeds()
    {
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 2 });
        var c = new Character { ClanId = 1, Clan = clan };
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 1, Rating = 2 });
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 2, Rating = 1 });

        var r = _service.ValidateCreationDisciplines(c);

        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Validate_TwoInClan_OneFree_Succeeds()
    {
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 2 });
        var c = new Character { ClanId = 1, Clan = clan };
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 1, Rating = 1 });
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 2, Rating = 1 });
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 99, Rating = 1 });

        var r = _service.ValidateCreationDisciplines(c);

        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Validate_TwoOutOfClan_Fails()
    {
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        var c = new Character { ClanId = 1, Clan = clan };
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 1, Rating = 1 });
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 88, Rating = 1 });
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 99, Rating = 1 });

        var r = _service.ValidateCreationDisciplines(c);

        Assert.False(r.IsSuccess);
        Assert.Contains("2 of your 3", r.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_LessThanThreeDots_NoValidation()
    {
        var c = new Character();
        c.Disciplines.Add(new CharacterDiscipline { DisciplineId = 1, Rating = 1 });

        var r = _service.ValidateCreationDisciplines(c);

        Assert.True(r.IsSuccess);
    }
}
