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
    public sealed class SpreadPortableIntegrationTests
    {
        private const string Spread = "Overworld.12.10.0";
        private static Entity Create(SpawnRing3DIntegrationFixture f,string name)
        {
            if(name!="DetectiveNotebook"&&name!="CrunchyLocket")return f.Factory.CreateEntity(name);
            var item=new Entity{BlueprintName=name};item.SetTag("Item");
            item.AddPart(new PhysicsPart{Takeable=true});
            item.AddPart(new RenderPart{RenderString=name=="DetectiveNotebook"?"=":"*",ColorString=name=="DetectiveNotebook"?"&w":"&Y"});
            item.AddPart(new CompleteObjectiveOnTaken{Quest=name=="DetectiveNotebook"?"RootBeerGuyCase":"CrunchyLocket",Objective=name=="DetectiveNotebook"?"find_notebook":"find_locket"});
            return item;
        }
        private static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity item)=>SpawnRing3DRecipes.Resolve(f.Zone,item,f.Library.Definition);
        private static Entity Add(SpawnRing3DIntegrationFixture f,string name)
        {var item=Create(f,name);var cell=f.FreeCell();Assert.True(f.Zone.AddEntity(item,cell.x,cell.y));return item;}
        private static void Exact(SpawnRing3DIntegrationFixture f,Entity item)
        {
            Assert.True(SpreadPortableRecipes.TryRecipe(item,out string expected));
            var recipe=Recipe(f,item);Assert.AreEqual(expected,recipe.ModelId,recipe.Failure);Assert.AreSame(item,recipe.Owner);
            Assert.True(recipe.Transient);Assert.False(recipe.Batched);
            var library=SpreadPortable3DLibrary.Load();var entry=library.Find(expected);
            Assert.AreSame(entry.Spec,f.Library.Definition.FindModel(expected));Assert.AreSame(entry.Prefab,f.Library.FindModel(expected));
            Assert.True(f.Find(item,out var root,out string actual));Assert.AreEqual(expected,actual);Assert.True(f.Rendered(item));
            Assert.AreSame(entry.Mesh,root.GetComponent<MeshFilter>().sharedMesh);
            var material=f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(library.Material);
            Assert.AreSame(material,root.GetComponent<MeshRenderer>().sharedMaterial);Assert.AreNotSame(library.Material,material);
            Assert.AreSame(library.Palette,material.GetTexture("_BaseMap"));Assert.AreSame(library.Material.shader,material.shader);
            Assert.True(f.Pick(item,out _));Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
        }
        [TestCase("Cudgel")][TestCase("Hatchet")][TestCase("LeatherBoots")][TestCase("DetectiveNotebook")][TestCase("Torch")]
        public void ActualCensusGapUsesExactAdoptedMeshAndOwnedPalette(string name)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var item=Add(f,name);int version=f.Zone.EntityVersion;var owners=f.Zone.GetReadOnlyEntities().ToArray();
                f.Refresh();Exact(f,item);f.Refresh();Exact(f,item);
                Assert.AreEqual(version,f.Zone.EntityVersion);CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());
            }
        }
        [TestCase("carried")][TestCase("equipped")][TestCase("hidden")][TestCase("removed")]
        [TestCase("foreign-physics")][TestCase("visual-override")][TestCase("natural")][TestCase("creature")]
        public void RejectedNativeOwnerNeverClaimsPortableGroundGeometry(string mode)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var item=Add(f,"Dagger");var physics=item.GetPart<PhysicsPart>();var render=item.GetPart<RenderPart>();
                switch(mode){case "carried":physics.InInventory=f.Player;break;case "equipped":physics.Equipped=f.Player;break;
                    case "hidden":render.Visible=false;break;case "removed":f.Zone.RemoveEntity(item);break;
                    case "foreign-physics":physics.ParentEntity=new Entity();break;case "visual-override":render.VisualID="foreign";break;
                    case "natural":item.SetTag("Natural");break;case "creature":item.SetTag("Creature");break;}
                var recipe=Recipe(f,item);Assert.IsNull(recipe.ModelId,mode);f.Refresh();Assert.False(f.Authored(item),mode);
            }
        }
        [Test]
        public void ActualPickupDropHiddenRemovalAndSaveKeepExactIdentity()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                // This fixture creates the bare blueprint, so its free native hand
                // auto-equips a positive single dagger during PickupCommand.
                Assert.IsEmpty(f.Player.GetPart<InventoryPart>().Objects);
                Assert.IsEmpty(f.Player.GetPart<InventoryPart>().EquippedItems);
                var item=Add(f,"Dagger");string id=item.ID;f.Approach(item);f.Refresh();Exact(f,item);var old=f.View(item);
                Assert.True(InventorySystem.Pickup(f.Player,item,f.Zone));f.Refresh();Assert.False(f.Authored(item));SpawnRing3DIntegrationFixture.Hidden(old);
                Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);
                Assert.AreSame(item,f.Player.GetPart<InventoryPart>().FindEquippedBodyPart(item)._Equipped);
                Assert.AreEqual(1,item.GetPart<StackerPart>().StackCount);
                Assert.True(InventorySystem.Drop(f.Player,item,f.Zone));f.Refresh();Exact(f,item);
                Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
                var render=item.GetPart<RenderPart>();old=f.View(item);render.Visible=false;f.Refresh();Assert.False(f.Authored(item));SpawnRing3DIntegrationFixture.Hidden(old);
                render.Visible=true;f.Refresh();Exact(f,item);
                var loaded=f.RoundTrip();f.BindLoaded(loaded);var restored=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);
                Assert.AreNotSame(item,restored);Exact(f,restored);old=f.View(restored);f.Zone.RemoveEntity(restored);f.Refresh();
                Assert.False(f.Authored(restored));SpawnRing3DIntegrationFixture.Hidden(old);
            }
        }
        [Test]
        public void CurrentReceivingBiomeAndForeignGraphCannotBorrowPortableStyle()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var item=Add(f,"Dagger");f.Refresh();Exact(f,item);var first=f.View(item);
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Beating;f.Refresh();
                Assert.False((Recipe(f,item).ModelId??"").StartsWith("spread-portable-",StringComparison.Ordinal));SpawnRing3DIntegrationFixture.Hidden(first);
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;f.Refresh();Exact(f,item);
                var foreign=new Zone(f.Zone.ZoneID);Assert.IsNull(SpawnRing3DRecipes.Resolve(foreign,item,f.Library.Definition).ModelId);
            }
        }
        [TestCase("wrong-glyph")][TestCase("wrong-objective")][TestCase("foreign-part")]
        public void ConstructedQuestObjectCannotUseMissingArtAsAnAuthorityBypass(string mode)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var token=Add(f,"CrunchyLocket");
                if(mode=="wrong-glyph")token.GetPart<RenderPart>().RenderString="?";
                if(mode=="wrong-objective")token.GetPart<CompleteObjectiveOnTaken>().Objective="not-this-token";
                if(mode=="foreign-part")token.GetPart<CompleteObjectiveOnTaken>().ParentEntity=new Entity();
                Assert.IsNull(Recipe(f,token).ModelId);f.Refresh();Assert.False(f.Authored(token));
            }
        }
        [Test]
        public void CraftingStateChangesReplaceOnlyOwnedView()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var item=Add(f,"BrewedTonic");var part=new BrewItemPart{Form="Food"};item.AddPart(part);f.Refresh();Exact(f,item);var first=f.View(item);
                part.Form="Throwable";f.Refresh(f.Dirty(item));Exact(f,item);Assert.AreNotSame(first,f.View(item));SpawnRing3DIntegrationFixture.Hidden(first);
                part.Form="invalid";f.Refresh(f.Dirty(item));Assert.False(f.Authored(item));Assert.AreEqual("invalid",part.Form);
            }
        }
        [Test]
        public void CorpsesUseOriginalSpeciesThenRefuseWrongProvenance()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var corpse=Add(f,"CreatureCorpse");corpse.Properties["SourceBlueprint"]="CaveBat";corpse.Properties["SourceID"]="actual-prior-source";
                f.Refresh();Exact(f,corpse);Assert.AreEqual("spread-portable-corpse-bat",Recipe(f,corpse).ModelId);
                corpse.Properties["SourceBlueprint"]="UnknownSpecies";f.Refresh();Assert.False(f.Authored(corpse));
                Assert.AreEqual("actual-prior-source",corpse.Properties["SourceID"]);
            }
        }
        [Test]
        public void FilledFlaskUsesCurrentRegisteredColorWithoutMutatingVolume()
        {
            var field=typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic);
            var state=typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
            var live=(Dictionary<string,LiquidDefinition>)field.GetValue(null);var before=new Dictionary<string,LiquidDefinition>(live);bool initialized=LiquidRegistry.IsInitialized;
            try
            {
                LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(x=>x.text));
                using(var f=new SpawnRing3DIntegrationFixture(Spread))
                {
                    var flask=Add(f,"LiquidFlask");var part=flask.GetPart<LiquidVesselPart>();part.Volume=6;part.LiquidId="water";f.Refresh();Exact(f,flask);var first=f.View(flask);
                    part.LiquidId="acid";f.Refresh();Exact(f,flask);Assert.AreNotSame(first,f.View(flask));SpawnRing3DIntegrationFixture.Hidden(first);Assert.AreEqual(6,part.Volume);
                    part.Volume=0;part.LiquidId="";f.Refresh();Exact(f,flask);Assert.AreEqual("spread-portable-liquidflask",Recipe(f,flask).ModelId);
                }
            }
            finally{live.Clear();foreach(var pair in before)live.Add(pair.Key,pair.Value);state.SetValue(null,initialized);}
        }
    }
}
