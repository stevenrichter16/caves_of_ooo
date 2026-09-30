using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadSpecialistCombatTests
 {
  [TestCase("MarlbackCindercaller","CommandEmberSpit","FireMoss")]
  [TestCase("MarlbackSoursprayer","CommandAcidSpray","GlimmerBrine")]
  public void OriginalSpecialistUsesRealPowerWithCoverAndCooldownCounters(string blueprint,string command,string reagent)
  {
   using(var f=new DensityCombatFixture())
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(blueprint));var actor=f.Factory.CreateEntity(blueprint);f.Zone.AddEntity(actor,10,10);var target=f.Target();
    var tactics=actor.GetPart<CombatTacticsPart>();Assert.NotNull(tactics);Assert.True(tactics.AssistAllies);Assert.That(tactics.AbilityChance,Is.InRange(25,45));
    Assert.LessOrEqual(actor.GetPart<BrainPart>().SightRadius,6);Assert.That(actor.GetStatValue("Hitpoints"),Is.InRange(10,15));
    var ability=DensityCombatFixture.Ability(actor);Assert.AreEqual(command,ability.Command);
    Assert.AreEqual(1,actor.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName==reagent));
    var wall=f.Wall(11,10);Assert.False(f.Cast(actor,target));Assert.AreEqual(0,ability.CooldownRemaining);
    f.Zone.RemoveEntity(wall);Assert.True(f.Cast(actor,target));Assert.Less(target.GetStatValue("Hitpoints"),500);Assert.Greater(ability.CooldownRemaining,0);Assert.False(f.Cast(actor,target));
    var ordinary=f.Factory.CreateEntity("MarlbackScrabbler");Assert.Null(ordinary.GetPart<CombatTacticsPart>());
   }
  }
 }
}
