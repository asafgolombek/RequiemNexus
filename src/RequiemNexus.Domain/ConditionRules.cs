using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain;

/// <summary>
/// Canonical rules for Conditions and Tilts (VtR 2e).
/// Stateless — register as a singleton.
/// </summary>
public class ConditionRules : IConditionRules
{
    /// <inheritdoc />
    public string GetConditionDescription(ConditionType type) => type switch
    {
        ConditionType.Guilty =>
            "You feel responsible for a harm done to another. Resolve: make amends or accept blame publicly.",
        ConditionType.Swooned =>
            "You are infatuated with someone. Resolve: act against your own interests for that person's sake.",
        ConditionType.Tempted =>
            "You are tempted to act against a Touchstone or Virtue. Resolve: give in or spend a Willpower point to resist.",
        ConditionType.Shaken =>
            "Your confidence is shattered. Resolve: succeed at a risky task without spending Willpower.",
        ConditionType.Notoriety =>
            "Your reputation has taken a public hit. Resolve: a significant public act of redemption.",
        ConditionType.Leveraged =>
            "Someone holds leverage over you. Resolve: remove the leverage or comply with the demand.",
        ConditionType.Exhausted =>
            "You are overwhelmed and exhausted. You suffer −2 to all actions. Resolve: rest or succeed at a Composure roll.",
        ConditionType.Despondent =>
            "You have fallen into hopeless despair and cannot act proactively. Resolve: receive genuine hope from an outside source.",
        ConditionType.Frightened =>
            "You are gripped by deep fear. Resolve: flee from the source of fear or overcome it through direct action.",
        ConditionType.Bleeding =>
            "You are bleeding out. You suffer 1 Lethal damage per turn. Resolve: a successful Medicine roll.",
        ConditionType.OnFire =>
            "You are on fire and take escalating Aggravated damage each turn. Resolve: extinguish the flames.",
        ConditionType.Immolating =>
            "You suffer from the touch of fire or sunlight. Resolve: remove the source.",
        ConditionType.Wassail =>
            "Your Beast has taken control. You are in Wassail. Resolve: spend Willpower, restore Humanity, or be physically restrained.",
        ConditionType.Provoked =>
            "Your Beast is close to the surface. −1 to Composure rolls. Resolve: avoid Beast triggers for a scene.",
        ConditionType.Custom =>
            "A custom Condition defined by the Storyteller.",
        _ => type.ToString(),
    };

    /// <inheritdoc />
    public string GetTiltDescription(TiltType type) => type switch
    {
        TiltType.DimLight =>
            "Dim or absent lighting. −2 to all vision-based rolls.",
        TiltType.Concealment =>
            "Target is obscured. −2 to ranged attacks against them.",
        TiltType.UnstableGround =>
            "Ground is slippery or unstable. −1 to all physical actions.",
        TiltType.KnockedDown =>
            "Character is on the ground. −2 to Defense until they spend an action to stand.",
        TiltType.Stunned =>
            "Character is dazed and cannot take an action this turn.",
        TiltType.Blinded =>
            "Character cannot see. −3 to all attack and Perception rolls.",
        TiltType.ArmWrack =>
            "One arm is incapacitated. Cannot perform two-handed actions.",
        TiltType.LegWrack =>
            "One leg is incapacitated. Speed reduced to 1; cannot Dodge.",
        TiltType.Immobilized =>
            "Character is pinned and cannot move. Attacks against them gain +2.",
        TiltType.Frenzy =>
            "Character is in frenzy. The Beast controls their actions; Disciplines requiring focus are unavailable.",
        TiltType.Rotschreck =>
            "Character is paralysed by Rotschreck. They must flee or cower from the source.",
        TiltType.Custom =>
            "A custom Tilt defined by the Storyteller.",
        _ => type.ToString(),
    };

    /// <inheritdoc />
    public bool AwardsBeatOnResolve(ConditionType type)
        => type != ConditionType.Custom;

    /// <inheritdoc />
    public IReadOnlyList<string> GetTiltEffects(IEnumerable<TiltType> activeTilts)
    {
        List<string> effects = [];

        foreach (TiltType tilt in activeTilts)
        {
            switch (tilt)
            {
                case TiltType.DimLight:
                    effects.Add("−2 to vision-based rolls (Dim Light)");
                    break;
                case TiltType.Concealment:
                    effects.Add("−2 to ranged attacks (Concealment)");
                    break;
                case TiltType.UnstableGround:
                    effects.Add("−1 to physical actions (Unstable Ground)");
                    break;
                case TiltType.KnockedDown:
                    effects.Add("−2 to Defense (Knocked Down)");
                    break;
                case TiltType.Stunned:
                    effects.Add("Cannot act this turn (Stunned)");
                    break;
                case TiltType.Blinded:
                    effects.Add("−3 to attack and Perception rolls (Blinded)");
                    break;
                case TiltType.ArmWrack:
                    effects.Add("Cannot perform two-handed actions (Arm Wrack)");
                    break;
                case TiltType.LegWrack:
                    effects.Add("Speed 1, cannot Dodge (Leg Wrack)");
                    break;
                case TiltType.Immobilized:
                    effects.Add("Cannot move; attackers gain +2 (Immobilized)");
                    break;
                case TiltType.Frenzy:
                    effects.Add("Beast in control; focus Disciplines unavailable (Frenzy)");
                    break;
                case TiltType.Rotschreck:
                    effects.Add("Must flee or cower from source (Rötschreck)");
                    break;
            }
        }

        return effects.AsReadOnly();
    }
}
