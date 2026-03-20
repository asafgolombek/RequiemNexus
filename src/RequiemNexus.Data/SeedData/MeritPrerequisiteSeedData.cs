using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seeds structured merit prerequisites. Called after merits are inserted; matches by name.
/// Starts with a small set: Trained Observer, Atrocious/Cutthroat/Enticing exclusions, Heart of Stone.
/// </summary>
public static class MeritPrerequisiteSeedData
{
    /// <summary>
    /// Builds MeritPrerequisite rows for merits that have parsable structured prerequisites.
    /// </summary>
    /// <param name="meritIdsByName">Merit name -> Id (from context after seeding).</param>
    public static List<MeritPrerequisite> GetPrerequisitesToSeed(IReadOnlyDictionary<string, int> meritIdsByName)
    {
        var result = new List<MeritPrerequisite>();

        // Trained Observer: Wits 3 or Composure 3 (two OR groups)
        if (meritIdsByName.TryGetValue("Trained Observer", out int trainedObserverId))
        {
            result.Add(Create(trainedObserverId, MeritPrerequisiteType.Attribute, (int)AttributeId.Wits, 3, orGroupId: 1));
            result.Add(Create(trainedObserverId, MeritPrerequisiteType.Attribute, (int)AttributeId.Composure, 3, orGroupId: 2));
        }

        // Atrocious: Cannot have Cutthroat or Enticing ( MeritExclusion )
        if (meritIdsByName.TryGetValue("Atrocious", out int atrociousId) &&
            meritIdsByName.TryGetValue("Cutthroat", out int cutthroatId) &&
            meritIdsByName.TryGetValue("Enticing", out int enticingId))
        {
            result.Add(Create(atrociousId, MeritPrerequisiteType.MeritExclusion, cutthroatId, 0, orGroupId: 0));
            result.Add(Create(atrociousId, MeritPrerequisiteType.MeritExclusion, enticingId, 0, orGroupId: 0));
        }

        // Cutthroat: Cannot have Atrocious or Enticing
        if (meritIdsByName.TryGetValue("Cutthroat", out int cutthroatId2) &&
            meritIdsByName.TryGetValue("Atrocious", out int atrociousId2) &&
            meritIdsByName.TryGetValue("Enticing", out int enticingId2))
        {
            result.Add(Create(cutthroatId2, MeritPrerequisiteType.MeritExclusion, atrociousId2, 0, orGroupId: 0));
            result.Add(Create(cutthroatId2, MeritPrerequisiteType.MeritExclusion, enticingId2, 0, orGroupId: 0));
        }

        // Enticing: Cannot have Atrocious or Cutthroat
        if (meritIdsByName.TryGetValue("Enticing", out int enticingId3) &&
            meritIdsByName.TryGetValue("Atrocious", out int atrociousId3) &&
            meritIdsByName.TryGetValue("Cutthroat", out int cutthroatId3))
        {
            result.Add(Create(enticingId3, MeritPrerequisiteType.MeritExclusion, atrociousId3, 0, orGroupId: 0));
            result.Add(Create(enticingId3, MeritPrerequisiteType.MeritExclusion, cutthroatId3, 0, orGroupId: 0));
        }

        // Heart of Stone: Feeding Grounds 3
        if (meritIdsByName.TryGetValue("Heart of Stone", out int heartOfStoneId) &&
            meritIdsByName.TryGetValue("Feeding Grounds", out int feedingGroundsId))
        {
            result.Add(Create(heartOfStoneId, MeritPrerequisiteType.MeritRequired, feedingGroundsId, 3, orGroupId: 0));
        }

        return result;
    }

    private static MeritPrerequisite Create(int meritId, MeritPrerequisiteType type, int referenceId, int minRating, int orGroupId)
    {
        return new MeritPrerequisite
        {
            MeritId = meritId,
            PrerequisiteType = type,
            ReferenceId = referenceId,
            MinimumRating = minRating,
            OrGroupId = orGroupId,
        };
    }
}
