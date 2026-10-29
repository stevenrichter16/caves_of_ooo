# Beating tar-seep native presentation

**Status (2026-10-29 02:00 UTC):** bounded Editor-verified. The live Beating RuinField19.12 tar seeps at 33,6–7 now draw an original native flat tar film over their true sand. They keep the independent oil hazard mark and show the native portrait in the world and underfoot readers. The final art was seen in actual play, and an F5 save plus a fresh Continue were compared. No mechanics, placement, RNG, NPC, shadow or population change. Parent `0ec152a05`. Earned after-checkpoint: `Fixtures/beating-tar-seep199`.

Ownership: the Codex `overnight-clearing-art-pipeline` automation started this slice (plan, first art, nine RED tests). The user moved Unity/Git ownership to Claude at 00:48 UTC, and the automation was paused then: its `automation.toml` status is PAUSED, with the original backed up in the session scratchpad. Resume it only by explicit choice.

## Named defects and result

| Defect | Before (packaged captures 10/11) | Now |
|---|---|---|
| TAR1: world and reader lacked material identity | `~` glyph body, `~~` reader portrait | Native two-state tar film; native portrait in world and underfoot readers |
| TAR2: black rectangular backing interrupted the sand | Black cell under the glyph | True Sand/SaltCrust/SandstoneFloor/RoadStone substrate kept beneath one eligible seep |

The oil hazard mark stays on its own layer at both stocks. Zero stock still projects permanent oil, so frame 1 is a slightly duller skin, never dry or safe.

## Runtime contract (`FlatPixelBeatingMaterialsCatalog`)

- **Optional atlas.** `Sprites/Live16x24/tar-seep` (32×24, two frames) is an independent optional atlas. Missing or malformed tar leaves every other material, the oil mark and the fallback glyph untouched. The older four-argument constructor never loads it.
- **Admission.** `TarFrame` admits only the canonical TarSeep:
  - presentation: `~`, `&K`, layer 1;
  - tags and physics: Terrain, not solid, no destructible part;
  - state: Tar material, neutral temperature, no status effects;
  - one owned oil pool with stock ≥ 0, and one owned oil source with no heat, cold, charge or residue;
  - tile: that oil is the cell's sole live state, with no second coating, residue, cloud or energy.

  Wells now count as foreign meaning for every Beating owner.
- **Ground.** In `TryResolveGround`, exactly one eligible seep may keep its real substrate under its live oil. Any other non-empty state still refuses, as do overlapping seeps and hidden seeps.
- **Underfoot reader.** In `TryGetSubjectSprite`, the exact placed seep can be read beneath the player; world selection stays with the selector.

## Art (`ArtSource/Live16x24/tar-seep`, `Tools/Live16x24/build_tar_seep.py`)

The art is original integer-cell Blender geometry; the imagegen concept is reference only. Three corrections were made before publication. The comparison strip is `tar-seep-08-art-iterations-offline.png`.

1. **Dome draft rejected.** It was a centred 14×13 mass with an upper-left specular spot, which read as a boulder and implied collision. The approved Beating brine uses flat fills with short horizontal ripples instead.
2. **Heart outline rejected in actual play.** Paired notches at top and bottom made the two stacked seeps read as hearts (`tar-seep-02-beside-intermediate-art.png`).
3. **Oval rejected by independent review.** A near-round 13-row, full-width mass still read like the approved Rock frames at a larger size. It also merged with its neighbour and carried a bright top sheen.

The final art is a low film on the ground band:
- 9 rows tall, 1–2 px clear of both side edges and broader at the base;
- dim horizontal sheen dashes, with no bright highlight;
- frame 1 adds a duller skin.

`--verify` and `--inspect-blend` pass: 98 opaque pixels per frame, 6 changed pixels between states, binary alpha, contiguous mask, protected inputs unchanged.

## Tests

The nine TarSeep cases in `FlatPixelBeatingMaterialsTests` were saved before the runtime by the previous owner. RED: `tar-seep-red.xml`, 0/9 (CLI, 00:25 UTC).

| Run | Result |
|---|---|
| Beating class, CLI batch (`tar-seep-green-cli.xml`) | 110/111; the one failure is an unrelated SetUp log from the MCP bridge while its server was down |
| All `FlatPixel*` rendering fixtures, Editor | 3,689 passed, 0 failed, 6 existing allocation probes inconclusive |
| Beating class, Editor, final art and final test | 111/111 |
| Mutation: `!NoEffects(owner)` removed | `TarSeepRejectsCustomForeignCarriedAndChangedMeaningOwners` fails, as intended |

