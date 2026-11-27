using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The sounding rod (Docs/MACHINE-INVENTED-USES.md 3g; the V1 of Docs/MachineDesign/machine-resonator.md): a carried tool driven into the floor of an ordinary deep cave and struck. The stone
    /// answers about ground you have not yet seen on this floor, and only three kinds of it: open ground, a bore (a way up or down; the rod cannot tell which), and dense stone. Each is a
    /// bearing in eight points and a distance in three bands, never a count, a coordinate or a name. It hears terrain only: no creatures, items, containers or inscriptions.
    /// <para>The price is noise: the strike rings through the stone (<see cref="Noise.Mark"/>), which is how eyeless sound-hunters find you. It consumes nothing and is read-only on the world.
    /// How far the stone carries depends on what it is (soft sandstone drinks the pulse; quartzite carries it), which is the depth table the game already has.</para>
    /// </summary>
    public sealed class SoundingRodPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "SoundingRod";

        public const string Blueprint = "SoundingRod", Command = "SoundGround";
        const string NoiseWarning = "The strike rings through the stone and can draw nearby creatures that hunt by sound.";
        /// <summary>Nothing closer than this counts: a lead is about ground beyond the next few steps.</summary>
        public const int MinPaces = 4;
        /// <summary>An open-ground lead needs at least this many connected unseen cells, so a single stray gap is not a hollow.</summary>
        public const int MinHollowCells = 6;
        public const int CloseBand = 8, MiddlingBand = 16;

        /// <summary>The last answer and the tick it was heard (-1 for never). Saved with the rod.</summary>
        public string LastReading = "";
        public int LastTick = -1;

        /// <summary>The survey log: the last <see cref="LogSize"/> soundings, oldest first, each "tick|where you stood|what the stone said". It records only what the rod told you and where you were
        /// standing (never where the hollows are beyond what was said), so it is a notebook, not a map. Saved with the rod; absent in an older save (empty).</summary>
        public List<string> Log = new List<string>();
        public const int LogSize = 8;
        public const string LogVerb = "ReadSurveyLog";

        // ── how far the stone carries, by what the floor is made of (the shipped depth table) ──

        static readonly (string floor, int reach)[] Ground =
        {
            ("SandstoneFloor", 12), ("LimestoneFloor", 18), ("ShaleFloor", 14), ("SlateFloor", 20), ("QuartziteFloor", 26), ("ObsidianFloor", 30),
        };

        /// <summary>Paces the stone carries over this floor, or 0 when the floor is not ground the rod couples to.</summary>
        public static int Reach(string floorBlueprint)
        {
            foreach (var (floor, reach) in Ground) if (floor == floorBlueprint) return reach;
            return 0;
        }

        /// <summary>The floor blueprint of every cave depth must have a reach, so a future depth band cannot silently refuse the rod.</summary>
        public static IEnumerable<string> FloorTokens() { foreach (var (floor, _) in Ground) yield return floor; }

        static readonly string[] Points = { "east", "northeast", "north", "northwest", "west", "southwest", "south", "southeast" };

        /// <summary>The eight-point bearing of (dx, dy), north being up the map (y decreasing).</summary>
        public static string Bearing(int dx, int dy)
        {
            double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI; if (angle < 0) angle += 360.0;
            return Points[(int)Math.Round(angle / 45.0) % 8];
        }

        public static string Band(int paces) => paces <= CloseBand ? "close" : paces <= MiddlingBand ? "middling" : "far";

        // ── eligibility ──

        /// <summary>
        /// Null when the rod can sound here; otherwise a refusal code and the sentence that says why. The ground must be an ordinary cave floor (never a village, a site or the authored
        /// columns, which the game keeps unmapped) of a kind the rod couples to.
        /// </summary>
        public static string Refusal(Entity actor, Zone zone, out string message, out int reach)
        {
            message = null; reach = 0;
            var origin = actor == null || zone == null ? null : zone.GetEntityCell(actor);
            if (origin == null) { message = "You cannot sound the ground from here."; return "no-place"; }
            var (zx, zy, depth) = WorldMap.FromZoneID(zone.ZoneID);
            if (depth <= 0) { message = "The ground up here is too loose to carry a sounding."; return "surface"; }
            // The same words as for ground the rod cannot bite into, so a refusal never marks a floor as special (the dossier's rule).
            if (!OrdinaryColumn(zone, zx, zy, depth)) { message = "There is no solid floor here for the rod to bite into."; return "not-ordinary"; }
            string floor = FloorUnder(origin) ?? BareCaveFloor.Resolve(zone, origin, actor);
            reach = Reach(floor);
            if (reach <= 0) { message = "There is no solid floor here for the rod to bite into."; return "ground-uncoupled"; }
            return null;
        }

        /// <summary>Whether the floor is an ordinary cave column: the layout plan's own predicate where the zone's world is known, minus the bespoke columns it cannot see.</summary>
        internal static bool OrdinaryColumn(Zone zone, int x, int y, int depth)
        {
            if (zone.ZoneID == GleanersCellarZone) return false;   // a bespoke cellar the plan would call ordinary (dossier correction C3)
            var manager = WorldLocationContext.For(zone);
            if (manager?.WorldMap == null) return true;            // no world to ask (a detached zone): the floor and depth rules still apply
            var biome = manager.WorldMap.GetBiome(x, y); var poi = manager.WorldMap.GetPOI(x, y);
            return UndergroundLayoutPlan.Select(manager.WorldSeed, zone.ZoneID, biome, poi, manager.Factory).IsOrdinaryColumn;
        }

        public const string GleanersCellarZone = "Overworld.11.10.1";

        static string FloorUnder(Cell cell)
        {
            foreach (var o in cell.Objects) if (o.HasTag("Terrain") && Reach(o.BlueprintName) > 0) return o.BlueprintName;
            return null;
        }

        // ── the sensing ──

        /// <summary>One thing the stone told: what class of ground, which way, how far.</summary>
        public readonly struct Lead
        {
            public readonly string Kind; public readonly string Bearing, Band; public readonly int Paces;
            public Lead(string kind, string bearing, string band, int paces) { Kind = kind; Bearing = bearing; Band = band; Paces = paces; }
            public string Sentence => Kind == "hollow" ? "Open ground lies " + Bearing + ", " + Band + "." : Kind == "bore" ? "Something bored through the stone lies " + Bearing + ", " + Band + "." : "Dense stone lies " + Bearing + ", " + Band + ".";
        }

        /// <summary>
        /// Up to three leads, one per class, about unexplored ground within <paramref name="reach"/> and at least <see cref="MinPaces"/> from the origin: the largest-enough connected stretch of
        /// unseen open ground (by its nearest cell), the nearest unseen stair, the nearest unseen vein. Deterministic; reads only the grid.
        /// </summary>
        public static List<Lead> Sense(Zone zone, Cell origin, int reach)
        {
            var leads = new List<Lead>(3); if (zone == null || origin == null || reach < MinPaces) return leads;
            int x0 = Math.Max(0, origin.X - reach), x1 = Math.Min(Zone.Width - 1, origin.X + reach), y0 = Math.Max(0, origin.Y - reach), y1 = Math.Min(Zone.Height - 1, origin.Y + reach);
            int Dist(Cell c) => Math.Max(Math.Abs(c.X - origin.X), Math.Abs(c.Y - origin.Y));
            Cell bore = null, seam = null; var open = new List<Cell>();
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var cell = zone.GetCell(x, y); if (cell == null || cell.Explored) continue; int d = Dist(cell); if (d < MinPaces || d > reach) continue;
                    bool stairs = false, vein = false;
                    foreach (var o in cell.Objects) { if (o.HasPart<StairsUpPart>() || o.HasPart<StairsDownPart>()) stairs = true; if (o.HasTag("MineralVein")) vein = true; }
                    if (stairs) { if (bore == null || Closer(cell, bore, origin)) bore = cell; }
                    else if (vein) { if (seam == null || Closer(cell, seam, origin)) seam = cell; }
                    else if (cell.IsPassable()) open.Add(cell);
                }
            Cell hollow = NearestHollow(zone, open, origin);
            if (hollow != null) leads.Add(Make("hollow", hollow, origin));
            if (bore != null) leads.Add(Make("bore", bore, origin));
            if (seam != null) leads.Add(Make("seam", seam, origin));
            return leads;
        }

        static bool Closer(Cell a, Cell b, Cell origin)
        {
            int da = Math.Max(Math.Abs(a.X - origin.X), Math.Abs(a.Y - origin.Y)), db = Math.Max(Math.Abs(b.X - origin.X), Math.Abs(b.Y - origin.Y));
            return da != db ? da < db : (a.Y, a.X).CompareTo((b.Y, b.X)) < 0;
        }

        static Lead Make(string kind, Cell cell, Cell origin)
        {
            int paces = Math.Max(Math.Abs(cell.X - origin.X), Math.Abs(cell.Y - origin.Y));
            return new Lead(kind, Bearing(cell.X - origin.X, cell.Y - origin.Y), Band(paces), paces);
        }

        /// <summary>Of the connected stretches (four-way) of unseen open ground with at least <see cref="MinHollowCells"/> cells, the one whose nearest cell is nearest; null if none.</summary>
        static Cell NearestHollow(Zone zone, List<Cell> open, Cell origin)
        {
            if (open.Count < MinHollowCells) return null;
            var inSet = new HashSet<Cell>(open); var seen = new HashSet<Cell>(); Cell best = null;
            foreach (var start in open)
            {
                if (seen.Contains(start)) continue;
                var queue = new Queue<Cell>(); var size = 0; Cell nearest = null; queue.Enqueue(start); seen.Add(start);
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue(); size++; if (nearest == null || Closer(c, nearest, origin)) nearest = c;
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var n = zone.GetCell(c.X + dx, c.Y + dy); if (n == null || !inSet.Contains(n) || !seen.Add(n)) continue; queue.Enqueue(n);
                    }
                }
                if (size >= MinHollowCells && (best == null || Closer(nearest, best, origin))) best = nearest;
            }
            return best;
        }

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;

        public string DescribeState(DescribeContext context)
        {
            var parts = new List<string>();
            if (context.Viewer != null && context.Zone != null && Refusal(context.Viewer, context.Zone, out _, out int reach) == null)
                parts.Add("The rod can survey up to " + reach + " paces here.");
            if (!string.IsNullOrEmpty(LastReading))
            {
                int age = LastTick >= 0 ? WorldClock.CurrentTick - LastTick : -1;
                parts.Add("Last sounding" + (age >= 0 ? " (" + Ago(age) + ")" : "") + ": " + LastReading);
            }
            if (Log.Count > 1) parts.Add(Log.Count + " soundings are in its log.");
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        /// <summary>How to use it, what it needs and what it costs, from the live constants (the owner, 2026-11-21). The rod always shows its sound row (disabled with the reason when it cannot sound), so the "right now" line says why.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            int low = int.MaxValue, high = 0; foreach (var (_, reach) in Ground) { low = Math.Min(low, reach); high = Math.Max(high, reach); }
            guide.How.Add("Press C on your own cell and choose sound the ground (or choose it from the pack). The stone answers with a bearing and a rough distance for unseen open ground, a bore through stone, or dense rock.");
            guide.How.Add("Choose read the survey log to read back your last " + LogSize + " soundings.");
            guide.Needs.Add("an ordinary cave floor below the surface, including dry bare rock in a natural cave");
            guide.Needs.Add("the rod in your pack");
            guide.Limits.Add("Survey reach depends on the stone (" + low + " to " + high + " paces). It reports only terrain and nothing nearer than " + MinPaces + " paces. It uses nothing up.");
            guide.Limits.Add(NoiseWarning);
            if (context.Viewer != null && !UseGuides.CarriedBy(context.Viewer, ParentEntity)) guide.WhyNot = UseGuides.PickUpFirst;
        }

        static string Ago(int ticks) => ticks < 60 ? "just now" : ticks < WorldClock.DayLengthTicks / 4 ? "a little while ago" : ticks < WorldClock.DayLengthTicks ? "today" : ticks / WorldClock.DayLengthTicks + (ticks / WorldClock.DayLengthTicks == 1 ? " day ago" : " days ago");

        // ── the verb ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRow(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == Command)
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Sound(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == LogVerb)
            {
                if (ReadLog(e.GetParameter<Entity>("Actor"))) { e.Handled = true; return false; }
            }
            return true;
        }

        bool Carried(Entity actor) => actor != null && actor.GetPart<InventoryPart>()?.Objects.Contains(ParentEntity) == true;

        void AddRow(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Carried(actor)) return;   // a dropped rod only offers pickup: no "installed" or origin ambiguity
            // The log is a notebook: it reads anywhere, takes no time, and changes nothing.
            if (Log.Count > 0) actions.AddActionWithHint("SurveyLog", "read the survey log (" + Log.Count + (Log.Count == 1 ? " sounding)" : " soundings)"), LogVerb, "Reads back what the rod told you, oldest first, and where you stood. It changes nothing and takes no time.", 'l', 19, false, ActionTime.Free);
            else actions.AddDisabled("SurveyLog", "read the survey log", "empty", "You have not sounded anything with this rod yet.", 'l', 19);
            string code = Refusal(actor, zone, out string why, out int reach);
            const string hint = "Drives the rod into the floor and strikes it. It reports a direction and a rough distance for unseen open ground, a bore through stone, or dense rock. " + NoiseWarning;
            if (code == null) actions.AddActionWithHint("SoundGround", "sound the ground (survey up to " + reach + " paces)", Command, hint, 's', 20, false, ActionTime.OneAction).AtCell(zone.GetEntityCell(actor).X, zone.GetEntityCell(actor).Y);
            else actions.AddDisabled("SoundGround", "sound the ground", code, why, 's', 20);
        }

        bool Sound(Entity actor, Zone zone, InventoryTransaction tx)
        {
            if (!Carried(actor)) return Block(actor, "not-carried", "You are not holding the rod.");
            string code = Refusal(actor, zone, out string why, out int reach); if (code != null) return Block(actor, code, why);
            var origin = zone.GetEntityCell(actor); var leads = Sense(zone, origin, reach);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, Command)) return Block(actor, "in-progress", "The rod is already in use.");
                string reading = leads.Count == 0 ? "the stone answers, and there is nothing in reach that you have not already seen." : string.Join(" ", leads.ConvertAll(l => l.Sentence));
                string beforeReading = LastReading; int beforeTick = LastTick; int noise = actor.GetIntProperty(Noise.Property, int.MinValue); var beforeLog = new List<string>(Log);
                var (zx, zy, depth) = WorldMap.FromZoneID(zone.ZoneID); string where = "map (" + zx + "," + zy + ") depth " + depth + ", at (" + origin.X + "," + origin.Y + ")";
                tx.Do(() =>
                {
                    Noise.Mark(actor); LastReading = reading; LastTick = WorldClock.CurrentTick;
                    Log.Add(LastTick + "|" + where + "|" + reading); if (Log.Count > LogSize) Log.RemoveRange(0, Log.Count - LogSize);
                },
                () => { LastReading = beforeReading; LastTick = beforeTick; Log = beforeLog; if (noise == int.MinValue) actor.IntProperties.Remove(Noise.Property); else actor.IntProperties[Noise.Property] = noise; });
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You drive the rod into the floor and strike it. The stone rings.");
                    MessageLog.Add(leads.Count == 0 ? "The stone answers, and there is nothing in reach that you have not already seen." : "The stone answers: " + reading);
                    CavesOfOoo.Diagnostics.Diag.Record("skill", "Sounded", actor, ParentEntity, new { zone = zone.ZoneID, reach, hollow = leads.Exists(l => l.Kind == "hollow"), bore = leads.Exists(l => l.Kind == "bore"), seam = leads.Exists(l => l.Kind == "seam") });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        /// <summary>Prints the log to the message log, oldest first. Read-only.</summary>
        bool ReadLog(Entity actor)
        {
            if (!Carried(actor)) return Block(actor, "not-carried", "You are not holding the rod.");
            if (Log.Count == 0) return Block(actor, "empty", "You have not sounded anything with this rod yet.");
            MessageLog.Add("The rod's survey log, oldest first:");
            foreach (var entry in Log)
            {
                var bits = entry.Split(new[] { '|' }, 3);
                if (bits.Length != 3 || !int.TryParse(bits[0], out int tick)) continue;
                MessageLog.Add("- " + Ago(Math.Max(0, WorldClock.CurrentTick - tick)) + ", " + bits[1] + ": " + bits[2]);
            }
            return true;
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            CavesOfOoo.Diagnostics.Diag.Record("skill", "SoundRefused", actor, ParentEntity, new { code });
            return false;
        }
    }
}
