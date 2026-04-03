using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class CharacterPdfExportService(ICharacterExportCharacterLoader loader) : ICharacterPdfExportService
{
    private readonly ICharacterExportCharacterLoader _loader = loader;

    /// <inheritdoc />
    public async Task<byte[]> ExportCharacterAsPdfAsync(int characterId, string userId, CancellationToken cancellationToken = default)
    {
        Character? character = await _loader.LoadOwnedCharacterAsync(characterId, userId, cancellationToken);

        if (character == null)
        {
            return [];
        }

        return await Task.Run(() => ExportCharacterAsPdf(character), cancellationToken);
    }

    /// <inheritdoc />
    public byte[] ExportCharacterAsPdf(Character character)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Item().BorderBottom(2).BorderColor("#550000").PaddingBottom(6).Column(header =>
                    {
                        header.Item().Text(character.Name).FontSize(22).Bold().FontColor("#1a1a1a");
                        header.Item().Text(text =>
                        {
                            text.Span("Clan: ").Bold();
                            text.Span(character.Clan?.Name ?? "—");
                            text.Span("   |   ").FontColor("#888888");
                            text.Span("Concept: ").Bold();
                            text.Span(string.IsNullOrEmpty(character.Concept) ? "—" : character.Concept);
                            text.Span("   |   ").FontColor("#888888");
                            text.Span("Mask: ").Bold();
                            text.Span(string.IsNullOrEmpty(character.Mask) ? "—" : character.Mask);
                            text.Span("   |   ").FontColor("#888888");
                            text.Span("Dirge: ").Bold();
                            text.Span(string.IsNullOrEmpty(character.Dirge) ? "—" : character.Dirge);
                        });

                        if (!string.IsNullOrEmpty(character.Touchstone))
                        {
                            header.Item().Text(text =>
                            {
                                text.Span("Touchstone: ").Bold();
                                text.Span(character.Touchstone);
                            });
                        }

                        if (!string.IsNullOrEmpty(character.Height) || !string.IsNullOrEmpty(character.EyeColor) || !string.IsNullOrEmpty(character.HairColor))
                        {
                            header.Item().Text(text =>
                            {
                                if (!string.IsNullOrEmpty(character.Height))
                                {
                                    text.Span("Height: ").Bold();
                                    text.Span(character.Height + "   ");
                                }

                                if (!string.IsNullOrEmpty(character.EyeColor))
                                {
                                    text.Span("Eyes: ").Bold();
                                    text.Span(character.EyeColor + "   ");
                                }

                                if (!string.IsNullOrEmpty(character.HairColor))
                                {
                                    text.Span("Hair: ").Bold();
                                    text.Span(character.HairColor);
                                }
                            });
                        }
                    });

                    col.Item().PaddingTop(8);

                    col.Item().Background("#f5f0f0").Padding(6).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Blood Potency").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text(Dots(character.BloodPotency, 10));
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Humanity").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text(Dots(character.Humanity, 10));
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Health").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text($"{character.CurrentHealth} / {character.MaxHealth}");
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Willpower").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text($"{character.CurrentWillpower} / {character.MaxWillpower}");
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Vitae").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text($"{character.CurrentVitae} / {character.MaxVitae}");
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("XP / Total").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text($"{character.ExperiencePoints} / {character.TotalExperiencePoints}");
                        });
                        row.ConstantItem(1).Background("#ccbbbb");
                        row.RelativeItem().PaddingLeft(6).Column(c =>
                        {
                            c.Item().Text("Speed / Defense / Armor").Bold().FontSize(8).FontColor("#550000");
                            c.Item().Text($"{character.Speed} / {character.Defense} / {character.Armor}");
                        });
                    });

                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("ATTRIBUTES").Bold().FontSize(10).FontColor("#550000");
                            c.Item().PaddingTop(4).Row(attrRow =>
                            {
                                foreach ((string label, TraitCategory category) in new[]
                                {
                                    ("Mental", TraitCategory.Mental),
                                    ("Physical", TraitCategory.Physical),
                                    ("Social", TraitCategory.Social),
                                })
                                {
                                    attrRow.RelativeItem().Column(catCol =>
                                    {
                                        catCol.Item().Text(label).Bold().FontSize(8).FontColor("#333333");
                                        foreach (CharacterAttribute attr in character.Attributes.Where(a => a.Category == category))
                                        {
                                            catCol.Item().Row(r =>
                                            {
                                                r.RelativeItem().Text(TraitMetadata.GetDisplayName(attr.Name)).FontSize(8);
                                                r.AutoItem().Text(Dots(attr.Rating, 5)).FontSize(8);
                                            });
                                        }
                                    });
                                }
                            });
                        });

                        row.ConstantItem(8);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("SKILLS").Bold().FontSize(10).FontColor("#550000");
                            c.Item().PaddingTop(4).Row(skillRow =>
                            {
                                foreach ((string label, TraitCategory category) in new[]
                                {
                                    ("Mental", TraitCategory.Mental),
                                    ("Physical", TraitCategory.Physical),
                                    ("Social", TraitCategory.Social),
                                })
                                {
                                    skillRow.RelativeItem().Column(catCol =>
                                    {
                                        catCol.Item().Text(label).Bold().FontSize(8).FontColor("#333333");
                                        foreach (CharacterSkill skill in character.Skills.Where(s => s.Category == category))
                                        {
                                            catCol.Item().Row(r =>
                                            {
                                                r.RelativeItem().Text(TraitMetadata.GetDisplayName(skill.Name)).FontSize(8);
                                                r.AutoItem().Text(Dots(skill.Rating, 5)).FontSize(8);
                                            });
                                        }
                                    });
                                }
                            });
                        });
                    });

                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("MERITS").Bold().FontSize(10).FontColor("#550000");
                            foreach (CharacterMerit merit in character.Merits)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(text =>
                                    {
                                        text.Span(merit.Merit?.Name ?? "—").FontSize(8);
                                        if (!string.IsNullOrEmpty(merit.Specification) && merit.Specification != "N/A")
                                        {
                                            text.Span($" ({merit.Specification})").FontSize(8).FontColor("#555555");
                                        }
                                    });
                                    r.AutoItem().Text(Dots(merit.Rating, 5)).FontSize(8);
                                });
                            }
                        });

                        row.ConstantItem(8);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("DISCIPLINES").Bold().FontSize(10).FontColor("#550000");
                            foreach (CharacterDiscipline disc in character.Disciplines)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(disc.Discipline?.Name ?? "—").FontSize(8);
                                    r.AutoItem().Text(Dots(disc.Rating, 5)).FontSize(8);
                                });
                            }
                        });
                    });

                    if (character.Aspirations.Count > 0 || character.Banes.Count > 0)
                    {
                        col.Item().PaddingTop(8);
                        col.Item().Row(row =>
                        {
                            if (character.Aspirations.Count > 0)
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("ASPIRATIONS").Bold().FontSize(10).FontColor("#550000");
                                    foreach (CharacterAspiration aspiration in character.Aspirations)
                                    {
                                        c.Item().Text($"• {aspiration.Description}").FontSize(8);
                                    }
                                });
                                row.ConstantItem(8);
                            }

                            if (character.Banes.Count > 0)
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("BANES").Bold().FontSize(10).FontColor("#550000");
                                    foreach (CharacterBane bane in character.Banes)
                                    {
                                        c.Item().Text($"• {bane.Description}").FontSize(8);
                                    }
                                });
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(character.Backstory))
                    {
                        col.Item().PaddingTop(8);
                        col.Item().BorderBottom(1).BorderColor("#550000").PaddingBottom(2).Text("BACKSTORY").Bold().FontSize(10).FontColor("#550000");
                        col.Item().PaddingTop(4).Text(character.Backstory).FontSize(8);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Requiem Nexus — ").FontColor("#888888");
                    text.Span(character.Name).FontColor("#550000");
                    text.Span($" — Generated {DateTime.UtcNow:yyyy-MM-dd}").FontColor("#888888");
                });
            });
        }).GeneratePdf();
    }

    /// <inheritdoc />
    public Task<byte[]> ExportCharacterAsPdfAsync(Character character, CancellationToken cancellationToken = default) =>
        Task.Run(() => ExportCharacterAsPdf(character), cancellationToken);

    private static string Dots(int filled, int max)
    {
        filled = Math.Clamp(filled, 0, max);
        return new string('●', filled) + new string('○', max - filled);
    }
}
