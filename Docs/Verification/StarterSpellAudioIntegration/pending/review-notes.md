# Starter spell audio tests — author review

Current pending and landed tests: 32 integration/unit cases plus 60 dedicated adversarial cases. The parent centrally controls Unity runs and RED/GREEN classification; this file does not claim a run result.

## Verified API and fixture corrections

- Read CLAUDE.md, ADVERSARIAL_TESTING.md, the existing Ember player/tests, SpellFxSequence and the actual coordinator lifecycle.
- Exact captured successful effect is `FrozenEffect`; Calm is `Pacified`.
- Default copied Jet endpoint is two cells from source; Hands is one cell. Rain is within its actual Chebyshev radius three.
- Duplicate and invalid path entries do not select a new endpoint. Displacement needs the copied original and destination visible.
- Rain requires an actual copied crop target in copied affected geometry. A source cell or water residue cannot stand in for a watered crop.
- Longer Calm finishes after 2.35 source seconds; at speed .25 its wall-clock audio tail can exceed five seconds. The tests use the new independent sixteen-second audio safety deadline, retaining five seconds as an invalid contact input.
- Headroom assertions compare each player's solo gain to the same gain divided by the combined voice count. They do not assume the new catalog safety factor is one.

## Observable proof and limits

Tests check actual assigned AudioClip prefixes/suffixes, suffix names, asset sample metadata, exact once-only counters, both pool sizes, deterministic matching variants, gain/pitch, timing, lifetime, cancellation and managed allocation. Conditional layers are distinguished from neutral layers using both recorded counts and actual assigned source clips.

EditMode does not play the DSP device. These checks do not establish subjective sound quality, speaker loudness, audible phase alignment under real frame jitter, or native scene feel. The parent owns imported-audio sample fidelity and a real native audio capture gate.

## Runtime cold-eye observations sent to parent

- Clock, cancellation, disposal and shared gain wiring mirror Ember in the coordinator. No independent audio queue consumes SpellFxBus.
- Parent clarified Rain deliberately allows a visible caster to reserve a silent voice for an actually copied but hidden crop. There are no prefix sounds. Crop visibility at contact decides the suffix once; paired cases now pin a crop revealed before contact versus still hidden. This is an explicit design interpretation, not an unresolved finding.
- Production `LoadCatalog` accepts a conditional layer with a prefix, while Play starts every non-null prefix without its outcome gate. The actual exporter correctly omits those prefixes. Defensive catalog rejection would pin the causal requirement against malformed future content rather than relying solely on current data.

- New hypothesis cases measure actual warmed Calm/Rime/Surge contact updates, not only the pre-contact steady loop. A03 confirmed all three pass: no contact-allocation bug was established; retain these as pinned-correct regressions. The proposed boxing issue must not be reported as a confirmed bug.

- A03: 142 combined cases, 138 passed and four real geometry failures, zero compile errors (parent report). The fixture now explicitly marks selected targets through the appended `isDirectTarget` constructor argument; a paired exact-cell ambient/direct Jet test prevents location alone from inventing the wet-hit suffix. The constructor implementation and geometry fixes remain parent-owned.

## Final read-only lifecycle review after copied provenance fields landed

Constructor/SetZone/Update/CancelNative/ClearAll/native surface transitions/Dispose consistently handle Starter alongside Ember. The coordinator supplies both selected playback clocks, refreshes their combined voice count after acceptance/update/cancellation, and excludes audio tails from turn-blocking handles. No duplicate consumer or new lifecycle defect was found in this pass.

The current loader accepts legitimate prefix-only layers, still rejects wholly empty layers, and rejects a non-always layer with a prefix. Exact variant1 prefix counts are pinned (Hands2/Jet2/Surge2/Rime1/Calm2/Rain0); Ground Surge successful suffix counts are explicitly tested as3/4/3 across variants1/2/3. The tests remain evidence of scripted assignment and lifetime, not a claim of audible device playback or a completed native acceptance run.

- A04 central combined run passed269/269 with zero compile errors before the final two moved-Surge direct/ambient cases were added. The final two preserve exact path position and visible movement while flipping only copied direct-selection provenance; expected variant1 contact suffix counts are3 versus2. They are awaiting the final full suite, not claimed as already run. Final owned inventory:92cases (32main+60adversarial).
