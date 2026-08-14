# Flammability — Stock-Take and Redesign Plan

> Status: **PROPOSED** — awaiting user green-light.
> Trigger: charm flowers wreathed in fire FX while provably not burning
> (diag: one `BurningEffect` in the whole buffer, zero refusals).
> Companion docs: `Docs/OBJECT-INTERACTION-PLAN.md` §5/§8 (the effect
> matrix and the FireDose calibration), `CLAUDE.md` §Observability.

---

## 1. The incident that prompted this

Ember Spit hit a snapjaw ("…and catches!"). The player saw red ASCII
flashing over the surrounding charm flowers and reasonably concluded
they were alight. Look mode and the 'c' menu said otherwise.

**The diag settles it — the flowers never caught, and were never even
considered:**

- `effect/OnApply` with `BurningEffect`: **exactly one** record in the
  buffer — actor `3006` (player) → target `7065` (the snapjaw), turn
  1570, matching the log line.
- `effect/ObjectEffectRefused`: **count 0**. Not refused — refusal
  requires reaching a gate, and no gate was ever consulted.

**Why nothing reached them.** `CharmFlowers` is authored as
`Render + Examinable + Physics` — no `MaterialPart`, no `ThermalPart`,
no `DestructiblePart`. Consequences, layer by layer:

1. `MaterialSimSystem.EmitHeatToAdjacent` (the burning snapjaw's
   per-turn radiation) checks `GetPart<ThermalPart>()` and `continue`s
   past anything without one — **silently** (MaterialSimSystem.cs:126).
2. `AbilityTargeting.IsElementalTarget` returns false (no structural
   HP, no material, no temperature), so direct casts pass through.
3. Had the matrix ever been asked, it would have refused
   `WrongMaterial` — but nothing asks.

**The red flash is cosmetic.** `BurningEffect` implements
`IAuraProvider`; `StatusEffectsPart.TryStartAura` →
`AsciiFxBus.StartAura` paints fire-themed ASCII *around the burning
anchor* — over whatever happens to be adjacent, burning or not. The
game showed fire where there was none. Look mode was the honest
surface.

---

## 2. Stock-take — the four layers as they exist

| Layer | Owner | Gate | Records a refusal? |
|---|---|---|---|
| **Thermal sim** | `ThermalPart` — temperature, `FlameTemperature`, capacity, per-turn decay; fed by `ApplyHeat` events | `MaterialPart` veto (`Combustibility ≤ 0`), `WetEffect > 0.35` | ❌ no — and absent ThermalPart is a silent skip |
| **Direct effect** | `ObjectStatusMatrix.TryApply(BurningEffect)` — FireBolt, PyroIgnition (all Pyromancy skills) | material **tags** (`Flammable/Organic/Wood/Plant/Cloth/Paper/Fungal`) | ✅ `ObjectEffectRefused` with reason |
| **Tile layer** | `ZoneTileStateSystem` — ground heat/residues (oil slicks, embers); disjoint from entities | tile state definitions | n/a |
| **Presentation** | `IAuraProvider` → `AsciiFxBus` aura around a burning anchor | none — cosmetic | n/a |

### What is sound and should be kept

- **The physical model.** Joules → degrees via capacity, threshold
  crossings, per-turn decay, wet suppression with boil-off,
  volatility lowering the bar. It produces the right *texture*: heavy
  fuel is slow, tinder is fast, water is a delay not an immunity.
- **`FireDose`** (calibrated this week): one anchor, measured table,
  regression-pinned.
- **The matrix's fail-closed shape** for effect-kind gating (no bleed
  timers on barrels), with diag on both refusal kinds.
- **The veto layering** — combustibility zero means never, wherever
  the temperature goes.

### Defects

