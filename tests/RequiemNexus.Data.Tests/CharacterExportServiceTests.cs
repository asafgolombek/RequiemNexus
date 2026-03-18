using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Helpers;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for CharacterExportService, covering JSON and PDF export paths.
/// </summary>
public class CharacterExportServiceTests
{
    static CharacterExportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Character BuildTestCharacter()
    {
        var clan = new Clan { Name = "Ventrue" };
        var character = new Character
        {
            ApplicationUserId = "test-user",
            Name = "Lord Ashford",
            Concept = "Noble vampire",
            Mask = "The Patron",
            Dirge = "The Tyrant",
            BloodPotency = 2,
            Humanity = 6,
            MaxHealth = 8,
            CurrentHealth = 8,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 4,
            CurrentVitae = 4,
            Clan = clan,
        };

        CharacterTraitHelper.SeedAttributes(character);
        CharacterTraitHelper.SeedSkills(character);

        character.Merits.Add(new CharacterMerit
        {
            Merit = new Merit { Name = "Resources" },
            Rating = 3,
            Specification = "N/A",
        });
        character.Disciplines.Add(new CharacterDiscipline
        {
            Discipline = new Discipline { Name = "Dominate" },
            Rating = 2,
        });
        character.Aspirations.Add(new CharacterAspiration { Description = "Rebuild the family estate" });
        character.Banes.Add(new CharacterBane { Description = "Cannot enter without invitation" });

        return character;
    }

    // -----------------------------------------------------------------------
    // JSON export
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExportCharacterAsJsonAsync_WithValidId_ReturnsValidJson()
    {
        using var ctx = CreateContext(nameof(ExportCharacterAsJsonAsync_WithValidId_ReturnsValidJson));
        var character = BuildTestCharacter();
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var svc = new CharacterExportService(ctx);
        var json = await svc.ExportCharacterAsJsonAsync(character.Id, "test-user");

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Lord Ashford", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal("Ventrue", doc.RootElement.GetProperty("clan").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("BloodPotency").GetInt32());
    }

    [Fact]
    public async Task ExportCharacterAsJsonAsync_WithInvalidId_ReturnsEmptyJson()
    {
        using var ctx = CreateContext(nameof(ExportCharacterAsJsonAsync_WithInvalidId_ReturnsEmptyJson));
        var svc = new CharacterExportService(ctx);

        var json = await svc.ExportCharacterAsJsonAsync(999, "test-user");

        Assert.Equal("{}", json);
    }

    [Fact]
    public void ExportCharacterAsJson_WithLoadedCharacter_ContainsExpectedFields()
    {
        using var ctx = CreateContext(nameof(ExportCharacterAsJson_WithLoadedCharacter_ContainsExpectedFields));
        var svc = new CharacterExportService(ctx);
        var character = BuildTestCharacter();

        var json = svc.ExportCharacterAsJson(character);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Lord Ashford", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal("Ventrue", doc.RootElement.GetProperty("clan").GetString());
        Assert.Equal(9, doc.RootElement.GetProperty("attributes").GetArrayLength());
        Assert.Equal(24, doc.RootElement.GetProperty("skills").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("merits").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("disciplines").GetArrayLength());
    }

    // -----------------------------------------------------------------------
    // PDF export
    // -----------------------------------------------------------------------

    [Fact]
    public void ExportCharacterAsPdf_WithValidCharacter_ReturnsPdfBytes()
    {
        using var ctx = CreateContext(nameof(ExportCharacterAsPdf_WithValidCharacter_ReturnsPdfBytes));
        var svc = new CharacterExportService(ctx);
        var character = BuildTestCharacter();

        var pdfBytes = svc.ExportCharacterAsPdf(character);

        Assert.NotEmpty(pdfBytes);
        // PDF files start with the "%PDF" magic bytes
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    [Fact]
    public async Task ExportCharacterAsPdfAsync_WithInvalidId_ReturnsEmptyBytes()
    {
        using var ctx = CreateContext(nameof(ExportCharacterAsPdfAsync_WithInvalidId_ReturnsEmptyBytes));
        var svc = new CharacterExportService(ctx);

        var pdfBytes = await svc.ExportCharacterAsPdfAsync(999, "test-user");

        Assert.Empty(pdfBytes);
    }
}
