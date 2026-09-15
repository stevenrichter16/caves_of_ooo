# Trapdoor Tactics — collapsing floors, and who is allowed to cause them

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-15): cave floors have visible weak points that
> either combatant can collapse mid-fight, deliberately or by accident,
> dropping whoever stands there into the layer below — verticality as a
> tool rather than scenery. **With the refinement that matters:** it
> should *not* be "anyone can break the floor under an enemy at any
> time." It needs a gate — a skill, a Strength threshold, or something
> else. §2 is the answer to that question.

## 0. Verification sweep

| Assumption | Actual state | Impact |
|---|---|---|
| Vertical levels would need building | **Fully built.** Zone IDs are `Overworld.X.Y.Z` with Z=0 surface and Z>0 underground; `CaveBuilder`, `StrataBuilder`, `SolidEarthBuilder`, `CaveEntranceBuilder` all generate them | The layer below already exists and is already generated |
| Moving between layers would need building | **Fully built.** `StairsDownPart`/`StairsUpPart` entities route through `ZoneTransitionSystem.TransitionPlayerVertical(...)`, returning a `ZoneTransitionResult` that `InputHandler.HandleZoneTransition` consumes | A trapdoor is an *involuntary, non-stairs* vertical transition — it can reuse this exact call path rather than inventing one |
| Falling exists in some form | **Zero.** No fall damage, no involuntary transition, no pit/chasm concept anywhere (`FallDamage\|Falling\|PitPart\|Chasm` — no matches) | The fall itself is the genuinely new mechanic |
| Weight exists for "bait the heavy enemy" | Half-true, and this is the important correction. `PhysicsPart.Weight` (in pounds) exists and 106 blueprints set it — but **only items do**, read only by `HandlingService.GetWeight` for inventory load. Snapjaw, CaveBear, AncientGuardian and the Player **do not carry a PhysicsPart at all** | The load-triggered half needs a creature-weight content pass, directly analogous to the FUN-P0 natural-weapon pass that armed 20 hostiles |
| Forced movement exists to push someone onto a weak tile | **Yes**: `Cudgel_GroundPound` (knockback), `Axe_HookAndDrag` (forced drag), `Acrobatics_Tumble` | The "shove them onto the bad tile" combo already has verbs; no new push mechanic needed |

## 1. The tile

A **weak floor tile** carries two authored numbers and one state:

- **Load rating** — the weight it holds before it gives way.
- **Integrity** — how much deliberate abuse it takes to break on purpose.
- **Visibility tier** — how obvious it is (see §2, tier 3).

It does not need a new terrain system: a weak tile is an entity on the
cell with a `WeakFloorPart`, the same way furniture, stairs, and gather
nodes already sit on cells today.

## 2. The gate — three tiers, not one button

The pitch's instinct is right: one ungated "collapse the floor under
anyone" verb would be both trivializing and characterless. The fix is
that **collapsing, breaking, and *seeing* are three different things
with three different gates.**

### Tier 1 — Load-triggered collapse (ungated, environmental)

Standing on a weak tile with weight over its load rating collapses it
on its own, after a short delay. No skill, no ability, no button — it is
physics, and it applies to everyone equally.

This is the entire "bait a heavy enemy onto thin rock" and "watch your
own escape route give way" fantasy, and it requires **no ability at
all**. The player's tool here is *positioning*, plus the forced-movement
verbs that already exist: shove the armored brute onto the tile with a
ground pound and let the floor do the work. The skill expressed is
tactical, not a purchased power.

### Tier 2 — Deliberate breaking (gated: Strength + heavy weapon + your action)

*Choosing* to smash a floor is the part that needs the gate the user is
asking for. Recommended gate, using patterns the codebase already has:

- **A Strength threshold**, scaled against the tile's integrity — a
  strong character breaks stone-thin floors a weak one cannot dent.
- **A heavy weapon class** — gated exactly like every existing ability
  of this shape, via `SkillCombatHelpers.FindEquippedWeaponOfClass`
  (Cudgel and Axe qualify; a dagger or an empty hand does not).
- **It costs your action**, and it can fail on sturdier tiles. It is a
  tempo investment, not a free removal.

So a cudgel bruiser can open a hole under someone on purpose. A scribe
with a knife stands on the same tile and can only hope something heavy
walks onto it.

### Tier 3 — Seeing the weak floor (the skill line, and the best gate)

