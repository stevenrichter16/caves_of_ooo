# Felling Arc (W0–W4) — Holistic Optimization Review

**Date:** 2026-08-21
**Status:** Assessment complete. No fixes applied — findings await triage.
**Method:** 21-agent adversarial workflow (4 find lenses — turn/combat hot
paths, gen-time algorithmics, per-frame/render, diag/allocation — then one
skeptic per candidate with instructions to refute), cross-checked by hand
where verifiers disagreed. 32 candidates raised, 12 survived verification,
5 refuted with corrected arithmetic, 15 verified fine-as-shipped. The
judge standard was the project's own rulebook: `Docs/PERF-FOUNDATION.md`
budgets (>32KB/frame investigate, <5KB/frame acceptable), CLAUDE.md's
"GC pressure is acceptable in turn-based combat" and profile-first rules.

---

## 0. HEADLINE — a correctness bug found in passing: TickEnd fires per ACTOR, and three systems assume per ROUND

> **STATUS: FIXED.** TurnManager stamps the ending actor onto the TickEnd
> event; GasSystemPart and CropSystemPart forward only for the player's
> turn end (the round boundary — including blocked/stunned rounds) or for
> unstamped events (bench/test compat). RED-confirmed pre-fix: the poison
> dose read 20 (4 actors × 5) where one round doses 5; crops advanced per
> NPC turn. Pinned by `TickEndActorGateTests` (6 tests: 3 RED→GREEN, 3
> counters). NarrativeStatePart/StoryletPart stay per-actor deliberately —
> their reactors are idempotent predicate polls; noted, not changed.

Two workflow verifiers disagreed on the gas tick rate ("once per player
turn" vs "once per actor turn"), so I settled it by hand. The per-actor
reading is correct, and it is worse than a perf finding:

- `TurnManager.EndTurn` fires `TickEnd` at the end of **every actor's**
  turn — player and each NPC (TurnManager.cs:404, called for NPCs at
  :332/:352).
- `ZoneTileStateSystem` and `WorldClock` **documented this exact trap and
  dodged it** — ZoneTileStateSystem.cs:25-30: *"TurnManager.EndTurn fires
  TickEnd once per ACTOR, so a 6-turn coating would evaporate in about one
  player turn in a crowded zone… duration would become a function of local
  population."* They route through `InputHandler.EndTurnAndProcess` (per
  player turn) instead.
- **`GasSystemPart` (GasSystemPart.cs:29) listens to raw TickEnd with no
  guard** — `GasSystem.OnTickEnd` runs the full dispersal + per-turn dose
  pass on every call. Consequences with A actors in the zone:
  - Gas decays and spreads A× per player round — clouds are nearly
    useless in crowded fights, exactly where gas matters.
  - `DispatchPerTurnApply` → `GasPoisonPart.ApplyGas` fires A× per
    round. The Poisoned effect is refresh-not-stack (mitigated), **but
    the immediate exposure damage (min 1, CombatSystem.ApplyDamage,
    GasPoisonPart.cs:79-84) lands on every call** — standing in gas
    beside 10 NPCs deals ~11× the authored per-turn dose.
  - Every pass re-emits the per-entity diag records (see §1).
- **`CropSystemPart` is a "byte-for-byte mirror of GasSystemPart"** (its
  own docstring) — `CropSystem.OnTickEnd` unconditionally decrements
  `MoistureTicks` and bumps `TicksInStage` per call. Crops in a populated
  village grow and dry out A× faster than authored.
- `NarrativeStatePart`/`StoryletPart` also poll per TickEnd — mostly
  idempotent predicate checks (redundant work, not wrong outcomes), but
  any turn-counting trigger semantics inherit the same skew.

**Fix shape (when approved):** either route gas/crops through the same
player-turn seam ZoneTileStateSystem uses, or stamp `OnTickEnd` with the
player-turn counter and no-op repeats. Needs RED tests pinning "one
dispersal pass per player round regardless of actor count" and "poison
dose independent of zone population." This is a W-arc-relevant bug (marsh
gas, spore clouds, and the Grovelands' burn-off all ride GasSystem) but
the seam itself predates W4.

