# C6 — recognizable local people

**Status:** implemented; standalone core/adversarial/review 63/63 pass. Independent blind
voice review incorporated. Native EditMode passed the original 59 cases; four new
save-review cases await the final editor sweep. Native keyboard acceptance is pending.
This is CoO-original local identity and grounded conversation, with no cosmology
or quest rewrite. Root approved the generation, conversation UI and save seams;
Objects.json remains root-owned and does not need new name fields.

## Verification sweep and exact scope

| Premise | Verified source | Bounded decision |
|---|---|---|
| Every visible proper name denotes a unique | Hermit huts repeat titled Mosskeeper/Saltwalker/Herbalist/Lamplighter roles | Never infer uniqueness from capitalization or conversation presence |
| Generic villagers are the only generic residents | VillagePopulationBuilder makes role NPCs; stamps add faction specialists | Explicit allowlist of 27 generic role blueprints below |
| A name can be assigned at ObjectCreated | Quest builders overwrite Villager display/conversation/IDs afterward; Morrowfast authors its cast separately | Run only after fresh generation and authored overrides; never cached/save attachment |
| Blueprint names can become personal names | Quest predicates, saves and canonical hosts depend on those keys | Modify Render.DisplayName and additive identity properties only |
| Named Choir people already have unique placement | Repeatable GroveShrine stamps create all five on every successful grove | Claim only these five individual people; generic Choir entities remain repeatable |
| Choir people have a protected coordinate host | All source references point to GroveShrine; BloomFront only recognizes/vetoes their presence; lore gives relationships but no coordinate host | First successfully placed grove owner per person, with persistent claim; no moving or respawning an owner |
| Global singleton facts are adequate ownership | Detached worlds and failed candidate loads coexist with a live game | Manager-owned ledger, serialized as a Part on that world's saved World entity |
| Shared conversation data can be edited per speaker | ConversationLoader returns shared data; DialogueUI directly reads CurrentNode.Text | Add a read-only per-speaker rendered-text seam; never mutate shared nodes/actions/predicates |
| A local line can assume a shop or repair exists | Generated owner/site presence can change through death/repair | Resolve live owners and saved settlement state at read time; no generation or made-up services |

### Generic name roster (27)

Ordinary roles (16): Elder, Villager, Weaponsmith, Armorer, Apothecary,
Arcanist, Provisioner, Tinker, Merchant, Quartermaster, Warden, WellKeeper,
Farmer, Innkeeper, Undertaker, Scribe.

Faction/local roles (11): PalimpsestEcho, SaccharineEnvoy, ConcordFactor,
PaleCurator, TentRightHost, SaltMaster, RecensionScribe, CurationSorter,
PeatCutter, FilerClerk, GantryRegistrar.

A roster member is eligible only if its visible name still equals its authored
role name and its conversation is still the blueprint's authored conversation.
Quest owners and custom IDs/displays are not renamed. Preserve all existing
proper names, four hermit titles, all Morrowfast residents, Hollin Vesk, Teodra
Halm, Warden Nossik, Founding listeners/tenders, the Plaque-Tender, EncasedElder,
ChoirTendril, GlassblownDrifter, gods, children and every non-allowlisted entity.
Do not invent a personal identity for a collective or protected silent remnant.

Names use constructed CoO pools keyed to the actual faction/culture. Support
Villagers, Palimpsest (display culture Recension), SaccharineConcord, PaleCuration,
BowerFolk, TentRight and CatacombFolk. Unknown culture is an explicit no-op.
Names retain the role in the visible label. Seed + zone + sorted local placement
choose names with a private stable hash, never consuming generation/combat RNG.
Reserve existing visible names and all protected proper names; probe the finite
pool deterministically for collisions. Exhaustion preserves the role and records
refusal instead of looping or silently duplicating a personal name. Store chosen
name/culture/role properties; repeated application does not alter them. Existing
saves retain their existing names; new zones in an old game receive new names.

### Unique people (five, explicit)

| Frozen blueprint | Visible name |
|---|---|
| Mogu | Solm |
| Grib | Vurn |
| Nam | Amai |
| Sien | Isk |
| Sopp | Ketch |

Retain the first successfully placed actor's ID and owning zone in a bounded
LocalPeopleLedgerPart. Remove only newly generated duplicate actors before the
zone is published; this is spawn suppression, not combat death, and produces no
loot/corpse/XP. Never delete items, unrelated NPCs, cached actors or authored quest
owners. A dead, removed or unloaded owner retains the claim and cannot respawn.
Separate managers have separate ledgers even with the same seed.

