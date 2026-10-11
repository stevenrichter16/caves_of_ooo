# Place soundscape and combat variety

Status: implemented and native-verified, 2026-10-11; fresh Linux delivery pending below. Builds on the shipped world audio system; preserve its music and the nature recordings the player likes. Implementation proceeds through observed RED, native GREEN, adverse review and a real gameplay route. Qud is an atmospheric reference, not a source of copied recordings or a claimed hearing-system port.

## Player experience

Give each chunk an audible identity through three layers: a quiet habitat bed, occasional details separated by silence, and sounds tied to nearby visible objects or committed actions. A river bank should sound different from a dry field in the same biome. Night changes wildlife, entering a building shelters outdoor ambience, and a town's hearths and residents give it local character. Combat stays prominent and uses distinct weapon/impact families with multiple recordings.

| Place | Bed and intermittent detail | Source limits |
| --- | --- | --- |
| Spread / legacy jungle | Existing birds/wind by day; insects at night; occasional distinct bird phrases outdoors | Interior suppresses outdoor wildlife. Only actual visible flowing water replaces the broad field bed; still pools give local laps. |
| Sodden | Quiet wind by day, insects at night; wetland life and water near banks | No generic river everywhere, oil/acid never qualify as water, animal calls tied to appropriate actual wildlife where identifiable. |
| Grovelands | Forest by day, nocturnal insects after dusk, sparse branch/foliage movement | No invented Choir speech or crowd. Quiet gaps preserve unease. |
| Beating / desert | Low dry wind with long quiet intervals | No forest birds or cave drips. |
| Overwrit | Sparse wind with longer quiet intervals | Deliberately less activity, not a busy desert clone. |
| Stump / surface ruins | Exposed wind and restrained stone/wood creaks where real surfaces qualify | Underground remains the existing cave bed; surface ruins do not automatically imply dripping caves. |
| Underground | Existing cave bed, sparse locally justified details | No birds, nocturnal insects or fake animals inferred from the surface biome. |
| Sill, Sumphold and other inhabited places | Their actual biome outdoors; shelter inside, nearby hearths/water/wood and real interaction sounds | A forge's mere existence must not imply a working smith. No disembodied generic crowd in an empty town. |

Exact new recordings and supported detail IDs are finalized against licensed sources in AUDIO-PLACE-ASSETS.md. Source predicates and authored region evidence are in AUDIO-PLACE-SWEEP.md. Weapon/outcome design is in AUDIO-COMBAT-VARIETY.md.

## Verified implementation constraints

| Existing implementation | Consequence |
| --- | --- |
| WorldAudioPlayer has sixteen fixed sources, twelve for action sounds | Keep the same total. At most two idle action slots may carry environmental details; gameplay cues can preempt them. Ambient details obey Ambience volume, never the SFX control. |
| WorldAudioRuntime samples after FOV and only on graph, map, turn or resume changes | Extend the existing bounded neighborhood sample. No full-zone or world enumeration per frame; revalidate only retained source witnesses before playback. |
| WorldAudioLibrary currently fixes cue arrays at13 | Size from the append-only enum; load an explicit second expansion catalog so the original22 asset provenance stays intact. No asset loads in callbacks. |
| WorldClock.GetBand(tick), Cell.IsInterior and current wind fields exist | Use real time and shelter. Do not infer active rain from a previous spell or assume default wind means a weather system. |
| LiquidPoolPart is shared by water, oil and acid and has mutable Volume | Require positive water-like liquid and actual live visible membership; draining/hiding/removing a source must silence it. |
| FiniteCooking means stored heat, not a burning flame | Do not crackle solely because a finite cooking source is warm. Honor actual BurningEffect/fuel and the explicitly perpetual legacy campfire policy. |
| Existing spell audio owns spell outcomes | Preserve it. Melee variants must follow actual accepted swings and positive melee damage, with no fabricated blocked-hit/armor events. |

## Milestones and acceptance

1. Source new CC0 nature/object recordings and multiple distinct attack recordings. Retain full provenance, licenses, importer policy, duration/headroom/loop tests. No arbitrary fifty-pass target for this follow-up.
2. Add a data-backed detail catalog and bounded shared-pool playback. Test independent mutes, action preemption, missing assets, malformed IDs/gains, source loss, and no queue after unmute.
3. Add place/time/interior policy plus finite nearby sources, deterministic cosmetic spacing and differing chunk cadence. Test every source with its hidden/dead/drained/removed/out-of-range counterpart. No simulation RNG or state writes.
4. Add outcome-aware combat families and multiple variants; test real weapon families, missing/malformed weapon state, natural/unarmed fallback, successful/failed damage and spell silence.
5. Run complete affected native fixtures and actual in-game movement/settings/save-load checks, inspect live sources and capture audible output. Explicitly distinguish script state and signal checks from subjective listening and physical Deck testing.
6. Review symmetry, doc/runtime agreement and ownership; fix significant findings. Update living docs, commit scoped changes, fetch/rebase/push authorized main, and deliver a distinct current Linux build.

