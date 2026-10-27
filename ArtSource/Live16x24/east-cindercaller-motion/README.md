# DEFERRED — East Cindercaller motion source

Status, 2026-10-27: **preserved first-build source; not imported or released.**
The user reprioritized filling missing sprites across all biomes and areas over
further nuanced animation work. Production stopped after the first native render
and source verification. This packet is a recoverable handoff, not an active
animation task or evidence of improved in-game Cindercaller motion.

No new atlas was copied into `Assets`, no runtime implementation was made for
this packet, and no Unity test, import or game build was performed by its author.
The separate proposed Unity fixture is preserved outside `Assets`; see
[DeferredTests](../../../Tools/Live16x24/DeferredTests/README.md).

## Inputs and concept decisions

The approved identity remains
[`../cindercaller/source-cells.json`](../cindercaller/source-cells.json) and its
unchanged native PNG, Blender source and generator. Seven existing colors,
119-pixel idle mass, narrow slate cap, scorched rust rim, short tawny face,
bent hind leg and two asymmetric foreclaws are the baseline.

Actual pre-change encounter evidence is in
[`G2Action-20261027-032836-072`](../../../Docs/Verification/Live16x24/G2Action-20261027-032836-072):
`frame-02.png/.json` and `frame-05.png/.json` show Cindercaller4920 at67,20
casting at Magpie4884 at65,20 during tick1630, after the player's Lunge caused
3 damage. These are existing gameplay captures with the unchanged static
caster body, not captures of this packet's frames.

The parent-authored concept and exact prompt are retained unchanged:

- [Concept](../concepts/east-caster-motion-concept.png), SHA256
  `012d53e006d3348ba66c2f2bd7168dc5ddab0a808a0096f40fb3df83ab406135`.
- [Exact imagegen prompt](../concepts/east-caster-motion-prompt.txt), SHA256
  `e453742200dcecc1519e0338051fe5278f9219f9682709ffe6106f999c6e8ac8`.

Accepted: alternate foot/claw contacts, a lifted foreclaw for melee, a separate
forward-head spit gesture, and compact recoil while retaining the narrow cap.
Rejected: the concept's oversized mass, faux grid, wider cap, extra detail,
palette changes, replacement idle, weapon-like accessories and a spit pose that
flattens the entire body. The concept was visual guidance only; no raster was
traced, resized, cropped or repainted into the native outputs.

## Saved parts and proposed ownership

[`build_east_cindercaller_motion.py`](../../../Tools/Live16x24/build_east_cindercaller_motion.py)
deep-copies the original named geometry and authors integer-cell derivatives.
It imports pure helpers only; it never invokes the old generator's
Assets-writing main routine. Output stays in this packet even when
`--defer-unity` is omitted.

| Atlas index | Proposed state | Authored operation |
| --- | --- | --- |
|0|WalkA|Rearward hind contact, near claw under the body, far foreclaw forward.|
|1|WalkB|Hind contact draws inward, near claw plants forward, far claw folds back.|
|2|Attack|One foreclaw lifts beside the unchanged face; other supports brace.|
|3|Hurt|Whole cap/mantle/face translates one cell back and down, with rebuilt braced feet.|
|4|SpitCast|Original limbs remain planted; face advances one cell forward/up with one connecting neck cell.|

The proposed resource is `Sprites/Live16x24/east-cindercaller-motion`,80×24,
five16×24 frames in one row,16PPU, bottom actor pivot(0.5,0). This name is a
future integration contract; the resource does not exist in `Assets` from this
work. Idle/recovery would borrow `Sprites/Live16x24/cindercaller` unchanged.
The packet metadata describes a centered single-sprite import; runtime slicing
would supply the bottom actor pivot. Canonical facing is East, with West
mirroring left to runtime integration.

Owner is exact `MarlbackCindercaller`, subject to the existing native admission
and visibility rules. This packet adds no global admission, equipment state,
spell fuel, gameplay action or event callback. FireMoss is carried inventory,
not Ember Spit ammunition. Existing spell FX/audio remain independent and
unchanged. There is no full/empty body variant or permanent flame in the atlas.

`native.blend` contains named editable anatomy collections and the native plus
two contact scenes. `source-cells.json` retains named cells and exact palette.
Both contacts render geometry directly:152×40 at1× and1216×320 at8×. Their six
columns are borrowed unchanged idle, WalkA, WalkB, Attack, Hurt and SpitCast.
The quiet032F23 backdrop is contact-only; the native atlas has transparency.

## First-build evidence and limits

`source-red.txt` records the genuine initial missing-output failure, before the
new generator produced its atlas, Blender source, metadata or contacts.
`build-first.log` records the one Blender generation. `source-green-first.txt`
and `verification.json` record the subsequent passing decode-only source check:

- Five distinct non-idle poses, each using the original seven colors; binary
  alpha, foot baseline22 and fully transparent row23.
- Exact source-cell/native-pixel agreement and exact direct1×/8× contact pixels.
- Frozen non-limb anatomy comparisons, including coherent hurt/face transforms.
- Eight deliberately corrupted in-memory source variants rejected: duplicate
  walk, changed cap, opaque bottom row, stale hurt crown, modified spit hand,
  invented weapon part, changed hue and removed mantle cell. These are source
  integrity counterchecks, not tests of gameplay equipment or ammunition.
- All232 entries in `preserved-inputs.json` unchanged, including approved art,
  source helpers, original identity, existing spell FX/audio and exact references.
- Source import metadata contract checked semantically:69 mapping keys,
  51 scalar values,10 lists and11 explicit empty containers.

The author inspected the original native and8× contacts. Identity remains
consistent and the gestures differ, but their normal-speed readability has
not been accepted in game. Root visual approval for integration is outstanding.

| Saved output | SHA256 |
| --- | --- |
|`east-cindercaller-motion.png`|`27a3a1041af48064472aebef8e40d24169d584bc807a6b67ef3aa8dd8ed0132b`|
|Decoded RGBA pixels|`52b79c88590a6914f0ab77e921314d289de822d972b045dc77d17fa09577e0d1`|
|`east-cindercaller-motion.png.meta`|`4e6f7b054428478eac10964f29211b62096ef8928d4d76ee363e3760173484a4`|
|`source-cells.json`|`10f26a9884f8ae5a8c9196ff09a816863cd12408980d0b52afe34772452cd80b`|
|`native.blend`|`fc35f12e72123fbf3edbd1ecc8d0ddacd6b7c9bc45258428653b594bd65cf953`|

Still outstanding: second-build determinism, reopening and independently
inspecting saved Blender geometry, root visual approval, actual resource import,
native runtime RED→GREEN and regression tests, event/owner/fog correctness,
ordinary gameplay motion captures, performance observations and packaged-game
acceptance. Source GREEN proves none of those. No second build, art revision,
import or test run was performed during the preservation handoff.

Resume only if this deferred task becomes a priority again. Re-read the current
[graphics priorities](../../../Docs/GRAPHICS-COMPLETION-PLAN.md) and
[East motion plan](../../../Docs/GRAPHICS-EAST-CASTER-MOTION-PLAN.md), then review
the unchanged packet and outstanding gates before deciding whether to integrate.
The current task is worldwide sprite coverage, not additional animation polish.
