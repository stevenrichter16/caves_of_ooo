# Codex machine-program handoff — November 21, 2026

Read this before the older [Claude handoff](MACHINE-PROGRAM-HANDOFF.md).
The user transferred repo/Unity/Git control to Codex, requested the shared-resource
fix, completion of regression assessment and actual machine/camp play, then added
an inventory density redesign **plan**. Latest instruction: write this handoff,
commit and push to main. Resume the unfinished work below without asking again.
The hourly machine task remains ACTIVE. Graphics is cancelled; cultures/capstan
are parked, Deck deferred. All twelve complete-machine gates remain open.

## Published work in this handoff commit

Parent main/origin/main independently matched `bc6e00c06eccb41bf50c042a5fbca15e304ceddd`.
Runtime inherited from Claude is `e89a81af6`. The commit containing this document
adds three bounded corrections, native receipts, gameplay observations and the
inventory plan; it does not accept the entire machine program or a new Mac build.

1. `ProofingSources.StockHolder` previously truncated connected-source search at
   400 cells, creating independent budgets on large pools. It now covers the
   finite 80×25 zone, examines eligible co-located owners, validates stock
   parentage and prioritizes existing saved stock holders before new canonical
   election. Multiple legacy holders are not summed; secondary holders stay
   dormant while connected. Splitting topology/removing the elected holder can
   expose old budgets: that migration is outside this correction.
2. `CampfireWorkActions` declares OneAction on its five executable work rows.
   The two newer commands, Stoke and FeedFull, previously mutated resources for
   zero ticks and bypassed the outer command transaction. Old feed/relight/dry
   behavior still costs once; disabled rows/rest are unchanged.
3. `ProofingPressActions` declares OneAction on enabled press rows. Presses were
   free despite the explicit one-action design. Coating already charged correctly.

Native evidence: source-family final **184/184**, combined fixes **303/303**,
then **5/5** after strengthening the press assertions to re-read the current
owned flask Part. These overlap: do not add totals. Native REDs reproduce large
geometry, saved legacy holders, missing hearth timing/outer rollback, and free
presses. Two stock mutants, two hearth mutants and one press mutant are killed.
No compiler errors in those accepted runs. Source and selected native evidence
received bounded independent review with zero remaining P1/P2. The reviewer did
not operate Unity; evidence review is not independent game operation.

Durable receipts:
- [Shared-source correction](Verification/MachineSystems/SharedSourceStock20261121/README.md)
- [Camp/workshop play and timing](Verification/MachineSystems/CampPlay20261121/README.md)
- [Inventory redesign plan](INVENTORY-DENSITY-REDESIGN-PLAN.md)

## Actual play completed

SampleScene/GameBootstrap, seed64, authored fixtures, ordinary keyboard input
through the real inventory/C menus/readers. Read-only observations recorded
state; no direct gameplay method replaced an action. NativeSaveIsolation owned
temporary save roots. Fixture supplies are not ordinary acquisition evidence.

- Camp: move off road; lay four same-ID kits; light finite fuel; rest Dark→Dawn
  (16→40 HP, fuel30→15); relight/feed; settle12 mire→3 water; store dagger stack
  in box; name Home/world-map contents note; move-picker cancellation; world-map
  travel away/back; fresh Play/Continue; pack up with the same cargo/fixture IDs.
- Fixed costs played: FeedFull1340→1350; workshop Stoke60→70; press90→100.
- Workshop: 71 item entries/77 rows, current renderer only twelve rows. Coat
  one cloak from a stack, permanent proofing across multiple flasks, F1 treatment
  disclosure, equip and frost tonic. +50 innate/+35 coat/+25 lasting prevented
  freezing; capped display100 and no FrozenEffect. Generic tonic flavor still
  says it freezes you after the shrug-off line: wording issue remains.
- Frost vent2389: scraper plus trays2134/2135 at33,8 and33,9 share one holder.
  Five rime flakes total exhaust5 stock; extra scrape refused without a turn.
  Ordinary waiting was interrupted by a viper, defeated with equipped dagger
  and bump combat. No debug invincibility/healing/time advance. Catch NoTrade.
  F5/fresh Play Continue retained stock0/5, LastTick190 at tick910, HP333.
- Pressing a harvested flake advanced910→920. An apparent lost-output concern
  was an aggregate observation error: RimeFlake's menu puts press rows before
  Examine, unlike FrostLichen; Down selected a part-filled1→2 recipient.
  No conversion production defect was confirmed. Retain the stronger tests.
- CampMachines group picker/F1 return and set-down worked. Attempted move onto
  unsuitable ground refused without payment. Steam processing did **not** run:
  the fixture failed to supply flasks (next section). Do not count this as heat
  qualification. Successful move, fish trap, awning, rod, spring trap, Posy,
  carriage and full machine/consumer combinations still need native play.

