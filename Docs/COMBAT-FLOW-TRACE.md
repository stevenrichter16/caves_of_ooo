# Combat Flow Trace — one melee attack, keypress to corpse

> Follows a single **bump-to-attack** end to end, every step a clickable
> `file:line`. Read it next to Rider's debugger (see the last section).
>
> **The one thing to internalize:** control does **not** flow as a call stack you
> can ⌘B through. It flows as a *relay of string-keyed events*. At each **▶ EVENT**
> below, the code calls `entity.FireEvent(...)`, which (in [Entity.cs:255](Assets/Scripts/Gameplay/Entities/Entity.cs:255))
> loops **every** Part on that entity and calls `HandleEvent`; whichever Parts
> care (`if (e.ID == "…")`) react — and may fire more events. Those six ▶ EVENT
> lines *are* the data flow. Go-to-Declaration stops at `FireEvent`; to see who
> reacts, **⌘⇧F the event string**.

---

## 0 · Trigger — the player bumps a hostile

- [InputHandler.cs:670](Assets/Scripts/Presentation/Input/InputHandler.cs:670) — moving into an occupied tile, `FactionManager.IsHostile(player, blockedBy)`?
- [InputHandler.cs:673](Assets/Scripts/Presentation/Input/InputHandler.cs:673) — yes → `CombatSystem.PerformMeleeAttack(player, blockedBy, zone, rng)`

