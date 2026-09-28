using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadGenerationReceiptTests
    {
        DensityLootTestScope scope;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){scope.Dispose();}
        static void Capture(object builder,bool enabled)
        {var field=builder.GetType().GetField("CaptureSourceReceipts");Assert.NotNull(field,"Optional exact source capture must exist.");field.SetValue(builder,enabled);}
        static object Receipt(object builder)
        {var property=builder.GetType().GetProperty("SourceReceipt");Assert.NotNull(property,"Source receipt must expose exact successful owners.");return property.GetValue(builder);}
        static object Value(object value,string name)
        {Assert.NotNull(value,"Receipt is required after opted-in build.");var p=value.GetType().GetProperty(name);Assert.NotNull(p,name);return p.GetValue(value);}
        static Entity[] Owners(object receipt)=>((IEnumerable)Value(receipt,"Owners")).Cast<Entity>().ToArray();
        static bool Current(object receipt)=>(bool)Value(receipt,"IsCurrent");
        static int Revision(object receipt)=>(int)Value(receipt,"Revision");
        static string Shape(Zone zone)=>string.Join(";",zone.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+zone.GetEntityPosition(e)+":"+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item).OrderBy(x=>x))+":"+string.Join(",",DensityLootTestScope.Gear(e).Select(Item).OrderBy(x=>x))).OrderBy(x=>x));
        static string Item(Entity e)=>e.BlueprintName+"*"+(e.GetPart<StackerPart>()?.StackCount??1);
        static PopulationTable Group(string blueprint="MarlbackScrabbler",int count=2)
        {var t=new PopulationTable{Name="SpreadTier1"};t.Entries.Add(new PopulationEntry{BlueprintName=blueprint,EncounterGroup="SpreadTier1Encounter",MinCount=count,MaxCount=count});t.Entries.Add(new PopulationEntry{BlueprintName="Magpie",MinCount=1,MaxCount=1});return t;}
        static Zone NewZone()=>new Zone("Overworld.8.8.0");
        ContainerBuilder Containers()=>new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);

        [TestCase(true)][TestCase(false)]
        public void LegacySourcesStillGenerateOrdinaryContentWithoutOptIn(bool population)
        {var z=NewZone();if(population)new PopulationBuilder(Group()).BuildZone(z,scope.Factory,new Random(14));else Containers().BuildZone(z,scope.Factory,new Random(14));Assert.Greater(z.EntityCount,0);}
        [TestCase(true)][TestCase(false)]
        public void OptInCapturesActualSourceOwnersAndExactZoneWithoutRngOrShapeChanges(bool population)
        {
            var a=NewZone();var b=NewZone();var ra=new Random(14);var rb=new Random(14);scope.Seed(8);
            IZoneBuilder first=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();first.BuildZone(a,scope.Factory,ra);string shape=Shape(a);
            scope.Seed(8);IZoneBuilder second=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(second,true);second.BuildZone(b,scope.Factory,rb);var receipt=Receipt(second);
            Assert.AreSame(b,Value(receipt,"Zone"));Assert.True(Current(receipt));Assert.Greater(Revision(receipt),0);Assert.AreEqual(shape,Shape(b));Assert.AreEqual(ra.Next(),rb.Next());
            var owners=Owners(receipt);Assert.Greater(owners.Length,0);Assert.True(owners.All(e=>ReferenceEquals(b.GetEntityCell(e)?.Objects.SingleOrDefault(x=>ReferenceEquals(x,e)),e)));
            if(population)CollectionAssert.AreEqual(new[]{"MarlbackScrabbler","MarlbackScrabbler"},owners.Select(e=>e.BlueprintName));else Assert.True(owners.All(e=>e.HasPart<ContainerPart>()));
        }
        [TestCase(true)][TestCase(false)]
        public void DisabledCaptureLeavesNoReceipt(bool population)
        {IZoneBuilder b=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(b,false);b.BuildZone(NewZone(),scope.Factory,new Random(2));Assert.IsNull(Receipt(b));}
        [TestCase(true)][TestCase(false)]
        public void SameAddressDifferentBuildInvalidatesPriorReceipt(bool population)
        {IZoneBuilder b=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(b,true);var a=NewZone();b.BuildZone(a,scope.Factory,new Random(2));var old=Receipt(b);var next=NewZone();b.BuildZone(next,scope.Factory,new Random(3));Assert.False(Current(old));Assert.AreSame(next,Value(Receipt(b),"Zone"));Assert.Greater(Revision(Receipt(b)),Revision(old));}
        [TestCase(true)][TestCase(false)]
        public void OptOutRebuildInvalidatesAndClearsPriorReceipt(bool population)
        {IZoneBuilder b=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(2));var old=Receipt(b);Capture(b,false);b.BuildZone(NewZone(),scope.Factory,new Random(3));Assert.False(Current(old));Assert.IsNull(Receipt(b));}
        [TestCase("ambient")][TestCase("other-group")][TestCase("table-name")]
        public void AmbiguousOrForeignGroupCannotBecomeOrdinaryHostileAuthority(string change)
        {var t=Group();if(change=="table-name")t.Name="other";else t.Entries.Add(new PopulationEntry{BlueprintName="MarlbackScrabbler",MinCount=1,MaxCount=1,EncounterGroup=change=="ambient"?null:"other"});var b=new PopulationBuilder(t);Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(7));Assert.IsEmpty(Owners(Receipt(b)));}
        [Test] public void PreexistingSameBlueprintOwnersAndStampContainersAreNotCaptured()
        {var z=NewZone();var foreignActor=scope.Factory.CreateEntity("MarlbackScrabbler");var foreignCache=scope.Factory.CreateEntity("Crate");z.AddEntity(foreignActor,4,4);z.AddEntity(foreignCache,6,4);var p=new PopulationBuilder(Group());var c=Containers();Capture(p,true);Capture(c,true);p.BuildZone(z,scope.Factory,new Random(4));c.BuildZone(z,scope.Factory,new Random(5));Assert.False(Owners(Receipt(p)).Contains(foreignActor));Assert.False(Owners(Receipt(c)).Contains(foreignCache));}
        [TestCase("moved")][TestCase("removed")][TestCase("same-id-replacement")][TestCase("carried")][TestCase("physics-backlink")][TestCase("health")]
        public void OwnerMutationInvalidatesPopulationReceipt(string change)
        {var z=NewZone();var b=new PopulationBuilder(Group());Capture(b,true);b.BuildZone(z,scope.Factory,new Random(4));var r=Receipt(b);var e=Owners(r)[0];var p=z.GetEntityPosition(e);
            if(change=="moved"){Assert.True(z.MoveEntity(e,(p.x+1)%Zone.Width,p.y));Assert.AreNotEqual(p,z.GetEntityPosition(e));}if(change=="removed")z.RemoveEntity(e);if(change=="same-id-replacement"){z.RemoveEntity(e);var replacement=scope.Factory.CreateEntity(e.BlueprintName);replacement.ID=e.ID;z.AddEntity(replacement,p.x,p.y);}if(change=="carried")e.GetPart<PhysicsPart>().InInventory=new Entity();if(change=="physics-backlink")e.GetPart<PhysicsPart>().ParentEntity=new Entity();if(change=="health")e.GetStat("Hitpoints").BaseValue--;
            Assert.False(Current(r));}
        [TestCase("remove-item")][TestCase("quantity")][TestCase("replaced-part")]
        public void StockMutationInvalidatesContainerReceipt(string change)
        {var z=NewZone();var b=Containers();Capture(b,true);b.BuildZone(z,scope.Factory,new Random(14));var r=Receipt(b);var e=Owners(r)[0];var cp=e.GetPart<ContainerPart>();Assert.Greater(cp.Contents.Count,0);
            if(change=="remove-item")cp.RemoveItem(cp.Contents[0]);if(change=="quantity"){var item=cp.Contents[0];var stack=item.GetPart<StackerPart>();if(stack==null){stack=new StackerPart();item.AddPart(stack);}stack.StackCount++;}if(change=="replaced-part"){e.RemovePart(cp);e.AddPart(new ContainerPart());}Assert.False(Current(r));}
        [TestCase(true)][TestCase(false)]
        public void UnrelatedLaterPlacementDoesNotInvalidateExactReceipt(bool population)
        {var z=NewZone();IZoneBuilder b=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(b,true);b.BuildZone(z,scope.Factory,new Random(4));var r=Receipt(b);z.AddEntity(scope.Factory.CreateEntity("Magpie"),0,0);Assert.True(Current(r),"Later haulables/population legitimately change zone revision.");}

        public sealed class ReceiptCreatedPart:Part
        {public static Action<Entity> Callback;public override string Name=>"ReceiptCreated";public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
        [TestCase("duplicate-id")][TestCase("empty-id")][TestCase("already-carried")]
        public void MalformedFactoryOwnerCannotAuthorizeAReplacementAtCapture(string change)
        {
            scope.Factory.RegisterPartType<ReceiptCreatedPart>("ReceiptCreated");scope.Factory.Blueprints["MarlbackScrabbler"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            ReceiptCreatedPart.Callback=e=>{if(change=="duplicate-id")e.ID="same-id";if(change=="empty-id")e.ID="";if(change=="already-carried")e.GetPart<PhysicsPart>().InInventory=new Entity();};
            try{var b=new PopulationBuilder(Group());Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(8));Assert.False(Current(Receipt(b)));}finally{ReceiptCreatedPart.Callback=null;}
        }
        [Test]public void MissingOneRequiredGroupBlueprintCannotMakeAPartialGroupAuthoritative()
        {
            var t=Group();t.Entries[0].BlueprintName="MissingActor";var b=new PopulationBuilder(t);Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(8));Assert.IsEmpty(Owners(Receipt(b)));Assert.False(Current(Receipt(b)));
        }
        [Test]public void StockForeignCarryBacklinkCannotRemainCurrent()
        {
            var b=Containers();Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(14));var r=Receipt(b);
            var cp=Owners(r)[0].GetPart<ContainerPart>();Assert.Greater(cp.Contents.Count,0);
            cp.Contents[0].GetPart<PhysicsPart>().InInventory=new Entity();Assert.False(Current(r));
        }

        [Test]public void EmptyTableEntriesRemainAnOptionalEmptyBuild()
        {var b=new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=null});Capture(b,true);Assert.DoesNotThrow(()=>b.BuildZone(NewZone(),scope.Factory,new Random(7)));Assert.IsEmpty(Owners(Receipt(b)));}
        [Test]public void EnablingCaptureDuringAnUncapturedFactoryCallCannotPublishAnIncompleteReceipt()
        {
            var b=new PopulationBuilder(Group());Capture(b,false);scope.Factory.RegisterPartType<ReceiptCreatedPart>("ReceiptCreated");scope.Factory.Blueprints["MarlbackScrabbler"].Parts["ReceiptCreated"]=new Dictionary<string,string>();ReceiptCreatedPart.Callback=e=>Capture(b,true);
            try{Assert.DoesNotThrow(()=>b.BuildZone(NewZone(),scope.Factory,new Random(7)));Assert.IsNull(Receipt(b));}finally{ReceiptCreatedPart.Callback=null;}
        }

        [TestCase(true)][TestCase(false)]
        public void FactoryBlueprintSubstitutionCannotClaimTheRolledSource(bool population)
        {
            scope.Factory.RegisterPartType<ReceiptCreatedPart>("ReceiptCreated");foreach(var bp in population?new[]{"MarlbackScrabbler"}:new[]{"Crate","Sack","StrongBox"})scope.Factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            int calls=0;ReceiptCreatedPart.Callback=e=>{calls++;e.BlueprintName=population?"Viper":e.BlueprintName=="Crate"?"Sack":"Crate";};
            try{IZoneBuilder b=population?(IZoneBuilder)new PopulationBuilder(Group()):Containers();Capture(b,true);b.BuildZone(NewZone(),scope.Factory,new Random(14));Assert.Greater(calls,0);Assert.False(Current(Receipt(b)));}finally{ReceiptCreatedPart.Callback=null;}
        }
    }
}