---

## 1. RIPE — the burning-field cascade (three interlocking findings)

> **STATUS: FIXED (all three).** §1a: `GasFactory.SpawnGas` merges into a
> compatible same-cell cloud via the new public
> `GasSystem.FindMergeTarget` (MergeChunk semantics: sum density, max
> level, OR seeping, receiver keeps creator; `gas/SpawnMerged` record) —
> pinned by `GasSpawnMergeTests` (6 RED→GREEN + 3 counters); eight
> existing pins updated from entity-count to conserved-density asserts
> (BurnOff Number rolls, grenade overlap/double-detonate/cross-actor,
> contagion cadences) — the grenade overlap pin existed precisely "so a
> future merge-at-spawn change is explicit." §1b: `gas/Dispersed` moved
> to the off-by-default `gas-verbose` channel. §1c: `GasVisuals.Refresh`
> skips writes + dirty mark when glyph and background are unchanged.
> Live gasbench matrix re-run deferred to the next PlayMode session
> (honesty bound: EditMode pins prove merge/skip logic, not the live
> field-fire population curve).

The only cluster that crosses the project's own perf lines, and it is one
scenario: **set a peat/bog field on fire** (a designed-for W3/W4 player
action — the grove-law arc practically invites it).

### 1a. Unbounded stacked gas entities (the root)

`BurnOffGasPart` (BurnOffGasPart.cs:115) spawns a **fresh density-60
cloud per 6 fire damage** with no spawn-time merge and no cap. A 10–20
tile fire front sustains ~50–250 concurrent gas entities for tens of
turns (~400+ for a large simultaneous burn) — the same scale as the
documented 2026-05-23 236-entity editor freeze. Each turn at N=150:
~150 dispersal passes (RNG + part lookups + GasVisuals.Refresh +
MarkCellDirty), ~150 snapshot List allocations, and 300–900 eager diag
serializations — including a **quadratic** ApplyVetoed component from
stacked siblings vetoing each other.

**Fix:** merge-on-spawn. Expose `GasSystem.FindCompatibleGas` (currently
private, GasSystem.cs:311) and have `GasFactory.SpawnGas` add density
into a compatible existing cloud instead of creating a new entity —
reuses the shipped merge code, collapses the stacking, matches Qud merge
semantics, and fixes a K-doses-per-turn compounding on stacked clouds as
a side effect. Optional backstop: per-zone soft cap on Gas-tagged
entities. Verify on the `gasbench` deterministic scenario with a scripted
field burn, per the self-auditing-scenario rule.

### 1b. gas/Dispersed diag: one eager Newtonsoft serialization per gas entity per tick, always-on

GasSystem.cs:129-131 emits `gas/Dispersed` for every unstable pool on
every tick — and per §0, ticks fire per actor. At fire scale that is
hundreds to thousands of records per player round: 30–200KB of transient
garbage in the single turn-advance frame (crossing PERF-FOUNDATION's
>32KB investigate line, >100KB at the high end), and **the 8192-slot ring
buffer fully rotates in ~10–80 fire-turns**, evicting all other channels'
history — the observability substrate destroys itself precisely during
the fire you'd want to debug.

**Fix:** demote the per-entity `Dispersed` (and optionally `Spread`,
`Merged`) to an off-by-default `gas-verbose` channel — zero substrate
changes (unknown channels are off; `gasbench` is the precedent). Keep
`Dissipated`, `BurnOff`, `BurnOffChanceFailed` on the default channel
(low-frequency, high-signal). Companion: GasSystemTests.cs:573/601/625
assert gas/Dispersed and must enable the verbose channel in setup.
`GasPoisonPart`'s per-creature-per-tick `gas/Applied` (GasPoisonPart.cs:85)
deserves the same triage.

### 1c. GasVisuals.Refresh dirties the cell every tick even when the glyph is unchanged

GasVisuals.cs:64 — on stationary turns, ~N_gas redundant RenderCellCore
repaints plus a dilated sprite re-resolve pass (~100–500 cell ops/turn at
realistic fire N_gas). Verified real but small next to 1a; **ship it as a
ride-along** with the merge fix, not standalone: compare new glyph char +
background code against current RenderPart values, skip the write and
MarkCellDirty when both are unchanged.

