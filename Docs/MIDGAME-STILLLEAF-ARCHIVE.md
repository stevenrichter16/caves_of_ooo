# The Stillleaf Archive — the first middle-game chain

**Status:** complete, 19 September 2026 — SA.1–SA.6 shipped. The whole journey is playable, hardened and proven through native keys: key, Searcher, Indexer and file, the three custody outcomes with their aftermath, robustness to the wrong order, loss and theft, and a validated native run of the delivery outcome with 33 inspected captures. Release roadmap step 3 is met for one chain. Release roadmap step 3 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap; [GAME-STATE](GAME-STATE-2026-09-17.md) §15, §20). CoO-original campaign content; no Qud parity claim.

## Goal

One connected middle-game journey, as the release documents define it: **a real settlement need, a reason to descend, a preparation decision, conflicting testimony or loyalties, an enacted choice and a visible aftermath** — built from places, systems and factions that already exist, and robust to refusal, changed allegiance and a lost participant.

## Why this chain

- **Canon names it.** The Recension's player surface lists *The Lost Library rediscovery* — "the player rediscovers a Sealed Library by methods the Recension cannot use… the Mainline will seal it again immediately; the Unforgetting will agitate to read it publicly" — and *the Searcher approach* — Searchers "identify the player as an outsider who can investigate without political cost" ([02_Recension](../Lore/Factions/02_Recension.md) §VII).
- **The release vision sketches it** as "an archival journey: knowledge with a use and a cost… a record should open a concrete course of action while its custody creates a choice about who benefits… using the release-scoped Recension/Curation arcs."
- **The v1 triage puts it on the spine.** Recension and Pale Curation are two of the five deep factions and carry Preserve ([V1-DramaticCore](../Lore/Design/V1-DramaticCore.md) §II).
- **The vertical site already exists and is unfinished.** Stillleaf (world cell 2,4; floor `Overworld.2.4.2`) was built with a locked `SealedLibraryDoor` whose lock expects key `coo.sealed-library.stillleaf`, and its builder says "its key and readable contents belong to the later quest arc." **No key exists anywhere**, so the vault is unreachable in ordinary play today.

## The journey

1. **Quillhold — the need.** A Recension Searcher at the Primary Archive has catalogue fragments naming a library the Reader cannot remember and ordinary Recension means cannot find. She hires the player as an outsider. She knows one more thing: the library's last keeper was preserved at the Salt-Vault, and Pale Curation indexes its dead by their last words. She gives those words — the only index key that can find the keeper's file.
2. **The Salt-Vault — preparation and the first loyalty.** A Pale Curation Indexer can retrieve a file only by its last words. With them, the keeper's file is found, and the vault key filed beside the keeper. Curation's price is a request, not a toll: *if you open it, bring what you find here to be filed, not read.* The player may agree, bargain, or refuse and leave without the key. This is where the Schism becomes the player's problem: record against vessel.
3. **Stillleaf — the descent.** The existing sinkhole: mouth, descent, floor, and the vault's guardians. The key opens the door. Inside, one contested record — *the Stillleaf register* — and the keeper's own note asking that it stay sealed.
4. **The enacted choice.** Three incompatible testimonies — the Searcher's (bring it to be known), the Indexer's (file it unread), the keeper's (leave it sealed). The player enacts one, physically:
   - **Deliver it to Quillhold.** The Searcher receives it; the Mainline seals it in the archive. Recension standing rises; a broken promise to Curation costs standing there.
   - **File it at the Salt-Vault.** The Indexer files it unread beside its keeper. Curation standing rises; the Searcher's lead is closed without the record.
   - **Reseal it in place.** Leave the register in the vault and relock the door. Neither faction is paid; both are told truthfully. A principled refusal is closure, not failure.
5. **Visible aftermath.** The record is physically where the choice put it; the people involved say so; the door's state follows the choice; standing changes; the Field Note records the outcome. Nothing respawns or reverts on a revisit.

## Verification sweep — corrections before any code

| Tempting premise | Verified fact | Consequence |
|---|---|---|
| Quillhold has a Recension quest-giver | Its only scribe is the generic village grimoire-copying `Scribe` (`Scribe_1`, `VillagePopulationBuilder` service role). `Palimpsest.json` holds only `PalimpsestEcho_1`, a creature | Author a Searcher resident |
| The Salt-Vault is a built institution | It is an ordinary village carrying the `PaleCuration` faction; no profile (`WorldMapAuthoring.Places`) | Author an Indexer and a file cabinet; do not claim the canon set-piece |
| The vault holds readable records | `SealedArchiveShelf` has no `ContainerPart` (Render/Physics/Examinable/Material/Destructible) | The register is a ground item inside the vault |
| The vault can be opened somehow | `LockPart` unlocks on bump only with a carried `KeyPart` of the same id; no item anywhere carries `coo.sealed-library.stillleaf` | The key is new content and the chain's gate |
| Surface install hooks reach the vault | `OnZoneGenerated` returns in its `wz > 0` branch before the surface hooks (`OverworldZoneManager` ~1060–1083) | The vault install needs its own underground hook |
| The code faction id is "Recension" | Code still uses the pre-rename id `Palimpsest` for Quillhold and the Drowned Ledger | Use `Palimpsest` in code; say "Recension" in player text |
| Grimoires would make a unique reward | Every rite is already in `LootTables.json` | Keep v1 about the record; a guaranteed rite is optional later |
| The mouth is visible on the map | Sinkhole mouths render as plain biome until visited (W5.7 found-not-shown) | The Searcher's directions must be navigable in human terms |

