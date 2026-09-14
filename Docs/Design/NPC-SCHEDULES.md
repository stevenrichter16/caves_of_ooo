# NPC schedules — the one missing ingredient is time

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-14): NPC schedules — villagers who do different
> things at different times of day instead of standing in place forever.

## 0. The discovery: this is not a new architecture

Grepped the codebase for `Schedule`/`TimeOfDay`/`Calendar`/`Routine`
first — zero matches, so the concept genuinely doesn't exist yet. But
the machinery a schedule needs to *sit on top of* is already fully
built and already in production:

- **`BoredGoal`** (the bottom-of-stack default goal every idle NPC
  runs) already has an eight-step decision ladder: hostile-scan first,
  then `AIBoredEvent.Check(entity)` — an explicit, already-used hook
  point for "let a Part inject custom idle behavior" — then a one-shot
  destination override (`WhenBoredReturnToOnce`), then home-position
  return (`BrainPart.Staying`/`StartingCellX/Y`), then furniture
  scanning, then wander/wait.
- **Five actor-side idle-behavior modules already ship**:
  `AIWellVisitorPart`, `AIGuardPart`, `AIPetterPart`,
  `AIUndertakerPart`, `AIFleeToShrinePart` — all hooking
  `AIBoredEvent`, exactly the shape a "go do a specific thing when
  idle" module already takes.
- **Furniture-side interaction already ships**: `ChairPart`/`BedPart`
  respond to `IdleQueryEvent` with an `IdleOffer` (target cell + an
  `Action` closure + a `Cleanup` rollback closure), which `BoredGoal`
  turns into `PushChildGoal(new DelegateGoal(offer.Action,
  offer.Cleanup, x, y)); PushChildGoal(new MoveToGoal(x, y, ...))`.
  Sitting in a chair, sleeping in a bed — the plumbing to walk
  somewhere and start an activity already exists and is already
  tested.

**What's missing is the one thing that turns "always eligible,
chosen opportunistically" into "eligible right now, because of what
time it is": a clock.** Every one of the systems above runs today with
no notion of time at all — a well-visitor always wants to visit the
well when bored, a chair is always sittable. Schedules are that gap,
and only that gap.

## 1. The new primitive: a Calendar off the existing tick counter

`TurnManager.Active.TickCount` already exists (a monotonic tick count,
used today by `GatherNodePart` for regrow timing via the exact
`TestCurrentTurn` override pattern this reuses). Add a small static
`Calendar`:

```csharp
public static class Calendar
{
    public const int TicksPerDay = 1200; // placeholder — needs playtesting
    public enum Period { Night, Dawn, Morning, Midday, Evening, Dusk }

    public static Period CurrentPeriod(int? testTick = null)
    {
        int tick = testTick ?? TurnManager.Active?.TickCount ?? 0;
        int timeOfDay = tick % TicksPerDay;
        // divide TicksPerDay into 6 bands, map to Period
    }
}
```