---

## 2. RIPE — per-frame: the water shimmer now scales with bog size

> **STATUS: FIXED.** The skip logic lives in the pure static
> `WaterShimmer` (ColorIndex = the branch's historical formula,
> ClaimPaint stamps a byte[] cache reset by RefreshWaterCache on every
> full redraw; takeover paths Invalidate so creature transit repaints
> honestly). Pinned by `WaterShimmerTests` (RED was the compile error —
> new pure class). PERF-FOUNDATION's budget table carries the
> re-measure-live honesty bound; known ⚪: a dirty-cell repaint that
> leaves water on top can show base color ≤0.5s on that one cell until
> the next band tick.

The one true per-frame finding. ZoneRenderer.cs:2010 — the stationary
water-shimmer loop runs `GetCell` + `GetTopVisibleObject` (per-object
GetPart scan) + 2× HasTag + `SetTileFlags`/`SetColor` for **every**
water cell **every frame**. W3's Sodden formations put 250–450 MirePool/
PeatBog cells in one zone (OpenMire worst case) → ~15k–27k iterations/sec
with ~500–900 native tilemap calls per frame, **~97% of which write an
unchanged color**. Estimated +0.8–1.5ms/frame — a ≥2× regression on the
measured 0.78ms `UpdateAmbientAnimations` budget row
(PERF-FOUNDATION.md:408).

**Fix:** parallel `byte[]` of last-written colorIndex (255 sentinel,
rebuilt in RefreshWaterCache); compute colorIndex first, skip both native
calls when unchanged (~30× fewer — the index only changes 2×/sec/cell).
Optionally tick the stationary branch on a 6–10Hz accumulator; visually
lossless either way. Verify with the existing
`PerformanceMarkers.Zone.UpdateAmbientAnimations` recorder and update the
PERF-FOUNDATION budget table per its living-follow-ups rule.

---

## 3. MODERATE — observability fixes wearing perf clothing

> **STATUS: FIXED (all three).** §3a: NPC turn Begin/End route to
> `turn-verbose` (off by default), the player's stay on `turn`; pairing
> survives per-actor within each channel. §3b: one `tile/PropagationWave`
> aggregate per flood that moved anything (wave, seedCount, reached,
> medium split, bbox); per-cell `PropagationStep` behind `tile-verbose`;
> the 3×-per-turn seed `List<int>` hoisted to static scratch. §3c: the
> attacker null/self early-outs moved above the elemental check — no
> rejection record without an attacker to reject. All pinned by
> `DiagChannelSplitTests` (7 RED→GREEN + 2 counters);
> `TilePropagationTests.PropagationEmitsAStepRecord` retargeted to the
> verbose channel it now proves.

All three are cheap, none is a frame-rate issue; their real payoff is
that the diag ring buffer stops eating itself.

| # | Finding | Cost today | Fix |
|---|---|---|---|
| 3a | `turn/Begin`+`turn/End` eagerly serialize 2 records per **actor** per turn (TurnManager.cs:307/376) — 38 records per player action at 18 NPCs; the ring rotates in ~215 player turns (~2–5 min), evicting all other history. Diag.cs:107-109 already measured this (`dropped_records: 17733`) and pre-approves the fix. | ~45–95KB + ~0.1–0.2ms per player turn (below the bar as perf) | Route NPC records to default-off `turn-verbose`; keep the player's on `turn` (`actor.HasTag("Player")` gate). ~10 lines + emission-contract test updates. Do **not** hand-roll the serializer — that optimizes the below-bar half. |
| 3b | `TilePropagationSystem` emits one eagerly-serialized `tile/PropagationStep` per newly reached cell inside a flood that runs **every player turn** (TilePropagationSystem.cs:262); peat fires are self-sustaining (embers re-ignite interior cells indefinitely), so a 100–300-cell field emits 25–75/turn at steady state — 0.75–4.5MB GC/player-minute and the debug window drops to 1–3 min. | CPU negligible; buffer + GC real | One aggregate `tile/PropagationWave` record per flood call (medium breakdown, cellsReached, steps, bbox); per-cell record behind default-off `tile-verbose`. One test updates (TilePropagationTests.cs:371). Freebie: hoist the 3× per-turn `new List<int>` at :141/:222 to static scratch, matching the file's own `_frontier`/`_seen` hygiene. |
| 3c | `CausticSkinPart` records `SkinContactRejected` **before** the attacker-null gate (CausticSkinPart.cs:73 vs :82-85) — source-less environmental ticks (burning, poison) emit a stray rejection record per tick per frog. | Stream pollution, not GC | Move the Source null/self early-outs above the IsElemental check; ~5-line move, zero reflect-behavior change, both pinned diag-count tests still pass. |

