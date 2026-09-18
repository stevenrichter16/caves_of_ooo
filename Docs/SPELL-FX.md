# Spell presentation

The 49 current castable magic skills resolve once and publish a copied presentation
sequence. Their targeting, damage, resistance, cooldown, ink and resonance rules
remain in the gameplay code. Visuals never execute a spell or reconstruct its targets.
The verified registration breakdown is 12 Pyromancy, 8 Cryomancy, 6 Galvanism,
7 Hydromancy, 1 Corrosion, 4 Spellcraft and 11 Rites. The earlier count of eight
Hydromancy abilities was stale. See [the definition matrix](Verification/SpellFx/COVERAGE.md).

## Playback and compatibility

`SpellSkillPart` wraps the actual command execution, including direct scenario
calls. Observation hooks record the cells visited by the gameplay algorithms and
the outcomes they resolve. Refusals discard the observation scope. Target positions
are captured before damage, destruction and movement; the published sequence owns
copies of its path, affected cells and target results. Cosmetic seeds use a separate
serial and never draw from gameplay RNG. Passive retorts use nonblocking scopes and
coalesce repeated accents from the same caster within a pending batch.
Scorch, Frost, Shock and Acid Retorts have sprite definitions. Charsplit,
Brittle Strike, Ground Strike and Etch also publish resolved accents, using the
explicit ASCII fallback. Coalescing lasts for one pending batch; it is not a
cross-turn rate limiter.

`WorldFxCoordinator` owns both the compatibility ASCII queue and the spell sequence
queue. The world renderer invokes it once per frame. Sprite and ASCII backends accept
requests directly; they do not compete to drain a queue. A standalone ASCII adapter
is retained for existing callers and harnesses. Existing damage numbers and aura
emit APIs continue to work.

Sprite playback schedules Cast, Charge, Travel, Impact and Aftermath on one clock.
Independent affected cells and targets play in parallel. The JSON catalog supplies
art, family, timings and direction; it supplies no combat rules. Missing definitions
or invalid art use a restrained ASCII representation of the recorded cells. The
ordinary sprite toggle also selects the spell backend.

Queued work is included in the input-wait predicate. Playback handles terminate on
completion, cancellation or a five-second wall-clock timeout. Input has an independent
timeout in case a renderer stops ticking. Hiding the world, disabling rendering,
switching sprite/FX modes, loading a world and changing zones cancel transient work.
Status auras rebuild from the current entities; terrain-state marks retain their
existing state-driven layer. No animation objects or clocks enter save data.

## Art and controls

`Assets/Resources/Content/Data/SpellVisuals.json` maps actual skill class IDs to visual
definitions. `ArtTools/coo_spell_fx.py` generates original, deterministic pixel art:
seven school/material palettes, six-frame 16×16 details and 32×32 impacts at 16 PPU.
It also regenerates the catalog. Shapes distinguish embers, ice shards, branching
electricity, water streams, acid bubbles, binding threads and ruled ink inscriptions.
Rite expenditure indicators use the marks actually consumed; cold rites have no
consumed-mark indicators. Domestic definitions have short, restrained timings.
Where the gameplay permits it, a zero-mark rite still spends ink and resolves its
small base payoff. Scalding Veil on a dry caster remains a refusal with no expenditure.

Filtering is Point, alpha is binary, mipmaps and texture compression are disabled.
Large impacts are sliced at world-cell boundaries so their overhang respects FOV.
ASCII glyphs also require explored and currently visible cells. Spell sprites use
the world render layer, below fullscreen UI, and transient sprite objects are pooled.
No new URP lights, bloom, shaders or external visual assets are required.

Press **F10** to open the settings panel without spending a turn. **Done** or F10
closes it and saves local presentation preferences.

| Control | Behavior |
|---|---|
| Off | Suppresses new spell sequences; retains terrain/status indicators and damage readouts. |
| Reduced | Omits charge and selected trails/echoes/aftermath, uses smaller impacts, and limits live spell sprites to 48. |
| Full | Enables the complete compositions with at most 192 live spell sprites. |
| Animation speed | Advances spell playback at 0.25×–4×; simulation still resolves once. |
| Flash | Adjusts the initial sprite spell impact brightness; it is not a fullscreen flash. |
| Shake | Scales camera accents independently; defaults to zero, with offsets snapped to pixels. |
| Backslash | Existing environment/actor sprite toggle also switches spell playback between sprite and ASCII. |

