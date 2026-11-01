# Upper-tier bestiary and enemy behaviour (P5) — design and plan

**Status:** design 2026-10-31. Implements priorities 1 and 2 of the fun-gap
analysis: fill tiers 3–5 with creatures worth meeting, and give enemies
decisions. CoO-original behaviour; canon from `sarisarinama_bestiary_design.md`,
`catacomb_village_design.md`, `Lore/Factions/08_DrivingBloom.md`,
`Lore/History/05_Spirits.md` and `Docs/FELLING-WORLD-DESIGN.md` §3.5–3.6.

## 1. Evidence (seed 64 progression bench, 2026-10-31)

| Tier | Zones | Hostiles | Kinds | Kill XP vs band need | Melee duel win |
|---|---|---|---|---|---|
| 3 | 138 | 249 | 8 | 56,222 / 72,220 | 18–71% |
| 4 | 80 | 57 | 2 | 49,760 / 186,580 | 12–75% |
| 5 | 26 | 22 | 1 | 34,744 / 264,360 | 0–25% |

- The roster has 72 tier-1, 35 tier-2, 11 tier-3, 2 tier-4 and 1 tier-5
  creatures.
- Only 16 of 122 creatures use tactics. Of the 14 creatures at tier 3 and
  above, only the marlback breacher does.
- Eleven of the 14 have no description. Most are bare stat blocks: no brain
  settings, no behaviour, no way to tell them apart except by numbers.
- Surface tier-4 cells are the Overwrit (23), the Stump (19) and the Grovelands
  (2); tier-5 cells are the Stump (2). The rest of tiers 4–5 are underground
  floors under deep places.

## 2. Verification sweep — corrections and confirmed premises

| Premise | Evidence | Result |
|---|---|---|
| The Overwrit is empty because content is missing | Canon (FELLING-WORLD-DESIGN §3.5): "**Population.** Nearly none." Scarcity is "enforced by canon, not taste" | **False premise.** The Overwrit stays sparse. The fill goes to the Stump, the deep floors and the dead zones. |
| The Sky-Sari and the Sari-Snake spawn on the Stump | `StumpSlopes`/`StumpSummit` gate both on `RequiresWorldFlag = "UrquActive"`. Nothing in the code ever sets that fact | **False premise, and a real gap.** The Stump's apex predator and its Urqu indicator never spawn. A producer is required (§3.1). |
| Urqu manifests on a timer | Canon (`05_Spirits.md` §manifestation): "**consequence, not trigger** … at catastrophic conditions — a faction's collapse, a city falling, a mass-death event, a specific atrocity"; the Thinning has doubled their frequency; "the *sari* gradient peaking at the Site" (§3.6) | The producer is a fixed gradient around the Felling-Site plus consequence-driven local manifestations. There is no clock. |
| New AI behaviour needs a new AI framework | `BoredGoal` already asks per-creature parts for an idle action (`SpreadTerritoryPart`, `SpreadPredatorPart`, `SpreadGrazerPart`); `KillGoal` asks `CommittedMeleePart` and `CombatTacticsPart` before an ordinary attack | **Confirmed hooks.** New behaviours are parts consulted at those two points, behind one small interface each. |
| A grapple effect exists | `RootedEffect` (cannot move, can act) exists; there is no grapple or drag | A new `GrappledEffect` builds on the Rooted contract (break-free, a source, drag). |
| A noise or stealth system exists | None (`SimpleNoise` is terrain noise) | A minimal noise stamp: moving, attacking or casting records the actor's last noise turn. |
| Telegraphed heavy strikes exist | `CommittedMeleePart`: wind-up, fixed-ray strike, recovery; used by two blueprints | **Confirmed.** Reused for the constructs and brutes. |
| The upper creatures have flat-view art | Native flat catalogs cover SkySari, SariSnake, BrittleHound, ChoirTendril and Mosshulk. AncientGuardian, CanopyStrangler, DuneLurker, StoneGolem, VaultSentinel, PaleStalker, ObsidianBrute, PalimpsestEcho, EncasedElder and PrickleBrowGecko are glyphs. Most have approved 16×16 environment sprites, the accepted Merchant/Elder format (`GRAPHICS-SPECIAL-SITES.md` wild fauna) | The art slice admits the existing 16×16 bodies and authors originals for the new species. |
| Bloomed creatures exist | `BloomedEffect` drives a bearer (attack whatever is adjacent, walk to open ground) and **already erupts into spore gas when its bearer dies** (`OnRemove` with `CAUSE_OWNER_DIED`); `GasBloomSporesPart` passes the Bloom on | **Confirmed, and more.** Scope-pruned: `BloomEruptionPart` would duplicate the shipped eruption. The hosts need only a part that applies the effect at creation (`BloomOnCreatePart`). |

