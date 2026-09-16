# Disease — environmental hazard, weapon, and the one only this world can have

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented
> as a *system* — but see §0, because far more of it ships than anyone
> would guess. User pitch (2026-09-16): a disease mechanic, either as a
> semi-realistic environmental hazard and/or as spells and weaponry.

## 0. Verification sweep — the framework is roughly 80% built

**`FungalInfectionEffect` is already a complete, tested, contagious,
multi-stage disease.** It ships with:

| Stage | Turns | Behavior |
|---|---|---|
| Incubation | 0–9 | silent; no damage — you don't know you have it |
| Symptomatic | 10–19 | 1 dmg/turn, −1 Toughness |
| Blooming | 20–29 | 2 dmg/turn, −2 Toughness, **spawns spore gas at the host's cell** |
| Terminal | 30–39 | 3 dmg/turn, −3 Toughness, larger contagion burst |
| Expired | 40+ | self-removes |

It has save-round-trip resilience, a deliberate non-refresh-on-reapply
rule (reinfection doesn't reset your progression) pinned by tests, and
**working contagion** through the shipped gas system.

Two more findings that matter more than they look:

- **`Effect.TYPE_DISEASE = 16384` already exists** — and nothing uses
  it. Even the one actual disease in the game is typed
  `TYPE_GENERAL | TYPE_NEGATIVE`.
- **The transmission-vector vocabulary already exists as type flags**:
  `TYPE_RESPIRATORY`, `TYPE_CONTACT`, `TYPE_CIRCULATORY`,
  `TYPE_METABOLIC`, `TYPE_CHEMICAL`, `TYPE_NEUROLOGICAL`,
  `TYPE_PSIONIC`, `TYPE_STRUCTURAL`, `TYPE_TEMPORAL`. That is an
  airborne / contact / bloodborne / ingested taxonomy sitting entirely
  unused.

So this is not "build a disease system." It is **generalize one shipped
disease into a family, and start using flags that are already there.**

## 1. Generalize the template

Turn the hardcoded fungal case into a data-driven one: a disease
definition carries stages, per-stage effects, an incubation window, a
transmission vector (a type flag), an infectivity, and a **resolution** —
recover, become chronic, or kill. JSON-authored, registry-loaded, in the
shape `LootTableRegistry` and `BrewRules` already use.

The fungal infection becomes the first entry in that registry rather
than a special case, and its existing test suite becomes the template's
regression suite.

## 2. Vectors — the environmental-hazard half

Each vector is a different *way to be careless*, which is what makes a
hazard interesting rather than a tax:

- **Respiratory** — spore blooms, gas, enclosed rooms, unventilated
  caves. Already has a complete delivery system.
- **Contact** — handling infected corpses, creatures, or objects. **This
  is where it fuses with the body lattice** (`BODY-LATTICE.md`): armor
  covering a node blocks contact transmission at that node. Armor now
  prevents disease, not just damage, which makes coverage gaps matter
  twice.
- **Circulatory** — wounds as entry points. An open, damaged node is how
  you catch something from a bite.
- **Metabolic** — food and water. The villages already ship a
  permanently *Fouled well* as static set-dressing (noted in the FUN-P0
  fun-gap analysis). Disease turns that existing content into a live
  hazard and a quest hook with no new authoring.
- **Vector-borne** — a **symbiote** (`BARNACLE-SYMBIOTES.md`) that
  carries something. The thing on your arm is a host.

## 3. The second family: a disease of naming

Biological plague is generic. **This setting can have something no other
game can**, and it costs nothing in new fiction because it is already
canon: *"A person who is Unsaid forgets what they are."*

Treat the Unsaying as **communicable**. A nominal disease's symptoms
aren't fever:

- you begin forgetting your own name — and the interface shows it, your
  own character sheet degrading,
- NPCs who knew you start not recognizing you,
- the **Names** you earned (`BUILD-EXPRESSIVITY.md` §10) slip one by one,
- your possessions stop being reliably *yours*.

**And the cure is social, not medical.** You are cured by **being
named** — someone who knows you says your name, a Recension scribe finds
your entry and reads it back, a Tent-Right host names you guest. A
disease cured by being remembered, in a world whose central catastrophe
is a failure of naming.

That single design turns the setting's theology into a mechanic, gives
the Recension's archives an emergency-room function, and makes isolation
genuinely dangerous in a way no HP number conveys: **a hermit cannot be
cured of this.**

## 4. Weaponization — the spells-and-weaponry half

**The cheap path already exists.** The liquid-coating system
(`WeaponTemperingService`, `LiquidCoveredEffect`) already applies
liquids to weapons. A blade coated in a culture transmits on hit via the
circulatory vector. That is a plague weapon with almost no new
engineering.

- **Spore satchels and gas grenades** — already proposed in
  `BUILD-EXPRESSIVITY.md` §9.1 D, already deliverable by the gas system.
- **Choir-aligned infection spells** — and note the framing: to the
  Choir this is *not* a weapon, it is **evangelism**. Casting inclusion
  on someone is a sincere kindness. That is the faction's established
  register and its horror in one mechanic.
- **The Driving Bloom's strain** is the aggressive, addictive, driven
  version — canon already has a ~120-year Choir–Bloom war to draw on.
- **Consequences must exist.** Using disease as a weapon should carry
  real faction and ledger cost. The game should let you do it and make
  you own it.

## 5. It connects to nearly everything designed this session

- **Compost ecology** (`COMPOST-ECOLOGY.md`): corpse piles breed. A
  compost farm is an epidemic risk, which gives that feature its natural
  environmental cost.
- **Body lattice**: disease degrades node integrity; wounds admit it;
  armor blocks it.
- **NPC schedules** (`NPC-SCHEDULES.md`): an epidemic **breaks a
  village's schedules** — the sick stay home, the market thins, the
  forge goes cold. That is the most legible possible signal, and it
  costs nothing once schedules exist.
- **Glacier Clock**: plague-dead go unburied, unburied dead are
  abandonment, abandonment feeds a front.
- **The Catchers** (`09_ImminentArchive.md`): their answer to a plague
  is to preserve the sick *before* they die. A quarantine of the
  salt-cured living is the most horrifying, most in-character content
  this mechanic could generate.

## 6. What "semi-realistic" should and shouldn't mean

**Take:** incubation before symptoms, so you carry and spread it
unknowingly — this is the single most important realistic property,
because it makes *you* a vector and creates guilt. Also: variable
severity, partial treatments rather than binary cures, and immunity
after recovery (which rewards surviving something awful).

**Don't take:** an epidemiological simulation. No R-values, no
population modeling, no infection curves. The fantasy is "I was careless
and now something is wrong with me and I may have brought it home," not
a spreadsheet.

## 7. Risks

- 🔴 **Permanent village depopulation.** This is a persistent-world RPG
  with no run to restart. A plague that spreads freely through NPCs can
  erase hand-authored content forever. Guards needed: NPC infection
  should be **authored or bounded** (named NPCs resist, villages have a
  floor, epidemics are scripted events rather than free simulation)
  until proven safe.
- 🟡 **Tedium.** Constant sickness management is miserable. Diseases
  should be rare, consequential, and finite — not an upkeep bar.
- 🟡 **The player as accidental plague-bearer** is a great story and a
  terrible surprise. Make carrying something detectable *before* it
  wipes a village — a symptom, a scribe's warning, a smell.
- 🔵 **Interaction volume**: disease × lattice × symbiotes × compost is a
  lot of cross-system surface. Ship the vectors one at a time.

## 8. Scope-prune (v1) and build order

**Cut from v1:** the nominal/Unsaying family (§3 — ship biological
first); NPC-to-NPC spread (§7's red risk); weaponization; immunity;
epidemics as events.

1. **Generalize `FungalInfectionEffect` into a registry-driven disease
   definition**, with the fungal case as entry one and its existing
   tests as the regression suite. Start using `TYPE_DISEASE`.
2. **Two more diseases on two different vectors** — one respiratory
   (reusing gas), one metabolic (the fouled well, which is already
   placed content). Player-only infection.
3. **Treatment**: partial cures, the existing alchemy system as the
   pharmacy, a village healer who can help and a Concord charm that
   half-works.
4. **Weaponization** via the shipped liquid-coating path, with faction
   consequences.
5. **Then** the nominal family and its social cure, bounded NPC spread,
   epidemics as authored events, and the Catcher quarantine content.
