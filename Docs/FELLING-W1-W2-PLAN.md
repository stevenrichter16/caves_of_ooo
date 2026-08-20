# W1 (finish) + W2 — The Spread & Sill close-out, then The Beating

> **Status: W1-remainder ✅ SHIPPED 2026-08-18 (SM0–SM8, log in §2.3).
> W2 scoped at content-readiness level; detailed sub-milestones written
> at W2's start per `Docs/FELLING-IMPLEMENTATION-PLAN.md` §3's own
> convention ("detailed at phase start").**
>
> Written 2026-08-15/16. Continues
> `Docs/FELLING-IMPLEMENTATION-PLAN.md` (W0 shipped 2026-08-11; W1.1
> formations + W1.2 bestiary shipped 2026-08-12, commits `8c2aecf0`,
> `ffe01efa`, `f155380e`). Sources: `Docs/FELLING-WORLD-DESIGN.md`
> §§2,3.1,3.3,5-9 (design doc — NOT canon, see §1.2 below), `Lore/`
> (canon), two Explore-agent sweeps (code state, lore sourcing) plus
> direct verification of every code claim used below.

---

## 0. Scope

**This session: finish W1, then W2.** W1 remainder = FlowerField
charm-terrain + the bleed-wilt instrument, folk shrines, three new
stamps (RiverShrine/MillStead/FestivalField), Spread container loot,
the Sill inciting storylet. W2 = The Beating's formations, Parched +
glare, Tent-Right camps + the three-day oath, Wellmeet/First Tent as
differentiated places, salt economy, the tenth fire, the Last Counter.

---

## 1. Verification sweep

### 1.1 What's actually shipped (re-confirmed against `git log`, not memory)

| Phase | Status | Evidence |
|---|---|---|
| W0 — authored map, biome enum, tier table, WorldClock, factions, crossroads retired | ✅ shipped | commits through `f155380e~1` |
| W1.1 — Spread formations (Hedgerow, FieldStrips, OldRoad, FlowerMeadow, Fallow, RiverMeadow) | ✅ shipped | `8c2aecf0`, `ffe01efa` |
| W1.2 — Spread population tables (not loot) | ✅ shipped | `f155380e` |
| W1 remainder (this doc, §2) | ✅ shipped 2026-08-18 | `31b2247d`…SM8, log §2.3 |
| W2 (this doc, §7) | 🔲 scoped, not detailed | — |

### 1.2 Design doc vs. canon — corrections table

