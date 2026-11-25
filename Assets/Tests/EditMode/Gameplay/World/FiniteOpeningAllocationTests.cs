using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Deliberately hostile declared provider: allocation guards must retain the
    // quoted finite budget even when an extension violates its creation promise.
    public sealed class OpeningAllocationMutationProbe : Part
    {
        public static Action Run;
        public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Run?.Invoke();return true;}
    }
    public sealed class FiniteOpeningAllocationTests : FiftyWorldFixture
    {
        sealed class HostileDeclaredPolicy : IMaterialProcessCreationPartPolicy
        {
            public bool AllowsCreation=>true;
            public bool ValidTemplate(IReadOnlyDictionary<string,string> p)=>p.Count==0;
            public bool MatchesTemplate(Part p,IReadOnlyDictionary<string,string> values)=>p is OpeningAllocationMutationProbe && values.Count==0;
            public object Capture(Part p)=>true;
            public bool Matches(Part p,object state)=>p is OpeningAllocationMutationProbe;
        }
        EntityFactory previous;readonly HostileDeclaredPolicy policy=new HostileDeclaredPolicy();
        [SetUp] public void ReadyOpening(){previous=TraderPart.Factory;TraderPart.Factory=Factory;Factory.RegisterPartType<OpeningAllocationMutationProbe>();MaterialProcessPayloadPolicies.Register(typeof(OpeningAllocationMutationProbe),policy);}
        [TearDown] public void RestoreOpening(){OpeningAllocationMutationProbe.Run=null;TraderPart.Factory=previous;MaterialProcessPayloadPolicies.Unregister(typeof(OpeningAllocationMutationProbe),policy);}
        [TestCase(false)][TestCase(true)] public void CreationCallbackCannotEnlargeOrRedirectTheFiniteOpeningAllocation(bool redirect)
        {
            const string input="CuredLeatherOffcut";Factory.Blueprints[input].Parts[nameof(OpeningAllocationMutationProbe)]=new Dictionary<string,string>();
            var original=Carry("DriedMeat");var trader=new TraderPart{OpeningStockBlueprint=input,OpeningStockUnits=2};Actor.AddPart(trader);bool first=true;
            OpeningAllocationMutationProbe.Run=()=>{if(!first)return;first=false;if(redirect)trader.OpeningStockBlueprint="FireClay";else trader.OpeningStockUnits=5;};
            Actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.False(first,"must actually reach the adversarial factory callback");
            Assert.AreEqual(0,Count(input));Assert.AreEqual(0,Count("FireClay"));Assert.True(Pack.Objects.Contains(original));Assert.True(trader.OpeningStockSupplied);
            Assert.AreEqual(redirect?"FireClay":input,trader.OpeningStockBlueprint);Assert.AreEqual(redirect?2:5,trader.OpeningStockUnits);
        }
    }
}
