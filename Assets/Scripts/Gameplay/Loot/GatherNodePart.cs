using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A renewable (or single-use) forage/mineral point: a plant, ore vein,
    /// or similar placed world object that yields items from a
    /// <see cref="LootTable"/> when harvested. Declares a "Harvest"
    /// inventory action via the same GetInventoryActions/InventoryAction
    /// pipeline as GrimoirePart/SchematicPart/ContainerPart — no new UI.
    ///
    /// Unlike ContainerPart (items sit inside, waiting to be opened),
    /// harvested items go straight into the harvester's inventory, and the
    /// node itself tracks depletion:
    /// - MaxUses harvests are available before it runs dry.
    /// - RegrowTurns &gt; 0: after running dry, the node becomes harvestable
    ///   again once that many turns have passed (a drosera ring, a berry
    ///   bush).
    /// - RegrowTurns &lt;= 0 (default): the node is single-use — once dry,
    ///   it is removed from the zone entirely (a one-time cutting, a vein
    ///   worked out).
    ///
    /// Blueprint params: LootTableID, MaxUses, RegrowTurns,
    /// DepletedDisplayName, HarvestMessage, AlreadyDepletedMessage.
    /// </summary>
    public class GatherNodePart : Part
    {
        public override string Name => "GatherNode";

        public string LootTableID = "";
        public int MaxUses = 1;
        public int RegrowTurns = 0;
        public string DepletedDisplayName = "";
        public string HarvestMessage = "";
        public string AlreadyDepletedMessage = "There is nothing left to gather here.";

        /// <summary>
        /// -1 = not yet initialized (resolves to MaxUses on first check —
        /// blueprint fields are applied via reflection AFTER the
        /// parameterless constructor runs, so MaxUses isn't known yet at
        /// construction time; mirrors ReagentPart's lazy-resolve pattern).
        /// </summary>
        public int RemainingUses = -1;

        /// <summary>Turn number at/after which a depleted, regrowing node becomes harvestable again.</summary>
        public int DepletedUntilTurn = 0;

        /// <summary>Test injection hook — mirrors CorpsePart.TestRng.</summary>
        public Random TestRng;

        /// <summary>
        /// Test injection hook for the turn clock. Deliberately NOT wired to
        /// TurnManager's EndTurn/CurrentActor machinery (tuned for status
        /// effects, not needed here) — regrow timing only needs a
        /// monotonic counter. Null in real play (falls through to
        /// TurnManager.Active.TickCount); tests set this directly to drive
        /// regrow-elapsed scenarios deterministically without constructing
        /// a real turn loop.
        /// </summary>
        public int? TestCurrentTurn;

        private Random _rng;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                EnsureUsesInitialized();
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null && IsHarvestable(CurrentTurn()))
                    actions.AddAction("Harvest", "harvest", "HarvestNode", 'h', 25);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "HarvestNode") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                return DoHarvest(actor, e);
            }

            return true;
        }

        private void EnsureUsesInitialized()
        {
            if (RemainingUses < 0)
                RemainingUses = Math.Max(0, MaxUses);
        }

        private bool IsHarvestable(int currentTurn)
        {
            if (RemainingUses > 0)
                return true;

            return RegrowTurns > 0 && currentTurn >= DepletedUntilTurn;
        }

        private int CurrentTurn()
        {
            return TestCurrentTurn ?? TurnManager.Active?.TickCount ?? 0;
        }

        private bool DoHarvest(Entity actor, GameEvent e)
        {
            e.Handled = true;
            EnsureUsesInitialized();

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null)
            {
                MessageLog.Add("You have nowhere to put it.");
                Diag.Record("craft", "NodeHarvestRejected", actor: actor, target: ParentEntity,
                    payload: new { lootTableId = LootTableID, reason = "NoInventory" });
                return false;
            }

            int currentTurn = CurrentTurn();

            // Regrow-due: a depleted-but-regrowing node resets before this
            // harvest proceeds.
            if (RemainingUses <= 0 && RegrowTurns > 0 && currentTurn >= DepletedUntilTurn)
                RemainingUses = Math.Max(1, MaxUses);

            if (RemainingUses <= 0)
            {
                MessageLog.Add(AlreadyDepletedMessage);
                Diag.Record("craft", "NodeHarvestRejected", actor: actor, target: ParentEntity,
                    payload: new { lootTableId = LootTableID, reason = "Depleted" });
                return false;
            }

            if (!LootTableRegistry.TryGetTable(LootTableID, out LootTable table))
            {
                MessageLog.Add("There's nothing here worth taking after all.");
                Diag.Record("craft", "NodeHarvestRejected", actor: actor, target: ParentEntity,
                    payload: new { lootTableId = LootTableID, reason = "UnknownTable" });
                return false;
            }

            var rng = TestRng ?? _rng ?? (_rng = new Random());
            List<string> blueprintNames = table.Roll(rng);

            var factory = GatherHarvestFactory.Factory;
            int given = 0;
            if (factory != null)
            {
                for (int i = 0; i < blueprintNames.Count; i++)
                {
                    Entity item = factory.CreateEntity(blueprintNames[i]);
                    if (item != null && inventory.AddObject(item))
                        given++;
                }
            }

            if (given > 0 && !string.IsNullOrEmpty(HarvestMessage))
                MessageLog.AddAnnouncement(HarvestMessage);
            else if (given > 0)
                MessageLog.Add($"You gather from the {ParentEntity.GetDisplayName()}.");
            else
                MessageLog.Add("You come away empty-handed this time.");

            RemainingUses -= 1;

            if (RemainingUses <= 0)
                Deplete(actor);

            Diag.Record("craft", "NodeHarvested", actor: actor, target: ParentEntity,
                payload: new { lootTableId = LootTableID, itemCount = given, remainingUses = RemainingUses });

            return false;
        }

        private void Deplete(Entity actor)
        {
            if (RegrowTurns > 0)
            {
                DepletedUntilTurn = CurrentTurn() + RegrowTurns;

                if (!string.IsNullOrEmpty(DepletedDisplayName))
                {
                    var render = ParentEntity?.GetPart<RenderPart>();
                    if (render != null)
                        render.DisplayName = DepletedDisplayName;
                }

                return;
            }

            // Single-use: remove the node from the zone entirely. Resolve
            // the zone from the harvesting actor's BrainPart (mirrors
            // ChairPart/AIRetrieverPart — there is no zone back-reference
            // on the entity/part itself).
            Zone zone = actor.GetPart<BrainPart>()?.CurrentZone;
            if (zone == null || ParentEntity == null)
                return;

            zone.RemoveEntity(ParentEntity);
        }
    }

    /// <summary>
    /// Static factory-injection point for GatherNodePart, following the
    /// established MaterialReactionResolver.Factory / CorpsePart.Factory /
    /// ConversationActions.Factory convention. Wired by GameBootstrap at
    /// startup; left null in tests that don't exercise item creation (the
    /// harvest still resolves availability/depletion correctly — it just
    /// gives zero items, which callers assert on explicitly).
    /// </summary>
    public static class GatherHarvestFactory
    {
        public static EntityFactory Factory;
    }
}
