# Control Flow — mobile field guide

> **Reading this on an iPhone:** every diagram is **vertical** (top-to-bottom), so
> it fits a phone column — open on GitHub (web or the mobile app) and tap a
> diagram to zoom. Under each diagram is a numbered list saying the same thing in
> words, so you never *need* the picture. Wide tables are avoided on purpose.
>
> Companion: [DATA-FLOW-MOBILE.md](DATA-FLOW-MOBILE.md) (how the *data* moves) ·
> [COMBAT-FLOW-TRACE.md](COMBAT-FLOW-TRACE.md) (the attack flow in full detail).

---

## The one idea: everything is an event relay

Systems don't call each other directly. A system fires a **string-keyed event**
on an entity; `Entity.FireEvent` ([Entity.cs:255](Assets/Scripts/Gameplay/Entities/Entity.cs:255))
loops **every Part** and calls `HandleEvent`; the Parts that care
(`if (e.ID == "...")`) react — and may fire more events.

```mermaid
flowchart TD
  S["some system"] --> FE["entity.FireEvent(evt)"]
  FE --> L["loop every Part"]
  L --> H1["Part A.HandleEvent"]
  L --> H2["Part B.HandleEvent"]
  L --> H3["Part C.HandleEvent"]
  H2 --> R["e.ID matches?<br/>react"]
```

**Why ⌘B (go-to-definition) fails you:** there is no static link across
`FireEvent`. To find who reacts to an event, **⌘⇧F the event string** (e.g.
`"DamageDealt"`). That search result *is* the control flow.

---

## Contents

