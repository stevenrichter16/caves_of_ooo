# New-game save isolation — A06N / GA03b

Status: COMPLETE; baseline7c7d87a7,9097GREEN →9168/9168GREEN (+71). CoO-original
session consistency repair. W6 and FLOW1–6/GA02k are complete; this advances the
remaining whole-game audit. A07hotbar and A08decode atomicity stay separate.

## Player contract

Choosing N starts the already generated fresh world, immediately binds that world's
fresh game ID, and attempts an initial Quick checkpoint. Subsequent F6/pause Load/
death-L must reference the new character. Failure to capture/write must retain the
fresh binding and allow play with an explicit autosave-failure/F5-retry message.
Continue C still addresses the previously discovered save until the player chooses;
C wins simultaneous C/N, and a failed C stays at the boot menu. Old save/metadata/
backup bytes remain unchanged. This repairs load binding, not an old-file overwrite
(the existing first successful F5 already writes the fresh captured ID).

## Pre-implementation verification sweep

| Source | Verified behavior / decision |
|---|---|
| GameBootstrap41/669–703/829 | `_gameID` is freshly generated independently of capture. Runtime registers, discovery binds previous ID, boot menu is offered, and initial autosave is skipped when old Quick exists. Register the fresh ID independently of capture; preserve discovery until choice. Use new-session operation also for no-prior-save startup. |
| BootMenuController82 | N merely deactivates and logs. Add explicit service new-session dispatch; deactivate after handling success/failure and provide truthful save status. |
| SaveLoadInputAdapters22 and ISaveLoadService | All three load controls share the same service. Add an explicit new-session method and production adapter; update five existing fake-service fixtures to preserve their old test semantics. |
| SaveSystem.SaveGameService536–578 | Capture occurs before save's exception handler; null/missing callback returns before rebinding. Bind the independently registered fresh ID BEFORE capture/file access, and catch initial capture failure. Never rediscover an old slot as failure recovery. |
| SaveSystem645/650 and discovery503 | Save/metadata paths and boot discovery hardcode persistent/Saves. Isolate tests/native arenas using a common scoped root seam covering every path, not only writes. |
| Save data/metadata writers | Each file is atomic separately; save=false may still leave a new Quick data file if metadata failed. Do not promise pairwise atomicity here. |
| 21native marker launchers | N will now checkpoint bootstrap's fresh GUID outside their marker directory unless root isolation is widened. Adapt all21 under a shared disposable save-root helper that survives Editor domain reload and restores/flushed previous preference; delete only that root. |
| FoundingVillage/FellingSite/SealedLibrary players | Three pre-N RegisterRuntime(null,null) calls would deliberately disable new autosave. Remove those suppressions after the new scoped-root isolation lands. Keep ordinary end-of-run unregistration. |

API design to finalize before first production edit: RegisterRuntime receives an
optional independently known new-game ID (bootstrap supplies `_gameID`), new-session
operation binds it before capture and refuses mismatched captured identity without
changing the binding. Missing registration needs a safe fresh fallback binding,
never previous ID. Avoid double capture. Factor captured-state writing only as
needed; existing F5/save paths keep behavior and gain no unrelated save-schema changes.
The save-root seam is only for isolated runtimes/audits; normal sessions default to
the existing persistent Saves directory. Native helpers must restore the override
across Editor domain reload and retain it through cleanup.

## TDD and verification plan

First source-backed REDs: N→immediate F6/pause/death restore fresh B while old A bytes
remain identical; Continue/collision controls; missing/null/throwing capture and
write-failure retain new binding; later F5 saves/reloads B; repeated/inactive N does
not mint another ID/checkpoint; no-prior-save startup despite stale static binding.
Test new public boundary with actual serialized states as well as controller fakes.
Then minimumGREEN, dedicated20–60adversarial cases (identity mismatch, capture
mutation, metadata failure, root/pref cleanup, late load/save refusal), independent
cold-eye, native actual keyboard N/save/reload and C control, full suite, living docs.

Performance: saving is existing explicit I/O; one initial checkpoint is added only
at start choice/no-save startup. No per-frame allocation or rendering optimization
claim. Native report must distinguish queued-keyboard behavior, serialized state and
file/pref isolation from physical input/pixels/subjective feel.

Protected source files: GameBootstrap.cs and SaveSystem.cs contain unrelated existing
animation/save changes. Save before copies; stage only independently attributable
hunks that apply against HEAD. Do not adopt protected animation work. A31's exact
working-tree camera guard remains applied and is separately recorded as a patch.

## Final API and review corrections before RED

