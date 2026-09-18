using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.FellingScene;

internal static class Program
{
    private static int passed;
    private static int failed;

    private static void Main()
    {
        Test("ground corners map into the embedded 48x25 region", () =>
        {
            Equal(new Cell(16, 0), Map(0, 224));
            Equal(new Cell(63, 24), Map(1535.99, 1023.99));
        });
        Test("half-open boundaries and nonfinite pixels are rejected", () =>
        {
            foreach (var p in new[] { P(-0.01, 224), P(1536,224), P(0,223.999), P(0,1024), P(double.NaN,500), P(50,double.PositiveInfinity) })
                Check(!SceneCoordinates.TryImageToCell(p, out _), "unexpected mapped cell");
        });
        Test("every embedded cell center round-trips without off-by-one", () =>
        {
            foreach (var c in AllCells()) Equal(c, Map(SceneCoordinates.CellCenterInImage(c)));
        });
        Test("world art bounds preserve all seven rows of overhang", () =>
        {
            Equal(P(16,32), SceneCoordinates.ImageToWorld(P(0,0)));
            Equal(P(64,0), SceneCoordinates.ImageToWorld(P(1536,1024)));
            Equal(P(768,512), SceneCoordinates.WorldToImage(P(40,16)));
        });
        Test("image and world centers match the game's inverted Y convention", () =>
        {
            foreach (var c in AllCells())
            {
                var world = SceneCoordinates.ImageToWorld(SceneCoordinates.CellCenterInImage(c));
                Equal(P(c.X + 0.5, 24.5-c.Y), world);
                Equal(world, SceneCoordinates.CellCenterInWorld(c));
            }
        });
        Test("invalid logical cells and nonfinite transforms fail explicitly", () =>
        {
            Throws<ArgumentOutOfRangeException>(() => SceneCoordinates.CellCenterInImage(new Cell(15,0)));
            Throws<ArgumentOutOfRangeException>(() => SceneCoordinates.CellCenterInWorld(new Cell(16,25)));
            Throws<ArgumentOutOfRangeException>(() => SceneCoordinates.ImageToWorld(P(double.NaN,0)));
        });
        Test("occupancy is copied, outside cells never walkable", () =>
        {
            var list = new List<Cell> { new Cell(20,20) };
            var grid = new SceneOccupancy(list);
            list.Clear();
            Check(grid.IsWalkable(new Cell(20,20)), "source was not copied");
            Check(!grid.IsWalkable(new Cell(15,20)), "outside walkable");
            Check(!grid.IsWalkable(new Cell(21,20)), "unlisted cell walkable");
            Throws<ArgumentOutOfRangeException>(() => new SceneOccupancy(new[] {new Cell(0,0)}));
        });
        Test("path finds connected cells and includes both endpoints", () =>
        {
            var grid = new SceneOccupancy(new[] {new Cell(20,20),new Cell(21,20),new Cell(22,20)});
            Check(grid.TryFindPath(new Cell(20,20),new Cell(22,20), false,out var path), "path missing");
            Equal("20,20;21,20;22,20", string.Join(";",path));
        });
        Test("blocked and disconnected targets return empty paths", () =>
        {
            var grid = new SceneOccupancy(new[] {new Cell(20,20),new Cell(22,20)});
            Check(!grid.TryFindPath(new Cell(20,20),new Cell(22,20),true,out var disconnected), "disconnected path");
            Equal(0, disconnected.Count);
            Check(!grid.TryFindPath(new Cell(20,20),new Cell(19,20),true,out var blocked), "blocked path");
            Equal(0, blocked.Count);
            Check(!grid.TryFindPath(new Cell(15,0),new Cell(20,20),true,out _), "out-of-scene start");
        });
        Test("diagonals cannot cross either blocked cardinal neighbor", () =>
        {
            var start = new Cell(20,20); var end = new Cell(21,21);
            var grid = new SceneOccupancy(new[] {start,end});
            Check(!grid.TryFindPath(start,end,true,out _), "cut two blocked corners");
            var oneOpen = new SceneOccupancy(new[] {start,end,new Cell(21,20)});
            Check(oneOpen.TryFindPath(start,end,true,out var around), "cardinal detour missing");
            Equal(3, around.Count);
        });
        Test("diagonal step is allowed only when both side cells are open", () =>
        {
            var grid = new SceneOccupancy(new[] {new Cell(20,20),new Cell(21,20),new Cell(20,21),new Cell(21,21)});
            Check(grid.TryFindPath(new Cell(20,20),new Cell(21,21),true,out var diagonal), "diagonal missing");
            Equal(2, diagonal.Count);
            Check(grid.TryFindPath(new Cell(20,20),new Cell(21,21),false,out var cardinal), "cardinal missing");
            Equal(3, cardinal.Count);
        });
        Test("zero-step path succeeds only on walkable cells", () =>
        {
            var grid = new SceneOccupancy(new[] {new Cell(20,20)});
            Check(grid.TryFindPath(new Cell(20,20),new Cell(20,20),true,out var path), "zero path missing");
            Equal(1,path.Count);
            Check(!grid.TryFindPath(new Cell(21,20),new Cell(21,20),true,out _), "blocked zero path");
        });
        Test("ground fog uses the exact sample cell", () =>
        {
            var states = new SceneVisibility(c => c.Equals(new Cell(16,0)) ? Visibility.Visible : Visibility.Unexplored);
            Equal(Visibility.Visible, states.ResolveGround(P(16,240)));
            Equal(Visibility.Unexplored, states.ResolveGround(P(48,240)));
        });
        Test("unowned overhang and outside art stay hidden even beside visible ground", () =>
        {
            var states = new SceneVisibility(c => Visibility.Visible);
            Equal(Visibility.Unexplored, states.ResolveGround(P(16,100)));
            Equal(Visibility.Unexplored, states.ResolveGround(P(1536,500)));
            Equal(Visibility.Unexplored, states.ResolveGround(P(double.NaN,500)));
        });
        Test("owned overhang uses explicit footprint cell, never any-visible aggregation", () =>
        {
            var piece = new SceneryOwner("trunk",new[] {new Cell(16,0),new Cell(17,0)});
            var states = new SceneVisibility(c => c.X == 16 ? Visibility.Visible : Visibility.Remembered);
            Equal(Visibility.Visible,states.ResolveOwned(P(10,100),piece,new Cell(16,0)));
            Equal(Visibility.Remembered,states.ResolveOwned(P(50,100),piece,new Cell(17,0)));
            Equal(Visibility.Unexplored,states.ResolveOwned(P(1536,100),piece,new Cell(16,0)));
            Throws<ArgumentException>(() => states.ResolveOwned(P(50,100),piece,new Cell(18,0)));
        });
        Test("invalidation fans out to every cell of every directly affected owner", () =>
        {
            var root = new SceneryOwner("root",new[] {new Cell(20,5),new Cell(21,5)});
            var flower = new SceneryOwner("flower",new[] {new Cell(21,5),new Cell(21,6)});
            var unrelated = new SceneryOwner("far",new[] {new Cell(60,20)});
            var index = new OwnerInvalidationIndex(new[] {root,flower,unrelated});
            var affected = index.ForCell(new Cell(21,5));
            Equal("flower;root",string.Join(";",affected.OwnerIds));
            Equal("20,5;21,5;21,6",string.Join(";",affected.Cells));
            Equal(0,index.ForCell(new Cell(30,20)).OwnerIds.Count);
            Equal("20,5;21,5",string.Join(";",index.ForOwner("root").Cells));
        });
        Test("owners copy and deduplicate footprints; invalid ownership fails", () =>
        {
            var cells = new List<Cell> {new Cell(20,5),new Cell(20,5)};
            var owner = new SceneryOwner("root",cells); cells.Clear();
            Equal(1,owner.Footprint.Count);
            Throws<ArgumentException>(() => new SceneryOwner(" ",new[] {new Cell(20,5)}));
            Throws<ArgumentException>(() => new SceneryOwner("empty",Array.Empty<Cell>()));
            Throws<ArgumentOutOfRangeException>(() => new SceneryOwner("bad",new[] {new Cell(80,0)}));
            Throws<ArgumentException>(() => new OwnerInvalidationIndex(new[] {owner,owner}));
            Throws<KeyNotFoundException>(() => new OwnerInvalidationIndex(new[] {owner}).ForOwner("missing"));
        });
        Console.WriteLine($"RESULT: {passed} passed; {failed} failed.");
        Environment.ExitCode = failed == 0 ? 0 : 1;
    }

    private static void Test(string name,Action run) { try {run();passed++;Console.WriteLine("PASS "+name);} catch(Exception e) {failed++;Console.WriteLine("FAIL "+name+": "+e.GetType().Name+" "+e.Message);} }
    private static Point2 P(double x,double y) => new Point2(x,y);
    private static Cell Map(double x,double y) => Map(P(x,y));
    private static Cell Map(Point2 p) {Check(SceneCoordinates.TryImageToCell(p,out var c),"mapping failed");return c;}
    private static IEnumerable<Cell> AllCells() {for(int y=0;y<25;y++) for(int x=16;x<64;x++) yield return new Cell(x,y);}
    private static void Check(bool value,string message) {if(!value) throw new Exception(message);}
    private static void Equal<T>(T expected,T actual) {if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}; got {actual}");}
    private static void Throws<T>(Action action) where T:Exception {try {action();} catch(T) {return;} throw new Exception("Expected "+typeof(T).Name);}
}
