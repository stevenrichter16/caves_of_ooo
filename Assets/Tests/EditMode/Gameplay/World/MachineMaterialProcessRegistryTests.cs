using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // RED contract for the bounded M3a draft; no gameplay acceptance.
    public sealed class MachineMaterialProcessRegistryTests
    {
        Type Registry => typeof(Part).Assembly.GetType("CavesOfOoo.Core.MaterialProcessRegistry");
        const string Row = "{\"Id\":\"recover-iron\",\"SourceBlueprint\":\"ReleasedExtractionTool\",\"Mode\":\"Reclaim\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"RecoveredIronBlank\",\"SalvagedTimber\"],\"OutputCounts\":[1,1]}";
        int Install(string json)
        {
            Assert.NotNull(Registry,"M3a needs a validated finite process catalogue before production actions.");
            var errors=Registry.GetMethod("InitializeFromJson",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{json}) as IEnumerable;
            Assert.NotNull(errors);int count=0;foreach(var error in errors)count++;return count;
        }
        object Get(string id)=>Registry.GetMethod("Get",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{id});
        string File(string row)=>"{\"Processes\":["+row+"]}";
        [Test] public void BoundedProcessCatalogueAcceptsExplicitFiniteRecovery()
        {Assert.AreEqual(0,Install(File(Row)));Assert.NotNull(Get("recover-iron"));Assert.Null(Get("unknown"));}
        [TestCase(null)] [TestCase("")] [TestCase("{")] [TestCase("{}")] [TestCase("{\"Processes\":[]}")]
        public void MalformedCatalogueClearsEarlierDefinitions(string json)
        {Assert.AreEqual(0,Install(File(Row)));Assert.Greater(Install(json),0);Assert.Null(Get("recover-iron"));}
        [TestCase("\"InputCount\":1","\"InputCount\":0")]
        [TestCase("\"InputCount\":1","\"InputCount\":65")]
        [TestCase("\"Reclaim\"","\"Invent\"")]
        [TestCase("\"ForgeCold\"","\"UniversalMachine\"")]
        [TestCase("\"recover-iron\"","\"../recover\"")]
        [TestCase("\"RecoveredIronBlank\",\"SalvagedTimber\"","\"RecoveredIronBlank\",\"RecoveredIronBlank\"")]
        [TestCase("\"OutputCounts\":[1,1]","\"OutputCounts\":[0,1]")]
        [TestCase("\"OutputCounts\":[1,1]","\"OutputCounts\":[1]")]
        [TestCase("\"OutputBlueprints\":[\"RecoveredIronBlank\",\"SalvagedTimber\"]","\"OutputBlueprints\":[\"ReleasedExtractionTool\",\"SalvagedTimber\"]")]
        public void InvalidProcessFailsAtomically(string from,string to)
        {Assert.Greater(Install(File(Row+","+Row.Replace(from,to).Replace("recover-iron","other-process"))),0);Assert.Null(Get("recover-iron"));}
        [TearDown] public void ClearDraftCatalogue()
        { if(Registry!=null)Registry.GetMethod("ResetForTests").Invoke(null,null); }
        [Test] public void ScopedPilotRejectsMoreThanTwoOutputKinds()
        {
            string row=Row.Replace("\"RecoveredIronBlank\",\"SalvagedTimber\"","\"RecoveredIronBlank\",\"SalvagedTimber\",\"FieldHaftComponent\"").Replace("[1,1]","[1,1,1]");
            Assert.Greater(Install(File(row)),0);Assert.Null(Get("recover-iron"));
        }
        [Test] public void PublishedOutputsCannotBeMutatedByPreviewCallers()
        {
            Install(File(Row));var definition=Get("recover-iron");
            var outputs=(IList)definition.GetType().GetProperty("OutputBlueprints").GetValue(definition);
            Assert.Throws<NotSupportedException>(()=>outputs[0]="SteelBladeComponent");
            Assert.AreEqual("RecoveredIronBlank",outputs[0]);
        }
        [Test] public void NullRowInvalidatesTheWholeCatalogue()
        {Assert.Greater(Install(File(Row+",null")),0);Assert.Null(Get("recover-iron"));}
        [TestCase("[65,1]")] [TestCase("[1,65]")]
        public void ExcessiveOutputQuantityFailsClosed(string counts)
        {Assert.Greater(Install(File(Row.Replace("[1,1]",counts))),0);Assert.Null(Get("recover-iron"));}
        [Test] public void DuplicateIdFailsAtomically()
        {Assert.Greater(Install(File(Row+","+Row)),0);Assert.Null(Get("recover-iron"));}
    }
    public sealed class MachineMaterialProcessBlueprintTests : FiftyWorldFixture
    {
        const string Data="{\"Processes\":[{\"Id\":\"test-process\",\"SourceBlueprint\":\"Dagger\",\"Mode\":\"Process\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"IronSpikeComponent\"],\"OutputCounts\":[1]}]}";
        Type Registry=>typeof(Part).Assembly.GetType("CavesOfOoo.Core.MaterialProcessRegistry");
        void Install(string data)=>Registry.GetMethod("InitializeFromJson").Invoke(null,new object[]{data});
        int Validate(EntityFactory factory)
        {
            var method=Registry.GetMethod("Validate",BindingFlags.Static|BindingFlags.Public);
            Assert.NotNull(method,"Syntax-only rows cannot authorize missing or non-portable content.");
            return ((IEnumerable)method.Invoke(null,new object[]{factory})).Cast<object>().Count();
        }
        [TearDown] public void ClearDraftCatalogue()=>MaterialProcessRegistry.ResetForTests();
        [Test] public void ExistingPortableBlueprintsPassContentValidation()
        { Install(Data);Assert.AreEqual(0,Validate(Factory)); }
        [TestCase("Dagger","MissingSource")]
        [TestCase("IronSpikeComponent","MissingProduct")]
        [TestCase("IronSpikeComponent","TinkersForge")]
        [TestCase("Dagger","Player")]
        public void MissingOrNonPortableContentCannotAuthorizeAProcess(string from,string to)
        { Install(Data.Replace(from,to));Assert.Greater(Validate(Factory),0); }
        [TestCase("blueprints")] [TestCase("tags")] [TestCase("parts")] [TestCase("physics")]
        [TestCase("solid-tag-false")] [TestCase("solid-tag-omitted")]
        public void MalformedOrActuallySolidBlueprintsFailClosed(string defect)
        {
            Install(Data);var output=Factory.Blueprints["IronSpikeComponent"];
            if(defect=="blueprints")Factory.Blueprints=null;
            if(defect=="tags")output.Tags=null;
            if(defect=="parts")output.Parts=null;
            if(defect=="physics")output.Parts["Physics"]=null;
            if(defect.StartsWith("solid-tag"))
            {output.Tags["Solid"]="";if(defect.EndsWith("false"))output.Parts["Physics"]["Solid"]="false";else output.Parts["Physics"].Remove("Solid");}
            Assert.Greater(Validate(Factory),0);
        }
        [Test] public void MissingFactoryAndMalformedCatalogueFailClosed()
        {Install(Data);Assert.Greater(Validate(null),0);Install(null);Assert.Greater(Validate(Factory),0);}
    }
}
