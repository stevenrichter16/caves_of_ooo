using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>A voluntary recovery journey west of Morrowfast. The basket and
    /// cloth are native, destructible owners; cached access never refills them.
    /// Only the actual recovered parcel satisfies delivery, including recovery
    /// before acceptance. Completion never gates the settlement's hospitality.</summary>
    public static class MorrowfastExpedition
    {
        public const string QuestId = "MorrowfastDryGoods", FieldZoneId = "Overworld.2.6.0";
        public const string ParcelId = "morrowfast-dry-goods:parcel", CacheId = "morrowfast-dry-goods:cache";
        public const int RewardDrams = 15;
        private const string Installed = "MorrowfastDryGoodsInstalled", Rewarded = "MorrowfastDryGoodsRewarded";

        /// <summary>Called only by fresh zone generation. Placement is on dry
        /// ground reachable from the east edge; no existing owner is removed.</summary>
        public static bool TryInstall(Zone zone, EntityFactory factory)
        {
            if (zone?.ZoneID != FieldZoneId || factory == null) return false;
            var origin = zone.GetCell(0, 0);
            Entity state = null;
            foreach (var e in origin.Objects) if (e.BlueprintName == "Grass") { state = e; break; }
            if (state == null || state.GetIntProperty(Installed) == 1) return false;
            var seat = FindSeat(zone); if (seat == null) return false;
            // Validate every content dependency before touching the native graph.
            if (!factory.Blueprints.ContainsKey("WovenBasket") || !factory.Blueprints.ContainsKey("Sack")) return false;
            var cache = factory.CreateEntity("WovenBasket"); var parcel = factory.CreateEntity("Sack");
            if (cache?.GetPart<ContainerPart>() == null || cache.GetPart<PhysicsPart>() == null
                || cache.GetPart<RenderPart>() == null || cache.GetPart<ExaminablePart>() == null
                || parcel?.GetPart<PhysicsPart>() == null || parcel.GetPart<RenderPart>() == null
                || parcel.GetPart<ExaminablePart>() == null) return false;
            cache.ID = CacheId; cache.SetIntProperty("MorrowfastDryGoodsCache", 1);
            cache.GetPart<RenderPart>().DisplayName = "Morrowfast supply basket";
            cache.GetPart<ExaminablePart>().Text = "A dry wicker basket beside the eastward path. A cloth tag reads: Farra, common supper, Morrowfast -- one chunk east. Open it and take the wrapped cloth before crossing into town. You can haul this basket within the field, but hauling stops at chunk boundaries. The surrounding growth burns, and the Choir remembers fire.";
            var cp = cache.GetPart<PhysicsPart>(); cp.Weight = 35; cp.Takeable = false;
            cache.AddPart(new HandlingPart { Carryable = false, Throwable = false, Weight = 35 });
            if (!cache.HasPart<DestructiblePart>()) cache.AddPart(new DestructiblePart { HP = 12, MaxHP = 12, Hardness = 0 });
            parcel.ID = ParcelId; parcel.SetIntProperty("MorrowfastDryGoodsParcel", 1);
            parcel.GetPart<RenderPart>().DisplayName = "wrapped supper cloth";
            parcel.GetPart<ExaminablePart>().Text = "Dry cloth wrapped for Farra's common supper in Morrowfast, one chunk east. Recovering it does not oblige you to help. Ordinary sacks cannot replace this marked parcel.";
            var pp = parcel.GetPart<PhysicsPart>(); pp.Takeable = true; pp.Weight = 12;
            var nested = parcel.GetPart<ContainerPart>(); if (nested != null) parcel.RemovePart(nested);
            var stack = parcel.GetPart<StackerPart>(); if (stack != null) parcel.RemovePart(stack);
            if (!parcel.HasPart<DestructiblePart>()) parcel.AddPart(new DestructiblePart { HP = 8, MaxHP = 8, Hardness = 0 });
            parcel.AddPart(new CompleteObjectiveOnTaken { Quest = QuestId, Objective = "recover" });
            if (!cache.GetPart<ContainerPart>().AddItem(parcel)) return false;
            if (!zone.AddEntity(cache, seat.X, seat.Y)) return false;
            state.SetIntProperty(Installed, 1);
            Diag.Record("worldgen", "MorrowfastSupplyPlaced", target: cache,
                payload: new { zone = zone.ZoneID, x = seat.X, y = seat.Y, parcel = ParcelId });
            return true;
        }

        private static Cell FindSeat(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var queue = new Queue<Cell>();
            for (int y = 1; y < Zone.Height - 1; y++)
            {
                var c = zone.GetCell(Zone.Width - 1, y);
                if (!c.BlocksMovement()) { seen[c.X, c.Y] = true; queue.Enqueue(c); }
            }
            Cell best = null; int score = int.MaxValue;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                bool clear = c.X >= 4 && c.X < Zone.Width - 3 && c.Y >= 2 && c.Y < Zone.Height - 2;
                foreach (var e in c.Objects)
                    if (e.BlueprintName != "Grass") { clear = false; break; }
                int distance = Math.Abs(c.X - 67) + Math.Abs(c.Y - 12);
                if (clear && distance < score) { best = c; score = distance; }
                for (int i = 0; i < 4; i++)
                {
                    var n = zone.GetCell(c.X + dx[i], c.Y + dy[i]);
                    if (n == null || seen[n.X, n.Y] || n.BlocksMovement()) continue;
                    seen[n.X, n.Y] = true; queue.Enqueue(n);
                }
            }
            return best;
        }

        private static bool ValidConversation(Entity speaker, Entity player, out Zone zone)
        {
            zone = SettlementRuntime.ActiveZone;
            if (zone?.ZoneID != MorrowfastSceneRuntime.ZoneID || WorldLocationContext.For(zone) == null
                || player == null || player != StoryletPart.LocalPlayer || !player.HasTag("Player")
                || player.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(player)
                || speaker == null || speaker != MorrowfastSceneRuntime.FindOwner(zone, "farra-sprig")
                || speaker.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(speaker)
                || FactionManager.IsHostile(speaker, player) || StoryletPart.Current == null) return false;
            var manager = WorldLocationContext.For(zone);
            if (!manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || !ReferenceEquals(current, zone)) return false;
            var pc = zone.GetEntityCell(player); var sc = zone.GetEntityCell(speaker);
            return pc != null && sc != null && pc.Objects.Contains(player) && sc.Objects.Contains(speaker)
                && Math.Max(Math.Abs(pc.X - sc.X), Math.Abs(pc.Y - sc.Y)) <= 1;
        }

        private static bool IsParcel(Entity e) => e?.ID == ParcelId
            && e.GetIntProperty("MorrowfastDryGoodsParcel") == 1 && e.GetIntProperty("MorrowfastDryGoodsReturned") == 0;

        private static Entity CarriedParcel(Entity player)
        {
            var inventory = player.GetPart<InventoryPart>();
            if (inventory != null) foreach (var e in inventory.Objects)
                if (IsParcel(e) && e.GetPart<PhysicsPart>()?.InInventory == player) return e;
            return null;
        }

        /// <summary>One contextual boot hint, only for the actual supply-bearing
        /// field. Does not start work or fabricate content in cached zones.</summary>
        public static string ArrivalHint(Zone zone)
        {
            if (zone?.ZoneID != FieldZoneId) return null;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.ID == CacheId)
                    return "Morrowfast lies one chunk east. Farra in its square can use help with the tagged supply basket near this field's east edge. Open the basket and carry its cloth into town. [C] then a direction opens nearby actions; [Q] keeps accepted work.";
            return null;
        }

        public static bool CanConversation(Entity speaker, Entity player, string command)
        {
            if (!ValidConversation(speaker, player, out var zone)) return false;
            bool active = StoryletPart.Current.IsQuestActive(QuestId);
            switch (command)
            {
                case "accept":
                    var map = WorldLocationContext.For(zone).WorldMap;
                    return map.GetPOI(2, 6) == null && map.GetBiome(2, 6) == BiomeType.Grovelands
                        && !active && !StoryletPart.Current.IsQuestCompleted(QuestId) && player.GetIntProperty(Rewarded) == 0;
                case "deliver": return active && player.GetIntProperty(Rewarded) == 0 && CarriedParcel(player) != null;
                case "release": return active;
                default: return false;
            }
        }

        public static bool TryConversation(Entity speaker, Entity player, string command)
        {
            if (!CanConversation(speaker, player, command)) return Reject(speaker, player, command, "unavailable");
            var zone = SettlementRuntime.ActiveZone; var manager = WorldLocationContext.For(zone);
            if (command == "accept")
            {
                // Validate the authored destination without generating or repairing it.
                // Farra remembers where supplies were left; loss is resolved by release.
                var poi = manager.WorldMap.GetPOI(2, 6);
                if (poi != null || manager.WorldMap.GetBiome(2, 6) != BiomeType.Grovelands)
                    return Reject(speaker, player, command, "destination_changed");
                StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0, EnteredStageAtTurn = TurnManager.Active?.TickCount ?? 0 });
                StoryletPart.Current.SetGiver(QuestId, speaker, SettlementRuntime.ActiveZone);
                if (CarriedParcel(player) != null)
                    StoryletPart.Current.FinishObjective(QuestId, "recover", player);
                MessageLog.Add("Farra: 'One chunk west, we left a tagged supply basket near the east edge of the compost field. Open it and take the wrapped supper cloth before crossing back into town. No trees need burning and no ore needs digging. The guest beds remain yours either way.' [Q] keeps these directions.");
            }
            else if (command == "release")
            {
                StoryletPart.Current.RefuseQuest(QuestId, player);
                MessageLog.Add("Farra releases the errand. Lost cloth is not a debt, and your place at supper stays open.");
            }
            else
            {
                var parcel = CarriedParcel(player);
                var clay = manager.Factory.Blueprints.ContainsKey("FireClay") ? manager.Factory.CreateEntity("FireClay") : null;
                if (parcel?.GetPart<RenderPart>() == null || parcel.GetPart<ExaminablePart>() == null || clay == null)
                    return Reject(speaker, player, command, "missing_supplies");
                var table = MorrowfastSceneRuntime.FindOwner(zone, "guest-supper-table");
                var seat = (table == null ? null : zone.GetEntityCell(table)) ?? zone.GetEntityCell(speaker);
                if (seat == null) return Reject(speaker, player, command, "no_return_place");
                // Preflight before transfer; the normal global quest and a persisted
                // reward latch prevent reentrant/repeated dialogue from paying twice.
                if (!player.GetPart<InventoryPart>().RemoveObject(parcel)) return Reject(speaker, player, command, "transfer_failed");
                player.SetIntProperty(Rewarded, 1);
                parcel.SetIntProperty("MorrowfastDryGoodsReturned", 1);
                parcel.GetPart<PhysicsPart>().Takeable = false;
                parcel.GetPart<RenderPart>().DisplayName = "dry supper cloth";
                parcel.GetPart<ExaminablePart>().Text = "The cloth recovered from the western field is laid ready for Morrowfast's common supper. The place was offered before the favor was done.";
                zone.AddEntity(parcel, seat.X, seat.Y);
                if (table != null)
                {
                    table.SetIntProperty("MorrowfastSupperSupplied", 1);
                    var examination = table.GetPart<ExaminablePart>();
                    if (examination != null) examination.Text = "A common supper table with the recovered dry cloth laid ready. No seat or bed is owed in return.";
                }
                StoryletPart.Current.CompleteQuest(QuestId, player);
                TradeSystem.SetDrams(player, TradeSystem.GetDrams(player) + RewardDrams);
                var inv = player.GetPart<InventoryPart>();
                if (inv == null || !inv.AddObject(clay))
                { var pc = zone.GetEntityCell(player); zone.AddEntity(clay, pc.X, pc.Y); ZoneRenderHooks.MarkCellDirty(pc.X, pc.Y, "ExpeditionReward"); }
                ZoneRenderHooks.MarkCellDirty(seat.X, seat.Y, "MorrowfastSupperSupplied");
                MessageLog.Add("Farra lays out the recovered cloth and gives you 15 drams and one fire clay. 'Nemm, west of the northern arch, is working on the bell. That clay can quiet its clapper, if you choose to help.'");
            }
            Diag.Record("quest", "MorrowfastExpeditionApplied", actor: player, target: speaker, payload: new { command, questId = QuestId });
            return true;
        }
        private static bool Reject(Entity speaker, Entity player, string command, string reason)
        { Diag.Record("quest", "MorrowfastExpeditionRejected", actor: player, target: speaker, payload: new { command, reason }); return false; }
    }
}
