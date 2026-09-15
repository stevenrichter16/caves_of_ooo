# Counter-Chains — the face-off as a rare, configurable set-piece

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> Grows out of `Docs/Design/TELLS-AND-WINDUPS.md`. User pitch
> (2026-09-15): very few NPCs should have the tells/counter capability,
> so meeting one is an event — a swing gets countered, the counter gets
> countered, and the exchange resolves as an automated face-off. Plus:
> the player can assign **preset chains** of skills and spells to run as
> a counter-attack, and counters become highly configurable.

## 1. Scarcity is the mechanic, not just the flavor

Only a small, named roster can read tells and counter. That restraint
is doing three jobs at once, and the feature does not work without it:

1. **It solves the pacing problem** that the parent doc flags as this
   family's standard failure mode. If every creature countered, every
   fight would become a chain resolution and combat pace would die.
   Rare counter-capable opponents mean ordinary fights stay ordinary.
2. **It keeps the prepared loadout expressive instead of mandatory.**
   If countering were universal, every player would be forced into the
   same defensive configuration. Because it is rare, a prepared chain is
   something you build *for* a known duel, or carry as insurance.
3. **It makes the encounter a set-piece.** A face-off can afford
   special presentation, special music, a special log format — precisely
   because the player meets a handful of them, not a hundred.

### Who has it

Counter-capable should read as *trained*, never as a stat block:

- **Named duelists, faction champions, and bosses.**
- **The Declined** — Naro's lineage, whose entire practice is the
  enacted refusal. `Docs/Design/BUILD-EXPRESSIVITY.md` §3.7 already
  proposes **The No** as their signature: a spoken refusal that closes
  an act aimed at you. **A counter *is* a refusal.** Two Declined
  fencing is two people refusing each other's refusals, which is the
  single most thematically-loaded fight the setting could stage.
- **Bower-Folk Finishers** (motion as composition) and **senior
  Recension body-readers** (they read the living the way they read the
  dead) as secondary, flavored cases.
- **Never:** vermin, wildlife, rank-and-file. A snapjaw swings and eats
  what it eats.

## 2. How an exchange resolves

An exchange is a chain of links. Each link is: someone declares, the
other answers from their prepared loadout, and the answer itself becomes
a declaration the first party may answer in turn.

```
A: Gutting Lunge        (declared, windup)
   B: Riposte           (prepared counter clears its threshold)
      A: Turn the Blade  (prepared counter-to-a-counter)
         B: — no valid entry, no energy —
→ A's Turn the Blade resolves. Exchange over.
```

### What stops it from being infinite

**The energy ledger terminates it on its own.** The parent doc's §5
establishes that a partial cast spends exactly the energy the window
affords. Every link in a chain is paid for out of the responder's
future actions, and each successive link happens in a *tighter* window
than the last — the exchange accelerates. A chain ends when someone
cannot pay, has no chain entry matching the situation, or fails a
threshold profile.

That is a natural, in-fiction terminator rather than an arbitrary
"maximum three counters" rule. (A hard safety cap should still exist,
because a bug here is an infinite loop in the turn scheduler, not just
a long fight.)

### Countering a counter must be harder than countering an opener

Otherwise whoever swings first always loses, and the correct play
becomes "never attack." So difficulty escalates per link: each
successive response has a tighter window, and may require a higher Read
rank to perceive at all. Deep chains should be rare and memorable, and
the fourth link should feel like a small miracle.

**Momentum ties in for free:** if each landed link builds a stack per
`Docs/Design/MOMENTUM-COMBAT.md`, a long exchange escalates in damage as
it goes, so the person who finally lands the terminating blow hits
hardest. The exchange has a natural crescendo without any special-casing.

## 3. The configurable chain

A chain is an **ordered list of condition → response entries, evaluated
top-down, first match wins.** This is the Gambit/tactics shape, and it
is the right one: readable, debuggable, and configurable without
becoming a programming language.

```
1. IF incoming is Heavy      → Slip
2. IF incoming is Quick      → Riposte (prepared: Shortblade Flurry)
3. IF my HP below 30%        → Brace
4. IF chain depth ≥ 3        → Disengage
5. (otherwise)               → Brace
```

### This is not a new scripting language

The codebase already ships a data-driven, JSON-authored, registered
condition vocabulary for conversations — 25 predicates including
`IfHaveItem`, `IfHaveTag`, `IfFact`, `IfReputationAtLeast`,
`IfFactionFeelingAtLeast`, `IfHaveIntProperty`, `IfNot`. Chain
conditions should follow that exact shape and authoring convention,
extended with a small combat-local vocabulary:

