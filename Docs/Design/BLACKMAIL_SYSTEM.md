# Blackmail — Design Doc

> A skill / system design for blackmail in Caves of Ooo. Synthesizes the
> existing Witness, Lineage, House, and Dialogue Beat systems into a
> derived skill tree built around weaponized secrets. Includes the
> anti-cheese constraint framework that prevents blackmail from breaking
> the game's economy.

---

## Why blackmail is worth building

Blackmail is the cleanest mechanical synthesis of everything already built
— Witness, Lineage, Houses, Dialogue Beats — because it literally
weaponizes the information those systems generate. But it's genuinely
at risk of breaking the game's economy, so the skill design and the
constraint design have to be worked out together.

---

## The core mechanical question

Before the skill details: blackmail is fundamentally a *threat*, not an
action. The distinction matters because the game has to model two states
— **"I have leverage I haven't used"** and **"I have spent my leverage"** —
and the decision to convert one to the other needs to have weight. Lots
of game systems get this wrong by making blackmail feel like instantly
trading secrets for rewards, which is the cheese failure mode.

The mental model should be: **every secret you hold is a loaded gun.
Firing it once discharges it. You cannot reload the same secret.** That
framing generates the natural scarcity that prevents overuse.

---

## Skill or derived skill: derived

Blackmail should be a derived skill, not a base one. Specifically, it
should emerge from the intersection of:

- **Mycelial Communion** — information gathering
- **Persuasion or Intimidation** (a social base skill) — threat delivery
- Optionally, a **criminal or courtly skill tree** — technique refinement

The reason it's derived: a character who can discover secrets but can't
threaten with them effectively is a journalist; a character who can
threaten but doesn't know any secrets is a generic intimidator. Blackmail
is specifically the conjunction.

This has pleasant design consequences. It means blackmail builds are
naturally investment-heavy — you're leveling multiple skill trees to
support one playstyle — which justifies the power level. It also means
the mechanic scales: a pure intimidation character can threaten *known
public information* (weak); a pure information character can *reveal
secrets* (destructive but unfocused); only the dedicated blackmailer can
**extract concessions without revelation.**

---

## The taxonomy of secrets

Not all secrets are equal. A "you slept with your sister-in-law" secret
is qualitatively different from a "your House committed genocide three
generations ago" secret. Five grades:

### Minor secrets
Personal embarrassments, small financial irregularities, forgettable
past indiscretions.
- **Leverage value:** a single small favor, some gold, minor dialogue access.
- **Discovery rate:** common — any Lineage query at Mycelial Communion
  Rank 2 might surface one.

### Significant secrets
Hidden bastards, past crimes the statute has expired on, disowned kin,
substance dependencies, broken oaths to minor figures.
- **Leverage value:** meaningful concessions, House-level aid, item or
  information trades.
- **Discovery rate:** moderately rare — require specific investigation,
  Mycelial Rank 3+, or piecing together rumor chains.

### Serious secrets
Active crimes, ongoing affairs with political consequences, secret
lineage ties to enemy Houses, broken faction oaths, heresies.
- **Leverage value:** House-scale concessions — reshape their political
  positions, demand an heir be named, force alliance changes.
- **Discovery rate:** rare — deep investigation or Palimpsest-level access.

### Lineage-shaking secrets
The House was founded on a crime, the current head is a bastard with no
legitimate claim, the ancestral mythic deed is a lie, the House's sacred
relic is fake.
- **Leverage value:** House-existential — restructure the House's
  internal politics, force abdication, claim titles.
- **Discovery rate:** very rare — requires deep Mycelial Communion,
  often multiple sources to corroborate.

### World-shaking secrets
Evidence of something that implicates a great faction as a whole,
knowledge of a cosmic scale (the founding myth is wrong, the gods are
silent), evidence of coordinated wrongdoing across multiple Houses.
- **Leverage value:** faction-restructuring.
- **Discovery rate:** extremely rare — main-quest tier discovery, often
  requiring Palimpsest abilities or specific endgame content.

Each grade has different discovery rates, different leverage costs, and
— critically — different detection and retaliation rates. See the
constraint section.

---

## The skill tree

Four ranks. Note what's *not* in this tree: nothing that generates
secrets. Secret acquisition stays in the Mycelial Communion / investigation
trees. This keeps the investment heavy and forces players to engage
with the game's discovery systems rather than just grinding blackmail
skill.

### Rank 1 — Pressure
Threaten to reveal a known secret in exchange for a minor concession.
Leverage is consumed on use; the threat is made, the target capitulates,
the secret is now discharged. (You can still reveal it — but it no
longer extracts further value from this target.) Only works on Minor
and Significant secrets.

### Rank 2 — Leverage
Maintain a *standing threat* — the target provides ongoing small services
as long as the secret remains undisclosed. Creates a relationship rather
than a one-off transaction. Works on Significant secrets. Opens up
**blackmail retainers** — a House member who pays you off regularly.

