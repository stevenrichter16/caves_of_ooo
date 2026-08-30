using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Why a destruction attempt was refused, or that it worked.</summary>
    public enum DestroyVerdict
    {
        /// <summary>Destroyed. The entity is out of the zone.</summary>
        Destroyed = 0,
        /// <summary>Damaged, but still standing.</summary>
        Damaged,
        NoTarget,
        /// <summary>No <see cref="DestructiblePart"/> — not breakable at
        /// all. The default for everything in the game.</summary>
        NotBreakable,
        /// <summary>Explicitly protected: a staircase, a quest door.</summary>
        Indestructible,
        /// <summary>Alive. Creatures go through the combat death path, not
        /// this one.</summary>
        Living,
        /// <summary>A listener vetoed it.</summary>
        Vetoed,
    }

    /// <summary>
    /// Breaking things that are not alive.
    ///
    /// <para><b>This is deliberately a SEPARATE path from creature death.</b>
    /// <c>CombatSystem.HandleDeath</c> is creature-shaped: it awards kill
    /// XP, drops equipment off body parts, rolls a death-loot table,
    /// emits a blood splatter, fires <c>Died</c> (which spawns a corpse),
    /// and broadcasts the death to nearby NPCs so they react. Routing a
    /// barrel through it would award XP for smashing a fence and leave a
    /// corpse behind. So objects get their own path, and the creature one
    /// is untouched.</para>
    ///
    /// <para><b>Qud parity, with one deliberate divergence.</b> Qud has ONE
    /// hitpoint-depletion path — <c>Physics.CheckHP</c> calls
    /// <c>GameObject.Die()</c> for creature and chest alike, and the
    /// creature/object difference is handled by branches INSIDE it plus
    /// <c>IsCreature</c> checks. Its lower-level <c>Destroy()</c> primitive
    /// is what actually removes the object, and calling it directly fires
    /// none of the death chain. This class is a port of <c>Destroy()</c>,
    /// not of <c>Die()</c> — CoO's death path has no equivalent internal
    /// branch structure to hang object handling off, so forking at the
    /// "reached zero" point is both smaller and safer than widening it.</para>
    ///
    /// <para>Event contract follows Qud: a vetoable <c>BeforeDestroy</c> and
    /// a non-vetoable <c>Destroyed</c> notification. Qud has no
    /// <c>AfterDestroyObjectEvent</c> and neither does this.</para>
    /// </summary>
    public static class DestructionSystem
    {
        /// <summary>
        /// Whether this entity can be broken at all. Everything that is not
        /// explicitly destructible is immune — the safe default.
        /// </summary>
        public static bool IsBreakable(Entity target)
        {
            if (target == null) return false;
            if (target.HasTag("Creature")) return false;
            var part = target.GetPart<DestructiblePart>();
            return part != null && !part.Indestructible;
        }

        /// <summary>
        /// Deal structural damage. Destroys the object if this takes it to
        /// zero.
        /// </summary>
        /// <returns><see cref="DestroyVerdict.Damaged"/> if it survived,
        /// <see cref="DestroyVerdict.Destroyed"/> if it did not, or the
        /// reason nothing happened.</returns>
        public static DestroyVerdict Damage(Entity target, int amount, Entity source, Zone zone)
        {
            if (target == null) return DestroyVerdict.NoTarget;
            if (target.HasTag("Creature")) return DestroyVerdict.Living;

            var part = target.GetPart<DestructiblePart>();
            if (part == null) return DestroyVerdict.NotBreakable;
            if (part.Indestructible)
            {
                MessageLog.Add($"{target.GetDisplayName()} will not break.");
                Record("Refused", target, source, new { reason = "indestructible" });
                return DestroyVerdict.Indestructible;
            }

            // Hardness never fully absorbs: a hit that connects always does
            // something, or a stone wall becomes literally unbreakable by
            // arithmetic rather than by design.
            int applied = amount - part.Hardness;
            if (applied < 1) applied = 1;

            part.HP -= applied;
            Record("Damaged", target, source,
                new { amount = applied, hpAfter = part.HP, maxHp = part.MaxHP });

            if (part.HP > 0)
            {
                // Only narrate a deliberate blow. This method is also the
                // per-turn damage path for a burning hedgerow, where
                // "You strike the hedgerow." every turn is both untrue and
                // noise — BurningEffect logs its own line for that.
                if (source != null && source.HasTag("Player"))
                    MessageLog.Add($"You strike {target.GetDisplayName()}.");
                return DestroyVerdict.Damaged;
            }

            return Destroy(target, source, zone, "damage");
        }

        /// <summary>
        /// Destroy outright, skipping the HP pool. The port of Qud's
        /// <c>Destroy()</c> primitive.
        /// </summary>
        public static DestroyVerdict Destroy(Entity target, Entity source, Zone zone, string cause)
        {
            if (target == null) return DestroyVerdict.NoTarget;
            if (target.HasTag("Creature")) return DestroyVerdict.Living;

            var part = target.GetPart<DestructiblePart>();
            if (part != null && part.Indestructible) return DestroyVerdict.Indestructible;

            // Idempotency, first — the port of Qud's IsInGraveyard() check,
            // which opens its Destroy() (XRL.World/GameObject.cs:3306-3311).
            // A second destroy (a listener re-entering on the Destroyed
            // notification, or two damage sources in one turn both taking it
            // below zero) would otherwise fire the event twice and spawn a
            // SECOND pile of wreckage. Reports Destroyed, as Qud returns
            // true: the caller's intent is satisfied.
            if (part != null && part.Gone) return DestroyVerdict.Destroyed;

            // Vetoable, per Qud's BeforeDestroyObjectEvent. A Part can
            // refuse — a sealed vault that must survive its own explosion,
            // a quest object mid-script.
            var before = GameEvent.New("BeforeDestroy");
            before.SetParameter("Target", (object)target);
            before.SetParameter("Source", (object)source);
            before.SetParameter("Cause", cause);
            if (!target.FireEventAndRelease(before))
            {
                Record("Refused", target, source, new { reason = "vetoed", cause });
                return DestroyVerdict.Vetoed;
            }

            // Past the veto, so it is going to happen. Flag it BEFORE firing
            // Destroyed, so a listener that re-enters finds the guard set
            // rather than recursing.
            if (part != null) part.Gone = true;

            // Position must be read BEFORE removal — it is where the
            // contents land and which cell needs repainting.
            var (x, y) = zone != null ? zone.GetEntityPosition(target) : (-1, -1);

            int spilled = SpillContents(target, zone, x, y);

            // Not vetoable — this is notification that it HAS happened, so
            // listeners can react without being able to rewrite history.
            var destroyed = GameEvent.New("Destroyed");
            destroyed.SetParameter("Target", (object)target);
            destroyed.SetParameter("Source", (object)source);
            destroyed.SetParameter("Cause", cause);
            destroyed.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(destroyed);

            string name = target.GetDisplayName();
            zone?.RemoveEntity(target);

            // Wreckage AFTER removal, so the replacement is not competing
            // with the thing it replaces for the same cell.
            string wreckage = part != null ? part.WreckageBlueprint : null;
            if (!string.IsNullOrEmpty(wreckage) && zone != null && x >= 0)
                BuilderSpawn.TryPlace(zone, EntityFactoryRef, wreckage, x, y);

            if (x >= 0 && y >= 0)
                ZoneRenderHooks.MarkCellDirty(x, y, "Destruction");

            MessageLog.Add($"{name} is destroyed.");
            Record("Destroyed", target, source,
                new { blueprintName = target.BlueprintName, cause, spilled, wreckage });

            return DestroyVerdict.Destroyed;
        }

        /// <summary>
        /// Send damage to whichever hitpoint pool this target actually has:
        /// <c>CombatSystem.ApplyDamage</c> for the living,
        /// <see cref="Damage(Entity,int,Entity,Zone)"/> for scenery.
        ///
        /// <para>The reason this exists as one helper rather than an
        /// if-statement at each call site: <c>CombatSystem.ApplyDamage</c>
        /// silently early-returns on a target with no <c>Hitpoints</c> stat
        /// (deliberately — statues and props are not creatures). Every
        /// caller that reached objects therefore did nothing at all, and
        /// did it invisibly. A burning hedgerow took fire damage every turn
        /// and never burned down.</para>
        /// </summary>
        /// <returns>How much actually landed, in whichever pool.</returns>
        public static int RouteDamage(Entity target, Damage damage, Entity source, Zone zone)
        {
            if (target == null || damage == null || damage.Amount <= 0) return 0;

            if (!target.HasTag("Creature"))
            {
                var part = target.GetPart<DestructiblePart>();
                if (part != null)
                {
                    // W3.3 — the structural path announces TakeDamage with
                    // the same contract as CombatSystem.ApplyDamage: fired
                    // BEFORE the decrement, listeners may mutate
                    // damage.Amount, clamped so over-mutation can't heal.
                    // First listener that needed it: BurnOffGasPart on
                    // burning peat — a prop with attribute-typed damage
                    // (BurningEffect routes here with "Fire") previously
                    // burned in silence and nothing could react.
                    var takeDamage = GameEvent.New("TakeDamage");
                    takeDamage.SetParameter("Target", (object)target);
                    takeDamage.SetParameter("Source", (object)source);
                    takeDamage.SetParameter("Amount", damage.Amount);
                    takeDamage.SetParameter("Damage", (object)damage);
                    target.FireEventAndRelease(takeDamage);
                    int amount = System.Math.Max(0, damage.Amount);
                    if (amount <= 0) return 0;

                    int before = part.HP;
                    Damage(target, amount, source, zone);
                    return System.Math.Max(0, before - part.HP);
                }
            }

            int hpBefore = target.GetStatValue("Hitpoints", 0);
            CombatSystem.ApplyDamage(target, damage, source, zone);
            return System.Math.Max(0, hpBefore - target.GetStatValue("Hitpoints", 0));
        }

        /// <summary>
        /// One deliberate swing at a non-living thing — the seam BOTH
        /// player melee paths (bump-to-break, the Break world-action)
        /// route through.
        ///
        /// <para>W5.7 close-out 🔴, deferred to its own RED-first
        /// commit: these call sites used to call <see cref="Damage"/>
        /// directly, and Damage never fires TakeDamage — only
        /// <see cref="RouteDamage"/> does. Every Part listening on the
        /// prop-damage seam was blind to swords: a player could chop
        /// down all ~51 hearth-patch tiles for ZERO reputation while a
        /// fire spell billed the war. The blow now travels as a
        /// Bludgeoning-attributed Damage through RouteDamage, so
        /// listeners see it; fire-gated listeners (BurnOffGasPart's
        /// Heat;Fire accumulator) ignore it by their own attribute
        /// gates — pinned in StructuralStrikeTests.</para>
        /// </summary>
        public static void StrikeStructure(Entity target, Entity striker,
            Zone zone, System.Random rng)
        {
            var blow = new Damage(ComputeStructuralBlow(striker, rng));
            blow.AddAttribute("Bludgeoning");
            RouteDamage(target, blow, striker, zone);
        }

        /// <summary>What a bare fist swings for when nothing better is to hand.</summary>
        private const string UnarmedBlow = "1d2";

        /// <summary>How far you can reach to hit something. Melee: adjacent.</summary>
        public const int StrikeReach = 1;

        /// <summary>
        /// Can <paramref name="actor"/> actually reach
        /// <paramref name="target"/> to hit it?
        ///
        /// <para>The bump path is adjacent by construction, but the interact
        /// menu's Break row is also reachable from look mode, whose cursor
        /// can sit anywhere on the map. Without this the player could
        /// demolish a wall across the zone by pointing at it.</para>
        /// </summary>
        public static bool IsWithinStrikeReach(Entity actor, Entity target, Zone zone)
        {
            if (actor == null || target == null || zone == null) return false;

            var (ax, ay) = zone.GetEntityPosition(actor);
            var (tx, ty) = zone.GetEntityPosition(target);
            if (ax < 0 || tx < 0) return false;

            return System.Math.Abs(ax - tx) <= StrikeReach
                && System.Math.Abs(ay - ty) <= StrikeReach;
        }

        /// <summary>
        /// How hard <paramref name="attacker"/> hits a thing.
        ///
        /// <para>Deliberately much simpler than
        /// <c>CombatSystem.PerformSingleAttack</c>: no to-hit roll, no
        /// penetration ladder, no hit location. A barrel has no dodge value
        /// and no armour to punch through — it just has structural HP, so
        /// the swing is weapon dice plus the wielder's stat modifier and
        /// <see cref="DestructiblePart.Hardness"/> stands in for armour.</para>
        ///
        /// <para>Never returns less than 1, so chipping away at a stone wall
        /// with your hands is slow rather than impossible.</para>
        /// </summary>
        public static int ComputeStructuralBlow(Entity attacker, System.Random rng)
        {
            MeleeWeaponPart weapon = null;
            var inventory = attacker?.GetPart<InventoryPart>();
            var equipped = inventory?.GetEquippedWithPart<MeleeWeaponPart>();
            if (equipped != null)
                weapon = equipped.GetPart<MeleeWeaponPart>();
            // Fall back to a natural weapon (claws, a mutation) on the
            // attacker itself, mirroring CombatSystem's weapon lookup.
            if (weapon == null)
                weapon = attacker?.GetPart<MeleeWeaponPart>();

            string dice = weapon != null && !string.IsNullOrEmpty(weapon.BaseDamage)
                ? weapon.BaseDamage : UnarmedBlow;
            int blow = DiceRoller.Roll(dice, rng ?? new System.Random());

            if (attacker != null)
                blow += StatUtils.GetModifier(attacker, weapon?.Stat ?? "Strength");

            return blow < 1 ? 1 : blow;
        }

        /// <summary>
        /// Injected by GameBootstrap so wreckage can be spawned — the
        /// <c>CorpsePart.Factory</c> convention. Null = no wreckage, which
        /// degrades to "the object is simply gone" rather than throwing.
        /// </summary>
        public static Data.EntityFactory EntityFactoryRef;

        /// <summary>
        /// Tip a container's contents onto the floor where it stood.
        ///
        /// <para>Qud does this inside the death chain at
        /// <c>BeforeDeathRemovalEvent</c>, which is creature-side; the
        /// object path needs its own. Without it, breaking a chest silently
        /// destroys everything in it — the player's reward for the
        /// interaction.</para>
        /// </summary>
        private static int SpillContents(Entity target, Zone zone, int x, int y)
        {
            var container = target.GetPart<ContainerPart>();
            if (container == null || zone == null || x < 0 || y < 0) return 0;

            int count = 0;
            // Reverse iteration: Contents is mutated as items leave it.
            for (int i = container.Contents.Count - 1; i >= 0; i--)
            {
                var item = container.Contents[i];
                if (item == null) continue;
                container.Contents.RemoveAt(i);
                zone.AddEntity(item, x, y);
                var physics = item.GetPart<PhysicsPart>();
                if (physics != null) physics.InInventory = null;
                count++;
            }

            if (count > 0)
                MessageLog.Add(count == 1
                    ? "Its contents spill onto the ground."
                    : $"Its {count} contents spill onto the ground.");
            return count;
        }

        private static void Record(string kind, Entity target, Entity source, object payload)
        {
            Diag.Record(category: "damage", kind: "Object" + kind,
                actor: source, target: target, payload: payload);
        }
    }
}
