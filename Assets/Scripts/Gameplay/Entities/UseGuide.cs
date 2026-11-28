using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What one machine or tool tells the player about using it. The owner (2026-11-21): a description should say exactly how the thing works, how to use it, whether it can be used at
    /// this moment, and what materials or surroundings it needs. A Part fills the three static lists from its own live constants (so a number in the text can never drift from the rule
    /// that enforces it); the collector adds the live "right now" line from the rows the thing's own Parts would show in its menus.
    /// </summary>
    public sealed class UseGuide
    {
        /// <summary>Plain imperative sentences, in the order a player would do them: "Press C beside a steam vent and choose scrape."</summary>
        public readonly List<string> How = new List<string>();
        /// <summary>What must be true or carried, each a short phrase joined with semicolons: "a steam vent or ash bed within one step".</summary>
        public readonly List<string> Needs = new List<string>();
        /// <summary>What it costs or risks, as sentences: finite supply, noise, time, what it refuses.</summary>
        public readonly List<string> Limits = new List<string>();
        /// <summary>Why the thing offers nothing at this moment, said only when none of its Parts shows a row (a vent is not near; the ground is wrong). Computed by the Part from the same rule that withholds the row.
        /// The first reason given stands, so the Part that owns the broadest condition (a kit still being carried) is not overruled by a machine's narrower one.</summary>
        public string WhyNot { get => _whyNot; set { if (_whyNot == null && !string.IsNullOrWhiteSpace(value)) _whyNot = value; } }
        string _whyNot;

        public bool HasContent => How.Count > 0 || Needs.Count > 0 || Limits.Count > 0;
    }

    /// <summary>
    /// Implemented by the Part that owns a machine's or tool's verbs: it describes how to use them. Its own menu rows (the <c>GetInventoryActions</c> it answers, or the rows it states through
    /// <see cref="IDescribeUseRows"/> when its menu lists rows that are always there but only sometimes work) are what the "right now" line is built from, so the line says what the menu would offer and why a
    /// refused row is refused. Describing is read-only: no state change, no diag record.
    /// </summary>
    public interface IDescribeUse
    {
        /// <summary>Position among the thing's Parts; ties keep Part insertion order.</summary>
        int Order { get; }
        void DescribeUse(DescribeContext context, UseGuide guide);
    }

    /// <summary>
    /// For a Part that states its rows for describing itself instead of letting its menu handler answer: the collector asks THIS for the rows and does not fire the Part's <c>GetInventoryActions</c> at all.
    /// A Part uses it when its menu lists rows that are always there but only sometimes work (a forge's Craft with nothing picked, a skin's drink when empty), so the "right now" line says what is true, with the
    /// reason as a disabled row, and when its rows come from a shared service rather than its own handler.
    /// </summary>
    public interface IDescribeUseRows
    {
        void AddUseRows(Entity actor, Zone zone, InventoryActionList rows);
    }

    /// <summary>One collector for both Examine readers. A throwing describer is skipped and recorded, never allowed to hide the others.</summary>
    public static class UseGuides
    {
        public const string HowLabel = "How to use: ", NeedsLabel = "Needs: ", LimitsLabel = "Cost and limits: ", NowCan = "Right now you can: ", NowCannot = "Right now you cannot: ", NowNot = " Not now: ", NowReason = "Right now: ";
        /// <summary>The heading of the full reader's section.</summary>
        public const string Heading = "How to use";
        const int ShownRows = 4;
        /// <summary>What a carried tool says when it lies on the ground or in someone else's keeping: its rows are the carrier's, so nothing is offered until it is picked up.</summary>
        public const string PickUpFirst = "you are not carrying it: pick it up first";
        /// <summary>Whether <paramref name="item"/> is in the keeping of <paramref name="viewer"/> (in the pack or worn), the thing a carried tool's rows require.</summary>
        public static bool CarriedBy(Entity viewer, Entity item)
        {
            var physics = item?.GetPart<PhysicsPart>(); return viewer != null && physics != null && (physics.InInventory == viewer || physics.Equipped == viewer);
        }

        /// <summary>
        /// Adds the guide's lines to <paramref name="into"/> (how, needs, limits, then the live line) and returns whether the entity describes its use at all. A carried thing is asked about its carrier;
        /// pass the viewer when the reader knows who is looking. With no viewer there is no "right now" line, only the facts.
        /// </summary>
        public static bool Collect(Entity owner, Zone zone, Cell cell, Entity viewer, List<string> into)
        {
            if (owner == null || into == null) return false;
            var parts = owner.Parts; var users = new List<(int order, int index, IDescribeUse part)>();
            for (int i = 0; i < parts.Count; i++) if (parts[i] is IDescribeUse use) users.Add((SafeOrder(use), i, use));
            if (users.Count == 0 && owner.BlueprintName != "GroveRedGrowth") return false;
            users.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.index.CompareTo(b.index));
            viewer = viewer ?? CarrierOf(owner);
            zone = zone ?? owner.SpatialZone ?? viewer?.SpatialZone;
            cell = cell ?? (zone != null ? zone.GetEntityCell(owner) ?? zone.GetEntityCell(viewer) : null);
            var context = new DescribeContext { Owner = owner, Zone = zone, Cell = cell, Viewer = viewer, Full = false };
            var guide = new UseGuide();
            AddGroveRedGuide(context, guide);
            for (int i = 0; i < users.Count; i++)
            {
                try { users[i].part.DescribeUse(context, guide); }
                catch (Exception error)
                {
                    if (Diag.IsChannelEnabled("event")) Diag.Record("event", "DescribeUseFailed", target: owner, payload: new { part = users[i].part.GetType().Name, error = error.GetType().Name, message = error.Message });
                }
            }
            if (!guide.HasContent) return false;
            if (guide.How.Count > 0) into.Add(HowLabel + string.Join(" ", guide.How));
            if (guide.Needs.Count > 0) into.Add(NeedsLabel + Sentence(string.Join("; ", guide.Needs)));
            if (guide.Limits.Count > 0) into.Add(LimitsLabel + string.Join(" ", guide.Limits));
            if (users.Count > 0 && viewer != null && zone != null)
            {
                // A machine standing in the world answers only to someone beside it: from further off its menu cannot be opened, so say that and nothing more.
                var now = StandingFarFrom(owner, viewer, zone) ? NowReason + "you are not beside it." : RightNow(OwnRows(users, viewer, zone), guide.WhyNot);
                if (now != null) into.Add(now);
            }
            return true;
        }

        static void AddGroveRedGuide(DescribeContext context, UseGuide guide)
        {
            var owner = context.Owner;
            if (owner.BlueprintName != "GroveRedGrowth"
                || !CellStatusReadout.IsDisclosedCell(context.Zone, context.Cell)
                || !WorldResourceActions.Ground(owner, context.Zone)
                || context.Zone.GetEntityCell(owner) != context.Cell
                || owner.GetPart<RenderPart>() is not { Visible: true } render || render.ParentEntity != owner)
                return;

            var harvest = owner.GetPart<HarvestablePart>();
            if (harvest == null || harvest.GetType() != typeof(HarvestablePart) || harvest.ParentEntity != owner
                || harvest.Harvested || harvest.YieldBlueprint != "GroveRed"
                || harvest.YieldMin < 1 || harvest.YieldMax < harvest.YieldMin
                || harvest.YieldMax > HarvestablePart.MaximumYieldCount || harvest.YieldChance is < 1 or > 100)
                return;

            guide.How.Add("At an alchemy still, cleanse one harvested grove-red into pulp. Brew the pulp by itself for a mending tonic.");
        }

        /// <summary>The Examine document's section text for the same block, or null when the entity describes no use.</summary>
        public static string SectionText(Entity owner, Zone zone, Cell cell, Entity viewer)
        {
            var lines = new List<string>(); if (!Collect(owner, zone, cell, viewer, lines)) return null;
            // The section's heading already says "How to use", so its first line does not repeat the label.
            if (lines.Count > 0 && lines[0].StartsWith(HowLabel, StringComparison.Ordinal)) lines[0] = lines[0].Substring(HowLabel.Length);
            return string.Join("\n", lines);
        }

        /// <summary>Who is carrying or wearing it, when that is the player: a thing lying in a chest, or in a merchant's stock being previewed, is not being asked about by the chest or the merchant.</summary>
        static Entity CarrierOf(Entity owner)
        {
            var physics = owner.GetPart<PhysicsPart>(); var carrier = physics?.InInventory ?? physics?.Equipped;
            return carrier != null && carrier.HasTag("Player") ? carrier : null;
        }

        /// <summary>The Part's order, or zero if it throws: describing must never be stopped by one Part.</summary>
        static int SafeOrder(IDescribeUse use) { try { return use.Order; } catch (Exception) { return 0; } }

        /// <summary>A thing standing (or lying) in the world with the viewer more than one step from it. A carried thing is never far from its carrier.</summary>
        static bool StandingFarFrom(Entity owner, Entity viewer, Zone zone)
            => owner.SpatialZone != null && viewer != owner && CarrierOf(owner) == null && SpatialQuery.Distance(zone, viewer, owner) > 1;

        /// <summary>The rows the thing's own describing Parts would show this viewer, built the way the menus build them: the same event with an actor and an action list, answered by each Part alone.</summary>
        static List<InventoryAction> OwnRows(List<(int order, int index, IDescribeUse part)> users, Entity viewer, Zone zone)
        {
            var list = new InventoryActionList();
            for (int i = 0; i < users.Count; i++)
            {
                if (!(users[i].part is IDescribeUseRows) && users[i].part is Part part)
                {
                    var e = GameEvent.New("GetInventoryActions");
                    try { e.SetParameter("Actions", (object)list); e.SetParameter("Actor", (object)viewer); part.HandleEvent(e); }
                    catch (Exception error)
                    {
                        if (Diag.IsChannelEnabled("event")) Diag.Record("event", "DescribeUseRowsFailed", target: part.ParentEntity, payload: new { part = part.GetType().Name, error = error.GetType().Name, message = error.Message });
                    }
                    finally { e.Release(); }
                }
                if (users[i].part is IDescribeUseRows source)
                {
                    try { source.AddUseRows(viewer, zone, list); }
                    catch (Exception error)
                    {
                        if (Diag.IsChannelEnabled("event")) Diag.Record("event", "DescribeUseRowsFailed", payload: new { part = source.GetType().Name, error = error.GetType().Name, message = error.Message });
                    }
                }
            }
            return list.Actions;
        }

        /// <summary>
        /// What the rows offer, in the menu's own words: each enabled row by its label, except that a crowd of rows under one heading (<see cref="WorldInteractionSystem.ToolGroupAt"/> or more: the eight cells a kit
        /// could be set down on) is said as the heading and the count, the way the interact menu shows it. Inert header rows are left out, and so is any row that only repeats another.
        /// </summary>
        static string RightNow(List<InventoryAction> rows, string whyNot)
        {
            var can = new List<string>(); var cannot = new List<string>(); var crowd = new Dictionary<string, int>(); var said = new HashSet<string>();
            foreach (var row in rows) if (row != null && !row.IsDisabled && row.Command != InventoryAction.InertCommand && !string.IsNullOrWhiteSpace(row.Display) && !string.IsNullOrEmpty(row.Group))
                crowd[row.Group] = crowd.TryGetValue(row.Group, out int n) ? n + 1 : 1;
            foreach (var row in rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Display) || row.Command == InventoryAction.InertCommand) continue;
                if (row.IsDisabled) { string line = row.Display + " (" + Trim(row.DisabledReason) + ")"; if (!cannot.Contains(line)) cannot.Add(line); continue; }
                string said1 = !string.IsNullOrEmpty(row.Group) && crowd[row.Group] >= WorldInteractionSystem.ToolGroupAt ? row.Group + " (" + crowd[row.Group] + ")" : row.Display;
                if (said.Add(said1)) can.Add(said1);
            }
            var text = new StringBuilder();
            if (can.Count > 0)
            {
                text.Append(NowCan).Append(Summarise(can)).Append('.');
                if (cannot.Count > 0) text.Append(NowNot).Append(Summarise(cannot)).Append('.');
                return text.ToString();
            }
            if (cannot.Count > 0) return text.Append(NowCannot).Append(Summarise(cannot)).Append('.').ToString();
            return string.IsNullOrWhiteSpace(whyNot) ? null : NowReason + Sentence(whyNot);
        }

        static string Summarise(List<string> items)
            => items.Count <= ShownRows ? string.Join(", ", items) : string.Join(", ", items.GetRange(0, ShownRows)) + " and " + (items.Count - ShownRows) + " more";

        static string Trim(string text) => (text ?? "").Trim().TrimEnd('.');
        static string Sentence(string text) { text = (text ?? "").Trim(); return text.Length == 0 || text.EndsWith(".") || text.EndsWith("!") || text.EndsWith("?") ? text : text + "."; }
    }
}
