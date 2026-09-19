# The Stillleaf Archive — the first middle-game chain

**Status:** plan and verification sweep, 19 September 2026. No production code yet. Release roadmap step 3 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap; [GAME-STATE](GAME-STATE-2026-09-17.md) §15, §20). CoO-original campaign content; no Qud parity claim.

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

(appended per sub-milestone)
