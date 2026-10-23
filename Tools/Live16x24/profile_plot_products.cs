if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stopped-editor measurement only.");
var factory = CavesOfOoo.Tests.DensityReferenceGladeTests.Factory();
var names = new[] { "CandyCarrot", "CandyCarrotSeed", "EmberwheatSeed", "MarlrootClod", "MarlrootSeed", "PitchpodResin", "PitchpodSeed", "InertSludge" };
var owners = new CavesOfOoo.Core.Entity[names.Length];
for(int i=0;i<owners.Length;i++) owners[i]=factory.CreateEntity(names[i]);
using(var art=new CavesOfOoo.Rendering.FlatPixelOpeningArtCatalog()) {
 var frames=new UnityEngine.Tilemaps.Tile[owners.Length];
 for(int k=0;k<100;k++) for(int i=0;i<owners.Length;i++) if(!art.TryResolve(owners[i],0,0,out frames[i])) throw new System.InvalidOperationException(names[i]);
 var watch=new System.Diagnostics.Stopwatch();
 long before=System.GC.GetAllocatedBytesForCurrentThread();
 watch.Start();
 int matches=0;
 for(int k=0;k<10000;k++) for(int i=0;i<owners.Length;i++) {
  UnityEngine.Tilemaps.Tile tile;
  if(art.TryResolve(owners[i],0,0,out tile) && object.ReferenceEquals(tile,frames[i])) matches++;
 }
 watch.Stop();
 long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
 return new {scope="Stopped-editor warmed selector only; detached fixture owners, not Play, frame rendering or Deck", calls=80000, matches=matches, elapsedMs=watch.Elapsed.TotalMilliseconds, allocatedBytes=allocated, atlasWidth=128, atlasHeight=24, ownersPlacedInWorld=false};
}
