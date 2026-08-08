# Graphics Pass 13 — Muted Overgrowth Ships: Style Guide + Actor Sprite Pilot

> The style decision lands in the game. Origin: user directive
> 2026-08-01 — "codify the ruleset as Docs/STYLE-GUIDE.md … then put
> them in the game." Follows the five-style comparison
> (`Docs/StyleExploration/`) and the user's Muted Overgrowth pick.

**Status:** ✅ shipped 2026-08-01. Compile green; targeted suites
44/44 green live (8 new resolver pins RED-first); full EditMode
sweep run this pass (also retro-closing Pass 12's deferred gate).
Live sprite-mode eyeball remains the manual check (§4).

## 1. What shipped

1. **`Docs/STYLE-GUIDE.md`** — the binding ruleset: palette swatches
   (terrain fills, body family ramps, accent set), the four rules
   (no outlines / muted bodies / saturation-is-life ≤10% /
   speckle-not-shading), per-material speckle densities, the
   pre-ship checklist, and the renderer integration contract.
2. **Five Muted Overgrowth sprites into the game**
   (`Assets/Sprites/Environment/`):
   - `tree.png`, `mushroom.png` — REPLACED in place (same GUIDs, no
     meta churn) with the restyled tree and glowcap cluster.
   - `pillar.png` — NEW; claimed by `Pillar` ('I') and
     `BrokenColumn` (',' — which previously fell through to the
     BONES glyph mapping) via the blueprint tier.
   - `player.png`, `snapjaw.png` — NEW; **the first creature
     sprites** (the "Pass 9" the Pass 7 doc deferred).
3. **Actor sprite tier** in `EnvironmentSpriteRenderer`:
   - `ResolveActorKind(bp)`: exact `"Player"`; prefix `Snapjaw*`
     minus `*Corpse` (covers Scavenger/Hunter/Chieftain; the corpse
     keeps Pass 11 corpse handling).
   - Runs before every other tier in `ChooseTile`.
4. **Authored-color rendering**: `ChooseTile` gained
   `out bool authoredColor`; actor sprites carry their own palette,
   so PostRender applies only the cell's LIGHTING (max channel of
   the copied glyph color, which already includes the lightmap) as a
   gray tint — a `&Y` player no longer inherits yellow, but still
   dims correctly in darkness. Environment tiles keep the Pass 7-12
   color-copy behavior unchanged.
5. **Sprite pass on the incremental path**: `ZoneRenderer.LateUpdate`
   now runs `_envSpriteRenderer.PostRender` after `RenderDirtyCells`
   too. Without it, a moving NPC (dirty-cell repaint while the
   player waits) left its sprite claimed at the OLD cell and painted
   a bare glyph at the new one until the next full redraw; this also
   fixes crop overlays not advancing seed→sprout under sprite mode.
   Perf: same O(cells) scan PostRender already runs on every
   player-move frame; it now also runs on frames with queued dirty
   cells.

## 2. Tests

`EnvironmentSpriteRendererBlueprintTests` Pass 13 section — 8 tests,
RED-first (CS0117 ×38 before implementation): player exact-match;
all four living snapjaw variants; SnapjawCorpse exclusion
(counter-check: a live-actor sprite on a corpse would un-kill it
visually); other creatures + null/empty refuse; Pillar +
BrokenColumn share the pillar tile.

## 3. Known bounds / deliberate scope

- Other creatures (Villager `@`, IceWight, SporeShambler, …) remain
  CP437 — the actor tier is a PILOT; extending it is per-sprite art
  work gated by STYLE-GUIDE.md §5, not new architecture.
- The player sprite tint drops glyph hue entirely; if a future
  effect recolors the player glyph (e.g. poison green), sprite mode
  will show lighting only. Revisit when status-tint matters.
- Snapjaw variants share one sprite (color-identity between
  Scavenger/Hunter/Chieftain is lost in sprite mode v1).

## 4. Manual check (Play mode, `\` toggle)

New game → village: player renders as the hooded wanderer (not
yellow-tinted), trees/mushrooms show the restyled art; find/spawn a
snapjaw → it renders as the rust jackal and its sprite FOLLOWS it
cell-to-cell while you wait in place (the incremental-path fix);
ruins with pillars/broken columns show the weathered column (not
bones); kill a snapjaw → corpse renders as the corpse sprite, not
the live jackal.
