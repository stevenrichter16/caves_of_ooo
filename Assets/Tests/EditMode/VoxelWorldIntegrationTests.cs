using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>The four-chunk adapter is presentation only. Reflection records a
    /// runnable missing-contract RED before the implementation exists.</summary>
    public sealed class VoxelWorldIntegrationTests
    {
        bool oldEnabled;
        [SetUp] public void SetUp() { oldEnabled = Village3DSettings.Enabled; }
        [TearDown] public void TearDown() { Village3DSettings.Enabled = oldEnabled; }
        static Type Adapter()
        {
            var type = typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.VoxelWorldPresentation");
            Assert.NotNull(type, "Missing four-chunk voxel presentation adapter.");
            return type;
        }
        static object Static(string name, Type argumentType, object value)
        {
            var method = Adapter().GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, new[] { argumentType }, null);
            Assert.NotNull(method, name);
            try { return method.Invoke(null, new[] { value }); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        [TestCase("Overworld.2.6.0")]
        [TestCase("Overworld.3.6.0")]
        [TestCase("Overworld.3.7.0")]
        [TestCase("Overworld.4.7.0")]
        public void ExactlyTheFourConnectedNativeChunksAreSupported(string id)
        {
            Assert.IsTrue((bool)Static("IsSupported", typeof(string), id));
            Village3DSettings.Enabled = true;
            Assert.IsTrue((bool)Static("IsEnabledFor", typeof(Zone), new Zone(id)));
            Village3DSettings.Enabled = false;
            Assert.IsFalse((bool)Static("IsEnabledFor", typeof(Zone), new Zone(id)), "ASCII preference must remain authoritative.");
            Assert.IsTrue((bool)Static("IsSupported", typeof(string), id), "Content support must not change with a display preference.");
        }

        [TestCase(null)]
        [TestCase("")]
        // The ordinary Stump and Stillleaf's first three levels have voxel
        // authority; deeper Stillleaf, the Root, and other authored sites remain
        // excluded. Ginmere's deeper catacombs also stay outside its composition.
        [TestCase("Overworld.2.4.3")]
        [TestCase("Overworld.3.5.0")]
        [TestCase("Overworld.3.3.0")]
        [TestCase("Overworld.4.6.3")]
        [TestCase("Overworld.2.7.3")]
        [TestCase("Overworld.10.10.0")]
        [TestCase("Overworld.3.6.1")]
        [TestCase("overworld.3.6.0")]
        [TestCase("Overworld.3.6.0.extra")]
        [TestCase(" Overworld.3.6.0")]
        public void AdjacentSitesDepthsAndMalformedNamesDoNotAcquireVoxelAuthority(string id)
        {
            Village3DSettings.Enabled = true;
            Assert.IsFalse((bool)Static("IsSupported", typeof(string), id));
            var zone = id == null ? null : new Zone(id);
            Assert.IsFalse((bool)Static("IsEnabledFor", typeof(Zone), zone));
            Assert.IsNull(Static("ForZone", typeof(Zone), zone), "Unsupported content must retain its existing presenter.");
        }

        [Test] public void ResolvingSupportDoesNotRewriteNativeZoneContentsOrTileState()
        {
            Village3DSettings.Enabled = true;
            var zone = new Zone("Overworld.3.7.0");
            var owner = new Entity { ID = "voxel-authority-control", BlueprintName = "voxel-authority-control" };
            Assert.IsTrue(zone.AddEntity(owner, 5, 6));
            zone.TileState.WriteCoating(5, 6, "water", ZoneTileState.Permanent);
            int version = zone.EntityVersion;
            string tiles = zone.TileState.ToSaveString();
            Assert.IsTrue((bool)Static("IsEnabledFor", typeof(Zone), zone));
            Assert.AreSame(owner, zone.GetAllEntities()[0]);
            Assert.AreEqual(1, zone.GetAllEntities().Count);
            Assert.AreEqual((5, 6), zone.GetEntityPosition(owner));
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(tiles, zone.TileState.ToSaveString());
        }

        [Test] public void AdapterExposesMeshReplacementAndPerBindCoverageDiagnostics()
        {
            var type = Adapter();
            Assert.IsTrue(type.IsSealed);
            var resolve = type.GetMethod("Resolve", new[] { typeof(Mesh) });
            Assert.NotNull(resolve); Assert.AreEqual(typeof(Mesh), resolve.ReturnType);
            var apply = type.GetMethod("Apply", new[] { typeof(GameObject) });
            Assert.NotNull(apply); Assert.AreEqual(typeof(int), apply.ReturnType);
            foreach (string name in new[] { "AppliedMeshCount", "MissingMeshCount" })
            { var property = type.GetProperty(name); Assert.NotNull(property, name); Assert.AreEqual(typeof(int), property.PropertyType); }
            Assert.AreEqual(typeof(string), type.GetProperty("ZoneId")?.PropertyType);
        }

        [TestCase(typeof(Village3DPresenter))]
        [TestCase(typeof(SpawnRing3DPresenter))]
        public void BothExistingPresentersExposeRealVoxelCoverage(Type presenter)
        {
            Assert.AreEqual(typeof(bool), presenter.GetProperty("VoxelPresentationActive")?.PropertyType);
            Assert.AreEqual(typeof(int), presenter.GetProperty("VoxelAppliedMeshCount")?.PropertyType);
            Assert.AreEqual(typeof(int), presenter.GetProperty("VoxelMissingMeshCount")?.PropertyType);
        }
    }
}
