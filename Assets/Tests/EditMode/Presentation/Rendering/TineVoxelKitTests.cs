using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class TineVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "water", "pier", "path" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = TineVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(20, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(TineVoxelKitLibrary.ModelId(family, variant));
                    Assert.NotNull(entry, family);
                    Assert.That(entry.Mesh.uv.Distinct().Count(), Is.InRange(1, 2), entry.Id);
                    Assert.That(entry.Mesh.vertexCount, Is.InRange(24, 240), entry.Id);
                    Assert.GreaterOrEqual(entry.Mesh.bounds.min.x, -.5001f, entry.Id);
                    Assert.LessOrEqual(entry.Mesh.bounds.max.x, .5001f, entry.Id);
                    Assert.GreaterOrEqual(entry.Mesh.bounds.min.z, -.5001f, entry.Id);
                    Assert.LessOrEqual(entry.Mesh.bounds.max.z, .5001f, entry.Id);
                    Assert.AreEqual(1, entry.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<Collider>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<Light>(true).Length, entry.Id);
                    Assert.AreSame(entry.Mesh, entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial,
                        entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
                    Assert.AreEqual(entry.Mesh.bounds.center, entry.Spec.boundsCenter, entry.Id);
                    Assert.AreEqual(entry.Mesh.bounds.size, entry.Spec.boundsSize, entry.Id);
                    Assert.AreEqual(entry.Mesh.triangles.Length / 3, entry.Spec.triangles, entry.Id);
                    Assert.AreEqual(family == "ground" || family == "path" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("Floor", "ground")]
        [TestCase("SandstoneWall", "wall")]
        [TestCase("WaterPuddle", "water")]
        [TestCase("Duckboard", "pier")]
        [TestCase("Sand", null)]
        [TestCase("StoneFloor", null)]
        [TestCase("RoadStone", "path")]
        [TestCase("BoatFrame", null)]
        [TestCase("Scribe", null)]
        [TestCase("MirePool", null)]
        [TestCase("DuckBoard", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, TineVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("tine-ground-3", TineVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => TineVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => TineVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TineVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => TineVoxelKitLibrary.ModelId("ground", 4));
            var kit = TineVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("tine-no-such-model-0"));
        }

        [Test]
        public void QuietLakeSurfaceAndGroundAreContinuousAcrossAllVariants()
        {
            foreach(string family in new[]{"ground","water","path"})for(int v=0;v<4;v++)
            {
                var m=Model(family,v);Assert.AreEqual(1,m.uv.Distinct().Count());Assert.LessOrEqual(m.vertexCount,48);
                Assert.That(m.bounds.size.x,Is.EqualTo(1).Within(.0001f));Assert.That(m.bounds.size.z,Is.EqualTo(1).Within(.0001f));
                Assert.That(m.bounds.max.y,Is.InRange(-.0001f,.025f));
                Assert.AreEqual(Model(family,0).bounds.max.y,m.bounds.max.y);
            }
            Assert.AreNotEqual(Model("ground",0).uv[0],Model("water",0).uv[0]);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void WallsStayLowAndPiersRemainCoarseWalkablePlanks(int variant)
        {
            var wall=Model("wall",variant);var pier=Model("pier",variant);
            Assert.That(wall.bounds.max.y,Is.InRange(1f,1.12f));Assert.LessOrEqual(wall.vertexCount,48);
            Assert.That(wall.bounds.size.x,Is.EqualTo(1).Within(.0001f));Assert.That(wall.bounds.size.z,Is.EqualTo(1).Within(.0001f));
            Assert.That(pier.bounds.max.y,Is.InRange(.1f,.2f));Assert.LessOrEqual(pier.vertexCount,144);
            Assert.IsTrue(Hit(pier,new Vector3(0,.6f,0),Vector3.down,.6f));
            Assert.IsFalse(Hit(pier,new Vector3(.155f,.6f,0),Vector3.down,.6f),"Coarse plank gaps remain real geometry.");
            Assert.IsTrue(Hit(pier,new Vector3(.155f,.04f,1),Vector3.back,2),"Gapped planks still have lower supports.");
        }

        // CQ05 camera review found the shared bright road and pale masonry
        // competing with the quiet lake. Test actual sampled palette luminance.
        [Test] public void MappedApproachHasItsOwnQuietEarthFamily()
        {
            for(int v=0;v<4;v++)
            {
                var entry=TineVoxelKitLibrary.Load().Find(TineVoxelKitLibrary.ModelId("path",v));
                Assert.AreEqual("ground",entry.Spec.kind);Assert.AreEqual(1,entry.Mesh.uv.Distinct().Count());
                Assert.AreEqual(Swatch(64),entry.Mesh.uv[0]);Assert.AreNotEqual(Model("ground",0).uv[0],entry.Mesh.uv[0]);
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void CutawayWallTopStaysSubduedAgainstLakeAndPeople(int variant)
        {
            var mesh=Model("wall",variant);var top=mesh.vertices.Select((p,i)=>new{p,i}).Where(v=>v.p.y>1.049f).Select(v=>mesh.uv[v.i]).Distinct().ToArray();
            Assert.AreEqual(1,top.Length);var texture=new Texture2D(2,2);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes("Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png")));
                var color=texture.GetPixel((int)(top[0].x*texture.width),(int)(top[0].y*texture.height));
                float luma=.2126f*color.r+.7152f*color.g+.0722f*color.b;
                Assert.LessOrEqual(luma,.59f,"Bright cream architecture must not dominate the lake.");
                CollectionAssert.AreEquivalent(new[]{Swatch(56),Swatch(107)},mesh.uv.Distinct());
            }finally{UnityEngine.Object.DestroyImmediate(texture);}
        }

        private static Mesh Model(string family, int variant) => TineVoxelKitLibrary.Load().Find(TineVoxelKitLibrary.ModelId(family, variant)).Mesh;

        private static Vector2 Swatch(int index) => new Vector2((index % 16 + .5f) / 16f, (index / 16 + .5f) / 8f);

        // Two-sided bounded ray/triangle intersection: probes actual openings rather than a bounding-box proxy.
        private static bool Hit(Mesh mesh, Vector3 origin, Vector3 direction, float maxDistance)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], e1 = vertices[triangles[i + 1]] - a, e2 = vertices[triangles[i + 2]] - a;
                Vector3 p = Vector3.Cross(direction, e2); float determinant = Vector3.Dot(e1, p);
                if (Mathf.Abs(determinant) < .000001f) continue;
                float inverse = 1 / determinant; Vector3 t = origin - a;
                float u = Vector3.Dot(t, p) * inverse; if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(t, e1); float v = Vector3.Dot(direction, q) * inverse;
                if (v < 0 || u + v > 1) continue;
                float distance = Vector3.Dot(e2, q) * inverse;
                if (distance >= 0 && distance <= maxDistance) return true;
            }
            return false;
        }

        [TestCase("duplicate")]
        [TestCase("missing")]
        [TestCase("null-entry")]
        [TestCase("null-id")]
        [TestCase("metadata-kind")]
        [TestCase("metadata-bounds")]
        [TestCase("metadata-triangles")]
        [TestCase("metadata-path")]
        [TestCase("metadata-rig")]
        [TestCase("metadata-material")]
        [TestCase("wrong-mesh")]
        [TestCase("wrong-material")]
        [TestCase("extra-light")]
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = TineVoxelKitLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var entry = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = entry; break;
                    case "missing": copy.Entries = copy.Entries.Take(copy.Entries.Length - 1).ToArray(); break;
                    case "null-entry": copy.Entries[0] = null; break;
                    case "null-id": entry.Id = null; break;
                    case "metadata-kind": entry.Spec.kind = "actor"; break;
                    case "metadata-bounds": entry.Spec.boundsSize += Vector3.one; break;
                    case "metadata-triangles": entry.Spec.triangles++; break;
                    case "metadata-path": entry.Spec.path = "wrong/path.prefab"; break;
                    case "metadata-rig": entry.Spec.rigged = true; break;
                    case "metadata-material": entry.Spec.materialFamily = "wrong-palette"; break;
                    case "wrong-mesh":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.GetComponent<MeshFilter>().sharedMesh = copy.Entries[4].Mesh;
                        entry.Prefab = badPrefab;
                        break;
                    case "extra-light":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.AddComponent<Light>();
                        entry.Prefab = badPrefab;
                        break;
                    case "wrong-material":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WaterMaterial;
                        entry.Prefab = badPrefab;
                        break;
                }
                Assert.Throws<InvalidOperationException>(() => copy.Validate(), corruption);
                Assert.DoesNotThrow(() => source.Validate(), "Corruption controls must leave real source assets intact.");
            }
            finally
            {
                if (badPrefab != null) UnityEngine.Object.DestroyImmediate(badPrefab);
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
