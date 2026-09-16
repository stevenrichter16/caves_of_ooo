# Still-Crewed Vehicles — mobile dungeons that are still running their last order

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-16): derelict walkers, sand-skiffs and sunken
> barges as crewable, semi-sentient mobile dungeons, haunted by
> memory-echoes of their last crew who must be appeased, fought off, or
> recruited before the vehicle answers a new pilot — "get a car" as a
> questline instead of a purchase. Plus: they roam, maybe randomly, maybe
> in patterns dictated by lore and environment. **And the lead question:
> "haunted" isn't the right term for this world.**

## 1. It isn't haunted — it never closed

Correct instinct. This world has no ghosts, and it doesn't need them,
because it already has two mechanisms that fit better and are load-
bearing canon:

- **The closure-ledger** (`11_SecondSpine.md` C4): what feeds the
  world's decay is *acts left dangling without a spoken end*. A crew
  that died mid-voyage left the voyage **open**.
- **The under-text** (`TERMS.md`, the Unsaying): people who are Unsaid
  are not destroyed, they are **scraped** — faint, still legible, partly
  recoverable by underreading.

Put together: **the vehicle is still executing its last standing order,
because nobody ever told it the voyage ended**, and the crew is still
faintly written on it. What you meet aboard is not a spirit. It is an
**unclosed account with people still in it.**

That reframing is worth more than a word swap. It converts a horror
trope into this setting's actual physics, and it means the vehicle's
behavior is *legible*: it does what it was last told to do.

### The word, in the world's own established pattern

Canon never settles on one name for a thing — the Felling is the
Falling-Due to the Concord, the Severance to the Curation, the
Dissolution to others, and the Recension keeps four accounts and refuses
to pick. Do the same here rather than coining one term:

| Who | What they call it |
|---|---|
| Deep villages / common folk | **still-crewed** — plain, eerie, needs no gloss |
| The Recension | **unclosed**; "an open entry" — the voyage was never entered as finished |
| The Concord | **under charter**; "an outstanding manifest" — it is still working off a contract |
| The Rot Choir | **unlaid** — they lay down and were never gathered |
| Tent-Right / the Namers | **unspoken** — nobody ever said they were gone |
| Pale Curation | **unfiled** |

**Recommended player-facing default: "still-crewed."** It is folk
register, immediately understandable, faintly horrible, and it never
needs a codex entry. The faction terms live in their dialogue, and a
player who talks to everyone slowly learns that six institutions are
describing the same object.

## 2. Four ways to take the helm

The pitch's "appeased, fought off, or recruited" maps onto lore-native
verbs — and each is a different faction's answer, so acquiring a vehicle
expresses your build:

1. **Close the account.** Finish the last voyage: carry the cargo where
   it was going, arrive where it was due. The order completes, the crew
   rests, the vehicle is yours. **The best option and the most
   questlike** — the vehicle's own history is the quest, so every
   still-crewed craft ships with a bespoke objective for free.
2. **Countermand it.** A Namer speaks an end to the standing order —
   the Tent-Right practice of the enacted, spoken *no*
   (`BUILD-EXPRESSIVITY.md` §3.7). Fast, clean, requires the Naming
   line, and it is the only method that leaves the crew *neither*
   finished nor gathered.
3. **Inherit it.** Become the crew: recruit the under-text crew via the
   follower system, or let the Choir gather them. The vehicle answers
   because you are now who it was waiting for.
4. **Scrape it.** Erase the order violently. Fast, ugly, permanently
   damages the vehicle (a scraped craft is less than it was), and
   offends the Recension, the Namers, and the Choir simultaneously.

Four paths, four registers, no "correct" one — consistent with how the
project handles its endings.

## 3. Routes: patterned, published, and learnable

**Not random.** The frame in §1 dictates the answer: a still-crewed
vehicle is *running its last route*, so **its pattern is its history**.
A sand-skiff still makes the Concord circuit it made a thousand years
ago. A barge still follows its river. A walker still patrols a border
that no longer exists.

Three consequences, all good:

