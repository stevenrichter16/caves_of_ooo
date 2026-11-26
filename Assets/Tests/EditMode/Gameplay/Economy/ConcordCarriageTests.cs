using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Concord's carriage (Docs/MACHINE-INVENTED-USES.md, section 3k): entrust one carried stack at a post, collect it days later at the other. The goods are the real entity held on the
    /// player; the fee is a ledger sink; arrival is the clock compared at the destination; clause five of Lore/Codex/06_ConcordCarriageContract.md is enforced; a refusal at any step
    /// changes nothing.
    /// </summary>
    public sealed class ConcordCarriageTests
    {
        static EntityFactory factory;
        static readonly string Cinderhold = WorldMap.ToZoneID(6, 6, 0), LastCounter = WorldMap.ToZoneID(18, 18, 0), Sill = WorldMap.ToZoneID(10, 10, 0);
        TurnManager clock; TurnManager oldClock;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            ConversationLoader.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset();
            ConversationManager.EndConversation(); ConversationManager.PendingTradePartner = null;
            oldClock = TurnManager.Active; clock = new TurnManager();
        }

        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset();
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            typeof(TurnManager).GetProperty("Active", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, oldClock);
        }

        // ── fixtures ──

        static Entity Player(int drams = 100) { var p = factory.CreateEntity("Player"); TradeSystem.SetDrams(p, drams); return p; }

        /// <summary>A keeper standing in a zone with the given id: the Factor (ledger and credit) or the Envoy (ledger only).</summary>
        static Zone lastZone;
        static Entity Keeper(string zoneId, string blueprint = "ConcordFactor")
        {
            var zone = lastZone = new Zone(zoneId); var keeper = factory.CreateEntity(blueprint);
            Assert.True(zone.AddEntity(keeper, 20, 12)); return keeper;
        }

        static Entity Carry(Entity player, string blueprint, int count = 1)
        {
            var item = factory.CreateEntity(blueprint);
            if (count > 1) item.GetPart<StackerPart>().StackCount = count;
            Assert.True(player.GetPart<InventoryPart>().AddObject(item), "fixture: the pack takes it");
            return item;
        }

        static string Arg(string dest, Entity item) => dest + "|" + item.ID;
        static InventoryPart Pack(Entity p) => p.GetPart<InventoryPart>();
        static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = kind, Limit = 50 }).Records.Count;

        // ── the posts and the rates ──

        [Test] public void TheCartRunsBetweenCinderholdAndTheLastCounterAndNowhereElse()
        {
            CollectionAssert.AreEquivalent(new[] { Cinderhold, LastCounter }, ConcordCarriage.Posts.Select(p => p.ZoneId).ToArray());
            Assert.False(ConcordCarriage.TryPost(Sill, out _), "counter: an ordinary village keeps no cart");
            Assert.False(ConcordCarriage.TryPost(WorldMap.ToZoneID(10, 14, 0), out _), "counter: Tally keeps no ledger yet, so no cart");
        }

        [Test] public void TheRoadTakesDaysByDistanceAndIsTheSameBothWays()
        {
            ConcordCarriage.TryPost(Cinderhold, out var a); ConcordCarriage.TryPost(LastCounter, out var b);
            Assert.AreEqual(3, ConcordCarriage.Days(a, b)); Assert.AreEqual(3, ConcordCarriage.Days(b, a));
            Assert.AreEqual(1, ConcordCarriage.Days(a, a), "never less than a day");
        }

        [Test] public void TheFeeIsTheBaseRatePlusOneDramPerFiveWeight()
        {
            Assert.AreEqual(4, ConcordCarriage.Fee(Carry(Player(), "SalvagedTimber", 6)), "6 weight: 2 + 2");
            Assert.AreEqual(3, ConcordCarriage.Fee(Carry(Player(), "Dagger")), "4 weight: 2 + 1");
            Assert.AreEqual(3, ConcordCarriage.Fee(Carry(Player(), "LiquidFlask")), "counter: a light thing still pays the base and one");
            Assert.AreEqual(3, ConcordCarriage.Fee(Carry(Player(), "SalvagedTimber", 5)), "5 weight: exactly one dram on top");
            Assert.AreEqual(5, ConcordCarriage.Fee(Carry(Player(), "SalvagedTimber", 11)), "11 weight rounds up to three");
        }

        // ── what clause five and the carrying rules refuse ──

        [Test] public void ClauseFiveRefusesSporesBodiesAndAnythingThatSings_ByWhatItIsNotByHowItIsSpelled()
        {
            var p = Player(); var k = Keeper(Cinderhold);
            foreach (var spore in new[] { "BlastcapSpore", "ShamblerSporeSac" }) StringAssert.Contains("spores", ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity(spore)), spore);
            foreach (var body in new[] { "PreFellingBody", "BogTakenBody", "SaltCuredBody" }) StringAssert.Contains("bodies", ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity(body)), body);
            StringAssert.Contains("sings", ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("ChoirCutting")), "a living thread of the Choir");
            // The general doors: a tag opts any future thing in.
            foreach (var (tag, word) in new[] { ("Spore", "spores"), ("Corpse", "bodies"), ("Sings", "sings") })
            { var tagged = factory.CreateEntity("Dagger"); tagged.Tags[tag] = ""; StringAssert.Contains(word, ConcordCarriage.ItemRefusal(p, k, tagged), tag); }
            // Counters: a name that merely contains a word is not the thing, and ordinary goods go.
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("Sporeblade")), "a sword called a sporeblade is a sword, not a spore");
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("FruitingBody")), "a fungus is not a body in the Curation's sense");
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("Dagger"))); Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("SalvagedTimber")));
        }

        [Test] public void ThingsThatCannotChangeHandsAreRefused_BoundWornAndRented()
        {
            var p = Player(); var k = Keeper(Cinderhold);
            var bound = factory.CreateEntity("SealedBogTakenBody");
            StringAssert.Contains("change hands", ConcordCarriage.ItemRefusal(p, k, bound));
            var rented = factory.CreateEntity("Dagger"); rented.AddPart(new RentalPart());
            StringAssert.Contains("change hands", ConcordCarriage.ItemRefusal(p, k, rented), "a rental is the lessor's, never the cart's");
            var worn = factory.CreateEntity("Dagger"); worn.GetPart<PhysicsPart>().Equipped = p;
            StringAssert.Contains("Take it off", ConcordCarriage.ItemRefusal(p, k, worn));
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("Dagger")), "counter");
        }

        [Test] public void OnlyEligibleStacksAreListedHeaviestFirst()
        {
            var p = Player(); var k = Keeper(Cinderhold);
            var light = Carry(p, "FireClay"); var heavy = Carry(p, "SalvagedTimber", 9); Carry(p, "BlastcapSpore");
            CollectionAssert.AreEqual(new[] { heavy, light }, ConcordCarriage.Eligible(p, k), "heaviest first; the spore is not offered");
        }

        [Test] public void AManyStackPackIsPagedNotCut_EveryStackCanBeReached()
        {
            var p = Player(); var k = Keeper(Cinderhold); var all = new List<Entity>();
            foreach (var bp in new[] { "FireClay", "Dagger", "LiquidFlask", "SalvagedTimber", "MendleafSprig", "FireMoss", "IronSpikeComponent", "OakHaftComponent", "LeatherBindingComponent", "Torch", "RawMeat", "PitchpodResin" }) all.Add(Carry(p, bp));
            Assert.AreEqual(12, Pack(p).Objects.Count, "fixture: twelve distinct stacks");
            ConcordCarriage.TryPost(LastCounter, out var to);
            var first = ConcordCarriage.BuildNode("send|" + LastCounter, k, p); var second = ConcordCarriage.BuildNode("send|" + LastCounter + "|1", k, p);
            int rows(NodeData n) => n.Choices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == ConcordCarriage.OpenAction && a.Value.StartsWith("confirm|")));
            Assert.AreEqual(ConcordCarriage.PageSize, rows(first)); Assert.AreEqual(12 - ConcordCarriage.PageSize, rows(second), "the rest are on the next page");
            Assert.True(first.Choices.Any(c => c.Text.Contains("More goods")) && !first.Choices.Any(c => c.Text.Contains("Earlier goods")));
            Assert.True(second.Choices.Any(c => c.Text.Contains("Earlier goods")) && !second.Choices.Any(c => c.Text.Contains("More goods")), "counter: the last page has no more");
            StringAssert.Contains("page 1 of 2", first.Text);
            var reached = new HashSet<string>(); foreach (var n in new[] { first, second }) foreach (var c in n.Choices) foreach (var a in c.Actions ?? new List<ConversationParam>()) if (a.Value.StartsWith("confirm|")) reached.Add(a.Value.Split('|')[2]);
            CollectionAssert.AreEquivalent(all.Select(e => e.ID).ToArray(), reached.ToArray(), "no stack is unreachable");
            Assert.IsNull(ConcordCarriage.BuildNode("send|" + LastCounter + "|-1", k, p), "a negative page is refused"); Assert.IsNull(ConcordCarriage.BuildNode("send|" + LastCounter + "|x", k, p));
            StringAssert.Contains("page 2 of 2", ConcordCarriage.BuildNode("send|" + LastCounter + "|99", k, p).Text, "a page past the end shows the last one");
        }

        // ── sending ──

        [Test] public void SendingMovesTheRealGoodsOntoTheCartAndTakesTheFeeThroughTheLedger()
        {
            var p = Player(10); var k = Keeper(Cinderhold); var timber = Carry(p, "SalvagedTimber", 6); int before = TradeSystem.GetDrams(p);
            Assert.IsNull(ConcordCarriage.Send(k, p, Arg(LastCounter, timber)));
            Assert.False(Pack(p).Contains(timber), "the goods left the pack");
            var part = p.GetPart<ConcordCarriagePart>(); Assert.AreEqual(1, part.Count); Assert.AreSame(timber, part.Goods[0], "the same entity, not a copy");
            Assert.AreEqual(Cinderhold, part.From[0]); Assert.AreEqual(LastCounter, part.To[0]); Assert.AreEqual(4, part.Fee[0]);
            Assert.AreEqual(clock.TickCount + 3 * WorldClock.DayLengthTicks, part.ReadyTick[0]);
            Assert.AreEqual(before - 4, TradeSystem.GetDrams(p), "exactly the fee left the purse");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Debited", Limit = 5 }).Records.Count(r => r.PayloadJson.Contains("concord-carriage")), "a ledger sink");
            Assert.AreEqual(1, Count("CarriageSent"));
            StringAssert.Contains("3 days", string.Join(" ", MessageLog.GetRecent(3)));
        }

        [Test] public void AFlaskKeepsItsLiquidOnTheRoad()
        {
            var p = Player(); var k = Keeper(Cinderhold); var flask = Carry(p, "LiquidFlask");
            flask.GetPart<LiquidVesselPart>().LiquidId = "bog-mire"; flask.GetPart<LiquidVesselPart>().Volume = 9;
            Assert.IsNull(ConcordCarriage.Send(k, p, Arg(LastCounter, flask)));
            var gone = p.GetPart<ConcordCarriagePart>().Goods[0].GetPart<LiquidVesselPart>();
            Assert.AreEqual("bog-mire", gone.LiquidId); Assert.AreEqual(9, gone.Volume);
        }

        [Test]
        public void ALoadedFieldSatchelTravelsAsOneSealedParcelAndNeedsRoomForItsWholeWeight()
        {
            var player = Player();
            var factor = Keeper(Cinderhold);
            var origin = lastZone;
            Assert.True(origin.AddEntity(player, 19, 12));
            var satchel = Carry(player, "FieldSatchel");
            var food = Carry(player, "DriedMeat", 2);
            var tonic = Carry(player, "HealingTonic");
            Assert.True(CarriedStorage.TryDeposit(player, satchel, food, origin, out var foodFailure), foodFailure);
            Assert.True(CarriedStorage.TryDeposit(player, satchel, tonic, origin, out var tonicFailure), tonicFailure);
            var contents = satchel.GetPart<ContainerPart>().Contents;
            CollectionAssert.AreEquivalent(new[] { food, tonic }, contents);
            Assert.AreEqual(6, HandlingService.GetWeight(satchel));
            Assert.AreEqual(4, ConcordCarriage.Fee(satchel));
            Assert.AreEqual(6, ConcordCarriage.Fee(food) + ConcordCarriage.Fee(tonic));

            Assert.False(TradeSystem.CanBeTraded(satchel, player, factor, "Sell"), "A normal shop still quotes only the empty shell.");
            Assert.True(ConcordCarriage.Eligible(player, factor).Contains(satchel), "The freight list offers the sealed load.");
            Assert.IsNull(ConcordCarriage.Send(factor, player, Arg(LastCounter, satchel)));
            var carriage = player.GetPart<ConcordCarriagePart>();
            Assert.AreEqual(96, TradeSystem.GetDrams(player));
            Assert.AreSame(satchel, carriage.Goods.Single());
            Assert.IsEmpty(Pack(player).Objects);
            Assert.AreSame(satchel, food.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(satchel, tonic.GetPart<PhysicsPart>().InInventory);

            var envoy = Keeper(LastCounter, "SaccharineEnvoy");
            origin.RemoveEntity(player);
            Assert.True(lastZone.AddEntity(player, 19, 12));
            clock.AdvanceClock(3 * WorldClock.DayLengthTicks);
            Pack(player).MaxWeight = 5;
            Assert.AreEqual("too_heavy", ConcordCarriage.Collect(envoy, player, LastCounter));
            Assert.AreSame(satchel, carriage.Goods.Single());
            Assert.IsEmpty(Pack(player).Objects);
            Pack(player).MaxWeight = 6;
            Assert.IsNull(ConcordCarriage.Collect(envoy, player, LastCounter));
            Assert.IsEmpty(carriage.Goods);
            Assert.AreSame(satchel, Pack(player).Objects.Single());
            CollectionAssert.AreEquivalent(new[] { food, tonic }, contents);
            Assert.AreEqual(2, food.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(96, TradeSystem.GetDrams(player));
            Assert.AreEqual("nothing_to_collect", ConcordCarriage.Collect(envoy, player, LastCounter));
        }

        [Test]
        public void AChildVetoCannotBeBypassedByClosingTheSatchel()
        {
            var player = Player();
            var factor = Keeper(Cinderhold);
            Assert.True(lastZone.AddEntity(player, 19, 12));
            var satchel = Carry(player, "FieldSatchel");
            var food = Carry(player, "DriedMeat");
            var tonic = Carry(player, "HealingTonic");
            Assert.True(CarriedStorage.TryDeposit(player, satchel, food, lastZone, out _));
            Assert.True(CarriedStorage.TryDeposit(player, satchel, tonic, lastZone, out _));
            tonic.AddPart(new CarriageVetoProbe { EarlierItem = food });

            Assert.AreEqual("not_carried_by_cart", ConcordCarriage.Send(factor, player, Arg(LastCounter, satchel)));
            Assert.True(food.HasTag("Sings"), "An earlier child gained a Clause Five restriction in a later callback.");
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
            Assert.IsNull(player.GetPart<ConcordCarriagePart>());
            Assert.AreSame(satchel, Pack(player).Objects.Single());
            CollectionAssert.AreEquivalent(new[] { food, tonic }, satchel.GetPart<ContainerPart>().Contents);
        }

        sealed class CarriageVetoProbe : Part
        {
            internal Entity EarlierItem;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "CanBeTraded") EarlierItem.SetTag("Sings");
                return true;
            }
        }

        [Test]
        public void AQuotedSatchelRefusesAReplacementOfTheSameKindWeightAndCount()
        {
            var player = Player();
            var factor = Keeper(Cinderhold);
            Assert.True(lastZone.AddEntity(player, 19, 12));
            var satchel = Carry(player, "FieldSatchel");
            var original = Carry(player, "HealingTonic");
            Assert.True(CarriedStorage.TryDeposit(player, satchel, original, lastZone, out _));
            var node = ConcordCarriage.BuildNode("confirm|" + Arg(LastCounter, satchel), factor, player);
            StringAssert.Contains("Contents: healing tonic", node.Text);
            StringAssert.Contains("2 lb total", node.Text);
            var quote = node.Choices.SelectMany(c => c.Actions).Single(a => a.Key == ConcordCarriage.SendAction).Value;

            Assert.True(CarriedStorage.TryWithdraw(player, satchel, original, lastZone, out _));
            Assert.True(Pack(player).RemoveObject(original));
            var replacement = Carry(player, "HealingTonic");
            Assert.AreNotEqual(original.ID, replacement.ID);
            Assert.True(CarriedStorage.TryDeposit(player, satchel, replacement, lastZone, out _));

            Assert.AreEqual("not_carried_by_cart", ConcordCarriage.Send(factor, player, quote));
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
            Assert.IsNull(player.GetPart<ConcordCarriagePart>());
            Assert.AreSame(replacement, satchel.GetPart<ContainerPart>().Contents.Single());
        }

        [TestCase("insufficient_drams")] [TestCase("ledger_closed_default")] [TestCase("ledger_closed_standing")] [TestCase("cart_full")] [TestCase("worn")] [TestCase("spore")]
        public void ARefusedSendChangesNothing(string why)
        {
            var p = Player(100); var k = Keeper(Cinderhold); var item = Carry(p, why == "spore" ? "BlastcapSpore" : "SalvagedTimber", why == "spore" ? 1 : 6);
            if (why == "insufficient_drams") TradeSystem.SetDrams(p, 3);
            if (why == "ledger_closed_default") p.SetIntProperty(ConcordAccount.DefaultProp, 1);
            if (why == "ledger_closed_standing") PlayerReputation.Set(ConcordAccount.Faction, -60);
            if (why == "worn") item.GetPart<PhysicsPart>().Equipped = p;
            if (why == "cart_full")
            {
                var part = new ConcordCarriagePart(); p.AddPart(part);
                for (int i = 0; i < ConcordCarriage.MaxParcels; i++) part.Add(factory.CreateEntity("Dagger"), Cinderhold, LastCounter, 0, 99999, 3);
            }
            int drams = TradeSystem.GetDrams(p), parcels = p.GetPart<ConcordCarriagePart>()?.Count ?? 0;
            var refusal = ConcordCarriage.Send(k, p, Arg(LastCounter, item));
            Assert.IsNotNull(refusal); Assert.AreEqual(drams, TradeSystem.GetDrams(p), "no fee taken");
            Assert.True(Pack(p).Contains(item), "the goods are still in the pack"); Assert.AreEqual(parcels, p.GetPart<ConcordCarriagePart>()?.Count ?? 0);
            Assert.AreEqual(1, Count("CarriageRefused")); Assert.AreEqual(0, Count("CarriageSent"));
        }

        [Test] public void BadArgumentsAndWrongPlacesRefuseWithoutTouchingAnything()
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 3); var k = Keeper(Cinderhold);
            Assert.AreEqual("invalid_argument", ConcordCarriage.Send(k, p, "nonsense"));
            Assert.AreEqual("invalid_argument", ConcordCarriage.Send(k, p, null));
            Assert.AreEqual("unknown_destination", ConcordCarriage.Send(k, p, Arg(Sill, timber)), "the cart does not run to Sill");
            Assert.AreEqual("unknown_destination", ConcordCarriage.Send(k, p, Arg(Cinderhold, timber)), "nor from a post to itself");
            Assert.AreEqual("not_carried", ConcordCarriage.Send(k, p, LastCounter + "|no-such-item"));
            Assert.AreEqual("not_a_post", ConcordCarriage.Send(Keeper(Sill), p, Arg(LastCounter, timber)), "a keeper in a village without a cart");
            Assert.AreEqual("not_a_post", ConcordCarriage.Send(factory.CreateEntity("Merchant"), p, Arg(LastCounter, timber)), "a merchant keeps no ledger");
            var dead = Keeper(Cinderhold); dead.GetStat("Hitpoints").BaseValue = 0;
            Assert.AreEqual("not_a_post", ConcordCarriage.Send(dead, p, Arg(LastCounter, timber)), "a dead keeper takes nothing");
            Assert.True(Pack(p).Contains(timber)); Assert.AreEqual(100, TradeSystem.GetDrams(p)); Assert.IsNull(p.GetPart<ConcordCarriagePart>());
        }

        [Test] public void TheEnvoyAtTheLastCounterRunsTheCartToo()
        {
            var p = Player(); var envoy = Keeper(LastCounter, "SaccharineEnvoy"); var dagger = Carry(p, "Dagger");
            Assert.IsNull(ConcordCarriage.Send(envoy, p, Arg(Cinderhold, dagger)));
            Assert.AreEqual(Cinderhold, p.GetPart<ConcordCarriagePart>().To[0]);
        }

        // ── collecting ──

        [Test] public void NothingArrivesEarlyAndTheSameGoodsComeBackOnTime()
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 6); Assert.IsNull(ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, timber)));
            var there = Keeper(LastCounter, "SaccharineEnvoy");
            clock.AdvanceClock(3 * WorldClock.DayLengthTicks - 1);
            Assert.AreEqual("nothing_to_collect", ConcordCarriage.Collect(there, p, LastCounter)); Assert.AreEqual(1, p.GetPart<ConcordCarriagePart>().Count, "still on the road");
            clock.AdvanceClock(1);
            Assert.AreEqual(1, ConcordCarriage.ReadyHere(p, LastCounter, clock.TickCount));
            Assert.IsNull(ConcordCarriage.Collect(there, p, LastCounter));
            Assert.True(Pack(p).Contains(timber), "the same entity is back"); Assert.AreEqual(6, timber.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(0, p.GetPart<ConcordCarriagePart>().Count); Assert.AreEqual(1, Count("CarriageCollected"));
        }

        [Test] public void AParcelIsOnlyHandedOverAtItsOwnPost_Counter()
        {
            var p = Player(); var dagger = Carry(p, "Dagger"); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, dagger)); clock.AdvanceClock(10 * WorldClock.DayLengthTicks);
            var back = Keeper(Cinderhold);
            Assert.AreEqual(0, ConcordCarriage.ReadyHere(p, Cinderhold, clock.TickCount), "it is bound for the Last Counter");
            Assert.AreEqual("nothing_to_collect", ConcordCarriage.Collect(back, p, Cinderhold));
            Assert.False(Pack(p).Contains(dagger));
        }

        [Test] public void AParcelThatDoesNotFitStaysOnTheCartAndTheMessageSaysSo()
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 20); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, timber)); clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            Pack(p).MaxWeight = 5; MessageLog.Clear();
            var there = Keeper(LastCounter, "SaccharineEnvoy");
            Assert.AreEqual("too_heavy", ConcordCarriage.Collect(there, p, LastCounter));
            Assert.AreEqual(1, p.GetPart<ConcordCarriagePart>().Count, "nothing lost");
            StringAssert.Contains("cannot carry", string.Join(" ", MessageLog.GetRecent(3)));
            Pack(p).MaxWeight = 400; Assert.IsNull(ConcordCarriage.Collect(there, p, LastCounter)); Assert.True(Pack(p).Contains(timber), "and it comes once there is room");
        }

        [Test] public void ACollectedStackMergesIntoOneAlreadyCarried()
        {
            var p = Player(); var sent = Carry(p, "SalvagedTimber", 4); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, sent)); clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            var kept = Carry(p, "SalvagedTimber", 2);
            Assert.IsNull(ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), p, LastCounter));
            Assert.AreEqual(6, kept.GetPart<StackerPart>().StackCount, "the arrived stack joined the carried one"); Assert.AreEqual(0, p.GetPart<ConcordCarriagePart>().Count);
        }

        [Test] public void ATornRecordIsNeverDeliveredOrCountedAndNeverCrashes()
        {
            var p = Player(); var part = new ConcordCarriagePart(); p.AddPart(part);
            part.Goods.Add(null); part.From.Add(Cinderhold); part.To.Add(LastCounter); part.SentTick.Add(0); part.ReadyTick.Add(0); part.Fee.Add(3);
            part.Goods.Add(factory.CreateEntity("Dagger")); part.From.Add(Cinderhold); part.To.Add(LastCounter); // short lists: no ticks or fee
            Assert.AreEqual(0, ConcordCarriage.ReadyHere(p, LastCounter, 99999));
            Assert.IsNull(ConcordCarriage.Statement(p, LastCounter, 99999));
            Assert.AreEqual("nothing_to_collect", ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), p, LastCounter));
        }

        // ── what the greeting says ──

        [Test] public void TheStatementNamesEachParcelItsPlaceAndItsTime()
        {
            var p = Player(); Assert.IsNull(ConcordCarriage.Statement(p, Cinderhold, 0), "nothing in your name: nothing said");
            var timber = Carry(p, "SalvagedTimber", 6); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, timber));
            string onTheRoad = ConcordCarriage.Statement(p, Cinderhold, clock.TickCount);
            StringAssert.Contains("salvaged timber", onTheRoad.ToLowerInvariant()); StringAssert.Contains("the Last Counter", onTheRoad); StringAssert.Contains("due in 3 days", onTheRoad);
            clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            StringAssert.Contains("ready to collect", ConcordCarriage.Statement(p, LastCounter, clock.TickCount)); StringAssert.Contains("this post", ConcordCarriage.Statement(p, LastCounter, clock.TickCount));
            StringAssert.Contains("waiting at the Last Counter", ConcordCarriage.Statement(p, Cinderhold, clock.TickCount), "from the other post it reads as waiting");
        }

        // ── in conversation ──

        static ConversationData Start(string id)
        {
            var conv = new ConversationData { ID = id };
            conv.Nodes.Add(new NodeData { ID = "Start", Text = "The post is open.", Choices = new List<ChoiceData> { new ChoiceData { Text = "More.", Target = "More" } } });
            conv.Nodes.Add(new NodeData { ID = "More", Text = "Yes?", Choices = new List<ChoiceData> { new ChoiceData { Text = "Back.", Target = "Start" } } });
            return conv;
        }
        static List<ChoiceData> With(string action) => ConversationManager.VisibleChoices.Where(c => c.Actions != null && c.Actions.Any(a => a.Key == action)).ToList();
        static void Choose(ChoiceData c) => ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(c));

        [Test] public void ThePostGreetingOffersTheCartAndTheWholeBargainBeforeAnythingIsSpent()
        {
            ConversationLoader.Register(Start("ConcordFactor_1"));
            var p = Player(20); var timber = Carry(p, "SalvagedTimber", 6); var factor = Keeper(Cinderhold);
            Assert.True(ConversationManager.StartConversation(factor, p));
            Assert.IsEmpty(With(ConcordCarriage.CollectAction), "nothing has arrived: no collect row");
            var open = With(ConcordCarriage.OpenAction); Assert.AreEqual(1, open.Count);
            StringAssert.Contains("Send a parcel to the Last Counter (about 3 days)", open[0].Text);
            Choose(open[0]);
            StringAssert.StartsWith(ConcordCarriage.NodePrefix, ConversationManager.CurrentNode.ID);
            StringAssert.Contains("The rate is the rate", ConversationManager.CurrentText); StringAssert.Contains("The cart stops at the Last Counter", ConversationManager.CurrentText);
            var stack = With(ConcordCarriage.OpenAction).Single(c => c.Text.Contains("salvaged timber", System.StringComparison.OrdinalIgnoreCase));
            StringAssert.Contains("4 drams", stack.Text);
            Choose(stack);
            StringAssert.Contains("3 days", ConversationManager.CurrentText); StringAssert.Contains("You carry 20 drams", ConversationManager.CurrentText);
            var rows = With(ConcordCarriage.SendAction);
            Assert.AreEqual(3, rows.Count, "all, half and one"); StringAssert.Contains("all 6 - 4 drams", rows[0].Text); StringAssert.Contains("half (3) - 3 drams", rows[1].Text); StringAssert.Contains("one - 3 drams", rows[2].Text);
            Assert.AreEqual(20, TradeSystem.GetDrams(p), "nothing spent while reading the bargain"); Assert.True(Pack(p).Contains(timber));
            Choose(rows[0]);
            Assert.AreEqual(16, TradeSystem.GetDrams(p)); Assert.False(Pack(p).Contains(timber));
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID, "back at the greeting");
            StringAssert.Contains("In your name on the cart", ConversationManager.CurrentText);
        }

        [Test] public void TheGreetingOffersCollectOnlyWhenSomethingHasArrivedAndCollectHandsItOver()
        {
            ConversationLoader.Register(Start("SaccharineEnvoy_1"));
            var p = Player(); var dagger = Carry(p, "Dagger"); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, dagger));
            var envoy = Keeper(LastCounter, "SaccharineEnvoy");
            Assert.True(ConversationManager.StartConversation(envoy, p)); Assert.IsEmpty(With(ConcordCarriage.CollectAction)); ConversationManager.EndConversation();
            clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            Assert.True(ConversationManager.StartConversation(envoy, p));
            var collect = With(ConcordCarriage.CollectAction); Assert.AreEqual(1, collect.Count); StringAssert.Contains("Collect what has arrived (1)", collect[0].Text);
            Choose(collect[0]);
            Assert.True(Pack(p).Contains(dagger)); Assert.IsEmpty(With(ConcordCarriage.CollectAction), "nothing left to collect");
        }

        [Test] public void NoCartAnywhereButAPost_Counters()
        {
            ConversationLoader.Register(Start("ConcordFactor_1")); ConversationLoader.Register(Start("Merchant_1")); var p = Player(); Carry(p, "Dagger");
            Assert.True(ConversationManager.StartConversation(Keeper(Sill), p)); Assert.IsEmpty(With(ConcordCarriage.OpenAction), "a Factor outside a post offers no cart"); ConversationManager.EndConversation();
            Assert.True(ConversationManager.StartConversation(factory.CreateEntity("Merchant"), p)); Assert.IsEmpty(With(ConcordCarriage.OpenAction)); ConversationManager.EndConversation();
            Assert.True(ConversationManager.StartConversation(Keeper(Cinderhold), p)); Assert.AreEqual(1, With(ConcordCarriage.OpenAction).Count, "at the post it does");
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "More"));
            Assert.IsEmpty(With(ConcordCarriage.OpenAction), "only the greeting carries the cart");
        }

        [Test] public void AClosedLedgerSaysSoInTheSendNodeInsteadOfListingStacks()
        {
            ConversationLoader.Register(Start("ConcordFactor_1")); var p = Player(); Carry(p, "Dagger"); p.SetIntProperty(ConcordAccount.DefaultProp, 1);
            Assert.True(ConversationManager.StartConversation(Keeper(Cinderhold), p)); Choose(With(ConcordCarriage.OpenAction).Single());
            StringAssert.Contains("no room for your name", ConversationManager.CurrentText);
            Assert.IsEmpty(With(ConcordCarriage.OpenAction), "no stack is listed to a closed ledger");
        }

        [Test] public void AnEmptyPackIsToldNothingWillGo_AndAFullStackListIsNotOffered()
        {
            ConversationLoader.Register(Start("ConcordFactor_1")); var p = Player(); Carry(p, "BlastcapSpore");
            Assert.True(ConversationManager.StartConversation(Keeper(Cinderhold), p)); Choose(With(ConcordCarriage.OpenAction).Single());
            StringAssert.Contains("Nothing you carry will go", ConversationManager.CurrentText); Assert.IsEmpty(With(ConcordCarriage.OpenAction));
        }

        [Test] public void AShortPurseSeesTheFeeAndNoSendRow()
        {
            ConversationLoader.Register(Start("ConcordFactor_1")); var p = Player(2); Carry(p, "SalvagedTimber", 6);
            Assert.True(ConversationManager.StartConversation(Keeper(Cinderhold), p)); Choose(With(ConcordCarriage.OpenAction).Single());
            Choose(With(ConcordCarriage.OpenAction).Single(c => c.Text.Contains("drams")));
            StringAssert.Contains("You do not have the fee", ConversationManager.CurrentText); Assert.IsEmpty(With(ConcordCarriage.SendAction), "the choice never lies");
        }

        // ── part of a stack ──

        [Test] public void SendingPartOfAStackLeavesTheRestAndPaysForOnlyWhatGoes()
        {
            var p = Player(10); var timber = Carry(p, "SalvagedTimber", 6); var k = Keeper(Cinderhold);
            Assert.IsNull(ConcordCarriage.Send(k, p, Arg(LastCounter, timber) + "|2"));
            Assert.True(Pack(p).Contains(timber)); Assert.AreEqual(4, timber.GetPart<StackerPart>().StackCount, "four stay");
            var cart = p.GetPart<ConcordCarriagePart>(); Assert.AreEqual(1, cart.Count); Assert.AreNotSame(timber, cart.Goods[0]); Assert.AreEqual(2, cart.Goods[0].GetPart<StackerPart>().StackCount);
            Assert.AreEqual(3, cart.Fee[0], "two weight: 2 + 1"); Assert.AreEqual(7, TradeSystem.GetDrams(p));
            clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            Assert.IsNull(ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), p, LastCounter));
            Assert.AreEqual(6, timber.GetPart<StackerPart>().StackCount, "the two came back into the stack; nothing lost or made");
        }

        [TestCase("0")] [TestCase("-1")] [TestCase("7")] [TestCase("abc")] [TestCase("")]
        public void ABadCountRefusesAndChangesNothing(string count)
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 6); var k = Keeper(Cinderhold);
            Assert.AreEqual("invalid_argument", ConcordCarriage.Send(k, p, Arg(LastCounter, timber) + "|" + count));
            Assert.AreEqual(6, timber.GetPart<StackerPart>().StackCount); Assert.AreEqual(100, TradeSystem.GetDrams(p)); Assert.IsNull(p.GetPart<ConcordCarriagePart>());
        }

        [Test] public void ACountOfTheWholeStackSendsTheWholeStackNotASplit()
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 6);
            Assert.IsNull(ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, timber) + "|6"));
            Assert.False(Pack(p).Contains(timber)); Assert.AreSame(timber, p.GetPart<ConcordCarriagePart>().Goods[0]);
        }

        [Test] public void AStackThatWillNotDivideIsRefusedWholeNotHalfSent()
        {
            var p = Player(); var timber = Carry(p, "SalvagedTimber", 6);
            timber.AddPart(new CuttingToolPart()); var fx = new StatusEffectsPart(); timber.AddPart(fx); Assert.True(fx.ApplyEffect(new WetEffect(), null, null), "fixture: an effect on the instance");
            Assert.IsNull(timber.GetPart<StackerPart>().SplitStack(1), "fixture: this stack really will not divide");
            Assert.AreEqual("cannot_divide", ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, timber) + "|2"));
            Assert.AreEqual(6, timber.GetPart<StackerPart>().StackCount); Assert.AreEqual(100, TradeSystem.GetDrams(p)); Assert.IsNull(p.GetPart<ConcordCarriagePart>());
        }

        [Test] public void TheBargainOffersAllHalfAndOneOnlyForStacksAndOnlyWhatThePurseCanPay()
        {
            var k = Keeper(Cinderhold); ConcordCarriage.TryPost(Cinderhold, out var here); ConcordCarriage.TryPost(LastCounter, out var to);
            var lone = Carry(Player(), "Dagger"); var pair = Carry(Player(), "SalvagedTimber", 2); var many = Carry(Player(), "SalvagedTimber", 7);
            CollectionAssert.AreEqual(new[] { 1 }, ConcordCarriage.Quantities(lone)); CollectionAssert.AreEqual(new[] { 2, 1 }, ConcordCarriage.Quantities(pair)); CollectionAssert.AreEqual(new[] { 7, 3, 1 }, ConcordCarriage.Quantities(many));
            CollectionAssert.AreEqual(new[] { 3, 1 }, ConcordCarriage.Quantities(Carry(Player(), "SalvagedTimber", 3)), "three: half is one, offered once");
            var stuck = Carry(Player(), "SalvagedTimber", 6); stuck.AddPart(new CuttingToolPart()); var fx = new StatusEffectsPart(); stuck.AddPart(fx); fx.ApplyEffect(new WetEffect(), null, null);
            CollectionAssert.AreEqual(new[] { 6 }, ConcordCarriage.Quantities(stuck), "a stack that will not divide is offered whole only");
            var poor = Player(3); Carry(poor, "SalvagedTimber", 7);
            var node = ConcordCarriage.BuildNode("confirm|" + LastCounter + "|" + Pack(poor).Objects[0].ID, k, poor);
            Assert.AreEqual(2, node.Choices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == ConcordCarriage.SendAction)), "three drams buys half or one but not all: the dearer row is not offered");
            Assert.False(node.Choices.Any(c => c.Text.Contains("all 7")), "counter");
        }

        // ── held for work, and the map ──

        /// <summary>Put an item in the liquid-recovery custody table the way an in-flight recovery does (a weak table keyed by the item), so the cart sees it as held.</summary>
        static void HoldForWork(Entity item)
        {
            var table = typeof(ContainedLiquidSourcePart).GetField("RecoveryCustody", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            table.GetType().GetMethod("Add").Invoke(table, new object[] { item, new object() });
        }

        [Test] public void AnItemHeldForWorkInProgressIsNotCarried()
        {
            var p = Player(); var k = Keeper(Cinderhold); var held = factory.CreateEntity("Dagger"); Pack(p).AddObject(held);
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, held), "fixture: carried freely before the hold");
            HoldForWork(held);
            StringAssert.Contains("work in progress", ConcordCarriage.ItemRefusal(p, k, held));
            Assert.AreEqual("not_carried_by_cart", ConcordCarriage.Send(k, p, Arg(LastCounter, held))); Assert.True(Pack(p).Contains(held));
            Assert.IsNull(ConcordCarriage.ItemRefusal(p, k, factory.CreateEntity("Dagger")), "counter: another dagger is not held");
        }

        [Test] public void TheMapSaysHowManyParcelsRideAndWhereAnyHaveArrived()
        {
            var p = Player(); Assert.IsNull(ConcordCarriage.MapNote(p, 0), "nothing on the cart: nothing said");
            var a = Carry(p, "Dagger"); var b = Carry(p, "FireClay"); var k = Keeper(Cinderhold);
            ConcordCarriage.Send(k, p, Arg(LastCounter, a)); ConcordCarriage.Send(k, p, Arg(LastCounter, b));
            Assert.AreEqual("The Concord's cart carries 2 parcels in your name.", ConcordCarriage.MapNote(p, clock.TickCount));
            clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            Assert.AreEqual("The Concord's cart carries 2 parcels in your name; 2 have arrived at the Last Counter.", ConcordCarriage.MapNote(p, clock.TickCount));
            MessageLog.Clear(); ConcordCarriage.OnMapOpened(p); StringAssert.Contains("2 parcels", string.Join(" ", MessageLog.GetRecent(3)));
            MessageLog.Clear(); ConcordCarriage.OnMapOpened(Player()); Assert.IsEmpty(MessageLog.GetRecent(3), "counter: no parcels, no line");
        }

        // ── review fixes: the ledger found late, torn records, a real worn item, a partial merge, a keeper who cannot work ──

        [Test] public void ADebtPastItsDayIsFoundByTheCartWithoutLeavingTheZone()
        {
            var p = Player(); ConcordAccount.Borrow(Keeper(Cinderhold), p, 20, clock.TickCount); var dagger = Carry(p, "Dagger"); var k = Keeper(Cinderhold);
            Assert.IsNull(ConcordCarriage.GateRefusal(p), "counter: a debt inside its term is no bar");
            clock.AdvanceClock(ConcordAccount.TermTicks + 5);
            Assert.False(ConcordAccount.InDefault(p), "fixture: nothing has reviewed the account yet");
            Assert.AreEqual("ledger_closed", ConcordCarriage.Send(k, p, Arg(LastCounter, dagger))); Assert.True(ConcordAccount.InDefault(p), "the cart's own check found it, as Borrow and Repay would");
            Assert.True(Pack(p).Contains(dagger));
        }

        [Test] public void ATornRecordDoesNotUseUpASlotAndANewParcelStaysAlignedWithItsOwnRecord()
        {
            var p = Player(); var part = new ConcordCarriagePart(); p.AddPart(part);
            for (int i = 0; i < ConcordCarriage.MaxParcels; i++) { part.Goods.Add(null); part.From.Add(Cinderhold); part.To.Add(LastCounter); part.SentTick.Add(0); part.ReadyTick.Add(0); part.Fee.Add(3); }
            Assert.AreEqual(0, part.WholeCount); Assert.IsNull(ConcordCarriage.GateRefusal(p), "six torn records do not fill a cart of six");
            var dagger = Carry(p, "Dagger"); Assert.IsNull(ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, dagger)));
            Assert.AreEqual(1, part.WholeCount); int i0 = part.Goods.IndexOf(dagger);
            Assert.AreEqual(Cinderhold, part.From[i0]); Assert.AreEqual(LastCounter, part.To[i0]); Assert.AreEqual(3, part.Fee[i0], "the new parcel's fee sits at its own index");
            var short1 = new ConcordCarriagePart(); short1.Goods.Add(factory.CreateEntity("Dagger")); short1.Add(factory.CreateEntity("FireClay"), Cinderhold, LastCounter, 1, 2, 9);
            Assert.False(short1.Whole(0), "the record with no ends stays torn"); Assert.True(short1.Whole(1)); Assert.AreEqual(9, short1.Fee[1]); Assert.AreEqual(2, short1.ReadyTick[1]);
        }

        [Test] public void ARealWornItemIsNeverOfferedAndNeverSent()
        {
            var p = Player(); var k = Keeper(Cinderhold); var armor = Carry(p, "LeatherArmor"); Assert.True(InventorySystem.Equip(p, armor), "fixture: really worn");
            CollectionAssert.DoesNotContain(ConcordCarriage.Eligible(p, k), armor);
            Assert.AreEqual("not_carried", ConcordCarriage.Send(k, p, Arg(LastCounter, armor)), "a worn item is not in the carried list, so there is nothing to send");
            Assert.AreEqual(100, TradeSystem.GetDrams(p)); Assert.IsNull(p.GetPart<ConcordCarriagePart>());
            Assert.True(InventorySystem.UnequipItem(p, armor)); CollectionAssert.Contains(ConcordCarriage.Eligible(p, k), armor, "counter: taken off, it can go");
        }

        [Test] public void AnArrivalThatOnlyPartlyMergesLeavesTheRestAsItsOwnStack()
        {
            var p = Player(); var sent = Carry(p, "SalvagedTimber", 10); ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, sent)); clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            var kept = Carry(p, "SalvagedTimber", 15); int max = kept.GetPart<StackerPart>().MaxStack;
            Assert.IsNull(ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), p, LastCounter));
            var stacks = Pack(p).Objects.Where(o => o.BlueprintName == "SalvagedTimber").ToList();
            Assert.AreEqual(25, stacks.Sum(o => o.GetPart<StackerPart>().StackCount), "all twenty-five are carried: nothing lost or made"); Assert.AreEqual(2, stacks.Count, "a full stack and the remainder"); Assert.AreEqual(max, kept.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(0, p.GetPart<ConcordCarriagePart>().Count);
        }

        [Test] public void AKeeperWhoCannotWorkTakesAndGivesNothingAndSaysWhy()
        {
            var p = Player(); var dagger = Carry(p, "Dagger"); var k = Keeper(Cinderhold);
            k.ApplyEffect(new StunnedEffect(), null, lastZone); Assert.True(TradeSystem.TraderUnableToTrade(k, out _), "fixture: the keeper is stunned");
            MessageLog.Clear(); Assert.AreEqual("keeper_unable", ConcordCarriage.Send(k, p, Arg(LastCounter, dagger))); StringAssert.Contains("stunned", string.Join(" ", MessageLog.GetRecent(3)));
            Assert.True(Pack(p).Contains(dagger)); Assert.AreEqual(100, TradeSystem.GetDrams(p));
            StringAssert.Contains("stunned", ConcordCarriage.BuildNode("send|" + LastCounter, k, p).Text);
            var other = Keeper(LastCounter, "SaccharineEnvoy"); var otherZone = lastZone; ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, dagger)); clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            other.ApplyEffect(new FrozenEffect(), null, otherZone);
            Assert.AreEqual("keeper_unable", ConcordCarriage.Collect(other, p, LastCounter)); Assert.AreEqual(1, p.GetPart<ConcordCarriagePart>().Count, "the parcel waits");
        }

        // ── saving ──

        [Test] public void TheCartRidesTheSaveWithTheRealGoodsIntact_AndStillDeliversAfterTheReload()
        {
            var p = Player(); var flask = Carry(p, "LiquidFlask"); flask.GetPart<LiquidVesselPart>().LiquidId = "bog-mire"; flask.GetPart<LiquidVesselPart>().Volume = 7;
            var timber = Carry(p, "SalvagedTimber", 6); string timberId = timber.ID, flaskId = flask.ID;
            var k = Keeper(Cinderhold); ConcordCarriage.Send(k, p, Arg(LastCounter, flask)); ConcordCarriage.Send(k, p, Arg(LastCounter, timber));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(p); var part = loaded.GetPart<ConcordCarriagePart>();
            Assert.NotNull(part); Assert.AreEqual(2, part.Count);
            Assert.AreEqual(new[] { Cinderhold, Cinderhold }, part.From.ToArray()); Assert.AreEqual(new[] { LastCounter, LastCounter }, part.To.ToArray());
            Assert.AreEqual(p.GetPart<ConcordCarriagePart>().ReadyTick.ToArray(), part.ReadyTick.ToArray()); Assert.AreEqual(p.GetPart<ConcordCarriagePart>().Fee.ToArray(), part.Fee.ToArray());
            var f = part.Goods.Single(g => g.GetPart<LiquidVesselPart>() != null); Assert.AreEqual("bog-mire", f.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(7, f.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(flaskId, f.ID);
            var t = part.Goods.Single(g => g.GetPart<StackerPart>() != null); Assert.AreEqual(6, t.GetPart<StackerPart>().StackCount, "the stack count survives"); Assert.AreEqual(timberId, t.ID);
            Assert.True(Enumerable.Range(0, part.Count).All(part.Whole));
            Assert.IsNull(t.GetPart<PhysicsPart>()?.InInventory, "the goods hold no stale pack back-reference after the load");
            clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            Assert.IsNull(ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), loaded, LastCounter), "the reloaded cart still delivers");
            Assert.True(Pack(loaded).Objects.Any(o => o.ID == timberId) && Pack(loaded).Objects.Any(o => o.ID == flaskId), "both come back with their own identities");
        }

        // ── honesty: the carriage never makes money ──

        [Test] public void ARoundTripCostsDramsAndNeverMintsAny()
        {
            var p = Player(50); int start = TradeSystem.GetDrams(p); var dagger = Carry(p, "Dagger");
            ConcordCarriage.Send(Keeper(Cinderhold), p, Arg(LastCounter, dagger)); clock.AdvanceClock(4 * WorldClock.DayLengthTicks);
            ConcordCarriage.Collect(Keeper(LastCounter, "SaccharineEnvoy"), p, LastCounter);
            Assert.AreEqual(start - 3, TradeSystem.GetDrams(p), "the fee, once, and nothing back");
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Credited", Limit = 5 }).Records.Count, "no credit entered the world");
        }
    }
}