## 3. Design

### 3.1 Urqu pressure (unblocks the Stump's apex)

`UrquPressure.IsActiveAt(zoneId)` is true in any of these cases:
- **the story flag:** the global `UrquActive` fact is set;
- **the sari gradient:** the zone is a Stump cell within two cells of the
  Felling-Site (3,5). Canon's highest bleed point is always manifest;
- **a local manifestation:** consequence-driven, below.

**Local manifestation.** When five or more *persons* die in one zone within one
world day, the zone and its eight neighbours manifest for three days. Canon:
a mass-death event. Diag: `urqu/Manifested`.

Effects of an active zone:
1. Fresh generation admits `RequiresWorldFlag = "UrquActive"` entries. The
   population roll is zone-aware: `AllowedByWorldState(zoneId)`.
2. On a real arrival in an active zone, Urqu fauna is placed once per
   manifestation: a Sari-Snake clutch of 2–3, plus a Sky-Sari on the Stump or
   in the Beating. This is the `ConcernWorld` arrival pattern, with stable
   IDs, and it never refills.
3. A one-line arrival notice: *the air says sari, sari*.

### 3.2 The behaviour kit (priority 2)

Each behaviour is a blueprint-configured part, reached through one of two
interfaces:
- `ICombatBehavior.TryAct(target, zone, rng)`, which `KillGoal` consults
  before the ordinary attack;
- `IIdleBehavior.TryIdle(brain, zone)`, which `BoredGoal` consults.

Every gate emits a diag record (`ai/*`).

| Part | What the player meets | Users |
|---|---|---|
| **StoopPart** | A shadow marks your cell two turns before the hit. Step out and it misses; stay and take double damage. Canon: "a shadow crosses your tile two turns before the hit". | Sky-Sari |
| **GrapplePart** + `GrappledEffect` | A hit can seize you. You cannot walk away, and each move is a Strength contest to break free. A drag-grappler hauls you toward water, where you take drowning damage; a constrictor squeezes instead. Ends when you break free, the grappler dies, or the two of you are separated. | Riverwarden (drag), Canopy Strangler (constrict) |
| **PackHunterPart** | Pack-mates fan out to flank. A strike on a target that already has a pack-mate beside it gets +2 to hit, and the first sight brings the pack. | Brittle Hound, Pale Stalker |
| **HuntsBySoundPart** | Eyeless. It perceives what moved, struck or cast within its hearing radius in the last turn, through darkness and around corners, and nothing that stood still. Stand still and it passes. | Pale Stalker (the dead-zone apex the catacomb Wardens hunt) |
| **ColonyPart** | Strike one and the whole colony within range turns on you, with a warning call. | Pricklebrow nests, Sima-Steward roosts |
| `BloomedEffect` (existing) + **BloomOnCreatePart** | The host is driven: it walks for open ground and strikes whatever is beside it. At death it bursts, and the spores pass the Bloom on. | Bloomed hosts |
| `CommittedMeleePart` (existing) | Telegraphed heavy swing: the wind-up shows, so step out of the line. | Ancient Guardian, Stone Golem, Vault Sentinel, Obsidian Brute |
| `CombatTacticsPart` (existing) | Ranged and active abilities. | Vault Sentinel (arc bolt), Palimpsest Echo (ice lance), Mosshulk (slam) |
| Brain settings | Flee variety: constructs never flee; predators flee at 15–35%; colony members at 50%. | all |