- **Routes are researchable, not stumbled upon.** The Concord keeps old
  manifests; the Recension has the routes filed. Finding a vehicle is
  knowledge work — consistent with the knowledge-economy identity in
  `TELLS-AND-WINDUPS.md` §8 and `COUNTER-CHAINS.md` §5. The Concord will
  of course sell you a timetable that is half right.
- **Terrain constrains them**, so the roster writes itself: barges need
  water (the `RiverChunk` POI type already exists), skiffs need open
  ground, walkers can cross what nothing else can — which is the whole
  reason to want one.
- **With a calendar, routes become timetables.** "The barge passes Tine
  every ninth day." Catching one becomes planning rather than luck.
  (Note: this is now the **third** design asking for the same small
  calendar primitive — `NPC-SCHEDULES.md` and `COMPOST-ECOLOGY.md` want
  it too. Build it once.)
- **Deviation is a signal.** A vehicle *off* its route means its order
  changed, or something aboard changed it. Free mystery hook.

## 4. As a dungeon

The interior is a zone, the way a lair is — and the crew are encounters
that are *readable* rather than merely hostile. Underreading
(`UNDERREADING.md`) recovers what they were doing; a Recension scribe
aboard can take their testimony; the Choir wants to gather them.

Two properties that make it more than a reskinned lair:

- **Clearing it is optional and possibly wrong.** You may want the crew
  (path 3), and killing them forecloses that.
- **It is inhabited while you pilot it.** A vehicle you took by
  scraping is a vehicle you live inside with the residue of what you
  did to it.

## 5. Honest scoping — this is the most from-scratch pitch yet

| What it needs | State |
|---|---|
| A vehicle / mount / pilot system | **Nothing.** Zero matches for vehicle, mount, ridden, piloted |
| An interior space | Partial: `Cell.IsInterior` is a real per-cell flag with goals that use it (`MoveToInteriorGoal`/`MoveToExteriorGoal`); zone-as-interior is the lair pattern |
| A POI that moves | **New.** `PointOfInterest` is static data (Type, Name, Faction, Tier, BossBlueprint) living in a fixed `PointOfInterest[,]` grid — there is no concept of position over time |
| Crew you can recruit | **Exists** (followers, F.3) |
| A questline per vehicle | Exists (storylets) |

**So the v1 that gets most of the fantasy for a fraction of the work:
build the derelict as a stationary dungeon first.** A still-crewed barge
aground in a river bend is a lair with a better premise, a bespoke
questline drawn from its own manifest, and four resolution paths — all
of which is achievable with shipped systems. **Then** make it move.

Piloting a mobile zone across a world map is a genuinely large piece of
engineering and should be earned by proving the fiction first.

## 6. Risks

- 🔴 **Moving zones are hard.** A zone that changes world-map position
  touches zone caching, save/load, transitions, and the question of what
  happens to a zone the player is standing in while it moves. Do not
  start here.
- 🟡 **A vehicle trivializes travel**, which is where a lot of the
  game's content lives. Constrain hard: terrain limits, upkeep, and the
  fact that a still-crewed craft has opinions about where it goes.
- 🟡 **The crew must not be a loot piñata.** If killing them is the
  fastest path, nobody will ever pick the other three. Scraping should
  be the *worst* outcome mechanically as well as morally.
- 🔵 **Term drift** — if six factions have six words, the player-facing
  UI must consistently use one ("still-crewed") or it reads as
  inconsistency rather than as texture.

## 7. Build order

1. **One stationary still-crewed derelict** as a dungeon, with its
   manifest as the questline and all four resolution paths. Pure content
   plus a small amount of storylet work on shipped systems.
2. **The crew as under-text**: readable, recruitable, gatherable —
   reusing underreading and followers.
3. **A second and third derelict** in different biomes, to prove the
   pattern generalizes and that the four paths feel different.
4. **The calendar primitive** (shared with schedules and compost), then
   **published routes** as researchable documents — vehicles still
   stationary, but now they have *known* histories and locations.
5. **Then**, and only then, mobility: a vehicle that actually moves
   between world-map cells on its timetable.
