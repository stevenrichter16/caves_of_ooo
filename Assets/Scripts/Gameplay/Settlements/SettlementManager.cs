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
        public event Action<string, HouseDramaState> DramaStateChanged;

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
                    string relapseMsg;
                    if (site.SiteType == RepairableSiteType.HeatStone)
                        relapseMsg = "The oven's firebox has cracked open again while you were away.";
                    else if (site.SiteType == RepairableSiteType.LightBeacon)
                        relapseMsg = "The watch lantern has guttered out again while you were away.";
                    else
                        relapseMsg = "The village well has gone bitter again while you were away.";
                    state.AddPendingMessage(relapseMsg);
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
                    MessageLog.AddAnnouncement("You purify the bitter water for now, but the well still needs a proper repair.");
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
                    MessageLog.AddAnnouncement("You rebuild the well's filtration ring with silver sand and set the water running clear again.");
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
                    MessageLog.AddAnnouncement("You teach the well-keeper how to maintain the filtration ring between seasons.");
                    break;

                case RepairMethodId.MendingRite:
                    if (!KnowsMendingRite(player))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.TemporarilyPurified;
                    site.OutcomeTier = RepairOutcomeTier.Temporary;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = site.ResolvedAtTurn + SettlementRepairDefinitions.MendingRelapseTurns;
                    MessageLog.AddAnnouncement("You trace the mending glyph across the cracks. The firebox holds — for now.");
                    break;

                case RepairMethodId.OvenRebuild:
                    if (site.Stage == RepairStage.StableRepair || site.Stage == RepairStage.ImprovedWithCaretaker)
                    {
                        MessageLog.Add("The oven's firebox is already rebuilt.");
                        return false;
                    }

                    if (!HasInventoryItem(player, SettlementRepairDefinitions.OvenBuildersGuideBlueprint)
                        || !ConsumeInventoryItem(player, SettlementRepairDefinitions.FireClayBlueprint))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.StableRepair;
                    site.OutcomeTier = RepairOutcomeTier.Stable;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    MessageLog.AddAnnouncement("You pack fresh fire clay into the firebox and seal the cracks properly. Heat radiates evenly again.");
                    break;

                case RepairMethodId.TeachBaker:
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
                    MessageLog.AddAnnouncement("You teach the farmer how to read the firebox for stress fractures and re-clay before they spread.");
                    break;

                case RepairMethodId.KindleRite:
                    if (!KnowsKindleRite(player))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.TemporarilyPurified;
                    site.OutcomeTier = RepairOutcomeTier.Temporary;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = site.ResolvedAtTurn + SettlementRepairDefinitions.KindleRelapseTurns;
                    MessageLog.AddAnnouncement("You speak the kindle rite and the lantern flares back to life, but the ward oil is nearly spent.");
                    break;

                case RepairMethodId.LanternReforge:
                    if (site.Stage == RepairStage.StableRepair || site.Stage == RepairStage.ImprovedWithCaretaker)
                    {
                        MessageLog.Add("The lantern is already properly reforged.");
                        return false;
                    }

                    if (!HasInventoryItem(player, SettlementRepairDefinitions.LanternOilRecipeBlueprint)
                        || !ConsumeInventoryItem(player, SettlementRepairDefinitions.WardOilBlueprint))
                    {
                        MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                        return false;
                    }

                    site.Stage = RepairStage.StableRepair;
                    site.OutcomeTier = RepairOutcomeTier.Stable;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    MessageLog.AddAnnouncement("You fill the reservoir with ward oil and reseal the lantern housing. Steady light pushes back the dark.");
                    break;

                case RepairMethodId.TeachWarden:
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
                    MessageLog.AddAnnouncement("You teach the warden how to mix ward oil and trim the wick. The lantern will burn through any night.");
                    break;

                default:
                    MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                    return false;
            }

            SyncConditions(state);
            RaiseSiteChanged(settlementId, site);
            return true;
        }

        public HouseDramaState GetDrama(string settlementId, string dramaId)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            return state?.GetDrama(dramaId);
        }

        public HouseDramaState ActivateDrama(string settlementId, string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId))
                return null;

            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            if (state == null)
                return null;

            HouseDramaState drama = state.GetDrama(dramaId);
            if (drama == null)
            {
                drama = new HouseDramaState { DramaId = dramaId };
                state.SetDrama(drama);
            }

            if (drama.State == HouseDramaActivationState.Dormant)
            {
                drama.State = HouseDramaActivationState.Active;
                drama.ActivatedTurn = GetCurrentTurn();
                RaiseDramaChanged(settlementId, drama);
            }

            return drama;
        }

        public bool ResolvePressurePoint(string settlementId, string dramaId, string pressurePointId, string pathTaken)
        {
            HouseDramaState drama = GetDrama(settlementId, dramaId);
            if (drama == null || string.IsNullOrEmpty(pressurePointId))
                return false;

            HousePressurePointState pp = drama.GetPressurePoint(pressurePointId);
            if (pp == null)
            {
                pp = new HousePressurePointState { Id = pressurePointId };
                drama.SetPressurePoint(pp);
            }

            pp.State = HouseDramaActivationState.Resolved;
            pp.Substate = "resolved:complete";
            pp.PathTaken = pathTaken ?? string.Empty;
            RaiseDramaChanged(settlementId, drama);
            return true;
        }

        public bool FailPressurePoint(string settlementId, string dramaId, string pressurePointId, string substate)
        {
            HouseDramaState drama = GetDrama(settlementId, dramaId);
            if (drama == null || string.IsNullOrEmpty(pressurePointId))
                return false;

            HousePressurePointState pp = drama.GetPressurePoint(pressurePointId);
            if (pp == null)
            {
                pp = new HousePressurePointState { Id = pressurePointId };
                drama.SetPressurePoint(pp);
            }

            pp.State = HouseDramaActivationState.Failed;
            pp.Substate = !string.IsNullOrEmpty(substate) ? substate : "failed:ignored";
            RaiseDramaChanged(settlementId, drama);
            return true;
        }

        public bool SetDramaEndState(string settlementId, string dramaId, HouseDramaEndState endState)
        {
            HouseDramaState drama = GetDrama(settlementId, dramaId);
            if (drama == null)
                return false;

            drama.EndState = endState;
            drama.State = HouseDramaActivationState.Resolved;
            drama.ResolvedAtTurn = GetCurrentTurn();
            RaiseDramaChanged(settlementId, drama);
            return true;
        }

        public bool AddDramaCorruption(string settlementId, string dramaId, int delta)
        {
            HouseDramaState drama = GetDrama(settlementId, dramaId);
            if (drama == null)
                return false;

            drama.CorruptionScore += delta;
            RaiseDramaChanged(settlementId, drama);
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
            _settlements.TryGetValue(activeZone.ZoneID, out state);

            foreach (var entity in activeZone.GetAllEntities())
            {
                // Refresh settlement site visuals (wells, ground markers)
                if (state != null)
                {
                    string siteId = entity.GetProperty("SettlementSiteId");
                    if (!string.IsNullOrEmpty(siteId))
                    {
                        RepairableSiteState site = state.GetSite(siteId);
                        if (site != null)
                            SettlementSiteVisuals.ApplyToEntity(entity, site);
                    }
                }

                // Campfire embers are handled by CampfireEmberRenderer (world-space)
                // registered via ZoneRenderer.SetZone
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

        private void RaiseDramaChanged(string settlementId, HouseDramaState drama)
        {
            DramaStateChanged?.Invoke(settlementId, drama);
        }

        private void SyncConditions(SettlementState state)
        {
            RepairableSiteState mainWell = state.GetSite(SettlementSiteDefinitions.MainWellSiteId);
            bool improvedWell = mainWell != null && mainWell.Stage == RepairStage.ImprovedWithCaretaker;
            state.SetCondition(SettlementSiteDefinitions.ImprovedWellCondition, improvedWell);

            RepairableSiteState oven = state.GetSite(SettlementSiteDefinitions.VillageOvenSiteId);
            bool improvedOven = oven != null && oven.Stage == RepairStage.ImprovedWithCaretaker;
            state.SetCondition(SettlementSiteDefinitions.ImprovedOvenCondition, improvedOven);

            RepairableSiteState lantern = state.GetSite(SettlementSiteDefinitions.VillageLanternSiteId);
            bool improvedLantern = lantern != null && lantern.Stage == RepairStage.ImprovedWithCaretaker;
            state.SetCondition(SettlementSiteDefinitions.ImprovedLanternCondition, improvedLantern);
        }

        public int GetCurrentTurn()
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

        private static bool KnowsMendingRite(Entity player)
        {
            return player.Properties.ContainsKey(SettlementSiteDefinitions.OvenKnowledgeProperty)
                || player.HasTag(SettlementSiteDefinitions.OvenKnowledgeProperty);
        }

        private static bool KnowsKindleRite(Entity player)
        {
            return player.Properties.ContainsKey(SettlementSiteDefinitions.LanternKnowledgeProperty)
                || player.HasTag(SettlementSiteDefinitions.LanternKnowledgeProperty);
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
