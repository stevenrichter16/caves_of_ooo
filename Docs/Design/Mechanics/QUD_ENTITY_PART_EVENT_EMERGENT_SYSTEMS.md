# Qud Entity-Part/Event Architecture: How Emergent Systems Fall Out of the Design

## Purpose

This document explains how Caves of Qud's `GameObject` + `IPart` + event pipeline creates *emergent* gameplay without requiring hard-coded one-off interactions for every feature pair.

Rather than encoding behavior in deep inheritance trees ("all swords do X"), Qud composes entities out of independent parts and lets those parts negotiate outcomes through shared events.

---

## 1) Architecture in One Sentence

A **GameObject** is a container of **parts** (`IPart`), and game outcomes are resolved by **broadcasting events** through those parts (and effects), allowing multiple independent systems to cooperatively shape one action.

---

## 2) Core Building Blocks

## 2.1 GameObject as behavior host

`GameObject` is not just data storage; it is an event routing host.
It owns:

- identity/state/stat properties
- a collection of parts (capabilities)
- the event dispatch path that asks parts/effects to respond

This creates a strong separation:

- **what an entity is** = which parts it has
- **what happens now** = which events are fired and how handlers modify them

## 2.2 IPart as capability modules

Each `IPart` contributes one narrow slice of behavior (combat modifier, movement constraint, inventory logic, cybernetic rule, etc.).

A part typically exposes:

- interest declaration (`WantEvent` / registration pattern)
- response logic (`HandleEvent`)
- lifecycle hooks (attach/remove/turn/end-turn style hooks)

Because parts are composable and mostly local in scope, designers can add new behavior by adding parts rather than rewriting central control flow.

## 2.3 Event layer as negotiation protocol

Qud uses both string-like legacy events and typed `MinEvent` derivatives. Typed events matter because they:

- make payload contracts explicit
- support gating/veto patterns
- support cascade levels and multi-pass flows
- allow many systems to participate in one action safely

The key idea: one player action creates an event *conversation* among relevant parts.

---

## 3) Why Emergence Happens

Emergence appears when independent parts react to shared events with local rules.
No single system knows the full final result ahead of time.

A useful mental model:

1. intent event begins (e.g., move, apply effect, equip, spend resource)
2. many parts inspect/modify/deny/augment
3. final outcome is assembled from layered decisions
4. side-effect events fan out (logging, achievements, AI updates, UI feedback)

That pipeline turns small reusable rules into high combinatorial variety.

---

## 4) Five Major Emergent Patterns

## 4.1 Capability emergence (composition over inheritance)

If an object has `A + B + C` parts, it can produce behavior that no single part "owns."

Example pattern:

- physics part determines solidity
- equipment part modifies stats
- effect part mutates resistances
- movement/event part changes traversal outcomes

Result: same action (e.g., stepping, attacking) behaves differently based on current composition, with no custom subclass required.

## 4.2 Constraint emergence (veto chains)

Many events are intentionally gate-like: handlers can veto the action.

This enables rich constraints from independent systems:

- anatomy constraints
- phase/state constraints
- inventory availability constraints
- status/effect constraints

The player perceives this as a coherent world rule set, but it is implemented as many local vetoes over common intent events.

## 4.3 Resource-flow emergence (cascades + multi-pass)

Currency/liquid/resource operations can cascade across many holders/containers through multi-pass events.

This generates realistic behavior:

- partial contributions from many sources
- retry phases with stricter/looser rules
- safe/unsafe container filtering

No monolithic "wallet" model is required; value emerges from distributed containers plus event protocol.

## 4.4 Spatial emergence (cell + zone + object dispatch)

Spatial systems resolve outcomes by iterating objects in relevant cells/zones and consulting parts.

So projectile/movement/interaction outcomes emerge from:

- local cell occupancy
- per-object solidity/cover/phase logic
- contextual events fired to in-cell objects

This yields highly situational outcomes from generic traversal code.

## 4.5 Temporal emergence (ordering and phase hooks)

Turn/end-turn ordering, part processing order, and event cascade levels create temporal semantics.

Small timing differences produce big emergent differences:

- who gets to veto first
- which effects see modified values
- whether cleanup runs before or after follow-up checks

In other words: *when* handlers run is part of game design, not just implementation detail.

---

## 5) The "Single Action, Many Systems" Lens

Consider a generic action like "apply an effect" or "use an inventory command":

- intent event created
- parts get first pass (modify or block)
- effects and secondary systems receive follow-up notifications
- dependent systems recalculate derived state
- logs/UI/scripting hooks observe outcomes

One input causes a web of subsystem participation.
This is exactly where Qud-like emergent stories come from.

---

## 6) Design Strengths of the Architecture

1. **Extensibility**: add behavior via new parts/events, not giant switch statements.
2. **Interoperability**: new part can immediately interact with old systems if it listens to shared events.
3. **Testability**: behavior can be tested as event contracts.
4. **Data-driven growth**: object identity mostly defined by part composition and properties.
5. **Designer leverage**: huge behavior surface area from relatively small code units.

---

## 7) Common Failure Modes (and Why They Matter)

The same power brings risks:

- hidden coupling via shared events
- ordering sensitivity (priority/cascade bugs)
- duplicate dispatch without dedupe guards
- hard-to-debug emergent edge cases (three systems all "correct" locally)

Qud mitigates this with patterns like first-slot dedupe, explicit event classes, and layered check/send conventions.

---

## 8) Practical Guidance for COO-style Implementations

If you want Qud-like emergence in a remake:

1. Keep parts narrow and composable.
2. Treat events as contracts (typed where possible).
3. Preserve veto/modify/notify separation.
4. Make ordering explicit and documented.
5. Prefer distributed resource/state ownership over central god-systems.
6. Add instrumentation for event traces early (debugging emergent stacks is otherwise painful).

---

## 9) Bottom Line

Qud's emergent depth is not a mystery layer on top of the game.
It is a direct consequence of:

- compositional entities (`GameObject` + `IPart`)
- a shared event negotiation protocol (`Event` / `MinEvent`)
- explicit ordering/cascade semantics
- spatial and temporal dispatch tied into the same architecture

The result is a system where complex gameplay is *assembled at runtime* from many simple local rules.
