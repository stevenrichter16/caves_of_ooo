# M1d0 — readable existing torch fuel and flame

Bounded acceptance2026-11-16. One runtime file, ItemExamineService, adds an owned-part, read-only formatter to the shared equipment detail path. Full Examine and inventory F1 expose current/max fuel per torch, controlled flame, wet/frozen/empty suppression and ordinary refuel guidance. It neither ticks nor changes mechanics. Current Mac8bb2b8f82 predates this reader.

## Native evidence

- Initial native-red.json:29 completed, failed status, failure list capped25; missing new reader facts. Do not infer an exact failure total from the cap.
- First targeted run149: one stale Dagger attribute-order expectation, unchanged parser sorts ordinally. Corrected test to Agility then Strength; no stat behavior changed.
- f1-native-red.json:2/2 actual keyboard F1 cases fail before moving the formatter into the shared equipment path.
- targeted-173-of-174.json:174 completed with one new fire-affliction assertion mismatch, actual text is “Set ablaze - burning”; assertion corrected, existing affliction retained.
- final-30-green.json:30 torch checks passed. format-red.json then reproduces actual47.00001 display noise.
- final-33-green.json:31 torch and2 F1 cases pass33/33 in1.5619113s.142 remaining adjacent checks from the174-case run are retained: **175 distinct checks across overlapping receipts**, not a175 combined run or a sum of all runs. Includes the31 torch core/read-only/invalid-state/save checks and the real selected held/stowed F1 input seam; all unspecified aggregate timings remain unknown.
- Excluded zero-test import and two initialization-timeout receipts remain. No compile or gameplay failure is inferred from those zero-start attempts. One transient MCP WebSocket diagnostic recurred after reload; no C# compilation errors.

Independent bounded source review found the F1 omission, then verified the shared fix, owned parts, finite numbers, state truth, unchanged time and ordinary guidance with0 remainingP1/P2. A source-verified stale stat-order assertion and the new test's affliction wording were fixture corrections. No separate unchanged-source native historical claim.

## Actual GameBootstrap play

Baseline is the earned OilQualification/TorchCheckpoint2240HP40 atMorrowfast3.6:31,21. The exact purchased torch is da566d478be54efb8447b08239d44885, held in the left hand, fuel48.8000031; three fresh torches are stowed. Continue uses an isolated root, not normal saves. F12off; no simulation mutation or injected stock.

`held-reader-after.png` matches the baseline full reader and discloses48.8/50. `stowed-f1.png` shows50/50 per torch, not150 fuel. The first batched exit sequence reached the controls screen and one Down move2250; `held-f1-after-walk.png` is excluded as a reader acceptance capture. It is retained for chronology. After returning to normal play, Left2Right2Up completed five more moves, reaching2300HP40 at31,21. Six actual moves in total consumed1.8 fuel, yielding47.0000076. `held-f1-valid.png` records the working F1 path and the display noise that prompted G5 rounding.

Normal item action Extinguish reached2310HP40 and left fuel unchanged. F5 wrote AfterCheckpoint. The root was archived at/private/tmp/coo-torch-after-root-16b05713 before Finish. After the display correction, fresh GameBootstrap Continue from the unmodified checkpoint retained the same ID, location,HP40, fuel and controlled Out. `fresh-reload-f1.png` shows final47/50. Read-only JSON accompanies observations; it is not a replacement for UI actions. Independent gameplay/visual review inspected the before/after, stowed, held and fresh-reload captures:0 remainingP1/P2. Wet/frozen/corrupt-state cases are native evidence, not actual-play claims.

After acceptance reload, ordinary AutoEquip split a purchased spare into the free right hand, then the original torch's “light torch at nearby fire” action relit it from that held donor. `relight-menu.png` and `relit-world.png` retain the menu/result. Read-only2320HP40 shows original46.70001fuel/lit and donor283875759b6542cab96de233650933da49.7fuel/lit. Two full spares remain stowed. F5 saved RelitCheckpoint. This additional existing-source qualification is separate from reviewed reader acceptance; no LampOil refuel or process occurred. The second isolated root was archived at/private/tmp/coo-torch-reload-root-0e392968; Finish reported stopped/rootnull/sceneclean.

## Scope and next work

This accepts readable existing consumer state only. Raw FrogOil harvesting/grease risk is separately qualified in OilQualification. The provisional2FrogOil→1LampOil+1dregs recipe is not implemented or accepted. Useful expedition demand, two actual inputs, fresh Works placement, finite staged outputs/waste and useful refuelling remain required. No new heat, elapsed work, energy store, machine construction or full-machine gate is closed. No FPS/hardware qualification or new Mac build is claimed.

Preservation check before publication: all84 prior tracked dirty files match the07:02-restored snapshot; normal Editor/standalone save files have no changes/additions. Standalone preference domain unchanged; Editor preference differences are only existing operational session keys unity.player_sessionid, unity.player_session_count, PT_Run and unity_connect.session_id. Normal gameplay preferences remain intact. Required builds and parked graphics draft untouched. Exact92 owned files staged; safely fetched origin/main with0/0 divergence before commit.