---

## 4. VERIFIED FINE — suspects acquitted with corrected numbers

The verify pass existed to kill plausible-but-wrong findings. These were
all raised (several by me, pre-workflow) and refuted **as sized**, with
the mechanism confirmed real but under the bar. Recording the corrected
arithmetic so future audits don't re-litigate:

- **Grovelands reachability machinery** (double flood per
  PlaceSolidIfHarmless candidate, EnsureAllReachable's 200-iteration
  guard, per-flood bool[80,25]+Queue): the scary worst cases are
  geometrically unreachable — candidates that fail repeatedly fail cheap
  at the IsOpenGround gate before any flood; candidates that reach the
  floods almost always succeed and break the loop; EnsureAllReachable is
  bounded by placed.Count+1, not 200. Corrected: **typical 2–8 flood
  passes ≈ 1–5ms, unlucky tail ~10–40ms, once per zone ID ever** (zones
  cached). Not tens-to-hundreds of ms. Revisit only if a zone-gen
  stopwatch ever shows a Grovelands transition spike; then fix
  PlaceSolidIfHarmless's invariant `before` count and EnsureAllReachable
  together, or land the shared passability bitmap below.
- **ConnectivityBuilder's W4.1 `IsPassable` → `!BlocksMovement` change**
  multiplied its flood constant ~1.5–3× (entity/Parts scans per BFS
  probe). Real, gen-time-only, absorbed by the same bitmap if ever needed.