Save the ledger through the existing World entity Part graph; no save format
change. Bind before World is written, and restore only onto the candidate's own
manager after entity bodies resolve. A save without a World may create a minimal
World only when claims actually exist. Reject trying to attach another manager's
live ledger rather than replacing it. Old saves without a ledger adopt existing
cached owners into an empty ledger without renaming/removing cached actors; old
already-duplicated actors are preserved as pre-release save contents, but future
new duplicates are blocked. Counts/keys are validated and bounded on load.

## Grounded local conversation

First pass replaces only Villager_1 Start, PassingThrough and Dangers rendered
text for eligible generic Villager/Undertaker speakers attached to a real
settlement. All other authored lines, choices, predicates and repair/quest
conversations are preserved. Each supported culture receives a short local
observation, a practical lead to an actually present living conversational owner,
and a response to the existing well's broken/temporary/stable/improved state.
When no service survives, the lead says it cannot direct the visitor to one.
No directions to imagined shops, no new work claims, and no assigning global
mystery answers to local witnesses. Detached, dead, foreign-zone or hostile
speakers cannot produce a new local answer. CurrentNode.Text remains unmodified.

## Voice intent cards and review gate

Read: Lore/MYSTERY-LEDGER.md; Lore/Voices/VOICE-CARDS.md; ROTCHOIR_VOICES.md;
Lore/10_Bible.md §IX. Construct names; do not lift living-culture sacred words or
names. No line resolves Naro, Root dreams, the Tree's origin, tenth fire, doll,
Glassblown Remnant, Bower's sight, Gin Frogs, or the depth/count of old writing.

- Recovered-world folk: plain neighborly chores, weather and water. Give useful
  local information; never sound like an elder reciting a lore document.
- Recension: attributed observation, precise uncertainty. Give a living source;
  never sound like comic pedantry or assert an unattested account.
- Concord: practical terms/rates. Give explicit guidance; never imply a nonexistent
  paid service or gratuitous greed.
- Pale Curation: condition/method/status. Report bodily/local state; never use
  villainous bureaucracy or declare a resident dead in its voice.
- Bower-Folk: placement/light and permission. Describe an arrangement; never use
  usefulness as aesthetic praise or explain what the Bower sees.
- Tent-Right: water/shade offer before questions, precise host/guest terms. Never
  demand business or identity before the third day.
- Catacomb folk: light, kin, domestic tending. No cave-oracle tone or explanation
  of the Sealed Dim/Root's private dreams.

Authored lines need a fresh-context blind attribution/intent review before ship;
self-assessment is not acceptance. No new lines are approved merely by this plan.

## RED, counter-checks and acceptance

1. Actual fresh-zone generation gives deterministic role-preserving names; same
   seed/zone replay, different seed and finite collision cases; pure hash consumes
   no gameplay RNG. Authored proper names/conversations/IDs and quest owners are
   controls. A cached old zone and loaded old save are never renamed.
2. Two successful grove placements currently clone the five people: RED first.
   GREEN preserves first IDs/items/roles and suppresses only second actors. Failed
   placement, same-zone replay, distinct managers, death, unload/regenerate and
   full save/load exercise persistent ownership; no spurious death drops.
3. Actual ConversationManager navigation and rendered text vary by culture and
   real settlement. Dead/missing service, absent context, hostile speakers,
   repaired/broken states and changing state while talking are counter-checks.
   Compare shared node/action/predicate data before/after.
4. Dedicated adversarial sweep after GREEN: malformed ledger, field bounds,
   save isolation, name exhaustion/collision, custom names and content drift.
5. Native acceptance: keyboard-talk to generated residents in at least two
   contrasting cultures, inspect role labels and changed-state response; revisit
   and save/reload a claimed grove without clones. No native claims until observed.

## Ownership

This agent owns LocalPeople helper/ledger, new C6 tests + metadata, this doc,
narrow OverworldZoneManager generation hook, ConversationManager rendered-text
seam, DialogueUI caller and SaveSystem ledger save/restore hooks. Root owns
Objects.json, editor scheduling, integrated documentation and commits/pushes.

## Implementation and focused evidence

The implementation uses one small fresh-generation hook and read-only rendered
conversation text. `LocalPeopleLedgerPart` saves a sorted, bounded five-record
claim ledger in the existing World entity graph. Names and identity properties
use the existing entity serialization. No Objects.json changes are needed.