**D1 — Flammability costs three hand-authored parts, and each absence
fails differently and silently.** `MaterialPart` (else the matrix
refuses), `ThermalPart` (else heat is skipped and spread cannot
arrive), `DestructiblePart` (else it burns forever, unconsumed). There
is no single declaration and no enforcement. Measured: **70 scenery
blueprints carry neither Material nor Thermal**, roughly 25 of them
real-life flammable — CharmFlowers, Reeds, Grass, MendleafPlant,
crops (CandyCarrotCrop, EmberwheatCrop, CropRow), MushroomRing,
Beehive, HollowStump, Signpost, Chair, Bed, Sack, MarketStall,
FallenBeam, SaltCuredBody, Cactus… This same class of gap has now
shipped three times (Hedge/Tree in `7f7107c4`, the doses in
`dc1f533f`, the 70 today). The architecture makes the mistake easy
and invisible; that is an architecture problem, not a diligence
problem.

**D2 — Two flammability authorities that can disagree.** The matrix
reads *tags*; the thermal sim reads *`Combustibility` + ThermalPart
presence*. A blueprint can be matrix-flammable but thermally invisible
(direct FireBolt lights it; spread never can), or the reverse. Nothing
reconciles them.

**D3 — The heat path violates the observability rule.** Every gate is
supposed to emit a diag record on each branch (`CLAUDE.md`
§Observability). The matrix does. The heat path does not: a dropped
`ApplyHeat` on a thermal-less target leaves *no trace anywhere* —
which is precisely why "are the flowers burning?" needed a debugging
session instead of one query.

**D4 — Fourteen hand-rolled `ApplyHeat` call sites.** The same shape
that made damage routing wrong everywhere before `RouteDamage`
existed. Any future fix to heat delivery must find all fourteen again.

**D5 — The Combustibility/Conductivity dual-scale drift** (0–1 vs
0–100 in the same file; already spun off as its own task). Any numeric
gate added to this system inherits the ambiguity until it is resolved.

**D6 (minor) — The aura is honest about its anchor and misleading
about its neighbours.** It painted fire over the flowers. After D1 is
fixed the picture usually becomes true (adjacent flammables *will*
catch), so this is deferred as a playtest item, not engineering.

**Verdict: the mechanisms are good; the *authoring model* and the
*observability* are not.** The redesign below keeps every mechanism
and replaces "remember to add three parts" with "declare one material,
everything derives, a test proves the whole roster coheres."

---

## 3. The redesign

### 3.1 Material profiles — one declaration, everything derives

A `MaterialProfiles` registry keyed by `MaterialID` (`Wood`, `Plant`,
`Fungal`, `Cloth`, `Stone`, `Metal`, `Flesh`, `Ice`, `Oil`, …), each
profile holding: material tags, combustibility, flame temperature,
heat capacity, decay rate, and *default* structural HP + hardness.

`EntityFactory` bake: when a blueprint has a `Material` part naming a
profile, derive the missing parts —

- flammable profile + no `ThermalPart` → attach one from the profile;
- flammable profile + no `DestructiblePart` → attach the default
  (a thing that can burn away must have HP to burn away *from*);
- fill empty `MaterialTagsRaw` from the profile.

**Blueprint-authored values always win.** Profiles are defaults, not
mandates — Tree keeps its hand-tuned 30 HP and 360° flame point;
`Indestructible` and explicit omissions are respected. Nothing about
the existing 90 Material-carrying blueprints changes behaviour unless
they were relying on a missing part (which is the bug).

Authoring a flammable object drops from three parts (~20 JSON lines,
four numbers to get right) to one line: `"Material": "Plant"`.

### 3.2 One door for heat — `HeatDelivery`

`HeatDelivery.Deliver(target, joules, source, zone, radiant=false)` —
the `RouteDamage` of heat. All fourteen call sites route through it.
On the silent branch (no `ThermalPart`) it emits
`heat/HeatIgnored { reason: "no_thermal_part", blueprintName }`
(channel-gated). The adjacency emitter keeps its cheap skip in the
inner loop — per-turn × 8 neighbours must not spam the buffer — but
every *deliberate cast* that lands on a thermal-less target now leaves
a trace. "Why didn't it catch?" becomes one `diag_query` again.

### 3.3 Unify the authorities

`ObjectStatusMatrix`'s Flammable/Conductive/Freezable tag lists stop
being a third opinion: tags derive from the profile at bake (3.1), so
the matrix and the thermal sim read the same source. The matrix keeps
its per-*effect-kind* rows (bleed-on-a-barrel stays refused) — it
stops duplicating per-*material* truth.