### Rank 3 — Extortion
Make large demands against Serious secrets without immediate retaliation.
Introduces **graduated pressure** — ratchet the demand up or down based
on target compliance. Also opens up **secret trading** — using one
House's secret to acquire another House's secret from a third party.

### Rank 4 — Architectonics
Engineer leverage networks — hold multiple secrets across multiple
Houses and use them to orchestrate political outcomes. This is where
blackmail stops being a resource extraction tool and becomes a **political
engineering tool.** You're not getting paid; you're reshaping the
political topology.

---

## The anti-cheese mechanisms

This is the heart of the design. A blackmail system that's unbounded
breaks the game. Constraints in layers:

### 1. The "use once" rule
The most important rule: a given secret, against a given target, can
only be cashed in **once**. Using it converts it from leverage into
either a completed transaction (Rank 1) or an ongoing relationship
(Rank 2+). Either way, you cannot repeat-threaten to extract escalating
value. The target has already decided how much the secret is worth;
they won't pay again for the same information.

You *can* reveal the secret after extracting the concession. This ends
the relationship permanently. The target has lost their reputation hit
and you've lost future leverage — but in some cases that's exactly what
you want (to punish them, to destabilize a House, to clear the board
for political engineering).

This rule alone caps blackmail from becoming infinite resource
extraction. Every use is a real decision about final value.

### 2. The leverage-to-cost ratio
Concessions have to feel proportional. Threatening to reveal a minor
affair should not extract faction-scale concessions. The game enforces
this with a simple grade-check: when you make a demand, the target
evaluates it against the secret's grade. **Demands that exceed what
the secret can plausibly extract are simply refused.** The target calls
your bluff, effectively daring you to reveal something that won't
destroy them.

This forces discipline. You don't threaten with a Minor secret and ask
for a House's heir to abdicate. If you do, they refuse, you reveal,
their reputation takes a small hit, and you've wasted your leverage.

### 3. Mutual destruction — the secret can reveal you too
Every act of blackmail is itself witnessed, potentially, by the target.
A high-intellect target can piece together how you learned their secret,
which exposes your own methods, your Mycelial practices, your informants.
Blackmail a House, and that House now has an investigation running on
you. What do they find?

This is the elegant part. If you have your own secrets — broken oaths,
Palimpsest kin-rewrites, suspicious death counts — the blackmail target
can potentially counter-leverage you. The Witness and Lineage systems
already track your deeds; blackmailing an investigation-capable target
exposes you to **counter-blackmail.** Now both parties have secrets on
each other, creating a stable mutual-destruction truce rather than
lopsided exploitation.

### 4. Rumor network contamination
Every blackmail attempt, successful or not, leaves traces in the Witness
rumor network. Not the secret itself — but the fact that you approached
a specific House member with unusual leverage. Other Houses notice. Over
time, your reputation as a blackmailer accrues. That tag:

- Makes future blackmail attempts harder (Houses are on guard)
- Unlocks dialogue with criminal and courtly factions who respect the
  capability
- Triggers counter-intelligence efforts from Houses you haven't even
  targeted yet
- Creates assassination risk from the specific House of blackmailed
  individuals

You cannot blackmail invisibly forever. The world adapts.

### 5. Secrets have half-lives
Secrets lose value over time. A scandal that's ten years old has less
leverage than one from last season. A bastard heir grown to adulthood
is less useful to threaten with than one still a child. The Lineage
system's time-tracking naturally supports this — when you discover a
secret, its age and the target's current vulnerability to it affect the
leverage value.

This prevents hoarding. A player who discovers a great secret at hour
10 and tries to save it for the perfect moment at hour 60 may find it's
decayed substantially. Use-it-or-lose-it pressure keeps the economy
moving.

### 6. Grade-based retaliation scaling
The crucial anti-cheese mechanism. Every grade of secret has an
associated retaliation probability and severity when blackmail is used.

| Grade | Retaliation pattern |
|---|---|
| Minor | Target complies, mild resentment, no retaliation. Safe. |
| Significant | Target complies but starts a grudge. If they later acquire counter-leverage or opportunity, they'll move against you. Moderate risk. |
| Serious | Target complies visibly but begins active countermeasures. Hires investigators, places spies in your network, may attempt assassination. High risk. |
| Lineage-shaking | Target complies but the House enters existential threat mode. You are now a problem that must be eliminated. Very high risk of active hunting. |
| World-shaking | Using these at all triggers faction-level response. You're not just being hunted by a House; you're being hunted by coordinated forces. Extremely high risk. |

The math: the higher the leverage, the higher the risk of eventual
catastrophic retaliation. A player who ladder-climbs through Serious
and Lineage-shaking blackmail is accumulating enemies who are
specifically organized to destroy them. This caps the strategy at a
natural ceiling — pure blackmail builds get powerful in the mid-game
and then spend the late-game surviving their own accumulated enemies.

