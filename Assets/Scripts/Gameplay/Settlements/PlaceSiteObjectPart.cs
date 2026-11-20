using System;
using System.Collections.Generic;
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
    public sealed class PlaceSiteObjectPart : Part, IDescribeState, IMenuStatus
    {
        public override string Name => "PlaceSiteObject";

        /// <summary>The authored place and its site id (PlaceSites.json). Set by the placement that puts the object in the town; saved.</summary>
        public string Place = "", SiteId = "";
        /// <summary>How the object's state is worded: set in the blueprint ("sheared", "running" for a pump), plain words by default.</summary>
        public string BrokenWord = "broken", PatchedWord = "patched", MendedWord = "mended";

        public const string RepairPrefix = "PlaceSiteRepair|", LastingToken = "Lasting", PatchToken = "Patch";
        const int StatusWidth = 44;

        PlaceSiteData Data => PlaceSiteCatalog.Get(Place, SiteId);
        RepairableSiteState Site => PlaceSites.Site(Place, SiteId);

        public static bool IsCommand(string command) => command != null && command.StartsWith(RepairPrefix, StringComparison.Ordinal);

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
            if (site.Stage == RepairStage.TemporarilyPurified) { string left = Remaining(site); return "Patched for now" + (left == "" ? "." : ": it holds " + left + ", then fails.") + (context.Full ? " " + Advice(data, site, context.Viewer) : ""); }
            return data.Broken + (context.Full ? " " + Advice(data, site, context.Viewer) : "");
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
            string left = Remaining(site);
            lines.Add("The " + data.Name + " is " + StateWord(site) + (left == "" ? "." : " (" + left + ")."));
            if (site.Stage >= RepairStage.StableRepair) return;
            AddFix(lines, "Mend for good: ", data.Lasting, context.Viewer);
            if (data.HasPatch && site.Stage == RepairStage.Fouled) AddFix(lines, "Patch (" + data.Patch.RelapseTurns + " turns): ", data.Patch, context.Viewer);
        }

        static void AddFix(List<string> lines, string label, PlaceSiteFixData fix, Entity viewer)
        {
            var items = new List<string>();
            foreach (var m in fix.Materials) items.Add(PlaceSiteRepairs.NameOf(m.Blueprint) + " " + ConcernItems.CountCarried(viewer, m.Blueprint) + "/" + m.Count);
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
            if (e.ID == "EndTurn") { Note(); return true; }
            return true;
        }

        bool _noted;   // not saved: once per visit, like the well's proximity line

        /// <summary>The first time the player stands beside it in a visit, the pump says what it is doing (placeholder wording pending voice review).</summary>
        void Note()
        {
            if (_noted) return;
            var zone = ParentEntity?.SpatialZone; var site = Site; var at = zone?.GetEntityCell(ParentEntity); if (zone == null || site == null || at == null) return;
            Entity player = null; var all = zone.GetAllEntities(); for (int i = 0; i < all.Count; i++) if (all[i].HasTag("Player")) { player = all[i]; break; }
            var near = player == null ? null : zone.GetEntityCell(player); if (near == null || Math.Abs(near.X - at.X) > 1 || Math.Abs(near.Y - at.Y) > 1) return;
            _noted = true;
            MessageLog.Add(site.Stage >= RepairStage.StableRepair ? "The pump thuds on, steady, and the shaft below stays dry."
                : site.Stage == RepairStage.TemporarilyPurified ? "The patched pump coughs and keeps working, for now."
                : "The pump's rod hangs slack. Below it, water slaps against stone.");
        }

        /// <summary>The object's own name and colour follow its state each time it is drawn (cheap: two string reads), the way the well's do.</summary>
        void Look()
        {
            var render = ParentEntity?.GetPart<RenderPart>(); var data = Data; var site = Site; if (render == null || data == null || site == null) return;
            render.DisplayName = data.Name + " (" + StateWord(site) + ")";
            render.ColorString = site.Stage >= RepairStage.StableRepair ? "&c" : site.Stage == RepairStage.TemporarilyPurified ? "&W" : "&y";
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            var data = Data; var site = Site; var owner = ParentEntity;
            if (actions == null || actor == null || zone == null || data == null || site == null || owner == null || !WorldResourceActions.Nearby(actor, owner, zone)) return;
            if (site.Stage >= RepairStage.StableRepair) return;
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
            bool lasting = f[1] == LastingToken; string refusal = PlaceSiteRepairs.Check(data, site, lasting, actor);
            if (refusal != null) return Reject(actor, refusal, refusal == PlaceSiteRepairs.MissingMaterials || refusal == PlaceSiteRepairs.MissingTool
                ? PlaceSiteRepairs.Missing(lasting ? data.Lasting : data.Patch, actor) : refusal == PlaceSiteRepairs.AlreadyRepaired ? "The " + data.Name + " is already properly repaired." : "That cannot be done right now.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return Reject(actor, "in_progress", "That is already being worked on.");
                string zoneId = PlaceSites.ZoneIdOf(Place);
                // Nothing is taken until the whole action commits: an outer step that fails leaves the materials in the pack and the site as it was.
                tx.AfterCommit(() => SettlementManager.Current?.RepairPlaceSite(zoneId, SiteId, lasting, actor));
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
}
