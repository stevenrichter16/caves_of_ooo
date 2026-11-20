using System;
using System.Collections.Generic;
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
    public sealed class SoundingRodPart : Part, IDescribeState
    {
        public override string Name => "SoundingRod";

        public const string Blueprint = "SoundingRod", Command = "SoundGround";
        /// <summary>Nothing closer than this counts: a lead is about ground beyond the next few steps.</summary>
        public const int MinPaces = 4;
        /// <summary>An open-ground lead needs at least this many connected unseen cells, so a single stray gap is not a hollow.</summary>
        public const int MinHollowCells = 6;
        public const int CloseBand = 8, MiddlingBand = 16;

        /// <summary>The last answer and the tick it was heard (-1 for never). Saved with the rod.</summary>
        public string LastReading = "";
        public int LastTick = -1;

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
            if (!OrdinaryColumn(zone, zx, zy, depth)) { message = "The stone here answers wrong. You leave it be."; return "not-ordinary"; }
            string floor = FloorUnder(origin); reach = Reach(floor);
            if (reach <= 0) { message = "There is no solid floor here for the rod to bite into."; return "ground-uncoupled"; }
            return null;
        }

        /// <summary>Whether the floor is an ordinary cave column: the layout plan's own predicate where the zone's world is known, minus the bespoke columns it cannot see.</summary>
        static bool OrdinaryColumn(Zone zone, int x, int y, int depth)
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
                parts.Add("Here the stone would carry a sounding about " + reach + " paces.");
            if (!string.IsNullOrEmpty(LastReading))
            {
                int age = LastTick >= 0 ? WorldClock.CurrentTick - LastTick : -1;
                parts.Add("Last sounding" + (age >= 0 ? " (" + Ago(age) + ")" : "") + ": " + LastReading);
            }
            return parts.Count == 0 ? null : string.Join(" ", parts);
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
            return true;
        }

        bool Carried(Entity actor) => actor != null && actor.GetPart<InventoryPart>()?.Objects.Contains(ParentEntity) == true;

        void AddRow(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Carried(actor)) return;   // a dropped rod only offers pickup: no "installed" or origin ambiguity
            string code = Refusal(actor, zone, out string why, out int reach);
            const string hint = "Drives the rod into the floor and strikes it. It hears open ground, ways up or down and dense stone you have not seen, as a bearing and a distance. The strike is loud.";
            if (code == null) actions.AddActionWithHint("SoundGround", "sound the ground (carries about " + reach + " paces)", Command, hint, 's', 20, false, ActionTime.OneAction);
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
                string beforeReading = LastReading; int beforeTick = LastTick; int noise = actor.GetIntProperty(Noise.Property, int.MinValue);
                tx.Do(() => { Noise.Mark(actor); LastReading = reading; LastTick = WorldClock.CurrentTick; },
                      () => { LastReading = beforeReading; LastTick = beforeTick; if (noise == int.MinValue) actor.IntProperties.Remove(Noise.Property); else actor.IntProperties[Noise.Property] = noise; });
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

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            CavesOfOoo.Diagnostics.Diag.Record("skill", "SoundRefused", actor, ParentEntity, new { code });
            return false;
        }
    }
}