- **8–14 MycelialColumn lights per grove**: radius 3 → ~29 cells with
  ≤2-step LOS lines each; a full ring costs ~700–1,200 LOS checks ≈
  0.02–0.1ms per recompute, and recompute only runs on the full-dirty
  path (player's own move). The original 10–20k-check estimate was off
  20–50×. The glow-moth no-LightSourcePart rule (GrovelandsBestiaryTests)
  remains the right call — moving lights would hit every wander step.
- **DestructionSystem `damage/ObjectDamaged` per structural burn tick**:
  fuel-bounded transient, not a sustained rate — a whole 150–250-bank
  field burn emits ~800–1500 records TOTAL (~1–1.5MB once). Suppressing
  it would break the Observability mandate, pinned tests, and the exact
  diag trail the RouteDamage comment memorializes. Leave it.
- **Wholesale demotion of the 24 always-on diag channels**: refuted.
  Always-on is deliberate, documented design (diag_assert as debugging
  step 1); zero Record calls exist in any Update/LateUpdate path; typical
  play is ~36–60 records/turn. Only per-kind verbose splits (§1b, §3a,
  §3b) preserve the workflow.
- **`InventoryPart.GetCarriedWeight` per player move** (HashSet alloc +
  full inventory scan): ~2–10µs, ~1KB/s. The dirty-flag cache is
  **unsafe** — external mutations of Objects/EquippedItems (Body.cs:847/
  870, SaveSystem.cs:1399/1404, StackerPart count changes) bypass any
  invalidation hook, and a stale overburden read is a gameplay bug. Do
  not build it.
- **`BurnOffGasPart` re-splitting `DamageTriggerTypes` per damage event**:
  ~110B/event, ~2.2KB/turn-frame worst case — under the 5KB/frame budget.
  Fold in the cached-parse only if the file is touched anyway.
- **`UnderTheClothEffect.Protects` on the hostility path**: correctly
  gated (only consulted when GetFeelingCore ≤ −10), allocation-free,
  fine.
- **PopulationTable.GetBiomeTable fresh-allocating tables**: 1–2 calls
  per zone generation, <2KB. Fine.
- **TendrilFen recomputing its sine veins per row** (~3,200 Math.Sin
  where 152 suffice): gen-time, tens of µs. Hoist opportunistically.
- **AmbientMotes, TileState mark layer, LandmarkBuilder anchors, GroveLaw
  zone-id parsing, WorldClock, LiquidSlipSystem**: all verified gated,
  capped, or cold. Fine as shipped.
- **BiomeColorPatcher**: `ResolveCurrentBiome` runs every `Update` and
  calls `WorldMap.FromZoneID` → `zoneID.Split('.')` — a per-frame
  allocation in Update, which the perf rules ban categorically even
  though it is ~100B/frame. One-line fix (cache by zone-id reference
  equality); flagged for the next time the file is open.

---

## 5. The holistic/ops angle (session knowledge, not workflow output)

- **Four formation builders duplicate the reachability toolkit verbatim**
  (IsOpenGround / FloodFromWest / FullyReached / EnsureAllReachable /
  ClearFor-ClearVegetation across Spread/Beating/Sodden/Grovelands —
  four private copies confirmed by the verify pass). Extraction into a
  shared `FormationReachability` helper is a maintainability fix that
  also gives the bitmap optimization (should it ever be justified) a
  single landing site. The builders were grown one per phase; the
  duplication is now load-bearing enough that the next biome would copy
  it a fifth time.
- **Adding a biome touches ~6 parallel per-biome catalogs**: the biome's
  FormationBuilder + pool, PopulationTable tiers + lair guards,
  ContainerPlacementService pool, LandmarkBuilder stamps + ambients,
  BiomePalette case, OverworldZoneManager registration (plus
  TerrainRenderCoverage test rows). Each is individually clean; together
  they are a shotgun surgery pattern. A per-biome descriptor (even a
  static registry struct, not JSON) would collapse five switches — worth
  weighing before W5/W6 adds more biomes, not urgent before.
- **The verification loop itself**: the ~4-minute headless suite is the
  binding constraint on iteration speed (dominated by editor boot +
  import, not the ~30s test run). Nothing in this review changes it;
  noted so a future "optimize operations" pass starts at editor-boot
  amortization (keeping a warm editor, batching commits per launch —
  already the de facto practice) rather than at test-code speed.

---

## 6. Recommended sequence (EXECUTED 2026-08-21)

> Items 1-5 shipped as five commits (`348da151` TickEnd round gate,
> `1936de3a` merge-on-spawn + Refresh skip, `74ba7493` turn/tile/caustic
> channel splits, `6a848e20` gas/Dispersed verbose, `9fbfd3e0` shimmer
> skip). Item 6 remains deferred to the seam before the next
> biome-adding phase, per the section below. Outstanding honesty bounds:
> live gasbench field-fire matrix + UpdateAmbientAnimations re-measure,
> both next PlayMode session.

1. **§0 TickEnd per-actor seam** — correctness first: gas + crops to
   player-turn semantics with RED tests pinning population-independence.
2. **§1a merge-on-spawn** for BurnOffGas (with §1c ride-along), verified
   on the gasbench scenario.
3. **§1b + §3a + §3b diag channel splits** — one commit, one bug class
   (verbose-tier splits), test updates included; restores the ring
   buffer's ~227-turn design window.
4. **§2 shimmer skip-unchanged** — with before/after
   UpdateAmbientAnimations numbers and a PERF-FOUNDATION table update.
5. **§3c CausticSkin gate order** — trivial, fold into any of the above.
6. §5 consolidations — defer to the seam between W4 close-out and the
   next biome-adding phase.

Honesty bounds: all sizes above are static-analysis arithmetic against
read code, not profiler measurements — the house rule stands that each
fix confirms with the named recorder/bench before merge. The review is
bounded by the four lenses run; it did not fuzz, and it did not re-audit
pre-Wx substrate except where Wx content changed its inputs (shimmer,
lightmap, diag).
