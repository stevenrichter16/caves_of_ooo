using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Sorters' frame (Docs/MACHINE-INVENTED-USES.md 3s): a tended installation of six places, one for each hue, that the Bower-Folk of Posy keep. The Bower's regard goes by the eye, not by money ("a worthless thing in perfect
    /// arrangement outshines a precious thing thrown down anyhow", Lore/Factions/05_BowerFolk.md), so what it wants is a found thing that is only itself: a carried thing of a real hue, set in the place for that hue.
    /// A bright piece (a bright colour code) is worth two and a dim one is worth one; a plain grey, white or black is worth nothing; a piece worth much in drams is not taken at all. A place can be bettered once, from dim to bright.
    /// What is set in is the frame's for good (nothing is taken back out: damaging an installation is the Bower-Folk's primary crime). Each point is a small, one-time, non-tradable regard from the Bower-Folk
    /// (<see cref="PlayerReputation"/>), bounded by the six places, so it can be earned once and never farmed, and it mints no money.
    /// </summary>
    public sealed class SortingFramePart : Part, IDescribeState
    {
        public override string Name => "SortingFrame";

        public const string Prefix = "SortPiece|", Faction = "BowerFolk";
        /// <summary>Drams above which a thing is "precious" and not taken; regard per point; the one-time regard for a frame with every place full and bright; rows shown.</summary>
        public const int CheapLimit = 15, RegardPerPoint = 2, WholeRegard = 4, MaxRows = 8;
        public static readonly string[] Hues = { "red", "yellow", "green", "cyan", "blue", "magenta" };

        /// <summary>One digit per hue (0 empty, 1 dim, 2 striking), what has been paid out in points, and whether the whole frame's regard was paid. Saved.</summary>
        public string Places = "000000";
        public int PaidPoints;
        public bool WholePaid;

        public static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        // ── what an item is to the eye ──

        /// <summary>The hue (an index into <see cref="Hues"/>) and power (1 dim, 2 striking) of a thing's colour, or false for a plain grey, white or black. Qud colour codes: lower case is dim, upper case bright.</summary>
        public static bool TryHue(Entity item, out int hue, out int power)
        {
            hue = -1; power = 0; string color = item?.GetPart<RenderPart>()?.ColorString; if (string.IsNullOrEmpty(color)) return false;
            for (int i = 0; i + 1 < color.Length; i++)
            {
                if (color[i] != '&') continue;
                char c = color[i + 1];
                switch (char.ToLowerInvariant(c))
                {
                    case 'r': hue = 0; break;
                    case 'w': hue = 1; break;   // "dark yellow" (brown) and bright yellow are the yellow place
                    case 'g': hue = 2; break;
                    case 'c': hue = 3; break;
                    case 'b': hue = 4; break;
                    case 'm': hue = 5; break;
                    default: return false;      // k, y, K, Y (and anything else): plain
                }
                power = char.IsUpper(c) ? 2 : 1; return true;
            }
            return false;
        }

        int PowerAt(int hue) => hue >= 0 && hue < Places.Length && Places[hue] >= '0' && Places[hue] <= '2' ? Places[hue] - '0' : 0;
        int Points() { int sum = 0; for (int h = 0; h < Hues.Length; h++) sum += PowerAt(h); return sum; }
        bool Whole() { for (int h = 0; h < Hues.Length; h++) if (PowerAt(h) < 2) return false; return true; }

        static int Value(Entity item) => item?.GetPart<CommercePart>()?.Value ?? 0;

        // ── what you hold ──

        static List<Entity> Carried(Entity actor)
        {
            var list = new List<Entity>(); var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return list;
            foreach (var item in pack.Objects) if (item != null && !list.Contains(item)) list.Add(item);
            return list;
        }

        /// <summary>Null when this carried thing could be set in now; otherwise the code and the sentence that says why not. A plain thing is never offered at all.</summary>
        string Refusal(Entity item, int hue, int power, out string why)
        {
            why = null;
            if (item.HasTag("NoTrade") || item.HasTag("Owned") || item.HasTag("Essential") || item.HasTag("QuestItem") || item.HasTag("Creature")) { why = "It is not yours to give."; return "not-giveable"; }
            if (Value(item) > CheapLimit) { why = "The frame wants a thing that is only itself, not a thing worth keeping. This is worth too much."; return "precious"; }
            if (PowerAt(hue) >= power) { why = "The " + Hues[hue] + " place already holds " + (PowerAt(hue) >= 2 ? "a striking piece, as good as it gets" : "a piece as dim as this one") + "."; return "no-better"; }
            return null;
        }

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 20;

        public string DescribeState(DescribeContext context)
        {
            var wanted = new List<string>(); var dim = new List<string>(); var placed = new List<string>();
            for (int h = 0; h < Hues.Length; h++)
            {
                int p = PowerAt(h);
                if (p == 0) wanted.Add(Hues[h]); else { placed.Add(Hues[h] + (p >= 2 ? " (striking)" : " (dim)")); if (p == 1) dim.Add(Hues[h]); }
            }
            var sb = new StringBuilder("A frame of six places for found things, one for each colour. The Sorters keep it. It takes a thing that is only itself: bright is worth more than dim, grey, white and black are worth nothing, and nothing worth much in drams is taken. What is set in stays.");
            if (wanted.Count == 6) sb.Append(" It is empty: it wants ").Append(string.Join(", ", wanted)).Append('.');
            else if (Whole()) sb.Append(" Every place holds a striking piece. It is whole.");
            else
            {
                sb.Append(" Placed: ").Append(string.Join(", ", placed)).Append('.');
                if (wanted.Count > 0) sb.Append(" It wants a colour it does not have: ").Append(string.Join(", ", wanted)).Append('.');
                if (dim.Count > 0) sb.Append(" A striking piece would better: ").Append(string.Join(", ", dim)).Append('.');
            }
            return sb.ToString();
        }

        // ── rows ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Set(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            var rows = new List<(Entity item, int hue, int power, string refusal, string why)>();
            foreach (var item in Carried(actor))
            {
                if (!TryHue(item, out int hue, out int power)) continue;
                string refusal = Refusal(item, hue, power, out string why); rows.Add((item, hue, power, refusal, why));
            }
            if (rows.Count == 0) { actions.AddDisabled("SortPiece", "set a found thing in the frame", "nothing-to-set", "You carry nothing with a colour to it. Grey, white and black are plain: the frame wants red, yellow, green, cyan, blue or magenta.", 's', 20); return; }
            // Pieces that would improve a place first (the larger gain first), then the refused ones.
            rows.Sort((a, b) =>
            {
                int ga = a.refusal == null ? a.power - PowerAt(a.hue) : -1, gb = b.refusal == null ? b.power - PowerAt(b.hue) : -1;
                int c = gb.CompareTo(ga); return c != 0 ? c : string.CompareOrdinal(a.item.GetDisplayName() + a.item.ID, b.item.GetDisplayName() + b.item.ID);
            });
            for (int i = 0; i < rows.Count && i < MaxRows; i++)
            {
                var (item, hue, power, refusal, why) = rows[i];
                string label = "set " + item.GetDisplayName() + " in the " + Hues[hue] + " place (" + (power >= 2 ? "striking" : "dim") + ")";
                string hint = "It stays in the frame for good. The Sorters will see it: a small, lasting regard from the Bower-Folk, " + (power - PowerAt(hue)) * RegardPerPoint + " for this piece if it is the best the place has held.";
                if (refusal == null) actions.AddActionWithHint("SortPiece", label, Prefix + Uri.EscapeDataString(item.ID), hint, i == 0 ? 's' : '\0', 20 - Math.Min(i, 15), false, ActionTime.OneAction);
                else actions.AddDisabled("SortPiece", label, refusal, why, i == 0 ? 's' : '\0', 20 - Math.Min(i, 15));
            }
            if (rows.Count > MaxRows) actions.AddDisabled("SortPiece", "set a found thing (" + (rows.Count - MaxRows) + " more not shown)", "more", (rows.Count - MaxRows) + " more coloured things are not shown: set some aside to see them.", '\0', 0);
        }

        // ── the setting: one action; the piece, the frame and the regard come back together on any failure ──

        bool Set(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|'); var pack = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || pack == null || f.Length != 2 || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot reach the frame now.");
            string id = WorldResourceActions.Decode(f[1]); Entity item = null; foreach (var o in Carried(actor)) if (o.ID == id) { item = o; break; }
            if (item == null || !TryHue(item, out int hue, out int power)) return Block(actor, "stale-selection", "That is no longer something you carry that the frame could take.");
            string refusal = Refusal(item, hue, power, out string why); if (refusal != null) return Block(actor, refusal, why);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(item, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                string placesBefore = Places; int paidBefore = PaidPoints; bool wholeBefore = WholePaid; int regardBefore = PlayerReputation.Get(Faction);
                var receipt = InventoryTransferSnapshot.Capture(pack);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.TryConsumeOne(item)) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not let go of it.");
                int regard = 0; bool nowWhole = false;
                tx.Do(() =>
                {
                    var chars = Places.PadRight(Hues.Length, '0').ToCharArray(); chars[hue] = (char)('0' + power); Places = new string(chars, 0, Hues.Length);
                    int points = Points(), gain = points - PaidPoints; if (gain > 0) { regard += gain * RegardPerPoint; PaidPoints = points; }
                    if (Whole() && !WholePaid) { WholePaid = true; regard += WholeRegard; nowWhole = true; }
                },
                () => { Places = placesBefore; PaidPoints = paidBefore; WholePaid = wholeBefore; });
                string itemName = item.GetDisplayName(); var owner = ParentEntity; int placed = power;
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You set " + itemName + " in the " + Hues[hue] + " place. The Sorters will see it." + (nowWhole ? " The frame is whole." : ""));
                    if (regard > 0) PlayerReputation.Modify(Faction, regard);
                    Diag.Record("furniture", "FramePieceSet", actor, owner, new { hue = Hues[hue], power = placed, regard, whole = nowWhole });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "FrameRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
