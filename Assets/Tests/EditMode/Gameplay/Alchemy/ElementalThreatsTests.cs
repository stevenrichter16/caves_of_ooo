using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// What elemental harm you have actually seen (Docs/MACHINE-PROOFING.md): the trough, the press rows and the coat rows say which element the place in front of you has shown you, so a player who knows
    /// what they are about to meet is pointed at the right proofing. It reads only what the viewer has seen (a creature only while it is in view, terrain once it has been seen), never the generator's secrets.
    /// </summary>
    public sealed class ElementalThreatsTests : FiftyWorldFixture
    {
        Entity PlaceSeen(string blueprint, int x, int y, bool visible = true, bool explored = true)
        { var e = Place(blueprint, x, y); var cell = Zone.GetCell(x, y); cell.IsVisible = visible; cell.Explored = explored; return e; }
        System.Collections.Generic.List<string> Names(string element) => ElementalThreats.Seen(Zone, Actor).TryGetValue(element, out var l) ? l : new System.Collections.Generic.List<string>();

        [Test] public void SeenTerrainThatSeedsHeatColdOrChargeNamesItsElement()
        {
            PlaceSeen("SteamVent", 20, 10); PlaceSeen("FrostVent", 22, 10);
            CollectionAssert.Contains(Names("heat"), "steam vent"); CollectionAssert.Contains(Names("cold"), "frost vent"); CollectionAssert.IsEmpty(Names("acid"));
        }

        [Test] public void ASeenCreatureThatCarriesAnElementalSkillNamesItsElement()
        {
            PlaceSeen("MarlbackSoursprayer", 24, 10); PlaceSeen("MarlbackCindercaller", 26, 10); PlaceSeen("MarlbackStormbinder", 28, 10); PlaceSeen("IceWight", 30, 10);
            Assert.True(Names("acid").Any(n => n.ToLowerInvariant().Contains("soursprayer")), string.Join(",", Names("acid"))); Assert.True(Names("heat").Any(n => n.ToLowerInvariant().Contains("cindercaller")));
            Assert.True(Names("electric").Any(n => n.ToLowerInvariant().Contains("stormbinder"))); Assert.True(Names("cold").Any(n => n.ToLowerInvariant().Contains("wight")));
        }

        [Test] public void AHarmfulPoolCountsByTheDamageTheLiquidDoes()
        {
            Pool(5, "bog-mire", 18, 10); Zone.GetCell(18, 10).IsVisible = true; Zone.GetCell(18, 10).Explored = true; Pool(5, "water", 19, 10); Zone.GetCell(19, 10).IsVisible = true; Zone.GetCell(19, 10).Explored = true;
            Assert.AreEqual(1, Names("acid").Count, "the mire hurts like acid"); CollectionAssert.IsEmpty(Names("heat")); CollectionAssert.IsEmpty(Names("cold"));
        }

        [Test] public void WhatYouHaveNotSeenIsNotReported_Counter()
        {
            PlaceSeen("SteamVent", 20, 10, visible: false, explored: false); PlaceSeen("MarlbackSoursprayer", 24, 10, visible: false, explored: false);
            CollectionAssert.IsEmpty(Names("heat")); CollectionAssert.IsEmpty(Names("acid")); Assert.AreEqual("", ElementalThreats.Summary(Zone, Actor));
        }

        [Test] public void ACreatureInARememberedCellIsNotReportedButRememberedTerrainIs_Counter()
        {
            PlaceSeen("MarlbackSoursprayer", 24, 10, visible: false, explored: true); PlaceSeen("SteamVent", 20, 10, visible: false, explored: true);
            CollectionAssert.IsEmpty(Names("acid"), "creatures move: only while in view"); CollectionAssert.Contains(Names("heat"), "steam vent");
        }

        [Test] public void APoolIsReportedOnlyWhileItsCellIsInView_ButAVentOnceSeen_Counter()
        {
            Pool(5, "bog-mire", 18, 10); Zone.GetCell(18, 10).IsVisible = false; Zone.GetCell(18, 10).Explored = true;
            CollectionAssert.IsEmpty(Names("acid"), "a pool you only remember may have drained or filled since");
            Zone.GetCell(18, 10).IsVisible = true; Assert.AreEqual(1, Names("acid").Count);
        }

        [Test] public void YouAndWhatYouCarryAreNeverTheThreat_Counter()
        {
            var cell = Zone.GetCell(Zone.GetEntityPosition(Actor).x, Zone.GetEntityPosition(Actor).y); cell.IsVisible = true; cell.Explored = true;
            Carry("Torch"); var skills = Actor.GetPart<CavesOfOoo.Skills.SkillsPart>() ?? new CavesOfOoo.Skills.SkillsPart(); if (Actor.GetPart<CavesOfOoo.Skills.SkillsPart>() == null) Actor.AddPart(skills);
            Assert.True(skills.AddSkill("Pyromancy_EmberSpit"), "fixture: the viewer is a pyromancer, which must not make them their own threat"); Assert.AreEqual(0, ElementalThreats.Seen(Zone, Actor).Count);
        }

        [Test] public void TheSummaryNamesEachSeenElementOnceWithItsSources()
        {
            PlaceSeen("SteamVent", 20, 10); PlaceSeen("SteamVent", 21, 11); PlaceSeen("FrostVent", 22, 10);
            string text = ElementalThreats.Summary(Zone, Actor); StringAssert.Contains("heat (steam vent)", text, "two vents are one name"); StringAssert.Contains("cold (frost vent)", text); StringAssert.StartsWith("You have seen", text);
            Assert.AreEqual("steam vent", ElementalThreats.ForElement(Zone, Actor, "heat")); Assert.AreEqual("", ElementalThreats.ForElement(Zone, Actor, "acid")); Assert.AreEqual("", ElementalThreats.ForElement(null, Actor, "heat"));
        }
    }
}
