# E2 bounded ambush save lifecycle

Current status: published narrow repair; actual Unity127/127 lifecycle subset passes,
including native pre-fix bytes. Source/art and native encounter gates are recorded
separately in Source/ and ../E2Art/. The chronology below retains historical
private/pending statements; it does not supersede this current result.

## Historical preflight

Private only. No E2 blueprint, art or source activation. Goal: actual save/load of prospective ambush Viper using current AIAmbushPart/DormantGoal, plus real ordinary Viper and existing ambusher controls. Root owns publication and native gate.

Verified premises: AIAmbushPart._dormantPushed, DormantGoal._wakeRequested and _lastHp are private. SavePart writes public fields except explicit known handlers; SaveGoal writes public fields and bypasses constructors when loading. Brain.RestoreGoalsForLoad restores stack identity without invoking OnPush; loaded Part.Initialize is not replayed. Therefore duplicate sleep, erased explicit pending wake and lost pre-save damage baseline are concrete hypotheses requiring execution, not yet acceptance findings.

Test before any candidate: asleep exact-one goal before/after native TakeTurn; fully awake does not rearm after load; pending external/damage wake remains pending; ordinary awake Viper remains without AIAmbush; existing AmbushBandit/SleepingTroll/MimicChest saved stacks retain config. No combat RNG outcome, body stats, world content or gameplay grants used. Setup attaches AIAmbush only to a fixture Viper and disables sight wake/particles to isolate persistence.

A narrow compatible candidate, if RED confirms the hypotheses, would retain only the private lifecycle state through additive named public save fields and conservative old-save absence handling. It must not globally change SaveSystem, replay Initialize/OnPush or rearms, rename/remove saved fields, rebuild saved actors, or activate E2. Older readers cannot read new fields in the generic format, so forward loading old saves is the compatibility claim; old binary reading newly written saves would not be claimed. Escalate complexity or ambiguous legacy state to root before publication.


## Executed decision

Actual private standalone preflight: **9 cases, 7 RED / 2 controls PASS**, raw `red.xml`, full messages `red-summary.json`. Sleeping prospective Viper and all three existing ambushers gain a second DormantGoal after first restored TakeTurn. Fully awake Viper rearms after load. Pending explicit Wake loses Finished state immediately at restore. Damage between last sleep action and save no longer wakes after load. Ordinary Viper remains ordinary, and a no-damage sleeper does not spuriously wake.

Recommendation: narrow lifecycle repair is justified, rather than deferring E2 solely for this defect. Limit production to AIAmbushPart and DormantGoal; do not change SaveSystem globally. Persist additive scalar latch, prior-HP baseline and pending-wake state. An explicit presence/version scalar is needed for legacy AIAmbush absence vs newly explicit Rearm: old saves with an already restored brain should conservatively be treated as already armed, keeping existing sleep goals or existing awake state rather than adding another sleep. Test deliberate Rearm separately so it is not erased by compatibility inference. For old goal bytes, pre-save pending wake/private prior HP was never present and cannot be reconstructed; document that legacy limit rather than fabricate it. New saves must preserve all three states exactly.

Next required gates before E2 admission: old-byte fixtures captured using current baseline, pending Rearm, no-brain late attach, repeated save loops, replacement current brain, same-parent saved stack references/configuration/age, ordinary viper and existing ambusher controls, focused native tests. No source/data/art publication performed here. Parent asked that the M1 native route be prepared next, so this preflight is a completed decision with repair queued separately.

Adversarial fixture correction: the first 41-case candidate run was40PASS/1FAIL because its added child ParentHandler assertion presumed an unrelated generic serializer feature. SaveGoal explicitly excludes ParentHandler; dormant ambush roots do not use it. Removed the synthetic child setup and retained exact root config/age/ParentBrain/zone assertions. No generic goal-stack/schema expansion.

## Private implemented bounded repair

The two private production files now persist additive public lifecycle scalars through the existing generic serializer. `AIAmbushPart.DormantPushed` is versioned using `AmbushSaveStateVersion` written by the normal before-save hook. A legacy restored brain is treated as already armed, whether its actual saved stack is sleeping or awake; a brainless owner can still arm when a brain is later attached. New explicit pending Rearm=false survives the saved presence marker. `DormantGoal` persists pending `WakeRequested`, `LastHitpoints` and `HasDamageBaseline`; a legacy missing baseline waits for one current sample. No SaveSystem or goal reconstruction changes.

