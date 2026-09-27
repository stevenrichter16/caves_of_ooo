using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Real managed graphs/native spell assets and request buses. The
    /// copied outcomes are presentation fixtures, never replayed gameplay.</summary>
    public sealed class SpreadWorldFxLifecycleTests
    {
        FxGlobals prior;
        [SetUp] public void Setup() { prior = new FxGlobals(); SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.SoundVolume = 0; SpellFxSettings.FlashIntensity = 0; }
        [TearDown] public void Teardown() { prior?.Dispose(); prior = null; }

        [TestCase("Pyromancy_EmberSpit")][TestCase("Pyromancy_FlamingHands")]
        [TestCase("Hydromancy_JetBlast")][TestCase("Galvanism_GroundSurge")]
        [TestCase("Cryomancy_RimeGrip")][TestCase("Spellcraft_Calm")][TestCase("Hydromancy_ConjureRain")]
        public void ExistingSevenAssetsSurviveActualManagedSpreadForeignReturn(string spell)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            using (var fx = new Fx(f))
            {
                var spread = f.Zone; string before = Graph(spread);
                Assert.True(SpreadPresentationScope.IsActive(spread));
                var first = EmitAndObserve(f, fx, spell); var oldRoot = fx.World.NativeRenderer.Root;
                var foreign = f.Manager.GetZone(SpawnRing3DIntegrationFixture.Grove);
                Assert.False(SpreadPresentationScope.IsActive(foreign));
                Rebind(f, fx, foreign);
                Assert.AreEqual(WorldFxPlaybackState.Cancelled, first.State);
                Assert.False(fx.World.HasBlockingFx); Assert.Zero(SpellFxBus.PendingCount); Assert.Zero(AsciiFxBus.PendingCount);
                Assert.True(oldRoot == null || !oldRoot.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy));
                string foreignBefore = Graph(foreign); var second = EmitAndObserve(f, fx, spell);
                Assert.AreEqual(foreignBefore, Graph(foreign));
                Rebind(f, fx, spread); Assert.AreEqual(WorldFxPlaybackState.Cancelled, second.State);
                var returned = EmitAndObserve(f, fx, spell); Assert.AreEqual(before, Graph(spread));
                fx.World.CancelAll(); Assert.AreEqual(WorldFxPlaybackState.Cancelled, returned.State); Assert.False(fx.World.HasBlockingFx);
            }
        }

        [TestCase("hidden")][TestCase("unexplored")][TestCase("presentation")][TestCase("bus-reset")]
        public void CurrentVisibilityOrCancellationCannotLeaveAnOldNativeWait(string fault)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            using (var fx = new Fx(f))
            {
                string graph = Graph(f.Zone); var handle = EmitAndObserve(f, fx, "Pyromancy_EmberSpit");
                if (fault == "presentation") fx.World.Update(0, true, false);
                else if (fault == "bus-reset") { AsciiFxBus.Clear(); SpellFxBus.Clear(); fx.World.Update(0); }
                else
                {
                    foreach (var cell in f.Zone.Cells) { if (fault == "hidden") cell.IsVisible = false; else cell.Explored = false; }
                    fx.World.Update(0);
                    Assert.False(fx.World.NativeRenderer.Root.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy));
                    // Visibility hides existing art; its finite copied timeline may
                    // finish normally. It must never acquire an indefinite wait.
                    fx.World.Update(WorldFxPlayback.HardTimeoutSeconds + .1f);
                }
                Assert.True(handle.IsFinished); Assert.False(fx.World.HasBlockingFx); Assert.AreEqual(graph, Graph(f.Zone));
            }
        }

        [TestCase(false)][TestCase(true)]
        public void NumericReadoutRemainsExactUiDigitsAcrossNativeAndAsciiViews(bool native)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            using (var fx = new Fx(f))
            {
                if (!native) fx.World.SetNativeSurface(null);
                fx.World.Update(0, native, true);
                AsciiFxBus.EmitFloatingNumber(f.Zone, 20, 12, 37, "&R"); fx.World.Update(0, native, true);
                Assert.AreEqual(2, fx.Ascii.ActiveParticleCount); Assert.Zero(AsciiFxBus.PendingCount);
                Assert.AreSame(CP437TilesetGenerator.GetTile('3'), fx.Tiles.GetTile(At(20, 11)));
                Assert.AreSame(CP437TilesetGenerator.GetTile('7'), fx.Tiles.GetTile(At(21, 11)));
                Assert.AreEqual(Matrix4x4.identity, fx.Tiles.GetTransformMatrix(At(20, 11))); Assert.False(fx.World.HasBlockingFx);
                fx.World.Update(.16f, native, true); Assert.False(fx.Tiles.HasTile(At(20, 11)));
                Assert.AreSame(CP437TilesetGenerator.GetTile('3'), fx.Tiles.GetTile(At(20, 10)));
            }
        }

        [TestCase("current")][TestCase("removed-effect")][TestCase("removed-owner")]
        public void SameZoneSurfaceReplacementRebuildsOnlyActualPersistentAura(string state)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                // Status containers are lazy in the actual blueprint; this is an explicit
                // persistent-effect fixture, not a claim that fresh Player already owns one.
                var actor = f.Player; var effects = actor.GetPart<StatusEffectsPart>();
                if (effects == null) { effects = new StatusEffectsPart(); actor.AddPart(effects); }
                Assert.AreSame(actor, effects.ParentEntity);
                var burning = new BurningEffect(1, null, new System.Random(173));
                effects.RestoreEffectsForLoad(new List<Effect> { burning });
                using (var fx = new Fx(f))
                using (var replacement = new NativeZone3DRenderSurface(f.Root.transform, f.Library.Renderer, f.Library.RendererIndex,
                    f.Library.CompositeMaterial, new[] { f.Library.WorldMaterial, f.Library.WaterMaterial }, 2.2f))
                {
                    fx.World.Update(0); Assert.AreEqual(1, fx.Ascii.ActiveAuraCount);
                    if (state == "removed-effect") effects.RestoreEffectsForLoad(new List<Effect>());
                    else if (state == "removed-owner") Assert.True(f.Zone.RemoveEntity(actor));
                    string graph = Graph(f.Zone); var duration = burning.Duration;
                    replacement.Sync(f.Source, true, false); replacement.UpdateFog(f.Zone, f.Light, true);
                    fx.World.SetNativeSurface(replacement); fx.World.Update(0);
                    Assert.AreEqual(state == "current" ? 1 : 0, fx.Ascii.ActiveAuraCount,
                        "A replacement surface must observe existing status, without requiring a new gameplay aura event.");
                    Assert.AreEqual(graph, Graph(f.Zone)); Assert.AreEqual(duration, burning.Duration); Assert.False(fx.World.HasBlockingFx);
                }
            }
        }

        [Test] public void DisposalClearsNativeAndQueuedPresentationWithoutChangingSimulation()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var fx = new Fx(f);
                try
                {
                    string graph = Graph(f.Zone); var handle = EmitAndObserve(f, fx, "Cryomancy_RimeGrip");
                    SpellFxBus.Emit(Sequence(f.Zone, "Spellcraft_Calm")); fx.Dispose();
                    Assert.True(handle.IsFinished); Assert.False(fx.World.HasBlockingFx); Assert.Zero(SpellFxBus.PendingCount);
                    Assert.Zero(AsciiFxBus.PendingCount); Assert.AreEqual(graph, Graph(f.Zone));
                    Assert.NotNull(Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath));
                }
                finally { fx.Dispose(); }
            }
        }

        static WorldFxPlayback EmitAndObserve(SpawnRing3DIntegrationFixture f, Fx fx, string spell)
        {
            fx.World.Update(0); SpellFxBus.Emit(Sequence(f.Zone, spell)); Assert.True(fx.World.HasBlockingFx);
            fx.World.Update(0); var handle = fx.World.LastPlayback; Assert.NotNull(handle); Assert.False(handle.IsFinished);
            var entry = fx.Library.Find(spell); Assert.AreSame(entry, fx.World.NativeRenderer.LastEntry);
            bool observed = false;
            for (int i = 0; i < 8 && !observed; i++)
            {
                fx.World.Update(.08f);
                var root = fx.World.NativeRenderer.Root; Assert.NotNull(root);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    Assert.NotNull(mesh); Assert.Greater(mesh.vertexCount, 0); Assert.True(entry.Pieces.Any(p => p.Mesh == mesh));
                    Assert.AreSame(((SpawnRing3DPresenter)f.Presenter).ActiveSurface.FogTexture, renderer.sharedMaterial.GetTexture("_FogLight"));
                    Assert.AreEqual(1, renderer.sharedMaterial.GetFloat("_Transient"));
                    Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode); observed = true;
                }
            }
            Assert.True(observed, "Actual imported native pieces must draw during the bounded copied timeline.");
            Assert.Zero(SpellFxBus.PendingCount); return handle;
        }
        static SpellFxSequence Sequence(Zone zone, string spell)
        {
            // Native cone pieces require copied cells within their real authored reach.
            // The former four-cell generic target lies outside Hands (1) and Jet (2).
            int reach = spell == "Pyromancy_FlamingHands" ? 1 : spell == "Hydromancy_JetBlast" ? 2 : 4;
            var target = new Point(10 + reach, 10);
            var path = Enumerable.Range(1, reach).Select(i => new Point(10 + i, 10)).ToArray();
            return new SpellFxSequence(spell, zone, null, new Point(10, 10),
                path, new[] { target },
                new[] { new SpellFxTargetResult("copied-presentation-target", target, target, damage: 2,
                    appliedEffects: new[] { "FrozenEffect", "Pacified", "Watered" }, isDirectTarget: true) },
                cosmeticSeed: 91, reactions: new[] { new SpellFxCellResult(target, "coating", "water", 1) });
        }
        static void Rebind(SpawnRing3DIntegrationFixture f, Fx fx, Zone zone)
        {
            fx.World.SetNativeSurface(null); fx.World.SetZone(zone);
            f.Zone = zone; f.Manager.SetActiveZone(zone); f.Reveal(); f.Bind(zone); f.Refresh();
            fx.World.SetNativeSurface(((SpawnRing3DPresenter)f.Presenter).ActiveSurface); fx.World.Update(0);
        }
        static Vector3Int At(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
        static string Graph(Zone zone) => zone.TileState.ToSaveString() + "|" + zone.EntityVersion + "|" + string.Join("\n",
            zone.GetReadOnlyEntities().Select(e => e.ID + "|" + zone.GetEntityPosition(e) + "|" + e.GetStatValue("Hitpoints") + "|"
                + string.Join(",", e.GetPart<InventoryPart>()?.GetAllEquipped().Select(i => i.ID).OrderBy(x => x) ?? Enumerable.Empty<string>())).OrderBy(x => x));
        sealed class Fx : IDisposable
        {
            public readonly WorldFxCoordinator World; public readonly AsciiFxRenderer Ascii; public readonly Tilemap Tiles; public readonly NativeSpellFxLibrary Library;
            readonly GameObject host; bool disposed;
            public Fx(SpawnRing3DIntegrationFixture f)
            {
                host = new GameObject("Owned independent lifecycle UI", typeof(Grid)); host.transform.SetParent(f.Root.transform, false);
                var map = new GameObject("Owned readout map", typeof(Tilemap)); map.transform.SetParent(host.transform, false); Tiles = map.GetComponent<Tilemap>();
                try
                {
                    Ascii = new AsciiFxRenderer(Tiles); Library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath); Assert.NotNull(Library);
                    World = new WorldFxCoordinator(Ascii, host.transform, Library);
                    World.SetNativeSurface(((SpawnRing3DPresenter)f.Presenter).ActiveSurface); World.SetZone(f.Zone);
                }
                catch { World?.Dispose(); Object.DestroyImmediate(host); throw; }
            }
            public void Dispose() { if (disposed) return; disposed = true; World.Dispose(); Object.DestroyImmediate(host); }
        }
        sealed class FxGlobals : IDisposable
        {
            const BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
            readonly SpellFxMode mode = SpellFxSettings.Mode; readonly float speed = SpellFxSettings.AnimationSpeed, sound = SpellFxSettings.SoundVolume, flash = SpellFxSettings.FlashIntensity;
            readonly Queue<AsciiFxRequest> pending = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", Private).GetValue(null);
            readonly Stack<AsciiFxRequest> pool = (Stack<AsciiFxRequest>)typeof(AsciiFxBus).GetField("Pool", Private).GetValue(null);
            readonly List<SpellFxSequence> spells = (List<SpellFxSequence>)typeof(SpellFxBus).GetField("Pending", Private).GetValue(null);
            readonly FieldInfo clearField = typeof(AsciiFxBus).GetField("<ClearVersion>k__BackingField", Private);
            readonly Dictionary<string, SpellFxDefinition> definitions = (Dictionary<string, SpellFxDefinition>)typeof(SpellFxCatalog).GetField("Definitions", Private).GetValue(null);
            readonly Dictionary<string, SpellFxAsset> assets = (Dictionary<string, SpellFxAsset>)typeof(SpellFxCatalog).GetField("Assets", Private).GetValue(null);
            readonly List<string> issues = (List<string>)typeof(SpellFxCatalog).GetField("Issues", Private).GetValue(null);
            readonly FieldInfo loadedField = typeof(SpellFxCatalog).GetField("_loaded", Private);
            readonly AsciiFxRequest[] priorPending, priorPool; readonly SpellFxSequence[] priorSpells; readonly string[] priorIssues;
            readonly Dictionary<string, SpellFxDefinition> priorDefinitions; readonly Dictionary<string, SpellFxAsset> priorAssets;
            readonly int clear; readonly bool loaded; bool disposed;
            public FxGlobals()
            {
                priorPending = pending.ToArray(); priorPool = pool.ToArray(); priorSpells = spells.ToArray(); clear = (int)clearField.GetValue(null);
                priorDefinitions = new Dictionary<string, SpellFxDefinition>(definitions); priorAssets = new Dictionary<string, SpellFxAsset>(assets); priorIssues = issues.ToArray(); loaded = (bool)loadedField.GetValue(null);
                pending.Clear(); pool.Clear(); spells.Clear();
                definitions.Clear(); assets.Clear(); issues.Clear(); loadedField.SetValue(null, false);
            }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                pending.Clear(); pool.Clear(); spells.Clear(); foreach (var request in priorPending) pending.Enqueue(request);
                for (int i = priorPool.Length - 1; i >= 0; i--) pool.Push(priorPool[i]); spells.AddRange(priorSpells); clearField.SetValue(null, clear);
                foreach (var value in assets.Values.Distinct()) if (value != null && !priorAssets.Values.Contains(value)) value.Dispose();
                definitions.Clear(); foreach (var row in priorDefinitions) definitions.Add(row.Key, row.Value);
                assets.Clear(); foreach (var row in priorAssets) assets.Add(row.Key, row.Value); issues.Clear(); issues.AddRange(priorIssues); loadedField.SetValue(null, loaded);
                SpellFxSettings.Mode = mode; SpellFxSettings.AnimationSpeed = speed; SpellFxSettings.SoundVolume = sound; SpellFxSettings.FlashIntensity = flash;
            }
        }
    }
}
