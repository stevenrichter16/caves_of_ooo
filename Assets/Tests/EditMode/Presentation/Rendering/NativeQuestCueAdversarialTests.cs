using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent bug classes: graph identity, live owner replacement,
    /// resource ownership, state isolation and selection/visibility boundaries.</summary>
    public sealed class NativeQuestCueAdversarialTests
    {
        private StoryletPart previous;
        private Entity previousPlayer;
        private QuestCueRegistryFixture registry;
        [SetUp] public void SetUp(){registry=new QuestCueRegistryFixture();previous=StoryletPart.Current;previousPlayer=StoryletPart.LocalPlayer;StoryletPart.Current=new StoryletPart();}
        [TearDown] public void TearDown(){StoryletPart.Current=previous;StoryletPart.LocalPlayer=previousPlayer;registry?.Dispose();}

        [TestCase(false)] [TestCase(true)]
        public void UnknownOrNonQuestDefinitionsCannotAdvertiseAPlayableQuest(bool nonQuestDefinition)
        {
            using(var f=Scene(out var giver))
            {
                Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(giver,f.Zone,f.Player));
                const string missing="QuestCue_Undefined_Adversarial";
                if(nonQuestDefinition)StoryletRegistry.Register(new StoryletData{ID=missing});
                giver.GetPart<QuestBeaconPart>().Quest=missing;
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(giver,f.Zone,f.Player));
            }
        }

        [TestCase(.35f)] [TestCase(2.5f)]
        public void SmallAndLargeActorsKeepReadableMarkersWithinOneGameplayCell(float scale)
        {
            using(var f=Scene(out var giver,false))
            {
                var body=f.View(giver);body.transform.localScale=Vector3.one*scale;
                giver.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});f.Frame();
                Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out var marker,out _));
                var size=marker.GetComponent<Renderer>().bounds.size;
                Assert.Less(Mathf.Max(size.x,Mathf.Max(size.y,size.z)),.9f);
                Assert.Greater(Mathf.Max(size.x,Mathf.Max(size.y,size.z)),.35f);
                Assert.Less((marker.transform.lossyScale-Vector3.one).magnitude,.001f,"Actor scale must not turn cues into giant or unreadable signs.");
            }
        }

        // Native camera captures exposed two linked failures: amber was too
        // quiet, and the town's 8x8 atlas was sampled as the ring's 16x8 atlas.
        // Read the actual borrowed texture rather than assuming an index means
        // the same RGB in both presenters. No new material/light is needed.
        [TestCase(false)] [TestCase(true)]
        public void AvailableCueHasAPaleFaceDarkSilhouetteAndReadableCellBoundedSizeInBothNativePalettes(bool village)
        {
            if(village)
            {
                using(var f=new Village3DIntegrationFixture())
                {
                    StoryletPart.LocalPlayer=f.Player;f.Refresh();
                    var giver=f.View("north-guard-west").owner;
                    Assert.IsTrue(f.Presenter.TryGetQuestCue(giver,out var marker,out var state));
                    Assert.AreEqual("Available",state);
                    AssertReadableAvailable(marker,()=>
                    {
                        StoryletPart.Current.StartQuest(new QuestState{QuestId=giver.GetPart<QuestBeaconPart>().Quest});
                        Village3DIntegrationFixture.TickFrame(f.Presenter);
                    });
                }
            }
            else
            {
                using(var f=Scene(out var giver))
                {
                    Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out var marker,out var state));
                    Assert.AreEqual("Available",state);
                    AssertReadableAvailable(marker,()=>
                    {
                        StoryletPart.Current.StartQuest(new QuestState{QuestId=giver.GetPart<QuestBeaconPart>().Quest});f.Frame();
                    });
                }
            }
        }

        private static void AssertReadableAvailable(GameObject marker,Action activate)
        {
            var filter=marker.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
            var material=marker.GetComponent<Renderer>().sharedMaterial;
            var borrowed=material.GetTexture("_BaseMap")??material.mainTexture;
            Assert.NotNull(borrowed);
            string path=AssetDatabase.GetAssetPath(borrowed);Assert.IsTrue(path.EndsWith("Palette.png"),path);
            var palette=new Texture2D(2,2);
            try
            {
                Assert.IsTrue(palette.LoadImage(File.ReadAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName,path))));
                var swatches=mesh.uv.Distinct().ToArray();
                Assert.AreEqual(2,swatches.Length,"The available sign needs a light face and a dark silhouette, not one hue that can disappear against terrain.");
                var ordered=swatches.OrderBy(uv=>PaletteLuminance(palette,uv)).ToArray();
                float dark=PaletteLuminance(palette,ordered[0]),light=PaletteLuminance(palette,ordered[1]);
                Assert.LessOrEqual(dark,.16f,"The real borrowed atlas must provide a dark silhouette.");
                Assert.GreaterOrEqual(light,.80f,"The real borrowed atlas must provide a pale face.");
                Assert.GreaterOrEqual(light-dark,.65f);
                var outline=SwatchBounds(mesh,ordered[0]);var face=SwatchBounds(mesh,ordered[1]);
                Assert.Less(outline.min.x,face.min.x-.015f);Assert.Greater(outline.max.x,face.max.x+.015f);
                Assert.Less(outline.min.y,face.min.y-.015f);Assert.Greater(outline.max.y,face.max.y+.015f);
                Assert.Less(outline.max.z,face.max.z,"Dark backing must not cover the pale camera-facing face.");
                Assert.That(mesh.bounds.size.y,Is.InRange(.65f,.80f));
                Assert.That(mesh.bounds.size.x,Is.InRange(.23f,.36f));
                Assert.Less(mesh.bounds.size.z,.20f);Assert.LessOrEqual(mesh.vertexCount,144);
                Assert.IsEmpty(marker.GetComponentsInChildren<Light>(true));
                activate();
                Assert.AreSame(material,marker.GetComponent<Renderer>().sharedMaterial,"State changes keep the existing fog/palette material.");
                Assert.AreNotSame(mesh,filter.sharedMesh);
                Assert.AreEqual(1,filter.sharedMesh.uv.Distinct().Count(),"The subdued ongoing diamond remains a different, single-swatch signal.");
                Assert.That(filter.sharedMesh.bounds.size.x,Is.InRange(.4f,.6f));
            }
            finally{UnityEngine.Object.DestroyImmediate(palette);}
        }

        private static float PaletteLuminance(Texture2D texture,Vector2 uv)
        {
            Color c=texture.GetPixel(Mathf.FloorToInt(uv.x*texture.width),Mathf.FloorToInt(uv.y*texture.height));
            return .2126f*c.r+.7152f*c.g+.0722f*c.b;
        }

        private static Bounds SwatchBounds(Mesh mesh,Vector2 uv)
        {
            var vertices=mesh.vertices;var uvs=mesh.uv;bool found=false;var result=new Bounds();
            for(int i=0;i<vertices.Length;i++)if(uvs[i]==uv)
            {if(!found){result=new Bounds(vertices[i],Vector3.zero);found=true;}else result.Encapsulate(vertices[i]);}
            Assert.IsTrue(found);return result;
        }

        [Test]
        public void SameIdInAForeignGraphCannotBorrowALiveGiversCue()
        {
            using(var f=Scene(out var giver))
            {
                Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(giver,f.Zone,f.Player));
                var foreign=new Zone(f.Zone.ZoneID);var impostor=f.Factory.CreateEntity("Villager");impostor.ID=giver.ID;
                impostor.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});foreign.AddEntity(impostor,20,12);
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(impostor,f.Zone,f.Player));
                Assert.IsFalse(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(impostor,out _,out _));
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(giver,foreign,f.Player));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void MorrowfastOfferingOwnerCannotAdvertiseItsLocalErrandFromAnotherScene(bool forgedCopy)
        {
            using(var f=new Village3DIntegrationFixture())
            {
                StoryletPart.Current=new StoryletPart();StoryletPart.LocalPlayer=f.Player;
                var original=f.View("north-guard-west").owner;
                Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(original,f.Zone,f.Player));
                var other=new Zone("Overworld.8.16.0");var giver=original;
                if(forgedCopy)
                {
                    giver=MorrowfastContent.CreateResident("north-guard-west",f.Factory);giver.ID=original.ID;
                }
                else f.Zone.RemoveEntity(giver);
                f.Zone.RemoveEntity(f.Player);other.AddEntity(f.Player,19,12);other.AddEntity(giver,20,12);
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(giver,other,f.Player),
                    "These authored conversations themselves require Morrowfast, unlike portable generic givers.");
            }
        }

        [Test]
        public void TwoOwnersShareMarkerResourcesButTheirDifferentQuestsChangeIndependently()
        {
            using(var f=Scene(out var giver))
            {
                var other=f.Add("Villager",22,12);other.AddPart(new QuestBeaconPart{Quest="HiddenShrine"});f.Refresh(new HashSet<int>());
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.IsTrue(presenter.TryGetQuestCue(giver,out var first,out var firstState));
                Assert.IsTrue(presenter.TryGetQuestCue(other,out var second,out var secondState));
                Assert.AreEqual("Available",firstState);Assert.AreEqual(firstState,secondState);
                Assert.AreNotSame(first,second);
                var mesh=first.GetComponent<MeshFilter>().sharedMesh;var material=first.GetComponent<Renderer>().sharedMaterial;
                Assert.AreSame(mesh,second.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(material,second.GetComponent<Renderer>().sharedMaterial);
                StoryletPart.Current.StartQuest(new QuestState{QuestId="ClearTheWarren"});f.Frame();
                Assert.IsTrue(presenter.TryGetQuestCue(giver,out _,out firstState));Assert.AreEqual("Active",firstState);
                Assert.IsTrue(presenter.TryGetQuestCue(other,out _,out secondState));Assert.AreEqual("Available",secondState);
                Assert.AreSame(mesh,second.GetComponent<MeshFilter>().sharedMesh,"Changing one cue cannot rewrite its shared source mesh.");
                Assert.AreSame(material,first.GetComponent<Renderer>().sharedMaterial);
            }
        }

        [Test]
        public void AddingABeaconLaterDoesNotExpandNativeSelectionOrReplaceTheActor()
        {
            using(var f=Scene(out var giver,false))
            {
                var body=f.View(giver);var collider=body.GetComponent<Collider>();var bounds=collider.bounds;
                int colliderCount=body.GetComponentsInChildren<Collider>(true).Length;
                Assert.IsFalse(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out _,out _));
                giver.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});f.Frame();
                Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out var marker,out _));
                Assert.AreSame(body,f.View(giver));Assert.AreEqual(colliderCount,body.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(bounds,collider.bounds);Assert.IsEmpty(marker.GetComponentsInChildren<Collider>(true));
                Assert.IsTrue(f.Zone.MoveEntity(giver,21,12));f.Refresh(new HashSet<int>());
                Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out var moved,out _));
                Assert.AreSame(marker,moved);Assert.AreEqual(body.transform.position.x,moved.transform.position.x,.001f);
                Assert.AreEqual(body.transform.position.z,moved.transform.position.z,.001f);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void FullRevealNeverOverridesExplicitInvisibleNativeOwner(bool reveal)
        {
            using(var f=Scene(out var giver))
            {
                f.Set("FullReveal",reveal);f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.IsTrue(presenter.TryGetQuestCue(giver,out var marker,out _));
                giver.GetPart<RenderPart>().Visible=false;f.Frame();
                Assert.IsFalse(presenter.TryGetQuestCue(giver,out _,out _));Assert.IsFalse(marker.activeInHierarchy);
                giver.GetPart<RenderPart>().Visible=true;f.Frame();Assert.IsTrue(presenter.TryGetQuestCue(giver,out _,out _));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void LosingPlayerOrJournalHidesCuesUntilTheRealContextReturns(bool removePlayer)
        {
            using(var f=Scene(out var giver))
            {
                var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.IsTrue(presenter.TryGetQuestCue(giver,out var marker,out _));
                if(removePlayer)f.Zone.RemoveEntity(f.Player);else StoryletPart.Current=null;
                f.Frame();Assert.IsFalse(presenter.TryGetQuestCue(giver,out _,out _));Assert.IsFalse(marker.activeInHierarchy);
                if(removePlayer)f.Zone.AddEntity(f.Player,19,12);else StoryletPart.Current=new StoryletPart();
                f.Frame();Assert.IsTrue(presenter.TryGetQuestCue(giver,out var restored,out _));Assert.AreSame(marker,restored);
            }
        }

        [Test]
        public void ReplacingAnOwnerWithTheSameSavedIdDoesNotKeepItsRemovedQuestMarker()
        {
            using(var f=Scene(out var giver))
            {
                var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.IsTrue(presenter.TryGetQuestCue(giver,out var marker,out _));
                var replacement=f.Factory.CreateEntity("Villager");replacement.ID=giver.ID;
                f.Zone.RemoveEntity(giver);f.Zone.AddEntity(replacement,20,12);f.Refresh(new HashSet<int>());
                Assert.IsFalse(presenter.TryGetQuestCue(giver,out _,out _));Assert.IsTrue(marker==null||!marker.activeInHierarchy);
                Assert.IsFalse(presenter.TryGetQuestCue(replacement,out _,out _));
                Assert.NotNull(f.View(replacement));
            }
        }

        [Test]
        public void LeavingTheZoneDestroysOwnedMeshesButKeepsTheBorrowedPaletteAndCamera()
        {
            using(var f=Scene(out var giver))
            {
                Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetQuestCue(giver,out var marker,out _));
                var mesh=marker.GetComponent<MeshFilter>().sharedMesh;var palette=f.Library.WorldMaterial;var source=f.Source;var target=f.Borrowed;
                f.Bind(new Zone("quest-cue-resource-exit"));
                Assert.IsTrue(mesh==null,"The presenter must release its transient cue mesh on exit.");
                Assert.NotNull(palette);Assert.NotNull(source);Assert.AreSame(target,source.targetTexture);
            }
        }

        private static SpawnRing3DIntegrationFixture Scene(out Entity giver,bool withBeacon=true)
        {
            var f=new SpawnRing3DIntegrationFixture("Overworld.8.16.0");
            f.Zone=new Zone("Overworld.8.16.0");f.Player=f.Add("Player",19,12);giver=f.Add("Villager",20,12);
            if(withBeacon)giver.AddPart(new QuestBeaconPart{Quest="ClearTheWarren"});
            StoryletPart.Current=new StoryletPart();StoryletPart.LocalPlayer=f.Player;
            f.Reveal();f.Bind(f.Zone);f.Refresh();return f;
        }
    }
}
