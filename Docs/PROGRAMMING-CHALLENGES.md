# Caves of Ooo — Starter Programming Challenges

> Three self-contained, **fun** challenges designed to teach you the codebase by
> building real, player-visible mechanics. They form a deliberate difficulty
> ramp and a *connection* ramp: each one touches one more layer of the
> Entity → Part → Event → Combat → Effect → Stat stack, and Challenge 3 loops
> all the way back to Challenge 1.
>
> These are **briefs, not solutions.** They point you at the exact files to read
> as models, name the exact APIs you'll use, and define "done" as a test you
> write — but the implementation is yours. That's the fun part.
>
> **House style** (see `CLAUDE.md`): write the failing test FIRST, run it, watch
> it go RED, then write the minimum code to make it GREEN. Every "X happens"
> test gets a counter-check: "Y, identical but with the flag flipped, does NOT
> happen." You don't have to be dogmatic about it for these — but it's genuinely
> the fastest way to learn this codebase, because the test *is* the spec.

---

## The map you're learning

```
Blueprint JSON ──(EntityFactory)──> Entity ──holds──> Parts
   (Objects.json)                      │                 │
                                       │                 ├─ react to ─> GameEvent  ("DamageDealt", "Render", ...)
                                       │                 │
                                  Statistics        CombatSystem ──fires──> events, applies Damage
                                  (Hitpoints,             │
                                   Strength, ...)         └─ on hit ─> OnHitEffectFactory ──> Effect ──> StatusEffectsPart
                                                                         (Burning, Frozen, ...)   (ticks each turn)
```

| # | Challenge | Difficulty | New C#? | Systems you'll connect |
|---|-----------|------------|---------|------------------------|
| 1 | Forge an elemental weapon | ⭐ | **None** (pure JSON) | Blueprints · inheritance · EntityFactory · on-hit specs |
| 2 | Lifesteal trait | ⭐⭐ | One new `Part` | Part · GameEvent · combat · Stat · reflection-injected fields |
| 3 | Brew a new status effect | ⭐⭐⭐ | One new `Effect` + 1 factory line | Effect lifecycle · StatusEffectsPart · factory · combat · Stat — **and back to #1** |

---

## Challenge 1 — Forge an Elemental Weapon ⭐
### *"Add a venom dagger to the game without writing a single line of C#."*

**The fun:** you'll create a brand-new weapon that poisons enemies on hit, and
it works entirely through data. This is the gentlest possible tour of how
content becomes a live game object.

**What you'll learn / connect**
- How a blueprint inherits from a parent (`MeleeWeapon` → `Item` → `PhysicalObject`).
- How `Parts` + `Params` in JSON get reflection-injected into a Part's public
  fields by `EntityFactory` — *no wiring code anywhere*.
- The on-hit effect "spec string" mini-language that links a weapon to the
  combat/effect systems.

**Where to look (read these first)**
- [Objects.json](Assets/Resources/Content/Blueprints/Objects.json) — find the
  `Dagger` blueprint (~line 76) and the existing elemental weapons
  (search `OnHitEffectsRaw` — there's a Burning sword ~line 2161, a Frozen one
  ~line 2204, an Acidic one ~line 2324). Copy the *shape* of these.
- [MeleeWeaponPart.cs](Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs) — every
  `Key` you put under the `MeleeWeapon` part must match a public field here.
- [OnHitEffectFactory.cs](Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs) —
  the list of effect names you're allowed to use, and what each spec field means.

**The spec-string format** (semicolon-separated for multiple procs):
```
EffectName,ChancePercent,DamageDice,DurationTurns,Magnitude
```
e.g. `"Poisoned,50,1d4,6,0"` = 50% chance to apply Poisoned, 1d4 dmg/turn, 6 turns.

**Steps**
1. Add a new object to `Objects.json` named `VenomDagger` that `"Inherits": "MeleeWeapon"`.
2. Give it a `Render` part (a distinctive glyph + color), and a `MeleeWeapon`
   part with a `BaseDamage` and an `OnHitEffectsRaw` of `"Poisoned,50,1d4,6,0"`.
