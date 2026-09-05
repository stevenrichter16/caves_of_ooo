using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original communal nest defense. Persistent one-shot
    /// activation, preflighted before mutation; all sixteen join real turns.</summary>
    public sealed class PricklebrowNestPart : TriggerOnStepPart
    {
        public override string Name => "PricklebrowNest";
        public static EntityFactory Factory;
        public bool Triggered;
        public const int DefenderCount = 16;
        public PricklebrowNestPart() { ConsumeOnTrigger = false; TriggerFaction = "Beasts"; }

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            if (Triggered || !actor.HasTag("Creature") || actor.GetStatValue("Hitpoints") <= 0) return;
            var here = zone.GetEntityCell(ParentEntity);
            if (here == null || Factory == null || !Factory.Blueprints.ContainsKey("PrickleBrowGecko")
                || TurnManager.Active == null) { Reject("dependencies"); return; }
            // Leave the immediate ring open: the disturbance does not cage the
            // actor in a ring of overlapping or adjacent instant spawns.
            var cells = new List<Cell>(DefenderCount);
            CollectDefenderCells(zone, here.X, here.Y, cells);
            if (cells.Count != DefenderCount) { Reject("space"); return; }
            // Create before committing any spawn. Factory failure cannot leave
            // half a swarm or consume the only activation.
            var defenders = new Entity[DefenderCount];
            for (int i = 0; i < defenders.Length; i++)
            {
                defenders[i] = Factory.CreateEntity("PrickleBrowGecko");
                if (defenders[i]?.GetPart<BrainPart>() == null) { Reject("blueprint"); return; }
            }
            Triggered = true;
            var rng = actor.GetPart<BrainPart>()?.Rng ?? new Random(
                FormationSelector.StableIndex(zone.ZoneID + ":nest:" + here.X + ":" + here.Y, int.MaxValue));
            for (int i = 0; i < defenders.Length; i++)
            {
                var defender = defenders[i];
                zone.AddEntity(defender, cells[i].X, cells[i].Y);
                var brain = defender.GetPart<BrainPart>();
                brain.CurrentZone = zone; brain.Rng = rng;
                brain.SetPersonallyHostile(actor);
                TurnManager.Active.AddEntity(defender);
            }
            MessageLog.Add("Sixteen pricklebrows spill from the litter around the disturbed nest!");
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "PricklebrowNestDisturbed", ParentEntity, actor,
                    new { zoneId = zone.ZoneID, defenders = DefenderCount });
        }
        /// <summary>Shared cold-path capacity contract for generation and
        /// activation. Occupants, reservations and intervening walls all count.</summary>
        public static void CollectDefenderCells(Zone zone, int x, int y, List<Cell> cells)
        {
            cells.Clear();
            for (int r = 2; r <= 5 && cells.Count < DefenderCount; r++)
                for (int dx = -r; dx <= r && cells.Count < DefenderCount; dx++)
                    for (int dy = -r; dy <= r && cells.Count < DefenderCount; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                        var cell = zone.GetCell(x + dx, y + dy);
                        if (cell == null || cell.BlocksMovement() || zone.GenReservedCells.Contains((cell.X, cell.Y))) continue;
                        if (!AIHelpers.HasLineOfSight(zone, x, y, cell.X, cell.Y)) continue;
                        cells.Add(cell);
                    }
        }

        private void Reject(string reason)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "PricklebrowNestRejected", ParentEntity, null, new { reason });
        }
    }
}
