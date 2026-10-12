using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class VaultCursorRendererTests
    {
        [TestCase(false)] [TestCase(true)]
        public void PreviewSeparatesRedCreatureTargetFromYellowLanding(bool creature)
        {
            using(var c=new HaulingContentScope())
            {
                var go=new GameObject("vault preview");
                try
                {
                    var z=new Zone();var p=c.Factory.CreateEntity("Player");z.AddEntity(p,10,10);z.GetCell(12,10).IsVisible=true;
                    if(creature)z.AddEntity(c.Factory.CreateEntity("MarlbackScrabbler"),12,10);
                    var r=new WorldCursorRenderer(go.transform,null,0);r.SetZone(z);
                    var s=new WorldCursorState();s.Activate(WorldCursorMode.AbilityTarget,z,12,10,10,10,2);r.SetCursor(s,p);
                    Assert.AreEqual(creature?QudColorParser.BrightRed:QudColorParser.BrightYellow,r.CurrentColor);
                    var property=typeof(WorldCursorRenderer).GetProperty("LandingCell");Assert.NotNull(property);
                    Assert.AreEqual(new Vector2Int(creature?11:12,10),property.GetValue(r));
                    for (int i=0;i<100;i++) r.SetCursor(s,p);
                    long before=System.GC.GetAllocatedBytesForCurrentThread();
                    for (int i=0;i<10000;i++) r.SetCursor(s,p);
                    Assert.AreEqual(0,System.GC.GetAllocatedBytesForCurrentThread()-before,"Cached preview must not allocate every frame.");
                    z.GetCell(12,10).IsVisible=false; r.SetCursor(s,p);
                    Assert.AreEqual(QudColorParser.Gray,r.CurrentColor);
                    Assert.AreEqual(new Vector2Int(-1,-1),r.LandingCell,"Hidden cells reveal neither a target nor a landing.");
                    r.SetZone(new Zone());Assert.False(r.IsVisible);
                    Assert.AreEqual(new Vector2Int(-1,-1),property.GetValue(r));
                }
                finally{Object.DestroyImmediate(go);}
            }
        }
    }
}
