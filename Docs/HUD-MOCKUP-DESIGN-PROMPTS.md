# Skill bar and world log: image mockups

Status: concepts requested 2026-10-11. Review images before implementing UI. This addition does not count toward the twenty audio iterations or replace their pending work.

## Reusable design prompt

Review the current in-game screenshot and the actual hotbar/sidebar presentation data. Design two credible improvements to the skill bar and right-side world log for Caves of Ooo. Keep its dark, spare roguelike character and voxel world. Make selected, ready, cooling down, unavailable and empty skill slots distinguishable by shape and text as well as color. Keep ten skills accessible and show the selected skill's full name, targeting instructions and verified mechanics. Use the existing controller mappings, with device-aware keyboard hints in the implementation. Make vitals quickly readable, statuses explicit, inspection compact and the world log comfortable to follow without burying useful events in repeated housekeeping. Preserve access to full history; grouping and filters must not discard messages or reveal unseen enemies. Prioritize legibility at Steam Deck resolution and leave the world unobstructed. Offer one restrained refinement and one meaningfully different arrangement. Generate each as a separate image mockup, assess the tradeoffs, and show both before making UI changes. Do not present illustrative text, controls or mockups as implemented functionality.

## Shared image-generation constraints

Built-in imagegen, using the actual 1920x1080 game screenshot from AudioPolish20/06 as the edit target. Preserve the voxel world and camera; redesign the bottom hotbar and right sidebar. Flat near-black panels, subdued teal rules, warm ivory readable compact monospace, restrained amber selection, cyan cold skills and red damage. Sharp pixel icons, no glossy RPG chrome, parchment, ornate fantasy frames, quest arrows, minimap or additional game systems. Text should be comfortably readable when scaled to a handheld screen, without the current stretched lettering. Conceptual sample combat state, not a literal report of the screenshot's current state.

## Concept A: refined terminal

Keep one horizontal row of ten skill slots and a full-height right sidebar. Group vitals, inspection and log with fine rules and clear spacing. Give the selected Rime Grip a thin amber frame, readable full-name detail strip, Ready state and verified Range 5. Show numeric turn cooldown on another slot and quieter empty slots. Six occupied skills are Ember Spit, Flame Hands, Jet Blast, Ground Surge, Rime Grip and Calm. Give each a simple distinct pixel pictogram and short label; retain slot numbers. Use existing controller guidance: D-pad L/R: select; X: cast. Sidebar shows HP 32/40 with a modest bar, AV 0, DV 7, Dawn, compact focus Reedfrog / Frozen, and a generous world log. Illustrative entries: Rime Grip freezes the reedfrog.; Reedfrog takes 4 cold damage.; You draw 3 drams of water.; You enter the Spread. Latest event brightest, older events dimmer but legible. Small All / Combat / World tabs are a proposed presentation feature, not existing behavior. No giant floating tooltip or added bottom navigation.

## Concept B: field ledger

Make a different, practical arrangement: two rows of five skill slots with larger pixel icons and readable names, still ten slots directly accessible. Put the selected skill summary in the thin band immediately above them. Selected Rime Grip is framed in muted amber with Ready and Range 5, while one other skill has an explicit turn cooldown. Keep a full-height right sidebar but compress vitals and focus to its upper quarter, giving most space to a structured chronological field log. Group related combat lines under a quiet turn divider; put older exploration entries above the newest combat exchange. Keep small All / Combat / World filters and a clear latest-entry position, without losing full history. Use the same verified six skill names, simple icons, dark palette, illustrative stats and log text as A so comparison reflects layout. Existing D-pad L/R: select; X: cast. No new controls, minimap, quests, chat bubbles or meters. Give the world as much space as this larger, more legible dock permits.

## Review bounds

These are image-generated design concepts. Text fidelity, accessible colors, exact pixel dimensions, controller focus and runtime performance require implementation and testing. Mockup filtering and grouped history are proposals; Rime Grip's range and the controller hints were checked against source. The original screenshot's actual world remains the visual reference. No UI source changes have been made for these concepts.

## Generated concepts

- [A: refined terminal](Design/HudMockups/A-refined-terminal.png) preserves the single row and more world height.
- [B: field ledger](Design/HudMockups/B-field-ledger.png) enlarges skill labels and groups related log entries by turn.

Both were generated with built-in imagegen and visually reviewed. A targeted second pass corrected Jet Blast to a cyan water jet and Ground Surge to an electrical arc; B also uses a violet Calm glyph. Recommended combination: A’s compact skill row with B’s grouped log. These images are previews, not integrated UI. The future implementation must retain all existing vitals/history, responsive readability and input behavior; sample text and grouping in the raster are illustrative.