### 3.4 Coherence tests — make the D1 gap unrepresentable

A register test that walks **every baked blueprint** and asserts:

1. flammable material ⇒ `ThermalPart` present, flame point reachable
   (`FireDose.Blast` can cross it from ambient);
2. flammable + reachable ⇒ `DestructiblePart` (nothing burns forever);
3. matrix verdict for `BurningEffect` ⇔ thermal ignitability agree;
4. the must-never-burn register holds: stairs, floors, ovens (heat
   *sources*, not fuel), runes/traps, AshPile (already burnt), veins.

Written **first, RED** — they fail against today's 70-blueprint gap —
and turned green by the content sweep. Any future blueprint that
half-declares flammability fails CI instead of shipping a silent
partial state.

### 3.5 Content sweep — the roster

From the 70-blueprint survey (D1), by profile:

| Profile | Blueprints |
|---|---|
| `Plant` | CharmFlowers, Reeds, Grass, MendleafPlant, CropRow, CandyCarrotCrop, EmberwheatCrop |
| `Fungal` | MushroomRing |
| `Wood` | Signpost, Chair, MarketStall, FallenBeam, HollowStump, Beehive (wax≈wood) |
| `Wood`+`Cloth` mix | Bed, Sack (Cloth) |
| `Flesh` (dry) | SaltCuredBody |
| `Succulent` (new: ignitable but hard — high moisture analogue) | Cactus |
| **Inert — explicitly excluded** | all floors, Stairs↑↓ (softlock register), Rubble, Stalagmite, BrokenColumn, RoadStone, Sand, ore veins, Oven, WatchLantern, runes/traps, Shrine, stations (AlchemyStill, TinkersForge), StrongBox/Reliquary (metal), Urn (ceramic), Graveyard, Bone/OreCache, AshPile |
| **Check during implementation** | Well (wooden frame?), Bank (what is it?), MillStone/SmithAnvil/StoneCoffer (stone — inert) |

Item-base blueprints (Item, FoodItem, ArmorItem…) are out of this
sweep's scope; profiles apply to them naturally later since many
already carry Material.

### 3.6 Deliberately NOT in scope

- Tile-layer unification (`ZoneTileStateSystem` merging with entity
  thermal). Two models, but they meet only at ignition boundaries and
  both work; merging is high-risk, low-payoff now.
- Aura honesty (D6) — playtest item; revisit if post-sweep it still
  misleads.
- The dual-scale migration (D5) — already its own task; 3.1 sidesteps
  it by making profiles canonical for *new* derivation.

## 4. Sub-milestones (smallest blast radius first)

1. **M1 — RED coherence tests** (§3.4). Fails today; the measured
   statement of the problem.
2. **M2 — MaterialProfiles + bake derivation** (§3.1), unit-tested on
   fixtures; existing 90 Material blueprints pinned unchanged.
3. **M3 — Content sweep** (§3.5): one `"Material"` line per roster
   blueprint → M1 goes GREEN. *This is the commit where charm flowers
   actually catch fire.*
4. **M4 — HeatDelivery choke point + diag** (§3.2), all 14 sites.
5. **M5 — Matrix reads profile-derived tags** (§3.3) + adversarial
   sweep over the seams (profile overrides, Indestructible × derived
   HP, save/load of derived parts — reflection round-trip of parts
   attached at bake needs an explicit test).

**Performance note** (`CLAUDE.md` rule): derivation is bake-time only;
no per-frame or per-turn additions; the adjacency loop keeps its
early-out. The register tests are EditMode.

## 5. Honesty bounds

- The 70/25 counts come from an inherit-chain script over
  `Objects.json`, not from play; "real-life flammable" judgments are
  mine and listed per-blueprint above precisely so they can be argued
  with.
- Spread *pacing* after the sweep (a lit field of charm flowers) is
  arithmetic, not playtest. `FireDose.SpreadTotal` is the one knob.
- Whether derived parts round-trip through save/load is asserted in
  M5, not assumed — parts attached at bake rather than authored in
  JSON are exactly the kind of thing the save reflection could miss.
