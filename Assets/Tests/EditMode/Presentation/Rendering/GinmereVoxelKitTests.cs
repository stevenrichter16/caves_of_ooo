using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class GinmereVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "cliff", "rim", "ledge", "water", "anchor", "nest", "gecko", "frog", "torch", "meat", "tonic", "frost", "ice", "stalagmite", "cache" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = GinmereVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(64, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(GinmereVoxelLibrary.ModelId(family, variant));
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
                    Assert.AreSame(entry.Mesh, entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial,
                        entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
                    Assert.AreEqual(entry.Mesh.bounds.center, entry.Spec.boundsCenter, entry.Id);
                    Assert.AreEqual(entry.Mesh.bounds.size, entry.Spec.boundsSize, entry.Id);
                    Assert.AreEqual(entry.Mesh.triangles.Length / 3, entry.Spec.triangles, entry.Id);
                    Assert.AreEqual(family == "ground" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("SandstoneFloor", "ground")]
        [TestCase("SandstoneWall", "cliff")]
        [TestCase("SinkholeLip", "rim")]
        [TestCase("DescentLedge", "ledge")]
        [TestCase("MirePool", "water")]
        [TestCase("RopeAnchor", "anchor")]
        [TestCase("PricklebrowNest", "nest")]
        [TestCase("PrickleBrowGecko", "gecko")]
        [TestCase("GinFrog", "frog")]
        [TestCase("Torch", "torch")]
        [TestCase("DriedMeat", "meat")]
        [TestCase("HealingTonic", "tonic")]
        [TestCase("FrostVent", "frost")]
        [TestCase("IceSheet", "ice")]
        [TestCase("Stalagmite", "stalagmite")]
        [TestCase("BoneCache", "cache")]
        [TestCase("SteamVent", null)]
        [TestCase("Bones", null)]
        [TestCase("SnapjawScavenger", null)]
        [TestCase("SnapjawHunter", null)]
        [TestCase("SprayPool", null)]
        [TestCase("HelmwoodFrog", null)]
        [TestCase("BrocchiniaSentinel", null)]
        [TestCase("PricklebrowGecko", null)]
        [TestCase("Floor", null)]
        [TestCase("Grass", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, GinmereVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("ginmere-ground-3", GinmereVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => GinmereVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => GinmereVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GinmereVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GinmereVoxelLibrary.ModelId("ground", 4));
            var kit = GinmereVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("ginmere-no-such-model-0"));
        }

        [TestCase("ground")]
        [TestCase("water")]
        [TestCase("ice")]
        public void GroundAndWaterAreContinuousQuietSurfacesAcrossVariants(string family)
        {
            var kit = GinmereVoxelLibrary.Load();
            Vector2? swatch = null; float? top = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId(family, variant));
                Assert.AreEqual(24, entry.Mesh.vertexCount);
                Assert.AreEqual(1, entry.Mesh.uv.Distinct().Count());
                Assert.AreEqual(1, entry.Mesh.bounds.size.x, .0001f);
                Assert.AreEqual(1, entry.Mesh.bounds.size.z, .0001f);
                Assert.LessOrEqual(entry.Mesh.bounds.size.y, .12f);
                Assert.LessOrEqual(entry.Mesh.bounds.max.y, .06f);
                if (swatch.HasValue) Assert.AreEqual(swatch.Value, entry.Mesh.uv[0]);
                if (top.HasValue) Assert.AreEqual(top.Value, entry.Mesh.bounds.max.y, .0001f);
                swatch = entry.Mesh.uv[0]; top = entry.Mesh.bounds.max.y;
            }
            var floor = kit.Find(GinmereVoxelLibrary.ModelId("ground", 0));
            var water = kit.Find(GinmereVoxelLibrary.ModelId("water", 0));
            Assert.AreNotEqual(floor.Mesh.uv[0], water.Mesh.uv[0]);
            Assert.Greater(water.Mesh.bounds.min.y, floor.Mesh.bounds.max.y);
        }

        [TestCase("cliff", 1.50f, .20f)]
        [TestCase("rim", .60f, .18f)]
        public void GrayMassesUseBroadJoiningStrataAndOrderedHeightGrades(string family, float firstHeight, float step)
        {
            var kit = GinmereVoxelLibrary.Load();
            Vector2 ground = kit.Find(GinmereVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            Vector2? sharedTop = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId(family, variant));
                Assert.AreEqual(firstHeight + step * variant, entry.Mesh.bounds.max.y, .0001f);
                Assert.LessOrEqual(entry.Mesh.vertexCount, 48);
                Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count());
                foreach (var level in entry.Mesh.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
                {
                    Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f);
                    Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                    Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f);
                    Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
                }
                var vertices = entry.Mesh.vertices; var uvs = entry.Mesh.uv;
                var top = Enumerable.Range(0, vertices.Length).Where(i => Mathf.Abs(vertices[i].y - entry.Mesh.bounds.max.y) < .0001f)
                    .Select(i => uvs[i]).Distinct().ToArray();
                Assert.AreEqual(1, top.Length);
                Assert.AreNotEqual(ground, top[0]);
                if (sharedTop.HasValue) Assert.AreEqual(sharedTop.Value, top[0]);
                sharedTop = top[0];
            }
        }

        [Test]
        public void WalkableLedgesStayBroadAndLowerThanBlockingRims()
        {
            var kit = GinmereVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var ledge = kit.Find(GinmereVoxelLibrary.ModelId("ledge", variant));
                var rim = kit.Find(GinmereVoxelLibrary.ModelId("rim", variant));
                Assert.LessOrEqual(ledge.Mesh.bounds.max.y, .25f);
                Assert.GreaterOrEqual(ledge.Mesh.bounds.size.x, .9f);
                Assert.GreaterOrEqual(ledge.Mesh.bounds.size.z, .9f);
                Assert.Less(ledge.Mesh.bounds.max.y, rim.Mesh.bounds.max.y * .5f);
            }
        }

        [Test]
        public void NestContainsPaleEggClustersWithinALowDarkerRim()
        {
            var kit = GinmereVoxelLibrary.Load();
            Vector2 pale = new Vector2((19 % 16 + .5f) / 16f, (19 / 16 + .5f) / 8f);
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId("nest", variant));
                Assert.LessOrEqual(entry.Mesh.bounds.max.y, .40f);
                Assert.Greater(entry.Mesh.bounds.size.x, .70f);
                var vertices = entry.Mesh.vertices; var uvs = entry.Mesh.uv;
                var eggs = Enumerable.Range(0, vertices.Length).Where(i => uvs[i] == pale).Select(i => vertices[i]).ToArray();
                Assert.IsNotEmpty(eggs);
                Assert.IsTrue(eggs.All(v => Mathf.Abs(v.x) < .30f && Mathf.Abs(v.z) < .30f));
                Assert.Less(eggs.Max(v => v.y), entry.Mesh.bounds.max.y);
                Assert.AreEqual(2, uvs.Distinct().Count());
            }
        }

        [Test]
        public void PricklebrowHasALongTailAmberThroatAndPairedRaisedBrows()
        {
            var kit = GinmereVoxelLibrary.Load();
            Vector2 amber = new Vector2((21 % 16 + .5f) / 16f, (21 / 16 + .5f) / 8f);
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId("gecko", variant));
                var vertices = entry.Mesh.vertices; var uvs = entry.Mesh.uv;
                Assert.Greater(entry.Mesh.bounds.size.z, .80f);
                Assert.Less(entry.Mesh.bounds.max.y, .55f);
                var tail = vertices.Where(v => v.z < -.35f).ToArray();
                Assert.IsNotEmpty(tail);
                Assert.Less(tail.Max(v => v.x) - tail.Min(v => v.x), .20f);
                Assert.IsTrue(vertices.Any(v => v.x < -.15f && Mathf.Abs(v.z) < .25f));
                Assert.IsTrue(vertices.Any(v => v.x > .15f && Mathf.Abs(v.z) < .25f));
                var throat = Enumerable.Range(0, vertices.Length).Where(i => uvs[i] == amber).Select(i => vertices[i]).ToArray();
                Assert.IsNotEmpty(throat); Assert.IsTrue(throat.All(v => v.z > .12f));
                var brows = vertices.Where(v => v.y > .30f).ToArray();
                Assert.IsTrue(brows.Any(v => v.x < -.04f && v.z > .12f));
                Assert.IsTrue(brows.Any(v => v.x > .04f && v.z > .12f));
            }
        }

        [Test]
        public void GinFrogHasSquatGoldenBodyWideHaunchesAndPairedEyes()
        {
            var kit = GinmereVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId("frog", variant));
                var vertices = entry.Mesh.vertices;
                Assert.That(entry.Mesh.bounds.max.y, Is.InRange(.35f, .70f));
                Assert.Greater(entry.Mesh.bounds.size.x, .60f);
                Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count());
                Assert.IsTrue(vertices.Any(v => v.x < -.30f && v.z < 0 && v.y < .25f));
                Assert.IsTrue(vertices.Any(v => v.x > .30f && v.z < 0 && v.y < .25f));
                var eyes = vertices.Where(v => v.y >= entry.Mesh.bounds.max.y - .04f).ToArray();
                Assert.IsTrue(eyes.Any(v => v.x < -.05f)); Assert.IsTrue(eyes.Any(v => v.x > .05f));
            }
        }

        [Test]
        public void AnchorIsAnIronPinWithAShortAttachedRopeNotALadder()
        {
            var kit = GinmereVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(GinmereVoxelLibrary.ModelId("anchor", variant));
                Assert.That(entry.Mesh.bounds.max.y, Is.InRange(.40f, .80f));
                Assert.LessOrEqual(entry.Mesh.vertexCount, 120);
                Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count());
                Assert.Less(entry.Mesh.bounds.size.x, .70f);
                Assert.Less(entry.Mesh.bounds.size.z, .70f);
            }
        }

        [TestCase("torch", .45f, .90f)]
        [TestCase("meat", .06f, .25f)]
        [TestCase("tonic", .25f, .60f)]
        public void DroppedExpeditionSuppliesHaveSmallDistinctPortableSilhouettes(string family, float low, float high)
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = GinmereVoxelLibrary.Load().Find(GinmereVoxelLibrary.ModelId(family, variant));
                Assert.That(entry.Mesh.bounds.max.y, Is.InRange(low, high));
                Assert.Less(entry.Mesh.bounds.size.x, .70f);
                Assert.Less(entry.Mesh.bounds.size.z, .70f);
                Assert.LessOrEqual(entry.Mesh.vertexCount, 120);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ColdHazardsAreDistinctLowRimeAperturesAndJoinedPaleSheets(int variant)
        {
            var kit = GinmereVoxelLibrary.Load();
            var frost = kit.Find(GinmereVoxelLibrary.ModelId("frost", variant));
            var ice = kit.Find(GinmereVoxelLibrary.ModelId("ice", variant));
            Assert.AreEqual(frost.Id, kit.Entries[48 + variant].Id);
            Assert.AreEqual(ice.Id, kit.Entries[52 + variant].Id);
            Assert.AreEqual("ginmere-tonic-3", kit.Entries[47].Id, "The earlier forty-eight model identities remain in place.");
            Assert.That(frost.Mesh.bounds.max.y, Is.InRange(.20f, .45f));
            Assert.LessOrEqual(frost.Mesh.vertexCount, 144);
            Assert.AreEqual(2, frost.Mesh.uv.Distinct().Count());
            Vector2 dark = new Vector2((12 % 16 + .5f) / 16f, (12 / 16 + .5f) / 8f);
            var vertices = frost.Mesh.vertices; var uvs = frost.Mesh.uv;
            var aperture = Enumerable.Range(0, vertices.Length).Where(i => uvs[i] == dark).Select(i => vertices[i]).ToArray();
            Assert.IsNotEmpty(aperture);
            Assert.IsTrue(aperture.All(v => Mathf.Abs(v.x) <= .20f && Mathf.Abs(v.z) <= .20f));
            Assert.Less(aperture.Max(v => v.y), frost.Mesh.bounds.max.y - .12f);
            var raised = vertices.Where(v => v.y > aperture.Max(a => a.y) + .12f).ToArray();
            Assert.IsNotEmpty(raised);
            Assert.IsTrue(raised.All(v => Mathf.Abs(v.x) > .20f || Mathf.Abs(v.z) > .20f), "The vent opening cannot be capped by a solid pale cube.");
            Assert.Less(ice.Mesh.bounds.max.y, .08f);
            Assert.AreNotEqual(ice.Mesh.uv[0], kit.Find(GinmereVoxelLibrary.ModelId("water", variant)).Mesh.uv[0]);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StalagmitesTaperAboveTheGroundAndBoneCachesRemainLowHollows(int variant)
        {
            var kit = GinmereVoxelLibrary.Load();
            var stone = kit.Find(GinmereVoxelLibrary.ModelId("stalagmite", variant));
            var cache = kit.Find(GinmereVoxelLibrary.ModelId("cache", variant));
            Assert.AreEqual(stone.Id, kit.Entries[56 + variant].Id);
            Assert.AreEqual(cache.Id, kit.Entries[60 + variant].Id);
            Assert.That(stone.Mesh.bounds.max.y, Is.InRange(1.1f, 1.8f));
            Assert.LessOrEqual(stone.Mesh.vertexCount, 120);
            var top = stone.Mesh.vertices.Where(v => v.y > stone.Mesh.bounds.max.y - .04f).ToArray();
            Assert.Less(top.Max(v => v.x) - top.Min(v => v.x), stone.Mesh.bounds.size.x * .5f);
            Assert.Less(top.Max(v => v.z) - top.Min(v => v.z), stone.Mesh.bounds.size.z * .5f);
            Assert.That(cache.Mesh.bounds.max.y, Is.InRange(.30f, .65f));
            Assert.Greater(cache.Mesh.bounds.size.x, .65f);
            Assert.AreEqual(2, cache.Mesh.uv.Distinct().Count());
            Assert.Less(cache.Mesh.bounds.max.y, stone.Mesh.bounds.max.y * .5f);
        }

        [TestCase("FrostVent")]
        [TestCase("IceSheet")]
        public void ColdHazardArtDoesNotInventLiquidOrDestructionParts(string blueprint)
        {
            var owner = GrovelandsCompositionTests.Factory().CreateEntity(blueprint);
            Assert.NotNull(owner);
            Assert.NotNull(owner.GetPart<ThermalPart>());
            Assert.NotNull(owner.GetPart<TileStateSourcePart>());
            Assert.IsNull(owner.GetPart<LiquidPoolPart>());
            Assert.IsNull(owner.GetPart<DestructiblePart>());
        }

        [Test]
        public void BoneCacheRemainsTheNativeFiveItemSolidContainer()
        {
            var owner = GrovelandsCompositionTests.Factory().CreateEntity("BoneCache");
            Assert.AreEqual(5, owner.GetPart<ContainerPart>().MaxItems);
            Assert.IsTrue(owner.GetPart<PhysicsPart>().Solid);
            Assert.IsNull(owner.GetPart<DestructiblePart>());
            Assert.AreEqual("cache", GinmereVoxelLibrary.Family(owner.BlueprintName));
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
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = GinmereVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var entry = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = entry; break;
                    case "missing": copy.Entries = copy.Entries.Take(64 - 1).ToArray(); break;
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