Coarse six-period bands, not hour-by-hour — that matches how this kind
of schedule is actually authored in every game that does it well
(Stardew, Skyrim's radiant schedules): a handful of named periods a
writer can reason about, not a clock face nobody will tune precisely.

## 2. Two-tier build: a near-free fix, then a general system

### Tier 1 — gate the five behaviors that already exist (cheap, ships first)

`AIWellVisitorPart`, `AIGuardPart`, `AIPetterPart`, `AIUndertakerPart`,
`AIFleeToShrinePart` all already fire on `AIBoredEvent`. Give each an
optional `ActivePeriods` field (a small list/flags of `Calendar.Period`)
and one early-return line: if the current period isn't in the set,
pass the event through unhandled. Default empty = always active
(today's behavior, unchanged) — fully backward compatible, zero risk
to existing content. This alone makes "the well-visitor only goes to
the well at dawn and dusk, not at 2am" real for five NPCs with a
one-field, one-line change each.

### Tier 2 — `SchedulePart` for ordinary villagers (the general case)

Most NPCs in a village (per `VillagePopulationBuilder`) are plain
Villagers with no bespoke behavior Part — they get `ChairPart`/`BedPart`
access today but no reason to use one over another at any particular
time. New `SchedulePart` (an `AIBehaviorPart`, same family as the
furniture parts, but living on the *NPC* rather than on furniture):

- Blueprint-authored ordered entries: `Period → destination cell (or a
  named furniture/workstation entity) → activity`. String-encoded
  params, matching the convention already used for `ReagentPart`'s
  `PropertiesRaw` and `BitLocker`'s bit strings.
- On `AIBoredEvent`: compute `Calendar.CurrentPeriod()`, look up this
  period's entry. If already there and doing it, no-op. If not, push
  the exact same `DelegateGoal` + `MoveToGoal` pair `BoredGoal` already
  builds for furniture offers — for a bed/chair destination this can
  literally reuse `BedPart`/`ChairPart`'s existing `IdleQueryEvent`
  path; for anything else (see Tier 2b) the `SchedulePart` supplies its
  own `Action` closure directly.
- Consumes the event (so the generic furniture-scan/wander fallback in
  `BoredGoal` steps 6–8 doesn't fight the schedule for control).

**Combat and drama already outrank this, for free**, because of where
`AIBoredEvent` sits in `BoredGoal`'s ladder: the hostile-scan (step 2)
runs *before* it. A scheduled villager who gets attacked drops the
schedule immediately and fights; once the fight ends and `BoredGoal`
runs bored again, the schedule resumes on its own. No interrupt system
needed — this is a consequence of the existing goal-stack priority
order, not new code.

### Tier 2b — a minimal `WorkstationPart` for activities with no furniture yet

"Sleep" and "sit" already have a destination Part. "Work a forge,"
"tend a field," "stand at a scriptorium desk" don't. Smallest possible
addition: a no-op destination marker Part (same shape as `ChairPart`
minus the sitting) that an NPC's schedule can target, optionally firing
a flavor `MessageLog` line or a diag record on arrival. Purely additive,
no new goal-stack mechanics.

## 3. What this actually buys, beyond "NPCs look busier"

- **A village visibly changes shape by time of day** with almost
  entirely reused machinery: empty streets and dark windows at Night,
  a crowded well at Morning, a full tavern at Dusk. This is the
  single highest-leverage "the world feels alive" change available,
  and it costs one new utility class plus content authoring, not a new
  engine.
- **A free diegetic tell for the Glacier Clock work** (see
  `Docs/Design/GLACIER-CLOCK.md`): a village whose local incomplete-act
  tally is climbing can *show* it — an unattended shrine at shrine-hour,
  an empty forge at work-hour — instead of only being legible through a
  bulletin. The schedule system is the rendering layer that idea was
  missing.
- **Festival/market/tithe-day content becomes playable, not narrated.**
  The Bible's tonal register explicitly leans on "the everyday whimsy
  of ordinary people" as the thing the Thinning threatens (`10_Bible.md`
  §VIII) — a named villager who goes to the well at dawn, the market at
  midday, and the shrine at dusk is the concrete, walkable version of
  that line, and the knife-tithe (`02_Recension.md` §III) or a
  Falling-Due festival day are natural schedule-authored calendar
  events once a Calendar exists to hang them on.

## 4. Scope-prune (v1)

- **No per-NPC generated schedules.** Hand-author schedules for named
  or role-bearing NPCs first (innkeeper, smith, well-visitor,
  shrine-keeper); generic filler villagers can share a small library of
  template schedules rather than each needing bespoke authoring.
- **No live schedule interruption beyond what the existing priority
  order already gives for free** (combat, drama). A more elaborate
  "the innkeeper skips the tavern tonight because of a personal grief"
  override is real content work for later, once House Drama beats are
  wired to write a short-lived `WhenBoredReturnToOnce`-style override.
- **No hour-precision clock.** Six bands, tuned by playtest, not a
  24-hour dial nobody will read that precisely.

## 5. Build order (agent-pace)

1. `Calendar` utility + tests (pure function of `TickCount`, mirrors
   `GatherNodePart.TestCurrentTurn` for determinism).
2. Tier 1: `ActivePeriods` field + gate on the five existing
   `AIBehaviorPart` subclasses — smallest possible ship, immediate
   texture win, zero new goal-stack code.
3. `SchedulePart` (Tier 2) targeting only `ChairPart`/`BedPart`
   destinations first — proves the push-order reuse before adding new
   furniture types.
4. `WorkstationPart` (Tier 2b) + a first village's authored schedule
   content, with a `GrimoireDistribution`-style reachability test
   (every schedule entry resolves to a real destination entity).
5. Defer: visual sconce dimming by period, Drama-authored schedule
   overrides, festival/tithe-day calendar events.
