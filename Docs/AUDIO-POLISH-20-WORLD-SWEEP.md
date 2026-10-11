# World, animal and habitat audio: further polish sweep

Status: read-only audit, 2026-10-11, after `18f96ed7b`. Eight candidate improvements for the new twenty-pass plan; **none is implemented or live-verified by this sweep**. The earlier four atmosphere iterations are separate work. Root owns the ordered plan, Unity, fresh Play gate after each iteration, and commits. This document changes no Assets.

These are CoO sound-design proposals supported by current gameplay, not claims of Qud audio parity. Read alongside `AUDIO-POLISH-20.md`, `AUDIO-PLACE-SWEEP.md`, `AUDIO-ATMOSPHERE-ITERATIONS.md`, `AUDIO-WORLD-SOUND.md`, and the three audio source manifests in `Tools/Audio`.

## Verified present behavior and corrections

| Question | Actual source / correction |
|---|---|
| Is wildlife still competing with every scenery cue? | No. `WorldAudioSoundscape.cs:76–110` already has independent wildlife, material and habitat lanes, category rotation, and a four-second shared quiet gap. Do not count implementing those again. |
| Are leaves, reeds and indoor materials missing? | The second earlier pass added `leaf-rustle`, `reed-rustle`, `wood-settle`, `stone-shift`, and `water-drop`. Exact `Tree`, `Bush`, and `Reeds` qualify; `DryBrush` does not. Source identity and destroyed/portable rejection are already implemented. |
| Is water already separated from generic biome sound? | Yes, but only one nearest `Kind.Water` witness is retained (`WorldAudioSoundscape.cs:53–64`). `SelectBedCore` then asks whether that one owner flows (`:143–146`). An independently visible river is lost if nearer still water wins the slot. |
| Does frozen water disappear? | No. The finite pool can remain. `LiquidSourcePhase.CanDrawWater` explicitly rejects current frozen/cold sources and ice-covered occupied cells, while sound classification currently checks positive pool volume and liquid identity only (`WorldAudioSoundscape.cs:160–161`). |
| Does an exterior wall imply a separate outdoor source? | No. `VillageBuilder.cs:202–224` deliberately marks room floors interior and keeps perimeter walls/door cells exterior. Current sound scanning and `Current` reject every opposite-interior cell (`WorldAudioSoundscape.cs:56,152`); synthetic interior-wall fixtures do not prove real generated rooms. |
| Is the world clock a new timer to add? | No. `WorldClock.cs:18–48` derives four 300-tick bands from the existing saved turn count. Rest advances60 ticks; roughly30 ordinary actions span a band. Never create a separate audio day clock. |
| Is every inactive animal asleep? | No. `DormantGoal` also represents ambushers and inactive constructs. Use explicit living sleep/stasis effects, not `CanFight`, passive temperament, a rooted state, or a generic dormant-goal guess. |
| Are the deferred animals only blueprint ideas? | `PopulationTable.cs:278–282` places SummitSinger in the summit table; `StumpFaunaHabitat.cs:14–15` requires nearby actual trees. CaveBat is in actual cave population tables (`PopulationTable.cs:753–763`, also other cave tiers). Neither currently has an admitted soundscape voice. |

## Recommended eight candidates

Each candidate below is one coherent audible behavior. The positives, negatives, asset preparation and test repairs are gates for that behavior, not additional numbered iterations.

### W1 — Keep a visible river audible beside a small puddle

**Player result:** the local river bed remains while a nearer small standing pool supplies only its own sparse detail. Pouring a flask beside a river should not turn off the river.

**Bounded change:** retain one independent nearest valid flowing-water witness during the existing bounded discovery pass; keep the existing nearest general-water witness for lap/drop details. Recheck that exact flow owner before preserving the bed. Preserve current depth/interior priority, known-source invalidation, and the169-cell/512-owner limits. No extra scan, rainfall inference, or flowing-mire label.

