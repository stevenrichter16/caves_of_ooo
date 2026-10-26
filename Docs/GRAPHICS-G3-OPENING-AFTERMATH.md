# Opening combat aftermath identity

Status: native verification and bounded ordinary Magpie acceptance passed, 2026-10-26.
This is one bounded omission selected by a six-zone scene review; whole-opening
and regional completeness remain pending.

## Named defect and plan

Current package Nella screenshots showed a red `%` among otherwise native
creatures and crops. Root continued the earned seed65 session in isolated Unity,
then observed a lower pile member: `CreatureCorpse`9225 at(51,12), source
`Magpie`9182, killer `MarlbackScrabbler`9183. Ordinary movement from(22,8) to
(51,11), C/Down/X identified its real pile, including a cudgel and other stolen
items. No animal was spawned, killed by audit code or granted to the player.
The earlier death is already part of this earned checkpoint. Subsequent exact
pile inspection corrected the initial visual attribution: the red `%` actually
belongs to RawMeat9224, RenderLayer5, above the corpse at layer3. The corpse had
a separate missing-native identity; this pass fills that outcome while leaving
RawMeat and all existing pile priorities unchanged.

The initial extra Escape opened Pause; the first16Right presses therefore did
not move. Tab closed Pause; Right16,Right13,Down3 reached(51,11), tick2610,
HP40/40,42drams. C and Down were separated after the first immediate direction
was not processed. Return closed the pile notice; F5 saved the checkpoint.
[Before scene](Verification/Live16x24/aftermath-02-before-near.png) and its JSON
record the actual current state. Current coverage is not a fresh kill claim.

Plan: retain accepted living art and existing pile selection, and supply three
native identities: magpie remains, viper remains, and the viper's existing venom
gland. Verify exact-source world/reader admission, actual pickup/drop and save
return; verify other outcomes with native tests until ordinary encounters are
observed. Do not add butchery to magpies or alter chance, damage, AI or drops.

## Pre-implementation sweep

| Assumption | Verified source/correction |
| --- | --- |
| Missing animals | Living Magpie and Viper already use north-works frames5/6. Only their generic CreatureCorpse outcome is missing. |
| Corpses are Item-tagged | CreatureCorpse inherits PhysicalObject and has Corpse, not Item. |
| Death source must remain loaded | CorpsePart stores SourceBlueprint/SourceID; the source actor is removed. Saved metadata is identity, not cryptographic provenance. |
| Every corpse is butcherable | Magpie has no Harvestable. Viper has one 75% VenomGland attempt. |
| Failed butchery leaves an empty corpse | Both successful and zero-yield rolls consume the source. No empty-corpse frame is needed. |
| Pile art must show every member | Existing highest-owner selection remains authoritative; explicit readers use their own subject. |

Qud direction: the established legible symbol/material identity brief, without
claiming Qud code parity or copying its artwork. Existing approved native art is
the style reference; no new external asset or license is introduced.

## Production loop and extraction

The existing playable Nella geometry, paths and owner placement are reused;
there is no layout change. Actual1280×800 captures precede the built-in imagegen
[concept](../ArtSource/Live16x24/concepts/opening-aftermath/concept.png) and
[exact prompt](../ArtSource/Live16x24/concepts/opening-aftermath/prompt.txt).
The concept is a reference board, not gameplay evidence or a production raster.

Extracted parts: folded wing, cream breast edge, slack tail and head for the
magpie; flattened head, loose open S and underside edge for the viper; one sac,
tapered duct and membrane highlight for the gland. The generated board's alert
bird eye, raised snake head and bright speckled gland are rejected. Native art
uses lower inert poses, no eye, four/five-color broad clusters and no glow.
Each owner occupies one16×24 frame, baseline23, centered pivot,16PPU. No blood
pool, guaranteed product or animation is implied. Existing living frames and
Marlback remains are byte-preserved.

`Tools/Live16x24/build_opening_aftermath.py` authors original flat mesh quads in
Blender, with named subparts, source-cell JSON and editable `.blend`. It reads no
raster and performs no image downsampling. The48×24 atlas is imported unchanged,
Point/Clamp, no mipmaps/compression. All three frames share one texture. Native
verification compares every source and contact pixel, binary alpha, palette,
bounds and preserved living art. This is source validation, not gameplay proof.

## Runtime contract and verification

Optional `FlatPixelAftermathCatalog` uses exact blueprint/source plus owned
ordinary portable Render/Physics. Corpse metadata does not depend on a living
source reference. Optional Harvestable must be owned and unconsumed. VenomGland
requires its owned positive Toxic reagent; potency is not hard-coded. Explicit
reskins, malformed shapes, exhausted stacks and conflicting living/terrain/crop
identities fall back. Existing Greybox world graph/fog/custody and exact-reader
checks remain the authority. Cached frames are disposed without destroying the
borrowed resource. Three cached tiles share one48×24 atlas; no event bus or
new gameplay system is added. The mixed editor profile below is observation,
not a claim of an FPS improvement or physical-device acceptance.

