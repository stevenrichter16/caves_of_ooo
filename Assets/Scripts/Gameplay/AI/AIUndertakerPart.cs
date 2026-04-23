namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part: NPC haul-corpse-to-graveyard behaviour. Listens for
    /// <see cref="AIBoredEvent"/> and, on an idle tick, searches the zone for
    /// an unclaimed corpse + a reachable graveyard, reserves the corpse, and
    /// pushes <see cref="DisposeOfCorpseGoal"/>.
    ///
    /// <para><b>Qud parity note.</b> Qud's equivalent,
    /// <c>XRL.World.Parts.DepositCorpses</c>, is a Part on the <b>container</b>
    /// (the graveyard) that hijacks bored NPCs via <c>IdleQueryEvent</c>
    /// (DepositCorpses.cs lines 50-57). CoO's <see cref="IdleQueryEvent"/> is
    /// shaped for furniture-offer (TargetX/Y + Action/Cleanup), not NPC
    /// hijacking, so we invert the dispatch: behaviour part on the NPC,
    /// consuming <see cref="AIBoredEvent"/>. Mirrors the existing
    /// <see cref="AIPetterPart"/> / <see cref="AIWellVisitorPart"/> /
    /// <see cref="AIGuardPart"/> convention. Documented adaptation — same
    /// category as M4's per-cell vs zone-level IsInterior choice.</para>
    ///
    /// <para><b>Blueprint wiring</b> — attach to NPCs via:
    /// <code>
    ///   { "Name": "AIUndertaker", "Params": [] }
    /// </code>
    /// Intended wearer: <c>Undertaker</c> (new M5.3 NPC blueprint).</para>
    ///
    /// <para><b>Reservation.</b> On success, the selected corpse gets
    /// <c>DepositCorpsesReserve=50</c> set so subsequent undertakers skip
    /// it. <see cref="DisposeOfCorpseGoal.OnPop"/> clears the reservation
    /// when the goal terminates (success, failure, or give-up).</para>
    ///
    /// <para><b>Skipped when</b> (any of):</para>
    /// <list type="bullet">
    ///   <item>NPC has <c>NoHauling</c> tag (Qud parity DepositCorpses.cs line 75)</item>
    ///   <item>NPC already has a <c>DisposeOfCorpseGoal</c> on stack (idempotency)</item>
    ///   <item>NPC lacks <c>InventoryPart</c> — can't carry</item>
    ///   <item>Zone has no <c>Graveyard</c>-tagged entity with ContainerPart</item>
    ///   <item>No unclaimed <c>Corpse</c>-tagged entity in zone</item>
    ///   <item>Hauling the corpse would exceed NPC's max carry weight</item>
    /// </list>
    /// </summary>
    public class AIUndertakerPart : AIBehaviorPart
    {
        public override string Name => "AIUndertaker";

        /// <summary>
        /// Percent chance per bored tick to attempt corpse disposal (0-100).
        /// Undertaking is a job, not a whim — default 100 means "always try
        /// when idle and work is available." Can be lowered to make the NPC
        /// also idle-chat / wander occasionally.
        /// </summary>
        public int Chance = 100;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null)
                return true;

            // NoHauling blocks undertakers from ever hauling (Qud DepositCorpses.cs line 75).
            // Lets scenario authors opt NPCs out without removing the part.
            if (ParentEntity.HasTag("NoHauling"))
                return true;

            // Idempotency — don't stack DisposeOfCorpseGoals.
            if (brain.HasGoal("DisposeOfCorpseGoal"))
                return true;

            // Probability gate.
            if (brain.Rng.Next(100) >= Chance)
                return true;

            var inventory = ParentEntity.GetPart<InventoryPart>();
            if (inventory == null)
                return true;

            Entity graveyard = FindGraveyard(brain.CurrentZone);
            if (graveyard == null)
                return true;

            Entity corpse = FindNearestUnclaimedReachableCorpse(brain.CurrentZone, ParentEntity, inventory);
            if (corpse == null)
                return true;

            // Claim the corpse for this goal instance. DisposeOfCorpseGoal.OnPop
            // clears this so it's always transient.
            corpse.SetIntProperty("DepositCorpsesReserve", 50);

            brain.PushGoal(new DisposeOfCorpseGoal(corpse, graveyard));
            return false; // consumed
        }

        // ====================================================================
        // Search helpers
        // ====================================================================

        private static Entity FindGraveyard(Zone zone)
        {
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (!entity.HasTag("Graveyard")) continue;
                if (entity.GetPart<ContainerPart>() == null) continue;
                // First match wins — villages typically have one graveyard. If
                // we add multi-graveyard support later, swap for nearest-choice.
                return entity;
            }
            return null;
        }

        private static Entity FindNearestUnclaimedReachableCorpse(Zone zone, Entity actor, InventoryPart inventory)
        {
            var actorPos = zone.GetEntityPosition(actor);
            if (actorPos.x < 0) return null;

            int maxCarry = inventory.GetMaxCarryWeight();
            int currentWeight = inventory.GetCarriedWeight();

            Entity best = null;
            int bestDist = int.MaxValue;

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (!entity.HasTag("Corpse")) continue;

                // Already claimed by another undertaker's in-flight goal?
                if (entity.GetIntProperty("DepositCorpsesReserve", 0) > 0) continue;

                // Would carrying this corpse exceed the NPC's max carry weight?
                // maxCarry < 0 means unlimited (no Strength stat).
                if (maxCarry >= 0)
                {
                    int corpseWeight = InventoryPart.GetItemWeight(entity);
                    if (currentWeight + corpseWeight > maxCarry)
                        continue;
                }

                var cell = zone.GetEntityCell(entity);
                if (cell == null) continue;

                int d = AIHelpers.ChebyshevDistance(actorPos.x, actorPos.y, cell.X, cell.Y);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = entity;
                }
            }

            return best;
        }
    }
}
