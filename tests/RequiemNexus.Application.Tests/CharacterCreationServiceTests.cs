using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class CharacterCreationServiceTests
{
    private readonly CharacterCreationService _service = new();

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
