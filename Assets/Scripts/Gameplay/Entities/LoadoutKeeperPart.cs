using System;

namespace CavesOfOoo.Core
{
    /// <summary>Outfitters can change which two saved equipment presets are prepared.
    /// Opening their menu conveys no lasting permission; every edit rechecks presence.</summary>
    public sealed class LoadoutKeeperPart : Part
    {
        public override string Name => "LoadoutKeeper";
        public const string Command = "ManageLoadouts";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions"
                && CanServe(e.GetParameter<Entity>("Actor"), ParentEntity, out _))
                e.GetParameter<InventoryActionList>("Actions")?.AddAction(
                    "ManageLoadouts", "prepare loadouts (outfitter)", Command, '\0', 25);
            return true;
        }

        public static bool CanServe(Entity actor, Entity keeper, out string reason)
        {
            reason = "Visit an outfitter to change your two prepared loadouts.";
            if (actor == null || keeper == null || actor == keeper
                || !actor.HasTag("Player") || !keeper.HasTag("Creature") || keeper.HasTag("Player")
                || keeper.GetPart<LoadoutKeeperPart>()?.ParentEntity != keeper)
                return false;
            if (actor.GetStatValue("Hitpoints") <= 0 || keeper.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(actor) || CombatSystem.IsDeathHandled(keeper))
            {
                reason = "That outfitter cannot help you now.";
                return false;
            }
            var zone = actor.SpatialZone;
            if (zone == null || keeper.SpatialZone != zone)
                return false;
            var playerCell = zone.GetEntityCell(actor);
            var keeperCell = zone.GetEntityCell(keeper);
            if (playerCell == null || keeperCell == null || !playerCell.Objects.Contains(actor)
                || !keeperCell.Objects.Contains(keeper)
                || Math.Max(Math.Abs(playerCell.X - keeperCell.X), Math.Abs(playerCell.Y - keeperCell.Y)) > 1)
            {
                reason = "Stand next to the outfitter to prepare loadouts.";
                return false;
            }
            if (FactionManager.GetFeelingUnfloored(keeper, actor) <= FactionManager.HOSTILE_THRESHOLD
                || FactionManager.GetFeelingUnfloored(actor, keeper) <= FactionManager.HOSTILE_THRESHOLD)
            {
                reason = "That outfitter is unwilling to help you.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Known outfitters in existing saves predate this authorable Part.
        /// Upgrade their service without rerolling stock or altering their purse.</summary>
        internal static void UpgradeExistingTrader(Entity trader)
        {
            if (trader == null || trader.GetPart<LoadoutKeeperPart>() != null) return;
            if (trader.BlueprintName == "Armorer" || trader.BlueprintName == "Weaponsmith"
                || (trader.BlueprintName == "MorrowfastOrrit"
                    && trader.GetPart<MorrowfastResidentPart>()?.ResidentId == "southwest-craftsperson"))
                trader.AddPart(new LoadoutKeeperPart());
        }
    }
}