*(NPCs enter the exact same way: [KillGoal.cs:57](Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs:57), [FleeGoal.cs:49](Assets/Scripts/Gameplay/AI/Goals/FleeGoal.cs:49), [GlowmawAmbushPart.cs:97](Assets/Scripts/Gameplay/AI/GlowmawAmbushPart.cs:97). One code path, player or AI — that's the point of the Entity/Part design.)*

## 1 · `PerformMeleeAttack` — [CombatSystem.cs:69](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:69)

- **▶ EVENT `"BeforeMeleeAttack"`** ([:76](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:76)) — fired on the attacker; any Part may cancel the swing here.
- Routes to `PerformBodyPartAwareAttack` ([:95](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:95)) or `PerformLegacyAttack` ([:127](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:127)), which gather the wielded weapon(s) and call `PerformSingleAttack` once **per weapon** (dual-wield = two passes).

## 2 · `PerformSingleAttack` — [CombatSystem.cs:148](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:148)

The whole resolution for one weapon. (A per-attack `attackId` is stamped at [:159](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:159) so every `Diag` record below shares one `CauseTraceId`.)

1. **Hit roll** ([:180](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:180)): `1d20 + AgilityMod + weapon.HitBonus + skillHitBonus` vs the defender's **DV** (`GetDV` [:570](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:570)). Natural 20 → crit ([:187](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:187)). *(diag `HitRoll`)*
2. **Miss?** ([:215](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:215)) → log + fire miss-side skill events + **return**.
3. **Hit location** ([:245](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:245)): pick a body part on the defender → determines which AV applies.
4. **Penetration** ([:274](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:274), `RollPenetrations` [:674](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:674)): `StrMod + weapon.PenBonus + skillPenBonus` vs the part's **AV** (`GetPartAV` [:1259](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1259)). Crit adds +1 and can force a pen for the player ([:279](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:279)). Zero pens → "fails to penetrate", **return** ([:314](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:314)). *(diag `Penetration`)*
5. **Build `Damage`** ([:323](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:323)): tags it `Melee`, the stat (`Strength`), the weapon's `Attributes` (e.g. `Cutting`), and `Critical` on a nat-20. Then rolls `damageDice` once **per penetration** ([:332](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:332)) → `damage.Amount`. *(diag `DamageRoll`)*
6. Capture `hpBefore` ([:378](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:378)) → **`ApplyDamage(defender, damage, attacker, zone)`** ([:386](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:386)) → **§3**.
7. Back from ApplyDamage: compute `actualDamage = hpBefore − hpAfter` ([:389](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:389)), log the hit ([:399](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:399)), hit-stop FX on crit/kill ([:408](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:408)).
8. **On-hit hooks — only if the target survived** (`hpAfter > 0`, [:426](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:426)), each rolling its own chance:
   - [:432](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:432) `OnHitClassEffects` — Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse
   - [:437](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:437) `OnHitWeaponEffects` — **← your VenomDagger's poison proc (Challenge 1) fires here**
   - [:444](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:444) `OnHitGasEmit` — gas clouds (**Challenge 7**)
   - [:453](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:453) item-enhancement on-hit, then [:463](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:463) skill on-hit events

## 3 · `ApplyDamage` — [CombatSystem.cs:715](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:715)

The funnel **every** damage source flows through (melee, traps, DoT ticks, spell mutations). The in-flight pipeline:

1. Guard ([:737](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:737)): no `Hitpoints` stat, or already dead → **return** (keeps death from double-firing).
2. **▶ EVENT `"BeforeTakeDamage"`** ([:759](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:759)) — listeners mutate or **veto** `damage`. **Stoneskin reduces `Amount` here; your Challenge-4 "Marked" effect would *increase* it here.** Veto → `"DamageFullyResisted"` ([:796](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:796)) + return. *(diag `PreDamageMutation`)*
3. **Resistances** ([:808](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:808), `ApplyResistances` [:987](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:987)) — Heat/Cold/Acid/Electric, by the damage's attribute tags. Fully absorbed → `"DamageFullyResisted"` ([:815](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:815)) + return.
4. **▶ EVENT `"TakeDamage"`** ([:831](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:831)) — fired on the **victim**; last chance to mutate `Amount`. `DamageFlashPart` flashes the victim red off this one.
5. **HP decrement** ([:843](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:843)) — `Hitpoints.BaseValue −= amount`. *(diag `DamageDealt`)*
6. **▶ EVENT `"DamageDealt"`** ([:875](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:875)) — fired on the **ATTACKER**, only when `source != null`. Payload: `Attacker`/`Defender`/`Amount`/`Damage`. **← your LifestealPart heals here (Challenge 2); a crit-reactive trait (Challenge 8) checks `Damage` for `"Critical"` here.**
7. Floating damage number ([:894](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:894)); then if `HP ≤ 0` → **`HandleDeath`** ([:907](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:907)) → **§4**, else mark the cell dirty for redraw ([:911](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:911)).

## 4 · `HandleDeath` — [CombatSystem.cs:1046](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1046)

1. "X is killed by Y!" ([:1050](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1050)); award XP to a player killer ([:1054](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1054)).
2. Drop equipped gear ([:1061](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1061)) and carried inventory ([:1066](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1066)); splatter FX ([:1070](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1070)).
3. **▶ EVENT `"Died"`** ([:1072](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1072)) — fired on the dying entity (params `Target`/`Killer`/`Zone`). **← `GivesRepPart` shifts your reputation here (Challenge 23); `CorpsePart` spawns the corpse; `StatusEffectsPart` tears down active effects.**
4. Broadcast the death to nearby passive NPCs ([:1088](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1088)); remove from the zone ([:1106](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1106)) and the turn queue ([:1121](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1121)).

---

## The six junctions, in order

```
BeforeMeleeAttack ─▶ (cancel the swing?)
   PerformSingleAttack: hit roll → location → penetration → damage roll
      ApplyDamage:
        BeforeTakeDamage ─▶ (mutate/veto: Stoneskin, Marked)
        ApplyResistances  (Heat/Cold/Acid/Electric)
        TakeDamage       ─▶ (victim reacts: DamageFlash)
        HP −= amount
        DamageDealt      ─▶ (attacker reacts: Lifesteal, crit traits)
      on-hit procs: ClassEffects · WeaponEffects(VenomDagger) · Gas · Enhancements · Skills
   Died               ─▶ (GivesRep, Corpse spawn, effect teardown)
```

Every `─▶` is a `FireEvent` → a loop over Parts. **That relay is the architecture.** Your challenges weren't isolated tricks — each one was you plugging a `HandleEvent`/effect into one of these junctions.

## Watch it live (Rider + Unity)

1. Set breakpoints:
   - [Entity.cs:255](Assets/Scripts/Gameplay/Entities/Entity.cs:255) (`FireEvent`) — fires for **every** event; watch `e.ID` to see the whole relay in order. (Set a condition like `e.ID == "DamageDealt"` to skip the noise.)
   - [CombatSystem.cs:386](Assets/Scripts/Gameplay/Combat/CombatSystem.cs:386) (the `ApplyDamage` call) and **Step Into** (F7) to walk §3.
2. Attach Rider to Unity: the **Unity** run-config play button, or **Run → Attach to Unity Process**.
3. In-game, bump a hostile. Step with **F7** (into) / **F8** (over); inspect the `Damage` object and the `GameEvent` params in the Watches pane.
4. To find every reactor to a junction without the debugger: **⌘⇧F** the event string (e.g. `"DamageDealt"`) — fire site + every `e.ID == "DamageDealt"` handler.