Compatibility: old saves can be loaded by this candidate, including real unchanged baseline bytes in the portable compressed fixture. Pending Wake/prior HP that the old serializer never wrote cannot be recovered. The old generic binary reader cannot skip unfamiliar public fields, so old game builds reading new saves is not promised. No new `ParentHandler` persistence is added. These are baseline serializer limits, not a claim of full old/new binary interoperability.

Executed evidence: original 9 = 7 RED/2 controls →9 GREEN. Dedicated same-corpus 41 =23 PASS/18 RED before,41 GREEN after. Broader current 127/127 GREEN includes existing ambush, Phase6 goals, Tier1 brain save and lair population tests; exact broader before109PASS/18RED versus current127PASS/0FAIL, same127names, newly failing0. The 18 adversarial cases include repeated sleeping/awake/pending cycles, deliberate Rearm, unsampled/healing/no-damage controls, late-brain attachment, config/age/current-brain identity, real legacy asleep/awake bytes for Viper and three existing ambushers, legacy unrecoverable-state boundary and ordinary Viper refusal. Actual Unity-reference runtime and full test assembly compile with zero errors; this is not a native EditMode run.

## Q1–Q4 / adversarial closeout

- Q1 symmetry: fresh Initialize/TakeTurn latch behavior remains exactly once; save/load preserves both true and explicit false states. Awake versus sleeping and Rearm versus no Rearm are paired. Pending wake survives before cleanup; no unnecessary OnPush/Initialize replay.
- Q2 cross-feature: existing AmbushBandit, SleepingTroll and MimicChest use the same repaired component. Ordinary Viper has neither new part nor goal. Public scalar saving follows existing named-field serializers; no global save hook, factory schema, damage or AI dispatch changes.
- Q3 counters: WakeOnDamage true/false, old asleep/awake, legacy no-state/current presence, no-damage/healing/damage, no-brain/late-brain, explicit Rearm and ordinary Viper controls execute. Actual old bytes prevent a fake legacy test made by writing new fields with false values.
- Q4 drift: this closes a save lifecycle defect shared by existing ambushers, not admission, art or balance of the proposed new E2 encounter. Native run is pending. No gameplay route or visual result is claimed. Generic ParentHandler omission and unrecoverable old private fields are explicitly bounded.

The private `manifest.json` declares exactly two runtime files and three new test/support files plus their fresh metas. Root owns any shared publication, native run and E2 content decision. No Unity calls or user-save access occurred here.

## Native wire-corpus correction (pending recapture)

The first native142 selection found eight `Entity.End` parser failures only in the legacy-byte fixtures; current lifecycle and other selected controls passed. Those embedded old bytes were generated by standalone adapters and cannot establish native wire compatibility. Root is capturing the same nine synthetic pre-repair states with actual Unity and only the original two AI classes temporarily restored, with exact source/environment restoration. Native and standalone corpora will remain separately labelled; no serializer/gameplay change follows from this test-data mismatch. Broad native compatibility remains pending.

Actual Unity pre-repair capture completed nine synthetic files and the same7RED/2controls. Root restored both current source hashes and cleared the capture environment variable unconditionally. `dual-runtime-legacy-manifest.json` records exact native and standalone bytes; one helper candidate selects its own runtime's true pre-repair wire corpus. No global serializer fix or production change is proposed. Native rerun of this corrected support data remains pending.

Independent bounded peer review: combat_density read both runtime diffs and found no compatibility/initialization blocker. The reviewer specifically verified versioned absence vs explicit false, constructor-bypassed old goal load, and the no-brain late attachment path. No whole-save format or native success was inferred from that source review.

## Actual final native GREEN

`../source-art-save-paging-01.xml.gz` executes all127 scoped lifecycle+nearby cases PASS in actual Unity, including corrected native old-wire corpus. The broader238 batch had234PASS/4 unrelated failures; it is not claimed wholly green. Separate inventory15 controls also passed according to the parent’s by-selection extraction. First native corpus mismatch and genuine pre-repair native9 (7RED/2controls) remain archived. This closes the bounded shared ambush save defect. Warned-viper source/appearance/poison/bypass acceptance remains separate, not enabled or claimed by this repair.
