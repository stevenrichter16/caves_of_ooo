using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Player-flow hypothesis: a stationary native item change discovers a broken
    /// model, hiding the whole 3D surface during an incremental redraw. Every
    /// previously suppressed distant native cell must recover in that frame.
    /// Real imported models and current generated zones; no GPU/feel claim.
    /// </summary>
    public sealed class SpawnRing3DFallbackInvalidationTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase("ground", true)]
        [TestCase("entity", true)]
        [TestCase("ground", false)]
        [TestCase("entity", false)]
        public void DirtyRuntimeFailureRestoresDistantNativeBodyWhileHealthyRefreshKeepsSuppression(string probeKind, bool corrupt)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            using (var host = new NativeHost(f, false))
            {
                Entity probe = probeKind == "entity" ? f.Add("CaveHermit") : FindGround(f);
                var at = f.Zone.GetEntityPosition(probe);
                var dirty = FarFreeCell(f, at.x, at.y);
                host.Frame();
                Assert.IsTrue(f.Authored(probe), "The actual distant probe is represented before failure.");
                Assert.IsTrue(host.Suppressed(at.x, at.y), "Native body suppression must really exist before this test.");
                int playerVersion = f.Zone.EntityVersion;
                var playerAt = f.Zone.GetEntityPosition(f.Player);
                int tick = TurnManager.Active?.TickCount ?? 0;
                var item = f.Add("Tepuibone", dirty.x, dirty.y);
                using (var broken = new ModelFault(f, item, corrupt))
                {
                    Assert.IsFalse(host.FullDirty, "The real transition must enter through the cell-only path.");
                    host.Renderer.MarkCellDirty(dirty.x, dirty.y, "RingFallbackRegression");
                    Assert.AreEqual(1, host.DirtyCount);
                    Assert.Greater(Math.Max(Math.Abs(at.x-dirty.x), Math.Abs(at.y-dirty.y)), 1,
                        "The distant probe must lie outside the environment pass's dirty-neighbour expansion.");
                    host.Frame();
                    Assert.AreEqual(!corrupt, f.Get<bool>("PresentationVisible"));
                    if (corrupt)
                    {
                        Assert.IsNotEmpty(f.Get<string>("Failure"));
                        Assert.IsTrue(host.HasNativeBody(at.x, at.y),
                            "Whole ring failure hid 3D but left this untouched native glyph/sprite suppressed. Restore the entire native view in the failure frame.");
                    }
                    else
                    {
                        Assert.IsNull(f.Get<string>("Failure"));
                        Assert.IsTrue(host.Suppressed(at.x, at.y), "A healthy cell update must keep 3D ownership and suppression.");
                    }
                    Assert.AreEqual(playerAt, f.Zone.GetEntityPosition(f.Player), "Recovery must not require a player move.");
                    Assert.AreEqual(tick, TurnManager.Active?.TickCount ?? 0, "Rendering consumes no native turn.");
                    Assert.AreEqual(playerVersion + 1, f.Zone.EntityVersion, "Only the explicit added item changes native membership.");
                    Assert.AreSame(f.Borrowed, f.Source.targetTexture);
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DirtyRingFailureRestoresOriginalFellingPresentationInTheSameFrame(bool corrupt)
        {
            using (var f = new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            using (var host = new NativeHost(f, true))
            {
                host.Frame();
                Assert.IsTrue(host.Felling.IsReady, host.Felling.Failure);
                Assert.IsTrue(f.Get<bool>("PresentationVisible"));
                Assert.IsFalse(host.Felling.PresentationVisible, "Actual ready legacy Felling art is suppressed by the ready ring.");
                var at = f.FreeCell(); var item = f.Add("Tepuibone", at.x, at.y);
                using (var broken = new ModelFault(f, item, corrupt))
                {
                    Assert.IsFalse(host.FullDirty);
                    host.Renderer.MarkCellDirty(at.x, at.y, "RingFellingFallbackRegression");
                    host.Frame();
                    Assert.AreEqual(!corrupt, f.Get<bool>("PresentationVisible"));
                    Assert.AreEqual(corrupt, host.Felling.PresentationVisible,
                        "Resynchronize the existing Felling presentation after ring refresh fails, before this frame is rendered.");
                    Assert.AreSame(f.Zone, host.Felling.CurrentZone);
                    Assert.AreEqual(55, host.Felling.ComponentCount);
                }
            }
        }

        static Entity FindGround(SpawnRing3DIntegrationFixture f)
        {
            for (int y=1; y<24; y++) for (int x=1; x<79; x++)
            {
                var entity=f.Zone.GetCell(x,y).GetTopVisibleObject();
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition);
                if (recipe.ModelId != null && f.Library.Definition.FindModel(recipe.ModelId)?.kind == "ground") return entity;
            }
            Assert.Fail("Generated grove requires an actual top ground model for the distant native floor control."); return null;
        }
        static (int x,int y) FarFreeCell(SpawnRing3DIntegrationFixture f,int probeX,int probeY)
        {
            for (int y=1; y<24; y++) for (int x=1; x<79; x++)
            {
                var cell=f.Zone.GetCell(x,y);
                if (Math.Max(Math.Abs(x-probeX),Math.Abs(y-probeY)) >= 12 && !cell.BlocksMovement(f.Player)
                    && !cell.Objects.Any(e=>e.HasPart<BrainPart>() || e.HasTag("Player"))) return(x,y);
            }
            Assert.Fail("Generated native graph must have a free mutation cell well outside the distant probe's dirty neighbourhood."); return(-1,-1);
        }

        sealed class ModelFault : IDisposable
        {
            readonly SpawnRing3DLibrary library;
            readonly GameObject original;
            readonly object binding;
            readonly FieldInfo prefabField;
            readonly GameObject owned;
            public ModelFault(SpawnRing3DIntegrationFixture f, Entity newItem, bool corrupt)
            {
                if (!corrupt) return;
                library=f.Library;
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,newItem,library.Definition);
                Assert.IsNotNull(recipe.ModelId); Assert.IsFalse(recipe.Batched);
                binding=library.Models.Single(m=>m.Id==recipe.ModelId);
                prefabField=binding.GetType().GetField("Prefab"); Assert.NotNull(prefabField);
                original=(GameObject)prefabField.GetValue(binding); Assert.NotNull(original);
                owned=Object.Instantiate(original); owned.SetActive(false);
                var renderers=owned.GetComponentsInChildren<Renderer>(true); Assert.IsNotEmpty(renderers);
                foreach (var renderer in renderers) Object.DestroyImmediate(renderer);
                prefabField.SetValue(binding,owned); library.InvalidateCaches();
            }
            public void Dispose()
            {
                if (library!=null) { prefabField.SetValue(binding,original); library.InvalidateCaches(); }
                if (owned!=null) Object.DestroyImmediate(owned);
            }
        }

        sealed class NativeHost : IDisposable
        {
            readonly GameObject root;
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            public readonly ZoneRenderer Renderer;
            public readonly FellingScenePresenter Felling;
            readonly Tilemap main,overlay;
            public bool FullDirty => (bool)Field("_fullDirty").GetValue(Renderer);
            public int DirtyCount => ((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Count;
            public NativeHost(SpawnRing3DIntegrationFixture f,bool withFelling)
            {
                try
                {
                    // Ordinary render paths with only their actual native body pass.
                    // Keep this host inactive to avoid unrelated UI/FX Awake setup.
                    root=new GameObject("Owned native ring fallback host"); root.SetActive(false); root.AddComponent<Grid>();
                    var tiles=new GameObject("Native main",typeof(Tilemap),typeof(TilemapRenderer)); tiles.transform.SetParent(root.transform,false);
                    main=tiles.GetComponent<Tilemap>(); Renderer=tiles.AddComponent<ZoneRenderer>();
                    Field("_tilemap").SetValue(Renderer,main); Field("_mainCamera").SetValue(Renderer,f.Source);
                    Field("_spawnRing3DPresenter").SetValue(Renderer,f.Presenter);
                    var environment=root.AddComponent<EnvironmentSpriteRenderer>(); environment.Init(root.transform,main);
                    environment.SetAuthoredScenePredicates(
                        (x,y)=>(bool)Invoke("ClaimsAuthoredSceneCell",x,y),
                        e=>(bool)Invoke("IsAuthoredSceneEntity",e));
                    Field("_envSpriteRenderer").SetValue(Renderer,environment);
                    overlay=root.transform.Find("EnvironmentSpriteTilemap").GetComponent<Tilemap>();
                    if (withFelling)
                    {
                        var go=new GameObject("Borrowed ready Felling presentation"); go.transform.SetParent(f.Root.transform,false);
                        Felling=go.AddComponent<FellingScenePresenter>(); Field("_fellingScenePresenter").SetValue(Renderer,Felling);
                    }
                    // FOV is held at this fixture's explicit revealed setup; the real
                    // player remains in f.Zone. These tests exercise paint invalidation,
                    // not FieldOfView.Compute or gameplay turns.
                    Renderer.SetZone(f.Zone);
                    ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public void Frame() => Invoke("LateUpdate");
            public bool Suppressed(int x,int y) => !HasNativeBody(x,y);
            public bool HasNativeBody(int x,int y)
            { var p=new Vector3Int(x,Zone.Height-1-y,0);return main.GetTile(p)!=null || overlay.GetTile(p)!=null; }
            static FieldInfo Field(string name) => typeof(ZoneRenderer).GetField(name,Private);
            object Invoke(string name,params object[] args)
            {
                try { return typeof(ZoneRenderer).GetMethod(name,Private).Invoke(Renderer,args); }
                catch (TargetInvocationException e) { throw e.InnerException ?? e; }
            }
            public void Dispose()
            {
                if (root!=null) Object.DestroyImmediate(root);
                ZoneRenderHooks.CellDirtyCallback=oldCell; ZoneRenderHooks.FullDirtyCallback=oldFull;
            }
        }
    }
}