Seven humanoid cast sheets provide four-facing gather, binding/book, release and
settle poses. Other actors use the existing Attack sheet as a fallback; missing
actor art continues to use the established static/ASCII path. Cast art regeneration
and frame contracts are documented in [the art guide](SPELL-FX-ART.md).

## Reproducing verification

**Caves Of Ooo → Scenarios → Combat Stress → Spell FX Showcase** stages 66 actual
casts or intentional refusals: all 49 registered spells, 11 zero-mark rite cases,
and six resistance, obstruction, refusal, lethal-impact, movement and hidden-cast
variants. The eight requested representatives run first. The default interval is
2.2 seconds, and the controller also waits for active blocking presentation.
The visible **Pause**, **Replay**, and **Next** buttons support inspection.
`PrepareCase(index)` stages without casting; `ExecutePrepared()` resolves that stage
at most once. `CurrentCaseIndex`, `CurrentCaseLabel` and `CurrentStage` are available
to editor automation.

The arena preserves real StoneFloor terrain, clears prior material effects between
cases, and recomputes normal FOV. Fresh caster/target entities prevent buffs,
cooldowns and consumed marks from leaking between demonstrations. Running the
scenario clears its arena and existing creatures in the current play session; it
belongs in a disposable editor test session. The timed showcase does not advance
the normal turn loop.

Relevant tests cover resolved spell snapshots, rite expenditure/refusal, copied
geometry, damage and resistance, passive triggers, catalog coverage, sprite pooling,
FOV, fallback, queue ownership, input waiting, cancellation, timeout and save/load.
The verification record in `Docs/Verification/SpellFx/` records only checks actually
performed, including editor captures and measured budgets.
See the [verification report](Verification/SpellFx/REPORT.md) for the final results,
recorded animation, coverage map, and limits of the measurements.

The [capture body](Verification/SpellFx/capture-body.cs.txt) stages selected cases
while Play mode is paused, then advances presentation in 1/60-second steps. It saves
512×256 camera images at 0, 0.10, 0.20, 0.30, 0.45 and 0.65 seconds. Its CSV timings
measure `WorldFxCoordinator.Update` only, excluding actor reflection, camera
rendering, readback, PNG encoding and file output. They are not GPU frame times or
evidence of zero garbage collection. Use the recorded frame manifest and native
Unity test XML for the exact cases, counts and results actually captured.
The separate [live capture body](Verification/SpellFx/capture-live-body.cs.txt)
requires Unity Play mode running with showcase autoplay paused. It records normal
Update/LateUpdate progression and frame timestamps, providing a separate motion
check from deterministic presentation stepping.

## Scope and limits

The library uses shared school atlases and spell-specific composition/timing rather
than a separate bespoke flipbook for every spell. Seven humanoid visual definitions
have authored casting art; fauna retain their existing Attack fallback. Persistent
auras and terrain hazards continue through their existing state-driven renderers.
No additional dynamic light budget is needed because this pass adds no lights.

FX settings govern this spell presentation pass. Existing actor hurt reactions,
idle/walk animation, and older environmental animation keep their existing behavior;
the Flash slider does not replace their individual animation policies. Sprite and
scheduled-record ceilings are safeguards, not a claim that every combat load has
been profiled. A five-second timeout may shorten unusually long playback at very
slow animation settings so input always recovers.

## Architecture reference

The permitted local Qud decompilation was consulted in
`/Users/steven/qud-decompiled/Assembly-CSharp.decompiled.cs` and
`/Users/steven/qud-decompiled-project`. Its `CombatJuice` pooling and configure callbacks,
`CombatJuiceManager` active/waiting/queued entries, delayed follow-ups and synchronized
child durations informed the lifecycle design. The queue-aware waiting predicate,
hard timeout and FOV clipping here are project-specific safeguards. No Qud art was
copied. Canon priority follows `Lore/README.md`, `10_Bible.md`, `11_SecondSpine.md`
and `Docs/VISUAL-IDENTITY-BIBLE.md`; legacy skill IDs remain compatibility identifiers.
