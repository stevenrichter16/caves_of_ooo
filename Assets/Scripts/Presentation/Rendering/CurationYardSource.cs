using System;
using System.Collections.Generic;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 // Exact original intake kit, using the unchanged24-cell native glade palette.
 [Serializable]public sealed class CurationYardSource
 {
  public int schemaVersion;public string id;public string[] palette;public Model[] models;
  [Serializable]public sealed class Model{public string id;public Box[] boxes;}
  [Serializable]public sealed class Box{public string name;public Vector3 center,size;public int color;}
  public static readonly string[] ModelIds={"curation-yard-salt-cured-body-1","curation-yard-salt-cured-body-2","curation-yard-receiving-bay-1","curation-yard-receiving-bay-2","curation-yard-intake-index","curation-yard-tool-cabinet","curation-yard-salt-bench","curation-yard-quarantine-rail","curation-yard-quarantine-gate-closed","curation-yard-quarantine-gate-open","curation-yard-salt-rake","curation-yard-counterfoil","curation-yard-inspection-key","curation-yard-transfer-docket","curation-yard-discrepancy-report"};
  static readonly HashSet<string> Known=new HashSet<string>(ModelIds,StringComparer.Ordinal);
  public static bool IsModelId(string id)=>id!=null&&Known.Contains(id);
  public void Validate()
  {
   if(schemaVersion!=1||id!="curation-yard-original"||palette==null||palette.Length!=24||models==null||models.Length!=ModelIds.Length)throw new InvalidOperationException("Complete original Curation source required.");
   for(int i=0;i<24;i++)if(palette[i]!=SpreadEnvironmentSource.ApprovedPalette[i])throw new InvalidOperationException("Curation palette must remain approved.");
   var remaining=new HashSet<string>(ModelIds,StringComparer.Ordinal);
   foreach(var m in models)
   {
    if(m==null||!remaining.Remove(m.id)||m.boxes==null||m.boxes.Length<3||m.boxes.Length>100)throw new InvalidOperationException("Unknown or duplicate Curation form.");
    var names=new HashSet<string>(StringComparer.Ordinal);
    foreach(var b in m.boxes)
    {
     if(b==null||string.IsNullOrEmpty(b.name)||!names.Add(b.name)||b.color<0||b.color>=24)throw new InvalidOperationException("Invalid Curation piece.");
     for(int a=0;a<3;a++)if(float.IsNaN(b.center[a])||float.IsInfinity(b.center[a])||float.IsNaN(b.size[a])||float.IsInfinity(b.size[a])||b.size[a]<.003f||b.size[a]>1.65f)throw new InvalidOperationException("Invalid Curation geometry.");
     if(Mathf.Abs(b.center.x)+b.size.x*.5f>.501f||Mathf.Abs(b.center.z)+b.size.z*.5f>.501f||b.center.y-b.size.y*.5f<-.001f||b.center.y+b.size.y*.5f>1.65f)throw new InvalidOperationException("Curation geometry leaves native cell.");
    }
   }
  }
 }
}
