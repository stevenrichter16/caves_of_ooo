using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Public reader snapshots, including real parts and flipped visibility/ownership gates.
    /// Reflection lets the behavior suite fail assertions before the new document API exists.</summary>
    public sealed class ExamineDocumentTests
    {
        EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        static object Build(string name,params object[] arguments)
        {
            var type=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.ExamineDocumentBuilder");
            Assert.NotNull(type,"The shared field-ledger reader needs a truthful structured examine builder.");
            var method=type.GetMethod(name,BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method);return method.Invoke(null,arguments);
        }
        static object Property(object owner,string name)=>owner.GetType().GetProperty(name).GetValue(owner);
        static string Text(object document)=>(string)document.GetType().GetMethod("ToPlainText").Invoke(document,null);
        static string Fact(object document,string name)
        {
            foreach(var row in (IEnumerable)Property(document,"Facts"))
                if((string)Property(row,"Label")==name)return (string)Property(row,"Value");
            return null;
        }
        static string Section(object document,string name)
        {
            foreach(var row in (IEnumerable)Property(document,"Sections"))
                if((string)Property(row,"Heading")==name)return (string)Property(row,"Text");
            return null;
        }
        static Entity Plain(string name="widget")
        {
            var item=new Entity { ID=Guid.NewGuid().ToString("N"),BlueprintName="LedgerWidget" };item.SetTag("Item");
            item.AddPart(new PhysicsPart { Takeable=true,Weight=3 });
            item.AddPart(new RenderPart { DisplayName=name,RenderString="!",ColorString="&C" });
            item.AddPart(new ExaminablePart { Text="Old field notes." });return item;
        }
        static Zone Place(Entity owner)
        {
            var zone=new Zone("ledger-read");zone.AddEntity(owner,10,10);
            zone.GetCell(10,10).Explored=zone.GetCell(10,10).IsVisible=true;return zone;
        }
        static Entity Carry(Entity owner)
        {
            var actor=new Entity();actor.AddPart(new InventoryPart());
            actor.GetPart<InventoryPart>().Objects.Add(owner);owner.GetPart<PhysicsPart>().InInventory=actor;return actor;
        }
        [Test] public void RealSubjectHasSnapshotMetadataAndExplicitComposition()
        {
            var item=Plain();item.AddPart(new MaterialPart { MaterialID="Iron" });
            item.AddPart(new CompositionPart { MaterialsRaw="Iron, Wood,Iron,, " });
            var zone=Place(item);var document=Build("BuildWorld",item,zone,zone.GetCell(10,10),null);
            Assert.AreEqual("widget",Property(document,"Title"));Assert.AreEqual("EXAMINE",Property(document,"Kind"));
            Assert.AreEqual("LedgerWidget",Property(document,"BlueprintName"));Assert.AreEqual('!',Property(document,"Glyph"));
            Assert.AreEqual("&C",Property(document,"GlyphColor"));Assert.AreEqual("Iron",Fact(document,"Material"));
            Assert.AreEqual("Iron, Wood",Fact(document,"Composition"));StringAssert.Contains("Old field notes.",Text(document));
            StringAssert.Contains("Composition: Iron, Wood",Text(document));
        }
        [TestCase("no-material")] [TestCase("generic")] [TestCase("blank")]
        public void NameNeverInventsCompositionOrReactiveMaterial(string mode)
        {
            var item=Plain("gold-plated iron sword");
            if(mode!="no-material")item.AddPart(new MaterialPart { MaterialID=mode=="generic"?"Generic":" " });
            var document=Build("BuildInventory",item,Carry(item));
            Assert.IsNull(Fact(document,"Material"));Assert.IsNull(Fact(document,"Composition"));
            StringAssert.DoesNotContain("Combustibility",Text(document));
        }
        [TestCase("cell")] [TestCase("unexplored")] [TestCase("render")] [TestCase("wrong-cell")]
        [TestCase("other-zone")] [TestCase("removed")] [TestCase("carried-alias")]
        public void WorldReaderRejectsHiddenStaleOrNonPhysicalSubjects(string reason)
        {
            var item=Plain();var zone=Place(item);var cell=zone.GetCell(10,10);
            Assert.NotNull(Build("BuildWorld",item,zone,cell,null));
            if(reason=="cell")cell.IsVisible=false;
            if(reason=="unexplored")cell.Explored=false;
            if(reason=="render")item.GetPart<RenderPart>().Visible=false;
            if(reason=="wrong-cell")cell=zone.GetCell(11,10);
            if(reason=="other-zone")zone=new Zone("unrelated");
            if(reason=="removed")zone.RemoveEntity(item);
            if(reason=="carried-alias")item.GetPart<PhysicsPart>().InInventory=new Entity();
            Assert.IsNull(Build("BuildWorld",item,zone,cell,null));
        }
        [Test] public void InventoryReaderRequiresCurrentCarriedOwnerAndNeverReadsAWorldAlias()
        {
            var item=Plain();var actor=Carry(item);
            Assert.NotNull(Build("BuildInventory",item,actor));
            Assert.IsNull(Build("BuildInventory",item,new Entity()));
            var zone=Place(item);Assert.IsNull(Build("BuildInventory",item,actor));
        }
        [Test] public void ConditionsPrecedeLongFlavorAndRetainCurrentDurationWithoutMutatingIt()
        {
            var item=Plain();item.AddPart(new StatusEffectsPart());var guard=new EnGardeEffect { Duration=3 };
            Assert.True(item.GetPart<StatusEffectsPart>().ForceApplyEffect(guard));
            item.GetPart<ExaminablePart>().Text=new string('x',2400)+"authored ending";
            var actor=Carry(item);var document=Build("BuildInventory",item,actor);string text=Text(document);
            Assert.Less(text.IndexOf("En Garde",StringComparison.Ordinal),text.IndexOf("xxxx",StringComparison.Ordinal));
            StringAssert.Contains("3 of your turns left",Section(document,"Current state"));
            StringAssert.Contains("authored ending",text);Assert.AreEqual(3,guard.Duration);Assert.False(guard.Spent);
            guard.Duration=1;StringAssert.Contains("1 of your turns left",Text(Build("BuildInventory",item,actor)));
            StringAssert.Contains("3 of your turns left",text,"An open document remains its captured snapshot.");
        }
        [Test] public void EquipmentUsesLiveMechanicsAndHasNoInventedPriceOrRarity()
        {
            var item=factory.CreateEntity("LongSword");var actor=Carry(item);var weapon=item.GetPart<MeleeWeaponPart>();
            weapon.BaseDamage="2d3+7";weapon.PenBonus=-4;weapon.HitBonus=-2;
            var document=Build("BuildInventory",item,actor);string text=Text(document);
            StringAssert.Contains("Damage: 2d3+7 per penetration",text);StringAssert.Contains("Penetration bonus: -4",text);
            StringAssert.Contains("Hit bonus: -2",text);Assert.IsNull(Fact(document,"Price"));Assert.IsNull(Fact(document,"Rarity"));
            Assert.NotNull(Section(document,"Use & mechanics"));
        }
        [Test] public void HealthyObjectDoesNotReceiveAnInventedAffliction()
        {
            var item=Plain();var document=Build("BuildInventory",item,Carry(item));
            Assert.IsNull(Section(document,"Current state"));StringAssert.DoesNotContain("Broken",Text(document));
        }
        [TestCase("HealingTonic","Heals")]
        [TestCase("Mace","Stunned")]
        [TestCase("LongSword","Bleeding")]
        [TestCase("Spear","Confused")]
        [TestCase("VenomDagger","Poisoned")]
        public void ExistingConsumableAndEquipmentPayloadsRemainReadable(string blueprint,string expected)
        {
            var item=factory.CreateEntity(blueprint);var document=Build("BuildInventory",item,Carry(item));
            StringAssert.Contains(expected,Text(document));
        }
        [TestCase("Chest")] [TestCase("MarlbackScrabbler")] [TestCase("Warden")]
        [TestCase("StoneWall")] [TestCase("Floor")] [TestCase("FireClay")] [TestCase("KnotflaxCord")]
        public void WorldContentFamiliesHaveTruthfulNonemptyDescriptions(string blueprint)
        {
            var item=factory.CreateEntity(blueprint);var zone=Place(item);var document=Build("BuildWorld",item,zone,zone.GetCell(10,10),null);
            Assert.NotNull(document);Assert.AreEqual(item.GetDisplayName(),Property(document,"Title"));
            Assert.That(Text(document).Length,Is.GreaterThan(item.GetDisplayName().Length));
            StringAssert.DoesNotContain("null",Text(document));
        }
        [Test] public void StructuralConditionIsIndependentFromRepairAndNeverClaimsHealedCombatHp()
        {
            var item=Plain();item.GetPart<PhysicsPart>().Takeable=false;
            item.AddPart(new DestructiblePart { HP=7,MaxHP=20 });
            item.AddPart(new RepairablePart { RecipeId="unregistered-fault" });
            var zone=Place(item);var document=Build("BuildWorld",item,zone,zone.GetCell(10,10),null);
            Assert.AreEqual("7 / 20",Fact(document,"Structure"));
            StringAssert.Contains("cannot be repaired with a known method",Text(document));
            StringAssert.DoesNotContain("restores HP",Text(document));Assert.AreEqual(7,item.GetPart<DestructiblePart>().HP);
        }
        [Test] public void EmptySubjectsAreRefused()
        {
            Assert.IsNull(Build("BuildInventory",null,null));
            Assert.IsNull(Build("BuildWorld",null,null,null,null));
        }
    }
}
