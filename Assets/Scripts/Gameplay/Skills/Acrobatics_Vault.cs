using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Cursor-selected leap within two cells. A creature target causes
    /// a landing one cell short and one committed primary melee strike. Empty
    /// destinations preserve ordinary obstacle-crossing movement. Legacy direct
    /// direction commands remain movement-only. See SPREAD-OPENING-DECISION-DESIGN §12.</summary>
    public class Acrobatics_Vault : BaseSkillPart
    {
        public override string Name => nameof(Acrobatics_Vault);

        public const int COOLDOWN = 15;
        public const int VAULT_DISTANCE = 2;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Vault",
                Command = "CommandVault",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.CursorCell,
                Range = VAULT_DISTANCE,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            if(ctx.TargetCell!=null)return CursorVault(ctx);
            // Legacy direction-only AI/audit callers retain movement-only semantics.
            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            int landX = actorPos.x + dx * VAULT_DISTANCE;
            int landY = actorPos.y + dy * VAULT_DISTANCE;
            if (!ctx.Zone.InBounds(landX, landY))
            {
                EmitSkillRejectedDiag(ctx, "out_of_bounds");
                MessageLog.Add(actor.GetDisplayName() + "'s vault would land off the map.");
                return false;
            }
            var landCell = ctx.Zone.GetCell(landX, landY);
            bool crossesArchive = false;
            foreach (var cell in ctx.Zone.GetOccupiedCells(actor, actorPos.x + dx, actorPos.y + dy))
                if (cell?.HasClosedArchiveBarrier() == true) { crossesArchive = true; break; }
            if (crossesArchive)
            {
                EmitSkillRejectedDiag(ctx, "sealed_barrier");
                MessageLog.Add(actor.GetDisplayName() + " cannot vault through the sealed archive.");
                return false;
            }
            if (landCell == null || landCell.IsSolid())
            {
                EmitSkillRejectedDiag(ctx, "landing_blocked");
                MessageLog.Add(actor.GetDisplayName() + "'s vault has no clear landing.");
                return false;
            }
            // Check the complete landing body; vault never displaces a creature.
            var landingCreature = MultiCellAbilityQueries.CreatureAtPlacement(ctx.Zone, actor, landX, landY);
            if (landingCreature != null)
            {
                EmitSkillRejectedDiag(ctx, "landing_occupied");
                MessageLog.Add(actor.GetDisplayName() + "'s vault is blocked by " + landingCreature.GetDisplayName() + ".");
                return false;
            }

            if (!ctx.Zone.CanPlaceFootprint(actor, landX, landY))
            {
                EmitSkillRejectedDiag(ctx, "landing_blocked");
                MessageLog.Add(actor.GetDisplayName() + "'s vault has no clear landing.");
                return false;
            }

            // Move actor directly to landing — skipping whatever was at
            // distance 1 (which is the whole point of Vault).
            if (!MovementSystem.TryMoveTo(actor, ctx.Zone, landX, landY))
            {
                EmitSkillRejectedDiag(ctx, "landing_blocked");
                return false;
            }
            if (actor.SpatialZone == ctx.Zone && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor))
                actor.ApplyEffect(new VaultPoiseEffect(), actor, ctx.Zone);
            MessageLog.Add(actor.GetDisplayName() + " vaults forward and finds an attacking opening!");
        
            return true;
        }
        private bool CursorVault(SkillEventContext ctx)
        {
            var actor=ctx.Attacker;var zone=ctx.Zone;var selected=ctx.TargetCell;
            if(!ReferenceEquals(zone.GetCell(selected.X,selected.Y),selected)
                ||!VaultTargeting.TryPlan(actor,zone,selected.X,selected.Y,out var landing,out var target,out var reason))
            {EmitSkillRejectedDiag(ctx,"invalid_cursor_landing");MessageLog.Add("Vault has no clear landing within two cells.");return false;}
            var source=zone.GetEntityCell(actor);
            if(landing!=source&&!MovementSystem.TryMoveTo(actor,zone,landing.X,landing.Y))return false;
            // Landing can trigger traps, death or transfers. Never attack a stale owner from afar.
            if(actor.SpatialZone!=zone||actor.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(actor))return true;
            actor.ApplyEffect(new VaultPoiseEffect(),actor,zone);
            if(target!=null&&target.SpatialZone==zone&&target.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(target)
                &&SpatialQuery.Distance(zone,actor,target)==1)
            {
                target.GetPart<BrainPart>()?.SetPersonallyHostile(actor);
                MessageLog.Add(actor.HasTag("Player") ? "You vault into a melee strike!" : actor.GetDisplayName()+" vaults into a melee strike!");
                CombatSystem.PerformCommittedMeleeAttack(actor,target,zone,ctx.Rng??new System.Random());
            }
            else MessageLog.Add(actor.HasTag("Player") ? "You vault to the chosen landing." : actor.GetDisplayName()+" vaults to the chosen landing.");
            return true;
        }
    }
}
