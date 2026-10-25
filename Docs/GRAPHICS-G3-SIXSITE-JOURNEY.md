# G3 — Connected six-site service journey

2026-10-25 UTC. **Bounded ordinary six-site continuity accepted from an earned
checkpoint.** This pass connects the existing native sites through a real repair,
introduction, gathering permission, harvest/replant and return. It does not
complete the fresh-New-Game release route, all opening artwork, a new package or
physical Steam Deck acceptance. The preserved current package remains the
October11 build.

## Purpose and verification sweep

Follow the existing route in [GRAPHICS-COMPLETION-PLAN.md](GRAPHICS-COMPLETION-PLAN.md)
instead of adding disconnected assets or hunting random combat outcomes. Continue
the accepted frost-tempered/crop branch from
[GRAPHICS-G3-FORGE-CROP-JOURNEY.md](GRAPHICS-G3-FORGE-CROP-JOURNEY.md), repair Orven's
pan, relay his introduction to Nella, use the newly granted reserve, and return
to the planted kitchen bed. Record what actually changes and retain mistakes.

| Premise | Confirmed contract or correction |
| --- | --- |
| This is a fresh new game | No. It continues the unmodified earned seed64 checkpoint at kitchen25,14,42HP,tick2990. Prior cellar, forage, quench and planting work remains part of that character's history. |
| Any intact pan earns an introduction | No. `CookIntroductionPart` requires the actual actor's recorded repair, current cook/pan bindings and local access. The observed introduction preserves the same repair cause. |
| Nella's rights always cost8drams | Her normal introduction action grants Unknown→Granted without that payment. The observations remain at50drams before and after. |
| The second kitchen crop must be watered again on return | The previously dry13515 is watered before departure and is ripe on return. This pass observes growth across the ordinary journey, not a forced stage or clock change. |
| Visiting each zone accepts every service and item there | No. East/north/cellar are traversed or revisited in this pass; earlier forge/brew/loot acceptance is not replayed or counted as new here. |
| The route was a clean shortest-path walk | No. Real wall/hedge collisions and incidental strikes occurred; see the route notes below. |

Source semantics were checked in `CookIntroductionPart.cs` and
`LocalGatheringClaimPart.cs` under `Assets/Scripts/Gameplay/World`. This is an
original CoO service connection in the approved visual style, not a new Qud
source-parity claim.

## Ordinary launch and route

Root used ordinary CUA keyboard controls in real
`Assets/Scenes/Main/SampleScene.unity`. The launcher copied the unmodified earned
character folder into an owned disposable save root and selected seed64; it did
not force the art presentation. The01:31:22UTC launch receipt reports
`forcedPresentation=false`. Continue resumed the character. The gameplay route
used no teleport, item/health grant, forced RNG or direct simulation mutation;
the accompanying observation scripts only read state and capture output.

The initial route contains20 paired `sixsite-*.png/json` observations. The later
reader/Continue checks add six more, indexed in `sixsite-play-summary.json`. Every sampled state reports
flat presentation,42HP and50drams. All six distinct addresses occur in those
receipts. They are samples, not a continuous video or a complete disclosure of
every entity in the diagnostic object lists.

| Step and exact receipt stem | Zone / player cell | Tick | Actual result |
| --- | --- | ---: | --- |
| Earned start, prior `kitchen-crop-replanted-reloaded` |12.11.0 /25,14|2990|Dry Claspbean13515,4pulp and2fire clay carried;2699 blade equipped. |
| `sixsite-pan-before-repair` |12.11.0 /28,17|3090|Rain has watered13515; stage0,10units,30moisture. Pan13489 still split; clay4716×2 remains carried. |
| `sixsite-pan-repaired` |12.11.0 /28,17|3100|Pan becomes repaired and clay is consumed. Screenshot records30XP for restoring the service. |
| `sixsite-orven-introduction-choice` / `-earned` |12.11.0 /27,18|3120→3130|Normal action asks Orven to vouch; learned knowledge identifies the actual cook, pan and repair. |
| `sixsite-east-arrival` |12.10.0 /26,24|3330|Ordinary northern boundary exit from kitchen reaches the east connector. |
| `sixsite-glade-arrival` / `-court` |11.10.0 /79,24→40,15|3600→4080|Westward return reaches the glade and its pond/working area in native flat presentation. |
| `sixsite-cellar-revisit` |11.10.1 /40,12|4360|Ordinary descent reaches the actual stairs; return upstairs follows. No new cellar loot/growth transaction is claimed. |
| `sixsite-north-works-arrival` |11.9.0 /40,24|4730|Crosses into the north connector on the way to Nella. No new brew is claimed. |
| `sixsite-nella-arrival` / `-terms` |11.8.0 /40,24→20,9|5010→5360|Nella's reserve begins Unknown; terms distinguish the tied beds from public beds. |
| `sixsite-nella-introduction-accepted` |11.8.0 /20,9|5370|Relay grants reserve access with50drams retained. |
| `sixsite-pitchpod-harvested` |11.8.0 /18,7|5420|Ripe15729 is removed; inventory contains resin15732×2 and saved seed15734×1. |
| `sixsite-pitchpod-replanted` |11.8.0 /19,6|5450|Seed is consumed into replacement15735 at18,6; Rain leaves it stage0,2units,38moisture. |
| `sixsite-works-return` / `-glade-return` |11.9.0 /40,0 →11.10.0 /40,0|5850→6100|Ordinary return crosses the same northern sites. |
| `sixsite-east-return` / `-kitchen-return` |12.10.0 /0,15 →12.11.0 /26,0|6650→7010|Crosses east and then south back into the kitchen. |
| `sixsite-kitchen-crop-revisit` / `-reader-before` |12.11.0 /25,14|7160|Same13515 is ripe; repaired pan and introduction remain intact. Opening Examine does not advance the sampled tick. |

