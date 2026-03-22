using System.Text.Json;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Official V:tR 2e merit definitions for database seeding.
/// Loads from SeedSource/merits.json when available; falls back to hardcoded <see cref="GetAllMerits"/>.
/// </summary>
public static class MeritSeedData
{
    private static readonly string[] _statusMeritNames =
    [
        "Status (Carthian)",
        "Status (Crone)",
        "Status (Invictus)",
        "Status (Lancea)",
        "Status (Ordo)",
    ];

    /// <summary>
    /// Loads merit definitions from SeedSource/merits.json when available.
    /// Maps rating ("1", "1 to 5", etc.) to ValidRatings (bullet notation).
    /// Injects the five covenant Status merits if not present in the file.
    /// Falls back to <see cref="GetAllMerits"/> when file is missing or invalid.
    /// </summary>
    public static List<Merit> LoadFromDocs()
    {
        string? seedDir = SeedSourcePathResolver.GetSeedDirectory();
        if (seedDir == null)
        {
            return GetAllMerits();
        }

        var path = Path.Combine(seedDir, "merits.json");
        if (!File.Exists(path))
        {
            return GetAllMerits();
        }

        try
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var result = new List<Merit>();
            var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                string name = el.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                string description = el.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty;
                string rating = el.TryGetProperty("rating", out var rEl) ? rEl.GetString() ?? string.Empty : string.Empty;
                string prerequisites = el.TryGetProperty("prerequisites", out var pEl) ? pEl.GetString() ?? string.Empty : string.Empty;
                string rollInfo = el.TryGetProperty("roll info", out var rollEl) ? rollEl.GetString() ?? string.Empty : string.Empty;
                string drawback = el.TryGetProperty("drawback", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                string sourceBook = el.TryGetProperty("source book", out var sEl) ? sEl.GetString() ?? string.Empty : string.Empty;

                var fullDescription = BuildDescription(description, prerequisites, rollInfo, drawback, sourceBook);
                var validRatings = MapRatingToBullets(rating);

                result.Add(new Merit
                {
                    Name = name,
                    Description = fullDescription,
                    ValidRatings = validRatings,
                    RequiresSpecification = false,
                    CanBePurchasedMultipleTimes = false,
                    IsHomebrew = false,
                });
                nameSet.Add(name);
            }

            foreach (var statusName in _statusMeritNames)
            {
                if (!nameSet.Contains(statusName))
                {
                    result.Add(new Merit
                    {
                        Name = statusName,
                        Description = "Your standing within the covenant.",
                        ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022",
                        RequiresSpecification = false,
                        CanBePurchasedMultipleTimes = false,
                        IsHomebrew = false,
                    });
                }
            }

            return result.Count > 0 ? result : GetAllMerits();
        }
        catch
        {
            return GetAllMerits();
        }
    }

    /// <summary>Returns the full list of official merits for seeding (fallback when JSON unavailable).</summary>
    public static List<Merit> GetAllMerits() =>
    [
        new() { Name = "Acute Senses", ValidRatings = "\u2022", Description = "Add Blood Potency as a bonus to use senses or identify sensory details. Exceptional successes can temporarily inflict Obsession." },
        new() { Name = "Allies", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Altar", ValidRatings = "\u2022", Description = "You've contributed to attuning a blood altar. In the presence of the altar, contributors can apply teamwork to CrÃºac rituals, even if they don't know CrÃºac, at the cost of slowing the ritual." },
        new() { Name = "Alternate Identity", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Ambidextrous", ValidRatings = "\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Anointed", ValidRatings = "\u2022\u2022", Description = "You're ordained. Once per session, you can roll Presence + Expression to preach the Raptured Condition." },
        new() { Name = "Anonymity", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Area of Expertise", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Atrocious", ValidRatings = "\u2022", Description = "Take 8-Again to evoke the monstrous Beast, but lose 10-Again to evoke or resist the seductive or competitive Beast." },
        new() { Name = "AttachÃ©", ValidRatings = "\u2022", Description = "Your Retainers each gain access to your Invictus Status in dots distributed among Contacts, Resources and Safe Place." },
        new() { Name = "Automotive Genius", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Barfly", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Bloodhound", ValidRatings = "\u2022\u2022", Description = "Smelling blood is as good as tasting it for your Kindred Senses." },
        new() { Name = "Cacophony Savvy", ValidRatings = "\u2022", Description = "You can read the Cacophony with Intelligence + Streetwise. Within a vampire's Feeding Grounds, add their (not your) dots in that Merit as bonus dice." },
        new() { Name = "Carthian Pull", ValidRatings = "\u2022", Description = "Every month, you can leverage favors to access your Carthian Status in dots of Allies, Contacts, Haven or Herd." },
        new() { Name = "Cheap Shot", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Choke Hold", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Claws of the Unholy", ValidRatings = "\u2022", Description = "When you use Unnatural Aspect claws in frenzy, their weapon rating becomes +0A." },
        new() { Name = "Close Family", ValidRatings = "\u2022", Description = "Blood sympathy manifests as if blood ties were one step closer, with a +1 bonus and 8-Again, but you can't spend Willpower for bonus dice in a scene where you've felt it." },
        new() { Name = "Closed Book", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Common Sense", ValidRatings = "\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Contacts", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Crack Driver", ValidRatings = "\u2022\u2022 or \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Cutthroat", ValidRatings = "\u2022", Description = "Take 8-Again to evoke the competitive Beast, but lose 10-Again to evoke or resist the monstrous or seductive Beast." },
        new() { Name = "Danger Sense", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Defensive Combat", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Demolisher", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Direction Sense", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Distinguished Palate", ValidRatings = "\u2022", Description = "All Taste of Blood successes are exceptional, but you lose the first Vitae ingested in a scene from any vessel without a chosen trait you consider a delicacy." },
        new() { Name = "Double Jointed", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Dream Visions", ValidRatings = "\u2022", Description = "Once a night, when meeting a new person or visiting a new place, roll Blood Potency to have had prophetic dreams that answer a question about the subject." },
        new() { Name = "Dynasty Membership", ValidRatings = "\u2022", Description = "Once per session, you can substitute your dynasty's Clan Status for your own on a Social roll." },
        new() { Name = "Eidetic Memory", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Encyclopedic Knowledge", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Enticing", ValidRatings = "\u2022", Description = "Take 8-Again to evoke the seductive Beast, but lose 10-Again to evoke or resist the monstrous or competitive Beast." },
        new() { Name = "Esoteric Armory", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Eye for the Strange", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Fame", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Fast Reflexes", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Feeding Grounds", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "You hold known influence over territory. While there, add dots in this Merit as a bonus to hunt, and to clash with the Predatory Aura." },
        new() { Name = "Fighting Finesse", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Fixer", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Fleet of Foot", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Friends in High Places", ValidRatings = "\u2022", Description = "Every month, you can open up to your Invictus Status in Doors belonging to people held under the local Invictus thumb." },
        new() { Name = "Ghoul Retainers", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "As retainer, except they have dots in the regnant's disciplines equal to half the dots in this merit, rounded up." },
        new() { Name = "Giant", ValidRatings = "\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Good Time Management", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Greyhound", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Hardy", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Herd", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "You have marks willing to offer up twice your dots in this Merit in Vitae weekly, without a roll." },
        new() { Name = "Hobbyist Clique", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Holistic Awareness", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Honey Trap", ValidRatings = "\u2022", Description = "A vampire who tastes your blood regains Willpower, and also takes a beat if it advances the Vinculum." },
        new() { Name = "I Know a Guy", ValidRatings = "\u2022", Description = "Once per story, your non-temporary dots of Allies can do double duty as dots of Retainer." },
        new() { Name = "Indomitable", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Inspiring", ValidRatings = "\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Interdisciplinary Specialty", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Invested", ValidRatings = "\u2022", Description = "Distribute your dots in Invictus Status among free dots of Herd, Mentor, Resources and Retainer, provided by the covenant." },
        new() { Name = "Investigative Aide", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Investigative Prodigy", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Iron Skin", ValidRatings = "\u2022\u2022 or \u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Iron Stamina", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Iron Will", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Kindred Status", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "As the general Status Merit, divided into City, Clan, and Covenant Status. If you hold Status in multiple covenants, your total dots of Covenant Status can't exceed five." },
        new() { Name = "Kiss of the Succubus", ValidRatings = "\u2022", Description = "When you inflict Swooning by feeding on a mortal vessel, also inflict Addicted." },
        new() { Name = "Language", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Lex Terrae", ValidRatings = "\u2022\u2022", Description = "So long as your Feeding Ground has been clearly declared, vampires who feed there without your permission wake the next night and vomit the Vitae out as useless, and are visibly marked for a week." },
        new() { Name = "Library", ValidRatings = "\u2022 to \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Lineage", ValidRatings = "\u2022", Description = "Once per session, you can borrow one dot of your sire's Allies, Contacts, Mentor, Resources, or Status." },
        new() { Name = "Lorekeeper", ValidRatings = "\u2022", Description = "Your lorehouse attracts occultists. Distribute your dots in the Library Merit among bonus dots in Retainer and Herd." },
        new() { Name = "Mandate from the Masses", ValidRatings = "\u2022\u2022\u2022\u2022\u2022", Description = "When you preside over a majority vote of local Carthians to condemn an enemy of the people, you can voluntarily lose access to a dot of Willpower to strip the enemy of Blood Potency proportional to the Carthian vote. Lost dots return when the enemy goes into exile or either you or the enemy are destroyed." },
        new() { Name = "Meditative Mind", ValidRatings = "\u2022, \u2022\u2022, or \u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Mentor", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Multilingual", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Night Doctor Surgery", ValidRatings = "\u2022\u2022\u2022", Description = "Roll Intelligence + Medicine to treat a vampire medically for an hour, reducing lethal damage to bashing." },
        new() { Name = "Notary", ValidRatings = "\u2022\u2022\u2022", Description = "You're empowered by the Invictus to preside over Invictus Oaths. Invictus Status can't be used in Social rolls against you, and once a month, you can call upon one dot of Allies, Contacts, Herd, Mentor or Resources from the covenant." },
        new() { Name = "Oath of Action", ValidRatings = "\u2022\u2022\u2022\u2022", Description = "Swear a labor for your liege. For the duration of the Oath, you may access one of your liege's Disciplines (including bloodline gifts), while your liege's Blood Potency increases by one. The Oath concludes when the labor is complete or a month passes, and respectively the liege or the vassal suffers aggravated damage equal to the Discipline dots offered. You can only be party to one Oath of Action at a time." },
        new() { Name = "Oath of Fealty", ValidRatings = "\u2022", Description = "You can draw up to your Invictus Status in Vitae from your liege each week, without feeding from her. Your liege can tell when you lie to her. You can only swear an Oath of Fealty to one liege at a time." },
        new() { Name = "Oath of Penance", ValidRatings = "\u2022\u2022\u2022", Description = "For the duration of the Oath, every tenth point of Vitae you ingest is mystically transmitted to your liege as Kindred Vitae, and you are denied the benefits of any other Invictus Oath. Your liege cannot use Disciplines against you." },
        new() { Name = "Oath of Serfdom", ValidRatings = "\u2022\u2022", Description = "Receive a feudal territory from your liege. While there, or when defending your liege, you gain a free dot of Celerity, Resilience or Vigor. You intuit when another predatory aura crosses into your land and from where. Your liege adds your dots in Feeding Ground as bonus dice against you, and ignores any blood bond you may impose." },
        new() { Name = "Pack Alpha", ValidRatings = "\u2022", Description = "Decide how you ritually mark a vampire or ghoul as part of your pack. Pack members take 8-Again to teamwork support rolls. When questioned by your pack and you don't cow them, lose Willpower. Exile from the pack is violent and lasting." },
        new() { Name = "Patient", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Plausible Deniability", ValidRatings = "\u2022\u2022\u2022\u2022", Description = "Supernatural means can't prove your guilt of crimes against domain or Tradition, diablerie doesn't stain your aura, and you penalize nonmagical efforts to perceive your guilt by your Carthian Status. You can't use your Kindred Status against anyone who ascertains your guilt anyway." },
        new() { Name = "Pusher", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Quick Draw", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Relentless", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Resources", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Retainer", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Right of Return", ValidRatings = "\u2022\u2022", Description = "You're authorized to participate in multiple covenants. Your total maximum of Covenant Status dots ignores your dots of Carthian Status, and you improve your Social Maneuvering impression with your other affiliated covenants, but lose 10-Again to conceal suspicious behavior from them." },
        new() { Name = "Safe Place", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Secret Society Junkie", ValidRatings = "\u2022", Description = "Dots in Status or Mystery Cult Initiation representing human secret societies also count as dots of Herd." },
        new() { Name = "Seizing the Edge", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Shiv", ValidRatings = "\u2022 or \u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Sleight of Hand", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Small Unit Tactics", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Small-Framed", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Speaker for the Silent", ValidRatings = "\u2022\u2022\u2022", Description = "You've learned to voluntarily channel the will of a torpid elder. You can cut off an open channel with a point of Willpower." },
        new() { Name = "Spin Doctor", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Staff", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Status", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Strength of Resolution", ValidRatings = "\u2022", Description = "When a supernatural power is used to compel you to violate the law of the domain, add your Carthian Status dots as dice to contest it." },
        new() { Name = "Striking Looks", ValidRatings = "\u2022 to \u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Swarm Form", ValidRatings = "\u2022\u2022", Description = "You can become a swarm of Size 0 or 1 creatures when you take the Beast's Skin. Expand over a space up to (5 Ã— Blood Potency) yards in diameter. Your attacks roll Strength + Brawl, ignore Defense, and deal lethal damage, while attacks against you that don't encompass wide swaths can only deal a point of damage at most." },
        new() { Name = "Sworn", ValidRatings = "\u2022", Description = "Swear yourself to frequent duties either to the silencers of the Axe, learned of the Dying Light, or leaders of the Mysteries. Distribute your dots in Ordo Status among bonus dots of Mentor and Retainer." },
        new() { Name = "Sympathetic", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Table Turner", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Takes One to Know One", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Taste", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "The Mother-Daughter Bond", ValidRatings = "\u2022", Description = "Apply the benefits of the True Friend Merit to any Acolyte Mentor." },
        new() { Name = "Tolerance for Biology", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Touchstone", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "You have an extra Touchstone for each dot in this Merit. Each Touchstone attaches to the next level of Humanity down after the last." },
        new() { Name = "Trained Observer", ValidRatings = "\u2022 or \u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "True Friend", ValidRatings = "\u2022\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Undead Menses", ValidRatings = "\u2022\u2022", Description = "You produce unnatural menstrual blood that, once per night when applied, can grant 8-Again to a CrÃºac ritual or reduce Resistance against a Discipline by up to your Blood Potency." },
        new() { Name = "Unnatural Affinity", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Choose another type of supernatural creature for each dot in this Merit. You can feed on their Vitae as if they were Kindred." },
        new() { Name = "Unsettling Gaze", ValidRatings = "\u2022", Description = "When you roll an exceptional success to infect a victim with Integrity or Humanity greater than yours with the monstrous Beast, your victim experiences a breaking point. If your Humanity is greater than 2, so do you." },
        new() { Name = "Untouchable", ValidRatings = "\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Vice-Ridden", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Virtuous", ValidRatings = "\u2022\u2022", Description = "Universal Merit: Imported from CofD" },
        new() { Name = "Where the Bodies Are Buried", ValidRatings = "\u2022\u2022", Description = "Once per story for every dot in this Merit, your notes on dirty work can turn up dirt on a vampire whose associations you know." },
        new() { Name = "Status (Carthian)", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Your standing within the Carthian Movement." },
        new() { Name = "Status (Crone)", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Your standing within the Circle of the Crone." },
        new() { Name = "Status (Invictus)", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Your standing within the Invictus." },
        new() { Name = "Status (Lancea)", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Your standing within the Lancea et Sanctum." },
        new() { Name = "Status (Ordo)", ValidRatings = "\u2022 to \u2022\u2022\u2022\u2022\u2022", Description = "Your standing within the Ordo Dracul." },
    ];

    private static string BuildDescription(string description, string prerequisites, string rollInfo, string drawback, string sourceBook)
    {
        var parts = new List<string> { description };
        bool HasContent(string s) => !string.IsNullOrWhiteSpace(s) &&
            !string.Equals(s, "None specified in the text.", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(s, "None", StringComparison.OrdinalIgnoreCase);

        if (HasContent(prerequisites))
        {
            parts.Add("Prerequisites: " + prerequisites.TrimEnd('.'));
        }

        if (HasContent(rollInfo))
        {
            parts.Add("Roll: " + rollInfo.TrimEnd('.'));
        }

        if (HasContent(drawback))
        {
            parts.Add("Drawback: " + drawback.TrimEnd('.'));
        }

        if (HasContent(sourceBook))
        {
            parts.Add("Source: " + sourceBook.TrimEnd('.'));
        }

        return string.Join("\n\n", parts);
    }

    private static string Dots(int count) =>
        count > 0 ? new string('\u2022', count) : string.Empty;

    private static string MapRatingToBullets(string rating)
    {
        const string defaultRange = "\u2022 to \u2022\u2022\u2022\u2022\u2022";

        if (string.IsNullOrWhiteSpace(rating))
        {
            return defaultRange;
        }

        rating = rating.Trim();
        if (string.Equals(rating, "varies", StringComparison.OrdinalIgnoreCase))
        {
            return defaultRange;
        }

        var result = rating
            .Replace("1 to 5", $"{Dots(1)} to {Dots(5)}")
            .Replace("1 to 4", $"{Dots(1)} to {Dots(4)}")
            .Replace("1 to 3", $"{Dots(1)} to {Dots(3)}")
            .Replace("1 or 3", $"{Dots(1)} or {Dots(3)}")
            .Replace("2 or 4", $"{Dots(2)} or {Dots(4)}")
            .Replace("1, 2, or 3", $"{Dots(1)}, {Dots(2)}, or {Dots(3)}")
            .Replace("1, 2, or 4", $"{Dots(1)}, {Dots(2)}, or {Dots(4)}")
            .Replace("1, 3, or 5", $"{Dots(1)}, {Dots(3)}, or {Dots(5)}")
            .Replace("1, 2, 3, or 4", $"{Dots(1)}, {Dots(2)}, {Dots(3)}, or {Dots(4)}")
            .Replace("1, 2, 3, 4, or 5", $"{Dots(1)}, {Dots(2)}, {Dots(3)}, {Dots(4)}, or {Dots(5)}");

        for (int i = 5; i >= 1; i--)
        {
            var bullets = new string('\u2022', i);
            result = result.Replace(i.ToString(), bullets);
        }

        return string.IsNullOrEmpty(result) ? defaultRange : result;
    }
}