| New condition | Reads |
|---|---|
| `IfIncomingCategory` | heavy / quick / grapple / ranged / spell — gated on Read rank (parent doc §3) |
| `IfIncomingNamed` | the specific attack, high Read only |
| `IfChainDepth` | which link of the exchange this is |
| `IfSuspectFeint` | capstone Read only — see §5 |
| `IfSelfHP` / `IfTargetHP` | thresholds |
| `IfPrepared` | is this ability still available and affordable |

**Depth-aware entries are where chains get personal.** "Slip the first
exchange, riposte the second, disengage by the fourth" is a *fencing
plan*, and two players with different plans produce genuinely different
duels against the same opponent.

## 4. Where the player's skill actually lives

The obvious critique of any automated-resolution system is that the
game plays itself. Three answers, in order of importance:

1. **The skill was expressed in writing the chain.** This is a
   preparation game, which is exactly what "poker, not reflexes" implies
   — the face-off is the *reveal* of two prepared strategies colliding.
   That is a distinct and real pleasure, and it is the point.
2. **Any entry can be marked "ask me."** Automate the links you don't
   care about; hand-play the crux. The configuration is what decides
   which links are which, so a player who wants to hand-play everything
   can, and a player who wants it to run can let it run.
3. **The chain is a fallback, not a cage.** A manual override at any
   link is always available for the cost of attention.

## 5. Reading a chain — the highest-skill play

Feints (parent doc §4) are **chain-breakers**, and this is where the
system peaks. If you know your opponent's chain, you bait the entry you
want: telegraph a Heavy, watch them spend their Slip, then punish the
recovery. Conversely, an opponent who knows *your* chain does the same
to you.

Which makes **an opponent's chain a piece of knowledge, and therefore
content**:

- **Fight them and survive** — each exchange reveals entries, logged
  for later study.
- **The Recension files duels.** A famous duelist's known repertoire is
  a document, and the Recension will trade it for testimony — precisely
  how this world treats every other kind of knowledge.
- **The Concord sells one too**, half-accurate, caveat emptor, which is
  their established comic register and a live opportunity to hand the
  player a confidently wrong chain.

This makes the rematch a genuinely different fight from the first
meeting, without authoring a second encounter.

## 6. Presentation — the duel transcript

Because face-offs are rare, they can afford a distinct presentation: the
exchange resolving as a legible sequence rather than a wall of ordinary
combat lines. Every link should emit a diag record anyway (house rule:
every gate emits one), which means **the transcript is nearly free** —
the same data that makes the exchange debuggable makes it readable.

In-fiction, that transcript is a Recension entry. Let the player keep
it.

## 7. Honest risks

- 🔴 **The chain editor is the most expensive part of this feature**,
  and it is UI work, not systems work. `AbilityManagerUI` exists and
  can host a simple prepared-loadout list (parent doc §5), but an
  ordered condition→response editor is a bigger screen. **Do not start
  here.** Ship chains as JSON-authored NPC behavior first, with the
  player getting a short fixed-slot version, and build the full editor
  only once the resolution model is proven fun.
- 🟡 **First-mover disadvantage** if counter-difficulty escalation is
  mistuned: if countering is too easy, nobody attacks. Tune the
  window-tightening per link before anything else.
- 🟡 **Infinite-loop safety.** A chain bug is a hung turn scheduler.
  Hard cap the depth regardless of the energy terminator.
- 🔵 **Legibility.** If the player cannot tell *why* their chain picked
  entry 3, the system is opaque and frustrating. The transcript (§6)
  must name the entry that fired, not just the ability.

## 8. Scope-prune (v1) and build order

**Cut from v1:** the player-facing chain editor (§7); learning opponent
chains as tradeable documents (§5); the Concord's wrong-chain gag;
special face-off presentation beyond the transcript.

1. **Two-link exchanges only.** A declares, B counters, resolve. No
   counter-to-the-counter yet. This proves the interrupt-into-declare
   plumbing and the energy accounting without any chain machinery.
2. **One counter-capable NPC**, hand-authored, with a hardcoded
   two-entry chain. Confirm the encounter is fun before generalizing.
3. **Chain data model + JSON authoring** for NPCs, reusing the
   conversation-predicate shape, plus depth escalation and the hard cap.
   Player gets a fixed short prepared list, no editor.
4. **Deep chains** (3+ links) and the transcript.
5. **Then** the player-facing editor, then chain-reading as content,
   then the wider counter-capable roster.