All shortened surface zone addresses in the table have the `Overworld.` prefix.
The route begins and ends at kitchen25,14, advancing2990→7160 world ticks through
actual actions and travel. It is not an estimate based on prescribed key counts.

## Exact state and causal continuity

**Repair and introduction.** Player2695 repairs pan13489 at29,17 using recipe
`clay-batch-pan`. Cook13471, Orven Kett, is at27,19 in the recorded states. The
pan's `repaired` flag changes false→true and `by` becomes2695. The repair cause
is **`ed272954d6e94cedbb3937df123dba5a`**, zone`Overworld.12.11.0`.
`sixsite-orven-introduction-earned.json` records speaker13471, pan13489 and that
same cause/site. This knowledge remains present in every subsequent observation.
The pan remains Idle with no output; this pass does not commission a meal.

**Permission.** Nella15712 at19,9 binds the first reserve bed17,6, second18,6 and
tray21,7. `sixsite-nella-terms` records Unknown. The normal introduction action
changes it to Granted in `sixsite-nella-introduction-accepted`; drams remain50.
This is the introduction branch, not a hidden8dram charge or a claimed paid-rights
replay. The existing terms and granted messages are visible in the actual images.

**Harvest and renewal.** Initial ripe Pitchpod15729 at18,6 disappears after the
legitimate harvest. Resin15732×2 and seed15734×1 are genuinely carried. Replanting
consumes that seed and creates15735 at the same bed. The subsequent Rain leaves
15735 at stage0/2growth units/38moisture, with access still Granted. The same
capture records nearby gladroot and Marlroot wet while the more distant wheat
remains dry; no global watering is implied. This pass does not revisit Nella
after departure to accept the replacement Pitchpod's eventual ripe state or a
resin application.

**Kitchen return.** Claspbean13515 remains the same owner at25,15: it was dry at
the earned start, watered before departure, stage0/14units/26moisture at the
introduction receipt, and stage2/0units/0moisture at7160. The actual return image
and reader show its ripe native form. Pan13489 retains the exact repair cause and
repaired flag. Player retains LongSword2699 and Buckler2700 in hand slots,4pulp,
2resin and both healing tonics. Clay and the harvested PitchpodSeed are no longer
carried. These receipts preserve blade identity/equipment; this pass does not
reinspect or independently revalidate every temper payload field.

## Actual visual evidence and imperfections