**Tests:** a real flowing water owner at distance3 plus a still pool at distance1 retains `water` bed; reverse discovery order and unequal distances. Hide/drain/remove only the flowing owner and require the river bed to leave while standing-water detail remains. Water without flow markers, brine and bog-mire never create a river bed. Save/load replacement must not retain the old flow owner.

**Fresh Play route:** approach an existing visible river with an actual filled flask, hear/observe its real imported bed, use the existing pour inventory action into a nearer cell, and show continuity through the action. `LiquidVesselService.cs:141–217` creates the real finite poured pool. A disclosed travel shortcut may reach a generated river; do not fabricate a flowing pool or call `SetContext` as the live action. **Recordings:** existing water bed and details suffice.

### W2 — Let frozen water become quiet

**Player result:** freezing a visible pool stops watery lap/drip and flow-bed cues while ice remains; thawing restores eligibility on the next ordinary observation.

**Bounded change:** share the existing phase query or extract its read-only phase portion if its draw-specific ownership rules are too restrictive. Check literal water and authored brine/mire ground ice; preserve their actual liquid identities. Do not consume pools, simulate thawing, or claim that unrelated non-liquid sounds should stop.

**Tests:** positive finite water plus actual ice coating rejects both detail and flow bed; remove only ice and prove restoration. Repeat actual frozen-effect and thermal-freezing states, water-family brine/mire, removed/replaced owner, and a nearby unfrozen second source. A cosmetic `Cold` name or ice elsewhere must not silence the source. Cover in-progress tail invalidation without another turn.

**Fresh Play route:** gather finite `FrostLichen` from a real `FrostLichenPatch` in a generated worksite (`SpreadExplorationWorksites.cs:46–50`), then use its existing freeze-ground action next to an actual wet source. Observe the committed ice and exact previously playing water voice stopping; if thaw is not safely reachable in the finite route, label thaw unit-tested only. `MaterialFieldActions.cs:89–100,157,228–234` already performs the real operation. **Recordings:** none required; deliberate silence is the improvement.

### W3 — Make actual room boundaries sound like shelter

**Player result:** a player inside a generated timber/stone room can hear that room's visible structural settling, rather than requiring a wall incorrectly marked as floor interior.

**Bounded change:** admit only a visible, current, immovable wall/door bordering the listener's interior region as a structural source. Keep outside animals, foliage, water and unrelated farther walls excluded. Do not globally remove the same-interior check or change worldgen metadata. A small local structural-boundary predicate is preferable to a room flood-fill on audio updates; establish exact adjacency semantics before implementation.

**Tests:** build a real room through `VillageBuilder`, enter its interior, and prove the exterior-marked enclosing structure qualifies. Same-layout exterior animal/pool/loose plank remains silent. A wall across another room, hidden wall, destroyed wall, and carried material do not qualify. Returning outside must restore existing outdoor policy. Existing material tests manually mark both source and listener interior (`WorldAudioAtmosphereTests.cs:75`); keep those controls but add the generated-room premise.

**Fresh Play route:** enter and leave an ordinary generated village building through its actual door. Capture interior metadata, exact visible structural owner, imported settling voice, and the absence of an outdoor wildlife cue inside. **Recordings:** existing paired timber and stone clips suffice.

### W4 — Respect an animal's actual sleep or frozen stasis

**Player result:** a visibly gas-asleep or frozen animal does not continue its normal voluntary bark/croak/chatter. Waking restores its ordinary voice without a catch-up burst.

**Bounded change:** add explicit live-effect admission for approved vocal animals. `AsleepByGasEffect.cs:1–60` blocks action and wakes on positive damage; `HibernatingEffect` is explicit self-stasis; `FrozenEffect` represents action-blocking ice. Respect actual owner, live duration/intensity and post-wake state. Do not call `AllowAction` merely to obtain audio policy, or silence rooted/calm/passive animals generically.

