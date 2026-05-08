using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class active ability: smack an adjacent target's
    /// equipped weapon out of their hand — the weapon is unequipped,
    /// removed from their inventory, and dropped onto their cell.
    /// Distinct from Slam (push) and RendArmor (stat reduction) —
    /// Disarm is the only ability that MUTATES AN ENEMY'S
    /// EQUIPMENT BINDING.
    ///
    /// <para><b>Mechanic:</b> requires a Cudgel-class weapon equipped.
    /// Finds an adjacent creature, looks up their first equipped
    /// melee weapon (via <see cref="Body.ForeachEquippedObject"/>,
    /// the canonical "what's worn?" walk). Unequips via
    /// <see cref="InventorySystem.UnequipItem"/>, removes from the
    /// target's inventory, and adds to the zone at the target's
    /// cell so it's visible + retrievable on the ground.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cudgel_Disarm</c>): "the
    /// only ability that mutates an enemy's equipment binding."</para>
    /// </summary>
    public class Cudgel_Disarm : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Disarm);

        public const int COOLDOWN = 50;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Disarm",
                Command = "CommandDisarm",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a cudgel-class weapon to disarm.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            // Find adjacent target.
            Entity target = null;
            for (int dir = 0; dir < 8 && target == null; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    target = e;
                    break;
                }
            }
            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to disarm.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Find the target's first equipped melee weapon (preferring
            // primary-hand). The Body.ForeachEquippedObject walk picks
            // up every equipped item; we filter by MeleeWeaponPart.
            Entity weaponEntity = null;
            var body = target.GetPart<Body>();
            if (body != null)
            {
                body.ForeachEquippedObject((item, bp) =>
                {
                    if (weaponEntity != null || item == null) return;
                    if (item.GetPart<MeleeWeaponPart>() != null)
                        weaponEntity = item;
                });
            }
            if (weaponEntity == null)
            {
                MessageLog.Add(target.GetDisplayName() + " has no weapon to disarm.");
                EmitSkillRejectedDiag(ctx, "target_unarmed");
                return;
            }

            // Unequip + drop on target's cell. UnequipItem returns to
            // inventory; we then RemoveObject + zone.AddEntity to
            // surface the item on the ground.
            if (!InventorySystem.UnequipItem(target, weaponEntity))
            {
                EmitSkillRejectedDiag(ctx, "unequip_failed");
                MessageLog.Add("The disarm fails.");
                return;
            }
            var targetInventory = target.GetPart<InventoryPart>();
            if (targetInventory != null) targetInventory.RemoveObject(weaponEntity);

            var targetPos = ctx.Zone.GetEntityPosition(target);
            if (targetPos.x >= 0)
                ctx.Zone.AddEntity(weaponEntity, targetPos.x, targetPos.y);

            MessageLog.Add(actor.GetDisplayName() + " disarms " + target.GetDisplayName()
                + "! The " + weaponEntity.GetDisplayName() + " clatters to the ground.");
        }
    }
}