## Immediate next steps / unresolved findings

1. **Finish inherited full regression assessment.** Claude's final `fullrun_h`
   runs in `/Users/steven/coo-verification/clone4`, PID85151 (check exact process
   before acting). Started13:42Central; still running at handoff (~3h15), XML
   absent. Log/results `/Users/steven/coo-verification/results/fullrun_h.log/.xml`.
   Do not sync or start another job in clone4. It tests inherited e89 runtime,
   not these fixes. Old clone2 `fullg` was terminated guarded, with logs retained.
   Once XML exists:
   `python3 Tools/CloneVerification/compare_full.py /Users/steven/coo-verification/results/fullrun_h.xml`
   Baseline is full_s9.xml (39,500 tests,91 known failures). Rerun **every NEW
   failure alone** in idle clone3 before classification. Do not promote baseline.
   Full regression and native play overlapped; shared test preferences can race,
   so isolate suspected order/timing failures with main Play stopped.
2. **Fix CampMachinesShowcase's incomplete loadout with native RED first.**
   Actual default pack has `MaxWeight=150` while sidebar reports strength carry
   capacity270. Giving the long loadout truncates at two timber; no striker,
   torch, five flasks or repair components arrive. Actor at43,10, still44,10 had
   valid heat2166 at46,12, but no flask actions because the pack held no flasks.
   `CampKitShowcaseTests` around273 sets MaxWeight400 before applying the fixture,
   hiding this. Add a 150-cap/default-kit countercase and make the scenario
   explicitly accommodate its promised test loadout (Workshop already does).
   This is **unfixed**, no new RED or source edits yet. Do not change ordinary
   production carry limits to repair a fixture. Capacity display mismatch can be
   assessed separately. Restart the corrected fixture; no ad-hoc item injection.
3. Finish native play from [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md), including
   vent heat/bake refusal, fish catch, awning, sounding, traps and town/freight
   systems. Build/verify a new Mac at a coherent milestone, preserving old builds.
4. Inventory plan targets44 items at1280×800,64 at1080p with full-height columns,
   category filters and compact details. It includes stable selection/navigation,
   minimum name width, larger text fallback and disclosed flask/treatment state.
   Independent plan critique incorporated. **No inventory UI implementation.**

## Safe continuation / current workspace

- Main Editor is stopped, SampleScene clean, SaveRootOverride null. Temporary
  keyboard settings/devices removed; owned input callbacks drained. RequestedSeed
  and LastLaunched restored to their captured prior values. No main tests run.
- Main instance `caves-of-ooo@4f5664cceff9ea5e`; pin it before Editor operations.
  Use warm clone3 for native tests: `Tools/CloneVerification/clone_run.sh` and
  `each_mutant.sh`; see their README. Never test in main Editor.
- **Wait for actual assembly reload completion before Play.** A false compiling
  flag immediately after external edits can precede delayed reload. Force refresh
  while stopped, check assembly timestamps and editor-state domain-reload status.
  A reload during Play cleared runtime bindings; those attempts were discarded.
- `execute_code` is CodeDom/C#6. InputHandler namespace is
  `CavesOfOoo.Rendering`. Game screenshots: ScreenCapture.CaptureScreenshot,
  not camera-only captures. Avoid OS screenshots of unrelated foreground apps.
- Native input used a temporary virtual Keyboard, cloned InputSettings and
  onAfterUpdate Dynamic callbacks. Timed key edges need >0.12s between presses.
  Paid pack actions close inventory; free equip does not. Inspect modal/selection
  state before continuing. Check setup return values before dependent actions.
- Preserve all foreign files. Baselines outside repo:
  `/private/tmp/coo-shared-source-foreign-baseline.json` (84 tracked foreign files),
  `/private/tmp/coo-machines-normal-state-baseline.json` (1,226 persistent files).
  Last checked:84 foreign files and861 normal game files unchanged. Unity analytics
  and TestResults.xml naturally changed; no whole-tree byte-exact claim.
- Earned temp checkpoint backups (not committed):
  `/private/tmp/coo-machine-camp-earned-20261121`,
  `/private/tmp/coo-workshop-earned-20261121`,
  `/private/tmp/coo-workshop-spent-earned-20261121`.
  Last one has game8ab6a6384c2a44deb8b3a68303809d8f at910 with depleted source.
  Copy into a newly owned isolation root for Continue, never normal saves.
- Host32GiB/10logical CPUs; full gate and Editor competed for resources. Targeted
  test durations are not FPS/performance or hardware acceptance measurements.
- Review/stage explicit owned files only; fetch/integrate safely, push main and
  verify the remote hash. No storage reclamation or build deletion authorized.