**This is the gate I would build the feature around.** The interesting
question is not "can you break it" but "can you *see* it" — because that
makes the same battlefield a different room for different builds,
without giving anyone a button.

- **Untrained:** you see only the obvious ones — a visible sinkhole,
  rotted planking. These are authored to be legible to everybody, so the
  feature is never invisible to a new player.
- **Trained** (the Deep root's **Root-Sense**, already proposed in
  `Docs/Design/BUILD-EXPRESSIVITY.md` §3.6 as "feel the zone's
  structure: ore, water, hollows, what's under a wall" — this pitch is
  its combat application): hairline weakness nobody else can read.
- **High rank:** the actual **load rating** — you know the tile will
  hold *you* and not the thing chasing you. That is a complete tactical
  plan delivered as information rather than as damage, which is exactly
  what a perception-style line should sell.

### Tier 4 — The enemies do it too

A creature that can see weak floor will bait *you* onto it, and a heavy
enough creature will collapse one by accident and fall without anyone
intending it. The system should be symmetric or it becomes a one-way
player toy.

## 3. The fall — and the exploit this must not become

Dropping an enemy through the floor is a very strong effect, and the
obvious degenerate line is "drop everything down a hole, win." The
guard, stated as a design rule:

**A drop is tempo, not removal.** The dropped creature lands in the
zone below, takes fall damage scaled by distance and its own weight, and
**can path back up the stairs.** It buys you several turns and splits a
pack. It does not delete an encounter. A boss you drop is a boss you
will see again shortly, angrier and closer to the stairs.

For the player falling, the interesting consequence is not damage, it is
**location**: you land in a zone you had not cleared and were not ready
for. That is a far better punishment than a number, and it is free —
the zone below already generates.

## 4. The hole persists — and that is a feature

The project's identity is RPG, not roguelike: world state persists
(`Docs/PROJECT-IDENTITY.md`). So a collapsed floor should **stay
collapsed** — a permanent passage between two levels that the player
made.

That produces genuinely good emergent play: a player can deliberately
engineer a shortcut into a level they will be revisiting, and a cave
system slowly acquires the scars of every fight held in it. It also
means "I collapsed the floor" is a decision with a consequence outliving
the fight, which is the kind of permanence the project already commits
to elsewhere.

## 5. It fuses with Tells & Windups

A floor about to give way is **a tell** — dust, a crack, a creak — with
a resolve tick, which is precisely the structure proposed in
`Docs/Design/TELLS-AND-WINDUPS.md`. Build them to share a mechanism:

- The delay between "over-loaded" and "collapses" is a windup, visible
  as world state, with time to act.
- The **Read** line reading a creature's intent and **Root-Sense**
  reading a floor's load are the same verb pointed at different things,
  and should share vocabulary.
- Anyone standing on a telegraphing tile gets the window to step off —
  including the enemy, which is what stops the bait play from being
  automatic and makes *when* you spring it a real decision.

## 6. Content requirements (the honest cost)

1. **Creature weight pass** — every creature needs a `PhysicsPart` with
   a real `Weight`, since none has one today. Same shape and size as the
   FUN-P0 natural-weapon pass. This is the bulk of the work, and it is
   content, not engineering.
2. **Weak-tile placement** in the cave/strata builders — a small
   authored density per biome and depth, not random scatter, or the
   floor stops feeling trustworthy anywhere.
3. **Fall damage curve** and the landing-cell resolution rules (what
   happens if the cell below is solid, occupied, or out of bounds).

## 7. Scope-prune (v1) and build order

**Cut from v1:** deliberate breaking (tier 2) — ship the environmental
half first and confirm falling is fun before adding a verb for it;
enemy use of weak floors (tier 4); permanent-hole persistence across
save/load if it complicates the zone-diff work; multi-level falls.

1. **`WeakFloorPart` + the fall.** Involuntary vertical transition
   reusing `ZoneTransitionSystem.TransitionPlayerVertical`'s path, fall
   damage, landing-cell resolution. Diag records on load-exceeded,
   collapse, and landing.
2. **Creature weight content pass** (§6.1) — nothing in tier 1 works
   until creatures weigh something.
3. **Telegraph presentation** — the creak/dust windup as visible world
   state, shared with the Tells system if that ships first.
4. **Tier 3 perception** (Root-Sense ranks) — the gate that makes it a
   build, not a gimmick.
5. **Then** tier 2 deliberate breaking, then enemy usage, then
   persistence.