### 3.3 The roster (priority 1)

**Existing upper creatures, made distinct.** Each of the 14 gets a
description, brain settings and at least one behaviour from §3.2.

**New species (canon):**

| Creature | Tier | Where | Behaviour |
|---|---|---|---|
| **Riverwarden** (dwarf caiman) | 3 | Stump foothill creeks, drowned sima floors | ambush in water, grapple and drag, high armour |
| **Sima-Steward** (oilbird) | 3 | sinkhole floors, Stump simas | passive colony; striking one turns the roost; killing one costs Catacomb-folk standing (canon taboo) |
| **Bloomed Warden** (a catacomb warden, worn) | 4 | deep floors at tier 4+, the Grovelands near the Stump | Bloomed drive, eruption at death |
| **Bloomed Hound** | 4 | the same | a Bloomed pack, eruption at death |

**Elders** (the same body, older and worse; tier +1). Named variants by
blueprint inheritance:
- the **Old Riverwarden** (tier 4);
- the **Old Sky-Sari** (tier 5): two shadows, or a stoop at triple damage;
- the **Pale Matriarch** (tier 5): leads a Pale Stalker pack and hears at
  double radius.

After this slice the kinds per tier are:
- tier 3: 13;
- tier 4: 6 (Pale Stalker, Sky-Sari, Bloomed Warden, Bloomed Hound, Old
  Riverwarden, and the Sari-Snake clutches where Urqu is active);
- tier 5: 3 (Obsidian Brute, Old Sky-Sari, Pale Matriarch).

### 3.4 Population

- **Stump bands:** foothills gain the Riverwarden (tier ≥ 3) and the Old
  Riverwarden (tier ≥ 4). The summit and slopes gain Urqu fauna through §3.1.
  The two tier-5 cells always carry the gradient.
- **Underground floors at tier ≥ 3:** Pale Stalker packs (2–3), Bloomed hosts
  at tier ≥ 4, and the Pale Matriarch at tier 5.
- **Sinkhole floors and the Stump sima:** Sima-Steward roosts and Pricklebrow
  colonies.
- **The Overwrit is untouched.**

### 3.5 Art

**Flat 16×24 view.**
- A new catalog admits the approved 16×16 environment bodies for the upper
  creatures that are still glyphs. Same rules as the wild-fauna slice:
  canonical, visible, living and untinted.
- The new species get original editable 16×16 sources
  (`ArtSource/Live16x24/upper-bestiary/`).
- A threat cue layer draws the stoop shadow on its marked cell. Its art is
  original and editable.

## 4. Sub-milestones (smallest blast radius first)

| # | Ships |
|---|---|
| B1 | The existing upper creatures made distinct: descriptions, brain and flee settings, committed strikes, tactics |
| B2 | Urqu pressure: the gradient, local manifestations from mass death, zone-aware flags, arrival fauna |
| B3 | Stoop, plus the stoop shadow cue layer |
| B4 | Grapple and drag, plus the Riverwarden |
| B5 | Pack hunting and hunting by sound, used by the Pale Stalker |
| B6 | Colonies, used by Pricklebrows and the Sima-Steward |
| B7 | The Bloomed hosts (born worn; the shipped eruption does the rest) |
| B8 | Elders and population wiring; the bench re-run (kinds, hostiles and XP per tier) |
| B9 | Art: flat admission of existing bodies and the new species' sprites |

Each milestone runs RED → GREEN → counter-checks → adversarial → review →
doc → commit.