**Tests:** begin a real admitted animal cue, apply a live sleep effect, and require current tail/new-call rejection; after actual wake/removal, prove a later eligible call. Expired sleep, an effect owned by another entity, ordinary Rooted, passive temperament and zero-damage non-wake are counters. Hidden sleeping animals must never be newly discovered.

**Fresh Play route:** use an actually carried SleepGasGrenade against an already visible approved animal, observe real gas sleep and quiet, then wake/expiry through ordinary turns. The grenade exists in `Objects.json:28867` and several actual loot tables. A dedicated authored disposable inventory/animal setup may be needed for a bounded route; disclose that setup and require actual throw/gas/scheduler effects afterward. Do not claim ordinary acquisition if the bench supplies it. **Recordings:** existing calls suffice.

### W5 — Preserve still-valid phrase tails at a day-band boundary

**Player result:** a fire crackle or structural creak already sounding does not abruptly cut merely because Dawn becomes Height; night-inappropriate bird cues still stop.

**Bounded change:** replace blanket `policyChanged -> ClearDetails` (`WorldAudioSoundscape.cs:42–44`) with selective validation of the active phrase's current authority and policy. A graph replacement, hidden world, mute, lost source or newly invalid bird policy must still retire its voice. Keep the existing musical/bed fades and lane clocks. Since the shared phrase gap exceeds every current clip, do not introduce a general multi-voice tracking framework just for this case.

**Tests:** a real fire/wood phrase survives Dawn→Height with continuing playback position and no additional start count. A daytime bird cannot survive Height→Dusk. Test source mutation during the same boundary, rapid band changes, menu/load transitions, and no new emissions while paused. Habitat phrases also need the old/new policy distinction; fire survival must not accidentally preserve a daylight bird recording.

**Fresh Play route:** at a safe actual inn/fire location, use ordinary rest or waiting to approach a clock boundary, then cross it while the relevant real phrase is playing. Log the real saved turn count, old/new bands and continuing sample position. Allow a finite explicit timeout instead of assigning the clock. **Recordings:** none required.

### W6 — Give actual dry brush a restrained physical texture

**Player result:** dry regions have a close, source-backed vegetation rustle in addition to their distant wind bed, without importing a forest chorus.

**Bounded change:** admit exact current `DryBrush` as a sparse material-lane source using the existing dry-leaf Foley at an appropriately restrained gain/cadence. The original physical material is dry vegetation; calling the result designed brush texture is honest. Do not broaden all `Plant` or `Fuel` materials, add a weather simulation, or sound carried kindling. `BeatingFormationBuilder.cs:263` and several actual settlement compositions place DryBrush; `Objects.json:31097` gives it real DryPlant material and destructibility. Current exact Tree/Bush/Reeds tests leave it silent.

**Tests:** factory-built DryBrush in an exterior dry biome qualifies, unlike Cactus, a carried Plant item, a dead/removed brush, an interior source or the same source below ground. Hide/destroy the actual brush during a phrase and require removal. Vegetation density cannot increase the fixed source/voice budget.

**Fresh Play route:** approach actual visible DryBrush in a generated Beating or known dry settlement area, idle for a bounded phrase, then walk out of its visibility/range and show no retained local texture. **Recordings:** reuse the two licensed dry-leaf recordings; new recordings are optional only if listening shows the existing close Foley does not fit.

### W7 — Deliver SummitSinger's sparse single note

**Player result:** the authored summit frog finally gives its distinctive isolated note, with enough space to retain the mountain's quiet.

**Bounded change:** approve exact living visible SummitSinger with a deliberately sparse policy, initially Dusk as proposed in `AUDIO-PLACE-SWEEP.md:24`. This band choice is sound design, not a currently implemented animal scheduler. `Objects.json:35403` explicitly describes one note and long spaces. Use a real single-note frog recording, not a bird call or generic wetland chorus. Preserve CascadeFather's explicit no-call lore (`Objects.json:34710`) and do not implement or claim the deferred Urqu alarm/silence clock.