1. [Turn loop (the heartbeat)](#1-turn-loop)
2. [Melee attack](#2-melee-attack)
3. [Status-effect lifecycle](#3-status-effect-lifecycle)
4. [On-hit proc pipeline](#4-on-hit-proc)
5. [Equip / unequip](#5-equip--unequip)
6. [Mutation active ability](#6-mutation-active-ability)
7. [AI brain / goals](#7-ai-brain--goals)
8. [Quest objective](#8-quest-objective)
9. [Challenge → flow index](#challenge--flow-index)

---

## 1. Turn loop

*The clock everything hangs off. Energy-based: faster actors act more often.*

```mermaid
flowchart TD
  I["player key press"] --> P["TurnManager.<br/>ProcessUntilPlayerTurn"]
  P --> T["Tick: every entity<br/>Energy += Speed"]
  T --> F{"someone at<br/>Energy >= 1000?"}
  F -->|no| T
  F -->|yes| C["set CurrentActor"]
  C --> E1["EVT: BeginTakeAction"]
  E1 --> OS["effects OnTurnStart<br/>(Burn/Regen damage tick)"]
  OS --> G{"AllowAction?"}
  G -->|"no, stunned"| Z["EndTurn"]
  G -->|yes| A{"player or NPC?"}
  A -->|player| W["wait for input"]
  A -->|NPC| TT["EVT: TakeTurn -> brain"]
  W --> Z
  TT --> Z
  Z --> E2["EVT: EndTurn"]
  E2 --> OE["effects OnTurnEnd<br/>(Duration -1, drop expired)"]
  OE --> SP["Energy -= 1000"]
  SP --> P
```

1. Input → `TurnManager.ProcessUntilPlayerTurn` ([TurnManager.cs:212](Assets/Scripts/Gameplay/Turns/TurnManager.cs:212)).
2. `Tick` grants `Energy += Speed` to every entity until one reaches 1000.
3. That entity becomes `CurrentActor`.
4. **EVT `BeginTakeAction`** → `StatusEffectsPart.HandleBeginTakeAction` ([StatusEffectsPart.cs:398](Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs:398)) runs every effect's `OnTurnStart` — **this is where Burning/Regen deal their per-turn HP change**, and where `AllowAction` can block a stunned actor.
5. Player → waits for input. NPC → **EVT `TakeTurn`** drives the brain (§7).
6. `EndTurn` → **EVT `EndTurn`** → `HandleEndTurn` runs `OnTurnEnd` (Duration−1; drop effects at 0).
7. Spend 1000 energy; loop.

**Challenges here:** #10 Regeneration & #13 Corrosion (tick in step 4), #12 Haste/Slow (changes **Speed** → step 2 → turn order), #19 mutation cooldowns (tick in step 6).

---

## 2. Melee attack

*Full cited version in [COMBAT-FLOW-TRACE.md](COMBAT-FLOW-TRACE.md). Compact map:*

```mermaid
flowchart TD
  B["bump a hostile<br/>InputHandler:673"] --> PMA["PerformMeleeAttack"]
  PMA --> E0["EVT: BeforeMeleeAttack<br/>(cancel?)"]
  E0 --> PSA["PerformSingleAttack"]
  PSA --> HIT{"hit roll vs DV"}
  HIT -->|miss| MX["miss, return"]
  HIT -->|hit| PEN{"penetrate AV?"}
  PEN -->|no| PX["no damage, return"]
  PEN -->|yes| DMG["roll Damage<br/>(+attributes)"]
  DMG --> AD["ApplyDamage (sec.2b)"]
  AD --> SURV{"survived?"}
  SURV -->|yes| PROC["on-hit procs:<br/>class / weapon / gas"]
  SURV -->|no| DEAD["HandleDeath"]
```

**2b — inside `ApplyDamage`** ([CombatSystem.cs:715](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:715)):

```mermaid
flowchart TD
  A["ApplyDamage"] --> E1["EVT: BeforeTakeDamage<br/>(mutate / veto)"]
  E1 --> RES["ApplyResistances<br/>(Heat/Cold/Acid/Electric)"]
  RES --> E2["EVT: TakeDamage<br/>(victim reacts)"]
  E2 --> HP["Hitpoints -= amount"]
  HP --> E3["EVT: DamageDealt<br/>(ATTACKER reacts)"]
  E3 --> K{"HP <= 0?"}
  K -->|yes| D["HandleDeath -> EVT: Died"]
  K -->|no| M["mark cell dirty"]
```

**Challenges here:** #2 Lifesteal & #8 Crit trait (EVT `DamageDealt`), #4 Marked & #5 Wet+shock (EVT `BeforeTakeDamage`/resistances), #9 Thorns (EVT `TakeDamage`), #1/#6/#7 procs (on-hit stage), #23 reputation (EVT `Died`).

---

## 3. Status-effect lifecycle

*An `Effect` is a small object living on an entity's `StatusEffectsPart`.*

```mermaid
flowchart TD
  AP["entity.ApplyEffect(fx)"] --> EX{"same type<br/>already on?"}
  EX -->|yes| ST["existing.OnStack(fx)<br/>(refresh or accumulate)"]
  EX -->|no| OA["fx.OnApply<br/>(buff/log/attach)"]
  OA --> LIVE["lives on the entity"]
  LIVE --> TS["each turn: OnTurnStart<br/>(damage/heal)"]
  LIVE --> BTD["on hit: OnBeforeTakeDamage<br/>(reduce/amplify)"]
  LIVE --> TE["each turn: OnTurnEnd<br/>(Duration -1)"]
  TE --> EXP{"Duration == 0?"}
  EXP -->|yes| OR["fx.OnRemove<br/>(undo buff)"]
  EXP -->|no| LIVE
```

1. `ApplyEffect` ([StatusEffectsPart.cs:38](Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs:38)): if a same-type effect exists, call its `OnStack` (and usually *don't* add a duplicate); else `OnApply`.
2. While alive it gets `OnTurnStart`/`OnTurnEnd` from the turn loop (§1) and `OnBeforeTakeDamage` from combat (§2b).
3. `OnTurnEnd` decrements `Duration`; at 0 → `OnRemove` (undo whatever `OnApply` did — **symmetry**).

Base contract: [Effect.cs](Assets/Scripts/Gameplay/Effects/Effect.cs). Model to copy: [BerserkEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BerserkEffect.cs).

**Challenges here:** #3 Weakened, #4 Marked, #5 Wet+shock, #10 Regeneration, #11 Glow, #12 Haste/Slow, #13 Corrosion (`OnStack`), #17 cursed-item effect.

---

## 4. On-hit proc

*How a weapon's data string becomes a live effect on the victim.*

```mermaid
flowchart TD
  H["melee hit lands<br/>(sec.2, target survived)"] --> OW["OnHitWeaponEffects.Apply"]
  OW --> SP["read weapon.OnHitEffectsRaw<br/>e.g. Poisoned,50,1d4,6,0"]
  SP --> PA["OnHitEffectSpec.Parse"]
  PA --> CH{"chance roll<br/>per spec"}
  CH -->|pass| FA["OnHitEffectFactory.Create<br/>(name -> Effect)"]
  CH -->|fail| SK["skip"]
  FA --> AE["defender.ApplyEffect (sec.3)"]
```

1. After a surviving hit, `OnHitWeaponEffects.Apply` ([CombatSystem.cs:437](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:437)) reads the weapon's `OnHitEffectsRaw`.
2. `OnHitEffectSpec.Parse` splits the string; each spec rolls its own chance.
3. `OnHitEffectFactory.Create` ([OnHitEffectFactory.cs](Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs)) maps the name → an `Effect`, applied to the defender (→ §3).

**Challenges here:** #1 VenomDagger (data only), #6 new proc (add a factory `case`), #7 gas-on-hit (parallel `OnHitGasEmit`).

---

## 5. Equip / unequip

```mermaid
flowchart TD
  EQ["InventorySystem.Equip"] --> EC["EquipCommand.Execute"]
  EC --> BE["EVT: BeforeEquip<br/>(veto?)"]
  BE --> PL["EquipPlanner: find<br/>free body-part slot"]
  PL --> SET["BodyPart.SetEquipped(item)"]
  SET --> BON["ApplyEquipBonuses<br/>Stat.Bonus += n"]
  BON --> AE["EVT: AfterEquip"]
  AE --> ENH["enhancement OnEquip"]
```

- Equip: `EquipCommand.Execute` ([EquipCommand.cs:69](Assets/Scripts/Gameplay/Inventory/Commands/EquipCommand.cs:69)) → `BeforeEquip` → slot resolve → `SetEquipped` → `ApplyEquipBonuses` ([EquipBonusUtility.cs](Assets/Scripts/Gameplay/Items/EquipBonusUtility.cs), format `"Strength:2"`) → `AfterEquip`.
- Unequip mirrors it: `BeforeUnequip` → **remove** bonuses → clear slot → `AfterUnequip`.

**Challenges here:** #14 stat ring (`EquipBonuses`), #17 cursed item (listen `AfterEquip`/`AfterUnequip`).

---

## 6. Mutation active ability

```mermaid
flowchart TD
  M["Mutate()"] --> ADD["AddMyActivatedAbility<br/>-> ActivatedAbilitiesPart"]
  ADD --> SLOT["bound to a hotbar slot"]
  SLOT --> KEY["player presses the key"]
  KEY --> CD{"off cooldown?"}
  CD -->|no| NO["blocked"]
  CD -->|yes| DIR["pick direction/target"]
  DIR --> CMD["fire EVT: CommandName"]
  CMD --> HE["mutation.HandleEvent<br/>matches CommandName"]
  HE --> CAST["Cast(): effect + damage"]
  CAST --> SETCD["CooldownMyActivatedAbility"]
```

1. `Mutate` registers the ability via `AddMyActivatedAbility` ([BaseMutation.cs:210](Assets/Scripts/Gameplay/Mutations/BaseMutation.cs:210)) → `ActivatedAbilitiesPart`, bound to a hotbar slot.
2. Key press → cooldown check → direction → fires an event named after the ability's `CommandName`.
3. The mutation's `HandleEvent` matches that name → `Cast()` → applies effect/damage → sets cooldown (which ticks down in §1 step 6).

Models: [KindleMutation.cs](Assets/Scripts/Gameplay/Mutations/KindleMutation.cs) (projectile), [CalmMutation.cs](Assets/Scripts/Gameplay/Mutations/CalmMutation.cs) (targeted).

**Challenges here:** #18 passive mutation (just `Mutate`/`Unmutate` + a stat), #19 active ability (the whole chain).

---

## 7. AI brain / goals

*NPCs run on a stack of goals. Same `CombatSystem` entry as the player.*

```mermaid
flowchart TD
  TT["EVT: TakeTurn"] --> HB["BrainPart.HandleTakeTurn"]
  HB --> POP["pop finished goals"]
  POP --> ENS["ensure >=1 goal<br/>(BoredGoal default)"]
  ENS --> TOP["topGoal.TakeAction()"]
  TOP --> BORED{"BoredGoal:<br/>hostile near?"}
  BORED -->|yes| PUSH["push KillGoal"]
  BORED -->|no| WAN["wander / idle"]
  PUSH --> KILL["KillGoal.TakeAction"]
  KILL --> ADJ{"adjacent?"}
  ADJ -->|yes| ATK["PerformMeleeAttack (sec.2)"]
  ADJ -->|no| APP["approach (pathfind)"]
```

1. **EVT `TakeTurn`** → `BrainPart.HandleTakeTurn` ([BrainPart.cs:592](Assets/Scripts/Gameplay/AI/BrainPart.cs:592)).
2. Pop finished goals; ensure a default (`BoredGoal`); run the top goal's `TakeAction` ([GoalHandler.cs:26](Assets/Scripts/Gameplay/AI/Goals/GoalHandler.cs:26)).
3. `BoredGoal` scans for hostiles → pushes `KillGoal`; `KillGoal` attacks if adjacent (→ §2) else approaches. A pushed child runs the same tick.

**Challenges here:** #22 new AI goal (add a `GoalHandler` subclass, push it from a brain).

---

## 8. Quest objective

```mermaid
flowchart TD
  TE["EVT: TickEnd"] --> POLL["StoryletPart polls<br/>active quest stage"]
  POLL --> EV{"objective Triggers<br/>satisfied?"}
  EV -->|no| WAIT["keep waiting"]
  EV -->|yes| FIN["FinishObjective:<br/>mark + run OnEnter reward"]
  FIN --> ALL{"all required<br/>objectives done?"}
  ALL -->|no| WAIT
  ALL -->|yes| ADV["AdvanceQuestStage<br/>(run stage OnEnter)"]
  ADV --> LAST{"last stage?"}
  LAST -->|yes| DONE["CompleteQuest"]
  LAST -->|no| POLL
```

1. On `TickEnd`, `StoryletPart` ([StoryletPart.cs:509](Assets/Scripts/Gameplay/Storylets/StoryletPart.cs:509)) checks each active objective's `Triggers`.
2. Satisfied → `FinishObjective` ([StoryletPart.cs:338](Assets/Scripts/Gameplay/Storylets/StoryletPart.cs:338)): mark done + run `OnEnter` rewards.
3. All required objectives done → `AdvanceQuestStage` → next stage's `OnEnter`, or `CompleteQuest`.
4. World shortcut: items with `CompleteObjectiveOnTaken` / `QuestStarter` fire off the **`Taken`** event on pickup.

**Challenges here:** #20 collect-N quest (author the data), #21 quest-start item (`QuestStarter` on `Taken`).

---

## Challenge → flow index

- **#1 VenomDagger** → §4 on-hit + content (data-flow doc)
- **#2 Lifesteal** → §2 `DamageDealt`
- **#3 Weakened** → §3 effects + §4
- **#4 Marked** → §3 + §2 `BeforeTakeDamage`
- **#5 Wet+shock** → §3 + §2 resistances
- **#6 New proc** → §3 + §4
- **#7 Gas weapon** → §4 + content
- **#8 Crit trait** → §2 `DamageDealt`
- **#9 Thorns** → §2 `TakeDamage`
- **#10 Regeneration** → §3 + §1 (tick)
- **#11 Glow** → §3 (render only)
- **#12 Haste/Slow** → §3 + §1 (Speed → order)
- **#13 Corrosion** → §3 (`OnStack`) + §1
- **#14 Stat ring** → §5 (`EquipBonuses`)
- **#15 Flammable** → content + thermal (data-flow doc)
- **#16 Readable book** → the event relay (inventory-action events)
- **#17 Cursed item** → §5 + §3
- **#18 Passive mutation** → §6 (`Mutate`)
- **#19 Active ability** → §6 (full) + §1 (cooldown)
- **#20 Collect-N quest** → §8
- **#21 Quest item** → §8 (`Taken`)
- **#22 AI goal** → §7
- **#23 Rep creature** → §2 `Died`

---

## Explore it live (Rider + Unity)

- Breakpoint [Entity.cs:255](Assets/Scripts/Gameplay/Entities/Entity.cs:255) (`FireEvent`), condition `e.ID == "DamageDealt"` → watch one junction.
- Breakpoint [CombatSystem.cs:386](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:386), **Step Into (F7)** to walk §2b.
- Attach: **Run → Attach to Unity Process**, then act in-game.
- No debugger? **⌘⇧F** an event string to list its fire site + every handler.
