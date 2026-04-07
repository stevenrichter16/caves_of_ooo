# Caves of Qud Source Analysis: Break Strategies

This document converts source-level findings into reproducible, high-leverage strategies.

All findings are based on decompiled code in this workspace.

## Scope and framing

- This is a mechanics analysis, not a speedrun route.
- Any strategy below can be version-sensitive.
- File references are included so behavior can be re-verified quickly.

## Strategy 1: Polygel duplication economy loop

### What it breaks

- Item economy and access to rare resources.
- Especially strong when targeting high-value containers/liquids.

### Why it works in code

- Polygel deep-copies the selected inventory target.
- It does **not** wipe generic liquid contents on the copied item.
- It only strips nested inventory and clears energy/ammo sockets.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Polygel.cs:49`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Polygel.cs:58`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Polygel.cs:59`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Polygel.cs:60`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Polygel.cs:64`

### Setup

1. Acquire `polygel`.
2. Carry target item in inventory (prefer high-value or high-scarcity target).

### Execution

1. Apply polygel.
2. Select target item.
3. Keep original and replica.
4. Repeat for exponential value/material scaling.

### Failure conditions

- Target blocked by replication checks (`CanBeReplicatedEvent` / `Unreplicable`).
- Target flagged as unauthorized for this context.

### Hardening ideas

- Strip or normalize `LiquidVolume` on replicas.
- Add explicit denylist for high-impact liquid containers.
- Add diminishing returns or cooldown per blueprint.

## Strategy 2: Clone NPCs to repeat water-ritual rep payouts

### What it breaks

- Reputation progression pacing.
- Access gating for ritual purchases.

### Why it works in code

- Cloning draught applies `Budding`, which produces clones.
- On replica creation, `WaterRitualRecord` is removed from clone.
- Clone can regenerate fresh ritual record on demand (`RequirePart` path).
- `GivesRep` is also removed from replicas, changing downstream behavior but not preventing new ritual records.

Source:
- `/Users/steven/qud-decompiled-project/XRL.Liquids/LiquidCloning.cs:86`
- `/Users/steven/qud-decompiled-project/XRL.World.Effects/Budding.cs:172`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/WaterRitualRecord.cs:146`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/GivesRep.cs:98`
- `/Users/steven/qud-decompiled-project/XRL.World.Conversations.Parts/WaterRitual.cs:36`

### Setup

1. Access cloning draught (drink/smear mechanics).
2. Find a ritual-relevant NPC.

### Execution

1. Induce budding/cloning on NPC.
2. Engage clone in water ritual.
3. Repeat across clone generations.

### Failure conditions

- NPC has `Noclone`, `Unreplicable`, or fails replication checks.
- Faction/record configuration gives low practical yield.

### Hardening ideas

- Preserve ritual exhaustion state across clone lineage.
- Copy `WaterRitualRecord` + depletion fields to replicas.
- Add clone-origin discount/deny in ritual systems.

## Strategy 3: Infinite SP loop (faction-dependent) via ritual purchase

### What it breaks

- Skill point economy.

### Why it works in code

- `WaterRitualSkillPoint` has no per-speaker one-time guard.
- On purchase, SP is directly added to `SP.BaseValue`.
- If reputation can be re-farmed, SP purchase can be repeated.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Conversations.Parts/WaterRitualSkillPoint.cs:20`
- `/Users/steven/qud-decompiled-project/XRL.World.Conversations.Parts/WaterRitualSkillPoint.cs:34`

### Setup

1. Faction/NPC with configured `WaterRitualSkillPointAmount` and cost.
2. Reliable rep-gain loop (e.g., strategy 2).

### Execution

1. Farm rep.
2. Buy SP option.
3. Repeat.

### Failure conditions

- Faction lacks skill-point ritual configuration.
- Reputation costs outpace available gain loop.

### Hardening ideas

- Add record attribute flag for one-time SP purchase per speaker/faction.
- Cap lifetime SP from ritual source.

## Strategy 4: Follower-cap bypass through water-ritual join

### What it breaks

- Companion cap systems for proselytize/beguile/rebuke channels.

### Why it works in code

- Water ritual join uses direct `SetAlliedLeader<AllyProselytize>`.
- It does not route through skill-specific cap-sync helpers.
- Cap logic is enforced in `GetCompanionLimitEvent` and channel sync methods, not this direct path.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Conversations.Parts/WaterRitualJoinParty.cs:34`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs:895`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs:923`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Skill/Persuasion_Proselytize.cs:47`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Skill/Persuasion_Proselytize.cs:99`

### Setup

1. NPC offering ritual join option.
2. Enough reputation to pay join cost.

### Execution

1. Recruit via water ritual repeatedly.
2. Stack followers beyond intended per-channel limits.

### Failure conditions

- Join option disabled for speaker/faction.
- No reputation to pay cost.

### Hardening ideas

- Funnel ritual join through same companion-limit enforcement path.
- Global cap check before `SetAlliedLeader`.

## Strategy 5: Rebuke Robot repeat-XP farm on a single target

### What it breaks

- XP pacing via social combat.

### Why it works in code

- Rebuke path does not reject already-rebuked target before attempt.
- `Rebuked.Apply` removes existing rebuke and reapplies.
- Exceptional Sifrah rebuke grants `Level * 100` XP each critical success.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Skill/Persuasion_RebukeRobot.cs:117`
- `/Users/steven/qud-decompiled-project/XRL.World.Effects/Rebuked.cs:97`
- `/Users/steven/qud-decompiled-project/XRL.World/RebukingSifrah.cs:410`

### Setup

1. Rebuke Robot skill.
2. Viable robot target and safe retry context.

