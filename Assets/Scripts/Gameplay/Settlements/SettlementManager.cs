using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public class SettlementManager
    {
        public static SettlementManager Current { get; private set; }

        private readonly Dictionary<string, SettlementState> _settlements = new Dictionary<string, SettlementState>();
        private Func<int> _currentTurnProvider;
        private Func<string, PointOfInterest> _poiResolver;

        public event Action<string, RepairableSiteState> SiteStateChanged;

        internal const string WeirSiteId = "Weir";
        internal const string ContractPending = "contract_pending";

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

        /// <summary>Read existing saved state without creating a village or backfilling its sites.</summary>
        public bool TryGetExistingSite(string settlementId, string siteId, out RepairableSiteState site)
        {
            site = null;
            if (string.IsNullOrEmpty(settlementId) || string.IsNullOrEmpty(siteId)
                || !_settlements.TryGetValue(settlementId, out var state)
                || state == null || state.SettlementId != settlementId) return false;
            site = state.GetSite(siteId);
            return site != null && site.SiteId == siteId;
        }

        /// <summary>Read already saved local conditions without creating or
        /// backfilling a settlement. Content readers must not initialize supply.</summary>
        public bool HasExistingCondition(string settlementId, string condition)
            => ExistingSettlement(settlementId)?.HasCondition(condition) == true;

        internal SettlementState ExistingSettlement(string settlementId)
            => !string.IsNullOrEmpty(settlementId) && _settlements.TryGetValue(settlementId, out var state)
                && state?.SettlementId == settlementId ? state : null;

        public bool AdvanceSettlement(string settlementId, int currentTurn)
        {
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            if (state == null) return false;
            // A finished contract changes the weir in place. Returning true
            // here would unload the cached village and change its inventory.
            CompleteWeirContract(state, currentTurn, whilePresent: false);
            return AdvancePatches(state, currentTurn, whilePresent: false);
        }

        internal bool AdvanceActiveStructureWork(string settlementId, int currentTurn)
        {
            var state = ExistingSettlement(settlementId);
            if (state == null) return false;
            bool completed = CompleteWeirContract(state, currentTurn, whilePresent: true);
            return AdvancePatches(state, currentTurn, whilePresent: true) || completed;
        }

        internal static bool IsWeirContract(string settlementId, string siteId)
            => settlementId == TineCompositionPlan.ZoneID && siteId == WeirSiteId;

        internal int? PendingWeirContract(string settlementId, string siteId)
            => IsWeirContract(settlementId, siteId) ? ExistingSettlement(settlementId)?.WeirContractDueTick : null;

        internal string WeirContractStatus(string settlementId, string siteId)
        {
            if (!(PendingWeirContract(settlementId, siteId) is int due)) return null;
            int day = due / WorldClock.DayLengthTicks + 1;
            string band = WorldClock.BandName(WorldClock.GetBand(due), 0);
            return $"Concord repair booked. Due on day {day} at {band}.";
        }

        internal string BookWeirContract(string settlementId, string siteId, Entity player, int fee)
        {
            var state = ExistingSettlement(settlementId);
            var site = state?.GetSite(siteId);
            if (!IsWeirContract(settlementId, siteId) || site == null) return "unknown_site";
            if (site.Stage >= RepairStage.StableRepair) return "already_repaired";
            if (state.WeirContractDueTick.HasValue)
            {
                MessageLog.Add(WeirContractStatus(settlementId, siteId));
                return ContractPending;
            }
            int now = GetCurrentTurn();
            if (now < 0 || now > int.MaxValue - WorldClock.DayLengthTicks) return "repair_refused";
            var payment = new InventoryTransaction();
            if (fee > 0 && !Ledger.StageDebit(player, fee, "concord-repair-contract", payment)) return "insufficient_drams";
            try
            {
                payment.Do(() => state.WeirContractDueTick = now + WorldClock.DayLengthTicks,
                    () => state.WeirContractDueTick = null);
                payment.Commit();
            }
            finally { payment.Rollback(); }
            MessageLog.Add(WeirContractStatus(settlementId, siteId));
            return null;
        }

        private bool CompleteWeirContract(SettlementState state, int currentTurn, bool whilePresent)
        {
            if (state.SettlementId != TineCompositionPlan.ZoneID
                || !(state.WeirContractDueTick is int due) || currentTurn < due) return false;
            var site = state.GetSite(WeirSiteId);
            if (site == null) return false;
            state.WeirContractDueTick = null;
            if (site.Stage >= RepairStage.StableRepair) return false;
            site.Stage = RepairStage.StableRepair;
            site.OutcomeTier = RepairOutcomeTier.Stable;
            site.ResolvedByMethod = RepairMethodId.Contracted;
            site.ResolvedAtTurn = due;
            site.RelapseAtTurn = null;
            state.SetCondition(ContractCondition(WeirSiteId), true);
            const string finished = "The Concord has finished Tine's weir. The new stakes hold.";
            if (whilePresent) MessageLog.Add(finished);
            else state.AddPendingMessage(finished);
            RaiseSiteChanged(state.SettlementId, site);
            return true;
        }

        bool AdvancePatches(SettlementState state, int currentTurn, bool whilePresent)
        {
            string settlementId = state.SettlementId;
            bool changed = false;
            foreach (var kvp in state.Sites)
            {
                RepairableSiteState site = kvp.Value;
                if ((!whilePresent || site.SiteType == RepairableSiteType.Structure)
                    && site.Stage == RepairStage.TemporarilyPurified
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
                        relapseMsg = PlaceSiteCatalog.Get(PlaceSiteCatalog.PlaceAt(settlementId), site.SiteId)?.Relapse ?? "Something you patched here has failed again.";
                    else if (site.SiteType == RepairableSiteType.HeatStone)
                        relapseMsg = "The oven's firebox has cracked open again while you were away.";
                    else if (site.SiteType == RepairableSiteType.LightBeacon)
                        relapseMsg = "The watch lantern has guttered out again while you were away.";
                    else
                        relapseMsg = "The village well has gone bitter again while you were away.";
                    if (whilePresent) MessageLog.Add(relapseMsg);
                    else state.AddPendingMessage(relapseMsg);
                    // A booked weir can lose its temporary patch without
                    // discarding Tine's cached occupants and finite stock.
                    if (whilePresent || !IsWeirContract(settlementId, site.SiteId) || !state.WeirContractDueTick.HasValue)
                        changed = true;
                    RaiseSiteChanged(settlementId, site);
                }
            }

            if (!whilePresent) state.LastAdvancedTurn = currentTurn;
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
                    if (IsWeirContract(settlementId, siteId)) return false;
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
            => RepairPlaceSite(settlementId, siteId, lasting, player, null);

        internal string RepairPlaceSite(string settlementId, string siteId, bool lasting, Entity player, InventoryTransaction transaction)
        {
            if (player == null) return "missing_player";
            var data = PlaceSiteCatalog.Get(PlaceSiteCatalog.PlaceAt(settlementId), siteId);
            if (data == null) return "unknown_site";
            PointOfInterest poi = ResolvePoi(settlementId);
            SettlementState state = GetOrCreateSettlement(settlementId, poi);
            RepairableSiteState site = state?.GetSite(siteId);
            if (site == null) return "unknown_site";
            if (PendingWeirContract(settlementId, siteId).HasValue)
            {
                MessageLog.Add(WeirContractStatus(settlementId, siteId));
                return ContractPending;
            }
            // One rule for "could this repair be made": the same check a world object for the site reads (PlaceSiteRepairs). Only the messages differ.
            string refusal = PlaceSiteRepairs.Check(data, site, lasting, player);
            if (refusal != null)
            {
                switch (refusal)
                {
                    case PlaceSiteRepairs.AlreadyRepaired: MessageLog.Add("The " + data.Name + " is already properly repaired."); break;
                    case PlaceSiteRepairs.AlreadyPatched: MessageLog.Add("The " + data.Name + " is patched already."); break;
                    case PlaceSiteRepairs.MissingMaterials: MessageLog.Add("You don't have what the " + data.Name + " needs."); break;
                    case PlaceSiteRepairs.MissingTool: MessageLog.Add("You need a tool for that."); break;
                }
                return refusal;
            }
            var fix = lasting ? data.Lasting : data.Patch;
            if (fix.HasSubstitutes)
            {
                var zone = player.SpatialZone;
                var cell = zone?.GetEntityCell(player);
                var stage = site.Stage;
                var world = WorldLocationContext.For(zone);
                if (zone?.ZoneID != settlementId || world?.SettlementManager != this || world.ActiveZone != zone
                    || !WorldResourceActions.ActorCurrent(player, zone)
                    || player.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "invalid_context";
                bool own = transaction == null;
                transaction ??= new InventoryTransaction();
                try
                {
                    if (!transaction.TryClaim(player, player, "PlaceSiteRepair")
                        || !PlaceSiteRepairs.TryPaySubstituteBill(player, zone, fix, transaction)) return "payment_refused";
                    transaction.BeforeCommit(() => WorldResourceActions.ActorCurrent(player, zone)
                        && player.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                        && zone.GetEntityCell(player) == cell && WorldLocationContext.For(zone) == world && world.ActiveZone == zone
                        && TryGetExistingSite(settlementId, siteId, out var current) && current == site && current.Stage == stage);
                    transaction.AfterCommit(() => ApplyPlaceRepair(state, site, settlementId, lasting, fix));
                    if (own) transaction.Commit();
                    return null;
                }
                finally { if (own) transaction.Rollback(); }
            }
            foreach (var m in fix.Materials)
                CavesOfOoo.Concerns.ConcernItems.TakeCarried(player, m.Blueprint, m.Count);
            ApplyPlaceRepair(state, site, settlementId, lasting, fix);
            return null;
        }

        private void ApplyPlaceRepair(SettlementState state, RepairableSiteState site, string settlementId, bool lasting, PlaceSiteFixData fix)
        {
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
        }

        /// <summary>A copy of a site's state, for an undo (an interrupted action that repaired it). Null when the site is unknown.</summary>
        public RepairableSiteState SnapshotSite(string settlementId, string siteId) => GetSite(settlementId, siteId)?.Clone();

        /// <summary>Puts a <see cref="SnapshotSite"/> copy back and tells the listeners, so a repair that an outer step then undid leaves the site, and what shows it, as it was.</summary>
        public void RestoreSite(string settlementId, RepairableSiteState snapshot)
        {
            var site = snapshot == null ? null : GetSite(settlementId, snapshot.SiteId); if (site == null) return;
            site.Stage = snapshot.Stage; site.Severity = snapshot.Severity; site.ProblemType = snapshot.ProblemType; site.ResolvedByMethod = snapshot.ResolvedByMethod;
            site.ResolvedAtTurn = snapshot.ResolvedAtTurn; site.RelapseAtTurn = snapshot.RelapseAtTurn; site.OutcomeTier = snapshot.OutcomeTier;
            RaiseSiteChanged(settlementId, site);
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

            bool changed = state.HasCondition(condition) != enabled;
            state.SetCondition(condition, enabled);
            if (!changed || string.IsNullOrEmpty(condition)) return;

            if (condition == QuillholdDeskLighting.Condition)
                QuillholdDeskLighting.InvalidateActive(this, settlementId);

            var active = SettlementRuntime.ActiveZone;
            if (active?.ZoneID != settlementId || WorldLocationContext.For(active)?.SettlementManager != this) return;
            foreach (var owner in active.GetReadOnlyEntitiesWithPart<PlaceConditionLightPart>())
            {
                var binding = owner.GetPart<PlaceConditionLightPart>();
                if (binding.Condition != condition || !binding.IsInstalled(active)) continue;
                SettlementRuntime.MarkZoneDirty();
                break;
            }
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
