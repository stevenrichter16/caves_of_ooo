# Gantry NPC and quest journal review

Opened original NPC-01, UI-03, JRN-01 and JRN-02 plus their matching per-image JSON. All are valid and nonblack. Session `journal-64`, seed 64, current source `5bb8e249d1fc69d9213f684bace7a8628e11b4ad`, SampleScene, ordinary keyboard/mouse input on macOS Editor, no controller. Gantry is `Overworld.7.8.0`, Spread / FieldStrips, CrossroadsExchange profile. Full world quality, 100% render scale, shadows On, Full FX, CRT off. Runtime full reveal is off and source-default FOV radius 999 is preserved. World cameras being hidden while the full-screen journal is open is intentional, not a failure to render the 3D world.

## NPC-01 — Clerk Padok in the Examine reader

`NPC-01-padok-examine.png`, 1280×800, 13:31:55.0456100Z. Player 2699 at (15,7), tick 560, Height, HP 40; actual target Clerk Padok, Villager 6981 at (11,7).

Caption: Clerk Padok receives a pale three-dimensional character portrait and a current-state section showing Sitting. The reader identifies him as a Creature made of Flesh, while the Focus panel ties him to his chair and 20/20 HP.

Observed: the portrait is recognizable as a dressed humanoid and clearer than the tiny world figure. Title, condition, material and close controls fit without overlap. Descriptive content is very thin: only “You see Clerk Padok,” despite this being a named quest-giving clerk. Sitting is listed as “Afflicted” in the sidebar/log, which is mechanically consistent with status display but reads oddly for normal occupation. This is one NPC sample, not proof of distinct portraits or content for all NPCs.

## UI-03 — The levy quest offer

`UI-03-padok-quest-offer.png`, 1280×800, 13:33:07.4860330Z. Player at (12,7), tick 590, Height. Padok says three citizens owe their levy and asks the player to collect it; the selected response is “[Accept] I'll collect the tax.” Goodbye, Trade and Attack follow.

Observed: dialogue description and the acceptance choice match. All four choices and speech fit, and the green allied frame is clear. The dark keyboard footer again sits on terrain below the panel and is difficult to read. The speech shows a question-mark-like replacement after “at your service”; `Assets/Resources/Content/Conversations/CandyTax_Quest.json:8` contains an em dash there. The text atlas only covers its stored character set and falls back to `?` for this unsupported Unicode character. This is a visible punctuation/rendering issue rather than changed quest meaning. The actual acceptance happens after this capture; JRN-01 is its follow-through.

## JRN-01 — Current stage and progress at 800p

`JRN-01-levy-current-stage.png`, 1280×800, 13:33:36.5873430Z. Same player position/tick 590. QuestLogOpen after accepting “Three Outstanding Accounts” through Padok's real dialogue.

Caption: The journal shows one open quest, yellow current stage `collect`, an objective to settle three outstanding accounts, and the pending `report` stage. Current progress is zero of three.

**Confirmed visible defects:** the `>` current-stage marker is absent before `collect`; progress renders `(0 3)` instead of `(0/3)`; the page line renders `1 1`; slash-separated footer hints also lose `/`. This now directly verifies the font problem previously inferred from source. `QuestLogUI.cs:70` uses `>` for current and line 212 inserts `/` for progress. Line 269 draws with `CP437TilesetGenerator.GetTextTile`. Its generated text atlas creates all 256 tiles on transparent pixels but only paints `GetFontData` characters, whose dictionary omits `>` and `/`. Consequently these are blank cached tiles, not question-mark fallbacks. The yellow current-stage color survives, but its redundant shape cue and numeric separators do not.

The journal lettering is unusually narrow with wide spacing, while the content occupies only a small portion of the full-screen canvas. It appears much smaller than the polished Examine/Skills text at the same output resolution. Large empty space is visible, but a single short quest does not test list pagination or dense journals. The screenshot is still useful honest current-state evidence; it should not be presented as successful glyph validation.

## JRN-02 — Matched current-stage journal at 720p

`JRN-02-levy-current-stage-720.png`, 1280×720, 13:33:52.6184750Z. Identical quest, player tile, tick and input method; only output height changed.

Caption: The same accepted levy quest is shown at 720p, preserving the current/pending stage text and the zero-of-three objective.

Observed: missing current marker and slashes persist. Nothing is clipped by the viewport, but the narrow small text and wide spacing remain less readable than the other reviewed interfaces. This is a real resolution comparison on macOS, not physical Steam Deck testing. No quest turn or progress change occurs between these frames.

The prior “current-stage journal pending” limitation in manual/skills fragments is superseded by these real accepted-quest captures. No correction to source or rendered images was made. Travel-note and settled long-dialogue coverage may follow separately.

## UI-04 / UI-05 — Settled wilderness reports at 800p and 720p

Final `UI-04-scribe-wilderness-reports.png`, 1280×800, 13:39:04.9275040Z; `UI-05-scribe-wilderness-reports-720.png`, 1280×720, 13:38:28.1827460Z. Both are the settled branch, same player (17,5), tick 660, Dusk, real scribe Vellin Mosswick. The final 800p image was recaptured after the 720p image to replace an early typewriter frame; file IDs are not capture-time order. The early partial text frame is retained only as `evidence/excluded-typewriter-UI-04.*` and is excluded from final coverage.

Caption: Vellin offers three old wilderness reports: ditch-cutters near (10,2), poisonous chalk-ring vipers near (9,0), and Turnbank wayhouse at (11,7). Each paragraph states direction and source and warns that the report does not establish what remains now. The three Remember choices match the three reports.

Observed: all three descriptions finish and all six choices fit at both resolutions. Selection and action tags are active and colored, confirming the reveal has finished. The caption and choices now line up: these are wilderness reports, without unrelated settlement-choice rows. The large paragraph block is dense but readable; the 720p rendering uses a smaller visual text scale. The recurring report/source/uncertainty wording consumes much of the space. The dark keyboard footer remains hard to read over the world. The sidebar and six-spell hotbar fit alongside/below the conversation at both sizes. This supplies an actual long dialogue sample; it still does not exercise a second dialogue page or controller paging.

## JRN-03 — Recording a report into field notes

`JRN-03-travel-notes.png`, 1280×800, 13:39:35.6316750Z. Same player/tile/tick 660. After selecting **b: Remember ditch-cutters**, the operator pressed Escape, Q, then Tab through ordinary controls.

Caption: The Field Notes page now contains the exact ditch-cutter report selected from the scribe: destination (10,2), three east and six north from Gantry (7,8), reported by Vellin Mosswick, unconfirmed when heard.

Observed: the content, coordinates, directions, attribution and uncertainty match UI-04/05. This establishes the dialogue-to-journal connection rather than a staged note injection. It does not verify the actual encounter at the destination or a world-map marker. One note occupies four long, widely spaced lines at the top; the remaining screen is empty. The page count and footer once again lose slash glyphs. The full-screen journal intentionally hides the world renderers; this is not black-world gameplay. No text clipping is visible.

Final journal fragment now covers seven originals: NPC-01, UI-03/04/05 and JRN-01/02/03. Long single-page conversation and travel-note transfer are covered; multi-page dialogue remains untested. All captures preserve the current defects; no graphics/UI fixes were made.