Representative unmodified Unity captures:
[repaired pan](Verification/Live16x24/sixsite-pan-repaired.png),
[Orven's introduction](Verification/Live16x24/sixsite-orven-introduction-earned.png),
[glade court](Verification/Live16x24/sixsite-glade-court.png),
[cellar revisit](Verification/Live16x24/sixsite-cellar-revisit.png),
[Nella grants access](Verification/Live16x24/sixsite-nella-introduction-accepted.png),
[reserve replant](Verification/Live16x24/sixsite-pitchpod-replanted.png), and
[ripe kitchen return](Verification/Live16x24/sixsite-kitchen-crop-revisit.png).

Root reports incidental wall/hedge strikes during ordinary navigation. The pan
image visibly logs a stone-wall strike; the cellar and north-arrival images show
wall/worn-crossing-wall strikes. Preserve these as real actions and possible
terrain damage, not merely blocked movement or a flawless route. The glade log
also shows the warden killing a Scrabbler; do not credit that as a Player combat
or Frozen/Riposte verification. Player remains42HP in every sampled receipt.

The sites retain recognizable accepted native forms and the real service link
works, but this is not whole-scene visual approval. Repeated bright hedge bands,
large quiet areas and truthful incidental glyph coverage remain visible. The
returned Claspbean reader is still cramped in `sixsite-reader-before.png`.
[GRAPHICS-G3-READER-LEGIBILITY.md](GRAPHICS-G3-READER-LEGIBILITY.md) owns that
separate measured-leading correction; its native RED/GREEN and later captures
must not be attributed to this route-only record.

## Checkpoint preservation and cleanup

The final Quick save metadata records7160, kitchen12.11.0, level2,42/42HP at
01:53:28.466766UTC. Root exported the actual character folder to
`/tmp/coo-sixsite-final-checkpoint/70a87e9febf34b499e8aaf47a44601ed/`.
[sixsite-final-checkpoint.json](Verification/Live16x24/sixsite-final-checkpoint.json)
records its source disposable root and all four file sizes/SHA256 hashes. An
independent read-only check matched all four exported files to that receipt:

| File | Bytes | SHA256 |
| --- | ---: | --- |
| `Quick.sav.gz` |333298|`c9add40b3996a82f5d7d9d196e78ba19c7c7a29e1a25ceec33f0e7dfd701db56`|
| `Quick.sav.gz.bak` |333410|`53a7d9b7ef080e6b08a19e377a0b117a3fa38707e867c7cf221e630a3e434f2a`|
| `Quick.json` |411|`f17ae33659e3c86d695b611b6b0db302e60b269c37497f724d6bb68ba8e4c5a3`|
| `Quick.json.bak` |411|`52922adefb387495786e57d43ad3cfd2f30d842849cff0e4c7c4158ad7f2fe4e`|

Copy only the actual character folder when continuing. Keep the accepted
forge/kitchen and separate tween-replay branches intact. Export/hash agreement
proves preservation; it is not proof of a final quit/Continue roundtrip. Any
subsequent reader-fix reload belongs to its own evidence.

The01:54:01UTC cleanup observation reports Play stopped, isolation/artPending/
viewBorrowed false, null save-root override and ordinary SampleScene restored
(`Verification/Live16x24/sixsite-cleanup.json`). This describes that checkpoint only,
not the editor's state during later native-reader work.

## Self-review, performance and bounds

- 🔵 The previously separate sites now have one ordinary connected service
  journey with exact repair/introduction/permission/product ownership.
- 🔵 No production or artwork changed in this evidence pass; no new native test
  count or RED/GREEN result is claimed here.
- 🔵 Reader leading was corrected in the separately recorded native RED→GREEN
  and actual comparison pass below; no historical overlap claim is made.
- 🧪 Not accepted here: fresh New Game through all services, every visible-owner
  native-art census, failed/denied permission branches, full cooking batch,
  component forging, Frozen proc, actual Riposte, pulp use, full replacement
  Pitchpod growth, physical controls or hardware performance.
- ⚪ Existing scene composition/glyph gaps remain in the graphics release ledger;
  the successful service transaction does not erase them.

**Can verify:** normal keyboard route through six zones, sampled flat
presentation, exact causal state and finite consumption, unchanged sampled
HP/drams, harvested/renewed Pitchpod and Claspbean growth on return, final save
export bytes/hashes. **Cannot infer:** every frame or input, all seeds, complete
visual polish, physical Deck comfort, continuous frame pacing or a current
packaged release. No profiler speedup or new performance measurement is claimed.

**Files changed by this documentation task:** this living record only. Root owns
the corresponding capture/receipt packet and separate typography implementation.


## Subsequent Continue and reader acceptance

After the typography change, normal C Continue loaded the unmodified final
checkpoint. `sixsite-continue-comparison.json` records exact agreement for ten
sampled fields: zone, flat mode, position, HP, tick, drams, introduction,
equipment, inventory and sampled current-zone objects. Kitchen13515 is still
ripe; pan13489 retains the exact repair cause. This is a stop/Continue check,
not proof of every saved zone or unrecorded field. Nella's inactive-zone state
was not re-observed after this Continue.

`GRAPHICS-G3-READER-LEGIBILITY.md` records108 native passes and the actual same
crop plus two-page sword reader. All five reader/return snapshots remain at
7160. At02:02:13UTC Play/settings/view/save override were restored.

**Named follow-up:** after harvesting the reserve Pitchpod, targeting selected
original decorative `CropRow`14060, named “cut emberwheat row”, over cultivated
Grass14059. The crop was correctly removed; planting still worked. Preserve
that historical terrain name and add a canonical “prepared planting bed” cue
through `CellStatusReadout` in a separate test-first slice. The cue must be
current/disclosed, remain absent on unprepared or foreign/hidden cells, respect
live crops and retain water/hazard text. Do not fabricate a growth or custody
bug, globally rename crops, or mutate existing saves as a shortcut.

The next delivery priority is a current packaged flat build after storage
review, with package launch and physical Deck evidence kept separate. Fresh
New-Game whole-journey acceptance and remaining owner/state art are still open.
