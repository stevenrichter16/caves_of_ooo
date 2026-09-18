using System;
using System.Collections.Generic;

namespace CavesOfOoo.FellingScene
{
    public readonly struct Point2 : IEquatable<Point2>
    {
        public readonly double X; public readonly double Y;
        public Point2(double x,double y) {X=x;Y=y;}
        public bool Equals(Point2 other) => X==other.X && Y==other.Y;
        public override bool Equals(object obj) => obj is Point2 && Equals((Point2)obj);
        public override int GetHashCode() => X.GetHashCode()*397^Y.GetHashCode();
        public override string ToString() => X+","+Y;
    }
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X; public readonly int Y;
        public Cell(int x,int y) {X=x;Y=y;}
        public bool Equals(Cell other) => X==other.X && Y==other.Y;
        public override bool Equals(object obj) => obj is Cell && Equals((Cell)obj);
        public override int GetHashCode() => X*397^Y;
        public override string ToString() => X+","+Y;
    }
    public static class SceneCoordinates
    {
        public const int ArtWidth = 1536;
        public const int ArtHeight = 1024;
        public const int PixelsPerCell = 32;
        public const int OverhangPixels = 224;
        public const int RegionX = 16;
        public const int RegionWidth = 48;
        public const int RegionHeight = 25;

        public static bool IsInRegion(Cell cell)
            => cell.X >= RegionX && cell.X < RegionX+RegionWidth && cell.Y >= 0 && cell.Y < RegionHeight;

        public static bool IsInArt(Point2 p)
            => IsFinite(p) && p.X >= 0 && p.X < ArtWidth && p.Y >= 0 && p.Y < ArtHeight;

        // Art samples and logical cells have half-open bounds. The visual overhang has no cell.
        public static bool TryImageToCell(Point2 p,out Cell cell)
        {
            cell = default(Cell);
            if (!IsInArt(p) || p.Y < OverhangPixels) return false;
            cell = new Cell(RegionX+(int)Math.Floor(p.X/PixelsPerCell),
                (int)Math.Floor((p.Y-OverhangPixels)/PixelsPerCell));
            return true;
        }

        public static Point2 CellCenterInImage(Cell cell)
        {
            RequireCell(cell);
            return new Point2((cell.X-RegionX+0.5)*PixelsPerCell,
                OverhangPixels+(cell.Y+0.5)*PixelsPerCell);
        }

        public static Point2 CellCenterInWorld(Cell cell)
        {
            RequireCell(cell);
            return new Point2(cell.X+0.5,RegionHeight-cell.Y-0.5);
        }

        // Affine transforms deliberately accept finite points outside the artwork, including
        // right/bottom edge vertices. Use IsInArt/TryImageToCell for picking and visibility.
        public static Point2 ImageToWorld(Point2 p)
        {
            RequireFinite(p);
            return new Point2(RegionX+p.X/PixelsPerCell,(ArtHeight-p.Y)/PixelsPerCell);
        }

        public static Point2 WorldToImage(Point2 p)
        {
            RequireFinite(p);
            return new Point2((p.X-RegionX)*PixelsPerCell,ArtHeight-p.Y*PixelsPerCell);
        }

        internal static void RequireCell(Cell cell)
        {
            if (!IsInRegion(cell)) throw new ArgumentOutOfRangeException(nameof(cell),"Cell is outside the authored scene region.");
        }

        private static bool IsFinite(Point2 p)
            => !double.IsNaN(p.X) && !double.IsInfinity(p.X) && !double.IsNaN(p.Y) && !double.IsInfinity(p.Y);

        private static void RequireFinite(Point2 p)
        {
            if (!IsFinite(p)) throw new ArgumentOutOfRangeException(nameof(p),"Coordinate must be finite.");
        }
    }

    // An offline, immutable occupancy snapshot. This is not a replacement for the game's
    // movement/physics rules. It proves the authored layout's conservative connectivity.
    public sealed class SceneOccupancy
    {
        private readonly HashSet<Cell> walkable = new HashSet<Cell>();
        private static readonly Cell[] Steps =
        {
            new Cell(0,-1),new Cell(1,0),new Cell(0,1),new Cell(-1,0),
            new Cell(1,-1),new Cell(1,1),new Cell(-1,1),new Cell(-1,-1)
        };

        public SceneOccupancy(IEnumerable<Cell> walkable)
        {
            if (walkable == null) throw new ArgumentNullException(nameof(walkable));
            foreach (var cell in walkable)
            {
                SceneCoordinates.RequireCell(cell);
                this.walkable.Add(cell);
            }
        }

        public bool IsWalkable(Cell cell) => walkable.Contains(cell);

        // Breadth-first search minimizes number of equally priced steps. If gameplay has
        // different diagonal/terrain costs, its pathfinder must remain authoritative.
        public bool TryFindPath(Cell from,Cell to,bool allowDiagonal,out IReadOnlyList<Cell> path)
        {
            path = Array.AsReadOnly(new Cell[0]);
            if (!IsWalkable(from) || !IsWalkable(to)) return false;
            var pending = new Queue<Cell>();
            var parents = new Dictionary<Cell,Cell>();
            pending.Enqueue(from);
            parents.Add(from,from);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                if (current.Equals(to))
                {
                    var result = new List<Cell> {to};
                    while (!current.Equals(from))
                    {
                        current = parents[current];
                        result.Add(current);
                    }
                    result.Reverse();
                    path = result.AsReadOnly();
                    return true;
                }
                for (int i=0;i<(allowDiagonal ? Steps.Length : 4);i++)
                {
                    var step = Steps[i];
                    var next = new Cell(current.X+step.X,current.Y+step.Y);
                    if (!IsWalkable(next) || parents.ContainsKey(next)) continue;
                    if (step.X != 0 && step.Y != 0 &&
                        (!IsWalkable(new Cell(current.X+step.X,current.Y)) ||
                         !IsWalkable(new Cell(current.X,current.Y+step.Y)))) continue;
                    parents.Add(next,current);
                    pending.Enqueue(next);
                }
            }
            return false;
        }
    }

    public enum Visibility {Unexplored,Remembered,Visible}

    // Stable scenery ID plus all occupied/owning logical cells. Pixel coverage and depth
    // patches belong to the renderer's data; the simulation footprint is not a sprite rect.
    public sealed class SceneryOwner
    {
        private readonly HashSet<Cell> membership;
        public string Id {get;private set;}
        public IReadOnlyList<Cell> Footprint {get;private set;}

        public SceneryOwner(string id,IEnumerable<Cell> cells)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Scenery ID is required.",nameof(id));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            Id = id;
            membership = new HashSet<Cell>();
            foreach (var cell in cells)
            {
                SceneCoordinates.RequireCell(cell);
                membership.Add(cell);
            }
            if (membership.Count == 0) throw new ArgumentException("Scenery requires at least one footprint cell.",nameof(cells));
            Footprint = SortedCells(membership);
        }

        public bool Contains(Cell cell) => membership.Contains(cell);

        internal static IReadOnlyList<Cell> SortedCells(IEnumerable<Cell> cells)
        {
            var ordered = new List<Cell>(cells);
            ordered.Sort((a,b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            return ordered.AsReadOnly();
        }
    }

    public sealed class SceneVisibility
    {
        private readonly Func<Cell,Visibility> read;

        public SceneVisibility(Func<Cell,Visibility> read)
        {
            this.read = read ?? throw new ArgumentNullException(nameof(read));
        }

        public Visibility ResolveGround(Point2 p)
        {
            Cell cell;
            return SceneCoordinates.TryImageToCell(p,out cell) ? Read(cell) : Visibility.Unexplored;
        }

        // Use only for a sample actually covered by the renderer's authored ownership mask.
        // The caller supplies one exact footprint owner, including for samples above y=224.
        // Never collapse a multi-cell footprint into "any cell visible => whole sprite visible".
        public Visibility ResolveOwned(Point2 p,SceneryOwner owner,Cell footprintCell)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!owner.Contains(footprintCell)) throw new ArgumentException("Sample owner cell is not in this scenery footprint.",nameof(footprintCell));
            return SceneCoordinates.IsInArt(p) ? Read(footprintCell) : Visibility.Unexplored;
        }

        private Visibility Read(Cell cell)
        {
            var value = read(cell);
            if (value != Visibility.Unexplored && value != Visibility.Remembered && value != Visibility.Visible)
                throw new ArgumentOutOfRangeException(nameof(read),"Visibility provider returned an unknown state.");
            return value;
        }
    }

    public sealed class Invalidation
    {
        public IReadOnlyList<string> OwnerIds {get;private set;}
        public IReadOnlyList<Cell> Cells {get;private set;}

        internal Invalidation(IEnumerable<SceneryOwner> owners)
        {
            var ids = new List<string>();
            var cells = new HashSet<Cell>();
            foreach (var owner in owners)
            {
                ids.Add(owner.Id);
                foreach (var cell in owner.Footprint) cells.Add(cell);
            }
            ids.Sort(StringComparer.Ordinal);
            OwnerIds = ids.AsReadOnly();
            Cells = SceneryOwner.SortedCells(cells);
        }
    }

    public sealed class OwnerInvalidationIndex
    {
        private readonly Dictionary<string,SceneryOwner> byId = new Dictionary<string,SceneryOwner>(StringComparer.Ordinal);
        private readonly Dictionary<Cell,List<SceneryOwner>> byCell = new Dictionary<Cell,List<SceneryOwner>>();

        public OwnerInvalidationIndex(IEnumerable<SceneryOwner> owners)
        {
            if (owners == null) throw new ArgumentNullException(nameof(owners));
            foreach (var owner in owners)
            {
                if (owner == null) throw new ArgumentException("Null scenery owner.",nameof(owners));
                if (byId.ContainsKey(owner.Id)) throw new ArgumentException("Duplicate scenery ID: "+owner.Id,nameof(owners));
                byId.Add(owner.Id,owner);
                foreach (var cell in owner.Footprint)
                {
                    List<SceneryOwner> atCell;
                    if (!byCell.TryGetValue(cell,out atCell))
                    {
                        atCell = new List<SceneryOwner>();
                        byCell.Add(cell,atCell);
                    }
                    atCell.Add(owner);
                }
            }
        }

        // Returns every directly affected owner and the UNION of their complete footprints.
        // The renderer refreshes all patches for these IDs, including above-zone pixels.
        // Ordinary ground invalidation at the input cell is a separate renderer obligation.
        public Invalidation ForCell(Cell cell)
        {
            List<SceneryOwner> owners;
            return new Invalidation(byCell.TryGetValue(cell,out owners) ? owners : new List<SceneryOwner>());
        }

        // Obtain invalidation before replacing this immutable index after owner removal.
        // Unknown IDs fail so a missed removal does not silently leave stale scenery.
        public Invalidation ForOwner(string id)
        {
            SceneryOwner owner;
            if (id == null || !byId.TryGetValue(id,out owner)) throw new KeyNotFoundException("Unknown scenery owner: "+id);
            return new Invalidation(new[] {owner});
        }
    }
}
