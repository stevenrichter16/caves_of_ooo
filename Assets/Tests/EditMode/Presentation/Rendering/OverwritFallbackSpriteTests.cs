using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class OverwritFallbackSpriteTests
    {
        [TestCase("OverwritGround","overwrit_ground",1)]
        [TestCase("OverwritNewGrowth","overwrit_new_growth",4)]
        [TestCase("OverwritWaymarker","overwrit_waymarker",4)]
        [TestCase("OverwritPilgrimBench","overwrit_pilgrim_bench",4)]
        public void NativeFallbackKeepsExactIdentityAndRealQuietSixteenPixelArt(string blueprint,string file,int variants)
        {
            Assert.AreEqual(file,EnvironmentSpriteRenderer.FixtureSprites.SingleOrDefault(e=>e.Blueprint==blueprint).File);
            int count=EnvironmentSpriteRenderer.FixtureVariantCounts.TryGetValue(blueprint,out var n)?n:1;
            Assert.AreEqual(variants,count);
            var shapes=new System.Collections.Generic.HashSet<string>();
            for(int v=0;v<variants;v++)
            {
                string name=file+(v==0?"":"_v"+v);
                string path=Path.Combine(Application.dataPath,"Resources/Sprites/Environment",name+".png");
                Assert.IsTrue(File.Exists(path),name);
                Assert.NotNull(Resources.Load<Sprite>("Sprites/Environment/"+name),name);
                var texture=new Texture2D(2,2);
                try
                {
                    Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
                    Assert.AreEqual(16,texture.width);Assert.AreEqual(16,texture.height);
                    var pixels=texture.GetPixels32();Assert.IsTrue(pixels.All(p=>p.a==0||p.a==255));
                    Assert.IsTrue(pixels.Any(p=>p.a==255));
                    if(blueprint=="OverwritGround")Assert.AreEqual(1,pixels.Distinct().Count(),"The scraped floor has no visible variations or tiled border.");
                    else Assert.IsTrue(pixels.Any(p=>p.r==30&&p.g==32&&p.b==28&&p.a==255),"Shared outline ink.");
                    Assert.LessOrEqual(pixels.Where(p=>p.a==255).Distinct().Count(),2,name);
                    shapes.Add(System.Convert.ToBase64String(File.ReadAllBytes(path)));
                }
                finally{Object.DestroyImmediate(texture);}
            }
            Assert.AreEqual(variants,shapes.Count,"Variants change silhouette instead of just filenames.");
        }
    }
}