| Review/source correction | Decision before implementation |
|---|---|
| Domain reload disabled; old static runtime metadata can survive | Every RegisterRuntime(capture,apply,newGameID=null) replaces candidate metadata. Cache one fallback GUID per registration/absent-registration start. Registration itself must preserve active Continue binding. |
| Capture may fail or mutate active binding | BeginNewGame binds a locally frozen candidate before one capture, validates identity, and restores that intended binding through failure. Result means checkpoint success, not whether the fresh world starts. |
| Existing SaveSlot trusts each captured ID | Keep ordinary F5 identity behavior; initial mismatch refusal does not promise to override a permanently wrong future capture callback. Correct bootstrap capture supplies the intended ID. |
| Root seam spans assembly boundaries | Public documented SaveRootOverride, null/empty uses normal persistent Saves; resolver used by all implicit-root paths/discovery. Explicit-root discovery keeps its contract. Callers restore previous override. |
| Existing native Finish cleans before Play stops | Shared Editor helper owns isolation separately until EnteredEditMode, restores/flushed prefs then prior root, deletes only its generated temp root, and exits afterward. Reject concurrent owners. Reapply root synchronously after reload before bootstrap subscriptions. |
| QuickLoad captures separately for factory | Capture-count assertions end before loading; load's later capture is expected. Blank state-ID tests assign after GameSessionState.Capture, which otherwise fills a GUID. |

Initial tests use actual serialized A/B states and production adapter with isolated
unique slots even before the root API exists. Snapshot/restore runtime fields,
preference existence/value, log/reputation and turn state; delete only owned paths.
A no-op production stub must not satisfy New Game→F6/pause/death identity assertions.

Initial18 RED:2Continue controls pass,16fail;08:01:48–49UTC,.2782619s,0CS.
Observed N leaves old active binding/no checkpoint; missing new-session/root APIs
fail explicit boundary assertions. Before minimum implementation, independent review
caught fixture write-blocker cleanup and construction-failure cleanup; corrected
to delete owned files as well as directories and unwind partial construction.

Minimum85/85GREEN,08:04:14UTC,.7823219s,0CS. Dedicated35 + regression18
then53total:52pass/1recursive initial-checkpoint failure,08:09:17UTC,.6014114s.
A capture callback could recursively checkpoint twice; add an operation-in-progress
guard released in finally. Real graph controls verify loaded player/cell/turn aliases.
Metadata-failure controls explicitly allow new data without metadata/pref update.
Native isolation helper12boundary tests authored before its implementation.

Native isolation boundary RED47:35service-adversarial pass/12missing-helper
failures,08:11:09UTC,.5869419s,0CS. Shared helper added with ownership token,
all-save temp root, synchronous reload restore, deferred Play cleanup and pref flush.
21launchers adapted, including old4manual-stop unsubscription and special flags.
Expanded132/132GREEN,08:14:12–13UTC,1.1044306s,0CS. Adaptation script stopped
before three pre-N suppressions because their input enum is Key.N; removed those
exact calls afterward, retaining their other startup/input actions and teardown.

Independent lifecycle review found two helper defects. Cold-eye RED53:51pass/2fail,
08:17:08–09UTC,.6032626s,0CS: thrown manual-stop callback skipped cleanup; external
quit restored the real root before remaining shutdown callbacks. Manual stop now
logs failure and completes cleanup; external quit unregisters saving and keeps the
disposable destination selected for process shutdown while restoring prefs and
cleaning its owned files. Ordinary requested exit still restores after Play ends.
Added4direct Begin retries and exact fixture callback/candidate/guard restoration
assertions; all those controls passed before these helper fixes.

Cold-eye corrected138/138GREEN,08:18:38–39UTC,1.0939924s,0CS. Preliminary native
New13/13 and Continue11/11 passed; review replaced a tautological directory assertion
with exact owned paths plus absent unique IDs under the normal save root, and fixed
failure-mode checkpoint wording. Preliminary raws retained separately. Strengthened
New15/15 passed with actual OnDestroy observations: root stayed disposable and save
runtime was unregistered during normal completed shutdown. Post-exit root removed.
The added shutdown duration used Unity realtime, which resets across Play exit and
yielded a negative value; this timing field is invalid. Replace the observer clock
with monotonic Stopwatch before final native evidence; no gameplay/timing result
is inferred from that field. Three remaining modes are already running serially.

Final native matrix after Stopwatch correction:61/61PASS (New15, Continue13,
Empty13, Failure20); all0CS/exit0, shutdown observed with nonnegative monotonic
durations, exact unique IDs absent under normal Saves, and all4owned roots removed
after process exit. Native compatibility: Craft Receipt25/25PASS (7.88173625s),
Founding Village8/8PASS; both0CS/exit0. Prior report bytes restored after
archiving compatibility outputs under GA03b; this headless run produced no screenshot.2430AssetGUIDs/0collisions.
Independent final review0remaining🟡+; prepared SaveSystem/Bootstrap patches apply
against HEAD and exclude protected FX hunks. Full suite running, expected9168.

Final full9168/9168GREEN,08:31:11–08:33:27UTC,135.5073505s,0CS.
All71new cases,61native startup/shutdown checks and33native compatibility checks
pass. Self-review fixes and evidence bounds are in GA03b-REPORT.md. Commit includes
only owned whole files and independently applicable SaveSystem/Bootstrap hunks.
No protected animation work adopted. Next: prepared A07hotbar save selection.
