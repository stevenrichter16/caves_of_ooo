using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// LOOT OVERHAUL SM3 — the death loot roll.
    ///
    /// <para>Loadouts (SM1/SM2) answer "drop what they were carrying."
    /// This answers the second half of the ask: "random other loot
    /// calculated from a loot algorithm." It rolls ONE table per kill,
    /// chosen by (loot class × tier), and scatters the result on the
    /// death cell alongside the creature's own gear.</para>
    ///
    /// <para><b>What it drops, and why.</b> Deliberately NOT weapons —
    /// those come from loadouts, where the player can see them before
    /// the kill, which is more legible. Death tables carry the
    /// <i>supply line</i>: alchemical reagents, weapon components,
    /// coins, the occasional tonic. Both crafting systems (weaponcraft's
    /// Blade/Haft/Binding slots and the tag-driven brew rules) were
    /// fully built and completely starved — 5 reagents and 1 component
    /// appeared in ZERO loot tables before this.</para>
    ///
    /// <para><b>Loot class is derived, not authored</b>, so 73 creature
    /// blueprints didn't need editing: a creature with a Loadout is a
    /// Humanoid, a metal/stone one is a Construct, everything else is a
    /// Beast. An explicit <c>LootClass</c> tag overrides the
    /// derivation when an author wants something specific.</para>
    /// </summary>
    public static class LootDropSystem
    {
        /// <summary>Injected by GameBootstrap; null = graceful no-op
        /// (CorpsePart.Factory convention).</summary>
        public static EntityFactory Factory;

        /// <summary>Deterministic override for tests.</summary>
        public static System.Random Rng;

        public const string ClassBeast = "Beast";
        public const string ClassHumanoid = "Humanoid";
        public const string ClassConstruct = "Construct";

        /// <summary>
        /// Roll and scatter death loot for <paramref name="victim"/>.
        /// Safe to call for any entity: no table, no factory, no zone,
        /// or a suppressed victim all no-op.
        /// </summary>
        public static int RollDeathLoot(Entity victim, Entity killer, Zone zone)
        {
            if (victim == null || zone == null || Factory == null) return 0;

            // Same suppression contract as the equipment spill (SM2) —
            // a summoned creature is not a loot piñata.
            if (victim.HasTag("NoDropOnDeath") || victim.HasTag("Temporary"))
                return 0;

            var cell = zone.GetEntityCell(victim);
            if (cell == null) return 0;

            string table = ResolveTableName(victim);
            if (string.IsNullOrEmpty(table)) return 0;
            if (LootTableRegistry.Get(table) == null) return 0;

            var rng = Rng ?? new System.Random();
            var rolled = LootTableRegistry.Roll(table, rng);
            if (rolled == null || rolled.Count == 0) return 0;

            int spawned = 0;
            for (int i = 0; i < rolled.Count; i++)
            {
                Entity item;
                try { item = Factory.CreateEntity(rolled[i]); }
                catch (System.Exception) { continue; }
                if (item == null) continue;
                zone.AddEntity(item, cell.X, cell.Y);
                spawned++;
            }

            if (spawned > 0 && Diag.IsChannelEnabled("loot"))
            {
                Diag.Record(
                    category: "loot", kind: "DeathDrop",
                    actor: killer, target: victim,
                    payload: new
                    {
                        victim = victim.BlueprintName,
                        table,
                        lootClass = ResolveClass(victim),
                        tier = ResolveTier(victim),
                        items = spawned,
                    });
            }
            return spawned;
        }

        /// <summary>"DeathHumanoidT2" etc. Public: pinned by tests.</summary>
        public static string ResolveTableName(Entity victim)
        {
            if (victim == null) return null;

            // Explicit override wins — an author can point a specific
            // creature at any table.
            string explicitTable = victim.GetTag("LootTable", null);
            if (!string.IsNullOrEmpty(explicitTable)) return explicitTable;

            int tier = ResolveTier(victim);
            string cls = ResolveClass(victim);

            // Constructs only exist at T2+; fold T1 constructs into T2
            // rather than authoring an empty table.
            if (cls == ClassConstruct && tier < 2) tier = 2;

            return "Death" + cls + "T" + tier;
        }

        /// <summary>Derived, not authored — see the class docstring.</summary>
        public static string ResolveClass(Entity victim)
        {
            if (victim == null) return ClassBeast;

            string explicitClass = victim.GetTag("LootClass", null);
            if (!string.IsNullOrEmpty(explicitClass)) return explicitClass;

            // A creature that carries gear is a creature that trades,
            // camps, and pockets things — humanoid by behaviour.
            if (victim.GetPart<LoadoutPart>() != null) return ClassHumanoid;

            var material = victim.GetPart<MaterialPart>();
            if (material != null)
            {
                string id = material.MaterialID;
                if (id == "Metal" || id == "Stone" || id == "Glass" || id == "Crystal")
                    return ClassConstruct;
            }
            return ClassBeast;
        }

        /// <summary>Tier tag, clamped to the authored table range [1,3].</summary>
        public static int ResolveTier(Entity victim)
        {
            if (victim == null) return 1;
            string raw = victim.GetTag("Tier", "1");
            if (!int.TryParse(raw, out int tier)) tier = 1;
            if (tier < 1) tier = 1;
            if (tier > 3) tier = 3;
            return tier;
        }
    }
}
