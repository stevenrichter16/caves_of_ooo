using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
 /// <summary>Explicit source graph for scoped copies of existing static art.
 /// Availability does not establish native reachability or owner authority.</summary>
 [Serializable] public sealed class SpreadNativeStyleSource
 {
  public const int ModelCount=868;
  public int schemaVersion;public string id;public Model[] models;
  [Serializable] public sealed class Model
  {public string id,sourceLibrary,kind;public bool semanticColors;}
  private static readonly HashSet<string> Libraries=new HashSet<string>(StringComparer.Ordinal){
   "SpawnRing3D","SoddenVoxel3D","TallyVoxel3D","StillleafVoxel3D","WellmeetVoxel3D","DensityPhase1Voxel3D","GinmereVoxel3D","TineVoxel3D","CathedralVoxel3D","GantryVoxel3D","FirstTentVoxel3D","CinderholdVoxel3D","MarrowstyeVoxel3D","BeatingVoxel3D","LastCounterVoxel3D","DrownedLedgerVoxel3D","OlderdeepVoxel3D","QuillholdVoxel3D","SumpholdVoxel3D","SpreadVoxel3D","StumpVoxel3D","OverwritVoxel3D"};
  public void Validate()
  {
   if(schemaVersion!=1||id!="spread-native-style-original-geometry"||models==null||models.Length!=ModelCount)
    throw new InvalidOperationException("Incomplete exact static source graph.");
   var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(var m in models)
    if(m==null||!SafeId(m.id)||!ids.Add(m.id)||m.sourceLibrary==null||!Libraries.Contains(m.sourceLibrary)
      ||(m.kind!="ground"&&m.kind!="entity"&&m.kind!="felling-component"&&m.kind!="tile-overlay"))
     throw new InvalidOperationException("Invalid static source identity, library or kind.");
  }
  public static bool SafeId(string value)
  {
   if(string.IsNullOrEmpty(value)||value.Length>120)return false;
   foreach(char c in value)if(!((c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='-'))return false;
   return true;
  }
 }
}
