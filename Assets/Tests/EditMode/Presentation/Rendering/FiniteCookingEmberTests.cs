using System;
using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Exact existing ember paths, not screenshot or actual finite-site creation.
    public sealed class FiniteCookingEmberTests
    {
        const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;
        [TestCase(false)][TestCase(true)]
        public void DirectEmitterRegistrationExcludesOnlyOptedInResidualCoals(bool finite)
        {
            using(var f=new Fixture())
            {f.Owner.GetPart<CampfirePart>().FiniteCooking=finite;f.Embers.SetZone(f.Zone);f.Embers.RegisterCampfire(f.Owner,11,10);Assert.AreEqual(finite?0:1,f.Anchors.Count);Assert.AreEqual(0,f.Children.Count);f.Unchanged();}
        }
        [TestCase(false)][TestCase(true)]
        public void ActualZoneRegistrationKeepsLegacyCampfireAndSkipsFiniteSource(bool finite)
        {
            using(var f=new Fixture())
            {f.Owner.GetPart<CampfirePart>().FiniteCooking=finite;f.Renderer.SetZone(f.Zone);Assert.AreEqual(finite?0:1,f.Anchors.Count);Assert.AreEqual(0,f.Children.Count);f.Unchanged();}
        }
        [TestCase(false)][TestCase(true)]
        public void CachedLegacyAnchorCannotSpawnAfterSourceBecomesFinite(bool becomeFinite)
        {
            using(var f=new Fixture())
            {
                f.Embers.SetZone(f.Zone);f.Embers.RegisterCampfire(f.Owner,11,10);Assert.AreEqual(1,f.Anchors.Count);f.Owner.GetPart<CampfirePart>().FiniteCooking=becomeFinite;
                Call(f.Embers,"SpawnEmber");Assert.AreEqual(becomeFinite?0:1,f.Children.Count,"No new free-floating Ember owner from an opted-in residual source, including an already cached anchor.");f.Unchanged();
            }
        }
        static void Call(object target,string name){try{target.GetType().GetMethod(name,I).Invoke(target,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        sealed class Fixture:IDisposable
        {
            readonly EntityEquipmentContentFixture scope;
            readonly GameObject root;
            readonly UnityEngine.Random.State random;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);
            readonly object oldBound;
            readonly Zone previous;
            readonly Action<int,int> oldChanged;
            readonly Action oldSight;
            public readonly Zone Zone;
            public readonly Entity Owner;
            public readonly ZoneRenderer Renderer;
            public readonly CampfireEmberRenderer Embers;
            public IList Anchors=>(IList)typeof(CampfireEmberRenderer).GetField("_anchors",I).GetValue(Embers);
            public IList Children=>(IList)typeof(CampfireEmberRenderer).GetField("_embers",I).GetValue(Embers);
            public Fixture()
            {
                random=UnityEngine.Random.state;oldBound=bound.GetValue(null);
                if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out var z)){previous=z;oldChanged=z.TileState.OnCellChanged;oldSight=z.TileState.OnSightChanged;}
                try
                {
                    scope=new EntityEquipmentContentFixture();Zone=new Zone("Overworld.12.10.0");Owner=scope.Factory.CreateEntity("Campfire");Assert.NotNull(Owner);Assert.False(Owner.GetPart<CampfirePart>().FiniteCooking);Assert.True(Zone.AddEntity(Owner,11,10));Zone.GetCell(11,10).Explored=Zone.GetCell(11,10).IsVisible=true;
                    root=new GameObject("Owned finite ember registration fixture");root.SetActive(false);Embers=root.AddComponent<CampfireEmberRenderer>();Renderer=root.AddComponent<ZoneRenderer>();
                    typeof(ZoneRenderer).GetField("_campfireEmberRenderer",I).SetValue(Renderer,Embers);
                }
                catch{Dispose();throw;}
            }
            public void Unchanged(){Assert.AreEqual((11,10),Zone.GetEntityPosition(Owner));Assert.AreSame(Owner,Owner.GetPart<CampfirePart>().ParentEntity);Assert.AreEqual(500,Owner.GetPart<ThermalPart>().Temperature);Assert.AreEqual(200,Owner.GetPart<FuelPart>().FuelMass);Assert.False(Owner.HasEffect<BurningEffect>());}
            public void Dispose()
            {
                // The production particle component uses deferred Destroy for
                // live particles; this EditMode fixture owns and releases them
                // immediately before its OnDestroy can enqueue those same roots.
                if(Embers!=null){while(Embers.transform.childCount>0)Object.DestroyImmediate(Embers.transform.GetChild(0).gameObject);Children.Clear();}
                if(root!=null)Object.DestroyImmediate(root);
                bound.SetValue(null,oldBound);if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                UnityEngine.Random.state=random;scope?.Dispose();
            }
        }
    }
}
