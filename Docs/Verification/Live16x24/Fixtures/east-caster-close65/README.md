# Earned East caster baseline — close65

Earned closer approach with the partner already dead and the caster still at12 HP in the adjacent capture. Useful to replay movement and seek ordinary casting.

This is an unchanged ordinary-play Quick save archived by the root operator from the isolated E1 session on 2026-10-27. It continues the earlier `east-shelter-pre-generation65` game. Root reports the existing baseline source revision as `bf38` (shorthand); no new caster motion art or runtime implementation is represented here.

| Field | Verified value |
| --- | --- |
| Game | `50e670c30b6d48feb82756df64bd1cbc` |
| Seed / save version | 65 / 8 |
| Current graph | `Overworld.12.10.0` |
| Player cell | 69,3 |
| Tick / HP / level | 1320 / 30/40 / 1 |
| Save timestamp UTC | `2026-10-27T03:17:57.3628860Z` |
| Quick.json | 411 bytes; SHA256 `f4c0d31c72f5b4df6ddd9de206c4d96647a91ab3b8e7c5592a33c30db9aca1ae` |
| Quick.sav.gz | 118856 bytes; SHA256 `9eea503873f2a431e1e05fa5557d4f97d4bea17cbb8824d6d98299ce9dd5af33` |

Player69,3. The subsequent baseline followed the independently acting caster through the eastern meadow; it targeted a magpie, not necessarily the player. Its future route, target and spell choice are not guaranteed.

Related actual screenshot: [baseline image](../../east-caster-before-04-close.png). The `before-03-reader` filename denotes the visible target/action selection menu, not a complete F1 examine-document capture. See [independent baseline review](../../east-caster-baseline-independent-review.json) for frame and hook evidence.

The binary prefix was read without loading gameplay: it proves the game/seed, active graph and exact player cell, and cached Glade plus East graphs. Tick, HP, level and timestamp come from Quick.json. Entity bodies, carried/equipped stock and AI goals were not independently decoded by this prefix audit. Root's ordinary run used the existing Warden LongSword/Buckler; this README does not invent an inventory census or unused ammunition state. FireMoss is not Ember Spit ammunition.

Load only through the existing isolated ordinary Continue procedure, protecting all normal saves and preferences. Do not overwrite a normal save or edit these bytes. Future AI choices, combat outcomes and image timing may differ. These are Editor baseline fixtures, not package/Deck acceptance or proof of post-implementation motion, restart or full-world completeness. Capture helpers only observed actual frames and callbacks; root reports no event dispatch, grants, timing changes or AI overrides.
