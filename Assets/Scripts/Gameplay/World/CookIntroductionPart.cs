using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Historical speech, not a live remote query. The listener must
    /// explicitly relay it; losing the cook later does not erase past words.</summary>
    public sealed class CookIntroductionKnowledgePart : Part
    {
        public override string Name => "CookIntroductionKnowledge";
        public Entity Learner, Speaker, Pan;
        public string WorldKey, SpeakerID, PanID, RepairCauseID, SiteID;
        public static bool ValidFor(Entity actor, string worldKey)
        {
            var knowledge = actor?.GetPart<CookIntroductionKnowledgePart>();
            return knowledge?.ParentEntity == actor && knowledge.Learner == actor && !string.IsNullOrEmpty(worldKey)
                && knowledge.WorldKey == worldKey && knowledge.SiteID == KitchenBatchPart.KitchenZoneID
                && knowledge.Speaker != null && knowledge.Speaker.ID == knowledge.SpeakerID
                && knowledge.Pan != null && knowledge.Pan.ID == knowledge.PanID && !string.IsNullOrEmpty(knowledge.RepairCauseID);
        }
    }

    /// <summary>A particular cook acknowledges a particular paid repair while
    /// both player and repaired pan are locally observable.</summary>
    public sealed class CookIntroductionPart : Part
    {
        public override string Name => "CookIntroduction";
        public const string IntroductionCommand = "CookRepairIntroduction";
        public bool Configured;
        public int WorldSeed, PanX, PanY;
        public string WorldKey, ZoneID, CookID, PanID;
        public Entity Pan;
        public bool BindWorldKey(string key)
        { if (string.IsNullOrEmpty(key) || Configured && WorldKey != key) return false; WorldKey = key; return true; }
        public bool Configure(Zone zone, Entity pan, int worldSeed)
        {
            if (Configured || zone?.ZoneID != KitchenBatchPart.KitchenZoneID || ParentEntity?.GetPart<CookIntroductionPart>() != this
                || !LocalGatheringClaims.Ground(ParentEntity, zone, "SpreadWaysideCook")
                || !LocalGatheringClaims.Ground(pan, zone, "ConnectedBatchPan") || pan.ID == ParentEntity.ID
                || pan.GetPart<RepairablePart>()?.RecipeId != "clay-batch-pan") return false;
            string key = WorldKey ?? LocalGatheringClaims.WorldKeyFor(zone); if (string.IsNullOrEmpty(key)) return false;
            WorldKey = key; WorldSeed = worldSeed; ZoneID = zone.ZoneID; CookID = ParentEntity.ID; Pan = pan; PanID = pan.ID;
            var at = zone.GetEntityPosition(pan); PanX = at.x; PanY = at.y; Configured = true; return true;
        }
        bool CanIntroduce(Entity actor, Zone zone)
        {
            var manager = WorldLocationContext.For(zone); var fault = Pan?.GetPart<RepairablePart>();
            if (!Configured || ZoneID != KitchenBatchPart.KitchenZoneID || zone?.ZoneID != ZoneID
                || ParentEntity?.GetPart<CookIntroductionPart>() != this || ParentEntity.ID != CookID
                || !LocalGatheringClaims.Ground(ParentEntity, zone, "SpreadWaysideCook") || !LocalGatheringClaims.Alive(ParentEntity)
                || !LocalGatheringClaims.PlayerAvailable(actor, zone) || !LocalGatheringClaims.Friendly(ParentEntity, actor)
                || !ReferenceEquals(SettlementRuntime.ActiveZone, zone) || manager == null || manager.WorldSeed != WorldSeed
                || string.IsNullOrEmpty(WorldKey) || WorldKey != LocalGatheringClaims.WorldKeyFor(zone)
                || !manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) || cached != zone
                || Pan?.ID != PanID || !LocalGatheringClaims.Ground(Pan, zone, "ConnectedBatchPan")
                || zone.GetEntityPosition(Pan) != (PanX, PanY) || fault?.ParentEntity != Pan || !fault.Repaired
                || fault.RecipeId != "clay-batch-pan" || fault.RepairedBy != actor || fault.RepairWorldKey != WorldKey
                || fault.RepairedZoneID != ZoneID || string.IsNullOrEmpty(fault.RepairCauseID)
                || SpatialQuery.Distance(zone, actor, ParentEntity) > 1 || CookIntroductionKnowledgePart.ValidFor(actor, WorldKey)) return false;
            var eye = zone.GetEntityCell(ParentEntity); var target = zone.GetEntityCell(Pan); var brain = ParentEntity.GetPart<BrainPart>();
            return brain?.ParentEntity == ParentEntity && brain.SightRadius >= 0
                && AIHelpers.ChebyshevDistance(eye.X, eye.Y, target.X, target.Y) <= brain.SightRadius
                && AIHelpers.HasLineOfSight(zone, eye.X, eye.Y, target.X, target.Y);
        }
        public static bool IsCommand(string command) => command == IntroductionCommand;
        public bool TryAct(Entity actor, Zone zone, string action) => action == "introduction" && CanIntroduce(actor, zone)
            && InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity, IntroductionCommand), actor, zone).Success;
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (e.ID == "GetInventoryActions")
            {
                if (CanIntroduce(actor, zone)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("CookIntroduction", "ask Orven to vouch for the repair", IntroductionCommand, 'i', 21);
                return true;
            }
            if (e.ID != "InventoryAction" || !IsCommand(e.GetStringParameter("Command"))) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !CanIntroduce(actor, zone) || !tx.TryClaim(actor, actor, IntroductionCommand)
                || !tx.TryClaim(ParentEntity, actor, IntroductionCommand) || !tx.TryClaim(Pan, actor, IntroductionCommand)) return true;
            var old = actor.GetPart<CookIntroductionKnowledgePart>();
            var knowledge = new CookIntroductionKnowledgePart { Learner = actor, Speaker = ParentEntity, Pan = Pan,
                SpeakerID = CookID, PanID = PanID, RepairCauseID = Pan.GetPart<RepairablePart>().RepairCauseID, WorldKey = WorldKey, SiteID = ZoneID };
            tx.Do(() => { if (old != null) actor.RemovePart(old); actor.AddPart(knowledge); },
                () => { actor.RemovePart(knowledge); if (old != null) actor.AddPart(old); });
            tx.AfterCommit(() =>
            {
                MessageLog.Add("Orven studies your patch. Tell Nella I trust your hands. Her tied row lies northwest of here, beyond the glade.");
                Diag.Record("event", "CookIntroductionLearned", actor, ParentEntity, new { WorldKey, PanID, knowledge.RepairCauseID });
            });
            e.Handled = true; return false;
        }
    }
}
