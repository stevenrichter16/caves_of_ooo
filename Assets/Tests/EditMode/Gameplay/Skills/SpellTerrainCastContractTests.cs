using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SpellTerrainCastContractTests
    {
        [SetUp] public void Setup()
        {
            SpellCastFixture.Reset(); TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearDown() { TileReactionSystem.ResetForTests(); ResonanceSystem.ResetForTests(); SpellCastFixture.RestoreRuntime(); }

        [TestCase(typeof(Pyromancy_FlameJet), false)] [TestCase(typeof(Pyromancy_FlameJet), true)]
        [TestCase(typeof(Pyromancy_Backdraft), false)] [TestCase(typeof(Pyromancy_Backdraft), true)]
        public void ARealEmptyTerrainCastIsPaidAndOilActuallyIgnites(Type power, bool oil)
        {
            var f = new SpellCastFixture(); var spell = f.Learn(power); var ley = f.Learn<Spellcraft_LeyTap>();
            Assert.True(f.Cast(ley));
            if (oil) f.Zone.TileState.WriteCoating(6, 5, "oil", 8);
            int before = f.Turns.TickCount;
            Assert.True(f.Cast(spell), "A fired terrain cast must report success to the paid input path.");
            Assert.Greater(f.Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(spell.ActivatedAbilityID).CooldownRemaining, 0);
            Assert.AreEqual(60, ley.PendingBonus, "Ground-only damage does not spend a direct damage buff.");
            if (oil) { Assert.False(f.Zone.TileState.HasCoating(6, 5, "oil")); Assert.True(f.Zone.TileState.HasResidue(6, 5, "embers")); }
            f.Advance(); Assert.AreEqual(before + 10, f.Turns.TickCount);
        }

        [TestCase(typeof(Pyromancy_FlameJet))] [TestCase(typeof(Pyromancy_Backdraft))]
        public void AZeroCellPathRefusesWithoutCooldownOrStateChange(Type power)
        {
            var f = new SpellCastFixture(); var spell = f.Learn(power);
            Assert.True(f.Zone.RemoveEntity(f.Actor)); Assert.True(f.Zone.AddEntity(f.Actor, 0, 0));
            int ticks = f.Turns.TickCount, hp = f.Actor.GetStatValue("Hitpoints");
            Assert.False(f.Cast(spell, -1)); Assert.AreEqual(0, f.Zone.TileState.WrittenCount);
            Assert.AreEqual(0, f.Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(spell.ActivatedAbilityID).CooldownRemaining);
            Assert.AreEqual(ticks, f.Turns.TickCount); Assert.AreEqual(hp, f.Actor.GetStatValue("Hitpoints"));
        }
    }
}
