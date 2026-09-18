using System;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Malformed metadata cannot claim unrelated resources or native cells.
    /// These mutations exercise independent gates against the actual exported asset.</summary>
    public sealed class MorrowfastSceneArtAdversarialTests
    {
        private MorrowfastArtDefinition Fresh()
            => MorrowfastArtDefinition.Parse(Resources.Load<TextAsset>("SceneArt/Morrowfast/art-definition").text);

        [TestCase("revision")] [TestCase("identity")] [TestCase("width")] [TestCase("height")]
        [TestCase("scale")] [TestCase("origin")] [TestCase("null-owners")] [TestCase("null-layers")]
        [TestCase("null-rooms")] [TestCase("null-owner")] [TestCase("null-room")] [TestCase("null-layer")]
        [TestCase("owner-anchor")] [TestCase("owner-foot")] [TestCase("owner-foot-infinity")]
        [TestCase("unknown-owner-room")] [TestCase("unknown-layer-room")] [TestCase("room-door")]
        [TestCase("room-roof")] [TestCase("room-polygon-null")] [TestCase("room-polygon-nan")]
        [TestCase("layer-overflow")] [TestCase("layer-nan-depth")] [TestCase("layer-duplicate")]
        public void Adversarial_InvalidArtIsRefusedBeforeSceneOwnership(string mutation)
        {
            var art = Fresh();
            Assert.DoesNotThrow(art.Validate, "The unchanged actual source is the paired control.");
            switch (mutation)
            {
                case "revision": art.revision++; break;
                case "identity": art.id = "another-scene"; break;
                case "width": art.canvasWidth--; break;
                case "height": art.canvasHeight--; break;
                case "scale": art.pixelsPerCell = float.NaN; break;
                case "origin": art.originX = float.PositiveInfinity; break;
                case "null-owners": art.owners = null; break;
                case "null-layers": art.layers = null; break;
                case "null-rooms": art.rooms = null; break;
                case "null-owner": art.owners[0] = null; break;
                case "null-room": art.rooms[0] = null; break;
                case "null-layer": art.layers[0] = null; break;
                case "owner-anchor": art.owners[0].anchorY = 25; break;
                case "owner-foot": art.owners[0].sourceFoot = new[] { 2f }; break;
                case "owner-foot-infinity": art.owners[0].sourceFoot[0] = float.PositiveInfinity; break;
                case "unknown-owner-room": art.owners[0].roomId = "absent"; break;
                case "unknown-layer-room": art.layers[0].roomId = "absent"; break;
                case "room-door": art.rooms[0].doorId = null; break;
                case "room-roof": art.rooms[0].roofId = "absent"; break;
                case "room-polygon-null": art.rooms[0].interiorPolygon[0] = null; break;
                case "room-polygon-nan": art.rooms[0].interiorPolygon[0].x = float.NaN; break;
                case "layer-overflow": art.layers[0].bounds = new[] { 1, 1, int.MaxValue, int.MaxValue }; break;
                case "layer-nan-depth": art.layers[0].z = float.NaN; break;
                case "layer-duplicate": art.layers[1].id = art.layers[0].id; break;
                default: Assert.Fail("Unimplemented mutation"); break;
            }
            Assert.Throws<ArgumentException>(art.Validate);
        }

        [TestCase("SceneArt/Morrowfast/Art/../FellingSite/base")]
        [TestCase("SceneArt/Morrowfast/Art/%2e%2e/base")]
        [TestCase("SceneArt/Morrowfast/Art/other\\base")]
        [TestCase("SceneArt/Morrowfast/Art/C:/base")]
        [TestCase("SceneArt/Morrowfast/Art/")]
        [TestCase("SceneArt/FellingSite/base")]
        public void Adversarial_ResourceNamespacesCannotEscapeTheArtExport(string path)
        {
            var art = Fresh(); Assert.DoesNotThrow(art.Validate);
            art.layers[0].resource = path;
            Assert.Throws<ArgumentException>(art.Validate);
        }
    }
}
