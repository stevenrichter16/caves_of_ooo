# Caves of Ooo — Programming Challenges, Vol. 2 (#4–23)

> Twenty more, continuing from [PROGRAMMING-CHALLENGES.md](PROGRAMMING-CHALLENGES.md)
> (#1–3). Same spirit: each builds a real, player-visible mechanic and teaches
> how one more slice of the engine connects to the rest. Combat-heavy up top
> (as requested), then effects, items, mutations, quests/AI.
>
> **These are briefs, not solutions** — they name the exact files to read as
> models and define "done" as a test. The detailed walkthrough format + the
> NUnit test idiom + the RED-first workflow all live in Vol. 1; this volume is
> terser on purpose so it reads as a menu.
>
> **Difficulty:** ⭐ trivial · ⭐⭐ small · ⭐⭐⭐ moderate · ⭐⭐⭐⭐ ambitious.
> Most are ⭐–⭐⭐⭐ — pick whatever sounds fun, in any order.
>
> **Two habits worth carrying into every one** (house style — see `CLAUDE.md`):
> 1. **Write the failing test first**, watch it go RED, then make it GREEN.
> 2. **Stretch goal on all of them:** emit a `Diag.Record(...)` on the gate/
>    action so it's queryable (`diag_query`). See [Diag.cs](Assets/Scripts/Shared/Utilities/Diag.cs)
>    and the Observability section of `CLAUDE.md`.

---

## ⚔️ Combat & Damage

The reliable hooks: `"DamageDealt"` fires on the **attacker** (rich payload:
`Attacker`/`Defender`/`Amount`/`Damage`); `Effect.OnBeforeTakeDamage` lets an
effect mutate incoming `Damage` on the **defender**; the on-hit factory turns a
weapon's spec string into an effect.

### 4. Marked for Death (vulnerability effect) ⭐⭐
> A debuff that makes its victim take *more* damage from every hit.
- **Build:** a status effect whose `OnBeforeTakeDamage` *increases* `damage.Amount` (e.g. +50%).
- **Connects:** effect lifecycle · the damage pipeline · `Damage`.
- **Read:** [StoneskinEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/StoneskinEffect.cs) — the exact mirror (it *reduces* damage the same way), [Effect.cs](Assets/Scripts/Gameplay/Effects/Effect.cs) (`OnBeforeTakeDamage` doc).
- **Done:** apply Marked, `CombatSystem.ApplyDamage(target, 10)`, assert HP dropped by ~15. Counter-check: no effect → drops 10.

### 5. Wet → Shock synergy ⭐⭐
> Make `WetEffect` amplify electric damage — the classic "wet things conduct."
- **Build:** add `OnBeforeTakeDamage` to `WetEffect`; if `damage.IsElectricDamage()`, bump `Amount`.
- **Connects:** two effects interacting · `Damage` attribute flags · resistance/vulnerability.
- **Read:** [WetEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/WetEffect.cs), [Damage.cs](Assets/Scripts/Gameplay/Combat/Damage.cs) (`IsElectricDamage`), [StoneskinEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/StoneskinEffect.cs).
- **Done:** wet target + electric `Damage` → extra damage; wet target + melee `Damage` → unchanged (counter-check).

### 6. A new on-hit status effect, weaponized ⭐⭐⭐
> Generalize Vol.1 #3: invent another proc — e.g. **Slowed** (Agility penalty).
- **Build:** new `Effect` (stat penalty, like Weakened) → add a `case` in the factory → a weapon blueprint with `OnHitEffectsRaw`.
- **Connects:** effect lifecycle · `OnHitEffectFactory` · combat proc · content.
- **Read:** [OnHitEffectFactory.cs](Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs), [OnHitEffectSpec.cs](Assets/Scripts/Gameplay/Items/OnHitEffectSpec.cs), your own `WeakenedEffect.cs`.
- **Done:** `OnHitEffectFactory.Create(Parse("Slowed,50,,3,2")[0],…)` returns a `SlowedEffect`; apply → Agility drops.

### 7. A gas-on-hit weapon ⭐⭐
> A blade that puffs a gas cloud on every hit (no weapon ships with this yet — you'd be first).
- **Build:** set `EmitGasOnHitRaw` on a weapon blueprint in `Objects.json`; discover the spec format from the parser.
- **Connects:** content · combat · the gas/material sim.
- **Read:** [OnHitGasEmit.cs](Assets/Scripts/Gameplay/Combat/OnHitGasEmit.cs) and `EmitGasOnHitSpec` (format), [MeleeWeaponPart.cs](Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs) (`EmitGasOnHitRaw` field).
- **Done:** create the weapon, assert `MeleeWeaponPart.EmitGasOnHitRaw` parsed into ≥1 spec; (live) a hit spawns a gas cell.

### 8. Critical-strike trait ⭐⭐⭐
> A creature/weapon that does something special when it lands a **crit**.
- **Build:** a `Part` on the attacker that handles `"DamageDealt"`, inspects `Damage.Attributes` for `"Critical"`, and reacts (bonus effect, message, lifesteal-on-crit…).
- **Connects:** combat crit path (nat-20 tags `"Critical"`) · `Damage` attributes · events.
- **Read:** [DamageFlashPart.cs](Assets/Scripts/Gameplay/Entities/DamageFlashPart.cs) (the `DamageDealt` idiom), [CombatSystem.cs](Assets/Scripts/Gameplay/Combat/CombatSystem.cs) (search `"Critical"`).
- **Done:** fire `DamageDealt` carrying a `Damage` with `"Critical"` → reaction fires; without it → nothing (counter-check).

### 9. Thorns / Retaliation ⭐⭐⭐
> Armor (or a trait) that hurts whoever hits its wearer.
- **Build:** a `Part`/effect that, when its owner takes damage, deals damage back to the attacker.
- **Connects:** equipment ↔ wearer events · combat · `CombatSystem.ApplyDamage`.
- **Read:** [Effect.cs](Assets/Scripts/Gameplay/Effects/Effect.cs) (`OnTakeDamage(target, e)`), [DamageFlashPart.cs](Assets/Scripts/Gameplay/Entities/DamageFlashPart.cs), [CombatSystem.cs](Assets/Scripts/Gameplay/Combat/CombatSystem.cs).
- **First puzzle to solve:** *how do you get the attacker reference?* Check what params the `"TakeDamage"` event carries vs. what's on `DamageDealt` — that investigation is the lesson.
- **Done:** attacker hits wearer for 10 → attacker takes the thorns amount back.

---

## 🌀 Status Effects

All mirror an existing concrete effect — copy its shape, change the verb.

### 10. Regeneration (heal-over-time) ⭐⭐
> The inverse of poison: heal a little each turn for N turns.
- **Build:** `OnTurnStart` raises Hitpoints (clamped to Max); `Duration` expires it automatically.
- **Connects:** effect lifecycle · stats · the turn loop.
- **Read:** [BleedingEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BleedingEffect.cs) (DoT to invert), [Stat.cs](Assets/Scripts/Gameplay/Stats/Stat.cs) (the Max clamp).
- **Done:** apply Regen to a hurt entity, simulate a turn-start tick, assert HP rose; HP never exceeds Max (counter-check).

### 11. Glow (render-color effect) ⭐
> Tint an entity while an effect is active — the gentlest possible effect.
- **Build:** an effect that overrides only `GetRenderColorOverride()` (return a Qud color code like `"&G"`).
- **Connects:** effects · the renderer.
- **Read:** [BerserkEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BerserkEffect.cs) (`GetRenderColorOverride` → `"&R"`).
- **Done:** apply → `GetRenderColorOverride()` returns your code; pin it with one assert.

### 12. Haste / Slow ⭐⭐
> A buff (or debuff) on Agility/Speed — feel the turn order change.
- **Build:** `OnApply` adds `Bonus` to the stat, `OnRemove` removes it; `OnStack` refreshes duration.
- **Connects:** effects · the stat-modifier machinery · (Speed feeds turn order).
- **Read:** [BerserkEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BerserkEffect.cs) (near-identical), [Stat.cs](Assets/Scripts/Gameplay/Stats/Stat.cs).
- **Done:** apply Haste(+N) → `GetStat("Agility").Value` up; remove → restored.

### 13. Stacking corrosion ⭐⭐⭐
> Each application eats more armor, up to a cap — teaches *accumulating* `OnStack`.
- **Build:** an effect that lowers AV; `OnStack` adds to the magnitude (capped) instead of refusing the duplicate.
- **Connects:** `OnStack` accumulation (vs. the refresh pattern) · the armor stat.
- **Read:** [BurningEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs) (`OnStack` accumulates intensity), [ShatterArmorEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/ShatterArmorEffect.cs) (AV reduction).
- **Done:** apply twice → 2× reduction (not 1×, not 3×); a third past the cap → stays capped.

---

## 🎒 Items & Equipment

### 14. A stat-boosting ring (pure JSON) ⭐
> Equip it, get +Strength; take it off, lose it. No C#.
- **Build:** a blueprint with an `Equippable` part whose `EquipBonuses` grants a stat (flat-string convention, e.g. `Strength:2`).
- **Connects:** content · equipment · stats (the bonus/penalty machinery).
- **Read:** [EquippablePart.cs](Assets/Scripts/Gameplay/Items/EquippablePart.cs) (`EquipBonuses`), `ExtraArmPrototypeMutation.cs` (sets `EquipBonuses`), `EquipBonusUtility`/`ParseEquipBonuses` (search Assets/Scripts).
- **Done:** equip → Strength up; unequip → restored. (Pick a body slot the anatomy actually has.)

### 15. A flammable item ⭐⭐
> A wooden item that catches fire when heated — touch the emergent thermal sim.
- **Build:** a blueprint with a `Material` (high `Combustibility`) + `Thermal` part; applied heat should ignite it into `BurningEffect`.
- **Connects:** content · the material/thermal sim · effects.
- **Read:** `MaterialPart` / `ThermalPart` (search Assets/Scripts/Gameplay/Materials), [BurningEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs), and the `LeatherArmor` blueprint in `Objects.json` (Combustibility 0.4) as a template.
- **Done:** fire an `"ApplyHeat"` event with enough Joules at the item → it gains `BurningEffect`/`SmolderingEffect`.

### 16. A readable book ⭐⭐
> A new inventory action — select "Read", see lore in the log.
- **Build:** a `Part` that adds a `"Read"` action on `"GetInventoryActions"` and logs text on `"InventoryAction"`.
- **Connects:** Part + event idiom · the inventory-action menu.
- **Read:** [ExaminablePart.cs](Assets/Scripts/Gameplay/Entities/ExaminablePart.cs) — almost a direct template (it adds "Examine"; you add "Read").
- **Done:** `GetInventoryActions` → your action present; `InventoryAction` with `Command=="Read"` → `MessageLog` got the lore line.

### 17. A cursed item ⭐⭐⭐
> Equip it and a debuff sticks to you until you take it off.
- **Build:** a `Part` that applies a status effect on `"AfterEquip"` and removes it on `"AfterUnequip"`.
- **Connects:** equip/unequip events · the effect system · symmetry (apply ⇄ remove).
- **Read:** [Effect.cs](Assets/Scripts/Gameplay/Effects/Effect.cs)/[StatusEffectsPart.cs](Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs), grep `"AfterEquip"`/`"AfterUnequip"` for who fires them.
- **Done:** equip → `HasEffect<…>()` true; unequip → false.

---

## 🧬 Mutations & Skills

### 18. A passive stat mutation ⭐⭐⭐
> A mutation that just makes you tougher while you have it.
- **Build:** a `BaseMutation` whose `Mutate` adds a stat bonus and `Unmutate` removes it; register it in `Mutations.json`.
- **Connects:** mutations · stats · mutation content.
- **Read:** [BaseMutation.cs](Assets/Scripts/Gameplay/Mutations/BaseMutation.cs), `Mutations.json` (Assets/Resources/Content/Blueprints), a simple existing mutation.
- **Done:** `MutationsPart.AddMutation(new …(), 1)` → stat up; remove → restored.

### 19. A new active ability mutation ⭐⭐⭐⭐
> A self-heal or bolt with a cooldown — the full activated-ability flow.
- **Build:** a `BaseMutation` that registers an activated ability (`AddMyActivatedAbility`), with a `CommandName`, cooldown, and a `Cast`.
- **Connects:** mutations · `ActivatedAbilitiesPart` · cooldowns · targeting.
- **Read:** [KindleMutation.cs](Assets/Scripts/Gameplay/Mutations/KindleMutation.cs) (projectile), `CalmMutation.cs` (targeted effect) — pick the closer model.
- **Done:** mutate, `Cast(...)` → effect applied + ability goes on cooldown (`IsMyActivatedAbilityUsable` false right after).

---

## 🗺️ Quests, AI & Reputation

### 20. A "collect N" quest ⭐⭐⭐
> Author a quest in data: gather 3 of something, get a reward.
- **Build:** a `QuestData` with a stage + a `QuestObjectiveData` whose triggers complete on collection; reward via `OnEnter`.
- **Connects:** the storylet/quest data model · objectives · rewards.
- **Read:** [StoryletData.cs](Assets/Scripts/Gameplay/Storylets/StoryletData.cs) (Quest/Stage/Objective shapes), [CompleteObjectiveOnTaken.cs](Assets/Scripts/Gameplay/Storylets/CompleteObjectiveOnTaken.cs).
- **Done:** finishing the objective advances the stage (`StoryletPart.FinishObjective` → stage index moves / reward fires).

### 21. A quest-starting item ⭐⭐
> Pick up a scroll, a quest begins.
- **Build:** attach `QuestStarter` to an item blueprint so `"Taken"` starts a quest (optionally gated on a prior quest).
- **Connects:** items · the `"Taken"` event · quest state.
- **Read:** [QuestStarter.cs](Assets/Scripts/Gameplay/Storylets/QuestStarter.cs), [QuestState.cs](Assets/Scripts/Gameplay/Storylets/QuestState.cs).
- **Done:** take the item → quest is active; take it again → doesn't re-trigger (the `Activated` guard).

### 22. A new AI goal ⭐⭐⭐
> Give a creature a new behavior — e.g. "flee from fire" or "approach the player."
- **Build:** a new `Goal` class (or compose `DelegateGoal`) pushed onto a `BrainPart`'s goal stack.
- **Connects:** AI · `BrainPart` · the goal handler/stack.
- **Read:** [Goals/](Assets/Scripts/Gameplay/AI/Goals/DelegateGoal.cs) (`DelegateGoal`, `MoveToGoal`, `RetreatGoal`, `GoalHandler`), [BrainPart.cs](Assets/Scripts/Gameplay/AI/BrainPart.cs).
- **Done:** push your goal, tick the brain, assert the creature did the thing (moved toward/away).

### 23. A reputation-granting creature ⭐⭐
> Help (or kill) it and watch your standing with a faction move.
- **Build:** put `GivesRepPart` on a creature blueprint; on death/help it adjusts `PlayerReputation`.
- **Connects:** AI/faction · combat (`Died`) · the reputation ledger.
- **Read:** [GivesRepPart.cs](Assets/Scripts/Gameplay/AI/GivesRepPart.cs), [PlayerReputation.cs](Assets/Scripts/Gameplay/AI/PlayerReputation.cs), [CombatSystem.cs](Assets/Scripts/Gameplay/Combat/CombatSystem.cs) (`"Died"`).
- **Done:** kill the creature → reputation with its faction changed by the expected amount.

---

## Suggested order

- **Warm-ups (⭐/⭐⭐):** 14 (ring) → 11 (glow) → 10 (regen) → 16 (book) → 4 (marked).
- **Combat core (⭐⭐⭐):** 5 (wet+shock) → 6 (new proc) → 8 (crit) → 9 (thorns).
- **Connect-the-systems (⭐⭐⭐):** 13 (stacking) → 17 (cursed) → 15 (flammable) → 23 (rep) → 20/21 (quests).
- **Ambitious (⭐⭐⭐⭐):** 18 → 19 (mutations), 22 (AI goal).

*Not committed — your worklist. Same as Vol. 1, I can scaffold RED tests for any
of these on request.*