| Stage | Exact evidence | Result |
|---|---|---|
| Frozen pre-C6 integrated gameplay/content | `LocalPeople/baseline-source-manifest.json` | 2,290 source/content hashes; independent of concurrent Assets edits |
| Core RED on that snapshot | `LocalPeople/core-red.json`, raw compressed XML | 29 cases: 26 expected feature failures, 3 existing controls pass |
| Shared implementation + adversarial | `LocalPeople/focused-green.json`, raw compressed XML | 59/59 pass: core 29, adversarial 30; includes reviewed prose |
| Blind voice attribution and intent | `LocalPeople/voice-blind-review-combat.md` | Seven sets distinguishable as groups; no Mystery Ledger answer or unsupported service promise found |
| Prose revisions | `LocalPeople/voice-review-revisions.json` | Five substitutions: shorter Curation/Concord speech; Recension describes current word/account rather than inventing a written record |

The RED fixture originally assumed an authored Hallun ID; inspection showed the
quest builder gives Hallun a custom conversation and label but retains a numeric
factory ID. That assertion was corrected before the canonical RED run. A later
adversarial diagnostic assertion reused numeric actor IDs across test factories
and accidentally matched older trace records. The test now uses unique IDs;
`rejected-diag-fixture-id-collision.xml.gz` preserves that rejected fixture run.
Neither correction changed production behavior.

## In-phase self-review and honesty bounds

- 🟡 **Repeated factory IDs are not an ownership token — fixed.** A newly created
  actor can reuse a saved numeric ID. Claim replay accepts the same live object
  reference only; different/same-zone reused-ID counter-cases both pass. The
  serialized claim remains after death/removal/unload and never authorizes a clone.
- 🔵 **Invented local written record — fixed in prose.** Blind review correctly
  questioned “entry/record”: the query knows well state, not a physical ledger.
  Recension now attributes current word/account. The state query is unchanged.
- 🔵 **Old-save test name overstated its evidence — corrected.** The core
  cached-owner roundtrip invokes the new writer, which binds a ledger before
  writing. It proves migration-on-save, not a missing-ledger legacy load, and was
  renamed accordingly. Four follow-ups pass: actual ledger-free loaded-world
  reconstruction preserving legacy duplicates, new personal identity full-session
  roundtrip, and null-World full saves with/without claims. The absent-part check
  uses the actual post-load seam rather than an archived old-version binary.
- ⚪ **Finite name pool and legacy duplicates are explicit limits.** Each supported
  culture offers 64 combinations per generated zone. Exhaustion keeps the role and
  records refusal. Cached pre-release duplicate Choir people remain unchanged;
  future fresh duplicates are suppressed. This avoids rewriting a player's save.

Q1/Q2: fresh generation is the only identity mutation seam, while save restore
only reattaches/adopts claims. Success/refusal records share worldgen, actor,
zone and reason shapes. No extra ID is duplicated in payloads. Q3: authored
labels/IDs/quests, same/reused identity, world isolation, death/removal/unload,
name exhaustion, RNG preservation, malformed streams and absent/hostile/dead
conversation context have matching controls. Q4: scope is CoO-original identity
and voice; it does not claim Qud name-generation parity or globally unique
procedural names. The four follow-up save checks required no production correction.

**Can verify:** the standalone runner exercises actual factory generation,
conversation navigation and live-state text selection, save graph serialization,
claim survival, parsing rejection and diagnostics. The seven actual settlement
fixtures cover all supported cultures under its deterministic hashing patch.

**Cannot verify yet:** native dialogue layout/readability, keyboard conversation,
real editor save/reload, the feel of names in play, or exact Unity generation
maps. Standalone hashing differs from Unity. Blind prose review does not prove
native context gates; those are separate acceptance checks still due.

Native EditMode: parent ran all 59 original C6 cases in a 141/141 GREEN integrated
group after voice polish. The four save-review cases were first private GREEN and
are now published in `DensityLocalPeopleReviewTests`; final native sweep remains
pending for those four. This does not replace the native keyboard acceptance.

### Final independent listener-liveness counter-check

A cold-eye review by the combat agent found a real asymmetric gate: a listener
with positive effective HP but an existing death-handled flag could still receive
local speech. The dedicated RED had one failure plus five passing neighboring
controls. DescribeConversation now rejects death-handled listeners as it already
rejected death-handled speakers. Reverse hostility was deliberately unchanged:
the outer ConversationManager gate asks whether the speaker is willing to talk,
not whether the listener would attack.

Receipts: `LocalPeople/dead-listener-red.json` / compressed XML and
`LocalPeople/final-combined-green.json` / compressed XML. The final private group
passes **154/154**, including **64 C6 cases** (29 core, 31 adversarial, four save
review). The native 59-case result above remains the evidence for the earlier
subset; the four save-review cases and new listener case await the final native
sweep. No native result is inferred from standalone success.