The full 34,220-case suite was not completed. In the open Editor it ran at about 130 cases per minute and was cancelled at 1,412.

**Test corrections:**
- **False premise.** The "charge-effect" case asserted that `ElectrifiedEffect` can be force-applied to tar, but `ObjectStatusMatrix` refuses Electrified on non-conductors. It was replaced by a Burning effect, which the matrix admits on flammable tar.
- **Vacuous check.** Independent review found that Burning and Frozen are already refused by tint and temperature, so the "no status effects" rule was never independently tested. An `InertTestEffect` case (no tint, no temperature change, no matrix row) now pins it, and the mutation run above proves it.

## Actual play (Editor, isolated saves, real keys)

Immutable `Fixtures/beating-ruin-composition196` was copied into a fresh isolated launcher root and continued with a real C key: seed 64, Overworld.19.12.0, player at 66,6, HP 40, turn 1,540. Real arrow keys walked 66,6 → 60,12 → 40,12 → 34,7. F12 debug invincibility was declared for this walk because a skeletal sentry patrols the hut exit and hit for 24 HP in two earlier attempts. It absorbed every hit (0 damage) and was switched off before saving.

| Capture | Shows |
|---|---|
| `tar-seep-09-final-beside.png`, `-10-final-readable-crop5x.png` | Both seeps as separate low films on sand; the purple oil wave mark sits clearly above each film |
| `tar-seep-11-final-reader.png` | Look → examine: final native portrait; oil 40% slip text unchanged |
| `tar-seep-04-underfoot-oily.png` | Stepping onto 33,7: player drenched in oil, status "oily", underfoot menu names the seep |
| `tar-seep-12-final-underfoot-reader.png` | Underfoot reader shows the same final portrait |
| `tar-seep-13-final-saved.png`, `-14-final-fresh-continue.png` | F5 at 33,7, turn 1,990, HP 40; fresh Continue restores the same position, turn and HP |

A real step off the seep slipped in place on the oil, so a turn passed and the player stayed at 33,7. That is the existing 40% hazard.

**Save and Continue:** the world crop `(0,40,940,640)` differs by 253 of 564,000 pixels. The differences sit only on actors (`tar-seep-15-saved-vs-continue-diff-zoom.png`):
- the sentry's Z is red from a combat flash before saving and olive after loading;
- a few player-sprite pixels differ, cause unproven;
- two single pixels elsewhere differ.

The tar and ground cells are identical. Captures are the native 1× 1280×800 Game view. The readable-zoom review used a 5× enlargement of the same frame, not a 2× camera.

## Self-review (Methodology Template §5)

- 🟡 Fixed: dome silhouette implied collision.
- 🟡 Fixed: heart outline in actual play.
- 🟡 Fixed: oval still read as a boulder and merged with its neighbour (independent review).
- 🟡 Fixed: impossible RED precondition (Electrified on tar).
- 🟡 Fixed: vacuous "no effects" check (independent review); now pinned and mutation-proven.
- 🔵 Unity whitespace-only `.meta` rewrites from tonight's refreshes (the same ten files twice) were backed up and restored to their committed bytes. The 49 foreign paths are unchanged.
- 🔵 Review notes kept open:
  - The "cloud" case passes on the veil-mist mark, so it does not show the oil mark surviving.
  - DryBrush or Bones together with a seep can resolve a clean floor through their state-blind foreground check; the oil mark still shows.
  - The two stock states differ by only 6 px.
- 🧪 No 2× camera capture; save/Continue is not pixel-exact on actors.

## Limits and next steps

- No packaged build includes this change; current packages remain `aa92e2746`. Batch it with the next useful delivery.
- Next: **GroveSeep** wet backing. Its tests deliberately forbid dry grass under the seep (`SubmittedSeepKeepsItsActualWetOrHeatOverlayAndNoDryGrass`, `FenWater_PhysicalSeepSourcesKeepTheirOriginalWetBodyAndWave`), and `Zone.ProjectPool` writes unowned permanent water. The fix is to let one eligible seep sit on the approved Fen water bank tile, not grass, with mixed-liquid and heat guards. Those two pinned tests must be revised deliberately.
- Then the largest remaining whole-scene glyph gaps across biomes. Population and shadow work stay tracked separately, and no area is accepted as polished.