3. (No build step beyond Unity recompiling/reimporting the JSON.)

**Definition of done — write this test** (model: [AISelfPreservationBlueprintTests.cs](Assets/Tests/EditMode/Gameplay/AI/AISelfPreservationBlueprintTests.cs))
```csharp
// Loads the real Objects.json, creates your weapon, asserts the data landed.
var factory = new EntityFactory();
factory.LoadBlueprints(File.ReadAllText(
    Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));

var dagger = factory.CreateEntity("VenomDagger");
var weapon = dagger.GetPart<MeleeWeaponPart>();
Assert.IsNotNull(weapon, "VenomDagger should have a MeleeWeapon part.");
Assert.AreEqual("Poisoned,50,1d4,6,0", weapon.OnHitEffectsRaw);
```
- **Counter-check:** `factory.CreateEntity("Dagger")` (the plain one) has a
  *null or empty* `OnHitEffectsRaw` — proving your field isn't accidentally
  inherited from somewhere.

**Hints / gotchas**
- JSON typos fail silently-ish (the part just won't get the value). If your test
  fails, first re-read the `Key` names against `MeleeWeaponPart`'s fields.
- A malformed spec is skipped silently by the parser — so "no poison happens"
  usually means a bad spec string, not a code bug.

**Stretch goals**
- Stack two procs: `"Poisoned,50,1d4,6,0;Stunned,15,,1,0"`.
- Make a *themed set*: a frost spear, an acid whip — each one line of content.

---

## Challenge 2 — Lifesteal Trait ⭐⭐
### *"A creature that heals itself every time it deals damage."*

**The fun:** you'll build the single most important pattern in the whole
engine — a `Part` that listens for a `GameEvent` — and use it to make a little
vampire. This is the "aha, *that's* how everything talks to everything" moment.

**What you'll learn / connect**
- The core idiom: subclass `Part`, override `HandleEvent(GameEvent e)`, branch on
  `e.ID`, read params with `e.GetIntParameter(...)`.
- **Event scoping** — a crucial lesson: `"DamageDealt"` fires on the **attacker**,
  not the weapon and not the victim (see CombatSystem.cs:875 —
  `source.FireEventAndRelease(...)`). So your Part lives on the *creature*.
- The `Stat` API for healing, including the Max clamp.
- Reflection-injected Part fields again (so designers can tune it from JSON).

**Where to look**
- [DamageFlashPart.cs](Assets/Scripts/Gameplay/Entities/DamageFlashPart.cs) —
  your structural template. Note how it handles `"DamageDealt"` and reads
  `e.GetIntParameter("Amount", 1)`. Yours will look almost identical.
- [Part.cs](Assets/Scripts/Gameplay/Entities/Part.cs) — the base class contract
  (`HandleEvent`, return `true` to keep propagating).
- [Stat.cs](Assets/Scripts/Gameplay/Stats/Stat.cs) — `Value = BaseValue + Bonus
  − Penalty + Boost`, clamped to `[Min, Max]`. To heal, raise `BaseValue` but
  **don't exceed `Max`**.
- [BerserkEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BerserkEffect.cs) —
  shows the `target.GetStat("Strength")` access pattern you'll mirror for
  `"Hitpoints"`.

**Steps**
1. Create `Assets/Scripts/Gameplay/Items/LifestealPart.cs` (a new `Part`).
2. Give it a public `int HealPercent = 50;` field (so it's tunable from JSON).
3. In `HandleEvent`, when `e.ID == "DamageDealt"`, compute
   `heal = amount * HealPercent / 100` and add it to the owner's `Hitpoints`
   stat — clamped to `Max`. Return `true`.
4. Attach it to a creature in `Objects.json`:
   `{ "Name": "Lifesteal", "Params": [{ "Key": "HealPercent", "Value": "50" }] }`
   (try it on a new `VampireBat`, or temporarily on the player to feel it).

**Definition of done — write this test** (model: [CalmMutationTests.cs](Assets/Tests/EditMode/Gameplay/Mutations/CalmMutationTests.cs) for the entity-building helper style)
```csharp
// Arrange: an entity with the part and a Hitpoints stat sitting below Max.
var attacker = new Entity { BlueprintName = "biter" };
attacker.Statistics["Hitpoints"] =
    new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 100 };
attacker.AddPart(new LifestealPart { HealPercent = 50 });

// Act: simulate dealing 10 damage by firing the event ON THE ATTACKER.
var e = GameEvent.New("DamageDealt");
e.SetParameter("Amount", 10);
attacker.FireEvent(e);

// Assert: healed for 50% of 10.
Assert.AreEqual(15, attacker.GetStat("Hitpoints").BaseValue);
```
- **Counter-check #1:** an identical entity *without* `LifestealPart` stays at 10.
- **Counter-check #2 (the clamp):** start `BaseValue = 98`, deal 10 → heal 5 →
  HP is **100, not 103**. This is the bug most people ship; pin it with a test.

**Hints / gotchas**
- Healing 0 when `amount` is small? Integer division: `1 * 50 / 100 == 0`. Decide
  if that's fine or if you want `Math.Max(1, ...)`. Either is defensible — just
  be deliberate and test the boundary.
- `FireEvent` vs `FireEventAndRelease`: in a test, `FireEvent` is simplest. Read
  the doc comments in [GameEvent.cs](Assets/Scripts/Gameplay/Events/GameEvent.cs)
  to understand pooling if you're curious.

**Stretch goals**
- Emit a `Diag.Record("combat", "Lifesteal", ...)` so the heal shows up in
  `diag_query` (see the Observability section of `CLAUDE.md`).
- Add a `MessageLog.Add(...)` line so the player *sees* "The bat drinks your life!".

---

## Challenge 3 — Brew a New Status Effect ⭐⭐⭐
### *"A 'Weakened' debuff that saps Strength — then put it on a weapon."*

**The capstone.** You'll author a real status effect with a full lifecycle, then
register it so the on-hit system can apply it, then (tying back to Challenge 1)
forge a weapon that procs it. When this works, you've personally connected every
layer of the stack.

**What you'll learn / connect**
- The `Effect` lifecycle: `OnApply` (apply a Strength penalty), `OnRemove`
  (undo it), automatic `Duration` countdown, and `OnStack` (re-applying refreshes
  instead of double-stacking).
- The **symmetry discipline** the project lives by (`CLAUDE.md` "Q1 — Symmetry
  check"): whatever `OnApply` does, `OnRemove` must exactly reverse. A debuff is
  the perfect place to internalize this.
- How `OnHitEffectFactory` is the bridge from a content spec string to a live
  effect object.

**Where to look**
- [BerserkEffect.cs](Assets/Scripts/Gameplay/Effects/Concrete/BerserkEffect.cs) —
  your mirror image. Berserk *adds* `str.Bonus`; you'll *add* `str.Penalty`. Copy
  its `OnApply`/`OnRemove`/`OnStack` shape almost verbatim, flip the sign.
- [Effect.cs](Assets/Scripts/Gameplay/Effects/Effect.cs) — the base class. Note
  `DisplayName` is abstract (you must implement it), and `OnTurnEnd` already
  decrements `Duration` for you — so a fixed-duration debuff needs *no* turn
  logic at all.
- [OnHitEffectFactory.cs](Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs) —
  you'll add one `case`. See how `StoneskinEffect` maps `Magnitude`→reduction and
  `DurationTurns`→duration; mirror that.
- [StatusEffectsPart.cs](Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs) —
  how effects get applied/ticked. You apply via `entity.ApplyEffect(new WeakenedEffect(...))`.

**Steps**
1. Create `Assets/Scripts/Gameplay/Effects/Concrete/WeakenedEffect.cs`.
   - `public override string DisplayName => "weakened";`
   - Constructor `WeakenedEffect(int strPenalty = 2, int duration = 3)`.
   - `OnApply`: `target.GetStat("Strength").Penalty += strPenalty;` + a MessageLog line.
   - `OnRemove`: subtract the same amount back. (Symmetry!)
   - `OnStack`: refresh to the longer duration; **don't** re-apply the penalty.
2. Register it in `OnHitEffectFactory.Create`:
   ```csharp
   case "weakened":
   case "weaken":
       return new WeakenedEffect(
           strPenalty: spec.Magnitude > 0f ? (int)spec.Magnitude : 2,
           duration:   spec.DurationTurns > 0 ? spec.DurationTurns : 3);
   ```
3. **Loop back to Challenge 1:** add a `SappingMace` weapon to `Objects.json`
   with `"OnHitEffectsRaw": "Weakened,50,,4,3"` (50% chance, 4 turns, −3 Str).

**Definition of done — write these tests**
```csharp
// 1. Apply lowers Strength.
var target = MakeEntityWithStat("Strength", 16);   // helper: Min 1, Max 30
target.AddPart(new StatusEffectsPart());
target.ApplyEffect(new WeakenedEffect(strPenalty: 3, duration: 4));
Assert.AreEqual(13, target.GetStat("Strength").Value);

// 2. Remove restores it (symmetry counter-check).
target.GetPart<StatusEffectsPart>().RemoveEffect<WeakenedEffect>();   // check exact API
Assert.AreEqual(16, target.GetStat("Strength").Value);

// 3. Factory wiring: the name maps to your effect.
var spec = OnHitEffectSpec.Parse("Weakened,50,,4,3")[0];
var fx = OnHitEffectFactory.Create(spec, source: null, rng: new System.Random());
Assert.IsInstanceOf<WeakenedEffect>(fx);
```
- **Counter-check / OnStack:** apply Weakened twice; assert Strength dropped by
  the penalty *once* (not twice) and the duration refreshed. This is exactly the
  bug `OnStack` exists to prevent — prove yours does.

**Hints / gotchas**
- Use `Penalty`, not `BaseValue`. Penalty is reversible and stacks cleanly with
  other modifiers; mutating `BaseValue` corrupts the stat permanently.
- Check `GetStat("Strength")` for null before touching it — not every entity has
  every stat (mirrors how BerserkEffect guards with `if (str != null)`).
- Confirm the exact `RemoveEffect` overload name/signature in
  `StatusEffectsPart.cs` before writing test #2 — APIs drift, verify don't assume.
- Watch the `JustApplied` flag (documented in `Effect.cs`): an effect applied
  mid-turn survives that turn's `EndTurn`. It won't bite this challenge, but it's
  worth reading the comment to understand duration timing.

**Stretch goals**
- Add a `GetRenderColorOverride()` so weakened enemies tint a sickly color
  (Berserk returns `"&R"`; pick your own).
- Give it a per-turn flavor (e.g. it also slightly lowers `Toughness`) and extend
  the tests to cover the second stat.
- Run the project's adversarial-sweep mindset (`ADVERSARIAL_TESTING.md`): what
  happens with `strPenalty = 0`? Negative? Stat at `Min`? Write a test per answer.

---

## After you finish

You'll have touched: JSON content, the factory, the Entity/Part composition
model, the GameEvent system, the combat damage pipeline, the Stat machinery, and
the Effect lifecycle — i.e. a vertical slice through the whole game. Good next
explorations, in rough order of size:

- **Mutations** ([KindleMutation.cs](Assets/Scripts/Gameplay/Mutations/KindleMutation.cs)) —
  active, player-triggered abilities with cooldowns.
- **Quests/Storylets** ([StoryletData.cs](Assets/Scripts/Gameplay/Storylets/StoryletData.cs)) —
  objectives, triggers, rewards.
- **Gas & materials** (the thermal/combustion sim) — the most emergent system in
  the game.

*Not committed to git — this is your personal worklist. Delete or move it
whenever.*
