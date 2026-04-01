using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public class SettlementManager
    {
        public static SettlementManager Current { get; private set; }

        private readonly Dictionary<string, SettlementState> _settlements = new Dictionary<string, SettlementState>();
        private Func<int> _currentTurnProvider;
        private readonly Func<string, PointOfInterest> _poiResolver;

        public event Action<string, RepairableSiteState> SiteStateChanged;

        public SettlementManager(Func<int> currentTurnProvider = null, Func<string, PointOfInterest> poiResolver = null)
        {
            _currentTurnProvider = currentTurnProvider;
            _poiResolver = poiResolver;
            Current = this;
        }

        public void SetCurrentTurnProvider(Func<int> currentTurnProvider)
        {
            _currentTurnProvider = currentTurnProvider;
        }

        public SettlementState GetOrCreateSettlement(string settlementId, PointOfInterest poi)
        {
            if (string.IsNullOrEmpty(settlementId))
                return null;

            SettlementState state;
            if (_settlements.TryGetValue(settlementId, out state))
                return state;

            if (poi == null)
                return null;

            state = new SettlementState
            {
                SettlementId = settlementId,
                SettlementName = poi != null ? poi.Name : settlementId,
                LastAdvancedTurn = GetCurrentTurn()
            };

            foreach (var site in SettlementSiteDefinitions.CreateInitialSites(settlementId, poi))
                state.SetSite(site);

            SyncConditions(state);
            _settlements[settlementId] = state;
            return state;
        }

        public RepairableSiteState GetSite(string settlementId, string siteId)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            return state?.GetSite(siteId);
        }

        public bool AdvanceSettlement(string settlementId, int currentTurn)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            if (state == null)
                return false;

            bool changed = false;
            foreach (var kvp in state.Sites)
            {
                RepairableSiteState site = kvp.Value;
                if (site.Stage == RepairStage.TemporarilyPurified
                    && site.RelapseAtTurn.HasValue
                    && currentTurn >= site.RelapseAtTurn.Value)
                {
                    site.Stage = RepairStage.Fouled;
                    site.OutcomeTier = RepairOutcomeTier.None;
                    site.ResolvedByMethod = RepairMethodId.None;
                    site.RelapseAtTurn = null;
                    site.ResolvedAtTurn = -1;
                    state.AddPendingMessage("The village well has gone bitter again while you were away.");
                    changed = true;
                    RaiseSiteChanged(settlementId, site);
                }
            }

            state.LastAdvancedTurn = currentTurn;
            if (changed)
                SyncConditions(state);

            return changed;
        }

        public bool ApplyRepairMethod(string settlementId, string siteId, RepairMethodId method, Entity player)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            RepairableSiteState site = state?.GetSite(siteId);
            if (state == null || site == null || player == null)
            {
                MessageLog.Add("There is nothing to repair here.");
                return false;
            }

            switch (method)
            {
                case RepairMethodId.PurifySpell:
                    if (!KnowsPurifyWater(player))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.TemporarilyPurified;
                    site.OutcomeTier = RepairOutcomeTier.Temporary;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = site.ResolvedAtTurn + SettlementRepairDefinitions.PurifyRelapseTurns;
                    MessageLog.Add("You purify the bitter water for now, but the well still needs a proper repair.");
                    break;

                case RepairMethodId.ManualRepair:
                    if (site.Stage == RepairStage.StableRepair || site.Stage == RepairStage.ImprovedWithCaretaker)
                    {
                        MessageLog.Add("The well's filtration ring is already repaired.");
                        return false;
                    }

                    if (!HasInventoryItem(player, SettlementRepairDefinitions.WellMaintenanceManualBlueprint)
                        || !ConsumeInventoryItem(player, SettlementRepairDefinitions.SilverSandBlueprint))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.StableRepair;
                    site.OutcomeTier = RepairOutcomeTier.Stable;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    MessageLog.Add("You rebuild the well's filtration ring with silver sand and set the water running clear again.");
                    break;

                case RepairMethodId.TeachCaretaker:
                    if (site.Stage != RepairStage.StableRepair)
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.ImprovedWithCaretaker;
                    site.OutcomeTier = RepairOutcomeTier.Improved;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    MessageLog.Add("You teach the well-keeper how to maintain the filtration ring between seasons.");
                    break;

                default:
                    MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                    return false;
            }

            SyncConditions(state);
            RaiseSiteChanged(settlementId, site);
            return true;
        }

        public bool HasCondition(string settlementId, string condition)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            return state != null && state.HasCondition(condition);
        }

        public void SetCondition(string settlementId, string condition, bool enabled)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            if (state == null)
                return;

            state.SetCondition(condition, enabled);
        }

        public List<string> ConsumePendingMessages(string settlementId)
        {
            SettlementState state;
            if (!_settlements.TryGetValue(settlementId, out state))
                return new List<string>();
            return state.ConsumePendingMessages();
        }

        public void RefreshActiveZonePresentation(Zone activeZone)
        {
            if (activeZone == null)
                return;

            SettlementState state;
            if (!_settlements.TryGetValue(activeZone.ZoneID, out state))
                return;

            foreach (var entity in activeZone.GetAllEntities())
            {
                string siteId = entity.GetProperty("SettlementSiteId");
                if (string.IsNullOrEmpty(siteId))
                    continue;

                RepairableSiteState site = state.GetSite(siteId);
                if (site != null)
                    SettlementSiteVisuals.ApplyToEntity(entity, site);
            }
        }

        public static void ResetCurrent()
        {
            Current = null;
        }

        private void RaiseSiteChanged(string settlementId, RepairableSiteState site)
        {
            SiteStateChanged?.Invoke(settlementId, site);
        }

        private void SyncConditions(SettlementState state)
        {
            RepairableSiteState mainWell = state.GetSite(SettlementSiteDefinitions.MainWellSiteId);
            bool improvedWell = mainWell != null && mainWell.Stage == RepairStage.ImprovedWithCaretaker;
            state.SetCondition(SettlementSiteDefinitions.ImprovedWellCondition, improvedWell);
        }

        private int GetCurrentTurn()
        {
            return _currentTurnProvider != null ? _currentTurnProvider() : 0;
        }

        private PointOfInterest ResolvePoi(string settlementId)
        {
            return _poiResolver != null ? _poiResolver(settlementId) : null;
        }

        private static bool KnowsPurifyWater(Entity player)
        {
            return player.Properties.ContainsKey(SettlementSiteDefinitions.StartingVillageKnowledgeProperty)
                || player.HasTag(SettlementSiteDefinitions.StartingVillageKnowledgeProperty);
        }

        private static bool HasInventoryItem(Entity player, string blueprint)
        {
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                if (inventory.Objects[i].BlueprintName == blueprint)
                    return true;
            }

            return false;
        }

        private static bool ConsumeInventoryItem(Entity player, string blueprint)
        {
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                if (inventory.Objects[i].BlueprintName != blueprint)
                    continue;

                var item = inventory.Objects[i];
                inventory.RemoveObject(item);
                MessageLog.Add("You hand over " + item.GetDisplayName() + ".");
                return true;
            }

            return false;
        }
    }
}
