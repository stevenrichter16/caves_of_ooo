using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FloatingEventTextShowcaseTests
    {
        [TestCase("player-left")] [TestCase("patient-left")] [TestCase("dead-patient")]
        public void InvalidFixtureCannotMutateActorsAfterTravelOrDeath(string change)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var defender=f.Actor();var attacker=f.Actor(x:11);var patient=f.Target(x:20);
                if(change=="player-left") { f.Zone.RemoveEntity(defender);new Zone("Other").AddEntity(defender,10,10); }
                if(change=="patient-left") { f.Zone.RemoveEntity(patient);new Zone("Other").AddEntity(patient,20,10); }
                if(change=="dead-patient") patient.GetStat("Hitpoints").BaseValue=0;
                Assert.False(FloatingEventTextShowcase.Resolve(f.Zone,defender,attacker,patient));
                Assert.False(defender.HasEffect<EnGardeEffect>());Assert.False(patient.HasEffect<PoisonedEffect>());
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ShowcaseResolvesRealDefenseAndStatusesWithoutAdvancingTurns(bool hiddenPatient)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var defender=f.Actor(); var attacker=f.Actor(x:11); var patient=f.Target(x:20);
                foreach(var cell in f.Zone.Cells){cell.Explored=true;cell.IsVisible=true;}
                f.Zone.GetEntityCell(patient).IsVisible=!hiddenPatient;
                AsciiFxBus.Clear(); int hp=defender.GetStatValue("Hitpoints");
                Assert.True(FloatingEventTextShowcase.Resolve(f.Zone,defender,attacker,patient));
                Assert.AreEqual(hp,defender.GetStatValue("Hitpoints"));
                Assert.True(patient.HasEffect<StunnedEffect>());Assert.True(patient.HasEffect<PoisonedEffect>());
                Assert.True(patient.HasEffect<BurningEffect>());
                var requests=AsciiFxBus.Drain();var words=requests.Where(r=>r.Type==AsciiFxRequestType.EventText).Select(r=>r.Text).ToArray();
                CollectionAssert.Contains(words,"Parried");Assert.AreEqual(!hiddenPatient,words.Contains("Poisoned"));
                Assert.False(AsciiFxBus.HasPendingBlocking);
                foreach(var request in requests)AsciiFxBus.Release(request);
            }
        }
    }
}
