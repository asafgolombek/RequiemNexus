# Rules interpretations (VtR 2e → Nexus)

Deliberate mechanical choices where the book leaves table pacing ambiguous or where the app needs a clock.

## Social maneuvering (Phase 10)

| Topic | Interpretation |
|-------|----------------|
| **Perfect impression — “1 turn”** | In Nexus, **one roll per open-Door attempt** is allowed with **no minimum real-time wait** (`TimeSpan.Zero`). At the table this is “one turn”; digitally we do not infer scene length from wall clock. |
| **Hard leverage — severity vs Humanity** | Breaking-point **severity** is an **integer supplied by the ST** when forcing Doors with hard leverage. Doors removed before the force roll use **\|severity − persuader Humanity\|**: **≤ 2** removes **one** Door, **≥ 3** removes **two** (VtR 2e Social Maneuvering). |

## Social maneuvering — auto Conditions (Phase 10.6)

Nexus applies Conditions to the **initiating PC** only (targets are chronicle NPCs without character Conditions). **No second row** is added if the character already has an **unresolved** Condition of the same `ConditionType`.

| Trigger | Condition | Notes |
|---------|-----------|--------|
| Open Door: maneuver reaches **Succeeded** (remaining Doors 0) | *Swooned* | Description references target NPC name. |
| Open Door: **dramatic failure** | *Shaken* | |
| Open Door: **exceptional success** and at least one Door opened, maneuver still **Active** | *Inspired* | CoD *Inspired*; enum appended after `Custom` to preserve stored values. |
| **Force Doors**: success | *Swooned* | Same as success outcome. |
| **Force Doors**: failure (**Burnt**) | *Shaken* | |
| **Hostile** impression for **one week** (auto-fail) | *Shaken* | Applied as the chronicle Storyteller for audit (`ApplyConditionAsync` user id). |

ST narrative door edits that set remaining Doors to 0 **do not** auto-apply *Swooned* (ST fiat).