`Docs/FELLING-WORLD-DESIGN.md` is a **design document written against
canon, not canon itself**. The lore sweep (fresh read of `Lore/`, zero
reliance on the design doc's paraphrases) found several places where
the design doc's specific wording has **no canon source** — not
contradicting canon, just invented dressing on top of it. Corrected
here so this plan cites the real thing:

| Design doc says | Canon says | Cite | What this plan does |
|---|---|---|---|
| Shrine offerings: "bread, salt, a small vessel... for whichever force is listening this season" | Only **carved tokens** are canon: "river-offerings (carved tokens left for the river-god)" | `Lore/History/08_MaterialCulture.md:122` | RiverShrine's examine text uses carved tokens, not bread/salt/vessel |
| "Festival contracts" (charm-as-paid-service) | Charms are kin-transmitted folk practice, not a service economy; "It is cultural, not divine" | `Lore/History/09_Magic.md:36,52,79` | No contract/quest system invented for FestivalField; it's atmosphere |
| "The flowers aren't taking" | "Festival-flowers wilt before noon" | `Lore/History/09_Magic.md:157` | FlowerField's shortened lifespan is the mechanic; no invented quote used in-game |
| Trade-post "every second village" | "a presence in nearly every place" (Concord ubiquity, not a density rule) | `Lore/Factions/04_SaccharineConcord.md:98` | Not a placement rule this plan needs — Sill/Gantry already have Concord per `02_Geography.md:57-58` |
| No explicit rule that folk shrines omit god-names | No such rule stated; closest support: folk-gods are "*not* the Six" and Ylaes's name is "Never written" (a different, unrelated rule) | `Lore/History/03_History.md:128`; `Lore/History/07_Characters.md:23` | Applied as a **safe design constraint** (no Six names at folk shrines), not quoted as canon |

**No corrections needed for W2's canon-critical content** — the oath
text (`Lore/Codex/04_TentRightOath.md`), the three-day mechanics, the
worst-crime ordering (breaking hospitality > well-poisoning), the
salt-economy chain, and the tenth fire's Mystery-Ledger wording are
all verified verbatim in the lore sweep (full detail in the sweep
transcript; load-bearing quotes reproduced in §7 below when W2 needs
them).

### 1.3 Code-state corrections (re-verified myself, not taken on trust)

| Claim | Verified | Cite |
|---|---|---|
| Examine text: `Objects.json` writes `{"Key":"Description",...}`; `ExaminablePart` only has a `Text` field | **Confirmed — real bug.** `python3` scan: 40 `Description` keys under `Examinable` parts, 0 `Text` keys. `ExaminablePart.cs:39` has only `Text`. `EntityFactory.ApplyParameters` binds by exact field/property name and silently `continue`s on a miss. | grep + `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs:39`; `Assets/Scripts/Data/Factories/EntityFactory.cs:255-277` |
| `StampCatalog.For(Spread)` delegates to Jungle | **Confirmed.** `case BiomeType.Spread: return For(BiomeType.Jungle);` | `Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs:73` |
| `ContainerPlacementService.PoolFor` gives Spread wilderness containers the Jungle pool | **Confirmed, but narrower than assumed** — `Village`/`Camp` zone-kind wins BEFORE biome (`PoolFor:233`), so village interiors already use the generic `SettlementPool`. Only true wilderness cells (formations) get `JunglePool` (WovenBasket/HollowLog/Urn/Sack). | `ContainerPlacementService.cs:231-248` |
| Loot table content itself isn't jungle-flavored — `CrateT1-3`/`SackT1-3`/`StrongBoxT1-3` already exist and are generic | **Confirmed.** All 9 tables present in `LootTables.json`. | grep over `LootTables.json` |
| No non-quest storylet has ever fired | **Confirmed.** All 11 shipped storylet files have populated `Quest.Stages` → `IsQuest==true` → pass 1A skips every one (`if (s.IsQuest) continue;`). | `Assets/Scripts/Gameplay/Storylets/StoryletPart.cs:512` |
| No "player is in zone X" predicate exists | **Confirmed** by grep of the full predicate list (28 names, none location-based). A usable substitute exists: `SettlementRuntime.ActiveZone` is a static `Zone` kept current by every zone-transition site. | `Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs` (RegisterDefaults); `Assets/Scripts/Gameplay/Settlements/SettlementRuntime.cs:7`; writers at `GameBootstrap.cs:372,946`, `InputHandler.cs:855` |
| `TileStateSourcePart` can write a liquid coating or the 3 energy channels, but has no residue field | **Confirmed.** `Seed()` only calls `WriteCoating` and `SeedEnergyAt`. `ZoneTileState.WriteResidue(x,y,id,turns)` exists and is unused by this part. | `TileStateSourcePart.cs:97-114`; `ZoneTileState.cs:171` |
| No Urqu-bleed substrate exists anywhere in code | **Confirmed.** Zero hits for Urqu/Thinning/BleedLevel outside unrelated `Bleeding`/combat matches. Precedent for adding a minimal zone-scoped field exists: `Zone.AmbientLevel` (float, default const, zero writers until W0.2's light system arrived later). | grep; `Zone.cs:42,49` |
| All 16 named places generate as one identical generic village | **Confirmed.** `CreateVillagePipeline` never reads `Place.Faction`; only `Name`/`Tier` differ. | `OverworldZoneManager.cs:342-376` |
| `WantsMineralPart` exists but zero shipped blueprints use it | **Confirmed.** Scan of all 349 blueprints: 0 `WantsMineral` parts. Only a dev scenario constructs one. Salt mining works (`PaleSaltVein`→`Harvestable`), salt-for-reputation trade does not (unreachable). | `WantsMineralPart.cs`; scenario at `ItemEnhancementShowcase.cs:166` |
| `SackT1-3`/`CrateT1-3`/`StrongBoxT1-3` loot tables exist | **Confirmed** (see above). | — |

---

## 2. W1 remainder — detailed sub-milestone breakdown

### 2.1 Content-readiness

| Sub-feature | Readiness | Why |
|---|---|---|
| Fix `Examinable` Description binding | 🟢 | one property, mechanical, isolated |
| `TileStateSourcePart` residue capability | 🟢 | mirrors the shipped `Coating` branch exactly |
| `Zone.UrquBleedLevel` | 🟢 | mirrors the shipped `AmbientLevel` precedent exactly |
| FlowerField blueprint + bleed-wilt wiring | 🟢 | all substrate pieces exist after the above three |
| RiverShrine / MillStead / FestivalField stamps | 🟢 | `StructureStamp`/`LandmarkBuilder` substrate ships and is well-exercised (7 existing catalogs) |
| Spread container pool | 🟢 | zero new content — reuses existing generic tables |
| Sill voice card | 🟡 | one specimen text exists (`Lore/Codex/12_SariStory_Sill.md`); no card. Writing the card IS the readiness fix — small, templated on the other 10 cards |
| Sill inciting storylet | 🟡 | needs one new predicate (`IfPlayerInZone`); everything else (facts, effects, one-shot firing) ships |

### 2.2 Sub-milestones (smallest blast radius first)

#### SM0 — Fix the Examinable `Description`→`Text` binding bug

Pre-existing, found by the sweep, blocks every examine text this
plan writes. `ExaminablePart` gets one alias property:

```csharp
/// <summary>Alias for <see cref="Text"/> — 40 shipped blueprints
/// author their examine copy under the JSON key "Description"
/// (EntityFactory binds params by exact field/property name; the
/// mismatch was silent because ApplyParameters no-ops on a miss).
/// A property, not a rename, so old and new content both work.</summary>
public string Description { get => Text; set => Text = value; }
```

**RED test first**: build an entity from a blueprint carrying
`{"Key":"Description","Value":"..."}`, assert the examine line
contains it. Confirm RED against current `ExaminablePart` (no such
property exists → reflection miss → empty `Text`). Then implement.

**Counter-check**: a blueprint using `Text` directly still works
(the property setter routes to the same backing field either way —
one test asserting both keys produce identical output).

**Adversarial**: both keys present in one blueprint (last-applied
wins — `ApplyParameters`' iteration order, pin whichever it is,
don't guess); empty string; null Description skipped (existing
`ApplyParameters` null-guard, unchanged).

Files: `ExaminablePart.cs` (+1 property); `ExaminablePartTests.cs`
(new tests); no JSON changes — this fixes the 40 existing blueprints
for free, which is itself worth living-doc note-taking (a
game-wide content fix riding a W1 commit — SCOPE DIVERGENCE section
in the commit body, not hidden).

#### SM1 — `TileStateSourcePart` residue capability

```csharp
public string Residue = "";
public int ResidueTurns = 0;
```
`Seed()` gains, mirroring the `Coating` branch exactly:
```csharp
if (!string.IsNullOrEmpty(Residue) && ResidueTurns > 0)
    zone.TileState.WriteResidue(x, y, Residue, ResidueTurns);
```

**RED**: a part with `Residue="petals", ResidueTurns=50` seeded onto
a zone → `zone.TileState.CoatingTurns` is irrelevant, assert via the
residue reader (`ZoneTileState` exposes residue turns the same shape
as coatings — confirm exact accessor name when writing the test;
`CellStatusReadout` already reads `state.Residues` generically, so
the existing look-mode plumbing picks this up with zero further
wiring — a nice byproduct, not a new feature).

**Counter-check**: a part with `Coating` set but no `Residue` writes
only the coating (existing behavior unchanged) — regression pin.

Files: `TileStateSourcePart.cs`; a small addition to
`TileStateSourcePartTests.cs` (or wherever its existing tests live —
confirm file at implementation time).

#### SM2 — `Zone.UrquBleedLevel`

```csharp
/// <summary>How strongly Urqu-pressure reaches this zone, 0 =
/// none (the corpus's own Tier-1 baseline — Lore/History/
/// 02_Geography.md §Urqu-bleed table). Mirrors AmbientLevel's
/// shape exactly (Zone.cs:42): a field seeded here, with a real
/// writer arriving in a LATER phase (W7's bleed mask, Docs/
/// FELLING-WORLD-DESIGN.md §7.2). Nothing writes this above 0 in
/// W1/W2 — the flower-charm instrument it feeds is provably
/// dormant right now, which is thematically correct: the Thinning
/// has not escalated yet at game start.</summary>
public float UrquBleedLevel = 0f;
```

**RED/GREEN is trivial** (default 0, settable) — this SM's real test
value is in SM3, which proves the *consumer* behaves correctly at
both 0 and >0. Ship this SM with just the field + a one-line default
test, since a field with no consumer yet is not itself a testable
behavior — CLAUDE.md's own rule ("does this commit introduce
production behavior? is there a test covering it?") is satisfied by
folding this into SM3's commit rather than a bare field-add commit
with no behavior to test. **Decision: SM2 merges into SM3.**

#### SM3 — FlowerField blueprint + the bleed-wilt instrument

> **As shipped, this differs from the text below in two ways** (§2.3
> SM8 F1/F2): the residue is a 4-turn lease, not the flower's duration;
> and the formula lives on the entity (`FlowerCharmPart`, numbers in
> `Objects.json`), not in the formation builder — so stamp-placed and
> formation-placed flowers wilt identically. The plan text is kept as
> written so the correction is visible.

New blueprint `FlowerField` (`Objects.json`, `Inherits: Terrain`,
additive — `CharmFlowers` is untouched, still exists for anything
else that references it):
- `Render`: `*` `&M` layer 0 (matches `CharmFlowers`' existing look)
- `Examinable`: `Description: "petals someone conjured for the
  season — carved tokens half-buried underneath, for whichever
  river-god still gets the offerings here."` (carved-token flavor
  per §1.2's correction; no Six name, no "listening this season"
  invented quote)
- `TileStateSource`: `Residue: "petals", ResidueTurns: <computed at
  spawn, see below>`
- `Lifespan`: `TurnsRemaining: <same computed value>` — the entity
  self-removes when the residue would have expired anyway; the two
  numbers are set equal deliberately (scenery and status agree).

**Bleed-wilt formula** (small, in the placement code, not a new
system): `duration = BaseDuration - (int)(zone.UrquBleedLevel *
WiltPerBleedUnit)`, clamped to a minimum so it never goes ≤0.
`BaseDuration = 200` (design doc's number, unproblematic — a
duration tuning constant, not a lore claim). Exact `WiltPerBleedUnit`
is a content-feel number chosen in implementation and diag'd so it's
auditable, per this project's `liquid`/`tile` diag precedent.

**`SpreadFormationBuilder.BuildFlowerMeadow`** (`:265` today, places
`CharmFlowers` in a disc) swaps its placed blueprint to `FlowerField`
and threads `zone.UrquBleedLevel` through to the duration formula.

**Tests (RED first)**:
1. `FlowerField_AtZeroBleed_LivesTheFullBaseDuration` — `zone.UrquBleedLevel = 0`, placed flower's `LifespanPart.TurnsRemaining == BaseDuration`.
2. `FlowerField_AtHighBleed_WiltsEarly` — set `UrquBleedLevel` to a nonzero test value, assert a strictly shorter duration than test 1. **This is the instrument working, proven directly** — no need to wait for W7's real writer to exist.
3. `FlowerField_DurationNeverGoesNonPositive` — pathological high bleed still clamps to ≥1.
4. `FlowerMeadowFormation_PlacesFlowerFieldNotCharmFlowers` — formation-level integration, replaces the old (now-stale) assumption that `CharmFlowers` is what spawns.
5. Counter-check: `FlowerField_ResidueMatchesTheEntitysOwnLifespan` — the two numbers set at spawn are equal (guards against someone changing one formula and not the other).
6. Look-mode: `GroundLine_ShowsPetalsWhereAFlowerFieldStands` — proves the `CellStatusReadout`/`TileStateCatalog` byproduct from SM1 actually surfaces to the player (an honest "does this feature register as player-visible" check, per this session's own recent status-effects work).

Files: `Objects.json` (+1 blueprint); `SpreadFormationBuilder.cs`
(swap placement call + duration formula); `Zone.cs` (+`UrquBleedLevel`,
folded in here per SM2's note); new
`Assets/Tests/EditMode/.../FlowerFieldTests.cs`.

#### SM4 — Spread's own stamp catalog: RiverShrine, MillStead, FestivalField

Replaces `case BiomeType.Spread: return For(BiomeType.Jungle);`
(`LandmarkBuilder.cs:73`) with a real `Spread` array. This is also
the fix for the wrong-biome-stamp leak (a Ziggurat or a Rot-Choir
GroveShrine currently reachable in farmland).

**RiverShrine** (`Chance` moderate, `MinTier 1`): the `Shrine`
blueprint (already carries `SanctuaryPart` — donate → `StoneskinEffect`,
fully reused, zero new mechanics) placed beside `Reeds` (ships) and a
carved-marker prop. Examine text via SM0's now-working binding: carved
tokens, no god name, matches §1.2's canon correction.

**MillStead** ("one big stamp... a lived-in place; owners; doors" per
the design doc — canon-neutral, generic "recovered farmstead"
framing, nothing this needs to invent against canon): a small
walled footprint (reusing `Wall`, matching every other stamp's
idiom) with a door gap, one `Farmer`-tagged NPC (reuse the existing
`Farmer` role already spawned by `VillagePopulationBuilder` when a
well is present — confirm exact blueprint name at implementation
time), and a `Crate`/storage prop.

**FestivalField**: a denser `FlowerField` cluster (reusing SM3's
blueprint directly — this stamp is why FlowerField needs to exist as
scenery outside the formation roll too) plus a `Signpost` (ships).
Deliberately **no contract/quest content** per §1.2's correction —
purely atmospheric, matching "gone by morning" from canon rather than
the design doc's invented "festival contracts."

**Tests**: `SpreadCatalog_DoesNotDelegateToJungle` (the regression
pin — asserts `StampCatalog.For(Spread)` is no longer
reference-equal to `For(Jungle)`), one placement smoke test per
stamp (spawns without throwing, contains its expected legend
markers), `EveryBlueprintTheSpreadStampsNameActuallyExists` (mirrors
the existing `EveryBlueprintTheSpreadNamesActuallyExists` pattern
from the W1.2 population commit — fail-loud against real content).

Files: `LandmarkBuilder.cs` (new `Spread` array + `For` dispatch
fix); `Objects.json` if a new small prop is needed (carved-marker —
check whether an existing "Waystone"-class object covers it before
adding one); new/extended stamp tests.

#### SM5 — Spread container pool

`ContainerPlacementService`: new
```csharp
private static readonly ContainerKind[] SpreadPool =
{
    new ContainerKind("Crate", "CrateT", 4),
    new ContainerKind("Sack", "SackT", 3),
    new ContainerKind("StrongBox", "StrongBoxT", 1),
};
```
and `case BiomeType.Spread: return SpreadPool;` replacing the Jungle
fallthrough at `PoolFor:240`. **Zero new loot-table JSON** — all
three `T`-prefixed table families already exist and are generic
(confirmed §1.3).

**Test**: `Spread_WildernessContainers_DoNotUseJunglePool` — direct
pool-identity assertion, same shape as SM4's stamp regression pin.
Counter-check: village-interior containers are unaffected (zone-kind
still wins before biome — pin the existing precedence, don't
accidentally invert it).

Files: `ContainerPlacementService.cs`; one new test.

#### SM6 — Voice card: recovered-world folk (Sill)

**Lore content, not code.** `Lore/Voices/VOICE-CARDS.md` gains an
11th card, templated exactly on the other 10 (register / syntax
habits / lexicon / taboos / what it must never sound like / sample
lines), derived from the one existing specimen
(`Lore/Codex/12_SariStory_Sill.md`) rather than invented fresh.
Register observations from the specimen: warm, oral, addresses a
child directly, deflects the cosmic question back to "that's the
question, isn't it," refuses to resolve Naro's reason even in a
bedtime story (mirrors Mystery Ledger §1's build gate — the
grandmother doesn't know either), plain concrete nouns (geese,
axe, river), the closing move of denying its own truth ("none of
it's true") while still enforcing the rule (come inside).

This directly satisfies the corpus's own process
(`Lore/Voices/VOICE-CARDS.md:3-11`: "every in-world line... is
written from a card") for every future Sill scene, not just SM7's
one line — closing a real, cheap, high-leverage gap the lore sweep
found rather than writing SM7's line against nothing.

**Not run through the full blind-reviewer gate** (`README.md`'s
2026-07-14/15 process) — that's a heavier process this plan doesn't
have a fresh-context reviewer on hand for. Noted honestly as a debt:
the card and SM7's line are self-graded, not gate-passed. Flag for a
future gate run alongside other codex additions.

Files: `Lore/Voices/VOICE-CARDS.md` (+1 card).

#### SM7 — The Sill inciting storylet

**New predicate** `IfPlayerInZone` (generically reusable — every
future place-specific storylet wants this, not a one-off):
```csharp
Register("IfPlayerInZone", (speaker, listener, arg) =>
    SettlementRuntime.ActiveZone != null
    && SettlementRuntime.ActiveZone.ID == arg);
```

**New storylet** `Assets/Resources/Content/Data/Storylets/SillHearsIt.json`:
```json
{
  "Storylets": [
    {
      "ID": "SillHearsIt",
      "OneShot": true, "Tracked": false,
      "Triggers": [ { "Key": "IfPlayerInZone", "Value": "Overworld.10.10.0" } ],
      "Effects": [
        { "Key": "AddMessage", "Value": "<line, written against SM6's card>" },
        { "Key": "SetFact", "Value": "sill_sari_heard:1" }
      ]
    }
  ]
}
```
Message line drafted against SM6's card, present-tense, concrete,
no design-register aphorism (voice-gate self-check per
`VOICE-CARDS.md:3-11`) — working draft: *"Old [elder's placed name]
stops mid-sentence — something in the water said her grandmother's
word for it, sari, and doesn't say it again."* Finalized against the
actual elder-NPC naming convention at implementation time (confirm
whether placed elders get individual names or are generic "Elder" —
if generic, the line drops the name and reads "the elder stops
mid-sentence").

**Recension scribe reacts** — `Lore/Factions/02_Recension.md:270`
names this exact beat ("the first NPC who can explain the *sari...
sari...*"). One new choice + node added to the existing
`Palimpsest.json` (`PalimpsestEcho_1`), gated `IfFact:
sill_sari_heard:>=:1` (exact syntax confirmed against
`ConversationPredicates.cs:242-253` and the shipped `HiddenShrine`
precedent), Recension-voiced per its card
(`VOICE-CARDS.md:36-53`: "attribution before assertion," "the unsaid
marked aloud") — the scribe explains what little the Recension has
collated, explicitly not confirming Naro's reading (Mystery Ledger
§1 gate, unconditional).

**Tests (RED first)**:
1. `IfPlayerInZone_TrueWhenActiveZoneMatches` / `_FalseOtherwise` — predicate unit tests, standard shape.
2. `SillHearsIt_FiresOnceThePlayerIsInSill` — `SettlementRuntime.ActiveZone` set to Sill's zone, `OnTickEnd` called, assert `MessageLog` gained the line and the fact is set.
3. Counter-check: `SillHearsIt_DoesNotFireInAnyOtherZone` — identical setup, different zone id, no message, no fact.
4. `SillHearsIt_IsOneShot_NeverFiresTwice` — call `OnTickEnd` twice with the trigger still true, assert only one message.
5. `ScribeConversation_GainsTheSariTopic_OnlyAfterTheFactIsSet` — dialogue-branch gate pin, with the counter-check (before the fact, the choice is absent/unreachable).

**Adversarial** (cross-system: storylet↔dialogue↔fact, per CLAUDE.md's
taxonomy — this touches enough surfaces to warrant it): fact already
set from a prior session (save/load reach — the `OneShot` guard uses
`_firedStorylets`, a *separate* set from the fact store; verify they
can't desync, e.g. a save that has the fact but not the fired-flag
still shouldn't re-fire the message — pin this explicitly since it's
exactly the kind of two-sources-of-truth bug this project's
adversarial sweeps keep finding); player never visits Sill (storylet
simply never fires — no error, no orphaned quest state, since this
isn't a quest); `SettlementRuntime.ActiveZone` null at the moment
`OnTickEnd` runs (pre-bootstrap/test context — predicate must return
false, not throw).

Files: `ConversationPredicates.cs` (+1 predicate);
`Storylets/SillHearsIt.json` (new); `Conversations/Palimpsest.json`
(+1 choice/node); tests across
`ConversationPredicatesTests`/`StoryletPartTests`/a new
`SillHearsItTests.cs`.

#### SM8 — Final adversarial sweep + cold-eye + doc close-out

Per CLAUDE.md's mandatory post-implementation pass: Angle A (bug-class
taxonomy — already threaded through SM0-SM7 above) + Angle B
(Qud-parity — not applicable, this is CoO-original content, no Qud
source to diff against; note this explicitly rather than skip the
question) + the hypothesis-driven pass if the cold-eye finds nothing
🟡+. Update this doc's §2.3 implementation log. One commit per SM
through SM7; SM8 is the closing commit with the full self-review.

### 2.3 Implementation log

**Status: W1 remainder ✅ shipped 2026-08-18** — seven commits SM0–SM7
(`31b2247d` … `65ccb2e9`) plus the SM8 cold-eye commit. Tests 6702 →
6743. Full suite green at each step.

| SM | Commit | Tests | Notes |
|---|---|---|---|
| SM0 examine binding | `31b2247d` | +3 | 40 shipped blueprints' text un-broke |
| SM1+SM2 residue + bleed field | `978fd045` | +5 | folded per R1 |
| SM3 FlowerField + instrument | `36cbf465` | +6 | first cut; revised in SM8 (below) |
| SM4 Spread stamps | `f4c7e0f5` | +5 | RiverShrine / MillStead / FestivalField |
| SM5 container pool | `4ced558e` | +2 | |
| SM6 voice card | `060429a9` | — | lore only |
| SM7 inciting storylet | `65ccb2e9` | +9 | scope correction: Scribe_1, not PalimpsestEcho_1 |
| SM8 cold-eye | (this commit) | +10 | four findings, below |

#### SM7 — a plan premise corrected before content was written

The plan assumed the reactive dialogue belonged on `Palimpsest.json`'s
`PalimpsestEcho_1` ("the itinerant Recension scribe"). Verified false
at implementation: that conversation belongs to a separate
`PalimpsestEcho` blueprint that `VillagePopulationBuilder` never
places. The NPC actually reachable at Sill is the generic village
`Scribe` (Faction Villagers), conversation `Scribe_1` in
`FriendlyNPCs.json`. The topic was written there, in a plainer
village-copyist register — canon's beat survives, the plan's guess at
the NPC did not.

#### SM8 — the cold-eye pass, and what it caught

Run as CLAUDE.md prescribes: all seven diffs read together, then the
hypothesis-driven pass ("what does the player/engine do that the
per-SM tests don't simulate?"). Four findings, all fixed in one commit,
each with a RED test first:

**🟡 F1 — ghost petals.** `TileStateSourcePart.Seed` re-asserts every
player turn and `ZoneTileState.WriteLayer` keeps the LONGER lease on
refresh. SM3 set the petal residue's lease to the flower's whole
duration, so the residue was pinned at full for the flower's life and
then decayed for another full duration *after the flowers were gone* —
200 turns of "petals" on bare ground at bleed 0. Every other source
uses a 4-turn lease for exactly this reason. Fixed: `ResidueTurns: 4`;
the builder no longer touches it. Pinned by
`PetalsFadeShortlyAfterTheFlowersAreGone`. The old test
`ResidueDurationMatchesTheEntitysOwnLifespan` pinned the wrong
invariant and was removed.

**🟡 F2 — two placement paths, one formula.** The wilt math lived in
`SpreadFormationBuilder.FlowerMeadow`; the FestivalField STAMP places
the same blueprint through the generic `LandmarkBuilder`, which knows
nothing about bleed — so stamp flowers never wilted. One object, two
behaviours, keyed on which builder happened to place it. Fixed by
moving the formula onto the entity: new `FlowerCharmPart` (roots on
its first `EndTurn`, which is the first event that carries a Zone;
`Rooted` is a public field so it survives save/load and does not
re-hand a saved flower a fresh season). Numbers became content
(`BaseDuration/WiltPerBleedUnit/MinDuration` in `Objects.json`).
`ComputeDuration` compares in float before the int cast — a
pathological bleed would otherwise overflow the cast into a wrapped
bonus season — and reads negative/NaN bleed as 0. Pinned by
`AStampPlacedFlower_WiltsExactlyLikeAFormationFlower`,
`RootsOnce_ASecondTurnDoesNotResetTheSeason`,
`ComputeDuration_NeverGoesBelowTheFloor_AndNeverOverflows`,
`ComputeDuration_NegativeOrNaNBleed_ReadsAsZero`.

**🟡 F3 — a "mirror" that didn't persist.** `Zone.UrquBleedLevel`'s
docstring said it mirrors `AmbientLevel` exactly; `AmbientLevel` is
written by `SaveZone` (v5), the new field was not. Zero effect today
(always 0) — a data-loss trap the day W7 writes it and trusts the
docstring. Fixed: persisted beside `AmbientLevel`, **save FormatVersion
5 → 6** (strict-equality reader; existing saves are rejected — the
same pre-1.0 acceptance recorded for v4→v5, restated in the ledger).
Pinned by `Gap_Zone_UrquBleedLevel_RoundTrips` + the unset-default
counter-check; the version tripwire test bumped deliberately.

**🟡 F4 — a dispatch path with no diag.** `StoryletPart.OnTickEnd`
pass 2A (non-quest storylets) emitted nothing — never noticed because
no non-quest storylet had ever fired. `event/StoryletFired` added,
pinned with a fire/no-fire pair.

**🔵 F5 — every scribe, everywhere.** `Scribe_1` is the conversation
of every village's scribe; the sari topic was offered in Gantry too.
Gated additionally on `IfPlayerInZone: Sill` — canon names Sill's
scribe specifically (`02_Recension.md:270`). Pinned by
`ScribeConversation_SariTopic_IsSillsScribeOnly`.

Not found (checked, clean): `Description` alias vs the save reflection
(fields only — invisible to save ✓); FlowerField actually receives
`EndTurn` (the MaterialSim passive tick includes every LifespanPart
bearer ✓); `SettlementRuntime.ActiveZone` is written on both zone-change
paths (bootstrap + InputHandler transition ✓); Reeds are walkable, so
the RiverShrine's centre is reachable ✓; village buildings use the same
`Wall` blueprint MillStead does ✓; `Farmer` carries Conversation +
Trader ✓; new Part types are reflection-discovered with a `Part`-suffix
fallback ✓.

Honesty bounds: unit-verified + one earlier live probe (SillHearsIt
fired in Play mode via the exact `TryMoveEx` path); no screenshot of
any new stamp — whether RiverShrine/MillStead/FestivalField *read* on
an 80×25 grid is unverified, same bound the W1.1 formations carried
until their ASCII-dump pass. The Sill voice card + storylet line are
self-graded, not gate-reviewed (R4).

---

## 3. W1 test strategy

Each SM ships RED→GREEN→counter-check inline (per-SM tables above).
SM7 gets a dedicated adversarial pass (cross-system surface count ≥2:
save/load reach via the fired-flag/fact dual-store, cross-actor via
the scribe dialogue gate). SM3's bleed-wilt instrument is proven
directly via `Zone.UrquBleedLevel` manipulation in tests — it does
not need to wait for a real writer to exist to be verified correct.

## 4. W1 performance

Touches: `ZoneTileStateSystem.SeedTerrainSources` (already per-player-turn,
SM1 adds one more `if` inside an existing loop — no new pass);
`StoryletPart.OnTickEnd` (already runs every tick; SM7 adds one
storylet to an existing linear scan over `StoryletRegistry.GetAll()`,
and it deregisters itself from further consideration once
one-shot-fired — bounded, no growth). No new `Update`/`LateUpdate`,
no new per-frame allocation. `Zone.UrquBleedLevel` is a bare float
field, same cost class as the shipped `AmbientLevel`.

## 5. W1 observability

*(As shipped — see §2.3 for how this changed from the plan.)*

- `event/CharmWilted` — emitted by `FlowerCharmPart` when a flower
  roots into a zone whose bleed actually shortened its season
  (`finalDuration < baseDuration`), payload `{zoneID, bleedLevel,
  baseDuration, wiltPerUnit, finalDuration}`. Deliberately silent at
  bleed 0: eighty flowers saying "nothing wilted" eighty times is
  noise, not observability. The plan's `worldgen/FlowerFieldWilted`
  (one record per formation build) went away with the builder-side
  formula.
- `event/StoryletFired` — emitted by `StoryletPart.OnTickEnd` pass 2A
  for every non-quest storylet that fires, payload `{storyletId,
  oneShot}`. **Did not exist before this phase** — pass 2A had zero
  diag, invisible until SillHearsIt became the first content to ever
  reach it (quest passes 2B/2C already diag'd). Cold-eye finding.

## 6. Critical review of the W1-remainder plan (before implementation)

**R1 — Is folding SM2 into SM3 the right call, or does it hide a
commit?** Kept as stated: a bare field with zero consumers has no
testable production behavior of its own (CLAUDE.md's self-audit
checklist asks exactly this question), and SM3 is the very next
commit — there's no intervening state where `UrquBleedLevel` sits
unused and undocumented. If SM3 turns out larger than expected during
implementation, split it back out; noted as a live call, not a firm
rule.

**R2 — SM0 (the Examinable fix) touches 40 existing blueprints'
player-visible text as a side effect of a W1 commit.** This is a real
scope question: is silently fixing 40 unrelated objects appropriate
inside a "finish W1" plan? Decision: **yes, with an explicit SCOPE
DIVERGENCE section in that commit** (CLAUDE.md's own mechanism for
exactly this situation) rather than either hiding it or blocking W1
on a separate pre-pass. The fix is one property, it's a pure bugfix
(strictly makes broken text correct, changes no other behavior), and
every single stamp SM3/SM4 adds needs it to work — deferring it would
mean shipping new content with the exact same silent-binding bug.

**R3 — Does SM4's "Farmer NPC" reuse actually exist as a placeable
blueprint, or was that assumed?** Flagged honestly: the code sweep
confirmed `VillagePopulationBuilder` places a `Farmer`-role NPC when a
well exists (`VillagePopulationBuilder.cs:139-140` per the explorer's
report) but did **not** confirm the exact blueprint name used there.
**Action: confirm the exact name at SM4 implementation time before
writing the stamp's legend** — if it turns out to be an inline
`Villager`-tag reskin rather than a standalone blueprint, MillStead's
NPC becomes a `Villager` instead, which is a one-line legend change,
not a plan revision.

**R4 — SM6's voice card is unreviewed by the corpus's own gate.**
Accepted risk, stated plainly in SM6 itself rather than glossed over.
The alternative (blocking SM7 until a fresh-context review can run)
isn't available in this session; the mitigation is that the card is
derived tightly from an existing, already-shipped specimen text
rather than invented from nothing, which is the lowest-risk way to
write ungated voice content.

**R5 — Does "the elder stops mid-sentence" require the placed elder
to have an individual name, and does the game currently name
elders individually?** Left as an explicit open question resolved at
SM7 implementation time (noted inline in SM7 rather than guessed
here) — this is exactly the kind of thing a pre-impl verification
sweep step should catch by reading the actual `VillagePopulationBuilder`
NPC-naming code before writing the JSON, not something to decide from
a planning doc.

**Net revisions from this review:** none block implementation; R1-R5
are tracked decisions/open-questions carried into their respective
SMs rather than blocking questions back to the user, per this
session's standing instruction to proceed on reasonable defaults and
flag divergences rather than pausing.

---

## 7. W2 — The Beating & Tent-Right (detailed, ready to implement)

Per `Docs/FELLING-IMPLEMENTATION-PLAN.md`'s own convention ("W1-W8
milestone map (detailed at phase start)"), W2 gets a full
sub-milestone breakdown — mirroring §2 above, with its own
verification sweep, content-readiness table, and critical review —
**once W1 is committed**, so the plan's "what does the codebase look
like right now" premise doesn't go stale mid-write. This section
captures what the two sweeps already established, so that pass starts
from verified ground rather than re-discovering it.

### 7.1 What W2 ships (design doc §3.3, cross-checked against canon)

Formations (salt pan, ruin field, dune belt, caravan road, wind
barrens, brine lens — `DesertBuilder`/`RuinsBuilder` reuse, same
`FormationSelector` pattern SM-verified in W1.1); `Parched` status +
Height-band glare (genuinely new — no heat-tick-on-occupant path
exists, confirmed §1.3; no head-slot-cover query helper exists,
confirmed — `Body.GetPartByType("Head")` + `ForeachEquippedObject`
are the primitives to build it from); **Tent-Right camps + the oath**
(the largest single piece — no sanctuary/guest/oath AI concept exists
anywhere today, confirmed via full grep of `Assets/Scripts/Gameplay/AI/`;
closest primitive is `NoFightGoal`, a temporary pacifism stack,
documented at `NoFightGoal.cs:12` as intended for exactly this kind
of truce/ceasefire state); Wellmeet + the First Tent as places that
actually differ from a generic village (today all 16 places are
identical — this is a bigger gap than the design doc implied, since
`Place.Faction` isn't even read by the village pipeline); salt
economy (both halves of the substrate exist and are both dead:
`PaleSaltVein` only spawns underground, never on the surface;
`WantsMineralPart` has zero blueprint consumers); the tenth fire
(cheap once SM0's examine-text fix ships — an untended `Campfire`
with deliberately empty `Text` already reads correctly, since empty
`Text` is the part's own default); the Last Counter + two abandoned
predecessors (a Village POI at (18,18) today — becomes a
`Caravanserai`-flavored place per §12 of the code sweep, or a
dedicated stamp if the flavor needs to diverge further; the "two
abandoned predecessors" need new unnamed ruin markers, likely small
`RuinsBuilder`-flavored dressing rather than named POIs).

### 7.2 Load-bearing canon (verbatim, verified — for the detailed pass to cite directly)

- **The oath's exact terms**: `Lore/Codex/04_TentRightOath.md` in
  full — this is the actual spoken text to render in-game, not a
  paraphrase.
- **What claiming it grants**: "a *mechanical sanctuary* — from
  Catchers (who cannot follow), from pursuit (which must wait
  outside), from Urqu-bleed (weakest here)... it is *free*."
  `Lore/Factions/07_TentRight.md:141`.
- **Worst crime ordering**: breaking hospitality is "**the supreme
  crime. The only unforgivable one**" (`:104`); well-poisoning is
  explicitly "**the second-worst**" (`:107`) — confirms the design
  doc's ordering was right, now cited to the primary source.
  Oathbreaking costs "**the single worst reputation loss in the
  game**" (`:152`).
- **Salt chain**: mine (Tent-Right) → buy (Pale Curation) → move
  (the Concord, under guest-rights, "in the Beating, Tent-Right's
  claim wins") — `Lore/Factions/04_SaccharineConcord.md:100,112`.
- **The tenth fire**, exact Mystery Ledger wording:
  `Lore/MYSTERY-LEDGER.md:57-69` — "Forbidden from answering:
  everything and everyone." No examine text beyond what is seen; SM0's
  fix means an empty `Text` already produces exactly this.
- **The Last Counter**: "We cannot guarantee delivery beyond this
  point"; "pulled back twice in two generations."
  `Lore/Factions/04_SaccharineConcord.md:97`.
- **Voice**: Tent-Right's full card exists and is unusually strong
  (`VOICE-CARDS.md:138-156` — "hospitality precedes identity,"
  "clean, complete refusals," never "generic desert-nomad stock
  phrases... their courtesy is jurisprudence"). W2 dialogue has a
  real card to write against, unlike W1's Sill gap.

### 7.3 W2 verification sweep — corrections + new facts (all re-read 2026-08-19)

| Claim / question | Finding | Cite |
|---|---|---|
| "Beating routes to DesertTier3 at every tier" (earlier explorer summary) | **WRONG — corrected.** Aliasing is per-tier: T1→DesertTier1, T2→DesertTier2, T3→DesertTier3; LairGuards→Desert. GlassScorpion/BrittleHound/DuneLurker live only in DesertTier3. | `PopulationTable.cs:71,88,103,589` |
| Formation substrate extension point | `FormationSelector.PoolFor` returns null for every non-Spread biome ("W2+ fill these in"); enum is append-only; selection hashes zone id, so appending Beating members cannot re-roll the Spread. | `Formation.cs:73-80,17` |
| Drinking mechanic for Parched's cure | **Only `TonicPart` has a Drink action.** No drink-from-well/tile exists. `Well` blueprint is inert (Render+Physics only). | `TonicPart.cs:42`; blueprint scan |
| Apply-effect from dialogue | **No such conversation action.** `CureEffect` exists; `RestAtInn` is the precedent for a dedicated verb-action. The oath claim will be a dedicated action, not a generic apply-effect. | `ConversationActions.cs:710,676` |
| Tent shade via `Cell.IsInterior` | **Stamps never mark IsInterior** (zero hits in LandmarkBuilder). Only VillageBuilder floors and underground zones set it. Tents need a new stamp capability. | grep LandmarkBuilder.cs |
| The Great Salt Plain "≡" cells | **Not encoded in the map** — rows 16-18 cols 12-14 are plain `B`; the design doc's `≡` was notation. | `WorldMapAuthoring.cs` BiomeRows |
| `MineralTradeService.TryTrade` callers | **Zero production callers** (docstring only + one dev scenario). W2.7 is its first real wiring. | grep |
| Effect duration ticking | `Effect.Duration` decrements at `Effect.cs:180` (owner's turn tick). 3 days = 3 × `WorldClock.DayLengthTicks` (1200) = **3600 turns**. | `Effect.cs:180`; `WorldClock.cs:47` |
| Head-cover query | `Body.GetPartByType("Head")` → `BodyPart.Equipped`. No helper exists; W2.3 writes `HasHeadCover(entity)`. | `Body.cs:96`; `BodyPart.cs:213` |
| Canon fauna blueprints | SunStriker / Wardline / SariSnake / SkySari / Saltbriar: **none exist.** GlassScorpion / BrittleHound / DuneLurker / Scorpion ship. | blueprint scan |
| `FormationApplied` diag | Hardcodes `biome = nameof(BiomeType.Spread)` — the Beating builder must pass its own biome name (tiny generalization, W2.1). | `SpreadFormationBuilder.cs:53-58` |

### 7.4 Resolved design decisions (were §7.3's open questions)

**D1 — The oath ships as a real mechanic, first cut.** Not the narrow
"status + dialogue verb only" fallback: the hostility veto is the
faction's entire identity ("the delta between outside and inside IS the
faction") and the seam exists — `FactionManager.GetFeeling`'s override
precedence chain (personal hostility → party → reputation → table).
V1 = `UnderTheClothEffect` (Duration 3600) + a veto clause in
`GetFeeling` + claim/extend via dedicated conversation actions +
oathbreak (guest attacks anyone while under the cloth → effect removed,
largest single rep loss in the game, canon `07_TentRight.md:152`).
**Deferred, each a documented divergence:** pursuit-waits-at-the-boundary
geometry (needs zone-boundary AI that doesn't exist); Catcher-specific
vetoes (no Catcher NPCs until W5); Urqu-bleed suppression under cloth
(no bleed writer until W7). None of these deferrals is silent scope-loss:
the design doc's §7.4 scene ("your pursuers camped at the boundary,
waiting politely") moves to the W5+ backlog explicitly.

**D2 — Glare is player-only in v1.** The design doc says "creatures
without head-slot cover take heat," but Tent-Right NPCs live outdoors in
the Beating — a literal reading permanently Parches every native. The
honest v1: the glare writer targets the player only; natives are adapted
(that's why they live there). Documented divergence; revisit if NPC
exposure ever matters mechanically.

**D3 — Sari-Snake / Sky-Sari stay out of W2.** They are the bestiary's
indicator species — "only when Urqu is active" IS their design. Shipping
them as static spawns would falsify that design, which is worse than
their absence. They land with state-reactive spawning (§7.6 of the
design doc, W7-adjacent). Wardline (suppresses Sari-Snake spawns) is
meaningless without them and waits too. W2's bestiary is the static
roster: SunStriker (new), GlassScorpion, BrittleHound, DuneLurker,
Scorpion, Saltbriar (new forage), plus human trouble at T3.

**D4 — The Great Salt Plain is not a W2 place.** Salt-pan formations
carry `PaleSaltVein` outcrops across the Beating (supply everywhere the
pans are), and Wellmeet's salt-master is the demand. The *named* Plain
as a differentiated place joins a later place-pass; nothing in the salt
loop depends on it.

**D5 — Places differentiate via a minimal faction profile, not bespoke
builders.** `CreateVillagePipeline` starts reading `Place.Faction`
(today it reads nothing but Name/Tier): TentRight → tent stamps +
Tent-Right roster (clan-elder, well-keeper, salt-master hosts);
SaccharineConcord at the Last Counter → Caravanserai profile + Factor +
the disclaimer dialogue. This is deliberately the smallest seam that
makes Wellmeet ≠ Sill — the richer per-place system stays future work.

### 7.5 Content-readiness

| Sub-feature | Readiness | Why |
|---|---|---|
| Beating formations | 🟢 | Formation substrate proven by W1.1; DesertBuilder base stays; six self-contained formation routines |
| Beating population tables | 🟢 | pattern proven by W1.2; two small new blueprints (SunStriker, Saltbriar) |
| Parched + glare | 🟡 | new effect (pattern: HobbledEffect) + WorldClock's FIRST consumer + new Well drink action — three small firsts in one SM |
| Tent stamps + interior marking | 🟡 | needs the new `MarksInterior` stamp capability (small, but touches LandmarkBuilder.Apply) |
| The oath | 🟡 | GetFeeling veto is on the AI hot path (perf care); oathbreak hook needs a verify-first read of the attack event |
| Place profiles | 🟡 | first time `Place.Faction` is consumed; touches the village pipeline |
| Salt economy | 🟢 | all pieces exist unwired (veins, WantsMineralPart, MineralTradeService, rep) |
| Tenth fire / Last Counter dressing | 🟢 | trivial once profiles + formations exist |

### 7.6 Sub-milestones (smallest blast radius first)

#### W2.1 — Six Beating formations + the look pass

`Formation` enum appends `SaltPan, RuinField, DuneBelt, CaravanRoad,
WindBarrens, BrineLens` (append-only, after RiverMeadow). New
`BeatingFormationBuilder` (priority 2500, mirror of Spread's), pool
weighted pans/barrens common, brine lens rare. Formations are
self-contained routines placing existing blueprints — `PaleSaltVein`
(2-4 per salt pan: the supply half of W2.7), `SandstoneWall` segments
with gaps (ruin field: the exposed street grid, waist-high), `RoadStone`
lane + `Signpost` waymarkers + bone props (caravan road), `Sand`/`Rock`
banding (dune belt), `DryBrush`+`Rock` scatter (wind barrens),
`BrinePool` patches (brine lens). `FormationApplied` diag generalized to
carry the builder's own biome. **Gate: the W1.1b ASCII-dump look pass
runs BEFORE the commit** — three of four Spread formations were wrong in
ways only a picture showed; that lesson is now a step, not a memory.
Tests mirror `SpreadFormationTests`: stable selection, every-formation-
reachable, signature-per-formation + counter-check, crossability
flood-fill on ruin field (walls must never seal), veins-in-pans.

#### W2.2 — The Beating's own bestiary

`BeatingTier1/2/3` in `PopulationTable` + the three switch arms
(replacing the Desert aliasing), + `LairGuards(Beating)`. New
blueprints: `SunStriker` (bestiary-canon: fast territorial melee, no
venom, village-edge basker) and `Saltbriar` (forage bush, Harvestable →
SaltbriarSprig ingredient — the ingredient name already exists in
`Docs/Design/WORLD-INGREDIENTS.md`). Roster per tier: T1 sun-strikers/
scorpions/saltbriar + sparse human trouble; T2 GlassScorpion +
BrittleHound arrive; T3 DuneLurker + organized trouble. Tests mirror
`SpreadPopulationTests`: settled-vs-hostile weighting invariants,
`EveryBlueprintTheBeatingNamesActuallyExists`, fallback counter-check.
D3's deferral note goes in the table's docstring so the missing
indicator species read as deliberate.

#### W2.3 — Parched + Height-band glare + drinking

`ParchedEffect`: stacking (OnStack increments a `Stacks` field, max 3),
−1 Str/Agi per stack (StatShift pattern from HobbledEffect), no
duration (persists until cured — Duration 0 sentinel, verify the
no-duration idiom against HearthAura/Rooted at implementation).
Cure vectors: (a) standing on a water-coated tile clears all stacks
(checked in the effect's own turn tick — one TileState read),
(b) drinking at a Well — new `WellPart` adding a "Draw water" action
(SanctuaryPart's action idiom) that clears Parched + messages,
(c) any TonicPart drink clears one stack (one-line hook in ApplyTonic).
The glare writer: `BeatingGlareSystem.OnPlayerTurnEnd` (called beside
`WorldClock.NotifyPlayerTurnEnd`, InputHandler.cs:925): if
`WorldClock.GetBand == Height` && zone biome == Beating && player cell
`!IsInterior` && `!HasHeadCover(player)` (new helper:
`Body.GetPartByType("Head")?.Equipped != null`) → accumulate an
exposure counter; every 10th consecutive exposed turn applies/stacks
Parched. Counter resets on shade/night/cover. Diag: `effect` category
carries the apply via the normal pipeline; add `glare/Exposed` tick
record gated on channel. Tests: band gating (Dawn no, Height yes),
interior exemption, head-cover exemption (counter-check: bare head
stacks), stack cap, each cure vector + its counter-check, WorldClock's
first consumer pinned against band boundaries (tick 299/300).

#### W2.4 — Tent stamps + interior shade + the guest-cloth pole

`StructureStamp` gains `MarksInterior` (bool): `Apply()` sets
`Cell.IsInterior = true` on floor cells enclosed by the stamp footprint
(the tent's inside — which is what makes W2.3's shade exemption
*architecture*, per the design doc). New blueprints: `TentWall`
(cloth wall, Solid, flammable-low), `GuestClothPole` (the sanctuary
marker: Render `|` + Examinable text passing the Tent-Right voice card
— "the ones following you know what the cloth means"). New
`TentRightCamp` stamp (Beating catalog, replacing the Desert
delegation for the camp slot): tents (MarksInterior) + `Well` +
`GuestClothPole` + Tent-Right host NPC (new `TentRightHost` blueprint,
Faction TentRight, Conversation from W2.5). Beating gets its own
`StampCatalog` array (Desert delegation ends): TentRightCamp,
ConcordWaystation (reused), SandstoneTomb (reused), the tenth-fire-free
ambient set. Tests: stamp placement smoke, interior cells marked +
counter-check (outside the tent stays exterior → glare applies outside,
not inside — the biome's whole thesis in one test), catalog content
gate extended to Beating.

#### W2.5 — The oath, first cut

`UnderTheClothEffect` (Duration = `3 * WorldClock.DayLengthTicks`,
display "under the cloth", TYPE_GENERAL positive). Claim: dedicated
conversation action `ClaimGuestRight` on the TentRightHost conversation
("There is water. There is shade. Sit." — the codex text
`Lore/Codex/04_TentRightOath.md` is the spoken source; host lines
written from the Tent-Right voice card, which is strong). Veto: in
`FactionManager.GetFeeling`, after the personal-hostility check and
before the reputation read — if the candidate target `HasEffect
<UnderTheClothEffect>()` and the asker's faction is not Beasts/Snapjaws
(the oath binds people, not animals — canon: the covenant is between
people), return non-hostile floor (0). Cost: one short effect-list scan
only on paths that would otherwise go hostile — measured acceptable
(turn-based AI scans, ~30 NPCs). Oathbreak: hook the player-attack
seam — verify-first item: read `CombatSystem.PerformMeleeAttack` +
spell damage attribution to find the one choke point where
`attacker == player && player.HasEffect<UnderTheClothEffect>()` can
trigger `OathbreakService.Break(player)`: remove effect,
`PlayerReputation.Modify(TentRight, -100)` (largest single loss in the
game, tuned vs existing rep deltas at implementation), message in the
Tent-Right register, diag `oath/Broken`. Diag: `oath/Claimed`,
`oath/Expired` (effect OnRemove distinguishes cause), `oath/Broken`.
Tests: claim applies + duration exact, hostile NPC's GetFeeling returns
non-hostile while cloth holds + counter-check (expired → hostile
again), animals unaffected by the veto, oathbreak removes + rep hammer
+ counter-check (attacking AFTER expiry is not oathbreak), claim twice
= refresh not stack, save round-trip of the effect mid-oath.
Adversarial (2+ taxonomy surfaces — cross-actor, anti-exploit,
save/load): claim then immediately attack the host; claim in one camp
and walk to another zone (effect travels — the oath binds the person,
not the ground — pin as designed); rep already rock-bottom.

#### W2.6 — Place profiles: Wellmeet, the First Tent, the Last Counter

`CreateVillagePipeline` reads `Place.Faction` (first consumer):
TentRight → tent-flavored village (TentWall buildings via a tent
village variant flag on VillageBuilder OR post-pass swap — decide by
reading VillageBuilder's wall handling at implementation; smallest
diff wins), Tent-Right roster replacing the generic one (clan-elder,
well-keeper who is a WellPart host, salt-master carrying
`WantsMineralPart`), oath host at the well. First Tent additionally
places a monument stamp (the pilgrimage site: poles + cloth + NO god
imagery — canon: "not a temple; there is no god — a monument to a
choice"). Last Counter (Concord): Caravanserai-profile + Factor +
disclaimer dialogue line ("We cannot guarantee delivery beyond this
point." — verbatim canon `04_SaccharineConcord.md:97`); its two
abandoned predecessors are dressing in the two adjacent zones toward
the corner (a ruined-waystation stamp in the ambient catalog gated to
those world cells — verify the gating seam at implementation; if
per-cell stamp gating is awkward, ship the ruined stamp as a rare
ambient in deep-Beating and place the two authored ones by zone id).
Tests: faction-profile dispatch (TentRight village contains tents +
salt-master; generic village unchanged — counter-check), First Tent
monument present, Last Counter has Factor + Caravanserai.

#### W2.7 — Salt economy live

Supply shipped in W2.1 (veins in pans). Demand: the salt-master's
`WantsMineralPart { Minerals: "PaleSalt", Faction: "TentRight",
RepReward }` + new conversation action `SellMineral`
("SellMineral:PaleSalt") calling `MineralTradeService.TryTrade` — its
first production caller; the service already diags `mineral-trade`.
Concord Factors buy salt through the ordinary trade screen (already
works via Commerce.Value — pin with a test, no code expected). Tests:
sell → item consumed + rep moves + diag; sell without salt → refusal
path; the WantsMineralPart blueprint gate
(`EveryBlueprintCarryingWantsMineral...` content test) so the part
never goes consumer-less again.

#### W2.8 — The tenth fire + W2 close-out

`UntendedFire` blueprint: Render `*` warm, `LightSource`, `Thermal`
warm, **no Fuel part** (a Campfire's Fuel exhausts — this one must
never), Examinable Text EMPTY (prints exactly "You see a fire." —
Mystery Ledger §4: no answer, from anything, ever; the blueprint's
display name is just "fire"). Placed by zone id in one authored
deep-pan wilderness cell (constant in WorldMapAuthoring, chosen off-road
far corner; NOT a POI — it must never appear on the map UI, which
non-POI placement satisfies by construction). No quest, no fact, no
diag beyond placement. Then the full W2 close-out: cold-eye over all
W2 diffs together, hypothesis pass, adversarial sweep for the oath
(the one W2 feature with 2+ taxonomy surfaces), doc log, live ASCII
look at every formation + the camp, PlayMode sanity sweep (enter the
Beating, stand in noon glare bareheaded, claim the oath at a tent,
sell salt).

### 7.7 W2 performance (required — touches per-turn paths)

| Risk | Mitigation |
|---|---|
| `GetFeeling` oath veto on the AI hot path | Effect-list scan ONLY on the branch that would return hostile; list is short (≤5 effects typical); no allocation. Measured against the existing per-turn AI scan budget at implementation (ProfilerRecorder if in doubt). |
| Glare check per player turn | One call beside the existing WorldClock notify: band check (int math) + biome check + one cell read + head-slot read. Early-outs ordered cheapest-first. Player-only (D2). |
| Parched cure check per turn | Inside the effect's own tick — one TileState dictionary read. Only exists while Parched. |
| Formations | Generation-time only. |
| New stamps' interior marking | Generation-time only. |

### 7.8 W2 observability

- `oath/Claimed | Expired | Broken` — the faction's whole mechanic, queryable.
- `effect/GlareExposure` on each stack APPLICATION (bearer in target,
  per the effect-category convention) — not per-tick; at bleed-0
  cadence a per-tick record would be noise. Parched itself rides the
  standard effect pipeline. *(As shipped — the plan's earlier
  `glare/Exposed` per-tick wording superseded.)*
- `mineral-trade/*` already emitted by MineralTradeService — first real records.
- `worldgen/FormationApplied` now carries the true biome.
- Every gate that can reject (claim while already under cloth, sell without salt) emits its reason.

### 7.9 Critical review of the W2 plan (before implementation)

**R1 — Is the GetFeeling veto the right seam for the oath?** The
alternatives: (a) NoFightGoal pushed onto every hostile in the zone —
rejected: it suppresses self-preservation too (documented caveat at
NoFightGoal.cs:23-31) and has to chase spawns; (b) a Passive flip —
rejected: mutates NPC state that must be un-mutated on expiry, a
save/load trap; (c) the feeling chain — chosen: stateless, expires with
the effect automatically, already the documented precedence seam. The
cost is the hot-path scan, bounded per §7.7.

**R2 — Does the oath protect the player from BEASTS?** No (D5 note in
W2.5): canon frames the oath as a covenant between people; a scorpion
cannot be an oathbreaker. Pinned by a test either way so the behavior
is chosen, not accidental.

**R3 — Effect.Duration = 3600 assumes player effects tick once per
player turn.** Verify-first at W2.5: read Effect.cs:180's caller and
confirm the tick cadence for player-owned effects; if effects tick on
world ticks instead, the constant changes, not the design.

**R4 — The oathbreak hook is the riskiest unknown.** There may be no
single choke point for "player initiated an attack" (melee + spells +
push?). Verify-first: if scattered, v1 hooks melee + spell damage
attribution only, and the gap (indirect harm: pushing a guest into
lava) is a documented 🧪 deferral — matching how Qud-parity work
handles edge semantics.

**R5 — Tent villages via VillageBuilder may fight its assumptions**
(wall blueprint is hardcoded "Wall" at VillageBuilder.cs:85,91).
Smallest-diff option if a parameterized wall is invasive: keep stone
buildings, add tent stamps AROUND them, and let the roster + oath do
the differentiation. Decide at W2.6 implementation with the file open —
both outcomes acceptable, neither blocks anything upstream.

**R6 — The look-pass gate is now load-bearing** for six new formations
at once. Budget it as a real step (W1.1b found 3-of-4 wrong); the
commit for W2.1 does not land until the dump has been looked at.

**R7 — Scope guard: W2 adds no new status-effect *system* work** — the
recent simplification arc (one door, one readout) must not be quietly
re-complicated. Parched and UnderTheCloth go through Entity.ApplyEffect
and the existing readout like everything else; if either needs a
special path, that is a design smell to stop on, not code around.

### 7.10 W2 exit criteria

Full suite green; every SM's per-invariant counter-checks in;
the oath adversarial file (W2.5) run; ASCII look pass on all six
formations + the camp recorded in this doc; PlayMode sanity sweep with
honesty bounds; cold-eye + hypothesis pass logged in §7.11; SCOPE
DIVERGENCE sections wherever D1-D5 changed the design doc's word.

### 7.11 W2 implementation log

**W2.1 ✅** (`b664a0f6`, +13 tests) — six formations. The look pass
earned its keep again: DuneBelt's first cut failed it (modulo gaps
aligned into vertical fence-seams; free-rolled bands clumped) and was
reworked to stratified crests with rolled gaps before commit. Three
tiny prop blueprints (SaltCrust/DuneCrest/Bones). An early whole-file
json.dump rewrite of Objects.json was reverted for diff hygiene.

**W2.2 ✅** (`83817f23`, +6) — Beating bestiary; SunStriker + Saltbriar
new; the D3 indicator-species absence is PINNED by a test whose
failure message says it's a design conversation. The fail-loud
blueprint gate caught a nonexistent "Waterskin" during authoring.
Noted in passing: shipped GlassScorpion carries Faction=
SaccharineConcord (looks like a content bug; spun off as a task).

**W2.3 ✅** (`7c4548b3`, +13) — Parched + glare + wells.
VERIFICATION-LOOP CHANGE: the GUI editor wedged mid-compile and its
relaunches stalled on a stale Temp/UnityLockfile (root cause of the
previous session's stall too). Switched to HEADLESS batchmode test
runs (Unity -batchmode -runTests): full suite in ~40s, no bridge, no
modals. Adopted for the rest of W2/W3.

**W2.4 ✅** (`d23ac576`, +3) — tent camp + the `interior` stamp
pseudo-marker (wired in all three legend-validating places). Two gate
catches pre-commit (HermitHut's arg is an NPC blueprint; TentWall's
render-coverage entry).

**W2.5 ✅** (`94e7b862`, +13) — the oath. R3 resolved: expiry measured
on the WorldClock, not owner-turns (a rest jumps 60 ticks in one
turn). R4 resolved: melee chokes at PerformMeleeAttack:92 (breaks on
the SWING, pre-veto), spells at ApplySpellDamage's head; indirect harm
is the named 🧪 deferral. All 13 tests green first run.

**W2.6 ✅** (`c6a65ede`, +6) — place profiles; Wellmeet keeps its
canon "town-half" under the Tent-Right layer; predecessors authored
by zone id.

**W2.7 ✅** (`02b7e48f`, +5) — salt economy; MineralTradeService's
first production caller, months after it shipped with zero.

**Mid-W2 review ✅** (three-lens adversarial workflow over W2.1-W2.3;
20 agents, 17 confirmed findings, 0 refuted). Fixed in the review
commit:
- 🔴 RuinField's perimeter walk double-visited corners and the
  antipodal gap offset mapped corner indices onto each other — ~41%
  of rooms sealed with BOTH doors voided (verifier measured it by
  simulation). Third occurrence of the gaps-vs-walls coordinate bug
  class. Fixed: distinct-cell walk (2w+2h-4). AND the fix's own
  sealed-room detector then caught the COMPOSITION case (seed 21:
  overlapping rooms sealing a pocket through their doors) — the
  repair now enforces the real property, "every open cell reachable",
  breaching only builder-placed walls.
- 🟡 Glare streak is a static that survived save/load and new games —
  nine pre-death exposed turns carried into the loaded game. Reset()
  wired at bootstrap (beside WorldClock.Reset) and in ApplyLoadedGame.
- 🟡 ParchedEffect's water cure violated the Effect.cs removal-cause
  contract (reported "duration_expired" from a never-expires effect).
  Now "cured_by_water", pinned.
- 🟡 Parched + UnderTheCloth had no EffectDescriber lines (look mode
  showed the bare fallback). Added, with stacks / ticks remaining.
- 🟡 The tonic cure was tested only via a direct ReduceOneStack call —
  through-the-real-TonicPart testing exposed that WaterTonic wasn't
  Drink=true, so the OBVIOUS cure didn't work in play. WaterTonic now
  drinks ("Cool water, all the way down.").
- 🟡 THIS SECTION was empty through seven commits — the living-doc
  rule violated; filled now and kept current.
- 🔵 GlareExposure diag now follows the effect-category convention
  (bearer in target); §7.8's wording updated to match the shipped
  apply-only shape. SaltPan's lying "never adjacent" comment fixed.
- ⚪ WellPart everywhere: DECISION, not bug — every village well
  serves water and cures Parched ("water is the truest wealth and is
  given, never sold"). No fouled-well mechanic exists to conflict.

**W2.8 ✅** (`09c76524` the tenth fire; `b992af25` its look pass) —
the fire's zone rolled a hermit hut and a tomb on the first dump
(mystery needs emptiness → bare CreateTenthFirePipeline), then a vein
'*' collided with the fire's glyph (→ OmitSaltVeins knob). Final
state: one '*' in the zone, and it's the fire.

**W2 close-out ✅** (three-lens adversarial workflow over W2.4-W2.8 +
the review commit; 3 findings adversarially CONFIRMED before 15 of 21
verify agents died on a spend limit; the raised-but-unverified titles
were then triaged INLINE — reads, not agents — and every real one
fixed in the close-out commit):
- 🔴 **The one-way shield** (all three find-lenses raised it
  independently): the floor silenced ALL people's hostility toward
  the guest, but Break() fired only for TentRight/fellow-guest
  victims — a three-day window of one-sided killing (floored
  Snapjaws mechanically could not answer). Fixed by symmetry: one
  IsPerson test backs BOTH static seams — whoever the cloth floors,
  swinging at them forfeits it (D1's literal "attacks anyone",
  beasts carved out). RED-confirmed, then GREEN.
- 🟡 Recruit-under-truce: Persuasion_Recruit's hostility veto read
  the FLOORED feeling, so a truce-bound raider was recruitable — and
  stayed recruited after the third day. Veto #7 now reads
  GetFeelingUnfloored (new, contract-documented). RED→GREEN.
- 🟡 Panacea stripped the oath: CureEffect="All" called
  RemoveAllEffects, removing the covenant and printing the calm
  expiry line mid-oath. Cure-all now removes TYPE_NEGATIVE effects
  only (the WSP6.16 backfill finally load-bearing). RED→GREEN.
- 🟡 Break(guest, null) fell through the victim guard and landed the
  -100 hammer for a swing at nobody. Now: no victim, no crime.
  RED→GREEN.
- 🟡 The veto-side mirror: a FACTIONLESS wild critter with a personal
  grudge (you wounded it) was floored — null faction != "Beasts"
  made every wild thing a "person". IsPerson requires a faction: the
  wilds never signed. RED→GREEN.
- 🟡 Load-seam asymmetry (Q1): bootstrap resets WorldClock's band
  diff (W0.1) but ApplyLoadedGame reset only the glare static —
  loading into a different time-of-day band announced a transition
  the player never lived through. Reset() wired at the load seam;
  unit contract already pinned by WorldClockTests.
- 🔵 StampCatalog.Forced shared Rows/Legend refs with the ambient
  original (latent — current profiles reassign rather than mutate).
  Now deep-copies.
- Raised and REFUTED, pinned: "pursuers with an in-flight KillGoal
  keep attacking" — AI target selection re-validates hostility every
  evaluation (IsValidHostileTarget → IsHostile → the floor), so
  mid-hunt pursuit drops the guest the turn the cloth goes on.
  Pinned at the FindNearestHostile seam.

**SCOPE DIVERGENCE (W2.5, recorded here per §7.10):** plan §7.6 spec'd
the floor's exclusion as "Beasts/Snapjaws — the oath binds people,
not animals". Shipped: only Beasts excluded — **Snapjaws are floored,
deliberately**. Snapjaws are a registered people (Factions.json), and
canon's covenant is between people, not between nice people; OathTests
pins it as "even the raider knows what the cloth means". The Break
symmetry fix removes what made this divergence dangerous (attacking
the floored raider is now oathbreak, so the truce binds both ways).
The D1 narrowing ("attacks anyone" → TentRight/fellow-guests only)
was the OTHER unrecorded divergence — that one was a bug, and the
close-out fix restores D1's word.

**§7.10 exit status:** suite green ✅ (6837, twice); counter-checks
per SM ✅; oath adversarial file ✅ (OathAdversarialTests.cs, 14
tests: 6 RED-confirmed bugs, 8 pins, incl. the plan's two previously
untested named scenarios — claim-then-travel and rock-bottom rep);
look passes recorded ✅ (formations §W2.1, camp/profiles/tenth-fire
§W2.8); cold-eye + hypothesis pass ✅ (mid-review workflow W2.1-2.3 +
close-out workflow W2.4-2.8 + this inline triage); SCOPE DIVERGENCE
sections ✅ (above). **PlayMode sanity sweep: DEFERRED with honesty
bound** — the GUI editor's wedge cycle (stale UnityLockfile) made
live Play runs unreliable this arc; everything asserted above is
EditMode-observable (effects, feelings, diags, vetoes). What remains
UNVERIFIED live: message-log ordering on oath events in real play,
glare/well interaction on a real surface walk, camp look in color.
First manual playtest should walk a tent camp; the deferral is debt,
not completion.

## 8. Deferred / explicitly out of scope (this document)

- Full Urqu-bleed mask, per-cell bitfield, trigger tables (`§7.2` of
  the design doc) — W7. This plan's `Zone.UrquBleedLevel` is a
  deliberately minimal seed field, not that system.
- Hour-band-conditional lighting (a "faint glow at dusk" consumer of
  `WorldClock`) — cut from SM3 (§6 R-notes), first real use likely
  lands with W2's glare work (§7.3) or later.
- The full blind-reviewer voice gate for SM6/SM7's new text — noted
  as accepted debt (§6 R4).
- The closure-ledger's coarse counters (§7.7 of the design doc) — W7/W8.
- Any of W3-W8's biomes.