Native RED receipt: [aftermath-red.json](Verification/Live16x24/aftermath-red.json).
31cases completed before production existed. The retained MCP failure list is
capped at25: it demonstrates missing-adapter/native-route failures but does not
retain an exact failed/passed total. GREEN passed236/236,0failed/0skipped across
seven fixtures (51new tests plus185existing): exact identities, real corpse
creation/source removal and serialization, success/failure finite Viper harvest,
pickup/drop/reader custody, graph/fog/replacement/reskin/shape/stack rejection,
missing-atlas fallback and disposal. See [native receipt](Verification/Live16x24/aftermath-green-attempt1.json).

## Scope and honesty bounds

Script-observable: owner identity, selected tile/atlas, custody, finite harvest,
serialized metadata, source pixels and passed native tests. Visual/feel:
ordinary world/reader clarity requires actual Unity captures and human-style
inspection, not test assertions. Physical Deck input/performance and all world
regions remain unverified. A three-frame outcome addition is not graphics
completion. Any package built before this source still lacks this addition.

## Ordinary integration evidence and self-review

The earned before checkpoint is preserved at
`Verification/Live16x24/Fixtures/aftermath-earned-garden65/` with hashes.
Root resumed it through ordinary C in a separate NativeSaveIsolation run.
Capture03 was taken before C and is correctly named `continue-pending`; it is
not an after-comparison. No state, health, loot or death was fabricated.

| Actual action | Observed result | Evidence |
| --- | --- | --- |
| Continue, inspect pile51,12 | TopRawMeat9224 remains glyph37; corpse9225 separately resolves nativeframe0 | `aftermath-pile-observation.json` |
| Down,G,F1 | Existing pickup-info notice, with no portrait; not counted as exact Examine | Recorded here; no claim of changed picker UI |
| Return,Return | Pick up only corpse9225; six other pile items remain | Actual log and capture04 inventory |
| Escape,Down,I,Tab,arrows | At51,13,40HP,tick2640,36lb; selected native corpse preview | [04](Verification/Live16x24/aftermath-04-carried-preview.png) |
| F1 | Full Examine uses identical native frame, corpse name,Flesh,10lb; no turn | [05](Verification/Live16x24/aftermath-05-carried-examine.png) |
| Escape,D,Escape,Right,F5 | Drop at51,13 and step to52,13; native body exposed left of player | [06](Verification/Live16x24/aftermath-06-dropped-world.png) |
| F6 | Same corpseID/source/properties/position and nativeframe0; tick2650,HP35,42drams retained | [07](Verification/Live16x24/aftermath-07-reloaded-world.png),`aftermath-exposed-observation.json` |

The nearby Marlback attacked normally during the step after dropping; HP40→35
includes3damage plus2bleeding. This is retained in both F5/F6 state records; no
health restoration or forced safe state. A second F6 plus inventory reader was
observed during the bounded editor profile. No ordinary Viper kill or butchery,
border-return, standalone aftermath run or physical Deck is claimed.

Independent source/art/test and actual04/05 reviews found no material blocker.
- 🔵 Corrected visual attribution: RawMeat hides a lower corpse legitimately.
- 🧪 Viper remains/gland have native tests; ordinary encounter capture is pending.
- ⚪ Pickup-info stays an existing text-only notice. Reuse native portraits if
  that broader picker UI is polished later; this pass does not rewrite it.
- ⚪ Whole-opening graphics and unidentified loot remain incomplete.

Can verify from script: selected current owner, native atlas route, persistence,
consumption/custody counter-checks, exact art pixels and test results. Actual
captures support human-style readability review for the Magpie path only.
Cannot infer visual acceptance for unseen Viper outcomes, Linux/Deck execution
or whole-opening completion from these tests or screenshots.

Independent world review accepted06/07: the body is exposed immediately left
of the player and stays identical across reload. The final dropped checkpoint
was automatically removed by NativeSaveIsolation on leaving Play before a
separate fixture copy; only the earned pre-art checkpoint is retained. This
does not affect the captured F5/F6 evidence and is not a normal-save deletion.

[Profile](Verification/Live16x24/aftermath-profile.json):104.54s mixed editor
observation,57229frames,32768retained samples (buffer wrapped). Retained samples
are largely item-reader idle:24batches/draws, renderer LateUpdate p99=23209ns.
No controlled baseline or standalone/Deck speed claim is justified.

Owned Play stopped; normal SampleScene, save root, presentation preference and
borrowed view restored.776normal saves,374preexisting art/source files and both
foreign screenshots match their protection hashes.29existing generated PNG
metas were byte-backed-up and restored only after proving whitespace-only
diffs. No storage reclamation; about426.6GiB free.

Next selected scene defect: exact North sole-water rectangles still lack the
existing bank treatment. Capture a current matched baseline, reinterpret it
with imagegen, reuse the existing native bank vocabulary under exact graph and
material guards, then native and ordinary crossing/load review. Preserve dry
North court/field and all mixed/frozen/hidden-neighbor controls. Current
Mac/Linux packages2a8e71044 predate aftermath; package parity remains pending.

Files: new aftermath catalog, two native test fixtures,3-frame native atlas and
Unity metadata, editable Blender/source-cells/manifest/verification, generation
and import scripts, concept/prompt, actualcaptures and boundedrecords. Only the
existing Greybox catalog gains lazy world/reader routing and owned disposal.
