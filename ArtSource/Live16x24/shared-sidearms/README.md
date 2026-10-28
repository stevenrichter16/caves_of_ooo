# Shared sidearms: immutable reuse

Dagger, ShortSword and Mace reuse the existing `item_dagger`, `item_sword` and `item_mace` environment PNGs unchanged. `EnvironmentSpriteRenderer.GetSpriteKey` explicitly maps these identities; LongSword already has separate native portable art and remains unchanged. Each source is16×16. Runtime creates a full square view at16PPU inside the flat16×24 presentation; it does not stretch or redraw these assets. Existing imports and originals are pinned in the manifest.

The short blade, guarded sword and headed mace have distinct silhouettes. Preserve those differences, native pixel edges and modest highlights. Coverage does not establish a unified equipment palette or worldwide polish. Exact original editable provenance was not located, and none is invented.

This uses the already documented immutable-art reuse exception from `Docs/GRAPHICS-SHARED-WEAPONSMITH.md`: a repeated imagegen pass and newly fabricated editable source would not improve unchanged approved art. New original sprites still follow the full actual capture→imagegen→parts→editable native source pipeline. Actual before and after evidence, owner/state guards and RED/GREEN remain required here.

Root baseline: actual Dagger7359 in carried reader, ordinary drop at60,3, step to61,3, ground reader, F5fixture149; normal pickup and F5fixture150. Shop notes disclose ShortSword and Mace, but are text-only notices rather than portrait evidence. The grimoire sale confirmation was cancelled; no book sale or funding change occurred in this branch. All saves remain preserved. See `Docs/GRAPHICS-SHARED-SIDEARMS.md` for continuation and acceptance bounds.