## 5. Performance

- Behaviour parts run only in a creature's own turn, and only for creatures
  that carry them.
- The noise stamp is one int property per mover.
- Colony alarm scans the zone once, only when a member is struck.
- The cue layer is pooled and runs on a cadence, like `FlatQuestCueLayer`.

## 6. Observability

Every kind is recorded in the `ai` channel, except manifestation, which uses
`urqu`:
- **Stoop:** StoopMarked, StoopHit, StoopMissed.
- **Grapple:** Grappled, GrappleBroken, Dragged.
- **Pack:** Flanked.
- **Sound:** HeardTarget, LostTarget.
- **Colony:** ColonyAlarm.
- **Bloom:** BloomErupted.
- **Urqu:** Manifested, ArrivalFauna.

## 7. Honesty bounds

- These are CoO-original behaviours. Nothing here claims Qud parity.
- Live play of each behaviour is required before the wave counts as
  accepted. Offline tests prove contracts, not feel.

## 8. Implemented (2026-10-31)

### B1 — the existing upper creatures, made distinct
- **Descriptions:** all ten bare stat blocks now have one, saying how each
  fights.
- **Brain settings:** constructs and hulks never flee (Ancient Guardian, Stone
  Golem, Vault Sentinel, Obsidian Brute, Mosshulk). Predators flee at 15–30%.
- **Heavy swings:** `CommittedMeleePart.DamagePercent` (default 100, the
  original contract).
  - The Ancient Guardian and Stone Golem swing at 200%, the Obsidian Brute at
    250%: wound up, telegraphed, sidestepped.
  - The scope belongs to the striking attacker, so a defender's counter
    inside the same swing is not multiplied.
  - A zero or negative percent is clamped to 1% and never heals.
- **Abilities:** the Vault Sentinel uses arc bolt at range 3, the Palimpsest
  Echo ice lance, the Mosshulk slam.
- **Pack calls:** Brittle Hounds and Pale Stalkers call their pack
  (`AssistAllies`).

### B2 — Urqu pressure
- `UrquPressure` holds the Felling-Site gradient (Stump cells within two of
  3,5, at any depth) and the story fact.
- **Local manifestations.** Four persons dead in one settlement zone in one
  world day, or ten anywhere, manifest that cell and its eight neighbours for
  three days. A later manifestation extends but never shortens an earlier
  one. Beasts do not count, and deaths on different days do not add up.
- **Population gate.** Entries gated on `UrquActive` are now regional:
  `AllowedByWorldState(zoneId)`.
- **Arrival fauna.** A real arrival places a Sari-Snake clutch, plus a
  Sky-Sari on the Stump or in the Beating, once per manifestation, with
  stable IDs.
- Diag: `urqu/Manifested`, `urqu/ArrivalFauna` (channel on by default).

### B3 — the stoop
- `StoopPart` (Sky-Sari):
  - it marks a seen quarry 2–8 cells away, then is aloft for two turns,
    owning those turns through the brain's pending hook;
  - it drops beside the mark at 200%, or misses an empty cell;
  - it then rests four turns.
- `FlatThreatCueLayer` draws the original editable shadow
  (`ArtSource/Live16x24/threat-cue`) on the marked cell, under actor bodies.

### B4 — grapple and drag
- **`GrappledEffect`:**
  - the bearer acts but cannot walk away;
  - each move is a d20 + Strength contest, one per tick (spamming the key
    does not re-roll);
  - the grip ends when the grappler dies or is not adjacent;
  - the grappler is recorded by ID;
  - list-safe: a break marks the effect broken and lets the ordinary cleanup
    remove it.
- **`GrapplePart`:**
  - a drag grappler hauls its quarry toward the nearest free water (any
    liquid pool) and follows it; the quarry drowns there;
  - a constrictor squeezes instead.
- **The Riverwarden** (tier 3) and its drag. The Canopy Strangler constricts.
- A Stump habitat rule keeps riverwardens beside water.