### Execution

1. Run rebuke attempts repeatedly.
2. Aim for critical-success outcomes.
3. Loop on same target.

### Failure conditions

- Influence checks fail.
- Target becomes invalid/unreachable.

### Hardening ideas

- Add one-time XP flag for exceptional-rebuke bonus (per target).
- Block rebuke attempt when same rebuker already owns active rebuke.

## Strategy 6: Proselytize/Beguile critical XP cycling

### What it breaks

- XP economy for social recruitment.

### Why it works in code

- Both systems award `Level * 100` XP on exceptional success.
- These exceptional awards are separate from `*XPAwarded` accumulators.
- Effects can be cycled (dismiss/remove/reapply) to repeat high-value attempts.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World/ProselytizationSifrah.cs:441`
- `/Users/steven/qud-decompiled-project/XRL.World/BeguilingSifrah.cs:439`
- `/Users/steven/qud-decompiled-project/XRL.World.Effects/Proselytized.cs:67`

### Setup

1. Proselytize and/or Beguiling.
2. Target that can be repeatedly influenced.

### Execution

1. Trigger social Sifrah repeatedly.
2. Fish for critical-success outcomes.
3. Reset follower state and repeat.

### Failure conditions

- Influence protections, hostility escalations, or target invalidation.

### Hardening ideas

- Include exceptional bonus in persistent per-target XP cap.
- Add time-based lockout after successful conversion.

## Strategy 7: Temporal Fugue clone burst (amplified by Crystallinity)

### What it breaks

- Action economy and encounter difficulty.

### Why it works in code

- Temporal Fugue creates deep-copy replicas with gear.
- Copy count is multiplied by `MentalCloneMultiplier`.
- Crystallinity sets `MentalCloneMultiplier` to `2`.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/TemporalFugue.cs:163`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/TemporalFugue.cs:280`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/TemporalFugue.cs:323`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/Crystallinity.cs:79`

### Setup

1. Temporal Fugue.
2. Crystallinity for multiplier.
3. Space with viable spawn cells.

### Execution

1. Trigger Fugue at fight start.
2. Leverage extra geared copies to overwhelm local combat.

### Failure conditions

- Replication blocked by `CanBeReplicated`.
- Poor spawn geometry.

### Hardening ideas

- Strip or downgrade copied equipment.
- Clamp mental clone multipliers for temporary replicas.

## Strategy 8: Permanent body theft via Domination metempsychosis

### What it breaks

- Character identity continuity and risk model.

### Why it works in code

- If dominator body is lost while dominated state ends, `Metempsychosis` can strand player mind in target body.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Effects/Dominated.cs:150`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/Domination.cs:209`

### Setup

1. Domination mutation.
2. Controlled path to remove/fail original body while dominated.

### Execution

1. Dominate target.
2. Force dominator-body loss scenario.
3. Resolve into stranded-control state.

### Failure conditions

- Domination interruption before transition.
- Target dies first.

### Hardening ideas

- Require explicit confirmation + stronger failsafe on body-loss branch.
- Restrict transition in certain quest/unique-body contexts.

## Strategy 9: Cooldown collapse with willpower + refresh effects

### What it breaks

- Cooldown pacing and intended rotation friction.

### Why it works in code

- High willpower adds up to 80% cooldown reduction.
- Global floor still permits very short loops.
- `RefreshAllCooldownsOnEat` can instantly refresh many abilities.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World/GetCooldownEvent.cs:97`
- `/Users/steven/qud-decompiled-project/XRL.World/GetCooldownEvent.cs:37`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/ActivatedAbilities.cs:259`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/RefreshAllCooldownsOnEat.cs:59`

### Setup

1. Stack willpower/cooldown reducers.
2. Acquire reliable cooldown-refresh consumables/effects.

### Execution

1. Trigger high-impact actives.
2. Refresh cooldowns.
3. Recast in compressed cycles.

### Failure conditions

- Inability to sustain refresh inputs.
- Encounter constraints (line of sight, phase, etc.).

### Hardening ideas

- Add per-source refresh lockouts.
- Raise minimum cooldown floor for selected high-impact abilities.

## Strategy 10: Water-ritual cost inversion with extreme compute (conditional)

### What it breaks

- Reputation cost model for ritual uses.

### Why it works in code

- Social coprocessor reduction scales with compute via `AdjustUp`.
- No hard clamp in reduction path.
- Reduction is applied multiplicatively to negative reputation costs.
- If effective reduction exceeds 100%, sign inversion becomes possible.

Source:
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/CyberneticsSocialCoprocessor.cs:81`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/CyberneticsSocialCoprocessor.cs:114`
- `/Users/steven/qud-decompiled-project/XRL.World/GetAvailableComputePowerEvent.cs:75`
- `/Users/steven/qud-decompiled-project/XRL.World/Reputation.cs:247`

### Setup

1. Social coprocessor installed and powered.
2. Very high local compute sources.

### Execution

1. Trigger ritual action with reputation cost (`WaterRitualUse` flow).
2. Verify effective delta sign in reputation change event/result.

### Failure conditions

- Compute not high enough to cross inversion threshold.
- Additional modifiers/events offset result.

### Hardening ideas

- Clamp `GetWaterRitualReputationCostReductionPercentage` to `[0, 95]`.
- Clamp post-modified cost sign for `WaterRitualUse`.

## Suggested verification checklist

1. Re-run each strategy on a fresh save with minimal mod interference.
2. Log pre/post values for SP, rep, cooldowns, and companion counts.
3. Confirm whether exploit requires Sifrah toggles enabled.
4. Re-test after any patch touching replication, ritual, or social systems.
