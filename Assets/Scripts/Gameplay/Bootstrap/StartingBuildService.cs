using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public sealed class StartingBuildResult
    {
        public bool Success;
        public string BuildId;
        public List<string> Errors = new List<string>();
        public int ItemsGranted;
        public int SkillsGranted;
    }

    /// <summary>
    /// Applies a <see cref="StartingBuildDef"/> to a freshly created player
    /// (Docs/STARTING-BUILDS-IMPL.md). Atomic by construction: every rule that
    /// can fail is checked BEFORE the first mutation, so a bad content edit
    /// leaves the player exactly as it was and the caller can fall back to
    /// Classic. Emits <c>build/Applied</c> or <c>build/Rejected</c>.
    /// </summary>
    public static class StartingBuildService
    {
        public const string PropertyName = "StartingBuild";

        public static StartingBuildResult Apply(Entity player, EntityFactory factory, StartingBuildDef build)
        {
            var r = new StartingBuildResult { BuildId = build?.Id };

            if (player == null) return Reject(r, player, "no player");
            if (factory == null) return Reject(r, player, "no entity factory");
            if (build == null) return Reject(r, player, "no build");

            string already = player.GetProperty(PropertyName);
            if (!string.IsNullOrEmpty(already))
                return Reject(r, player, "player already has the '" + already + "' start; a second build would double every grant");

            if (build.IsClassic) return ApplyClassic(player, factory, build, r);

            var problems = StartingBuildRegistry.ValidateOne(build, factory);
            if (problems.Count > 0)
            {
                foreach (var p in problems) r.Errors.Add(p);
                return Reject(r, player, null);
            }

            var skills = player.GetPart<SkillsPart>();
            var inventory = player.GetPart<InventoryPart>();
            if (skills == null || inventory == null)
                return Reject(r, player, "the player has no " + (skills == null ? "Skills" : "Inventory") + " part");

            // ── all checks passed: mutate ──
            SetStat(player, "Strength", build.Attributes.Strength);
            SetStat(player, "Agility", build.Attributes.Agility);
            SetStat(player, "Toughness", build.Attributes.Toughness);
            SetStat(player, "Ego", build.Attributes.Ego);

            string firstEquippedWeapon = null;
            foreach (var it in build.Items)
            {
                int granted = GrantItem(factory, inventory, player, it, r);
                if (granted > 0) r.ItemsGranted++;
                if (it.Equip && firstEquippedWeapon == null
                    && factory.Blueprints.TryGetValue(it.Blueprint, out var bp) && bp.Parts.ContainsKey("MeleeWeapon"))
                    firstEquippedWeapon = it.Blueprint;
            }

            var grantedSkills = new List<string>();
            foreach (var s in build.Skills)
            {
                if (skills.AddSkill(s, "start-build:" + build.Id)) { r.SkillsGranted++; grantedSkills.Add(s); }
                else r.Errors.Add("could not add skill '" + s + "'");
            }

            if (firstEquippedWeapon != null)
            {
                var primary = PrimaryHandWeapon(player);
                if (primary?.BlueprintName != firstEquippedWeapon)
                    r.Errors.Add("the primary hand holds '" + (primary?.BlueprintName ?? "nothing") + "', not '" + firstEquippedWeapon + "'");
            }

            player.Properties[PropertyName] = build.Id;
            r.Success = r.Errors.Count == 0;
            Record("Applied", player, new { id = build.Id, success = r.Success, skills = grantedSkills, items = r.ItemsGranted, errors = r.Errors });
            return r;
        }

        /// <summary>
        /// Give the player the Classic start when no build was chosen or a chosen
        /// build could not be applied (the picker could not open, or content
        /// validation rejected the build before touching anything). A bare
        /// character with no weapon and no spells must never be checkpointed.
        /// Works with or without a loaded "classic" entry. A player that already
        /// has a start is left alone.
        /// </summary>
        public static StartingBuildResult ApplyClassicFallback(Entity player, EntityFactory factory)
        {
            var def = StartingBuildRegistry.Get(StartingBuildRegistry.ClassicId)
                      ?? new StartingBuildDef { Id = StartingBuildRegistry.ClassicId, Name = "Classic", Kind = "classic" };
            return Apply(player, factory, def);
        }

        /// <summary>Classic: the legacy kit and six spells, with the dagger now
        /// actually equipped (it used to sit in the pack, so a fresh character
        /// punched for 2.1 a swing instead of 3.9).</summary>
        private static StartingBuildResult ApplyClassic(Entity player, EntityFactory factory, StartingBuildDef build, StartingBuildResult r)
        {
            if (player.GetPart<SkillsPart>() == null || player.GetPart<InventoryPart>() == null)
                return Reject(r, player, "the player has no Skills or Inventory part");

            r.ItemsGranted = NewGameLoadout.Grant(player, factory);
            var dagger = player.GetPart<InventoryPart>().Objects.Find(o => o.BlueprintName == "Dagger");
            if (dagger != null && !InventorySystem.Equip(player, dagger))
                r.Errors.Add("could not equip the starting dagger");
            r.SkillsGranted = StartingSpellKit.GrantAll(player);

            player.Properties[PropertyName] = build.Id ?? StartingBuildRegistry.ClassicId;
            r.Success = r.Errors.Count == 0;
            Record("Applied", player, new { id = build.Id, success = r.Success, skills = StartingSpellKit.SpellClasses, items = r.ItemsGranted, errors = r.Errors });
            return r;
        }

        private static int GrantItem(EntityFactory factory, InventoryPart inventory, Entity player, StartingBuildItem it, StartingBuildResult r)
        {
            int count = it.EffectiveCount;
            var first = factory.CreateEntity(it.Blueprint);
            if (first == null) { r.Errors.Add("could not create '" + it.Blueprint + "'"); return 0; }

            var stacker = first.GetPart<StackerPart>();
            if (it.Equip)
            {
                // Equipped gear is one object each; a count above 1 would mean two shields.
                if (!inventory.AddObject(first)) { r.Errors.Add("pack refused '" + it.Blueprint + "'"); return 0; }
                if (!InventorySystem.Equip(player, first)) r.Errors.Add("could not equip '" + it.Blueprint + "'");
                return 1;
            }
            if (stacker != null && count > 1)
            {
                stacker.StackCount = count;
                if (!inventory.AddObject(first)) { r.Errors.Add("pack refused '" + it.Blueprint + "'"); return 0; }
                return 1;
            }
            int added = 0;
            for (int n = 0; n < count; n++)
            {
                var each = n == 0 ? first : factory.CreateEntity(it.Blueprint);
                if (each != null && inventory.AddObject(each)) added++;
            }
            if (added == 0) r.Errors.Add("pack refused '" + it.Blueprint + "'");
            return added > 0 ? 1 : 0;
        }

        /// <summary>
        /// The weapon that makes the PRIMARY attack: the same selection
        /// <c>CombatSystem.GatherMeleeWeapons</c> makes (an equipped melee
        /// weapon, else the hand's natural weapon; a hand holding a non-weapon
        /// such as a buckler falls through to its natural fist). Returns null
        /// when the player has no Hand with a weapon.
        /// </summary>
        public static Entity PrimaryHandWeapon(Entity actor)
        {
            var body = actor?.GetPart<Body>();
            if (body == null) return null;

            Entity firstAny = null;
            var hands = body.GetPartsByType("Hand");
            for (int i = 0; i < hands.Count; i++)
            {
                var part = hands[i];
                Entity weapon = null;
                if (part._Equipped != null && part.FirstSlotForEquipped
                    && part._Equipped.GetPart<MeleeWeaponPart>() != null)
                    weapon = part._Equipped;
                else if (part._DefaultBehavior != null && part.FirstSlotForDefaultBehavior
                    && part._DefaultBehavior.GetPart<MeleeWeaponPart>() != null)
                    weapon = part._DefaultBehavior;

                if (weapon == null) continue;
                if (part.Primary || part.DefaultPrimary) return weapon;
                if (firstAny == null) firstAny = weapon;
            }
            return firstAny;
        }

        private static void SetStat(Entity e, string name, int value)
        {
            var stat = e.GetStat(name);
            if (stat == null) return;
            stat.BaseValue = value;
        }

        private static StartingBuildResult Reject(StartingBuildResult r, Entity player, string reason)
        {
            if (reason != null) r.Errors.Add(reason);
            r.Success = false;
            Record("Rejected", player, new { id = r.BuildId, errors = r.Errors });
            return r;
        }

        private static void Record(string kind, Entity player, object payload)
        {
            if (Diag.IsChannelEnabled("build"))
                Diag.Record(category: "build", kind: kind, target: player, payload: payload);
        }
    }
}
