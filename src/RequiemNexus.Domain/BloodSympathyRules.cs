namespace RequiemNexus.Domain;

/// <summary>
/// Stateless rules for Blood Sympathy per V:tR 2e pp. 120–121.
/// </summary>
public static class BloodSympathyRules
{
    /// <summary>
    /// Returns the Blood Sympathy rating for a character.
    /// Rating = Blood Potency ÷ 2 (rounded down). Minimum 0.
    /// Characters with Blood Potency below 2 have no Blood Sympathy (rating 0).
    /// </summary>
    /// <param name="bloodPotency">The character's Blood Potency.</param>
    /// <returns>The sympathy rating used for range and pool bonuses.</returns>
    public static int ComputeRating(int bloodPotency) => bloodPotency < 2 ? 0 : bloodPotency / 2;

    /// <summary>
    /// Returns the maximum degrees of separation at which Blood Sympathy is active between two Kindred.
    /// This is the minimum of both participants' ratings.
    /// </summary>
    /// <param name="ratingA">First character's Blood Sympathy rating.</param>
    /// <param name="ratingB">Second character's Blood Sympathy rating.</param>
    /// <returns>The inclusive maximum graph distance for an active sympathy link.</returns>
    public static int EffectiveRange(int ratingA, int ratingB) => Math.Min(ratingA, ratingB);

    /// <summary>
    /// Returns the bonus dice granted when assisting a kin at the given degree of separation.
    /// Degree 1 = parent/child; degree 2 = grandparent/grandchild; etc.
    /// Bonus dice = rating ÷ degree (rounded down, minimum 0).
    /// </summary>
    /// <param name="rating">The assisting character's Blood Sympathy rating.</param>
    /// <param name="degree">Positive degree of separation to the kin being assisted.</param>
    /// <returns>Bonus dice, or zero when degree is not positive.</returns>
    public static int BonusDiceForDegree(int rating, int degree) =>
        degree <= 0 ? 0 : rating / degree;
}
