using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Markers describe a real offering owner and journal state. They
    /// are not a body tint, an invented turn-in predicate, or world-map decoration.</summary>
    public sealed class NativeQuestCueTests
    {
        private StoryletPart oldStorylets;
        private Entity oldPlayer;
        private QuestCueRegistryFixture registry;
        [SetUp] public void SetUp() { registry=new QuestCueRegistryFixture();oldStorylets=StoryletPart.Current;oldPlayer=StoryletPart.LocalPlayer;StoryletPart.Current=new StoryletPart(); }
        [TearDown] public void TearDown() { StoryletPart.Current=oldStorylets;StoryletPart.LocalPlayer=oldPlayer;registry?.Dispose(); }

        [TestCase("north-guard-west", "MorrowfastBell")]
        [TestCase("north-guard-east", "MorrowfastReturnNotch")]
        [TestCase("farra-sprig", "MorrowfastDryGoods")]
        public void ActualMorrowfastOfferingResidentsDeclareTheirRealQuest(string resident,string quest)
        {
            var factory=GrovelandsCompositionTests.Factory();var giver=MorrowfastContent.CreateResident(resident,factory);
            Assert.NotNull(giver.GetPart<QuestBeaconPart>(),"Authored residents must opt into the same native cue contract.");
            Assert.AreEqual(quest,giver.GetPart<QuestBeaconPart>().Quest);
            var merchant=MorrowfastContent.CreateResident("southern-food-vendor",factory);
            Assert.IsNull(merchant.GetPart<QuestBeaconPart>(),"A shop alone does not promise a quest.");
        }

        [TestCase("available", "Available")] [TestCase("active", "Active")]
        [TestCase("complete", "None")] [TestCase("no-journal", "None")]
        public void NativeQueryUsesActualJournalStateAndNeverClaimsTurnInReadiness(string state,string expected)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.8.16.0");
            var player=factory.CreateEntity("Player");var giver=factory.CreateEntity("Villager");
            giver.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});zone.AddEntity(player,1,1);zone.AddEntity(giver,20,12);
            SetQuest(state);Assert.AreEqual(expected,Query(giver,zone,player));
            Assert.AreEqual("None",Query(player,zone,player));
        }

        [TestCase("empty-quest")] [TestCase("no-part")] [TestCase("detached")]
        [TestCase("dead")] [TestCase("invisible")] [TestCase("no-player")]
        public void NativeQueryRejectsOwnersThatCannotOfferAVisibleLivingInteraction(string reason)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.8.16.0");
            var player=factory.CreateEntity("Player");var giver=factory.CreateEntity("Villager");
            zone.AddEntity(player,1,1);zone.AddEntity(giver,20,12);
            if(reason!="no-part")giver.AddPart(new QuestBeaconPart{Quest=reason=="empty-quest"?"":"ClearTheWarren"});
            if(reason=="detached")zone.RemoveEntity(giver);
            if(reason=="dead")giver.GetStat("Hitpoints").Value=0;
            if(reason=="invisible")giver.GetPart<RenderPart>().Visible=false;
            Assert.AreEqual("None",Query(giver,zone,reason=="no-player"?null:player));
        }

        [TestCase(false)] [TestCase(true)]
        public void RealPresenterShowsAvailableThenActiveThenNoneWithoutDirtyCells(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var first);var availableMesh=first.GetComponentInChildren<MeshFilter>().sharedMesh;
                StoryletPart.Current.StartQuest(new QuestState{QuestId=scene.Quest});
                scene.Frame();AssertCue(scene,"Active",out var ongoing);
                Assert.AreSame(first,ongoing,"A state transition reuses the marker owner.");
                Assert.AreNotSame(availableMesh,ongoing.GetComponentInChildren<MeshFilter>().sharedMesh,"An active journal needs a different silhouette, not only another tint.");
                StoryletPart.Current.MarkQuestCompleted(scene.Quest);scene.Frame();
                Assert.IsFalse(scene.Cue(out _,out _));Assert.IsFalse(first.activeInHierarchy);
                StoryletPart.Current=new StoryletPart();scene.Refresh();AssertCue(scene,"Available",out var restored);
                Assert.AreSame(first,restored,"Replacing the loaded journal must be observed without rebuilding the actor.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void MarkerSharesOwnerVisibilityAndNeverRevealsAnUnseenGiver(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var cue);
                var cell=scene.Zone.GetEntityCell(scene.Giver);cell.IsVisible=false;cell.Explored=true;
                scene.Refresh();Assert.IsFalse(scene.Cue(out _,out _));Assert.IsFalse(cue.activeInHierarchy);
                cell.IsVisible=true;scene.Refresh();AssertCue(scene,"Available",out _);
                scene.Giver.GetPart<RenderPart>().Visible=false;scene.Frame();
                Assert.IsFalse(scene.Cue(out _,out _));Assert.IsFalse(cue.activeInHierarchy);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RemovingOrKillingOwnerImmediatelyHidesTheExistingMarker(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var cue);
                scene.Giver.GetStat("Hitpoints").Value=0;scene.Frame();
                Assert.IsFalse(scene.Cue(out _,out _));Assert.IsFalse(cue.activeInHierarchy);
                scene.Giver.GetStat("Hitpoints").Value=10;scene.Frame();AssertCue(scene,"Available",out _);
                scene.Zone.RemoveEntity(scene.Giver);scene.Refresh();
                Assert.IsFalse(scene.Cue(out _,out _));Assert.IsTrue(cue==null||!cue.activeInHierarchy);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void HostilitySuppressesDiscoveryUntilTheActualFactionRelationshipRecovers(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var cue);
                string faction=scene.Giver.GetTag("Faction");int previous=PlayerReputation.Get(faction);
                try
                {
                    PlayerReputation.Set(faction,-1000);scene.Frame();
                    Assert.IsFalse(scene.Cue(out _,out _));Assert.IsFalse(cue.activeInHierarchy);
                    PlayerReputation.Set(faction,0);scene.Frame();AssertCue(scene,"Available",out var same);
                    Assert.AreSame(cue,same);
                }
                finally{PlayerReputation.Set(faction,previous);}
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RepeatedRefreshReusesGeometryAndDoesNotAddCollidersOrChangeNativeState(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var cue);
                var mesh=cue.GetComponentInChildren<MeshFilter>().sharedMesh;
                var material=cue.GetComponentInChildren<Renderer>().sharedMaterial;
                int count=scene.Body.GetComponentsInChildren<Transform>(true).Length,version=scene.Zone.EntityVersion;
                string color=scene.Giver.GetPart<RenderPart>().ColorString;
                for(int i=0;i<64;i++){scene.Frame();scene.Refresh();}
                AssertCue(scene,"Available",out var same);Assert.AreSame(cue,same);
                Assert.AreSame(mesh,same.GetComponentInChildren<MeshFilter>().sharedMesh);
                Assert.AreSame(material,same.GetComponentInChildren<Renderer>().sharedMaterial);
                Assert.AreEqual(count,scene.Body.GetComponentsInChildren<Transform>(true).Length);
                Assert.IsEmpty(cue.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(cue.GetComponentsInChildren<Rigidbody>(true));
                Assert.AreEqual(version,scene.Zone.EntityVersion);Assert.AreEqual(color,scene.Giver.GetPart<RenderPart>().ColorString);
                Assert.AreEqual(Village3DPresenter.WorldLayer,cue.GetComponentInChildren<Renderer>().gameObject.layer);
                Assert.Greater(cue.GetComponentInChildren<Renderer>().bounds.max.y,scene.Body.GetComponentInChildren<Collider>().bounds.max.y,
                    "The marker sits above the owner rather than disappearing inside its body.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void LeavingNativeZoneReleasesOnlyOwnedMarkersAndKeepsBorrowedCamera(bool village)
        {
            using(var scene=new CueScene(village))
            {
                AssertCue(scene,"Available",out var cue);var source=scene.Source;
                scene.Leave();Assert.IsTrue(cue==null||!cue.activeInHierarchy);Assert.NotNull(source);
                Assert.IsFalse(scene.Cue(out _,out _));
            }
        }

        private static void SetQuest(string state)
        {
            if(state=="no-journal"){StoryletPart.Current=null;return;}
            if(state=="active"||state=="complete")StoryletPart.Current.StartQuest(new QuestState{QuestId="ClearTheWarren"});
            if(state=="complete")StoryletPart.Current.MarkQuestCompleted("ClearTheWarren");
        }
        private static string Query(Entity giver,Zone zone,Entity player)
        {
            var type=typeof(QuestBeaconPart).Assembly.GetType("CavesOfOoo.Storylets.QuestCueStateQuery");
            Assert.NotNull(type,"Native availability must be a shared query rather than forced renderer color.");
            var method=type.GetMethod("Evaluate",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method);
            return method.Invoke(null,new object[]{giver,zone,player}).ToString();
        }
        private static void AssertCue(CueScene scene,string state,out GameObject marker)
        {
            Assert.IsTrue(scene.Cue(out marker,out var actual),"A visible native quest owner needs an actual 3D marker.");
            Assert.AreEqual(state,actual);Assert.NotNull(marker);Assert.IsTrue(marker.activeInHierarchy);
            Assert.IsNotEmpty(marker.GetComponentsInChildren<Renderer>());
        }

        private sealed class CueScene:IDisposable
        {
            private readonly Village3DIntegrationFixture village;
            private readonly SpawnRing3DIntegrationFixture ring;
            public readonly MonoBehaviour Presenter;
            public readonly Entity Giver;
            public readonly Zone Zone;
            public readonly GameObject Body;
            public readonly Camera Source;
            public string Quest=>Giver.GetPart<QuestBeaconPart>().Quest;
            public CueScene(bool useVillage)
            {
                if(useVillage)
                {
                    village=new Village3DIntegrationFixture();Presenter=village.Presenter;Zone=village.Zone;Source=village.Source;
                    var view=village.View("north-guard-west");Giver=view.owner;Body=view.root;
                    // Pin native authored creation separately; this setup also
                    // exercises the presentation independently of content wiring.
                    if(Giver.GetPart<QuestBeaconPart>()==null)Giver.AddPart(new QuestBeaconPart{Quest="MorrowfastBell"});
                    StoryletPart.LocalPlayer=village.Player;
                }
                else
                {
                    ring=new SpawnRing3DIntegrationFixture("Overworld.8.16.0");Presenter=ring.Presenter;Source=ring.Source;
                    ring.Zone=new Zone("Overworld.8.16.0");Zone=ring.Zone;
                    ring.Player=ring.Add("Player",19,12);Giver=ring.Add("Villager",20,12);
                    Giver.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});StoryletPart.LocalPlayer=ring.Player;
                    ring.Reveal();ring.Bind(Zone);ring.Refresh();Body=ring.View(Giver);
                }
                StoryletPart.Current=new StoryletPart();Refresh();
            }
            public bool Cue(out GameObject root,out string state)
            {
                var method=Presenter.GetType().GetMethod("TryGetQuestCue",BindingFlags.Public|BindingFlags.Instance);
                Assert.NotNull(method,"Presenter must expose its native owner-bound cue for lifecycle inspection.");
                object[] args={Giver,null,null};bool shown=(bool)method.Invoke(Presenter,args);root=args[1]as GameObject;state=args[2]as string;return shown;
            }
            public void Refresh(){if(village!=null)village.Refresh();else ring.Refresh(new HashSet<int>());}
            public void Frame(){if(village!=null)Village3DIntegrationFixture.TickFrame(village.Presenter);else ring.Frame();}
            public void Leave(){if(village!=null)village.Presenter.Bind(new Zone("quest-cue-exit"),village.Source);else ring.Bind(new Zone("quest-cue-exit"));}
            public void Dispose(){village?.Dispose();ring?.Dispose();}
        }
    }

    internal sealed class QuestCueRegistryFixture:IDisposable
    {
        private readonly Dictionary<string,StoryletData> live;
        private readonly Dictionary<string,StoryletData> before;
        private readonly FieldInfo loaded;
        private readonly bool wasLoaded;
        public QuestCueRegistryFixture()
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
            live=(Dictionary<string,StoryletData>)typeof(StoryletRegistry).GetField("_storylets",flags).GetValue(null);
            before=new Dictionary<string,StoryletData>(live);loaded=typeof(StoryletRegistry).GetField("_loaded",flags);wasLoaded=(bool)loaded.GetValue(null);
            ConversationActions.EnsureInitialized();ConversationPredicates.EnsureInitialized();StoryletRegistry.LoadAll();
        }
        public void Dispose(){live.Clear();foreach(var pair in before)live.Add(pair.Key,pair.Value);loaded.SetValue(null,wasLoaded);}
    }
}
