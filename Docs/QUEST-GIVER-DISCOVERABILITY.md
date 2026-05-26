# Quest-giver discoverability — the beacon

> **Gap.** The quest system has 6 pool archetypes + 2 starting-village quests,
> a quest log, and live counters — but a quest-giver was just another CP437
> glyph among the villagers (`r`, `f`, `P`, `c`, `b`, `h`, `p`, …). Players had
> no way to tell which NPC has a quest. All that content was effectively
> invisible.
>
> **Fix.** Quest-giver (offerer) NPCs glow a distinct **"quest available"
> color** (bright yellow `&Y`) while the quest they offer is *not yet started*,
> reverting to their themed color once it's accepted or completed. A
> recognizable "this one has something for you" signal among the villagers.

## Why color (not a floating "!")

The ZoneRenderer draws **one glyph per cell** from the top entity's
`RenderPart` (`ZoneRenderer.RenderCellCore`, ~840-924). Critically, it **fires
a `"Render"` GameEvent on the entity and reads `ColorString` back** (~904-916)
before applying lighting + `SetColor` — Qud's render-event mutation path. So a
Part can override its NPC's render **color** at render time with **zero new
infrastructure** (no overlay tilemap, MonoBehaviour, camera, or sorting-order
wiring).

The **glyph is locked before** that event fires (~894-897), so this hook can't
change the glyph. A floating "!" *over* the NPC would need a dedicated overlay
tilemap (the heavier option an Explore sweep surfaced) — **deferred**. Color is
a solid, low-risk v1 signal that also preserves the NPC's identity glyph.

## Design — `QuestBeaconPart`

`Assets/Scripts/Gameplay/Storylets/QuestBeaconPart.cs` (a `Part`):
- Fields: `Quest` (the offered quest id), `AvailableColor = "&Y"`.
- On `"Render"`: if `StoryletPart.Current` says the quest is **neither active
  nor completed** (= offerable, mirrors `IfQuestNotStarted`), set the event's
  `ColorString` to `AvailableColor`. Otherwise leave it (themed color passes
  through). Null-safe (pre-bootstrap → no-op); allocation-free (only dict/set
  lookups) so it's safe on the per-cell render path.

**States:** offerable → highlight `&Y`; active → themed (you have it, the log
tracks it); completed → themed (nothing to offer). v1 highlights *available*
only; a "ready to turn in" highlight (giver at the report stage) is a possible
follow-up.

**Refresh cadence:** the render re-evaluates each redraw, so the highlight
clears on the next zone redraw after the player accepts (redraws happen per
turn / on FOV change / after a UI overlay closes). No explicit dirty hook in v1;
if it ever feels laggy, fire `MarkFullDirty` from the `QuestStarted` event.

## Wiring — the 8 offerers

`VillagePopulationBuilder` adds `QuestBeaconPart { Quest = <id> }` to each quest
**offerer** it places (right after `SetConversation`):

| Giver convo | Quest id |
|---|---|
| RootBeerGuy_Quest | RootBeerGuyCase |
| BMO_Quest | BmoCartridge |
| Crunchy_Quest | CrunchyLocket |
| Pilgrim_Quest | HiddenShrine |
| Warren_Quest | ClearTheWarren |
| CandyTax_Quest | TheCandyTax |
| Baker_Quest | MessageForHermit |
| Strongman_Quest | StrongestInOoo |

NOT the candy citizens (tax-payers, not offerers) nor the hermit (a delivery
*recipient*) — only the NPC whose dialogue offers `StartQuest`.

## Verification
- **`QuestBeaconPartTests` (8):** offerable → `&Y`; **counter-checks** active →
  unchanged, completed → unchanged; custom color honored; null-StoryletPart
  no-crash; non-Render ignored; empty-quest ignored; save/load round-trips.
- **`VillagePopulationBuilderTests` (+1):** `BuildZone_QuestGivers_CarryMatchingBeacons`
  — a pool giver (Warren) + both starting givers (RootBeerGuy, BMO) carry a
  beacon referencing the right quest (builder↔beacon seam).
- **Regression chunk 39/39** (builder-instantiating suites + render policy +
  pool + dialogue) — the 8 `AddPart`s and the new `"Render"` handler break nothing.
- **Live (rule 7):** in the runtime, an offerable giver's `"Render"` →
  `ColorString=&Y`; after accepting → `&w` (themed). The renderer reads
  `ColorString` back → `SetColor`, so the NPC visibly turns yellow.

## Deferred
- A floating **"!"** glyph above the giver (needs a dedicated overlay tilemap).
- A distinct **"turn in here"** highlight when the giver's quest is at the
  report stage.