### B5 — pack and sound
- **`PackHunterPart`:** +2 to hit when a pack-mate (same faction, same part)
  is beside the quarry; it circles to the far flank rather than queueing.
- **`Noise`:** moving (every `MovementSystem` landing), swinging and casting
  stamp the actor's tick.
- **`HuntsBySoundPart`** (Pale Stalker) replaces line of sight in every AI
  perception check (`AIHelpers` routes through `IPerceptionBehavior`). A
  target that stands still is not perceived, even beside it.

### B6 — colonies
- **`ColonyPart`:** striking one member makes every member of that blueprint
  within its radius personally hostile, with one call per tick. Pricklebrow
  geckos are now passive colonies of 3–8 on the sima floors.
- **The Sima-Steward** (tier 3, Wildlife, passive) is a roosting colony.
  `TabooKillPart`: killing one costs the player 10 Catacomb-folk standing.

### B7 — the Bloomed hosts
- The Bloomed Warden and Bloomed Hound (tier 4) are created already worn
  (`BloomOnCreatePart`). The existing `BloomedEffect` drives them, and its
  shipped eruption spreads the Bloom.
- A cured host stays cured.

### B8 — elders and population
- **Elders:**
  - the Old Riverwarden (tier 4, 6 drowning, 65% grip);
  - the Old Sky-Sari (tier 5, 300% stoop, range 10);
  - the Pale Matriarch (tier 5, hearing 16).
- **XP and descriptions.** Every new kind pays `XPValueForLevel` (the
  `XPValue` maximum is raised past 999) and has a description.
- **Tables:**
  - Stump foothills gain the Riverwarden (tier ≥ 3) and the Old Riverwarden
    (tier ≥ 4); the tier-5 Stump gains the Old Sky-Sari (Urqu-gated, always
    open in the gradient).
  - Sima floors gain Sima-Steward roosts and Pricklebrow colonies.
  - Deep floors at tier ≥ 4 gain Pale Stalker packs (2–3) and the Bloomed
    hosts; tier 5 gains the Pale Matriarch with her line.

### B9 — art
- `FlatPixelWildFaunaCatalog` gains a table-driven upper-tier section. The
  existing 16×16 bodies now draw for:
  - Ancient Guardian, Canopy Strangler, Dune Lurker, Stone Golem, Vault
    Sentinel, Pale Stalker, Obsidian Brute, Palimpsest Echo and the
    Pricklebrow;
  - the elders, which wear their kind's body.
- Original editable bodies (`ArtSource/Live16x24/upper-bestiary`) for the
  Riverwarden, Sima-Steward, Bloomed Warden and Bloomed Hound.
- Bloomed hosts accept the Bloom's tint. Nothing else does.

### Found and fixed while building
- 🟡 A high-level creature's XP was silently capped at 999 by the base stat
  maximum; caught by the XP pin.
- 🟡 Tests with an always-maximum RNG hung in an endless penetration chain.
  The test RNG now caps the d10 at 9, the `CommittedMeleeTests` convention.
  This is a test-harness hazard, not a game bug.

### Divergences

| Plan | Shipped | Why |
|---|---|---|
| `BloomEruptionPart` | Not built | `BloomedEffect` already erupts on death (§2) |
| ai diag channel on by default | Left off | It is high-volume and deliberately off; tests enable it |
| Every behaviour sends HeardTarget/LostTarget | Not recorded | Perception runs inside hot scans; a diag there would cost every AI scan |

## Implementation log

- 2026-10-31 — design written; verification sweep (`UrquActive` has no
  producer; the Overwrit is canonically sparse; upper-creature flat art gap).
- 2026-10-31 — B1–B9 implemented: heavy swings, Urqu pressure, stoop, grapple/drag, pack and
  sound hunting, colonies, Bloomed hosts, elders, population and flat art.
