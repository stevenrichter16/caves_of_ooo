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
        private const string GroveRedWarning = "The sign says eat nothing red.";
        private const string GroveRedClue = "At an alchemy still, cleanse one harvested grove-red into pulp. Brew the pulp by itself for a mending tonic.";

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

        [Test]
        public void VisibleGroveRedGrowthAddsThePreparationClueToBothReadersWithoutChangingItsWarning()
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            var examine = growth.GetPart<ExaminablePart>();
            var harvest = growth.GetPart<HarvestablePart>();
            var zone = Place(growth);
            var anchor = zone.GetCell(10, 10);
            string authored = examine.Text;
            int tick = WorldClock.CurrentTick;

            string full = ExamineDocumentBuilder.BuildWorld(growth, zone, anchor).ToPlainText();
            string quick = examine.BuildWorldExamineLine(zone, anchor);

            StringAssert.Contains(GroveRedClue, full);
            StringAssert.Contains(GroveRedClue, quick);
            StringAssert.Contains(GroveRedWarning, full);
            StringAssert.Contains(GroveRedWarning, quick);
            Assert.AreEqual(authored, examine.Text);
            Assert.AreEqual(tick, WorldClock.CurrentTick);
            Assert.AreSame(anchor, zone.GetEntityCell(growth));
            Assert.IsFalse(harvest.Harvested);
        }

        [TestCase("spent")]
        [TestCase("wrong-yield")]
        [TestCase("wrong-blueprint")]
        [TestCase("zero-min")]
        [TestCase("reversed-range")]
        [TestCase("max-overflow")]
        [TestCase("zero-chance")]
        [TestCase("chance-overflow")]
        [TestCase("foreign-harvest-part")]
        public void InvalidGroveRedSourceRetainsTheWarningWithoutOfferingPreparation(string fault)
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            var harvest = growth.GetPart<HarvestablePart>();
            var zone = Place(growth);
            var anchor = zone.GetCell(10, 10);
            switch (fault)
            {
                case "spent": harvest.Harvested = true; break;
                case "wrong-yield": harvest.YieldBlueprint = "Mushroom"; break;
                case "wrong-blueprint": growth.BlueprintName = "OtherGrowth"; break;
                case "zero-min": harvest.YieldMin = 0; break;
                case "reversed-range": harvest.YieldMax = harvest.YieldMin - 1; break;
                case "max-overflow": harvest.YieldMax = HarvestablePart.MaximumYieldCount + 1; break;
                case "zero-chance": harvest.YieldChance = 0; break;
                case "chance-overflow": harvest.YieldChance = 101; break;
                case "foreign-harvest-part": harvest.ParentEntity = new Entity(); break;
            }

            string full = ExamineDocumentBuilder.BuildWorld(growth, zone, anchor).ToPlainText();
            string quick = growth.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, anchor);

            foreach (string text in new[] { full, quick })
            {
                StringAssert.Contains(GroveRedWarning, text);
                StringAssert.DoesNotContain("cleanse one harvested grove-red", text);
            }
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void DarkOrUnexploredGroveRedDoesNotExposeTheClueInEitherReader(bool visible, bool explored)
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            var zone = Place(growth);
            var anchor = zone.GetCell(10, 10);
            anchor.IsVisible = visible;
            anchor.Explored = explored;

            Assert.IsNull(ExamineDocumentBuilder.BuildWorld(growth, zone, anchor));
            StringAssert.DoesNotContain("cleanse one harvested grove-red",
                growth.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, anchor));
        }

        [Test]
        public void InactiveGroveRedZoneDoesNotExposeTheClueInEitherReader()
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            var zone = Place(growth);
            var anchor = zone.GetCell(10, 10);
            var manager = OverworldZoneManager.CreateDetached(factory, 65);
            manager.SetActiveZone(zone);
            Assert.AreSame(manager, WorldLocationContext.For(zone));
            manager.SetActiveZone(new Zone("Overworld.1.1.0"));

            var document = ExamineDocumentBuilder.BuildWorld(growth, zone, anchor);
            if (document != null) StringAssert.DoesNotContain(GroveRedClue, document.ToPlainText());
            StringAssert.DoesNotContain(GroveRedClue, growth.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, anchor));
        }

        [Test]
        public void AVisibleSecondaryGroveRedFootprintIsNotTheAnchorForTheClue()
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            growth.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            var zone = Place(growth);
            var secondary = zone.GetCell(11, 10);
            secondary.IsVisible = secondary.Explored = true;
            Assert.Contains(growth, secondary.Occupants.ToList());
            Assert.AreNotSame(secondary, zone.GetEntityCell(growth));

            var document = ExamineDocumentBuilder.BuildWorld(growth, zone, secondary);
            Assert.NotNull(document, "The ordinary world reader still allows a visible footprint cell.");
            StringAssert.DoesNotContain("cleanse one harvested grove-red", document.ToPlainText());
            StringAssert.DoesNotContain("cleanse one harvested grove-red",
                growth.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, secondary));
        }

        [Test]
        public void CarriedGroveRedSourceDoesNotOfferAnAnchoredForagingClue()
        {
            var growth = factory.CreateEntity("GroveRedGrowth");
            var zone = Place(growth);
            var anchor = zone.GetCell(10, 10);
            Assert.True(zone.RemoveEntity(growth));
            var actor = Carry(growth);
            var examine = growth.GetPart<ExaminablePart>();

            Assert.IsNull(ExamineDocumentBuilder.BuildWorld(growth, zone, anchor, actor));
            StringAssert.DoesNotContain("cleanse one harvested grove-red", examine.BuildWorldExamineLine(zone, anchor, actor));
            StringAssert.DoesNotContain("cleanse one harvested grove-red", examine.BuildExamineLine());
            StringAssert.DoesNotContain("cleanse one harvested grove-red",
                ExamineDocumentBuilder.BuildInventory(growth, actor).ToPlainText());
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
        [Test] public void ALeakingWellPutsItsExactCompatibleMaterialsInTheFirstWarning()
        {
            var well = factory.CreateEntity("RepairLinedWell");
            var zone = Place(well);
            var cell = zone.GetCell(10, 10);
            var warning = ExamineDocumentBuilder.BuildWorld(well, zone, cell).Sections.First();
            Assert.AreEqual("Needs repair", warning.Heading);
            Assert.AreEqual(AnnouncementTone.Warning, warning.Tone);
            StringAssert.Contains("2 measures of fire clay or prepared lining clay, in any mix", warning.Text);
            StringAssert.Contains("NEEDS REPAIR", well.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, cell));
            well.GetPart<RepairablePart>().Repaired = true;
            Assert.IsFalse(ExamineDocumentBuilder.BuildWorld(well, zone, cell).Sections.Any(s => s.Heading == "Needs repair"));
        }

        [Test] public void PortableDamageShowsTheActualBillAndClearsAfterTheEffectEnds()
        {
            var dagger = factory.CreateEntity("Dagger");
            var player = Carry(dagger);
            dagger.ApplyEffect(new BrokenEffect());
            var warning = ExamineDocumentBuilder.BuildInventory(dagger, player).Sections.First();
            Assert.AreEqual("Needs repair", warning.Heading);
            Assert.AreEqual(AnnouncementTone.Warning, warning.Tone);
            StringAssert.Contains("1 steel blade", warning.Text);
            dagger.RemoveEffect<BrokenEffect>();
            Assert.IsFalse(ExamineDocumentBuilder.BuildInventory(dagger, player).Sections.Any(s => s.Heading == "Needs repair"));
        }

        [TestCase("Dagger", "-2 melee accuracy.")]
        [TestCase("LeatherArmor", "-1 armor.")]
        public void BrokenEquipmentShowsAReadableSignedPenalty(string blueprint, string penalty)
        {
            var item = factory.CreateEntity(blueprint);
            var player = Carry(item);
            StringAssert.DoesNotContain("Broken:", ExamineDocumentBuilder.BuildInventory(item, player).ToPlainText());

            Assert.IsTrue(item.ApplyEffect(new BrokenEffect()));
            Assert.IsTrue(item.GetEffect<BrokenEffect>().PenaltyApplied);
            var document = ExamineDocumentBuilder.BuildInventory(item, player);
            var state = document.Sections.Single(section => section.Heading == "Current state");
            StringAssert.Contains("Broken: " + penalty, state.Text);
        }

        [Test] public void BrokenObjectWithNoApplicableRecipeShowsNoInventedBill()
        {
            var item = Plain();
            item.AddPart(new StatusEffectsPart());
            item.ApplyEffect(new BrokenEffect());
            var warning = ExamineDocumentBuilder.BuildInventory(item, Carry(item)).Sections.First();
            Assert.AreEqual("Needs repair", warning.Heading);
            StringAssert.Contains("No known repair method", warning.Text);
        }
        [Test] public void EmptySubjectsAreRefused()
        {
            Assert.IsNull(Build("BuildInventory",null,null));
            Assert.IsNull(Build("BuildWorld",null,null,null,null));
        }
    }
}
