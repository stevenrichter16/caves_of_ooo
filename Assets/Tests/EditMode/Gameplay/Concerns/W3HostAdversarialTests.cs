using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3 adversarial sweep for the two mechanics the wave added: the Host approach kind
    /// (lint, malformed guest tags, placement gating by state and place, save reach) and
    /// a Read whose place is a surface zone.
    /// </summary>
    public sealed class W3HostAdversarialTests
    {
        static EntityFactory factory;
        Entity player;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            PlaceSiteCatalog.Reset(); PlaceConditionCatalog.Reset(); ReadableDocumentCatalog.ResetForTests();
            StoryletRegistry.Reset(); ConcernRegistry.Reset();
            Diag.ResetAll(); ConversationActions.Factory = factory; PlayerReputation.Reset();
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            StoryletPart.Current = new StoryletPart(); NarrativeStatePart.Current = new NarrativeStatePart();
            new SettlementManager(() => 10, id => PlaceSiteCatalog.PlaceAt(id) is string name ? new PointOfInterest(POIType.Village, name) : null);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
        }
        [TearDown] public void Cleanup()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent(); ConversationActions.Factory = null;
        }

        static string Json(string hostApproach, string readPlace = "the First Tent")
            => @"{""Concerns"":[{""Id"":""t.host"",""Place"":""the First Tent"",""Owner"":""TentRightHost"",""Tier"":3,""Title"":""T"",""Pitch"":""p"",""Thanks"":""t"",
  ""Approaches"":[" + hostApproach + @",
   {""Id"":""read"",""Kind"":""Read"",""Label"":""r"",""Objective"":""r"",""Read"":{""Document"":""firsttent-cord"",""Place"":""" + readPlace + @"""}}],
  ""Refusal"":{""Text"":""n"",""Reply"":""m""}}]}";
        const string GoodHost = @"{""Id"":""host"",""Kind"":""Host"",""Label"":""h"",""Objective"":""h"",""Host"":{""Blueprint"":""Villager"",""Name"":""the guest""}}";

        static System.Collections.Generic.List<string> LintOf(string json)
        {
            ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(json, "test");
            return ConcernRegistry.Lint(ConcernRegistry.Get("t.host"), factory);
        }

        // ════════════════ lint ════════════════

        [Test] public void Adversarial_AWellFormedHostLintsClean()
            => CollectionAssert.IsEmpty(LintOf(Json(GoodHost)));

        [Test] public void Adversarial_AHostApproachMustNameAGuest()
            => Assert.True(LintOf(Json(@"{""Id"":""host"",""Kind"":""Host"",""Label"":""h"",""Objective"":""h""}")).Any(p => p.Contains("names no guest")));

        [Test] public void Adversarial_ABeastCannotBeAConcernsGuest()
            => Assert.True(LintOf(Json(@"{""Id"":""host"",""Kind"":""Host"",""Label"":""h"",""Objective"":""h"",""Host"":{""Blueprint"":""BrittleHound"",""Name"":""a dog""}}"))
                .Any(p => p.Contains("not a person")));

        [Test] public void Adversarial_AnUnknownOrNamelessGuestIsCaught()
        {
            Assert.True(LintOf(Json(@"{""Id"":""host"",""Kind"":""Host"",""Label"":""h"",""Objective"":""h"",""Host"":{""Blueprint"":""NoSuchPerson"",""Name"":""x""}}")).Any(p => p.Contains("unknown guest")));
            Assert.True(LintOf(Json(@"{""Id"":""host"",""Kind"":""Host"",""Label"":""h"",""Objective"":""h"",""Host"":{""Blueprint"":""Villager"",""Name"":"" ""}}")).Any(p => p.Contains("needs a name")));
        }

        [Test] public void Adversarial_ADocumentMayLieAtASurfaceZoneButNotOffTheMap()
        {
            CollectionAssert.IsEmpty(LintOf(Json(GoodHost, "Overworld.19.18.0")));
            Assert.True(LintOf(Json(GoodHost, "Overworld.99.99.0")).Any(p => p.Contains("unknown read place")));
            Assert.True(LintOf(Json(GoodHost, "Overworld.19.18.1")).Any(p => p.Contains("unknown read place")), "underground is not a surface zone");
            Assert.True(LintOf(Json(GoodHost, "nowhere")).Any(p => p.Contains("unknown read place")));
        }

        // ════════════════ the guest tag ════════════════

        [Test] public void Adversarial_MalformedGuestTagsSettleNothing()
        {
            ConcernRegistry.LoadFromJson(Json(GoodHost), "test");
            foreach (var tag in new[] { null, "", "|", "t.host", "|host", "nope|host", "t.host|nope", "t.host|read" })
            {
                var e = factory.CreateEntity("Villager");
                if (tag != null) e.Properties[ConcernWorld.GuestProperty] = tag;
                Assert.False(ConcernWorld.OnGuestHosted(e), "tag '" + tag + "'");
            }
            Assert.False(ConcernWorld.OnGuestHosted(null));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.HostedFact("t.host", "host")));
        }

        [Test] public void Adversarial_AWellFormedTagSetsOnlyItsOwnApproach()
        {
            ConcernRegistry.LoadFromJson(Json(GoodHost), "test");
            var e = factory.CreateEntity("Villager"); e.Properties[ConcernWorld.GuestProperty] = "t.host|host";
            Assert.True(ConcernWorld.OnGuestHosted(e));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ConcernRegistry.HostedFact("t.host", "host")));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.HostedFact("t.host", "read")));
        }

        // ════════════════ placement gates ════════════════

        [Test] public void Adversarial_NoGuestBeforeTheWorkIsTakenOrAtAnotherPlace()
        {
            ConcernRegistry.LoadAll();
            var tent = new Zone(W3ContentTests.FirstTentZone); tent.AddEntity(player, 10, 10);
            var host = factory.CreateEntity("TentRightHost"); tent.AddEntity(host, 12, 10);
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            Assert.False(tent.GetAllEntities().Any(e => e.GetProperty(ConcernWorld.GuestProperty) != null), "not before the work is taken");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            var wellmeet = new Zone(W3ContentTests.WellmeetZone);
            tent.RemoveEntity(player); wellmeet.AddEntity(player, 10, 10);
            ConcernWorld.OnZoneEntered(player, wellmeet, arriving: true);
            Assert.False(wellmeet.GetAllEntities().Any(e => e.GetProperty(ConcernWorld.GuestProperty) != null), "only at the concern's own place");
            wellmeet.RemoveEntity(player); tent.AddEntity(player, 10, 10);
            ConcernWorld.OnZoneEntered(player, tent, arriving: false);
            Assert.False(tent.GetAllEntities().Any(e => e.GetProperty(ConcernWorld.GuestProperty) != null), "a reload is not an arrival");
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            Assert.True(tent.GetAllEntities().Any(e => e.GetProperty(ConcernWorld.GuestProperty) != null));
        }

        [Test] public void Adversarial_TheGuestsTagSurvivesASave()
        {
            var e = factory.CreateEntity("Villager"); e.ID = "concernguest:firsttent.clan:host";
            e.Properties[ConcernWorld.GuestProperty] = "firsttent.clan|host";
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual("firsttent.clan|host", loaded.GetProperty(ConcernWorld.GuestProperty));
        }

        [Test] public void Adversarial_GuestPlacementIsRecorded()
        {
            ConcernRegistry.LoadAll();
            var tent = new Zone(W3ContentTests.FirstTentZone); tent.AddEntity(player, 10, 10);
            var host = factory.CreateEntity("TentRightHost"); tent.AddEntity(host, 12, 10);
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            Assert.Greater(DiagQuery.Apply(new DiagQuery.Filter { Category = "concern", Kind = "GuestPlaced", Limit = 5 }).Records.Count, 0);
        }
    }
}
