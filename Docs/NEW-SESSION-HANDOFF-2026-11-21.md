# Caves of Ooo: start here (complete context for a brand-new session)

Written 2026-11-21 (America/Chicago) by Claude, at the user's request: "a handoff doc i can give a brand new session so it has all context necessary."
This file is self-contained. It does not replace the deeper records it links to; it tells you what they are, which to trust, what is true right now, and what to do first.
If you only read one section, read **section 1** and **section 6**.

> To start a new session, give it this file and say: "Read `Docs/NEW-SESSION-HANDOFF-2026-11-21.md` in `/Users/steven/caves-of-ooo`, run the first-ten-minutes commands in it, then continue with section 6."

---

## 1. Sixty-second summary

- **The project.** *Caves of Ooo* is a Unity 6 (6000.3.4f1) **RPG, not a roguelike** (the character persists; death is recoverable by loading). The code mirrors Caves of Qud: an `Entity` is a bag of `Part`s, behaviour is driven by string-keyed `GameEvent`s. Repo: `/Users/steven/caves-of-ooo`, GitHub `stevenrichter16/caves_of_ooo`, branch **`main` only**. The user (Steven) does not hand-code; an AI agent does all coding, tests and docs.
- **The active program.** Since 2026-11-16 the user has directed work on **world systems and machines**: the twelve proposed "complete machines" (forge/reclamation rig, fractionating still, restoration cradle, assay cabinet, culture vat, anchored capstan, sump pump, thermal exchanger, dynamo, survey resonator, signal relay, treatment bath), a home/base layer (eight set-down camp kits), and "invented uses" for machines and tools placed wherever in the world is most engaging. Graphics work was **cancelled** on 2026-11-16: every new blueprint is glyph-only. **All twelve complete-machine gates remain open**; what exists are useful partial foundations.
- **Two agents have alternated on this repo: Claude (Claude Code) and Codex.** Both commit under the user's git identity. Only one may own Unity and Git at a time. The user hands control back and forth explicitly. Codex may also run an hourly automation (`caves-of-ooo-machine-foundations`) that Claude's scheduler cannot see: always check for foreign activity before you run Unity or commit (section 3).
- **State right now (2026-11-21, about 17:20 Central; re-check with `git log` and `ps`, this goes stale):**
  - `main` and `origin/main` were both at `019623bdb` when this was written. That is Codex's commit (shared finite-source stock correction, one-action cost on the newer hearth and press rows, native camp/workshop play receipts, the inventory-density plan), on top of Claude's `e89a81af6` (how-to blocks for every machine and tool) and `bc6e00c06`.
  - **The full-suite gate `fullrun_h` is still running** in `/Users/steven/coo-verification/clone4` (Unity pid 85151, started 13:42, over 3.5 hours; it had reached about 90% of the baseline's test time by 17:00). Its result file `/Users/steven/coo-verification/results/fullrun_h.xml` did not exist yet. It tests the runtime at `e89a81af6`, **not** Codex's `019623bdb` fixes and not the scenario fix below. Do not start another Unity run until it ends (section 8).
  - **A fix is in flight, uncommitted and unverified, in the working tree:** the hand-play fixture "Camp Machines (wild ground)" gives its player only part of its loadout, because the shipped pack has a hard cap of 150 and the loadout weighs about 160 (found by Codex in native play; `GiveItem` only writes a console warning). Pieces on disk: a new helper `Assets/Scripts/Scenarios/Builders/ScenarioLoadout.cs` (`MakeRoom`, `Give`, `WeightOf`; no `.meta` yet) and five new RED/guard tests in `Assets/Tests/EditMode/Gameplay/World/CampKitShowcaseTests.cs` (both written by Claude at 17:06); and edits to the five camp scenarios (`CampMachines`, `CampKits`, `CampAwning`, `SoundingRod`, `ConcordCarriage` showcases) that switch them to `ScenarioLoadout.Give`, plus a `using CavesOfOoo.Data;` added to the helper, **all made at 17:17-17:18 by an agent other than the author of this doc** (probably the Codex hourly automation or a Codex session; unconfirmed). Nothing has been compiled or run: RED was never proven, GREEN never seen. Run `git diff` and `git log -3` first to see whether it has since been committed or changed.
- **The five binding rules** (full list in section 4): never test in the user's open Unity Editor (use the verification clones); commit and push verified work to `main` by default (explicit paths only, never force); TDD with counter-checks, mutation checks and an independent read-only review; the world rules are the product (no one-off scripts); lore is protected.

## 2. First ten minutes (copy and run)

```bash
cd /Users/steven/caves-of-ooo
git branch --show-current && git fetch origin main && git status -sb | head -3     # on main? up to date?
git log --format='%h %ad %s' --date=format:'%m-%d %H:%M' -8                          # what landed lately, and by whom (Claude vs Codex)
git status --short | grep -v '^ M' | head                                            # untracked: foreign files, see section 7
ps -axo pid,etime,args | grep -E 'Unity.app/Contents/MacOS/Unity ' | grep -v grep | sed -E 's/-adb2.*-projectPath/ [worker] -projectPath/' | cut -c1-200
ls /Users/steven/coo-verification /Users/steven/coo-verification/results | head -40  # clones clone..clone5, results, baselines
```

Read what the output says before acting:
- A `Unity ... -projectPath /Users/steven/caves-of-ooo` process is the **main Editor** (the owner's / Codex's play surface). Do not run tests in it and do not enter Play mode without checking `isPlaying` first. Verification runs in the clones: `-projectPath /Users/steven/coo-verification/cloneN -batchmode`.
- A `clone_run.sh ... cloneN` process means that clone is busy. **Do not start a second Unity run while a long run is going** (see "two concurrent Unity runs" below).
- Then read, in this order: this file; [Docs/CODEX-MACHINE-HANDOFF-2026-11-21.md](CODEX-MACHINE-HANDOFF-2026-11-21.md) (the newest agent-written handoff, with the native-play receipts); [Docs/MACHINE-PROGRAM-HANDOFF.md](MACHINE-PROGRAM-HANDOFF.md) (the long reference: architecture, catalog, verification method, lessons).

## 3. Who is who, and ownership

| Actor | What they are | What to know |
|---|---|---|
| **Steven (the user)** | Owner and designer; reviews, decides, does not hand-code | Directions are quoted in section 5. Wants autonomy: "do this for as long as you can without my intervention". Wants work committed and pushed. |
| **Claude Code** | An agent in this repo | Wrote the machine program through 2026-11-21 (tool rows, grouping, how-to descriptions, the verification scripts). Uses MCP tools `mcp__unity__*` and the clones. |
| **Codex** | Another agent that the user sometimes hands "repo, Unity and Git control" to | Fixed the shared finite-source stock and action-time defects, played camp/workshop natively in the main Editor on 2026-11-21, wrote the inventory-density **plan**. Its handoff: [CODEX-MACHINE-HANDOFF-2026-11-21.md](CODEX-MACHINE-HANDOFF-2026-11-21.md). |
| Hourly automation | `caves-of-ooo-machine-foundations`, described in `CLAUDE.md` and the Codex handoff as ACTIVE | Not visible in Claude's scheduler (`list_scheduled_tasks` shows only two disabled tasks, brainstorm and lore analysis). Treat it as another agent that could commit or start Unity at any time. |

Ownership rules: one owner of Unity/Git at a time; if another agent's commit appears between your `git fetch` and your push, integrate it (`git pull --rebase` on your own commits is fine; never force-push). The user's explicit message is what transfers control.

## 4. The binding rules (condensed; the source is `CLAUDE.md` plus the owner's standing orders)

1. **Never run tests or compile in the user's open Unity Editor.** It is a play surface; a reload or test run wipes a session. Verify in the warm clones (`Tools/CloneVerification/`, section 8). A run whose log shows `error CS` proves nothing; a frozen test count after an edit means a stale assembly.
2. **Commit and push verified work to `main` by default** (the user: "commit and push it, always"; "make sure all your commits and pushes have been to main"). Stage **explicit paths only**, copy new `.meta` files from the clone, trailer line `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`, **never force-push, never `git add -A`**.
3. **TDD with counter-checks, mutation checks, then an independent read-only review.** Write the failing test first and prove RED (and that it fails for the right reason). Pair every positive assertion with a counter-case. Break the code deliberately (mutants) and show the tests catch it. Have a separate read-only agent review the diff; confirm each review claim against the code before acting. Record honest limits ("not played", "not run"). Update the living doc in the same commit.
4. **The world rules are the product** ([SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md)). New behaviour attaches to Parts/capabilities and shared rules (finite stock, custody, noise, heat, status resistance, declared action time). Keep several useful approaches and clear consequences; never a one-solution tool puzzle. No effect may mint money or a reagent, bypass payment, or break save/reload.
5. **Lore is protected.** `Lore/` (god-tree/tepui) is canon; `Docs/Lore/` is the superseded Palimpsest world: never design from it. Do not make an instrument claim to know what the lore says it cannot.
6. **Preserve the user's other work.** About 84 tracked files and some untracked ones are foreign (importer rewrites, settings, art sources, verification PNGs). Never stage, revert, reformat or delete them (section 7). No storage reclamation or build deletion is authorized.
7. **Performance.** No allocations in per-frame/per-turn paths; per-cell dirty hooks for visible changes; see `Docs/PERF-FOUNDATION.md` before touching hot paths.
8. **Diag everything.** Every gate that can reject and every success path emits a diag record (`Diag.Record(category, kind, ...)`), pinned by a test.

## 5. The user's directions, in order (so you can see intent, not just state)

- 2026-11-16: "Build machines, NPCs and objects from composable Parts, reusable capabilities and common events ... With the appropriate materials, components, objects, machines and access, player common-sense expectations should have consistent supported paths." Graphics scheduled task cancelled; hourly machine-foundations task authorized.
- 2026-11-19: invent and design uses for machines that have none, make them fit the world, gameplay and lore; do 5 new machines "for as long as you can without my intervention"; "the machines don't have to be for camp, they can be wherever in the world you think makes it most engaging and interesting and fun."
- 2026-11-21: scraper (and then every tool/machine usable on the environment) should be usable from the **C menu of the thing it acts on**, not only from the inventory; "commit and push it, always"; group similar actions into one row that opens "so it's not a long wall"; descriptions must "tell the player exactly how they work or how to use them, if they're usable in that moment, or what materials are needed, or what environmental factors"; then a handoff doc; "make sure all your commits and pushes have been to main."
- 2026-11-21 (to Codex): fix the shared finite-source bug, finish the regression assessment, actually play the machines, and write an inventory-density **plan** (no implementation).
- 2026-11-21 (this handoff): "write a handoff doc i can give a brand new session so it has all context necessary."

## 6. What to do next (priority order)

Do these in order. Re-read the state first (section 2): another agent may have finished some of it.

1. **Read the gate verdict.** When `/Users/steven/coo-verification/results/fullrun_h.xml` exists: `python3 Tools/CloneVerification/compare_full.py /Users/steven/coo-verification/results/fullrun_h.xml`. The baseline is `full_s9.xml` (39,500 tests, 91 known failures). Re-run **every NEW failure alone** (exact class filter, idle clone, nothing else running); a 180-second timeout that passes alone is contention. If the process died with no XML, say so and re-run. Record the verdict in `MACHINE-PROGRAM-HANDOFF.md` section 6.4 and commit it.
2. **Finish the scenario-capacity fix** (section 1, "A fix is in flight"). Design, so you can judge the code on disk: `ScenarioLoadout.MakeRoom` lifts the player's hard cap, only when one is set (`MaxWeight >= 0`), to the larger of the sidebar capacity (`GetMaxCarryWeight()`, Strength x 15) and carried weight plus the loadout's weight; it never lowers a cap and leaves an uncapped pack uncapped. `Give` makes room, gives every item, counts what arrived against what was held before, and writes one loud line to the in-game message log (`ctx.Log`) for any shortfall. Do **not** change the shipped cap of 150 to repair a fixture (Codex's instruction; the sidebar-versus-hard-cap mismatch is a separate question). Steps: (a) read `git diff` of the five scenarios, the helper and the tests; (b) prove RED: `red_run.sh clone3 'CavesOfOoo.Tests.CampKitShowcaseTests' red1 <the five scenario files>` (the new guard test must fail by truncation, not by a compile error; the helper first lacked `using CavesOfOoo.Data;`); (c) GREEN on `CampKitShowcaseTests`, `FishTrapTests`, `ProofingTroughTests` and the scenario-bench families; (d) mutants (drop `MakeRoom`; use `Math.Min`; make `Give` ignore the delta; drop the message); (e) an independent read-only review; (f) copy the helper's `.meta` from the clone, update `MACHINE-HAND-TEST.md` section 2 (one line: the scenario now lifts the cap and says so if anything is missing) and the handoff docs, commit explicit paths, push. Note the existing test `EveryMachinesLoadoutBlueprintExistsAndTheLoadoutIsCarriable...` asserted the SOFT limit ("Strength 16 carries 240"); its message now says so.
3. **Run a full gate on the new head.** `019623bdb` and the fix post-date `fullrun_h`. Once clone4 is idle and the fix is committed: `Tools/CloneVerification/clone_run.sh clone4 'CavesOfOoo' fullrun_i` in the background (about 3 hours), then step 1 again.
4. **Finish native play** from [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) in the main Editor with ordinary keyboard input (the main Editor is the play surface; check `isPlaying`, pin the instance with `set_active_instance`, wait for the assembly reload before pressing Play). Not yet played by anyone: a successful camp move, the fish trap, the shade awning, the sounding rod, the spring trap, hush strips, Posy's Sorters' frame and Setters' bench, the Concord carriage, steam-vent heat and the bake refusal. Codex's play receipts and its tips for driving native input are in [CODEX-MACHINE-HANDOFF-2026-11-21.md](CODEX-MACHINE-HANDOFF-2026-11-21.md) ("Safe continuation"). Build a new Mac at a coherent milestone and keep the old builds.
5. **Known small defects, not fixed:** the generic frost-tonic flavor line still says it "freezes you from the inside" after the shrug-off message (wording only); the sidebar shows a Strength capacity (270) that the pack's hard cap (150) contradicts.
6. **Inventory density redesign** is a plan only ([INVENTORY-DENSITY-REDESIGN-PLAN.md](INVENTORY-DENSITY-REDESIGN-PLAN.md)). The user asked for the plan, not the UI. Do not implement it without the user saying so.
7. **Owner decisions and wording** wait on the user: the open questions in `MACHINE-PROGRAM-HANDOFF.md` section 9, and a voice review of every placeholder string ([MACHINE-USE-TEXTS.md](MACHINE-USE-TEXTS.md) lists them all).
8. **Candidate builds** (each needs a lore read first; list in `MACHINE-PROGRAM-HANDOFF.md` section 10.4): hand-over objects for dialogue-only supply approaches, spore-salting as a fifth proofing element, a ward-oil still process, a charge rod, a Tally ledger keeper, underground kit placement, a thermal vent tap, a culture vat. Prefer verifying and playing what exists over adding machines.

## 7. Repo state and what not to touch

- Branch `main`, remote `origin` (GitHub `stevenrichter16/caves_of_ooo`). Check `git log` for the real head; do not trust a hash in a doc.
- **Foreign (not yours) and must be preserved:** ~84 tracked files, mostly `*.png.meta` / `*.json.meta` importer rewrites under `Assets/Resources/`, plus `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`; untracked: stray `Assets/InitTestScene*.unity`, `_to_delete/`, `Docs/RELEASE-CANDIDATE-*.md`, `ProjectSettings/Packages/com.unity.probuilder/`, images under `Docs/Verification/`, `ArtSource/**` logs and `.blend1` files. Inspect with `git status --short`; stage only files you changed, by path.
- Codex's baselines for that preservation check live outside the repo and may be gone: `/private/tmp/coo-shared-source-foreign-baseline.json`, `/private/tmp/coo-machines-normal-state-baseline.json`.
- `CLAUDE.md` is long and historical. Its many "Current graphics ..." paragraphs are superseded by the 2026-11-16 cancellation. Still binding: the central design philosophy, the TDD/review/adversarial/performance rules, the Unity MCP workflow notes.
- Saves and preferences: normal game saves are the user's. Never use them as a test target; copy checkpoints into a newly owned isolation root.

## 8. How to verify (the clones and the scripts)

Verification clones are warm APFS copies of the project in `/Users/steven/coo-verification/{clone,clone2,clone3,clone4,clone5}`; results go to `/Users/steven/coo-verification/results/` (use a NEW name for every run); the accepted full-suite baseline is `/Users/steven/coo-verification/baselines/full_s9.xml` (39,500 tests, 91 known failures, 2026-11-19). Durable scripts are in [`Tools/CloneVerification/`](../Tools/CloneVerification/README.md):

```bash
T=/Users/steven/caves-of-ooo/Tools/CloneVerification
$T/clone_run.sh clone3 'CavesOfOoo.Tests.XTests|CavesOfOoo.Tests.YTests' name1     # subset: sync working tree to the clone and run (2-5 min)
$T/clone_run.sh clone4 'CavesOfOoo' fullrun_X                                      # EVERYTHING, about 3 hours: run in the background, nothing else on Unity meanwhile
python3 $T/compare_full.py /Users/steven/coo-verification/results/fullrun_X.xml    # new failures vs baseline; marks timeouts
$T/red_run.sh clone3 'CavesOfOoo.Tests.XTests' red1 Assets/Scripts/.../File.cs      # RED proof: tests against the HEAD version of the files
$T/each_mutant.sh clone3 spec.json 'CavesOfOoo.Tests.XTests' mut1                   # one clean run per mutant, then a KILLED/SURVIVED table
$T/at_commit.sh clone4 <commit> 'CavesOfOoo.Tests.XTests' bis1                      # did it already fail at that commit?
```
Mutation spec: `[{"id":"m1","file":"Assets/Scripts/...cs","old":"exact text","new":"changed text"}]`.

What the clone scripts do and do not do: they sync `Assets`, `Packages`, `ProjectSettings`, `Docs` and four small `ArtSource` folders into the clone; refuse a clone that already has a Unity process; print `compile errors: N` (N above 0 proves nothing). New files need their `.meta` copied back from the clone (Unity generates it there) before you commit.

**Rules that cost hours to learn:**
- **Two concurrent Unity runs make time-sensitive tests exceed their 180-second timeout.** Re-run any "new" failure ALONE (its exact class filter, an idle clone) before believing it; a timeout that passes alone is contention, not a regression.
- Never sync the working tree into a clone while a batch is running in it; never edit production code in the working tree while a mutation batch runs (every run re-syncs).
- `read_console` misses compile errors: grep the batch log for `error CS`.
- A subset run misses cross-system regressions. After changing a global rule (status application, stacking, noise, a shared reader, inventory) also run the Equipment/Inventory/Item families; after adding a blueprint that appears in a generated settlement, run the rendering/coverage families and a full run.
- Disabled action rows have an EMPTY `Command`; find them by `DisabledCode`.
- Real movement goes through `TryMoveDetailedCore`, not `TryMove`.
- A mutant that fails to compile is INVALID, not killed. Mutations in one spec need disjoint expected tests.
- **Tests hide the real pack cap.** The shipped player's pack has a hard `MaxWeight` of **150** (blueprint `Creature`), while the sidebar shows a soft Strength capacity (Strength × 15 = 270). The test fixtures raise the cap (200 or 400) before applying anything, so anything that depends on the shipped cap needs a test that does not.
- The Unity MCP server (`~/bin/coo_mcp_keeper.py`, `~/bin/mcp_r.sh`) dies whenever any Unity quits; `clone_run.sh` restarts it. `pkill -f coo_mcp_keeper.py` at session end.
- `execute_code` is CodeDom/C# 6; read-only observation only (firing gameplay events through it corrupts state). Entering Play resets the scene: warn the user first.

## 9. Architecture in one page (the long version is MACHINE-PROGRAM-HANDOFF.md section 3)

- **Entity/Part/GameEvent.** Parts answer events. Menus are composed from `GetInventoryActions` (a Part adds `InventoryAction` rows: `AddAction`, `AddActionWithHint`, `AddDisabled`); `InventoryAction` carries `Name/Display/Command/Key/Priority/Hint/Time/Group/Shared`. Declare `time: ActionTime.OneAction` on any row that must cost a turn (the 2026-11-21 Codex fix: the newer hearth and press rows were free).
- **The C (interact) menu** is composed in one place, `WorldInteractionSystem.GatherCellActions`; carried-tool rows attach to the cell/entity they act on with `.AtCell/.AtEntity/.InGroup(ToolGroups.X)`; three or more rows under one heading collapse into one row that opens (`ToolGroupAt = 3`).
- **Readers.** `IDescribeState` says what a thing holds; `IDescribeUse` (+ `UseGuide`, `UseGuides.Collect`, `IDescribeUseRows`) says how to use it and what is possible right now, built from the same rows the menus show. Both feed Examine and the full reader (F1). See `Assets/Scripts/Gameplay/Entities/UseGuide.cs` and [MACHINE-READERS.md](MACHINE-READERS.md).
- **Machines are Parts + shared rules.** Camp kits are `PackableFixturePart` + a registry; processing uses finite sources (`SourceStockPart`, `ProofingSources`, `LiquidSettlingPart`), declared heat/noise, custody and status resistance. Checklist for adding a machine: MACHINE-PROGRAM-HANDOFF.md section 3.2.
- **Scenarios** (hand-test fixtures) are in `Assets/Scripts/Scenarios/Custom/` with menu entries under **Caves Of Ooo > Scenarios > Home**. They only grant items and place objects; every verb afterwards is the ordinary one. Guide: [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md).

## 10. Document index (what to read when)

| Need | Read |
|---|---|
| Why and how the program was built, catalog of every machine/tool, add-a-machine checklist, honest limits, owner decisions | [MACHINE-PROGRAM-HANDOFF.md](MACHINE-PROGRAM-HANDOFF.md) |
| What Codex fixed and played; the native-play receipts; the unfixed findings it left | [CODEX-MACHINE-HANDOFF-2026-11-21.md](CODEX-MACHINE-HANDOFF-2026-11-21.md), [CampPlay20261121](Verification/MachineSystems/CampPlay20261121/README.md), [SharedSourceStock20261121](Verification/MachineSystems/SharedSourceStock20261121/README.md) |
| Design principles | [SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md), [MACHINE-SYSTEMS-DESIGN.md](MACHINE-SYSTEMS-DESIGN.md), [MACHINE-SYSTEMS-EXECUTION-PLAN.md](MACHINE-SYSTEMS-EXECUTION-PLAN.md), [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md) |
| Every invented use (3a to 3w), considered-and-not-built | [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) |
| How to play-test each machine | [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) |
| The wording of every how-to block (for the owner's voice review) | [MACHINE-USE-TEXTS.md](MACHINE-USE-TEXTS.md) |
| Inventory redesign (plan only, no UI built) | [INVENTORY-DENSITY-REDESIGN-PLAN.md](INVENTORY-DENSITY-REDESIGN-PLAN.md) |
| Verification scripts | [Tools/CloneVerification/README.md](../Tools/CloneVerification/README.md) |
| Project identity and methodology | `Docs/PROJECT-IDENTITY.md`, `CLAUDE.md`, `ADVERSARIAL_TESTING.md`, `Docs/MCP_PlayMode_Testing_Strategy.md`, `Docs/PERF-FOUNDATION.md` |
| Claude project memory (diary of the same facts) | `/Users/steven/.claude/projects/-Users-steven-caves-of-ooo/memory/MEMORY.md` and the notes it lists (`machine-handoff.md`, `machine-program-state.md`, `clone-verification-chains.md`, ...) |

## 11. Honest limits (what is not established)

- **No complete full-suite verdict exists for the current head.** The last finished full verdict is for commit `eb12e00f9`. `fullrun_h` (head `e89a81af6`) was unfinished when this was written, and it cannot cover `019623bdb` or the scenario fix. Codex ran focused families (184/184 and 303/303 native cases, five mutants killed) and a bounded independent source review; those overlap and are not additive totals.
- **Little has been played.** Before 2026-11-21 only the camp box, hearth, bench, still and the world-map note had been played in the Editor. Codex then played camp placement, finite fuel, rest to dawn, mire settling, box custody, world-map travel and Continue, the proofing press and trough with gear, and a shared frost vent with a scraper and two trays. Everything else the program built (see section 6 step 4) is verified by EditMode tests, mutation checks and read-only reviews, not by a person at the keyboard. Fixture supplies are not evidence of ordinary acquisition.
- **The Codex play evidence is the operator's own report** (screenshots and exact IDs recorded by it); the independent review of it was of the evidence, not a second operation of the game.
- **Wording is placeholder** pending the user's voice review; numbers (fees, turns, degrees per unit, sound radius, regeneration rates) are proposals with no balance playtest.
- **All twelve complete machines remain unaccepted.** The camp kits, tools, proofing and readers are partial foundations. The capstan and culture work are parked; Steam Deck and Linux execution were never run; graphics are cancelled (all new blueprints are glyph-only); no new Mac build exists for the work since the last delivered one.
- **A foreign actor was editing the working tree while this was written** (section 1). The hourly Codex automation is not visible to Claude's tools, so any statement about what it will do next is a guess. Re-check `git status`, `git log` and `ps` before you trust a file's state.
- **Not acceptance of anything.** This document records state and intent. It does not close any gate.
