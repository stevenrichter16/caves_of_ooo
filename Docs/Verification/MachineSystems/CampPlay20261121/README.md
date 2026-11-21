# Camp and workshop native play — November 21, 2026

Codex used SampleScene/GameBootstrap in the main Editor, seed 64, with the
authored wild-night camp and proofing-workshop scenarios. Actions used the real
Input System keyboard, inventory, interact menus and readers. Read-only MCP
observations recorded IDs, clock, resources and UI state; no gameplay method was
invoked to substitute for a player action. Scenario loadouts are fixtures, not
ordinary acquisition evidence. Saves were isolated in owned temporary roots.

## Camp, travel and Continue

- The starting road correctly refused placement. Walking three cells north to
  (40,9) allowed `make camp here`: four kits stood at tick 940, one action after
  tick 930. Hearth 2134 (40,8), still 2136 (41,8), bench 2135 (41,9), box 2133
  (41,10), in Overworld.10.11.0.
- Stoke consumed three timber and one striker use and lit the hearth, but the
  clock initially stayed 940. This is the reproduced timing defect below.
- The rest preview promised 24 HP and about 15 fuel. Rest until Dawn advanced
  940→1200, restored 16→40 HP, spent 30→15 fuel, and extinguished the flame.
- Ordinary feed and relight worked. The still then spent 12 bog mire and five
  fuel to put three water in the same flask 2155; tick 1220→1230. Its reader
  identified water's electrical/fire behavior.
- The pack's `Put in camp box` transferred dagger stack 2127, count two, into
  box 2133. Naming the camp Home updated the world-map note, including its
  contents and last-observed fuel. Move opened its targeting border; cancel
  retained position, fuel and tick 1230. No successful move is claimed here.
- World-map travel to an adjacent zone and back retained all four fixture IDs
  and the same stored dagger. F5 at tick 1340 saved the earned state.
- An archived copy of that earned checkpoint was continued through a fresh
  Play/GameBootstrap session in another isolated root. All IDs/cargo survived.
  This is fresh Play, not a new Editor process or packaged-build qualification.
  An earlier attempted session was discarded after an assembly reload cleared
  runtime bindings; it supplies no acceptance evidence.
- After the timing correction, `feed coals to full` advanced 1340→1350 exactly
  once, spent two timber, and left 29.75 fuel after normal end-turn burning.
  Pack up advanced 1350→1360 and returned all four same-ID kits, including box
  2133 containing dagger 2127. F5 completed.

## Workshop and an outside-machine outcome

- The actual pack contains 71 item entries and six category headings (77 rows),
  while the active screen still exposes twelve rows. Its first page shows ten
  items. This capture supports the separate inventory density redesign plan.
- Three ordinary press selections spent frost lichen 8→5 and produced three
  flasks with two cold-proofing each. Tick stayed 10: a second timing defect,
  contrary to the documented one-action press cost. The enabled row now declares
  OneAction; a later earned-save replay advanced 90→100 for one press.
- A flask coat split one cloak from its stack and spent one unit; tick 10→20.
  Making camp on open ground placed five compatible kits in one action and
  retained the two tray kits that had no source there.
- The corrected stoke row spent three timber and one strike and advanced
  tick 60→70. Trough proofing then spent four units across the carried flasks
  and two fire clay, adding lasting cold proofing to the coated cloak; 70→80.
- F1 correctly disclosed both +35 temporary coating and +25 lasting proofing,
  in addition to the cloak's +50 equipment bonus. Selecting the separate
  treated cloak and equipping it made cold resistance display 100 (capped).
- Drinking a frost tonic outside the machine advanced 80→90. No FrozenEffect
  remained and the message said `You shrug off the freezing.` This demonstrates
  machine preparation combining with ordinary gear and status rules. The tonic
  also printed its generic 'freezes you from the inside' line: contradictory
  flavor wording remains a presentation issue, not evidence of an actual freeze.

## Shared finite source, ordinary tools and persistence

Scraping frost vent 2389 at (32,7) advanced 190→200 and made one rime flake;
its stock read four of five remaining. Two trays were set down at (33,8) and
(33,9); their independently queried source/holder were both 2389. Ordinary held
wait input was stopped when a viper reduced health below the observation guard.
The player equipped the ordinary dagger, killed the attacker with normal bump
combat, let poison expire, and continued waiting. No invincibility/heal grant or
clock-advance method was used.

At tick 850 both tray readers projected one catch each without committing it.
Gathering each spent one action and left the shared stock at two. Two more
ordinary scrapes exhausted it at tick 910: five rime flakes total across scraper
and trays, stack 2705, NoTrade true. A further scrape was visibly unavailable,
reported the spent source, and left tick 910 unchanged. F5 and a fresh
Play/GameBootstrap Continue retained source 2389 at 0/5, LastTick 190, tick 910
and the earned player state. This qualifies ordinary source sharing/persistence;
the >400-cell geometry and legacy-holder upgrade remain native-test evidence.

Pressing one harvested flake later advanced 910→920 and increased the selected
part-filled flask as previewed (1→2). An initial aggregate observation mistook
that recipient for an empty one; no conversion defect was confirmed. Assertions
were strengthened to re-read the currently owned flask Part: five native cases
pass. A test-script assembly reload invalidated a separate attempted replay;
that attempt is excluded from evidence.

## Timing correction evidence

`CampfireWorkActions` now declares OneAction for its five executable work rows;
rest retains its deliberate clock policy and disabled rows remain inert. This
also routes the two newer commands through the shared outer transaction.

- Initial native RED: 11 cases, three pass/eight fail. One stale-stoke test
  prerequisite was corrected: with existing fuel it may light without timber,
  so the stale countercase also removes the striker. Seven failures exposed
  missing declarations or missing outer rollback.
- Native GREEN: 114/114 action-time, campfire, camp quality-of-life and tool-row
  cases. Zero compile errors. These overlap other receipts, not additive totals.
- Two separate mutants are killed, two failing cases each: remove Stoke time,
  or remove FeedFull time. Their current-tree source is independently reviewed
  with zero remaining P1/P2 findings. Reviewer did not run Unity or gameplay.
- Press native RED: five cases, three pass/two fail (one/three presses spent
  zero ticks rather than ten/thirty). The declaration correction passes the
  combined 303-case native family, zero compilation errors; a separate press-time
  mutant is killed by both successful-cost cases. The final five-case current-Part
  strengthening also passes. Receipts overlap, so these are not additive totals.
- Independent source review and review of selected native screenshots found no
  remaining P1/P2; it was evidence review, not independent operation of the game.
  Exact IDs, counters and effect absence are the operator's read-only observations.

The inherited full-suite result, further tool/machine gameplay, packaged Mac
delivery and independent review of native gameplay evidence remain open. These
checks do not accept the twelve complete machines, broad home construction,
ordinary acquisition, culture/capstan work or Steam Deck support.

## Preservation and receipt limits

The 84 preexisting tracked dirty files matched their baseline hashes after
camp play. All prior normal game-save file hashes matched; the shared Unity
analytics tree and TestResults.xml changed under Editor/full-test activity and
are excluded from that statement. Isolation restores its captured last-game
preference and root; concurrent batch tests also write their own test markers.
No byte-exact restoration of the entire persistent-data tree is claimed.

PNG files are native game captures; XML files are native clone test results.
Large saves/logs and disposable checkpoints remain outside the repository.
