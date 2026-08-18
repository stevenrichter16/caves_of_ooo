# W1 (finish) + W2 — The Spread & Sill close-out, then The Beating

> **Status: W1-remainder planned in full, ready to implement. W2
> scoped at content-readiness level; detailed sub-milestones written
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
| W1 remainder (this doc, §2) | 🔲 planned here | — |
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

(filled after each SM)

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

New diag: `worldgen/FlowerFieldWilted` (or fold into the existing
`worldgen/FormationApplied` category — decide at implementation time
which reads better) carrying `{baseDuration, bleedLevel, wiltPerUnit,
finalDuration}` so the (currently-dormant) instrument is auditable
the moment W7 gives it a real writer. `SillHearsIt`'s firing is
already covered by the existing storylet dispatch's own diag path (no
new category needed — confirm at implementation time whether
`StoryletPart` diag's a fire event already; add one if not, mirroring
the quest-stage-advance diag pattern at `StoryletPart.cs`'s pass 2B).

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

## 7. W2 — The Beating & Tent-Right (scoped, not yet detailed)

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

### 7.3 Known gaps the detailed pass must resolve

- Whether the oath ships as a real AI-honored mechanic (a genuine
  `FactionManager`/`BrainPart` extension — "hostile AI holds at the
  boundary" per design doc §7.4) or a narrower first cut (a status +
  a dialogue-gated pacification verb, deferring full AI honor-gating).
  This is the single highest-uncertainty design call in W2 and
  deserves its own critical-review pass, not a decision buried in
  this scoping section.
- `Parched`'s exact numbers and whether Height-band glare needs a new
  `WorldClock` consumer pattern that W1's FlowerField dusk-glow (cut
  in SM3, §2.2) could share — worth resolving once, not twice.
- Whether Wellmeet/First Tent get bespoke builders or a
  richer-but-still-generic "place profile" layer that the other 14
  places could also eventually use (avoids a one-off special case
  that the *next* differentiated place has to reinvent).

---

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