References read: `CLAUDE.md`, `RELEASE-AGENT-PROMPT.md`, `RELEASE-VISION.md`, `V1-DramaticCore.md`, `02_Recension.md`, `03_PaleCuration.md`, `MYSTERY-LEDGER.md` (entry list and under-text policy), `SealedLibraryBuilder.cs`, `SealedLibraryBarrierPart.cs`, `LockPart.cs`, `MorrowfastExpedition.cs`, `MorrowfastContent.cs`, `QuillholdCompositionPlan.cs`, `SinkholeSites.cs`, `SinkholeArchetypes.cs`, the relevant `Objects.json` blueprints.

## Canon guardrails

- The register never resolves a Mystery Ledger entry: no Naro, no Root dream, no named relic, no under-text systematization.
- It does not spend the late-game Reader–Salted estrangement. The Schism is present as doctrine, not as the reconciliation beat.
- Urqu is not involved. No faction is the villain; each testimony is sincere.
- Refusal is closure. Resealing is never punished.

## Scope

**In:** two authored residents (Searcher, Indexer) with sprites; the key; the register and the keeper's note; one quest with a Field Note; three custody outcomes with physical, social and standing aftermath; death/refusal/order alternatives; save/load; native proof.

**Pruned, with reasons:** the Salt-Vault set-piece and the Salted's audience (canon late-game content); the Reader's channel; the Unforgetting and Mainline as separate characters (one Searcher speaks for the archive's decision); guaranteed rites; map-marker revelation of the mouth (would contradict found-not-shown). Each is deferred, not deleted.

## Sub-milestones (smallest blast radius first)

- **SA.1 — The key opens the vault.** `StillleafKey` blueprint (inherits `IronKey`, own key id, shared key body art); `StillleafRegister` and keeper's-note blueprints; a fresh-generation install of the register inside the vault via a new underground hook. Tests: the real key opens the real door, the iron key and no key do not, the register is inside the enclosure, install is idempotent and never duplicates on revisit.
- **SA.2 — The Searcher.** Quillhold resident, conversation (ask / accept / decline), quest definition, Field Note with navigable directions, the last-words knowledge. Tests through the real conversation path, including decline and repeat.
- **SA.3 — The Indexer and the file.** Salt-Vault resident and file cabinet holding the key; release requires the last words; the Curation request is an explicit choice. Tests for every branch, including arriving without the words.
- **SA.4 — Custody and aftermath.** Three outcomes, each physical, persistent and one-time. Tests per outcome plus mutual exclusion and no double payment.
- **SA.5 — Robustness.** Participant death, out-of-order arrival (vault found first, key taken first), dropped or destroyed register, save/load at each stage; dedicated adversarial file.
- **SA.6 — Native proof and close-out.** Native journey (synthetic long-distance travel labelled honestly), inspected captures, cold-eye review, full suite, publication.

## Performance and observability

Event-driven content: conversation verbs, one fresh-generation install per zone, no per-frame work, no new caches. Every verb emits a diag record on success and rejection (`quest` category), following `MorrowfastExpedition`.

## Execution rules for this work

All execution in the independent clone; the user's editor stays open. Guard check before every git write; avoid the :39 UTC automation window. Surgical JSON edits only. New residents ship with 16×16 sprites. Commit each sub-milestone independently with its evidence and this document's log.

## Implementation log

### SA.1 — The key opens the vault (19 September 2026)

**Status:** shipped. Full EditMode suite in the independent clone: 15,034 tests, 15,033 passed (`Docs/Verification/VoxelWorld/SA1-full-3`); the one failure was `DiagPerfTests.Diag_DisabledChannelOverhead_BoundedPerCall` (290 ns/call against a 200 ns ceiling) while other processes shared the machine — `Diag.cs` is untouched by this work, and the class re-run in isolation passed 3/3 (`SA1-diagperf-rerun`). Kit build receipt `SA1-kit-build`; focused suite `SA1-focused-2` (187/187).

**What the player can now do.** Carrying the *Stillleaf keeper's key* (`StillleafKey`, key id `coo.sealed-library.stillleaf`, `NoTrade`), bumping the sealed library door unlocks it on the first bump and walks through on the next — the ordinary `PhysicsPart` → `AttemptUnlock` path, nothing bespoke. Inside, on fresh generation of `Overworld.2.4.2`, *the Stillleaf register* (`StillleafRegister`, entity id `stillleaf-archive:register`) lies on the deepest bare archive-floor cell. Its examine text is the register itself plus the keeper's slate — the third testimony — so it is read wherever the player first examines it. Nothing yet supplies the key in the world; that is SA.3's job.

**Implementation.** `StillleafArchive.TryInstallVault(zone, factory)` (`Assets/Scripts/Gameplay/World/StillleafArchive.cs`), called from `OverworldZoneManager.OnZoneGenerated` inside the `wz > 0` branch before `MarkDungeonInterior` — the surface hooks below never run underground (sweep row 5). Seat choice is deterministic (max Manhattan distance from the door; ties by X then Y). A persisted int property on the door (`StillleafRegisterInstalled`, serialized by `SaveSystem.WriteIntDictionary`) latches the install, so a taken or destroyed register is never re-conjured: the record is finite. Every outcome emits `worldgen`/`StillleafRegisterPlaced` or `StillleafRegisterRefused` with a `reason` (`not_stillleaf_floor`, `no_vault`, `already_installed`, `missing_register_blueprint`, `no_free_archive_floor`, `register_incomplete`, `placement_failed`).

**Voxel presentation.** The register is a fourteenth Stillleaf family (`register`, kit offset 52; `StillleafVoxelLibrary`, `StillleafVoxelKitBuilder.Register`), four variants of a flat bound record — pale covers, dark spine, tie band, the keeper's slate on one corner — built from six `Box` primitives and rebuilt in place by the new generic runner `Tools/VoxelWorld/build_kit.py` (kit 52 → 56 entries, `Library.asset` updated, fresh GUIDs audited against `main`: 0 collisions). As a takeable item it resolves *transient* (never baked into the static chunk mesh) and keeps one shape wherever it is dropped, exactly as the portable boots do.

**Scope divergence from the plan.** The plan listed "`StillleafRegister` and keeper's-note blueprints" (two items). Shipped: one item; the keeper's note is the slate tied to the register's cover, carried in its examine text. Reason: a separate note is a second takeable with no independent use in SA.4's three custody outcomes, and a note that can be separated from the register would let the third testimony be lost by accident. The plan's SA.1 test list is otherwise shipped verbatim.

**Deliberate premise changes in existing pins (both documented at the pin).** `SealedLibraryTests.AllShippedKeysLeaveTheVaultLocked` asserted that no shipped key carries the vault's id; it is now `OnlyTheKeepersKeyOpensTheVault_AndNothingElseSuppliesIt` (exactly one key blueprint has the id; no `Content/Data` loot file supplies it). `SealedLibraryAdversarialTests` and `StillleafCompositionAdversarialTests` allowed only architecture inside the enclosure; both now allow exactly the one authored register, by entity id.

**Self-review (Methodology Template §5).**
- 🟡 *Fixed pre-commit:* the first full run failed 8 tests — 5 voxel-coverage pins (`StillleafRegister: unmodeled-native-blueprint`, i.e. the register would have been invisible in 3D) and 3 interior-purity pins. The coverage pins are exactly the guard that keeps a new item from silently vanishing in the presentation layer; they were satisfied by real art, not by widening the pin.
- 🟡 *Fixed pre-commit:* my first sanctum pin put the register in the immovable-architecture test (`DefiningOwnersHaveDistinctCurrentNativeModels`), which asserts `Transient == HasTag("Creature")`; takeable items are transient by design. Replaced with `PortableArchiveRecordsAreTransientNativeModelsThatKeepTheirShape` (register and boots).
- 🧪 *Deferred to SA.5:* refusal branches `no_vault`, `missing_register_blueprint`, `no_free_archive_floor`, `register_incomplete`, `placement_failed` are not individually pinned; the latch's save/load round-trip is not yet pinned (structurally covered by `SaveSystem`). Both belong to SA.5's dedicated adversarial file.
- 🔵 *Deferred to SA.3:* `StillleafKey` has no voxel model in the Stillleaf stack (only the Stump kit maps `IronKey`). A key dropped inside the vault would be skipped in 3D. It becomes a placed world object in SA.3 and gets art then.
- 🔵 *Noted:* the register does not yet answer the R4 material-guidance path (`examine_material`); it is a record, not a material, and its examine text is the guidance.
- ⚪ *Environment:* `Tools/Release/release_candidate_status.sh` exits 3 on two pre-existing conditions unrelated to this work. Topology: `main` is no longer an ancestor of `release-candidate`, because R4 (`fcc9793d`) and this plan (`f1f03b5d`) landed on `main` while that branch received only docs-only quiet runs; this commit widens the same divergence and touches nothing on `release-candidate`. Manifest: the R1 manifest (`Docs/ReleaseHandoff/R1-candidate-manifest.json`, commit `41820270`) predates `816ed752` and `d98af95f`, which are on `release-candidate` and changed three of its 99 files. `main` is unaffected; this commit does not touch `release-candidate`.

**Cold-eye Q1–Q4.** Q1: the hook sits where `MorrowfastExpedition.TryInstall` sits for surface zones, in the corresponding underground branch. Q2: diag naming (`<Thing>Placed` / `<Thing>Refused` + `reason`) matches `MorrowfastSupplyPlaced` and `FellingPopulationRefused`. Q3: key vs iron key vs no key; sealed vs opened reachability; install vs reinstall; taken vs never replaced; Stillleaf vs other floors; placed-without-refusal vs refused-without-placed. Q4: this section was checked against the shipped code before commit.

**Files.** NEW `StillleafArchive.cs`, `StillleafArchiveTests.cs`, `Tools/VoxelWorld/build_kit.py`, 16 register kit assets; MOD `OverworldZoneManager.cs`, `Objects.json` (two spliced blueprints after `IronKey`), `StillleafVoxelLibrary.cs`, `StillleafVoxelKitBuilder.cs`, `Library.asset`, `SealedLibraryTests.cs`, `SealedLibraryAdversarialTests.cs`, `StillleafCompositionAdversarialTests.cs`, `SanctumRenderingTests.cs`, `StillleafVoxelKitTests.cs`.

### SA.2 — The Searcher (19 September 2026)

**Status:** shipped. RED gate `SA2-red` (CS0103, the content class did not exist); focused GREEN `SA2-green-2` 62/62 after two pin corrections; full EditMode suite in the clone: 15,045 tests, 15,044 passed (`SA2-full`); the one failure was the loadout whitelist `GameAuditEntityEquipmentContentTests.Kits` (the Searcher inherits `RecensionScribe`'s gloves and boots), a deliberate premise change: she was added to the kit table and the affected classes re-ran green (`SA2-equip-rerun-2`).

**What the player can now do.** In Quillhold's stacks stands *Hollin Vesk*, a Recension Searcher (`StillleafSearcher`, entity id `stillleaf-archive:searcher`, faction `Palimpsest`, quest beacon). Through the ordinary conversation she explains the Searchers, offers the contract, and — on acceptance — starts the journal entry *What Stillleaf Kept* and gives the keeper's last words: *"Keeping is not the same as showing."* Those words are the index key the Salt-Vault will need (SA.3); they are spoken in the log and kept in the Field Note ([Q]) with navigable directions in chunk terms (the Salt-Vault six chunks south and one east of Quillhold; Stillleaf twelve west and five north, on the tepui). "Not now" is a plain choice that starts nothing and leaves the offer on the table; "release me" removes the journal entry without marking failure, and what was heard stays heard — refusal is closure.

**Implementation.** `StillleafArchiveContent` (`Assets/Scripts/Gameplay/World/StillleafArchiveContent.cs`): `EnsureRegistered` (the `StillleafArchive` required action, the `IfStillleafArchiveCan` predicate, guarded loads of `Content/Conversations/StillleafArchive.json` and `Content/Data/Storylets/StillleafArchive.json`); `TryInstallSearcher` seats her on the bare interior `StoneFloor` nearest the centroid of the archive shelves, latched on the origin ground like `MorrowfastExpedition`, from the surface hook in `OverworldZoneManager.OnZoneGenerated` beside the Morrowfast field hook and gated by `AreaCompositionScope.IsQuillholdSite`; `CanConversation`/`TryConversation` mirror `MorrowfastExpedition` line for line (active zone is the manager's cached zone, speaker by id and blueprint, both alive, adjacent, not hostile). `StillleafResidentPart` re-registers the verbs after a load. Diag: `worldgen`/`StillleafSearcherPlaced|Refused` and `quest`/`StillleafArchiveApplied|Rejected` with `command` and `reason`.

**Art.** A real 16×16 sprite (`Sprites/Environment/stillleaf_searcher.png`, binary alpha, the shared outline, 30 outline pixels) in the named-resident silhouette shared with the Curation and Concord envoys — slate-ink robe, pale collar, a satchel at her hip — registered in `EnvironmentSpriteRenderer.CreatureSprites` with the canonical `'@'`, so the reskin guard and the 3D layer agree; in Quillhold's voxel stack she keeps the scribe body (`quillhold-scribe-*`, transient), via the same alias the travelling Scribe uses in `SpawnRing3DRecipes`.

**Scope divergence from the plan.** "Decline" is not a verb: it is the plain "Not now" choice with no action and no state, and the tests pin that it starts nothing, gives nothing away and leaves the offer visible on the same and on a later visit. A recorded decline would have added state with no consequence.

**Deliberate premise changes.** `EnvironmentSpriteRendererHarnessTests` pins the sprite roster size (56 → 57, the Searcher). `GameAuditEntityEquipmentContentTests.Kits` enumerates every blueprint with a `Loadout`; the Searcher joins it with `LeatherGloves;LeatherBoots`, so the content tests also verify her inherited kit actually equips. No other shipped pin moved.

**Self-review (Methodology Template §5).**
- 🟡 *Fixed pre-commit:* the copied sprite `.meta` carried the source asset's sub-sprite name (`pale_curator_0`); renamed to `stillleaf_searcher_0` (three occurrences) and pinned. The family's sprites are `spriteMode: 2` slices to the figure (x 3, y 1, 10×13) — the same rect my figure occupies — so the pin now states the slice equals the silhouette rather than a full-texture rect.
- 🧪 *Deferred to SA.5:* install refusals other than `already_installed`; save/load round-trip of the latch, her id and `StillleafLastWordsKnown`; the `stillleaf_archive_unavailable` refusal string through the real UI.
- 🔵 *Noted:* she inherits `AISelfPreservation` from `RecensionScribe`, so under attack she flees rather than stands; SA.5's "participant death" cases will decide whether that is the wanted behaviour.
- ⚪ Guard state unchanged from SA.1 (topology and manifest drift on `release-candidate`; `main` only).

**Cold-eye Q1–Q4.** Q1: hook position and `ValidConversation` are the Morrowfast shapes; release mirrors Morrowfast's `RemoveActiveQuest`. Q2: diag names follow `<Thing>Placed|Refused` and `<Chain>Applied|Rejected`; payloads carry `command` and `questId`/`reason` only, ids at top level. Q3: accept vs not-now; release vs completed vs failed; adjacent vs remote; Searcher vs Scribe; alive vs dead (an accepted errand survives her); Quillhold vs Tine vs Sill; placed vs refused; applied vs rejected. Q4: this section was read against the shipped files.

**Files.** NEW `StillleafArchiveContent.cs`, `StillleafResidentPart.cs`, `Content/Conversations/StillleafArchive.json`, `Content/Data/Storylets/StillleafArchive.json`, `Sprites/Environment/stillleaf_searcher.png`, `StillleafSearcherTests.cs`; MOD `Objects.json` (`StillleafSearcher` spliced after `RecensionScribe`), `OverworldZoneManager.cs`, `EnvironmentSpriteRenderer.cs`, `SpawnRing3DRecipes.cs`, `EnvironmentSpriteRendererHarnessTests.cs`.

### SA.3 — The Indexer and the file (19 September 2026)

**Status:** shipped. RED gate `SA3-red` (CS0103, `StillleafSaltVault` did not exist); focused GREEN `SA3-green` 817/819 with one pin of mine corrected (below); full EditMode suite in the clone: 15,063 tests, 15,063 passed (`SA3-full`).

**What the player can now do.** At the Salt-Vault (world 15,15, a Pale Curation village) stands *Teodra Halm*, an Indexer (`StillleafIndexer`, id `stillleaf-archive:indexer`, faction `PaleCuration`), with a locked *salt file* beside the desk (`StillleafFileCabinet`, id `stillleaf-archive:file`) holding the keeper's key (`StillleafKey`, id `stillleaf-archive:key`). Without the words the Indexer explains the index and nothing opens. With them — spoken as a real choice — the file is read aloud ("Status: continuing"), the journal advances to *key*, and Curation names its request: *bring what you find here, to be filed, not read.* Agree or bargain ("I do not promise it stays") and the file unlocks; the player takes the key by the ordinary container path and the journal advances to *descend*. Refuse and leave without the key: nothing is failed, nothing is charged, and agree/bargain still stand on return. Once released, terms are one-time. Breaking the file open spills the key where it stood, records no terms, and leaves the Indexer nothing to release — a real out-of-order path whose standing consequences belong to SA.4/SA.5.

**Implementation.** `StillleafSaltVault` (`Assets/Scripts/Gameplay/World/StillleafSaltVault.cs`): `TryInstall` from the surface hook (gated by the Curation POI) chooses a bare interior `StoneFloor` pair using the population builder's own criteria, prefers the most wall around the file, and accepts a seat only if every other passable cell stays reachable from the zone edge with the solid file standing; `CanConversation`/`TryConversation` own `retrieve`, `agree`, `bargain`, `refuse` behind the chain's shared action/predicate names, dispatched from `StillleafArchiveContent`, whose Searcher-only gate became the shared `ValidConversation(zone, id, blueprint)`. The key carries `CompleteObjectiveOnTaken(objective "key")`, so the real `TakeFromContainerCommand` advances the journal. Player state: `StillleafFileFound`, `StillleafCurationTerms` (1 agreed, 2 bargained, 3 refused). Diag: `worldgen`/`StillleafIndexerPlaced|Refused`, `quest`/`StillleafArchiveApplied|Rejected`.

**Art.** `Sprites/Environment/stillleaf_indexer.png`, the named-resident silhouette in salt-white with a dark ledger and pale page at the hip, own slice name, fresh GUID; sprite row with `'@'`. Generic villages keep the native presentation (`VoxelWorldPresentation.IsSupported` is false there), pinned so a later voxel village does not silently lose her.

**Scope divergence from the plan.** The plan's "file cabinet holding the key" is a locked container, not a prop: the ordinary container path is the gate, which is why the smash path exists and is pinned as *not a release* rather than prevented.

**Deliberate premise changes.** Sprite roster 57 → 58; `GameAuditEntityEquipmentContentTests.Kits` gains the Indexer (`LeatherGloves;LeatherCap`, inherited from `CurationSorter`).

**Self-review (Methodology Template §5).**
- 🟡 *Fixed pre-commit (test-side):* my counter-check "the world holds one keeper's key" counted zone entities; the filed key is container content, not a zone entity. The pin now states the stronger fact: no keeper's key lies loose, and exactly one is filed.
- 🧪 *Deferred to SA.5:* install refusals other than `already_installed`; save/load of `Locked`, `Terms`, `FileFound`; standing after breaking the file; SA.4's "file it here" when the cabinet is gone.
- 🔵 *Noted:* the Indexer inherits `AISelfPreservation`, like the Searcher.
- ⚪ Guard state unchanged (`release-candidate` topology/manifest; `main` only).

**Cold-eye Q1–Q4.** Q1: install/latch/diag shapes match the Searcher's; the gate is literally shared code. Q2: verbs on both residents emit the same `quest` kinds with `command`; the file's release is a state change on a native part (`ContainerPart.Locked`), not a parallel flag. Q3: words vs no words; read once vs twice; agree vs bargain vs refuse; refuse then agree; released vs broken; wrong speaker, remote, dead; Indexer refusing the Searcher's verbs and vice versa; other villages hold neither resident. Q4: this section was read against the shipped files.

**Files.** NEW `StillleafSaltVault.cs`, `StillleafIndexerTests.cs`, `stillleaf_indexer.png`; MOD `StillleafArchiveContent.cs`, `Objects.json` (`StillleafIndexer` after `CurationSorter`, `StillleafFileCabinet` after `Chest`), `Content/Conversations/StillleafArchive.json` (`StillleafIndexer_1`), `OverworldZoneManager.cs`, `EnvironmentSpriteRenderer.cs`, `EnvironmentSpriteRendererHarnessTests.cs`, `GameAuditEntityEquipmentContentTests.cs`.

### SA.4 — Custody and aftermath (19 September 2026)

**Status:** shipped. RED gate `SA4-red` (CS0103, `StillleafCustody` did not exist); focused GREEN `SA4-green` 887/888, the one failure a self-contradictory setup of mine (below); full EditMode suite in the clone: 15,083 tests, 15,083 passed (`SA4-full`).

**What the player can now do.** With the register in hand — or deliberately left inside — the player enacts one of the three testimonies, physically and once:
- **Deliver it to Quillhold.** Through Hollin's conversation the register leaves the pack and lies sealed at her desk (`Takeable` off, "(sealed)" in its name, examine text saying who sealed it and that nobody will say what the entries said). Standing with the Recension rises by 10. If Curation was promised the record, that promise is broken in words and in standing: −10 for *agreed*, −4 for *custody not promised*, nothing if nothing was promised or the request was refused.
- **File it at the Salt-Vault.** Through the Indexer's conversation it goes into the salt file beside its keeper, unread; the drawer locks again; Curation standing rises by 10. Hollin promised nothing for its absence and loses nothing.
- **Reseal it in place.** At the opened vault door — a real [C] world action the door advertises only while open — with the keeper's key carried, the register inside and the player outside beside the door, the lock and the door's solidity return; it costs a turn's labour and pays no one. Each refusal names its reason (`no_key`, `carrying_register`, `register_not_inside`, `not_beside_the_door`, `doorway_occupied`, `already_sealed`, `custody_decided`) and changes nothing.

Whoever did not receive the record can be told the truth, once, unpaid and unpunished; the lines are specific to what was done and what was promised. The journal completes if it is open; custody is the player's even after releasing the errand. Custody is one-time and mutually exclusive: after resealing, carrying the register out later reopens nothing.

**Implementation.** `StillleafCustody` (`Assets/Scripts/Gameplay/World/StillleafCustody.cs`) owns `deliver`, `file`, `report` behind the chain's shared action/predicate names and the `StillleafReseal` world action; `SealedLibraryBarrierPart` answers `GetInventoryActions` with reseal while open; `InputHandler` dispatches it beside the Morrowfast world actions (the generic event path has no turn cost). "Inside" is a flood from the register's cell with the door closed that never reaches the player. The register placed in SA.1 now carries `CompleteObjectiveOnTaken("register")`, so the real `PickupCommand` advances the journal to *custody*. Player state: `StillleafOutcome` (1 delivered, 2 filed, 3 resealed), `StillleafToldSearcher`, `StillleafToldIndexer`. Diag: `quest`/`StillleafArchiveApplied|Rejected` with `command`, `outcome`/`reason`.

**Scope divergence from the plan.** None of substance: the plan's "the door's state follows the choice" is realised only by the reseal (the other two leave the door as the player left it, which is the truthful state), and "the Field Note records the outcome" is the completed journal entry plus the outcome-specific log lines rather than a separate note.

**Self-review (Methodology Template §5).**
- 🟡 *Fixed pre-commit (test-side):* the `no_key` refusal scenario tried to reach the door without the key, so the earlier `already_sealed` gate fired — the code's precedence is right (door state before key). The scenario now opens the door with the key and puts the key down before resealing.
- 🧪 *Deferred to SA.5:* standing for a broken salt file; a register destroyed or dropped in the wild; save/load at each custody state; the reseal dispatch through native keys (SA.6).
- 🔵 *Noted:* the delivered register lies on Hollin's own cell (as Morrowfast lays the cloth on the table); a shelf slot would be nicer art, not truer.
- ⚪ Guard state unchanged (`release-candidate` topology/manifest; `main` only).

**Cold-eye Q1–Q4.** Q1: reseal inverts exactly what the bump-unlock flips (`IsLocked`, `Solid`), nothing more; the dispatch block is the Morrowfast block with the chain's names. Q2: every verb emits the same `quest` kinds; standing deltas are named constants; ids stay at top level. Q3: deliver vs file vs reseal; each terms value against its standing delta; refusal reasons one by one; told vs not told; released errand vs open journal; before vs after the choice. Q4: this section was read against the shipped files.

**Files.** NEW `StillleafCustody.cs`, `StillleafCustodyTests.cs`; MOD `StillleafArchiveContent.cs`, `StillleafArchive.cs`, `SealedLibraryBarrierPart.cs`, `InputHandler.cs`, `Content/Conversations/StillleafArchive.json`.

### SA.5 — Robustness and the adversarial sweep (19 September 2026)

**Status:** shipped. Stub-phase RED `SA5-red` (23 tests: 5 red on their own assertions, 18 pinned green); GREEN `SA5-green` 269/270 (the one failure a false premise of mine, below); corrected focused gate `SA5-green-2` 43/43; full EditMode suite in the clone: 15,106 tests, 15,106 passed (`SA5-full`).

**Method.** The taxonomy sweep (boundary inputs, foreign zones, cross-actor, lost participants, anti-exploit, save/load reach, diag contracts) plus hypothesis-driven RED tests written *before* re-reading the code, each hypothesis stated in its docstring. Because the file references two new Part types, a bare RED run would have been one compile error; a stub phase (types and constants only) let each hypothesis fail on its own assertion, which is the record used for the classification below.

**Four gaps found and fixed.**
1. *Journal stuck:* accepting the errand after the file was read and the key taken opened the journal at *words*, whose objective could never fire again. `accept` now fast-forwards finished work in order (file, key, register), as Morrowfast's already-recovered parcel does. Two pins.
2. *Dangling loss:* a destroyed register left nothing to do. `StillleafRegisterPart` marks the player on `Destroyed`; either resident's *report* then closes the chain — `StillleafOutcome = 4` (lost), journal removed, nothing failed, nothing paid — with loss-specific lines.
3. *Free theft:* breaking the salt file spilled the key (native behaviour) at no cost. `StillleafFilePart` charges −8 Curation standing once, by name, when the player is the source, and the Indexer's telling reads the mark. Anyone else breaking it costs the player nothing.
4. *The slate:* a thief who never met the Searcher holds the keeper's slate once the register is picked up, and the slate carries the last words. `Taken` by the local player now grants the words; the index answers them.

**A false premise, corrected.** The first out-of-order hypothesis assumed the vault door could be broken; `DestructionSystem.IsBreakable` is false for it (its `Destructible` is flagged indestructible). The world offers exactly two ways in — the words, or theft of the key — and the hypothesis was rewritten on the theft path. That rewritten test was first run against the shipped code, so behaviour 4 has no recorded RED of its own; it is stated here rather than implied.

**Pinned as correct (18).** Nulls and nonsense never throw and never act; foreign zones are refused by name; an NPC taking the register or the key advances nothing and teaches the player nothing; a hostile Searcher offers nothing; an impostor register is not the record; filing with the key still inside makes room; a dead Searcher after acceptance still lets custody complete but cannot be told; a dead Indexer after release still leaves the key takeable; a dead player changes nothing; manual placement in the unlocked file is not filing and can be undone; someone else's breakage is free; every rejection names a reason and never claims success; and save/load keeps player chain state, a resealed door with both latches, the salt file with its lock and its key, both residents' identity and voice, and a delivered register's seal.

**Self-review (Methodology Template §5).** 🟡 the four gaps, fixed. 🧪 bounded by the hypotheses imagined; no fuzzing. 🧪 the reseal has not been driven by native keys (SA.6). ⚪ guard state unchanged.

**Files.** NEW `StillleafRegisterPart.cs`, `StillleafFilePart.cs`, `StillleafArchiveAdversarialTests.cs`; MOD `StillleafCustody.cs`, `StillleafArchiveContent.cs`, `Objects.json` (the two parts added to their blueprints).

### SA.6 — Native proof and close-out (19 September 2026)

**Status:** shipped. Native run `SA6-stillleaf-native`: 102/102 checks, validated (33 captures, 1,414 queued native steps, 348 s); full EditMode suite in the clone 15,109 tests, 15,109 passed (`SA6-full`; three new pins).

**What was played, with real keys.** The accepted native journey now continues from Morrowfast into the chain: `<` to the world map, WASD across world cells, `>` down into Quillhold; Hollin's contract read and accepted by labelled dialogue shortcuts, the journal opened with Q; on to the Salt-Vault, the words spoken, the file read, Curation's terms agreed, the released salt file opened in the native loot UI and the key taken; on to Stillleaf, F12 declared for the descent, both real stairs used, the sealed door opened by the ordinary bump, its native menu inspected (reseal offered, not executed), the register taken with G, back up, F12 restored; back to Quillhold to deliver — journal completed, Recension +10, Curation −10 for the broken agreement, no repeat offer — and to the Salt-Vault to tell the Indexer, unpaid. Twenty-one `stillleaf_native_*` checks and eight captures were added to the validator's required set; the exact capture count is pinned at 33.

**Two runs failed first, and both are kept.** `SA6-stillleaf-native` died on arrival at the Salt-Vault: the leg helpers waited for voxel readiness in every zone, and the Salt-Vault is a generic village with the native presentation — the very fact SA.3 pinned. The wait is now guarded by `VoxelWorldPresentation.IsSupported`. `SA6-stillleaf-native-2` then passed the arrival, the reading and the agreement and died approaching the salt file: the install preferred the most wall around the file, and with seed 64 its only passable neighbour was the Indexer's own seat, so a real player could never open it. That is a production defect the EditMode pins could not see (the SA.3 reachability pin ignored creatures). `StillleafSaltVault` now requires two bare neighbours — one for the Indexer, one to stand at — pinned RED→GREEN for three seeds (`SA6-fix-red`, `SA6-fix-green`), full suite `SA6-full`.

**Captures inspected at 1080p.** `stillleaf-quillhold-contract` — Hollin Vesk's dialogue in Quillhold's voxel stacks with the real choices (carry the words / not now / farewell). `stillleaf-journal` — the quest log page: *What Stillleaf Kept*, stage *words*, the full Field Note with the words and "six chunks south and one chunk east of Quillhold", stages key/descend/custody listed. `stillleaf-salt-vault-file` — Teodra Halm at the Salt-Vault (native 2D village presentation), the log carrying the reading verbatim, "Objective complete" and "Quest updated". `stillleaf-key-taken` — "You open the salt file: Stillleaf. It contains 1 item(s)." then "You take Stillleaf keeper's key", carried weight 13→14. `stillleaf-vault-door-open` — the vault floor in voxels, the opened door's native menu reading "You see a sealed archive door. [unbreakabl…] r) reseal the vault (keeper's key) / x) examine", snapjaw hunters hitting for 0 under the declared F12. `stillleaf-register-taken` — inside the vault on archive paving, "you picks up the Stillleaf register", weight →16. `stillleaf-register-delivered` — Hollin's line, "Quest complete: What Stillleaf Kept", "Your reputation with the Recension improves", "…with the Pale Curation worsens", and the broken-agreement sentence. `stillleaf-told-curation` — Halm's "Entered: register delivered to Quillhold, against terms agreed…" with no report choice left. Observed and recorded, not fixed: dialogue node text is caught mid-reveal by the capture (choices and log are complete); the pre-existing "you unlocks" / "you picks up" grammar of generic messages; and the player arriving "parched (badly)" — world-map steps advance the clock ten turns each, so a real player must carry water for this journey and the Field Note does not yet say so (backlog).

**Honesty bounds.** Native proof covers the *deliver* outcome only, with the *agreed* terms; filing, resealing, refusal, theft, loss, out-of-order arrival and save/load at each stage are EditMode-covered (SA.4, SA.5) and not native-captured. F12 was on for the sinkhole descent because its guardians are not the subject; every other step ran vulnerable. World-map travel is the game's own layer; walking the overland chunks between these places is not exercised. The ProfilerRecorder window predates these legs. No readability, accessibility or enjoyment claim.

**Close-out cold-eye (whole chain, Angle A).** Diag: every install emits `worldgen`/`<Thing>Placed|Refused` with a `reason`; every verb and the world action emit `quest`/`StillleafArchiveApplied|Rejected` with `command` and `reason`/`outcome`. Verbs: Searcher `accept`/`release`; Indexer `retrieve`/`agree`/`bargain`/`refuse`; custody `deliver`/`file`/`report` plus the `StillleafReseal` world action — one shared action and predicate name, dispatched by ownership. Player state is eight int properties, all serialized and pinned. Angle B (Qud-parity) does not apply: this is CoO-original campaign content with no parity claim. The dedicated adversarial sweep (SA.5) covered the taxonomy and found four gaps, fixed. Canon guardrails hold: no Mystery Ledger entry is resolved, Naro and Urqu are never named, the Salted is not spent, refusal is closure.

**Publication.** GAME-STATE checkpoint and RELEASE-STABILIZATION R5 entry record the chain as shipped with these bounds; roadmap step 3 is met for one chain — "only then multiply large arcs."