## Scope and review

🟢 Existing imported music, lifecycle, controls and outcome seams are usable. 🟡 Existing ambience is broad biome-only and arrays are hard-coded; both need expansion. 🟡 Source availability and town activity must be verified before attaching sounds. ⚪ No new gameplay hearing/stealth, voice dialogue, weather simulation or hidden enemy radar. Subjective mix and Deck hardware results will only be claimed if actually observed.

Sweep correction before production: actual watery liquid IDs are water/brine/bog-mire; invented brackish_water is rejected. A lone puddle may supply a quiet local lap but only actual flow tags or authored cascade pools justify the river bed. Stored-heat CampfirePart alone never establishes flame; the legacy decorative-fire exception also needs current thermal/fuel checks. Initial RED is a native missing-type compiler failure; assertions did not run.

## Implemented performance bounds

Follows `PERF-FOUNDATION.md`: discovery runs inside the existing context boundary, at most169 nearby cells and512 owners. Idle inspects seven retained source categories and at most two audible source witnesses; it never discovers new hidden sources or allocates collections. The same sixteen voices remain, with at most two environmental details sharing the twelve action slots. Actions can reclaim those voices. Assets load once at runtime initialization; biome/time policy and scheduling use only cosmetic state, never simulation RNG. Native route confirms no idle rescan or source-pool growth; this is not a measured FPS improvement or Deck benchmark.

## Review, corrections and results

- Initial native compiler RED established missing APIs; assertions did not run. Subsequent `native-red.xml`:118 cases,98 passed,20 failed. Sixteen place-source failures, three combat HP-callback failures and one missing expansion-credit failure were observed before correction.
- Source witnesses now survive snapshot replacement; hidden, dead, removed, drained, destroyed and foreign-owned objects cannot keep details playing. Structural Gone/HP and animal Brain/HP are checked. Raw shelter changes retire outdoor texture without scanning. Actual source-dependent river audio stops immediately when its water source becomes invalid, including menu entry; regional music stays independent.
- Combat audio measures the final HP decrement after callbacks, leaving gameplay/logging snapshots unchanged. Misses produce swings only; generic spell damage never invents weapon impacts. Existing guaranteed-hit skill helpers remain outside the regular strike hook.
- The first repair run passed all31 independent place-adversarial and52 combat cases. Two older synthetic frog fixtures lacked the real animal Brain premise and were corrected without weakening production checks; `native-fix-fixture-red.xml` preserves that distinction.
- Final actual Unity EditMode: **1478/1478 passed**, zero skipped/failed,49 fixtures, including119 new cases. Receipts: `Docs/Verification/PlaceAudio/broad-green.xml` and `broad-green-fixtures.json`. Separate source review found no remaining material regression in place authority, shared buses or combat HP/contact handling.
- Real isolated Play route: **22/22 passed**, zero unexpected errors, run `c0fd56e92d294e4b8593d3c2eb5a234a`. Receipt and three screenshots: `Docs/Verification/WorldAudio/Native/c0fd56e92d294e4b8593d3c2eb5a234a/`. A real wood detail played from the generated glade, snapshot visited222 owners and retained2 sources; idle did not rescan or grow the16-source pool. Music/ambience mute controls, footsteps, music continuity, save/load, exact preference restoration and synthetic-device removal passed. The settings screenshot was visually reviewed.
-31 new clips (2 beds,15 place phrases,14 attack takes) add1,319,315 compressed bytes and5,344,120 decoded short PCM bytes. All31 decode/import contracts,5 offline asset tests,16 offline adversarial checks and15 packaging checks pass. Original22 clips remain unchanged; rebuilds match with the same codec. Credits include original and expansion sources both in-game and in the package.
- Limits: native signal windows show nonzero, below-full-scale output in those windows only. They do not establish subjective listening quality, all-biome transitions, physical speakers, overall clipping or Deck performance. SummitSinger needs a suitable single-note recording; CascadeFather stays silent per lore. Working-smith/cooking sounds are deferred until an authoritative active-work event exists. No synthetic crowds, hidden-enemy radar or new hearing system.
