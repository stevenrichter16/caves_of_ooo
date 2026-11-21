using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The proving post (Docs/MACHINE-PROOFING.md, hand-testing): a test bench a scenario stands in the world. Beside it you choose an enemy that does an element's damage and it is called a few cells away,
    /// hostile to you and in the turn order like any creature; you can send everything it called away again, and mend yourself (full health, harmful statuses cleared, helpful ones such as a coat
    /// kept) between trials. It adds nothing to the ordinary game: only a scenario places one.
    /// </summary>
    public sealed class ProvingGroundTests : FiftyWorldFixture
    {
        static readonly (string blueprint, string element)[] Foes =
        {
            ("MarlbackCindercaller", "heat"), ("CharredHusk", "heat"), ("IceWight", "cold"), ("RuneCultist", "cold"),
            ("MarlbackStormbinder", "electric"), ("VaultSentinel", "electric"), ("MarlbackSoursprayer", "acid"), ("CaveSlime", "acid"),
        };

        [SetUp] public void SetupPost() { ProofingRegistry.ResetForTests(); MessageLog.Clear(); Pack.MaxWeight = 400; }

        /// <summary>The post stands next to the actor (the fixture's actor is at 10,10).</summary>
        Entity Post() { var post = Place("ElementalProvingPost", 11, 10); Zone.GetCell(11, 10).IsVisible = true; Zone.GetCell(11, 10).Explored = true; return post; }
        InventoryAction[] Rows(Entity post) => WorldInteractionSystem.GatherActions(post, Actor).Where(a => a.Name == "ProvingGround").ToArray();
        InventoryAction CallRow(Entity post, string blueprint) { var row = Rows(post).FirstOrDefault(r => r.Command == "CallProvingFoe|" + blueprint); Assert.NotNull(row, "no row for " + blueprint + " among: " + string.Join(" / ", Rows(post).Select(r => r.Display + " [" + r.Command + "|" + r.DisabledCode + "]")) + "; all: " + string.Join(" / ", WorldInteractionSystem.GatherActions(post, Actor).Select(r => r.Name))); return row; }
        Entity Called(string blueprint) => Zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == blueprint);

        [Test] public void BesideThePostThereIsARowForEveryElementalFoeAndTheTwoHousekeepingRows()
        {
            var post = Post(); var rows = Rows(post);
            foreach (var (blueprint, element) in Foes) { var row = CallRow(post, blueprint); Assert.False(row.IsDisabled, blueprint); StringAssert.Contains(element == "electric" ? "shock" : element, row.Display, blueprint); }
            Assert.NotNull(rows.SingleOrDefault(r => r.Command == "MendProvingSubject")); var away = rows.Single(r => r.DisabledCode == "none-called"); Assert.True(away.IsDisabled, "nothing has been called yet");
            Assert.AreEqual(Foes.Length + 2, rows.Length);
        }

        [Test] public void FromAFewCellsAwayThePostOffersNothing_Counter()
        {
            var post = Post(); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 20, 10)); Assert.IsEmpty(Rows(post));
        }

        [Test] public void CallingAFoePutsItInTheWorldHostileAndInTheTurnOrderAFewCellsOff()
        {
            var post = Post(); foreach (var (blueprint, _) in Foes)
            {
                Assert.True(Act(post, CallRow(post, blueprint).Command), blueprint + ": " + string.Join("|", MessageLog.GetRecent(2)));
                var foe = Called(blueprint); Assert.NotNull(foe, blueprint); var at = Zone.GetEntityPosition(foe); var me = Zone.GetEntityPosition(Actor);
                Assert.GreaterOrEqual(Math.Max(Math.Abs(at.x - me.x), Math.Abs(at.y - me.y)), 3, blueprint + " is called at a distance, not on top of you");
                var brain = foe.GetPart<BrainPart>(); Assert.NotNull(brain, blueprint); Assert.AreSame(Zone, brain.CurrentZone); Assert.True(brain.IsPersonallyHostileTo(Actor), blueprint + " is hostile to you");
                Assert.AreEqual("1", foe.GetProperty("ProvingGroundCall"), "marked, so only what the post called is sent away");
                Assert.True(Act(post, Rows(post).Single(r => r.Command == "DismissProvingFoes").Command), "one at a time: the next call starts clean");
            }
        }

        [Test] public void EveryFoeCarriesTheElementItsRowNames_AndOnlyThatOne()
        {
            var post = Post(); foreach (var (blueprint, element) in Foes)
            {
                Assert.True(Act(post, CallRow(post, blueprint).Command), blueprint); var foe = Called(blueprint); var c = Zone.GetEntityCell(foe); c.IsVisible = true; c.Explored = true;
                var seen = ElementalThreats.Seen(Zone, Actor); Assert.True(seen.ContainsKey(element), blueprint + " should show as " + element + ": " + string.Join(",", seen.Keys));
                Assert.AreEqual(1, seen.Count, blueprint + " is one element and no other: " + string.Join(",", seen.Keys));
                Assert.True(Act(post, Rows(post).Single(r => r.Command == "DismissProvingFoes").Command));
            }
        }

        [Test] public void SendingThemAwayRemovesWhatThePostCalledAndNothingElse()
        {
            var post = Post(); var bystander = Place("Villager", 30, 12); Assert.True(Act(post, CallRow(post, "IceWight").Command)); Assert.True(Act(post, CallRow(post, "CaveSlime").Command));
            var away = Rows(post).Single(r => r.Command == "DismissProvingFoes"); Assert.False(away.IsDisabled); StringAssert.Contains("2", away.Display);
            Assert.True(Act(post, away.Command)); Assert.Null(Called("IceWight")); Assert.Null(Called("CaveSlime")); Assert.NotNull(Zone.GetEntityCell(bystander), "the villager was not called, so it stays");
            Assert.AreEqual("none-called", Rows(post).Single(r => r.IsDisabled && r.DisabledCode == "none-called").DisabledCode);
        }

        [Test] public void NoMoreThanSixAreCalledAtOnce_AndTheRowSaysWhy_Counter()
        {
            var post = Post(); for (int i = 0; i < 6; i++) Assert.True(Act(post, CallRow(post, "CaveSlime").Command), "call " + i);
            var rows = Rows(post); Assert.AreEqual(8, rows.Count(r => r.IsDisabled && r.DisabledCode == "too-many"), "every call row says why"); Assert.False(Act(post, "CallProvingFoe|IceWight")); Assert.Null(Called("IceWight"));
        }

        [Test] public void MendingRestoresHealthAndClearsHarmfulStatusesButKeepsAHelpfulOne()
        {
            var post = Post(); var hp = Actor.GetStat("Hitpoints"); hp.BaseValue = 5; Actor.ApplyEffect(new BurningEffect(1f)); Actor.ApplyEffect(new AcidicEffect(0.4f)); Actor.ApplyEffect(new PoisonedEffect()); Actor.ApplyEffect(new ElectrifiedEffect(1f));   // (a frozen actor cannot act at all, so it is never the one pressing the row)
            Actor.ApplyEffect(new SteepedAcidEffect(35, 80, "garment", "cold")); Assert.True(Actor.HasEffect<BurningEffect>());
            Assert.True(Act(post, Rows(post).Single(r => r.Command == "MendProvingSubject").Command));
            Assert.AreEqual(hp.Max, Actor.GetStatValue("Hitpoints")); Assert.False(Actor.HasEffect<ElectrifiedEffect>()); Assert.False(Actor.HasEffect<PoisonedEffect>()); Assert.False(Actor.HasEffect<BurningEffect>()); Assert.False(Actor.HasEffect<AcidicEffect>()); Assert.True(Actor.HasEffect<SteepedAcidEffect>(), "a proofing you paid for is not a harm");
        }

        [Test] public void ThePostIsASolidNonTakeableOwnerWithNoPlaceInTheOrdinaryGame_Counter()
        {
            var post = Factory.CreateEntity("ElementalProvingPost"); Assert.True(post.GetPart<PhysicsPart>().Solid); Assert.False(post.GetPart<PhysicsPart>().Takeable); Assert.NotNull(post.GetPart<ProvingGroundPart>());
        }
    }
}
