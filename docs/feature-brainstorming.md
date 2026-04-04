# 🩸 Feature Brainstorming: The Next Horizon

Drawing inspiration from platforms like **D&D Beyond** and **Roll20**, this document outlines potential features to further immerse players and Storytellers in the **Danse Macabre**. All ideas must align with the **Antigravity Philosophy** (reducing cognitive weight) and the **Modern Gothic** aesthetic.

---

## 📚 1. The Living Compendium (D&D Beyond Style)
While we have extensive seed data, the "Reading" experience can be elevated from a database view to a digital book.

- **The Grimoire Search:** A global, lightning-fast search bar that finds Disciplines, Devotions, Rites, and Equipment with "Quick-Roll" buttons directly in the results.
- **Thematic Category Views:** Instead of simple lists, create "Chapters" for each Clan or Covenant, featuring lore snippets alongside mechanical data.
- **Interactive Rule Tooltips:** Hovering over a keyword (e.g., "9-again," "Stain," "Breaking Point") in a power description surfaces a "Veil" (popover) with the exact rule text.

## 🕸️ 2. The Web of Night (Visual Relationship Graph)
V:tR is a game of social ties. We currently track Blood Sympathy and Sires, but a visual representation is missing.

- **Dynamic Relationship Map:** A force-directed graph (using Mermaid or D3.js) showing the connections between the Coterie, their Touchstones, and NPCs.
- **The Political Landscape:** Color-coded nodes by Covenant or Clan to visualize the "power map" of the city at a glance.
- **Secret Ties:** A "Masquerade" mode where Storytellers can see hidden connections (e.g., a "secret sire") that players cannot.

## 🗺️ 3. Strategic City Mapping (The Haven & The Hunt)
*Note: This is not a tactical combat grid, but a strategic chronicle tool.*

- **The Territory Overlay:** A map of the city (image upload or OSM integration) with pins for havens, feeding territories, and known "Barrens."
- **Feeding Ratings Visualized:** Use heatmaps or colored borders to show which districts have higher "Feeding Ratings" (Phase 16a).
- **Chronicle Hotspots:** STs can drop "Scene Pins" that link directly to Session Notes or Encounter logs.

## 🎭 4. Storyteller Narrative Arsenal
Tools to help the ST manage the story without getting bogged down in math.

- **Archetype NPC Generator:** One-click generation of common mortal NPCs (e.g., "Curious Cop," "Wealthy Socialite," "Gang Member") with standardized stat blocks.
- **The Rumor Mill:** A shared "Lore" section where STs can post "Public Knowledge" vs. "Covenant Secrets," with discovery tied to successful Investigation rolls.
- **Session Journaling & Recaps:** A dedicated blog-like section for session recaps. STs can "Award Beats" directly to players who contribute to the journal.
- **SignalR "Whispers":** Private, real-time messages between the ST and a specific player (e.g., for Auspex results or private Beast prompts).

## 🔊 5. Immersive Atmosphere (The Quickening)
Roll20-style immersion tools adapted for the "bone-white and crimson" aesthetic.

- **Atmospheric Soundboard:** Integration with YouTube/Spotify or local audio to sync "Scene Music" across all connected clients.
- **Visual "Glimmer" Effects:** Subtle UI shifts when a scene is "Tense" (flickering candle effect) or "Violent" (crimson vignettes).
- **Digital Dice Skins:** Purely aesthetic dice visuals (e.g., "Glass-Morphed," "Blood-Filled," "Ancient Stone") for the Dice Nexus.

## 🧪 6. The Homebrew Workshop
- **Custom Content Creator:** A UI for users to create their own Bloodlines, Devotions, or Sorcery Rites that are immediately usable in their chronicles.
- **Content Packs:** Export your custom creations as a "Nexus Pack" (JSON) to share with other Storytellers.

---

## ⚖️ Strategic Filter (Antigravity Check)
*How do we ensure these don't add bloat?*

1. **Contextual Visibility:** Compendium data should only show what's relevant to the current character's Clan/Covenant by default.
2. **Opt-in Mapping:** The city map is an optional tool for the ST; the game works perfectly without it.
3. **Implicit to Explicit:** Any "Whisper" or "Rumor" should be archivable in the chronicle log for future reference.
