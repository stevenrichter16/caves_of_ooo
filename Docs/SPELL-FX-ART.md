# Spell FX art and playback

Original Caves of Ooo pixel assets implement the material language of `Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, and `Docs/VISUAL-IDENTITY-BIBLE.md`. `Lore/History/09_Magic.md` supplies the gesture/material/expenditure presentation vocabulary. Runtime spell IDs and mechanics remain the authority for coverage and geometry.

`ArtTools/coo_spell_fx.py` regenerates 14 spell sheets and `SpellVisuals.json`. The catalog contains all 49 registered castable magic classes and four reactive retorts. Unregistered spells and unavailable art use explicit ASCII fallback in the coordinator. The generator owns the spell catalog: make lasting generated timing/family changes there before regenerating.

| School | Silhouette and motion |
|---|---|
| Pyromancy | Irregular rising flame teeth, embers, soot |
| Cryomancy | Long crystalline head, contracting shards, angular frost |
| Galvanism | Segmented bolt and branches, ground charge pulses |
| Hydromancy | Coherent droplet head, streams, broad shallow splashes |
| Corrosion | Separated droplets, hollow bubbles, etched residue |
| Spellcraft | Crossed threads, knots, geometric ward boundary |
| Rites | Open book, ruled inscription, consumed-mark crossing, resonance echoes |

Detail sheets are 96×144: six columns, nine rows of 16×16 frames. Rows are Cast, Charge, Head, Trail, Beam, Wave, Overlay, Sigil, Mark. Impact sheets are 192×64: six columns, two rows of 32×32 frames, one for impact and one for resisted outcomes. All artwork is opaque-or-transparent with no resampling. Runtime slicing uses centered pivots and 16 pixels per unit. The existing postprocessor enforces Point filtering, uncompressed textures and no mipmaps, with explicit 2D/Single import for this subtree.

`SpriteSpellFxRenderer` schedules the Cast → Charge → Travel → Impact → Aftermath timeline and overlays parallel target results. It only reads copied geometry/results, uses no gameplay randomness and does not look up live targets. Cosmetic variants derive from the sequence seed. It never drains an event queue. `WorldFxCoordinator` owns routing, waiting, visibility, interruption, settings, timeout and sprite/ASCII fallback.

Large impact frames are cut into nine fragments with widths/heights 8/16/8. Each fragment is checked against its own visible and explored cell; a hidden impact anchor emits no fragment. This clips even the half-cell overhang at fog boundaries. Detail sprites rotate in quarter-turns near fog so rotation cannot expose neighboring hidden cells. All views use World layer 8; ground details sort at 4 and airborne effects at 12, below popup layers 20/21.

Actual target results select resisted impact frames, a brief death overlay and moved-to-cell ripples. The death overlay is cosmetic and does not assert a new persistent material deposit. Rites display expenditure ticks only for marks actually consumed and use recorded status names for their material sigils. Payoff echoes scale from each target's resolved resonance. Environmental aftermath uses captured coating/residue/cloud/energy/reaction writes, including freeze, steam and conducted charge; unknown materials retain the existing state layer.

GameObjects, SpriteRenderers and scheduling records are pooled. Full mode permits 192 simultaneously rendered sprites and Reduced mode 48. Scheduled records are capped at 1,536. These are configured ceilings; profile evidence must come from the running showcase. `ActiveSpriteCount`, `PeakActiveSpriteCount`, `AllocatedSpriteCount` and `DroppedSpriteCount` provide diagnostics. The last is a count of sprite-frame attempts suppressed by the live budget. No lights, shaders, bloom or transient save data are added. Existing tile-state and persistent aura layers carry essential state in Off mode.

## Actor casting art

`ArtTools/coo_casting_poses.py` adds seven 64×96 actor sheets, each containing four
columns of 16×24 frames and four rows in South, West, East, North order. These
112 frames show gather, binding/reading, release and settle, with a bottom-center
pivot at (8, 0) and 16 pixels per unit. Point filtering, binary alpha and fixed
feet preserve the existing actor registration.

| Visual definition | Casting method |
|---|---|
| Player | Open book and releasing palm |
| Sill villager | Gathered hands and binding gesture |
| Concord merchant | Folded account and counting gesture |
| Sill elder | Staff and held gesture |
| Sill warden | Braced staff and ward gesture |
| Sill child | Cupped hands and a small charm |
| Recension echo | Inscription and ruled book |

The additive generator imports palettes and anatomy helpers from the existing
`coo_vertical_slice.py`; it does not rewrite that script, its 13 actor sheets, or
environment art. Cast-only regeneration is:

```sh
python3 ArtTools/coo_casting_poses.py
```

If intentionally rebuilding the original visual slice, run
`python3 ArtTools/coo_vertical_slice.py` first, then the casting generator. Neither
generator rewrites `EntityVisuals.json`, so its authored `CastSheet`, `CastFrames`
and `CastDuration` registrations remain in place. Runtime pose timing follows the
duration emitted with the spell's casting hook. A missing or undersized optional
cast sheet falls back to Attack frames. Fauna currently use that fallback.

`SpellFx-casting-contact.png` shows the seven cast sheets for registration review.
The generator checks binary alpha, registered feet and four distinct beats per
facing. Repeated generation produced identical hashes and left the 13 original
actor sheets unchanged. This is asset evidence; runtime pacing is assessed from
the Unity captures separately.

## Verification and corrections

| Check | Observed evidence |
|---|---|
| Asset TDD | Python checks first failed because catalog/art did not exist, then passed after generation. |
| Registered coverage | Python checks identify the 49 actual castable classes from registered skill content and execution bases, plus four retort definitions. |
| Pixel contract | Python checks validate every frame nonempty, binary alpha, exact dimensions and seven distinct monochrome projectile silhouettes. |
| Determinism | Two generator runs produced identical SHA256 hashes for all 14 PNGs and the 53-definition JSON catalog. |
| Visual asset inspection | `SpellFx-atlas-preview.png` was inspected in the image viewer; it is an atlas preview, not a Unity screenshot. |
| Import correction | Initial minimal metadata imported sheets as Cubemaps. Unity resource queries exposed the failure. Explicit Texture2D/Single settings and corrected metadata fix the importer contract; root verification records the reimport/test result. |
| C# regression checks | Catalog coverage, missing art, phases, pool reset, FOV fragments, hidden anchors, world layer, Reduced budget, actual material reactions and rite marks/resonance are covered in EditMode tests. These C# tests were authored after production code; their Unity results are recorded with the coordinated feature verification. |

The reference architecture adapts queued/pooling/timing ideas from the permitted Qud source investigation; the assets and rendering implementation here are original. This document does not claim Qud visual parity, Unity motion inspection, or profile measurements from an atlas preview. The coordinated feature report contains final Unity evidence and any remaining limitations.
