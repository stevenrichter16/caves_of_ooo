using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking a tinker's forge - the weaponcraft station.
    /// Per M3-L3 (Docs/CRAFTING-ALCHEMY-SYSTEM.md), forging, re-forging and
    /// quenching all happen where the metal is worked: the forge/temper
    /// commands gate on this adjacency the same way brewing gates on
    /// <see cref="AlchemyStillPart"/>. Same furniture shape as
    /// ChairPart/BedPart: a marker part on a PhysicalObject blueprint.
    ///
    /// The forge is also the crafting VERB surface: it declares Forge /
    /// Re-forge / Quench rows on the look-mode world-action menu and, on
    /// selection, resolves the player's set-aside items
    /// (<see cref="CraftingMarkPart"/>) into command executions. Selection-
    /// count problems reject legibly here; everything else (gating,
    /// atomicity, stat math) lives in the commands and services.
    /// </summary>
    public class ForgePart : Part, IStationCapability, IStationReadinessGuard
    {
        public override string Name => "Forge";

        // ── station capability (Docs/MACHINE-STATIONS.md): found by capability, not by this concrete type ──
        static readonly string[] StationKinds = { "forge", "cold-work" };
        public System.Collections.Generic.IReadOnlyList<string> Kinds => StationKinds;
        public int Reach => 1;
        public ReadinessMode Readiness => ReadinessMode.Strict;
        /// <summary>The field work rig is only a working station while its exact payload holds (moved here from ForgeWorkReceipt.Ready).</summary>
        public bool TryBlock(Entity station, Zone zone, out string code)
        {
            code = null;
            if (FieldWorkRigRules.IsRig(station) && !FieldWorkRigRules.Payload(station)) { code = "rig-payload"; return true; }
            return false;
        }

        /// <summary>
        /// EntityFactory for weapon/component creation. Wired at bootstrap
        /// (mirrors CorpsePart.Factory); when unwired the forge rows degrade
        /// to a message instead of crashing mid-event.
        /// </summary>
        public static EntityFactory Factory;

        /// <summary>Stable readiness code to the one sentence a player reads on the disabled "work at the rig" row. Unknown codes fall back to a generic sentence.</summary>
        public static string RigUnavailableReason(string code)
        {
            switch (code)
            {
                case StationReadiness.Faulted: return "The rig is not sound. Repair it before working at it.";
                case StationReadiness.Hauled: return "The rig cannot be worked while it is being dragged.";
                case StationReadiness.Harvested: return "The rig has been taken apart.";
                case StationReadiness.Destroyed: return "The rig is wrecked.";
                default: return "The rig cannot be worked right now.";
            }
        }

        void AddRigUnavailableRow(InventoryActionList actions, Entity actor)
        {
            var zone = actor?.SpatialZone;
            if (actions == null || actor == null || zone == null || SpatialQuery.Distance(zone, actor, ParentEntity) > 1) return;
            if (StationReadiness.Evaluate(ParentEntity, zone, "forge", out string code)) return; // ready: nothing to explain
            // Low priority on purpose: the menu opens on its first row, which should be something usable (repair, haul), not the explanation.
            actions.AddDisabled("RigUnavailable", "work at the rig", code ?? "unavailable", RigUnavailableReason(code), '\0', 5);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                if (FieldWorkRigRules.IsRig(ParentEntity) && !ForgeWorkReceipt.Near(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Entity>("Actor")?.SpatialZone))
                {
                    // A rig that is not ready lists no craft rows. Someone standing beside it is told why instead of seeing nothing (spec S3/S4).
                    AddRigUnavailableRow(e.GetParameter<InventoryActionList>("Actions"), e.GetParameter<Entity>("Actor"));
                    return true;
                }
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    var actor = e.GetParameter<Entity>("Actor");
                    if (actor != null)
                    {
                        // Sectioned picker (forge redesign, user spec): one
                        // section per part kind, each listing the carried
                        // items with pick-marks, and ONE Craft button at the
                        // bottom. Headers are inert (CraftNoop).
                        AddSection(actions, actor, "Heads / blades", 200,
                            item => item.GetPart<WeaponComponentPart>()?.Slot == "Blade");
                        AddSection(actions, actor, "Hafts", 170,
                            item => item.GetPart<WeaponComponentPart>()?.Slot == "Haft");
                        AddSection(actions, actor, "Bindings", 140,
                            item => item.GetPart<WeaponComponentPart>()?.Slot == "Binding");
                        // Any effect-carrying brew is a quench medium
                        // (user-directed change, 2026-07-19: coatings,
                        // tonics, and throwable flasks all quench).
                        // Effect-less brews stay hidden — TryTemper would
                        // reject them as carrying nothing.
                        AddSection(actions, actor, "Quenches", 110,
                            item =>
                            {
                                var brewItem = item.GetPart<BrewItemPart>();
                                return brewItem != null && brewItem.GetEffects().Count > 0;
                            });
                        AddSection(actions, actor, "Weapon (re-forge / temper)", 80,
                            item => item.HasPart<MeleeWeaponPart>()
                                && !item.HasPart<WeaponComponentPart>());

                        actions.AddAction("Craft", ">> Craft <<", "CraftKit", 'c', 1);
                    }
                    else
                    {
                        // Actor-less gather (non-menu callers): the flat verb
                        // rows remain the stable programmatic surface.
                        actions.AddAction("Forge", "forge the kit (blade+haft+binding)", "ForgeWeapon", 'f', 20);
                        actions.AddAction("ForgeBatch", "forge a full batch", "ForgeWeaponBatch", 'F', 19);
                        actions.AddAction("Reforge", "re-forge weapon with component", "ReforgeWeapon", 'r', 18);
                        actions.AddAction("Quench", "quench one weapon in coating", "QuenchWeapon", 'q', 17);
                    }
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "ForgeWeapon" && command != "ForgeWeaponBatch"
                    && command != "ReforgeWeapon" && command != "QuenchWeapon"
                    && command != "CraftKit")
                {
                    return true;
                }

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null)
                    return true;

                var zone = e.GetParameter<Zone>("Zone");
                if (FieldWorkRigRules.IsRig(ParentEntity))
                {
                    if (!FieldWorkRigRules.Workshop(actor, ParentEntity, zone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
                    e.Handled = true; return false;
                }
                e.Handled = true;

                switch (command)
                {
                    case "CraftKit":
                        HandleCraftKit(actor, zone);
                        break;
                    case "ForgeWeapon":
                        HandleForge(actor, zone, batch: false);
                        break;
                    case "ForgeWeaponBatch":
                        HandleForge(actor, zone, batch: true);
                        break;
                    case "ReforgeWeapon":
                        HandleReforge(actor, zone);
                        break;
                    case "QuenchWeapon":
                        HandleQuench(actor, zone);
                        break;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// One menu section: an inert header row, then a toggle row per
        /// carried item matching <paramref name="eligible"/> (or a dim
        /// "(none carried)" row so the section still teaches the concept).
        /// </summary>
        private static void AddSection(InventoryActionList actions, Entity actor,
            string title, int basePriority, System.Predicate<Entity> eligible)
        {
            actions.AddAction("Section", "== " + title + " ==", "CraftNoop", '\0', basePriority);

            int before = actions.Actions.Count;
            CraftingMarkPart.AddToggleRows(actions, actor, eligible, basePriority - 1);
            if (actions.Actions.Count == before)
                actions.AddAction("SectionEmpty", "    (none carried)", "CraftNoop", '\0', basePriority - 1);
        }

        /// <summary>
        /// The one-button Craft dispatch (forge redesign): a full kit forges
        /// (and quenches the fresh weapon when a quench is picked); a picked
        /// weapon plus ONE component re-forges; a picked weapon plus a
        /// quench tempers it. Anything else explains what Craft needs.
        /// </summary>
        private static void HandleCraftKit(Entity actor, Zone zone)
        {
            if (Factory == null)
            {
                MessageLog.Add("The forge is cold. (Forging is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);

            Entity blade = null, haft = null, binding = null;
            for (int i = 0; i < marked.Components.Count; i++)
            {
                var part = marked.Components[i].GetPart<WeaponComponentPart>();
                if (part == null) continue;
                if (part.Slot == "Blade") blade = marked.Components[i];
                else if (part.Slot == "Haft") haft = marked.Components[i];
                else if (part.Slot == "Binding") binding = marked.Components[i];
            }

            if (blade != null && haft != null && binding != null)
            {
                var forgeCommand = new ForgeWeaponCommand(blade, haft, binding, Factory);
                var result = InventorySystem.ExecuteCommand(forgeCommand, actor, zone);
                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        MessageLog.Add(result.ErrorMessage);
                    return;
                }

                if (marked.Coatings.Count == 1 && forgeCommand.ForgedWeapons.Count > 0)
                {
                    var quenchResult = InventorySystem.ExecuteCommand(
                        new TemperWeaponCommand(forgeCommand.ForgedWeapons[0], marked.Coatings[0]),
                        actor, zone);
                    if (!quenchResult.Success && !string.IsNullOrEmpty(quenchResult.ErrorMessage))
                        MessageLog.Add(quenchResult.ErrorMessage);
                }
                return;
            }

            if (marked.Weapons.Count == 1 && marked.Components.Count == 1)
            {
                HandleReforge(actor, zone);
                return;
            }

            if (marked.Weapons.Count == 1 && marked.Coatings.Count == 1)
            {
                HandleQuench(actor, zone);
                return;
            }

            MessageLog.Add("Craft needs a blade, a haft, and a binding picked (add a quench to "
                + "temper the fresh weapon) - or pick a weapon plus one part to re-forge, "
                + "or a weapon plus a quench to temper it.");
        }

        private static void HandleForge(Entity actor, Zone zone, bool batch)
        {
            if (Factory == null)
            {
                MessageLog.Add("The forge is cold. (Forging is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);
            if (!TrySelectComponents(marked, out Entity blade, out Entity haft, out Entity binding))
                return;

            int count = 1;
            if (batch)
            {
                count = WeaponForgingService.GetMaxBatchCount(blade, haft, binding);
                if (count < 1)
                    count = 1;
            }

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, Factory, count), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        private static void HandleReforge(Entity actor, Zone zone)
        {
            if (Factory == null)
            {
                MessageLog.Add("The forge is cold. (Forging is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);
            if (marked.Weapons.Count != 1)
            {
                MessageLog.Add("Set aside exactly one weapon to re-forge. (Set aside: "
                    + marked.Weapons.Count + " weapons.)");
                return;
            }

            if (marked.Components.Count != 1)
            {
                MessageLog.Add("Set aside exactly one replacement component to re-forge with. (Set aside: "
                    + marked.Components.Count + " components.)");
                return;
            }

            var result = InventorySystem.ExecuteCommand(
                new ReforgeWeaponCommand(marked.Weapons[0], marked.Components[0], Factory), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        private static void HandleQuench(Entity actor, Zone zone)
        {
            var marked = CraftingMarkPart.CollectMarked(actor);
            if (marked.Weapons.Count != 1)
            {
                MessageLog.Add("Set aside exactly one weapon to quench. (Set aside: "
                    + marked.Weapons.Count + " weapons.)");
                return;
            }

            if (marked.Coatings.Count != 1)
            {
                MessageLog.Add("Set aside one brewed coating to quench in. (Set aside: "
                    + marked.Coatings.Count + " coatings.)");
                return;
            }

            var result = InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(marked.Weapons[0], marked.Coatings[0]), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        /// <summary>
        /// True when <paramref name="actor"/> stands on or orthogonally/
        /// diagonally adjacent to (3×3 box) a cell containing an entity
        /// with a ForgePart. False when the zone is null or the actor
        /// isn't placed in it.
        /// </summary>
        public static bool IsNearForge(Entity actor, Zone zone) => ForgeWorkReceipt.Resolve(actor, zone) != null;

        /// <summary>
        /// Resolve the marked components into exactly one blade + haft +
        /// binding; on any other shape, report what IS set aside (naming
        /// each slot) and return false with nothing consumed.
        /// </summary>
        private static bool TrySelectComponents(CraftingMarkPart.MarkedSelection marked,
            out Entity blade, out Entity haft, out Entity binding)
        {
            blade = null;
            haft = null;
            binding = null;
            int blades = 0, hafts = 0, bindings = 0;

            List<Entity> components = marked.Components;
            for (int i = 0; i < components.Count; i++)
            {
                var part = components[i].GetPart<WeaponComponentPart>();
                if (part == null)
                    continue;

                if (part.Slot == "Blade") { blade = components[i]; blades++; }
                else if (part.Slot == "Haft") { haft = components[i]; hafts++; }
                else if (part.Slot == "Binding") { binding = components[i]; bindings++; }
            }

            if (blades != 1 || hafts != 1 || bindings != 1)
            {
                MessageLog.Add("Forging needs exactly one blade, one haft, and one binding set aside. (Set aside: "
                    + blades + " blade(s), " + hafts + " haft(s), " + bindings + " binding(s).)");
                return false;
            }

            return true;
        }
    }
}