### 7. The "they already know" failure case
Some secrets turn out not to be secrets. The House head's affair? His
wife already knew; they have an arrangement. The bastard child? Publicly
acknowledged within the House, kept from outsiders as a formality. The
ancestral crime? The current generation quietly repudiated it decades
ago; revealing it would generate awkwardness, not scandal.

The game should occasionally let blackmail attempts fail because the
"secret" isn't actually one. This is realistic — not every hidden fact
is load-bearing — and it punishes careless use. Players who blackmail
without investigating the secret's *current status* get burned. This
rewards the engaged, patient player over the one who treats secrets as
fungible resources.

### 8. Social cost: the "I knew it" crowd
In cultures with strong privacy norms, being known as someone who uses
blackmail carries social cost even from non-targets. A merchant who's
never been blackmailed still finds blackmailers distasteful and adjusts
prices accordingly. A House that values honor (warrior cultures
especially) will refuse to ally with known blackmailers regardless of
whether they've been personally targeted.

This creates a reputation tradeoff. Using blackmail gains you short-term
concessions at the cost of long-term social access. Certain factions
become permanently unavailable to you if your blackmail reputation
exceeds a threshold. Your friend pool shrinks.

### 9. Consent matters for repair
Houses that capitulate to blackmail are functionally hostile to you.
They comply but they don't trust you. Future water-oaths, alliances, or
deep cooperation with a blackmailed House are much harder — not
impossible, but expensive. The player who blackmails early locks
themselves out of the full relational range with that target later.

This is important because it means **blackmail and genuine diplomacy
are in tension, not complementary.** A player can't blackmail their way
to a House's friendship. They can only blackmail their way to a House's
compliance.

---

## What this shape does mechanically

The design produces these properties naturally:

- Infinite blackmail is impossible (single-use per secret, time decay,
  contamination reputation)
- Large-scale blackmail is risky (retaliation scales with grade)
- Blackmail is a tradeoff with other social strategies (can't blackmail
  into genuine trust)
- Secret discovery remains a gameplay loop (leverage has to come from
  somewhere)
- Political engineering is possible at high cost (Rank 4 Architectonics
  is endgame tier)
- Blackmail has a natural competition with diplomacy and violence
  (different tradeoffs, not strictly better or worse)

---

## What blackmail gets you that nothing else can

The point of all this constraint is to make blackmail meaningfully
different from other forms of influence. After all the limits, what's
left?

The answer: **blackmail uniquely grants compliance without displacement.**
A House that's been blackmailed keeps all its power, its position, its
role — and does what you need. You haven't killed their leader; you
haven't replaced them with your ally; you haven't publicly shamed them.
The House continues to function normally, and the world sees continuity.

This is what no other mechanic provides:

- **Violence** removes a House from play.
- **Diplomacy** requires the House's agreement.
- **Political engineering** requires visible maneuvering.
- **Blackmail** is the only tool that produces hidden changes in House
  behavior — the House does what you want without anyone else knowing
  it's doing what you want.

That's why blackmail is worth building, despite the constraint
complexity. It's not just a lever; it's a specific lever with no
equivalent in the game's other systems.

---

## Cultural flavor

The skill tree should be named after a concept specific to the world —
something like **The Held Breath**, **Inkwork**, **Secret-Keeping**, or
**The Whispered Coin.** The name should suggest both the patience
required to hold leverage and the moral weight of using it. Qud's water
ritual gets its cultural specificity from its phrasing
("your thirst is mine, my water is yours"); the blackmail system
deserves similar cultural embedding.

A possible ritual-phrase for the moment of threat: *"I know the shape
of your hollow."* Specific, evocative, and unambiguously threatening
while staying in-voice for a mystical setting. The target's response
carries the weight of the whole system — do they refuse and dare the
revelation, or do they swallow their pride and pay?

---

## Constraint summary (one place)

To keep the design tight, the anti-cheese framework is:

1. Each secret used is consumed
2. Leverage is grade-capped and refused if exceeded
3. Blackmail exposes you to counter-blackmail
4. Rumor networks create a Blackmailer reputation over time
5. Secrets decay in value over game-time
6. Retaliation scales with grade, uncapped at the top
7. Some "secrets" aren't actually secret and using them fails
8. Social cost accrues from non-targets based on reputation
9. Blackmailed Houses cannot become genuine allies without heavy recovery

Any one of these alone wouldn't be enough. Together, they produce a
system where blackmail is **potent, specific, and self-limiting** — the
player will use it strategically a handful of times per playthrough
rather than grinding it as a resource extraction loop.

That's the shape of a blackmail system that feels Qud-like: powerful,
morally weighted, emergent from systems already in play, and inherently
constrained by the world's reaction rather than by arbitrary cooldowns.