**Tests:** factory SummitSinger at the approved band can emit its distinct imported cue with a meaningful long minimum interval; same actor at an excluded band, hidden/dead/asleep, and CascadeFather never emit it. A crowd of singers must not become a chorus or reset the cooldown through nearest-owner changes.

**Fresh Play route:** reach a generated summit habitat with a genuinely placed singer, observe it through normal FOV and cross/wait for the chosen band through actual actions. A disclosed travel shortcut can reach the existing generated habitat, but no spawned replacement or forced cue counts as this live proof. Include a finite no-source/no-safe-route failure gate. **Recordings:** a genuinely suitable licensed single-note frog take is required; source/license/import review and an audible inspection are part of this one implementation pass.

### W8 — Tie cave-bat flutter to actual visible movement

**Player result:** a real nearby bat contributes a brief wing flutter when it actually moves, rather than a cave biome inventing perpetual unseen animals.

**Bounded change:** a small exact-CaveBat branch on the existing committed `EntityVisualHooks.MovedCallback` can supply a throttled decorative flutter. `WorldAudioEventRouter.cs:38–45` currently admits only player footsteps; preserve that rule for all ordinary NPCs. Require current live owner, exact new cell, visible/explored source, non-forced displacement and appropriate range. Use the existing decorative voice budget and Ambience setting; never trigger combat ducking, gameplay hearing or a universal NPC footstep chorus. Cosmetic rate limiting must be bounded without an ever-growing entity dictionary.

**Tests:** actual successful CaveBat movement emits one eligible flutter, while attempted blocked movement, force-move, zero displacement, hidden/dead/detached bat and an otherwise identical non-bat do not. Rapid flock movements remain throttled and within16 voices. World-hidden/menu and load replacement clear pending work; no delayed cue later. Do not turn a stationary bat into a movement sound through an idle timer.

**Fresh Play route:** enter an actual generated cave with a current CaveBat (the real tables include it), take ordinary turns and correlate one visible movement callback to an imported flutter. If no suitable bat is generated, retain a truthful bounded failure or use a separately labeled authored bench; do not silently make the old cave wind recording count as a bat. **Recordings:** a short genuine wing-flutter recording is warranted. A generic whoosh can only be described as designed Foley and needs listening before acceptance.

## Pruned scope and risks

- Small/large/forest frog voice separation remains a useful reserve, but five variants of the same routing change should not be five iterations. Current five-species generic frog admission is real; authored lore does not specify all those species' exact acoustic timbres. Choose it as one content pass only after suitable recordings are found.
- Do not reimplement previous hidden-owner, pool-drain, fire-fuel, day/night-bed, phrase-lane, source-pool, work-commit or melee-mix fixes. The new work route passed21/21 using two original tonics; it does not prove a safe long farming or distant wildlife route.
- No passive forge/kitchen loop: actual work must remain tied to committed activity. No crowd chatter without a source and no storm audio inferred from region names.
- No universal room-acoustics simulation, offscreen hearing, dynamic pathfinding or whole-zone per-frame scan. Structural-boundary admission must preserve information limits.
- Most source corrections need no additional recordings. SummitSinger and bat flutter are the two clear missing palette roles in this list. Existing reed detail is processed dry-leaf Foley, not a literal wetland field recording; preserve that provenance language.
- All eight remain proposals. Each selected pass needs observed RED, positive/counter/adversarial verification, independent review and a fresh actual Play report with exact source/voice/signal evidence. PCM and counters do not establish pleasantness, physical speaker output or Steam Deck performance. No listening is claimed by this audit.

## Review / files

Read-only self-review: verified current signatures and source policies; corrected initial assumptions that every interior-looking wall has `IsInterior=true`, CaveBat is only a blueprint, or a pool vanishes when frozen. Kept generated-source live routes distinct from possible authored test setup. No runtime changes, test execution, Unity operation, new asset acquisition or commit performed here.

Only file created by this sweep: `Docs/AUDIO-POLISH-20-WORLD-SWEEP.md`.
