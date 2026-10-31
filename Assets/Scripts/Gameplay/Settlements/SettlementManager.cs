using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public class SettlementManager
    {
        public static SettlementManager Current { get; private set; }

        private readonly Dictionary<string, SettlementState> _settlements = new Dictionary<string, SettlementState>();
        private Func<int> _currentTurnProvider;
        private Func<string, PointOfInterest> _poiResolver;

        public event Action<string, RepairableSiteState> SiteStateChanged;

        public SettlementManager(Func<int> currentTurnProvider = null, Func<string, PointOfInterest> poiResolver = null)
            : this(currentTurnProvider, poiResolver, activate: true) { }

        /// <summary>Load candidates must not replace the live settlement registry.</summary>
        internal SettlementManager(Func<int> currentTurnProvider, Func<string, PointOfInterest> poiResolver, bool activate)
        {
            _currentTurnProvider = currentTurnProvider;
            _poiResolver = poiResolver;
            if (activate) Activate();
        }

        internal void Activate() => Current = this;

        // The resolver is runtime wiring, rebuilt from the owning loaded map.
        internal void SetPointOfInterestResolver(Func<string, PointOfInterest> resolver) => _poiResolver = resolver;

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
            {
                // C3c: a settlement saved before its place's structures were authored gains them.
                foreach (var data in PlaceSiteCatalog.ForZone(settlementId))
                    if (state.GetSite(data.Id) == null) state.SetSite(PlaceSiteCatalog.NewState(data));
                return state;
            }

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
                    if (site.SiteType == RepairableSiteType.Well && state.HasCondition(WellKeeperPurification.Condition))
                    {
                        // Reconcile once on entry, rather than simulating one
                        // refresh for every missed period during a long absence.
                        site.ResolvedAtTurn = currentTurn;
                        site.RelapseAtTurn = (int)Math.Min(int.MaxValue, (long)currentTurn + SettlementRepairDefinitions.PurifyRelapseTurns);
                        state.AddPendingMessage("The well-keeper has renewed the purification rite while you were away. The ring still needs a proper repair.");
                        changed = true;
                        RaiseSiteChanged(settlementId, site);
                        continue;
                    }
                    site.Stage = RepairStage.Fouled;
                    site.OutcomeTier = RepairOutcomeTier.None;
                    site.ResolvedByMethod = RepairMethodId.None;
                    site.RelapseAtTurn = null;
                    site.ResolvedAtTurn = -1;
                    string relapseMsg;
                    if (site.SiteType == RepairableSiteType.Structure)
                        relapseMsg = PlaceSiteCatalog.Get(PlaceSiteCatalog.PlaceAt(settlementId), site.SiteId)?.Relapse ?? "Something you patched here has failed again while you were away.";
                    else if (site.SiteType == RepairableSiteType.HeatStone)
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

        internal void EstablishPurificationCare(SettlementState state, RepairableSiteState site)
        {
            state.SetCondition(WellKeeperPurification.Condition, true);
            site.Stage = RepairStage.TemporarilyPurified;
            site.OutcomeTier = RepairOutcomeTier.Temporary;
            site.ResolvedByMethod = RepairMethodId.PurifySpell;
            site.ResolvedAtTurn = GetCurrentTurn();
            site.RelapseAtTurn = (int)Math.Min(int.MaxValue, (long)site.ResolvedAtTurn + SettlementRepairDefinitions.PurifyRelapseTurns);
            SyncConditions(state);
            RaiseSiteChanged(state.SettlementId, site);
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

                    if (!ConsumeInventoryItem(player, tag: "GrimoireCopy"))
                    {
                        MessageLog.Add("You'll need a grimoire copy to leave for study.");
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

                    if (!ConsumeInventoryItem(player, tag: "GrimoireCopy"))
                    {
                        MessageLog.Add("You'll need a grimoire copy to leave for study.");
                        return false;
                    }

                    site.Stage = RepairStage.ImprovedWithCaretaker;
                    site.OutcomeTier = RepairOutcomeTier.Improved;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    MessageLog.AddAnnouncement("You teach the warden how to mix ward oil and trim the wick. The lantern will burn through any night.");
                    break;

                case RepairMethodId.Contracted:
                    // C1: payment is taken by the ContractPlaceRepair action before this runs.
                    if (site.Stage == RepairStage.StableRepair || site.Stage == RepairStage.ImprovedWithCaretaker)
                    {
                        MessageLog.Add("That is already properly repaired.");
                        return false;
                    }
                    site.Stage = RepairStage.StableRepair;
                    site.OutcomeTier = RepairOutcomeTier.Stable;
                    site.ResolvedByMethod = method;
                    site.ResolvedAtTurn = GetCurrentTurn();
                    site.RelapseAtTurn = null;
                    state.SetCondition(ContractCondition(siteId), true);
                    MessageLog.AddAnnouncement("The Concord's mason does the work in a day and leaves a copy of the contract nailed by the door.");
                    break;

                default:
                    MessageLog.Add(SettlementRepairDefinitions.GetMethodFailureMessage(method));
                    return false;
            }

            SyncConditions(state);
            RaiseSiteChanged(settlementId, site);
            return true;
        }

        /// <summary>
        /// C3c: repair a data-driven structure (PlaceSiteCatalog) by hand. <paramref name="lasting"/>
        /// needs every material and the tool (kept) and gives a stable repair; a patch holds
        /// for its relapse turns. All materials or none are taken. Returns null on success or
        /// the refusal reason.
        /// </summary>
        public string RepairPlaceSite(string settlementId, string siteId, bool lasting, Entity player)
        {
            if (player == null) return "missing_player";
            var data = PlaceSiteCatalog.Get(PlaceSiteCatalog.PlaceAt(settlementId), siteId);
            if (data == null) return "unknown_site";
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            RepairableSiteState site = state?.GetSite(siteId);
            if (site == null) return "unknown_site";
            if (site.Stage >= RepairStage.StableRepair) { MessageLog.Add("The " + data.Name + " is already properly repaired."); return "already_repaired"; }
            var fix = lasting ? data.Lasting : data.Patch;
            if (!lasting && !data.HasPatch) return "no_patch";
            if (!lasting && site.Stage == RepairStage.TemporarilyPurified) { MessageLog.Add("The " + data.Name + " is patched already."); return "already_patched"; }
            foreach (var m in fix.Materials)
                if (CavesOfOoo.Concerns.ConcernItems.CountCarried(player, m.Blueprint) < m.Count)
                { MessageLog.Add("You don't have what the " + data.Name + " needs."); return "missing_materials"; }
            if (!string.IsNullOrEmpty(fix.Tool) && CavesOfOoo.Concerns.ConcernItems.CountCarried(player, fix.Tool) < 1)
            { MessageLog.Add("You need a tool for that."); return "missing_tool"; }
            foreach (var m in fix.Materials)
                CavesOfOoo.Concerns.ConcernItems.TakeCarried(player, m.Blueprint, m.Count);

            site.ResolvedAtTurn = GetCurrentTurn();
            if (lasting)
            {
                site.Stage = RepairStage.StableRepair; site.OutcomeTier = RepairOutcomeTier.Stable;
                site.ResolvedByMethod = RepairMethodId.ManualRepair; site.RelapseAtTurn = null;
            }
            else
            {
                site.Stage = RepairStage.TemporarilyPurified; site.OutcomeTier = RepairOutcomeTier.Temporary;
                site.ResolvedByMethod = RepairMethodId.PatchedRepair;
                site.RelapseAtTurn = (int)Math.Min(int.MaxValue, (long)site.ResolvedAtTurn + fix.RelapseTurns);
            }
            MessageLog.AddAnnouncement(fix.Text);
            SyncConditions(state);
            RaiseSiteChanged(settlementId, site);
            return null;
        }

        /// <summary>C1: the settlement condition left by a Concord-contracted repair of a site.</summary>
        public static string ContractCondition(string siteId) => "ConcordContract." + siteId;

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

        public Dictionary<string, SettlementState> GetAllSettlementsSnapshot()
        {
            return new Dictionary<string, SettlementState>(_settlements);
        }

        public void RestoreSettlements(Dictionary<string, SettlementState> settlements)
        {
            _settlements.Clear();
            if (settlements == null)
                return;

            foreach (var kvp in settlements)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    _settlements[kvp.Key] = kvp.Value;
            }
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
                if (inventory.Objects[i].BlueprintName == blueprint && inventory.CanConsumeOne(inventory.Objects[i]))
                    return true;
            }

            return false;
        }

        private static bool ConsumeInventoryItem(Entity player, string blueprint = null, string tag = null)
        {
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                var item = inventory.Objects[i];
                if ((blueprint != null && item.BlueprintName != blueprint)
                    || (tag != null && !item.HasTag(tag)) || !inventory.CanConsumeOne(item)) continue;
                string name = InventoryPart.GetUnitDisplayName(item);
                if (!inventory.TryConsumeOne(item)) continue;
                MessageLog.Add("You hand over " + name + ".");
                return true;
            }

            return false;
        }
    }
}
