using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A world object for one of a place's data-defined working structures (Content/Data/PlaceSites.json): the mine pump at Cinderhold, and any weir, scales or vat that later wants a body
    /// in its town. The Part owns nothing. The repair state lives where it always has (the settlement manager's <see cref="RepairableSiteState"/>), the repair rules are
    /// <see cref="PlaceSiteRepairs"/>, and the repair itself is <see cref="SettlementManager.RepairPlaceSite"/>, so the elder's "[Repair the pump.]" in conversation and this object's rows are
    /// two doors to one room: a Restore approach finishes the same way whichever door the player used, and neither path is taken away.
    /// <para>What the object adds is the world's side of it: it reads as sheared, patched or running; examining it says what happened to it; its interact menu says what each repair needs and
    /// what you carry of it, offers "mend for good" and "patch for now" when they could be made and says what is missing when they could not; and a repair is one action that takes the
    /// materials only at commit, so a refused or interrupted action takes nothing.</para>
    /// </summary>
    public sealed class PlaceSiteObjectPart : Part, IDescribeState, IMenuStatus, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "PlaceSiteObject";

        /// <summary>The authored place and its site id (PlaceSites.json). Set by the placement that puts the object in the town; saved.</summary>
        public string Place = "", SiteId = "";
        /// <summary>How the object's state is worded: set in the blueprint ("sheared", "running" for a pump), plain words by default.</summary>
        public string BrokenWord = "broken", PatchedWord = "patched", MendedWord = "mended";
        /// <summary>True when the thing is named in the plural ("pan scales"), so its state reads "are" and not "is". Set in the blueprint; saved; false in an older save.</summary>
        public bool Plural;

        public const string RepairPrefix = "PlaceSiteRepair|", LastingToken = "Lasting", PatchToken = "Patch";
        const int StatusWidth = 44;

        // Looked up once per object (not saved): the catalog row and the place's zone id do not change; the state is read live each time.
        PlaceSiteData _data; string _zoneId, _dataKey;
        PlaceSiteData Data
        {
            get
            {
                string key = Place + "|" + SiteId;
                if (_data == null || _dataKey != key) { _data = PlaceSiteCatalog.Get(Place, SiteId); _zoneId = PlaceSites.ZoneIdOf(Place); _dataKey = key; }
                return _data;
            }
        }
        RepairableSiteState Site => Data == null || _zoneId == null || SettlementManager.Current == null ? null : SettlementManager.Current.GetSite(_zoneId, SiteId);
        string ContractStatus => SettlementManager.Current?.WeirContractStatus(_zoneId, SiteId);

        public static bool IsCommand(string command) => command != null && command.StartsWith(RepairPrefix, StringComparison.Ordinal);

        public bool OwnsInventoryReceipt(string command) => Data?.OffersShelteredRest == true && IsCommand(command);

        // ── the world's side: the object reads as what happened to it ──

        string StateWord(RepairableSiteState site)
            => site.Stage >= RepairStage.StableRepair ? MendedWord : site.Stage == RepairStage.TemporarilyPurified ? PatchedWord : BrokenWord;

        /// <summary>"about 420 more turns" for a patch, or empty when the site is not patched or has no recorded failure turn.</summary>
        string Remaining(RepairableSiteState site)
        {
            if (site.Stage != RepairStage.TemporarilyPurified || site.RelapseAtTurn == null || SettlementManager.Current == null) return "";
            int left = site.RelapseAtTurn.Value - SettlementManager.Current.GetCurrentTurn();
            return left <= 0 ? "" : "about " + left + " more turns";
        }

        // ── readers ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 20;

        public string DescribeState(DescribeContext context)
        {
            var data = Data; var site = Site; if (data == null || site == null) return null;
            if (site.Stage >= RepairStage.StableRepair) return "Mended, and working.";
            if (ContractStatus is string booked) return booked;
            if (site.Stage == RepairStage.TemporarilyPurified) { string left = Remaining(site); return "Patched for now" + (left == "" ? "." : ": it holds " + left + ", then fails.") + (context.Full ? " " + Advice(data, site, context.Viewer) : ""); }
            return data.Broken + (context.Full ? " " + Advice(data, site, context.Viewer) : "");
        }

        internal string ExamineRepairRequirements()
        {
            var data = Data;
            var zone = ParentEntity?.SpatialZone;
            var world = WorldLocationContext.For(zone);
            if (data == null || zone?.ZoneID != _zoneId || world == null
                || !world.CachedZones.TryGetValue(_zoneId, out var current) || current != zone
                || !world.SettlementManager.TryGetExistingSite(_zoneId, SiteId, out var site)
                || site.Stage >= RepairStage.StableRepair || ContractStatus != null) return null;

            string left = Remaining(site);
            var lines = new List<string>
            {
                site.Stage == RepairStage.Fouled ? data.Broken
                    : "Patched for now" + (left == "" ? "." : " for " + left + "."),
                "", "Lasting repair"
            };
            AddRepairRequirements(lines, data.Lasting);
            if (data.HasPatch && site.Stage == RepairStage.Fouled)
            {
                lines.Add("");
                lines.Add("Temporary patch (about " + data.Patch.RelapseTurns + " turns)");
                AddRepairRequirements(lines, data.Patch);
            }
            return string.Join("\n", lines);
        }

        private static void AddRepairRequirements(List<string> lines, PlaceSiteFixData fix)
        {
            foreach (var material in fix.Materials)
                lines.Add("- " + material.Count + " " + PlaceSiteRepairs.MaterialName(material) + " (used)");
            lines.Add("Tools and machines");
            lines.Add(string.IsNullOrEmpty(fix.Tool) ? "None required." : "- " + PlaceSiteRepairs.NameOf(fix.Tool) + " (kept)");
        }

        /// <summary>How to mend it and what that takes, from the site's own repair data (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s rows, and a refused repair names what is missing.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            var data = Data; var site = Site; if (data == null || site == null) return;
            if (ContractStatus != null) return;
            if (site.Stage >= RepairStage.StableRepair)
            {
                if (!data.OffersShelteredRest) guide.Limits.Add("It is mended and working; there is nothing more to do to it.");
                return;
            }
            guide.How.Add("Stand beside it, press C and choose mend the " + data.Name + " for good" + (data.HasPatch && site.Stage == RepairStage.Fouled ? ", or patch it for now" : "") + ". The menu lists each material with how many you carry.");
            guide.Needs.Add("to mend it for good: " + PlaceSiteRepairs.NeedsPlain(data.Lasting));
            if (data.HasPatch && site.Stage == RepairStage.Fouled) guide.Needs.Add("to patch it for now: " + PlaceSiteRepairs.NeedsPlain(data.Patch));
            guide.Limits.Add("Mending uses up the materials and keeps any tool, and the repair lasts." + (data.HasPatch && site.Stage == RepairStage.Fouled ? " A patch holds about " + data.Patch.RelapseTurns + " turns, then fails." : ""));
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>What the viewer could do about it, from what they carry: the full reader's last sentence.</summary>
        static string Advice(PlaceSiteData data, RepairableSiteState site, Entity viewer)
        {
            if (viewer == null) return "To mend it for good it needs " + PlaceSiteRepairs.NeedsPlain(data.Lasting) + ".";
            if (PlaceSiteRepairs.Check(data, site, true, viewer) == null) return "You carry what it takes to mend it for good.";
            if (data.HasPatch && site.Stage == RepairStage.Fouled && PlaceSiteRepairs.Check(data, site, false, viewer) == null) return "You carry what it takes to patch it for now.";
            return "To mend it for good it needs " + PlaceSiteRepairs.NeedsPlain(data.Lasting) + (data.HasPatch && site.Stage == RepairStage.Fouled ? "; to patch it for now, " + PlaceSiteRepairs.NeedsPlain(data.Patch) : "") + ".";
        }

        /// <summary>The interact menu's status block: the state, and for each repair the materials with what the viewer carries ("iron spike 1/2": carried over needed). Each line fits the narrowest menu.</summary>
        public void AppendMenuStatus(DescribeContext context, List<string> lines)
        {
            var data = Data; var site = Site; if (data == null || site == null || context.Viewer == null) return;
            if (ContractStatus is string booked) { lines.Add(booked); return; }
            string left = Remaining(site);
            lines.Add("The " + data.Name + (Plural ? " are " : " is ") + StateWord(site) + (left == "" ? "." : " (" + left + ")."));
            if (site.Stage >= RepairStage.StableRepair) return;
            AddFix(lines, "Mend for good: ", data.Lasting, context.Viewer);
            if (data.HasPatch && site.Stage == RepairStage.Fouled) AddFix(lines, "Patch (" + data.Patch.RelapseTurns + " turns): ", data.Patch, context.Viewer);
        }

        static void AddFix(List<string> lines, string label, PlaceSiteFixData fix, Entity viewer)
        {
            var items = new List<string>();
            foreach (var m in fix.Materials) items.Add(PlaceSiteRepairs.MaterialName(m) + " " + PlaceSiteRepairs.CarriedAmount(viewer, m) + "/" + m.Count);
            if (!string.IsNullOrEmpty(fix.Tool)) items.Add(PlaceSiteRepairs.NameOf(fix.Tool) + " " + Math.Min(1, ConcernItems.CountCarried(viewer, fix.Tool)) + "/1 (kept)");
            // Greedy wrap at the narrowest menu's width, never splitting one item; continuation lines are indented.
            const string pad = "  "; string current = label;
            for (int i = 0; i < items.Count; i++)
            {
                string piece = items[i] + (i + 1 < items.Count ? "," : "");
                bool atStart = current == label || current == pad;
                string candidate = current + (atStart ? "" : " ") + piece;
                if (candidate.Length > StatusWidth && !atStart) { lines.Add(current); current = pad + piece; }
                else current = candidate;
            }
            lines.Add(current);
        }

        // ── rows ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command"); if (!IsCommand(command)) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (TryRepair(actor, zone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
                return true;
            }
            if (e.ID == "Render") { Look(); return true; }
            return true;
        }

        /// <summary>The object's own name and colour follow its state each time it is drawn (cheap: two string reads), the way the well's do.</summary>
        void Look()
        {
            var render = ParentEntity?.GetPart<RenderPart>(); var data = Data; var site = Site; if (render == null || data == null || site == null) return;
            render.DisplayName = data.Name + " (" + StateWord(site) + ")";
            render.ColorString = site.Stage >= RepairStage.StableRepair ? (data.OffersShelteredRest ? "&Y" : "&c")
                : site.Stage == RepairStage.TemporarilyPurified ? "&W" : "&y";
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            var data = Data; var site = Site; var owner = ParentEntity;
            if (actions == null || actor == null || zone == null || data == null || site == null || owner == null || !WorldResourceActions.Nearby(actor, owner, zone)) return;
            if (site.Stage >= RepairStage.StableRepair || SettlementManager.Current?.PendingWeirContract(_zoneId, SiteId).HasValue == true) return;
            AddRow(actions, actor, data, site, true, "PlaceSiteMend", "mend the " + data.Name + " for good", 'm', 20,
                "Uses up the materials and keeps any tool. The repair lasts.");
            if (data.HasPatch && site.Stage == RepairStage.Fouled)
                AddRow(actions, actor, data, site, false, "PlaceSitePatch", "patch the " + data.Name + " for now", 'p', 19,
                    "Uses up the patch materials. It holds about " + data.Patch.RelapseTurns + " turns, then fails.");
        }

        static void AddRow(InventoryActionList actions, Entity actor, PlaceSiteData data, RepairableSiteState site, bool lasting, string name, string label, char key, int priority, string hint)
        {
            string refusal = PlaceSiteRepairs.Check(data, site, lasting, actor);
            string token = lasting ? LastingToken : PatchToken;
            if (refusal == null) { actions.AddActionWithHint(name, label, RepairPrefix + token, hint, key, priority, false, ActionTime.OneAction); return; }
            string why = PlaceSiteRepairs.Missing(lasting ? data.Lasting : data.Patch, actor);
            actions.AddDisabled(name, label, refusal, why == "" ? "That cannot be done right now." : why, key, priority);
        }

        // ── the repair: one action; the materials are taken at commit by the same call the elder's conversation makes ──

        bool TryRepair(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var data = Data; var site = Site; var owner = ParentEntity; string[] f = command.Split('|');
            if (actor == null || zone == null || data == null || site == null || owner == null || f.Length != 2 || (f[1] != LastingToken && f[1] != PatchToken) || !WorldResourceActions.Nearby(actor, owner, zone))
                return Reject(actor, "invalid_context", "You cannot work on that right now.");
            if (ContractStatus is string booked) return Reject(actor, SettlementManager.ContractPending, booked);
            bool lasting = f[1] == LastingToken; string refusal = PlaceSiteRepairs.Check(data, site, lasting, actor);
            if (refusal != null) return Reject(actor, refusal, refusal == PlaceSiteRepairs.MissingMaterials || refusal == PlaceSiteRepairs.MissingTool
                ? PlaceSiteRepairs.Missing(lasting ? data.Lasting : data.Patch, actor) : refusal == PlaceSiteRepairs.AlreadyRepaired ? "The " + data.Name + " is already properly repaired." : "That cannot be done right now.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return Reject(actor, "in_progress", "That is already being worked on.");
                var manager = SettlementManager.Current; var pack = actor.GetPart<InventoryPart>(); if (manager == null || pack == null || _zoneId == null) return Reject(actor, "invalid_context", "You cannot work on that right now.");
                if ((lasting ? data.Lasting : data.Patch).HasSubstitutes)
                {
                    var cell = zone.GetEntityCell(owner);
                    tx.BeforeCommit(() => manager == SettlementManager.Current && owner.GetPart<PlaceSiteObjectPart>() == this
                        && WorldResourceActions.Nearby(actor, owner, zone) && zone.GetEntityCell(owner) == cell && cell.IsVisible
                        && (!data.OffersShelteredRest || RestoredPlaceRestPart.GantryPole(zone) == owner));
                    if (data.OffersShelteredRest && RestoredPlaceRestPart.GantryPole(zone) != owner) return false;
                    string paid = manager.RepairPlaceSite(_zoneId, SiteId, lasting, actor, tx);
                    if (paid != null) return Reject(actor, paid, "The materials could not be used to mend it.");
                    if (own) tx.Commit();
                    return true;
                }
                // The same call the elder's conversation makes does the work, inside this action: the pack and the site are captured first, so an outer step that fails (or the repair itself
                // refusing) puts the materials and the site back exactly. The announcement it shows is the only thing not taken back.
                var siteBefore = manager.SnapshotSite(_zoneId, SiteId); var receipt = InventoryTransferSnapshot.Capture(pack); string zoneId = _zoneId, siteId = SiteId;
                tx.Do(null, () => { receipt.Restore(); manager.RestoreSite(zoneId, siteBefore); });
                string result = null;
                if (!receipt.Apply(() => (result = manager.RepairPlaceSite(zoneId, siteId, lasting, actor)) == null) || !receipt.ClaimChanges(tx, actor, command))
                { CavesOfOoo.Diagnostics.Diag.Record("furniture", "SiteRepairRefused", actor: actor, target: owner, payload: new { place = Place, site = SiteId, code = result ?? "payment_refused" }); return false; }
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Reject(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            CavesOfOoo.Diagnostics.Diag.Record("furniture", "SiteRepairRefused", actor: actor, target: ParentEntity, payload: new { place = Place, site = SiteId, code });
            return false;
        }
    }

    /// <summary>Two salvageable spares beside the original line. The working structure stays in place.</summary>
    public sealed class SurveyLineSpareStakesPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "SurveyLineSpareStakes";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 32;
        internal override bool RetainsFixedSource => true;

        bool CurrentSource(Zone zone)
        {
            var owner = ParentEntity;
            var world = WorldLocationContext.For(zone);
            return zone?.ZoneID == DrownedLedgerCompositionPlan.ZoneID && world?.ActiveZone == zone
                && world.Factory == Factory && CanCreateYield(Factory, YieldBlueprint)
                && world.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone
                && owner?.BlueprintName == "SurveyLine" && owner.GetProperty("SettlementId") == zone.ZoneID
                && owner.GetPart<InstallationMarkerPart>() is { ParentEntity: var marked, InstallationId: "ledger-survey-line", Origin: "Generated", RetainZone: true, Revision: 1 }
                && marked == owner && owner.GetPart<PlaceSiteObjectPart>() is { Place: "the Drowned Ledger", SiteId: "SurveyLine" }
                && WorldResourceActions.Ground(owner, zone) && owner.GetPart<PhysicsPart>() is { Takeable: false, Solid: false }
                && owner.GetPart<RenderPart>() is { Visible: true }
                && owner.GetPart<HarvestablePart>() == this && owner.Parts.Count(part => part is HarvestablePart) == 1
                && zone.GetReadOnlyEntitiesWithPart<SurveyLineSpareStakesPart>().Count == 1
                && YieldBlueprint == "WillowHaftComponent" && YieldMin == 2 && YieldMax == 2 && YieldChance == 100;
        }

        public string DescribeState(DescribeContext context)
            => !CurrentSource(context.Zone ?? ParentEntity?.SpatialZone) ? null : Harvested
                ? "The spare stakes beside the line have been taken."
                : "Two spare willow stakes lie beside the cord. Take them to patch the line for a while.";

        public bool OwnsInventoryReceipt(string command)
            => command == "Harvest" && ParentEntity?.GetPart<HarvestablePart>() == this;

        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);

        internal override bool CanGatherFixedSource(Entity actor, Zone zone)
            => CurrentSource(zone) && RouteWorkMenu.ActorReady(actor, zone)
                && WorldResourceActions.Nearby(actor, ParentEntity, zone) && RouteWorkMenu.Visible(zone.GetEntityCell(ParentEntity));

        internal override Func<bool> CaptureFixedGathering(Entity actor, Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            var stillSame = RouteWorkMenu.Identity(owner);
            var origin = zone.GetEntityCell(actor);
            return () => Harvested && ParentEntity == owner && stillSame()
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(owner) == cell
                && CanGatherFixedSource(actor, zone);
        }
    }
}
