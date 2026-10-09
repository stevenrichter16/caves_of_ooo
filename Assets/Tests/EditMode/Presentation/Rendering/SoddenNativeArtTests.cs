using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Public visual contracts written before the additive art library.
    /// Source geometry/asset checks complement, not replace, native image review.</summary>
    public sealed class SoddenNativeArtTests
    {
        const string ResourcePath = "SoddenNativeArt3D/Library";
        static readonly string[] Families = { "ground", "mire", "peat", "snag", "boards", "body", "reeds" };
        static Type KitType()
        {
            var type = typeof(SpawnRing3DLibrary).Assembly.GetType("CavesOfOoo.Rendering.SoddenNativeArtLibrary");
            Assert.NotNull(type, "The new Sodden art must be a build-visible additive library.");
            return type;
        }
        static object Load()
        {
            var type = KitType(); var kit = Resources.Load(ResourcePath, type);
            Assert.NotNull(kit, "Import the reproducible new Sodden kit before GREEN.");
            type.GetMethod("Validate").Invoke(kit, null);
            return kit;
        }
        static T Field<T>(object value, string name) => (T)value.GetType().GetField(name).GetValue(value);
        static string Id(string family, int variant) => (family == "reeds" ? "spread-" : "sodden-") + family + "-" + variant;
        static object Find(object kit, string id)
        {
            var entry = KitType().GetMethod("Find").Invoke(kit, new object[] { id });
            Assert.NotNull(entry, id); return entry;
        }
        static Mesh MeshFor(object kit, string id) => Field<Mesh>(Find(kit, id), "Mesh");

        [Test]
        public void AdditiveKitCoversAllLegacySoddenShapesReedsAndTwoGreatdewForms()
        {
            var kit = Load(); var entries = Field<Array>(kit, "Entries");
            var ids = entries.Cast<object>().Select(e => Field<string>(e, "Id")).ToArray();
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), "No duplicate native model mapping.");
            foreach (string family in Families) for (int i = 0; i < 4; i++) Find(kit, Id(family, i));
            Find(kit, "sodden-native-greatdew-0"); Find(kit, "sodden-native-greatdew-1");
            Assert.GreaterOrEqual(entries.Length, 30);
            Assert.Null(KitType().GetMethod("Find").Invoke(kit, new object[] { "invented-sodden-model" }));
            Assert.Null(KitType().GetMethod("Find").Invoke(kit, new object[] { null }));
        }

        [Test]
        public void NewPaletteIsPrivateAndRetainsNativeFogAndTransientControls()
        {
            var kit = Load(); var material = Field<Material>(kit, "Material");
            Assert.NotNull(material); Assert.NotNull(material.shader);
            Assert.AreNotSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial, material);
            Assert.AreNotSame(ReferenceGladeVoxelLibrary.Load().Material, material);
            Assert.IsTrue(material.HasProperty("_FogLight")); Assert.IsTrue(material.HasProperty("_Transient"));
            var palette = material.GetTexture("_BaseMap") as Texture2D;
            Assert.NotNull(palette); Assert.IsTrue(palette.isReadable); Assert.AreEqual(1, palette.height);
            Assert.That(palette.width, Is.InRange(24, 32)); Assert.AreEqual(FilterMode.Point, palette.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, palette.wrapMode);
            var ground = MeshFor(kit, "sodden-ground-0"); var water = MeshFor(kit, "sodden-mire-0");
            Color groundColor = palette.GetPixel(Mathf.FloorToInt(ground.uv[0].x * palette.width), 0);
            Color waterColor = palette.GetPixel(Mathf.FloorToInt(water.uv[0].x * palette.width), 0);
            Assert.Greater(groundColor.g, groundColor.r * 1.3f, "Tea/teal land must replace olive-yellow land.");
            Assert.Less(groundColor.maxColorComponent, .32f, "Land remains dark at source, before native lighting.");
            Assert.Less(waterColor.grayscale, groundColor.grayscale * .85f, "Mire reads darker than land.");
        }

        [TestCase("ground", 120)] [TestCase("mire", 96)] [TestCase("peat", 576)]
        [TestCase("snag", 720)] [TestCase("boards", 384)] [TestCase("body", 576)] [TestCase("reeds", 600)]
        public void StaticFormsAreMeasuredCombinedMeshesInsideNativeCells(string family, int triangles)
        {
            var kit = Load(); var material = Field<Material>(kit, "Material");
            for (int i = 0; i < 4; i++)
            {
                string id = Id(family, i); var e = Find(kit, id); var mesh = Field<Mesh>(e, "Mesh");
                var prefab = Field<GameObject>(e, "Prefab"); var spec = Field<SpawnRing3DCatalog.Model>(e, "Spec");
                Assert.NotNull(mesh); Assert.IsTrue(mesh.isReadable); Assert.Greater(mesh.vertexCount, 24);
                Assert.LessOrEqual(mesh.GetIndexCount(0) / 3, (uint)triangles, id);
                Assert.AreEqual(1, mesh.subMeshCount); Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
                Assert.GreaterOrEqual(mesh.bounds.min.x, -.5001f); Assert.LessOrEqual(mesh.bounds.max.x, .5001f);
                Assert.GreaterOrEqual(mesh.bounds.min.z, -.5001f); Assert.LessOrEqual(mesh.bounds.max.z, .5001f);
                Assert.AreSame(mesh, prefab.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(material, prefab.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreEqual(1, prefab.GetComponentsInChildren<Renderer>(true).Length);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
                Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.IsEmpty(prefab.GetComponentsInChildren<Light>(true));
                Assert.AreEqual(Vector3.zero, prefab.transform.localPosition);
                Assert.AreEqual(Quaternion.identity, prefab.transform.localRotation);
                Assert.AreEqual(Vector3.one, prefab.transform.localScale);
                Assert.AreEqual(id, spec.id); StringAssert.StartsWith("Assets/Resources/SoddenNativeArt3D/", spec.path);
                Assert.AreEqual(mesh.bounds.center, spec.boundsCenter); Assert.AreEqual(mesh.bounds.size, spec.boundsSize);
                Assert.AreEqual((int)mesh.GetIndexCount(0) / 3, spec.triangles);
            }
        }

        [TestCase("ground")] [TestCase("mire")] [TestCase("peat")] [TestCase("snag")]
        [TestCase("boards")] [TestCase("body")] [TestCase("reeds")]
        public void FourVariantsChangeExposedGeometryNotOnlyBuriedThickness(string family)
        {
            var kit = Load(); var visible = new string[4];
            for (int i = 0; i < 4; i++)
            {
                var mesh = MeshFor(kit, Id(family, i));
                visible[i] = string.Join(";", mesh.vertices.Where(v => v.y > .001f)
                    .Select(v => v.ToString("F4")));
                Assert.IsNotEmpty(visible[i], "Exposed detail is required for " + family);
            }
            Assert.AreEqual(4, visible.Distinct().Count());
        }

        [Test]
        public void ReedsSnagsAndBodiesHaveReadableThreeDimensionalSilhouettes()
        {
            var kit = Load();
            for (int i = 0; i < 4; i++)
            {
                var reeds = MeshFor(kit, Id("reeds", i));
                Assert.That(reeds.bounds.size.y, Is.InRange(.48f, .96f));
                Assert.GreaterOrEqual(reeds.bounds.size.x, .60f);
                Assert.GreaterOrEqual(reeds.bounds.size.z, .48f);
                Assert.GreaterOrEqual(reeds.uv.Distinct().Count(), 3);
                var snag = MeshFor(kit, Id("snag", i));
                Assert.That(snag.bounds.size.y, Is.InRange(2.7f, 3.25f));
                var crown = snag.vertices.Where(v => v.y > 1.6f).ToArray();
                Assert.GreaterOrEqual(crown.Max(v => v.x) - crown.Min(v => v.x), .70f);
                Assert.GreaterOrEqual(snag.uv.Distinct().Count(), 3);
                var body = MeshFor(kit, Id("body", i));
                Assert.GreaterOrEqual(body.vertexCount, 18 * 24, "A recognizably articulated prone body requires fuller geometry.");
                Assert.That(body.bounds.size.y, Is.InRange(.18f, .50f));
                Assert.GreaterOrEqual(body.bounds.size.z, .74f); Assert.GreaterOrEqual(body.uv.Distinct().Count(), 3);
            }
        }

        [TestCase(0)] [TestCase(1)]
        public void GreatdewIsAnInertHookedPlantWithVisibleClearBeads(int variant)
        {
            var kit = Load(); var entry = Find(kit, "sodden-native-greatdew-" + variant);
            var mesh = Field<Mesh>(entry, "Mesh"); var prefab = Field<GameObject>(entry, "Prefab");
            var spec = Field<SpawnRing3DCatalog.Model>(entry, "Spec");
            Assert.AreEqual("Greatdew", spec.sourceBlueprint); Assert.AreEqual("entity", spec.kind);
            Assert.That(mesh.bounds.size.y, Is.InRange(.65f, 1.10f));
            Assert.LessOrEqual(mesh.bounds.size.x, 1f); Assert.LessOrEqual(mesh.bounds.size.z, 1f);
            Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 4, "Dark base, jade stem, pale tip and clear bead highlights.");
            Assert.That(mesh.vertexCount, Is.InRange(20 * 24, 60 * 24));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true));
            Assert.IsFalse(spec.rigged); Assert.AreEqual("none", spec.rigFamily);
        }

        [TestCase("missing")] [TestCase("duplicate")] [TestCase("foreign-material")]
        public void MalformedLibraryIsRejectedWithoutChangingBorrowedAssets(string corruption)
        {
            var source = (ScriptableObject)Load(); var copy = UnityEngine.Object.Instantiate(source);
            try
            {
                var type = KitType(); var entries = Field<Array>(copy, "Entries");
                if (corruption == "missing")
                {
                    var shorter = Array.CreateInstance(entries.GetType().GetElementType(), entries.Length - 1);
                    Array.Copy(entries, shorter, shorter.Length); type.GetField("Entries").SetValue(copy, shorter);
                }
                else if (corruption == "duplicate") entries.SetValue(entries.GetValue(0), 1);
                else type.GetField("Material").SetValue(copy, Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial);
                var error = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Validate").Invoke(copy, null));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
                Assert.DoesNotThrow(() => type.GetMethod("Validate").Invoke(source, null));
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void PeatShowsHorizontalStrataOnBothFacesInsteadOfLongGreenSlats(int variant)
        {
            var mesh = MeshFor(Load(), Id("peat", variant));
            var vertices = mesh.vertices; var uv = mesh.uv; int near = 0, far = 0, moss = 0;
            for (int i = 0; i < vertices.Length; i += 24)
            {
                var bounds = CuboidBounds(vertices, i); int paint = Mathf.RoundToInt(uv[i].x * 24 - .5f);
                if (paint == 12 || paint == 13)
                {
                    moss++;
                    Assert.LessOrEqual(Mathf.Max(bounds.size.x, bounds.size.z), .3401f,
                        "Moss caps need short irregular patches, not full-depth green vent slats.");
                }
                if ((paint == 7 || paint == 8) && bounds.size.x > .18f && bounds.size.y <= .201f)
                {
                    if (bounds.center.z < -.43f) near++;
                    if (bounds.center.z > .43f) far++;
                }
            }
            Assert.GreaterOrEqual(moss, 2); Assert.GreaterOrEqual(near, 4, "Native camera sees the near cut face.");
            Assert.GreaterOrEqual(far, 4, "Opposing banks must remain legible without presentation rotation tricks.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void DuckboardsShowThreeContinuousCrosswisePlanksInsteadOfParquet(int variant)
        {
            var mesh = MeshFor(Load(), Id("boards", variant)); var vertices = mesh.vertices; var uv = mesh.uv;
            int longBoards = 0;
            for (int i = 0; i < vertices.Length; i += 24)
            {
                var bounds = CuboidBounds(vertices, i); int paint = Mathf.RoundToInt(uv[i].x * 24 - .5f);
                if ((paint == 17 || paint == 18) && bounds.size.z > .84f && bounds.size.x > .20f
                    && bounds.size.x < .31f && bounds.max.y > .12f) longBoards++;
            }
            Assert.AreEqual(3, longBoards, "Three unbroken wood silhouettes should cross the direction of travel.");
        }

        [Test]
        public void SnagVariantsIncludeBothLeftAndRightHeavyBrokenCrowns()
        {
            var kit = Load(); int leftHeavy = 0, rightHeavy = 0;
            for (int i = 0; i < 4; i++)
            {
                var vertices = MeshFor(kit, Id("snag", i)).vertices;
                float left = vertices.Where(v => v.x < -.27f).Max(v => v.y);
                float right = vertices.Where(v => v.x > .27f).Max(v => v.y);
                if (left > right + .10f) leftHeavy++;
                if (right > left + .10f) rightHeavy++;
            }
            Assert.GreaterOrEqual(leftHeavy, 1, "Height jitter alone leaves every tree with the same cactus silhouette.");
            Assert.GreaterOrEqual(rightHeavy, 1);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void PeatTopUsesBroadBrokenLedgesInsteadOfThreeParallelCapColumns(int variant)
        {
            var mesh = MeshFor(Load(), Id("peat", variant)); var vertices = mesh.vertices; var uv = mesh.uv;
            int columns = 0, broadLedges = 0, moss = 0;
            for (int i = 0; i < vertices.Length; i += 24)
            {
                var bounds = CuboidBounds(vertices, i); int paint = Mathf.RoundToInt(uv[i].x * 24 - .5f);
                if (paint == 12 || paint == 13) moss++;
                if ((paint == 7 || paint == 8) && bounds.min.y > .85f)
                {
                    if (bounds.size.z > .70f && bounds.size.x < .40f && bounds.size.y > .08f) columns++;
                    if (bounds.size.x > .40f && bounds.size.z > .30f) broadLedges++;
                }
            }
            Assert.Zero(columns, "Three full-depth cap columns still look like rows of seats in the native camera.");
            Assert.GreaterOrEqual(broadLedges, 2, "Keep broad exposed uneven top ledges, not an empty flat box.");
            Assert.That(moss, Is.InRange(2, 4), "Use sparse staggered growth, not the previous six-patch matrix.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void VineWallHasAFullJadeThicketRatherThanTheSharedChartreuseWall(int variant)
        {
            var kit = Load(); var mesh = NativeOutlierMesh(kit, "ring-vine-wall-" + variant, "VineWall", 40);
            Assert.That(mesh.bounds.size.y, Is.InRange(1.1f, 1.5f));
            Assert.GreaterOrEqual(mesh.bounds.size.x, .75f); Assert.GreaterOrEqual(mesh.bounds.size.z, .75f);
            Assert.GreaterOrEqual(mesh.vertexCount, 16 * 24, "A blocking thicket must not resemble one harmless reed.");
            Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 4);
            Assert.IsTrue(mesh.uv.Any(at => Mathf.RoundToInt(at.x * 24 - .5f) == 12), "Dark rooted leaf mass.");
            Assert.IsTrue(mesh.uv.Any(at => Mathf.RoundToInt(at.x * 24 - .5f) == 14), "Readable jade tips.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void BrineRemainsCyanAndMineralMarkedInsteadOfDarkMire(int variant)
        {
            var kit = Load(); var mesh = NativeOutlierMesh(kit, "ring-brine-pool-" + variant, "BrinePool", 16);
            Assert.That(mesh.bounds.max.y, Is.InRange(.05f, .16f));
            Assert.GreaterOrEqual(mesh.bounds.size.x, .99f); Assert.GreaterOrEqual(mesh.bounds.size.z, .99f);
            var palette = (Texture2D)Field<Material>(kit, "Material").GetTexture("_BaseMap");
            var liquid = PaletteColor(mesh, palette, 0); var mire = PaletteColor(MeshFor(kit, "sodden-mire-0"), palette, 0);
            Assert.Greater(liquid.g, liquid.r); Assert.Greater(liquid.b, liquid.r, "Brine keeps its cyan cue.");
            Assert.Greater(liquid.grayscale, mire.grayscale + .20f, "Distinct brine and mire owners must retain different liquid cues.");
            Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 3);
            Assert.IsTrue(mesh.uv.Any(at => Mathf.RoundToInt(at.x * 24 - .5f) == 23), "Pale mineral traces identify brine.");
        }

        [Test]
        public void AcidKeepsReadableGreenWarningAndRaisedFoamInsteadOfSafeWaterPaint()
        {
            var kit = Load(); var mesh = NativeOutlierMesh(kit, "density-pool-acid", "AcidPool", 16);
            var palette = (Texture2D)Field<Material>(kit, "Material").GetTexture("_BaseMap");
            var liquid = PaletteColor(mesh, palette, 0); var mire = PaletteColor(MeshFor(kit, "sodden-mire-0"), palette, 0);
            Assert.Greater(liquid.g, liquid.r * 1.4f); Assert.Greater(liquid.g, liquid.b * 1.4f);
            Assert.Greater(liquid.grayscale, mire.grayscale + .20f);
            Assert.That(mesh.bounds.max.y, Is.InRange(.055f, .17f));
            Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 3);
            Assert.IsTrue(mesh.uv.Any(at => Mathf.RoundToInt(at.x * 24 - .5f) == 15), "Contrasting warning foam remains visible.");
        }

        [Test]
        public void SteamVentShowsAnOpenMineralMouthWithoutInventedGasOrLights()
        {
            var kit = Load(); var mesh = NativeOutlierMesh(kit, "ring-steam-vent", "SteamVent", 28);
            Assert.That(mesh.bounds.size.y, Is.InRange(.35f, .90f));
            Assert.That(mesh.bounds.size.x, Is.InRange(.45f, .85f));
            Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 4);
            var points = mesh.vertices; var uv = mesh.uv; bool mouth = false, paleMineral = false;
            for (int i = 0; i < points.Length; i += 24)
            {
                var bounds = CuboidBounds(points, i); int paint = Mathf.RoundToInt(uv[i].x * 24 - .5f);
                if (paint == 4 && bounds.size.x >= .20f && bounds.size.z >= .20f) mouth = true;
                if ((paint == 11 || paint == 23) && bounds.max.y > .20f) paleMineral = true;
            }
            Assert.IsTrue(mouth); Assert.IsTrue(paleMineral);
        }

        static Mesh NativeOutlierMesh(object kit, string id, string blueprint, int maxCuboids)
        {
            var entry = Find(kit, id); var mesh = Field<Mesh>(entry, "Mesh");
            var prefab = Field<GameObject>(entry, "Prefab"); var spec = Field<SpawnRing3DCatalog.Model>(entry, "Spec");
            Assert.AreEqual(blueprint, spec.sourceBlueprint); Assert.AreEqual("entity", spec.kind);
            Assert.That(mesh.vertexCount, Is.InRange(2 * 24, maxCuboids * 24));
            Assert.AreSame(Field<Material>(kit, "Material"), prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Light>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true));
            Assert.LessOrEqual(Mathf.Max(Mathf.Abs(mesh.bounds.min.x), Mathf.Abs(mesh.bounds.max.x)), .5001f);
            Assert.LessOrEqual(Mathf.Max(Mathf.Abs(mesh.bounds.min.z), Mathf.Abs(mesh.bounds.max.z)), .5001f);
            return mesh;
        }
        static Color PaletteColor(Mesh mesh, Texture2D palette, int vertex)
            => palette.GetPixel(Mathf.FloorToInt(mesh.uv[vertex].x * palette.width), 0);

        static Bounds CuboidBounds(Vector3[] points, int first)
        {
            var bounds = new Bounds(points[first], Vector3.zero);
            for (int i = first + 1; i < first + 24; i++) bounds.Encapsulate(points[i]);
            return bounds;
        }
    }
}
