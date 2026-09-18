using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Full exporter-manifest mutations against fresh actual native data.
    /// The production art manifest is a stable project asset, never a /tmp fixture.
    /// Parser tests verify metadata contracts; imported meshes/animations need their
    /// own import and native checks.</summary>
    public sealed class Village3DManifestNativeContractTests
    {
        private const string ManifestPath = "Assets/Art3D/Village/Definitions/manifest.json";
        private static Village3DManifest Load(out MorrowfastSceneDefinition native)
        {
            var art = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            Assert.NotNull(art, "Import the actual complete exporter manifest at " + ManifestPath + " before this RED gate.");
            var source = Resources.Load<TextAsset>("SceneArt/Morrowfast/definition");
            Assert.NotNull(source);
            // Do not mutate MorrowfastSceneDefinition.Load's process-wide cache.
            native = MorrowfastSceneDefinition.Parse(source.text);
            var manifest = Village3DManifest.Parse(art.text, native);
            Assert.AreEqual(native.owners.Length, manifest.owners.Length);
            Assert.AreEqual(native.buildings.Length, manifest.buildings.Length);
            return manifest;
        }
        [Test] public void ActualCompleteKitMatchesNativeOwnersRoomsAndModelMetadata()
        {
            var manifest = Load(out var native);
            Assert.DoesNotThrow(() => manifest.Validate(native));
            Assert.Greater(manifest.models.Length, 0);
            Assert.IsTrue(manifest.models.Any(m => m.rigged && m.clips.Length > 0 && m.sockets.Length > 0));
        }
        [Test] public void FootprintAndBuildingOrderAreNotPartOfTheGameplayContract()
        {
            var manifest = Load(out var native);
            foreach (var owner in manifest.owners) Array.Reverse(owner.footprint);
            Array.Reverse(manifest.buildings); Array.Reverse(native.buildings);
            Assert.DoesNotThrow(() => manifest.Validate(native));
        }
        [TestCase("unknown-visibility")]
        [TestCase("outdoor-room-open")]
        [TestCase("interior-owner-visible")]
        [TestCase("native-mutability")]
        [TestCase("missing-native-footprint")]
        [TestCase("shifted-native-footprint")]
        [TestCase("native-door-mapping")]
        [TestCase("native-roof-mapping")]
        [TestCase("zero-triangles")]
        [TestCase("duplicate-animation-clip")]
        [TestCase("blank-equipment-socket")]
        [TestCase("rigged-without-clips")]
        public void MeaningfulMetadataDriftIsRejectedBeforeViewsBind(string mutation)
        {
            var manifest = Load(out var native); // positive control before one mutation
            switch (mutation)
            {
                case "unknown-visibility": manifest.owners[0].visibleWhen = "always-and-through-walls"; break;
                case "outdoor-room-open": manifest.owners.First(o => string.IsNullOrEmpty(o.roomId)).visibleWhen = "room-open"; break;
                case "interior-owner-visible": manifest.owners.First(o => o.visibleWhen == "room-open").visibleWhen = "owner-visible"; break;
                case "native-mutability": manifest.owners[0].mutable = !manifest.owners[0].mutable; break;
                case "missing-native-footprint": manifest.owners.First(o => o.footprint.Length > 0).footprint = Array.Empty<Village3DManifest.CellPoint>(); break;
                case "shifted-native-footprint":
                    var point = manifest.owners.First(o => o.footprint.Length == 1).footprint[0]; point.x = (point.x + 1) % Zone.Width; break;
                case "native-door-mapping":
                    string oldDoor = native.buildings[0].doorId; native.buildings[0].doorId = native.buildings[1].doorId; native.buildings[1].doorId = oldDoor;
                    native.Validate(); break;
                case "native-roof-mapping":
                    string oldRoof = native.buildings[0].roofId; native.buildings[0].roofId = native.buildings[1].roofId; native.buildings[1].roofId = oldRoof;
                    native.Validate(); break;
                case "zero-triangles": manifest.models[0].triangles = 0; break;
                case "duplicate-animation-clip":
                    var animated = manifest.models.First(m => m.clips != null && m.clips.Length > 0); animated.clips = animated.clips.Concat(new[]{animated.clips[0]}).ToArray(); break;
                case "blank-equipment-socket": manifest.models.First(m => m.sockets != null && m.sockets.Length > 0).sockets[0] = " "; break;
                case "rigged-without-clips": manifest.models.First(m => m.rigged).clips = Array.Empty<string>(); break;
                default: Assert.Fail("Unknown mutation"); break;
            }
            Assert.Throws<ArgumentException>(() => manifest.Validate(native), mutation);
        }
    }
}
