using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FoundingTrustTests
    {
        private EntityFactory _factory;
        private Entity _player, _tender;
        private Zone _zone, _oldZone;
        private NarrativeStatePart _oldState;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Set("CatacombFolk", 0); ConversationLoader.Reset();
            _oldZone = SettlementRuntime.ActiveZone; _oldState = NarrativeStatePart.Current;
            NarrativeStatePart.Current = new NarrativeStatePart();
            _zone = SettlementRuntime.ActiveZone = new Zone("FoundingService");
            _player = new Entity(); _player.SetTag("Player"); _player.AddPart(new InventoryPart { MaxWeight = 100 });
            _tender = _factory.CreateEntity("FoundingPlaqueTender");
            _zone.AddEntity(_player, 10, 10); _zone.AddEntity(_tender, 11, 10);
        }
        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset();
            SettlementRuntime.ActiveZone = _oldZone; NarrativeStatePart.Current = _oldState; FactionManager.Reset();
        }
        private Entity Stone(int count = 1)
        {
            var stone = _factory.CreateEntity("Tepuibone"); stone.GetPart<StackerPart>().StackCount = count;
            _player.GetPart<InventoryPart>().AddObject(stone); return stone;
        }
        private void Offer() => ConversationActions.Execute("OfferFoundingStone", _tender, _player, "");
        [TestCase(1)] [TestCase(2)] public void OneRealStoneEarnsTrust_OnceOnly(int count)
        {
            Stone(count); Offer();
            Assert.AreEqual(50, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("FoundingStoneOffered"));
            int units = _player.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            Assert.AreEqual(count - 1, units);
            Offer(); Assert.AreEqual(50, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(count - 1, _player.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
        }
        [TestCase("missing")] [TestCase("remote")] [TestCase("war")] [TestCase("imposter")]
        public void RefusedServiceSpendsNothingAndDoesNotLatch(string refusal)
        {
            if (refusal != "missing") Stone();
            if (refusal == "remote") { _zone.RemoveEntity(_tender); _zone.AddEntity(_tender, 50, 10); }
            if (refusal == "war") PlayerReputation.Set("CatacombFolk", -150);
            if (refusal == "imposter") _tender = _factory.CreateEntity("PlaqueTender");
            int rep = PlayerReputation.Get("CatacombFolk"); Offer();
            Assert.AreEqual(rep, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact("FoundingStoneOffered"));
            Assert.AreEqual(refusal == "missing" ? 0 : 1, _player.GetPart<InventoryPart>().Objects.Count);
        }
        [Test] public void ActualConversationOffersServiceAndExplainsTheMeeting()
        {
            Stone(); Assert.IsTrue(ConversationManager.StartConversation(_tender, _player));
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferFoundingStone"));
            Assert.GreaterOrEqual(choice, 0); ConversationManager.SelectChoice(choice);
            Assert.AreEqual(50, PlayerReputation.Get("CatacombFolk")); ConversationManager.EndConversation();
            var listener = _factory.CreateEntity("FoundingListener");
            Assert.IsTrue(ConversationManager.StartConversation(listener, _player));
            Assert.IsTrue(ConversationManager.VisibleChoices.Any(c => c.Target == "Rest"));
            PlayerReputation.Set("CatacombFolk", 49); ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(listener, _player));
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Target == "Rest"));
        }
        [Test] public void CachedConversationChoiceCannotClaimAnAbsentStone()
        {
            var stone = Stone(); Assert.IsTrue(ConversationManager.StartConversation(_tender, _player));
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferFoundingStone"));
            Assert.GreaterOrEqual(choice, 0); _player.GetPart<InventoryPart>().RemoveObject(stone);
            ConversationManager.SelectChoice(choice);
            Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk")); Assert.AreEqual(0, NarrativeStatePart.Current.GetFact("FoundingStoneOffered"));
        }
        [TestCase(-1, false)] [TestCase(0, true)]
        public void OfferRowMatchesRawStandingBoundary(int rep, bool offered)
        {
            Stone(); PlayerReputation.Set("CatacombFolk", rep);
            Assert.IsTrue(ConversationManager.StartConversation(_tender, _player));
            Assert.AreEqual(offered, ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferFoundingStone")));
        }
        [TestCase(false)] [TestCase(true)]
        public void PersonalEnmityCannotBeLaunderedThroughGuestSafety(bool cloth)
        {
            Stone(); _tender.GetPart<BrainPart>().SetPersonallyHostile(_player);
            if (cloth)
            {
                _player.AddPart(new StatusEffectsPart());
                _player.GetPart<StatusEffectsPart>().ForceApplyEffect(new UnderTheClothEffect());
            }
            Offer(); Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact("FoundingStoneOffered"));
        }
    }
}
