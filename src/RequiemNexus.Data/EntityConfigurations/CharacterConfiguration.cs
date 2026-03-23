using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="Character"/> and its relationships.
/// </summary>
public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder
            .HasOne(c => c.User)
            .WithMany(u => u.Characters)
            .HasForeignKey(c => c.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.ApplicationUserId);

        builder
            .HasOne(c => c.Clan)
            .WithMany()
            .HasForeignKey(c => c.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ClanId);

        builder
            .HasOne(c => c.Covenant)
            .WithMany()
            .HasForeignKey(c => c.CovenantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.CovenantId);

        builder
            .HasOne(c => c.Campaign)
            .WithMany(c => c.Characters)
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(c => c.SireCharacter)
            .WithMany(c => c.Childer)
            .HasForeignKey(c => c.SireCharacterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(c => c.SireNpc)
            .WithMany(n => n.CharactersWithThisNpcSire)
            .HasForeignKey(c => c.SireNpcId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.CampaignId);
        builder.HasIndex(c => c.SireCharacterId);
        builder.HasIndex(c => c.SireNpcId);

        builder
            .HasOne(c => c.ChosenMysteryScale)
            .WithMany()
            .HasForeignKey(c => c.ChosenMysteryScaleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ChosenMysteryScaleId);

        builder
            .HasOne(c => c.PendingChosenMysteryScale)
            .WithMany()
            .HasForeignKey(c => c.PendingChosenMysteryScaleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.PendingChosenMysteryScaleId);
    }
}
