using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// LOOT OVERHAUL SM1 — blueprint-authorable starting gear.
    ///
    /// <para>Creates starting inventory and uses the normal equipment
    /// lifecycle for compatible free slots. Refused equipment stays carried.
    /// Normal death-drop policies then determine whether gear drops. This is
    /// an opt-in authoring API; current content reach is tracked separately
    /// in Docs/LOADOUT-LIFECYCLE-PLAN.md.</para>
    ///
    /// <para><b>Authoring.</b></para>
    /// <code>
    /// { "Name": "Loadout", "Params": [
    ///     { "Key": "Equip", "Value": "ShortSword;LeatherArmor" },
    ///     { "Key": "Carry", "Value": "HealingTonic:35;GoldCoin:80x3-9" },
    ///     { "Key": "Pick",  "Value": "1;Dagger;Hatchet;Cudgel" } ]}
    /// </code>
    /// <list type="bullet">
    /// <item><c>Equip</c> — semicolon list; created and equipped.</item>
    /// <item><c>Carry</c> — <c>blueprint:chance%[xMin-Max]</c>; into inventory.</item>
    /// <item><c>Pick</c> — <c>N;a;b;c</c>; N random picks, then equipped
    /// (or carried if not equippable). Weapon variety without one
    /// blueprint per variant.</item>
    /// </list>
    ///
    /// <para><b>Beasts get none of this.</b> Natural weapons live on
    /// body parts as default behaviours and correctly never drop — a
    /// spider must not drop "fangs". Loadouts are additive and opt-in
    /// (sweep correction 3).</para>
    ///
    /// <para>Applies itself on <c>ObjectCreated</c>, which EntityFactory
    /// fires immediately after <c>InitializeAnatomy</c> — so body parts
    /// exist and equipping resolves. No factory changes needed.</para>
    /// </summary>
    public class LoadoutPart : Part
    {
        public override string Name => "Loadout";

        /// <summary>Semicolon-separated blueprints to create + equip.</summary>
        public string Equip = "";

        /// <summary>Semicolon-separated <c>blueprint:chance[xMin-Max]</c>.</summary>
        public string Carry = "";

        /// <summary>"N;a;b;c" — pick N of the listed blueprints.</summary>
        public string Pick = "";

        // ====================================================================
        // Static wiring. Mirrors the CorpsePart.Factory / HarvestablePart.Factory
        // convention: GameBootstrap sets these once; null = graceful no-op so
        // headless tests and tools never explode.
        // ====================================================================

        public static EntityFactory Factory;
        public static System.Random Rng;

        /// <summary>
        /// Re-entrancy guard. <see cref="Apply"/> creates entities, and
        /// entity creation fires ObjectCreated — so a loadout that
        /// (transitively) grants an entity carrying its own Loadout part
        /// would recurse until the stack died. Authoring drift, not a
        /// player action, but it costs one int to make impossible.
        /// </summary>
        private static int _depth;
        private const int MaxDepth = 3;

        /// <summary>One parsed <c>Carry</c>/<c>Equip</c> entry.</summary>
        public struct GrantSpec
        {
            public string Blueprint;
            public int Chance;
            public int MinCount;
            public int MaxCount;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated")
                Apply();
            return true;
        }

        private void Apply()
        {
            if (ParentEntity == null || Factory == null) return;
            var inventory = ParentEntity.GetPart<InventoryPart>();
            if (inventory == null) return;
            if (_depth >= MaxDepth) return;
            var rng = Rng ?? new System.Random();

            _depth++;
            try
            {
                // 1. Equip list — bare entries are guaranteed; explicit
                //    chance/count syntax shares the Carry parser.
                var equipSpecs = ParseCarry(Equip);
                for (int i = 0; i < equipSpecs.Count; i++)
                    GrantOne(inventory, equipSpecs[i], rng, tryEquip: true);

                // 2. Pick list — N random draws WITHOUT replacement.
                ApplyPick(inventory, rng);

                // 3. Carry list — chance-gated, into inventory.
                var carrySpecs = ParseCarry(Carry);
                for (int i = 0; i < carrySpecs.Count; i++)
                    GrantOne(inventory, carrySpecs[i], rng, tryEquip: false);
            }
            finally
            {
                _depth--;
            }
        }

        private void ApplyPick(InventoryPart inventory, System.Random rng)
        {
            if (string.IsNullOrWhiteSpace(Pick)) return;
            var fields = Pick.Split(';');
            if (fields.Length < 2) return;
            if (!int.TryParse(fields[0].Trim(), out int picks)) return;
            if (picks <= 0) return;

            var pool = new List<string>(fields.Length - 1);
            for (int i = 1; i < fields.Length; i++)
            {
                var bp = fields[i].Trim();
                if (bp.Length > 0) pool.Add(bp);
            }
            if (pool.Count == 0) return;
            if (picks > pool.Count) picks = pool.Count;

            for (int n = 0; n < picks; n++)
            {
                int idx = rng.Next(pool.Count);
                var spec = new GrantSpec
                {
                    Blueprint = pool[idx],
                    Chance = 100,
                    MinCount = 1,
                    MaxCount = 1,
                };
                pool.RemoveAt(idx);   // without replacement
                GrantOne(inventory, spec, rng, tryEquip: true);
            }
        }

        private void GrantOne(InventoryPart inventory, GrantSpec spec,
            System.Random rng, bool tryEquip)
        {
            if (string.IsNullOrEmpty(spec.Blueprint)) return;
            if (spec.Chance <= 0) return;
            if (spec.Chance < 100 && rng.Next(100) >= spec.Chance) return;

            int count = spec.MinCount;
            if (spec.MaxCount > spec.MinCount)
                count = rng.Next(spec.MinCount, spec.MaxCount + 1);
            if (count <= 0) return;

            for (int c = 0; c < count; c++)
            {
                Entity item;
                try
                {
                    item = Factory.CreateEntity(spec.Blueprint);
                }
                catch (System.Exception)
                {
                    // Unknown blueprint in a loadout is authoring drift,
                    // not a crash: skip the entry and keep the creature.
                    return;
                }
                if (item == null) return;

                if (!inventory.AddObject(item)) return;   // overweight: stop

                if (tryEquip)
                {
                    // Keep the historical no-Body carried fallback; AutoEquip
                    // otherwise supports legacy slots. The command owns hooks,
                    // bonuses and no-displacement checks. Refusal retains the grant.
                    bool hasBody = ParentEntity.GetPart<Body>() != null;
                    bool completed = hasBody && InventorySystem.AutoEquip(ParentEntity, item);
                    bool equipped = completed && InventorySystem.IsEquipped(ParentEntity, item);
                    string reason = !hasBody ? "missing_body" : !completed ? "auto_equip_refused"
                        : equipped ? "equipped" : "removed_during_equip";
                    Diag.Record("event", "LoadoutEquipResult", actor: ParentEntity, target: item,
                        payload: new { blueprintName = item.BlueprintName, equipped, reason });
                }
            }
        }

        // ====================================================================
        // Parsing. Public + static so the adversarial suite can hammer it
        // directly (malformed input is the #1 bug class for string-driven
        // blueprint vocabularies — see ADVERSARIAL_TESTING.md).
        // ====================================================================

        /// <summary>
        /// Parse "<c>bp:chance[xMin-Max];bp2:chance</c>". Malformed entries
        /// are skipped, never fatal. A bare "<c>bp</c>" means 100% ×1.
        /// </summary>
        public static List<GrantSpec> ParseCarry(string raw)
        {
            var result = new List<GrantSpec>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var entries = raw.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                var trimmed = entries[i].Trim();
                if (trimmed.Length == 0) continue;

                string blueprint = trimmed;
                int chance = 100, minCount = 1, maxCount = 1;

                int colon = trimmed.IndexOf(':');
                if (colon >= 0)
                {
                    blueprint = trimmed.Substring(0, colon).Trim();
                    var tail = trimmed.Substring(colon + 1).Trim();

                    // Optional "xMin-Max" count suffix.
                    int xPos = tail.IndexOf('x');
                    if (xPos >= 0)
                    {
                        var countPart = tail.Substring(xPos + 1);
                        tail = tail.Substring(0, xPos).Trim();
                        int dash = countPart.IndexOf('-');
                        if (dash > 0)
                        {
                            int.TryParse(countPart.Substring(0, dash).Trim(), out minCount);
                            int.TryParse(countPart.Substring(dash + 1).Trim(), out maxCount);
                        }
                        else
                        {
                            int.TryParse(countPart.Trim(), out minCount);
                            maxCount = minCount;
                        }
                    }

                    // int.TryParse writes 0 on failure AND returns false —
                    // pair with an explicit sentinel guard (CLAUDE.md §7.2).
                    if (!int.TryParse(tail, out chance)) chance = 100;
                }

                if (blueprint.Length == 0) continue;   // ":50" is not a grant

                // Repair rather than reject: authoring typos degrade to a
                // sane grant instead of silently dropping content.
                if (chance < 0) chance = 0;
                if (chance > 100) chance = 100;
                if (minCount < 1) minCount = 1;
                if (maxCount < minCount) maxCount = minCount;

                result.Add(new GrantSpec
                {
                    Blueprint = blueprint,
                    Chance = chance,
                    MinCount = minCount,
                    MaxCount = maxCount,
                });
            }
            return result;
        }
    }
}
