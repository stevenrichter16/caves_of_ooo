using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityEverydayGrammarTests
    {
        Action<string> oldObserver;
        [SetUp]public void Setup(){oldObserver=MessageLog.OnMessage;MessageLog.OnMessage=null;MessageLog.Clear();}
        [TearDown]public void Cleanup(){MessageLog.OnMessage=oldObserver;MessageLog.Clear();}
        [TestCase("you is confused!","You are confused!")]
        [TestCase("you has recovered.","You have recovered.")]
        [TestCase("you was stunned.","You were stunned.")]
        [TestCase("you does not resist.","You do not resist.")]
        [TestCase("you picks up tepuibone.","You pick up tepuibone.")]
        [TestCase("you heals 3 HP.","You heal 3 HP.")]
        [TestCase("You throws a dagger, but misses!","You throw a dagger, but misses!")]
        [TestCase("you's overload finds no conductors.","Your overload finds no conductors.")]
        [TestCase("you’s skin sears the marlback.","Your skin sears the marlback.")]
        [TestCase("you dies.","You die.")]
        public void PlayerLeadingClause_UsesSecondPerson(string before,string after)
        {string published=null;MessageLog.OnMessage=m=>published=m;MessageLog.Add(before);Assert.AreEqual(after,MessageLog.GetLast());Assert.AreEqual(after,published);}
        [TestCase("A marlback is confused!")][TestCase("You are already rested.")]
        [TestCase("The keeper says, 'you is a stranger'.")][TestCase("\"you is unwelcome\"")]
        [TestCase("young moss is growing.")][TestCase("younger roots have spread.")]
        [TestCase("you is quoted lore.\nA second line.")][TestCase("Your sword falls to the ground.")]
        [TestCase("&Yyou is a colored quotation&y")]
        public void UnrelatedOrQuotedOrCorrectText_RemainsByteForByte(string text)
        {MessageLog.Add(text);Assert.AreSame(text,MessageLog.GetLast());}
        [Test]public void AnnouncementProse_RemainsRawInLogQueueAndCallback()
        {const string text="you is an intentionally recorded voice.";string published=null;MessageLog.OnMessage=m=>published=m;MessageLog.AddAnnouncement(text);Assert.AreSame(text,MessageLog.GetLast());Assert.AreSame(text,MessageLog.ConsumeAnnouncement());Assert.AreSame(text,published);}
        [Test]public void RealParchedEffect_PlayerAndNpcAreDistinct()
        {
            var player=new Entity();player.AddPart(new RenderPart{DisplayName="you"});player.AddPart(new StatusEffectsPart());
            player.ForceApplyEffect(new ParchedEffect());Assert.AreEqual("You are parched by the glare.",MessageLog.GetLast());
            var npc=new Entity();npc.AddPart(new RenderPart{DisplayName="Sella"});npc.AddPart(new StatusEffectsPart());
            npc.ForceApplyEffect(new ParchedEffect());Assert.AreEqual("Sella is parched by the glare.",MessageLog.GetLast());
        }
    }
}
