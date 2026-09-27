using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadTransientBudgetTests
    {
        Dictionary<string, GasDefinition> prior;
        bool initialized;
        static FieldInfo Registry => typeof(GasRegistry).GetField("_byId", BindingFlags.NonPublic | BindingFlags.Static);
        static FieldInfo Initialized => typeof(GasRegistry).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp] public void Setup()
        {
            prior = new Dictionary<string, GasDefinition>((Dictionary<string, GasDefinition>)Registry.GetValue(null));
            initialized = (bool)Initialized.GetValue(null);
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/GasDefinitions"), "*.json").Select(File.ReadAllText));
        }

        [TearDown] public void Cleanup()
        {
            var map = (Dictionary<string, GasDefinition>)Registry.GetValue(null);
            map.Clear();
            foreach (var pair in prior) map.Add(pair.Key, pair.Value);
            Initialized.SetValue(null, initialized);
        }

        static NativeZone3DRenderSurface Surface(SpawnRing3DIntegrationFixture f) =>
            ((SpawnRing3DPresenter)f.Presenter).ActiveSurface;

        static void Prepare(SpawnRing3DIntegrationFixture f)
        {
            foreach (var owner in f.Zone.GetReadOnlyEntities().ToArray())
                if (owner.HasPart<GasPoolPart>()) Assert.True(f.Zone.RemoveEntity(owner));
            foreach (var cell in f.Zone.Cells)
            {
                f.Zone.TileState.Clear(cell.X, cell.Y);
                cell.Explored = cell.IsVisible = true;
            }
        }

        static Entity Gas(SpawnRing3DIntegrationFixture f, int index) =>
            GasFactory.SpawnGas(f.Zone, index % Zone.Width, index / Zone.Width, "poison-vapor", 29);

        [TestCase(512)] [TestCase(513)]
        public void GasLimitRetainsHonestFallbackWithoutChangingAnySource(int count)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                var owners = Enumerable.Range(0, count).Select(i => Gas(f, i)).ToArray();
                int version = f.Zone.EntityVersion;
                using (var volumes = new SpreadTransientVolumes(Surface(f), f.Library.WorldMaterial))
                {
                    volumes.Refresh(f.Zone);
                    int shown = owners.Count(o => volumes.TryGetGas(o, out _, out _));
                    Assert.AreEqual(Math.Min(count, SpreadTransientVolumes.MaximumGasViews), shown);
                    Assert.AreEqual(512, SpreadTransientVolumes.MaximumGasViews);
                    Assert.AreEqual(version, f.Zone.EntityVersion);
                    foreach (var owner in owners) Assert.AreEqual(29, owner.GetPart<GasPoolPart>().Density);
                }
            }
        }

        [TestCase(512)] [TestCase(513)]
        public void ElementLimitRetainsHonestFallbackAndExactSourceState(int count)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                for (int i = 0; i < count; i++) f.Zone.TileState.AddHeat(i % Zone.Width, i / Zone.Width, 1);
                string before = f.Zone.TileState.ToSaveString();
                using (var volumes = new SpreadTransientVolumes(Surface(f), f.Library.WorldMaterial))
                {
                    volumes.Refresh(f.Zone);
                    int shown = Enumerable.Range(0, count).Count(i => volumes.TryGetElement(i % Zone.Width, i / Zone.Width, out _, out _));
                    Assert.AreEqual(Math.Min(count, SpreadTransientVolumes.MaximumElementViews), shown);
                    Assert.AreEqual(512, SpreadTransientVolumes.MaximumElementViews);
                    Assert.AreEqual(before, f.Zone.TileState.ToSaveString());
                }
            }
        }

        [TestCase(512)] [TestCase(513)]
        public void ParticleLimitRefusesOnlyExcessCurrentDraws(int count)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                string before = f.Zone.TileState.ToSaveString();
                using (var frame = new SpreadParticleFrame(Surface(f), f.Library.WorldMaterial))
                {
                    frame.BeginFrame(f.Zone);
                    int accepted = 0;
                    for (int i = 0; i < count; i++) if (frame.TryDraw(i % Zone.Width, i / Zone.Width, '*', "&Y")) accepted++;
                    frame.EndFrame();
                    Assert.AreEqual(Math.Min(count, SpreadParticleFrame.MaximumViews), accepted);
                    Assert.AreEqual(512, SpreadParticleFrame.MaximumViews);
                    int shown = Enumerable.Range(0, count).Count(i => frame.TryGet(i % Zone.Width, i / Zone.Width, out _));
                    Assert.AreEqual(accepted, shown);
                    Assert.AreEqual(before, f.Zone.TileState.ToSaveString());
                }
            }
        }

        [Test] public void FullRetiredGasSetAdmitsNewOwnersOnTheSameRefresh()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                var old = Enumerable.Range(0, 512).Select(i => Gas(f, i)).ToArray();
                using (var volumes = new SpreadTransientVolumes(Surface(f), f.Library.WorldMaterial))
                {
                    volumes.Refresh(f.Zone);
                    Assert.AreEqual(512, old.Count(o => volumes.TryGetGas(o, out _, out _)));
                    foreach (var owner in old) Assert.True(f.Zone.RemoveEntity(owner));
                    var current = Enumerable.Range(0, 512).Select(i => Gas(f, i)).ToArray();
                    Assert.Zero(old.Count(o => volumes.TryGetGas(o, out _, out _)));
                    volumes.Refresh(f.Zone);
                    Assert.AreEqual(512, current.Count(o => volumes.TryGetGas(o, out _, out _)),
                        "Retired views cannot occupy the limit during admission of current sources.");
                    Assert.Zero(old.Count(o => volumes.TryGetGas(o, out _, out _)));
                }
            }
        }

        [Test] public void FullRetiredElementSetAdmitsNewCellsOnTheSameRefresh()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                for (int i = 0; i < 512; i++) f.Zone.TileState.AddHeat(i % Zone.Width, i / Zone.Width, 1);
                using (var volumes = new SpreadTransientVolumes(Surface(f), f.Library.WorldMaterial))
                {
                    volumes.Refresh(f.Zone);
                    Assert.AreEqual(512, Enumerable.Range(0, 512).Count(i => volumes.TryGetElement(i % Zone.Width, i / Zone.Width, out _, out _)));
                    for (int i = 0; i < 512; i++) f.Zone.TileState.Clear(i % Zone.Width, i / Zone.Width);
                    for (int i = 700; i < 1212; i++) f.Zone.TileState.AddCold(i % Zone.Width, i / Zone.Width, 1);
                    string before = f.Zone.TileState.ToSaveString();
                    volumes.Refresh(f.Zone);
                    Assert.AreEqual(512, Enumerable.Range(700, 512).Count(i => volumes.TryGetElement(i % Zone.Width, i / Zone.Width, out _, out _)));
                    Assert.Zero(Enumerable.Range(0, 512).Count(i => volumes.TryGetElement(i % Zone.Width, i / Zone.Width, out _, out _)));
                    Assert.AreEqual(before, f.Zone.TileState.ToSaveString());
                }
            }
        }

        [Test] public void FullPriorParticleFrameAdmitsNewCellsBeforeCurrentEndFrame()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                using (var frame = new SpreadParticleFrame(Surface(f), f.Library.WorldMaterial))
                {
                    frame.BeginFrame(f.Zone);
                    for (int i = 0; i < 512; i++) Assert.True(frame.TryDraw(i % Zone.Width, i / Zone.Width, '*', "&Y"));
                    frame.EndFrame();
                    frame.BeginFrame(f.Zone);
                    for (int i = 700; i < 1212; i++) Assert.True(frame.TryDraw(i % Zone.Width, i / Zone.Width, '!', "&Y"), "New current cell " + i);
                    frame.EndFrame();
                    Assert.AreEqual(512, Enumerable.Range(700, 512).Count(i => frame.TryGet(i % Zone.Width, i / Zone.Width, out _)));
                    Assert.Zero(Enumerable.Range(0, 512).Count(i => frame.TryGet(i % Zone.Width, i / Zone.Width, out _)));
                }
            }
        }

        [Test] public void MixedNewAndRetainedParticleCellsNeverRecycleACurrentFrameView()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Prepare(f);
                using (var frame = new SpreadParticleFrame(Surface(f), f.Library.WorldMaterial))
                {
                    frame.BeginFrame(f.Zone);
                    for (int i = 0; i < 512; i++) Assert.True(frame.TryDraw(i % Zone.Width, i / Zone.Width, '*', "&Y"));
                    frame.EndFrame();
                    frame.BeginFrame(f.Zone);
                    var accepted = new Dictionary<int, GameObject>();
                    for (int i = 0; i < 256; i++)
                    {
                        foreach (int key in new[] { 700 + i, i })
                        {
                            Assert.True(frame.TryDraw(key % Zone.Width, key / Zone.Width, '!', "&Y"), "Interleaved current cell " + key);
                            Assert.True(frame.TryGet(key % Zone.Width, key / Zone.Width, out var root));
                            accepted.Add(key, root);
                        }
                    }
                    Assert.False(frame.TryDraw(1300 % Zone.Width, 1300 / Zone.Width, '*', "&Y"), "All512 views already belong to this frame.");
                    frame.EndFrame();
                    Assert.AreEqual(512, accepted.Values.Distinct().Count(), "One root cannot stand for two current cells.");
                    foreach (var pair in accepted)
                    {
                        Assert.True(frame.TryGet(pair.Key % Zone.Width, pair.Key / Zone.Width, out var current));
                        Assert.AreSame(pair.Value, current, "A current-seen view was evicted by a later draw.");
                    }
                    Assert.Zero(Enumerable.Range(256, 256).Count(i => frame.TryGet(i % Zone.Width, i / Zone.Width, out _)));
                }
            }
        }
    }
}
