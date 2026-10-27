# Earned East caster baseline — post-partner65

Earned checkpoint after the Scrabbler partner died through ordinary melee. Cindercaller remained alive; this is not a checkpoint with the partner disabled by tooling.

This is an unchanged ordinary-play Quick save archived by the root operator from the isolated E1 session on 2026-10-27. It continues the earlier `east-shelter-pre-generation65` game. Root reports the existing baseline source revision as `bf38` (shorthand); no new caster motion art or runtime implementation is represented here.

| Field | Verified value |
| --- | --- |
| Game | `50e670c30b6d48feb82756df64bd1cbc` |
| Seed / save version | 65 / 8 |
| Current graph | `Overworld.12.10.0` |
| Player cell | 64,3 |
| Tick / HP / level | 1270 / 30/40 / 1 |
| Save timestamp UTC | `2026-10-27T03:15:39.1053440Z` |
| Quick.json | 411 bytes; SHA256 `fa0c632faeceb9d404bbc518771afcade5d031496a2ee8e52b8d13e114442721` |
| Quick.sav.gz | 118710 bytes; SHA256 `13453b32448525ddb8b9f06ef81f8128fb2b7e9fff186dd153fbd9c8604a35c6` |

Player64,3. In the following recorded sequence, ordinary eastward movement reached69,3 while the caster moved naturally from75,4 to74,7. Reobserve its actual location before approaching.

Related actual screenshot: [baseline image](../../east-caster-before-03-reader.png). The `before-03-reader` filename denotes the visible target/action selection menu, not a complete F1 examine-document capture. See [independent baseline review](../../east-caster-baseline-independent-review.json) for frame and hook evidence.

The binary prefix was read without loading gameplay: it proves the game/seed, active graph and exact player cell, and cached Glade plus East graphs. Tick, HP, level and timestamp come from Quick.json. Entity bodies, carried/equipped stock and AI goals were not independently decoded by this prefix audit. Root's ordinary run used the existing Warden LongSword/Buckler; this README does not invent an inventory census or unused ammunition state. FireMoss is not Ember Spit ammunition.

Load only through the existing isolated ordinary Continue procedure, protecting all normal saves and preferences. Do not overwrite a normal save or edit these bytes. Future AI choices, combat outcomes and image timing may differ. These are Editor baseline fixtures, not package/Deck acceptance or proof of post-implementation motion, restart or full-world completeness. Capture helpers only observed actual frames and callbacks; root reports no event dispatch, grants, timing changes or AI overrides.
