using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// First-wave player-flow specifications. Real native managers, recipients,
    /// inventories and pickup/action commands are used; no synthetic giver or
    /// direct completion-property writes stand in for successful play.
    /// Kept outside Assets until the independent art milestone has finished.
    /// </summary>
    public sealed class RegionalSituationAdversarialTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;

        private static readonly string[] BindingIds =
        {
            "morrowfast-iron", "cinderhold-iron", "gantry-grain",
            "sumphold-oil", "wellmeet-filters"
        };

        [SetUp]
        public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            playerZone = null;
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            player.GetPart<InventoryPart>().MaxWeight = -1;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }


        [TestCase("dead-player")][TestCase("wrong-player")][TestCase("remote")]
        [TestCase("dead-recipient")][TestCase("foreign-zone")][TestCase("wrong-recipient-id")]
        public void AuthorityRefusalCannotConsumeSupplyAndRestoredAuthorityWorks(string fault)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Carry("Emberwheat",2);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            var hp=player.GetStat("Hitpoints");var rhp=b.recipient.GetStat("Hitpoints");
            int pv=hp.BaseValue,rv=rhp.BaseValue;string recipientId=b.request.RecipientId;
            var before=b.zone.GetEntityCell(player);int x=before.X,y=before.Y;
            Entity actor=player;Zone supplied=b.zone;
            if(fault=="dead-player")hp.BaseValue=0;
            if(fault=="dead-recipient")rhp.BaseValue=0;
            if(fault=="wrong-player")actor=factory.CreateEntity("Player");
            if(fault=="foreign-zone")supplied=new Zone(b.zone.ZoneID);
            if(fault=="wrong-recipient-id")b.request.RecipientId="not-this-recipient";
            if(fault=="remote")
            {
                var at=b.zone.GetEntityCell(b.recipient);
                var far=Enumerable.Range(0,Zone.Width*Zone.Height).Select(i=>b.zone.GetCell(i%Zone.Width,i/Zone.Width))
                    .First(c=>!c.BlocksMovement()&&Math.Abs(c.X-at.X)+Math.Abs(c.Y-at.Y)>10);
                MovePlayer(b.zone,far.X,far.Y);
            }
            int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,"Emberwheat");
            Assert.IsFalse(b.request.TryAct(actor,supplied,"deliver"));
            Assert.AreEqual(2,Units(player,"Emberwheat"));Assert.AreEqual(stock,Units(b.recipient,"Emberwheat"));
            Assert.AreEqual(money,TradeSystem.GetDrams(player));Assert.IsFalse(b.request.Completed);
            hp.BaseValue=pv;rhp.BaseValue=rv;b.request.RecipientId=recipientId;
            if(fault=="remote")MovePlayer(b.zone,x,y);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
        }
        [TestCase("equipped")][TestCase("foreign-owner")][TestCase("zero-stack")]
        public void InvalidSupplyOwnershipAndQuantityCannotComplete(string fault)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",2);
            var item=player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Emberwheat");
            var physics=item.GetPart<PhysicsPart>();var stack=item.GetPart<StackerPart>();Assert.NotNull(stack);
            if(fault=="equipped")physics.Equipped=player;
            if(fault=="foreign-owner")physics.InInventory=b.recipient;
            if(fault=="zero-stack")stack.StackCount=0;
            int money=TradeSystem.GetDrams(player);Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));
            Assert.IsFalse(b.request.Completed);Assert.AreEqual(money,TradeSystem.GetDrams(player));
            physics.Equipped=null;physics.InInventory=player;stack.StackCount=2;
            Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
        }
        [TestCase(false)][TestCase(true)]
        public void CurrencyOverflowRollsBackExactSplitAndDestinationMerge(bool overflow)
        {
            var d=RegionalSituations.Find("gantry-grain");var b=Bind(d.Id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry(d.ItemBlueprint,5);
            var source=player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName==d.ItemBlueprint);
            int money=overflow?int.MaxValue:0;TradeSystem.SetDrams(player,money);int stock=Units(b.recipient,d.ItemBlueprint);
            Assert.AreEqual(!overflow,b.request.TryAct(player,b.zone,"deliver"));
            Assert.AreEqual(overflow?5:3,Units(player,d.ItemBlueprint));Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(source));
            Assert.AreEqual(stock+(overflow?0:2),Units(b.recipient,d.ItemBlueprint));Assert.AreEqual(overflow?money:d.RewardDrams,TradeSystem.GetDrams(player));
            Assert.AreEqual(!overflow,b.request.Completed);
            if(overflow){TradeSystem.SetDrams(player,0);Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));}
        }
        [TestCase(false)][TestCase(true)]
        public void ReceiverCapacityRefusalRestoresSupplyAndAllowsRetry(bool full)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",2);
            var inv=b.recipient.GetPart<InventoryPart>();inv.MaxWeight=full?1:-1;
            int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,"Emberwheat");
            Assert.AreEqual(!full,b.request.TryAct(player,b.zone,"deliver"));
            Assert.AreEqual(full?2:0,Units(player,"Emberwheat"));Assert.AreEqual(stock+(full?0:2),Units(b.recipient,"Emberwheat"));
            if(full){Assert.AreEqual(money,TradeSystem.GetDrams(player));inv.MaxWeight=-1;Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));}
        }
        [TestCase(false)][TestCase(true)]
        public void SplitCloneFailureRestoresSourceAndReleasesRequestClaim(bool throws)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",5);
            var source=player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Emberwheat");source.AddPart(new SeparateOneCloneProbePart());
            int seen=0,stock=Units(b.recipient,"Emberwheat");
            SeparateOneCloneProbePart.Callback=e=>{seen++;if(throws)throw new InvalidOperationException("Regional split probe");};
            try
            {
                Assert.AreEqual(!throws,b.request.TryAct(player,b.zone,"deliver"));Assert.Greater(seen,0);
                Assert.AreEqual(throws?5:3,Units(player,"Emberwheat"));Assert.AreEqual(stock+(throws?0:2),Units(b.recipient,"Emberwheat"));
            }
            finally{SeparateOneCloneProbePart.Callback=null;}
            if(throws)Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
        }
        [TestCase(false)][TestCase(true)]
        public void NativeActionPostCallbackFailureIsAtomic(bool throws)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",2);
            var hook=new Probe{Event="AfterInventoryAction",Run=e=>{if(throws)throw new InvalidOperationException("Regional post-action probe");}};player.AddPart(hook);
            int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,"Emberwheat");
            var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient,"RegionalRequest:deliver"),player,b.zone);
            Assert.AreEqual(!throws,result.Success);Assert.AreEqual(1,hook.Calls);
            Assert.AreEqual(throws?2:0,Units(player,"Emberwheat"));Assert.AreEqual(stock+(throws?0:2),Units(b.recipient,"Emberwheat"));
            Assert.AreEqual(!throws,b.request.Completed);
            if(throws){Assert.AreEqual(money,TradeSystem.GetDrams(player));player.RemovePart(hook);Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));}
        }
        [Test] public void WalletObserverCannotReenterWithASecondEligibleStack()
        {
            var d=RegionalSituations.Find("gantry-grain");var b=Bind(d.Id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",4);
            int money=TradeSystem.GetDrams(player);bool nested=true;
            var hook=new Probe{Event="IntPropertyChanged",Run=e=>{if(e.GetStringParameter("Name")==TradeSystem.CURRENCY_PROP)nested=b.request.TryAct(player,b.zone,"deliver");}};player.AddPart(hook);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));Assert.Greater(hook.Calls,0);Assert.IsFalse(nested);
            Assert.AreEqual(2,Units(player,"Emberwheat"));Assert.AreEqual(money+d.RewardDrams,TradeSystem.GetDrams(player));
        }
        [TestCase(false)][TestCase(true)]
        public void ActualNativeHarvestChargesOnlyProtectedMineralExtraction(bool grain)
        {
            string id=grain?"gantry-grain":"morrowfast-iron";var d=RegionalSituations.Find(id);
            var zone=manager.GetZone(d.SourceZoneId);var source=Source(zone,id);StandBy(zone,source);
            var previous=HarvestablePart.Factory;HarvestablePart.Factory=factory;
            int before=PlayerReputation.Get("RotChoir");
            try{Assert.IsTrue(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source,"Harvest"),player,zone).Success);}
            finally{HarvestablePart.Factory=previous;}
            Assert.Greater(Units(player,d.ItemBlueprint),0);
            Assert.AreEqual(before+(grain?0:GroveLaw.DigRepLoss),PlayerReputation.Get("RotChoir"));
        }
        [Test] public void CompletedNotesSurviveEntitySerializationWithoutDuplicatingEntries()
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Carry("Emberwheat",2);Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
            var before=RegionalSituationNotes.Read(player).ToArray();Assert.AreEqual(1,before.Length);
            using(var stream=new MemoryStream())
            {
                SaveGraphSerializer.SaveEntityBody(player,new SaveWriter(stream));stream.Position=0;
                var loaded=new Entity();SaveGraphSerializer.LoadEntityBody(loaded,new SaveReader(stream,null));
                CollectionAssert.AreEqual(before,RegionalSituationNotes.Read(loaded));
            }
        }

        [TestCase("ordinary-sack")][TestCase("wrong-instance")][TestCase("unexpected-contents")]
        public void RecoveryRequiresExactCargoWithoutErasingUnrelatedContents(string fault)
        {
            var d=RegionalSituations.Find("sumphold-oil");var b=Bind(d.Id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            var sourceZone=manager.GetZone(d.SourceZoneId);var cargo=Source(sourceZone,d.Id);
            var at=sourceZone.GetEntityCell(cargo);MovePlayer(sourceZone,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,sourceZone).Success);
            string original=cargo.ID;Entity extra=null;
            if(fault=="ordinary-sack")
            {
                Assert.IsTrue(player.GetPart<InventoryPart>().RemoveObject(cargo));
                extra=factory.CreateEntity("Sack");Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(extra));
            }
            if(fault=="wrong-instance")cargo.ID=original+"-foreign";
            if(fault=="unexpected-contents")
            {
                var container=cargo.GetPart<ContainerPart>();if(container==null){container=new ContainerPart();cargo.AddPart(container);}
                extra=factory.CreateEntity("Dagger");Assert.IsTrue(container.AddItem(extra));
            }
            StandBy(b.zone,b.recipient);int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,d.ItemBlueprint);
            Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));Assert.IsFalse(b.request.Completed);
            Assert.AreEqual(money,TradeSystem.GetDrams(player));Assert.AreEqual(stock,Units(b.recipient,d.ItemBlueprint));
            if(fault=="unexpected-contents")
            {Assert.IsTrue(cargo.GetPart<ContainerPart>().Contents.Contains(extra));cargo.RemovePart(cargo.GetPart<ContainerPart>());}
            if(fault=="ordinary-sack")Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(cargo));
            cargo.ID=original;Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
            if(fault=="ordinary-sack")Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(extra));
        }
        [TestCase("sumphold-oil")][TestCase("wellmeet-filters")]
        public void RecoveryCargoPublicPayloadSurvivesNativeEntityBodySave(string id)
        {
            var d=RegionalSituations.Find(id);var zone=manager.GetZone(d.SourceZoneId);var cargo=Source(zone,id);
            using(var stream=new MemoryStream())
            {
                SaveGraphSerializer.SaveEntityBody(cargo,new SaveWriter(stream));stream.Position=0;
                var loaded=new Entity{ID=cargo.ID};SaveGraphSerializer.LoadEntityBody(loaded,new SaveReader(stream,null));
                Assert.AreEqual(cargo.BlueprintName,loaded.BlueprintName);
                CollectionAssert.AreEquivalent(cargo.Properties,loaded.Properties);
                CollectionAssert.AreEquivalent(cargo.IntProperties,loaded.IntProperties);
                Assert.IsTrue(loaded.GetPart<PhysicsPart>().Takeable);
                Assert.IsNull(loaded.GetPart<StackerPart>(),"Marked recovery goods cannot merge away their instance identity.");
            }
        }
        [TestCase(false)][TestCase(true)]
        public void RemovingEveryHabitatOwnerCannotVacuouslyEarnPreservation(bool remove)
        {
            var d=RegionalSituations.Find("sumphold-oil");var b=Bind(d.Id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            var zone=manager.GetZone(d.SourceZoneId);var cargo=Source(zone,d.Id);
            var habitat=zone.GetAllEntities().Where(e=>e.GetPart<RegionalHabitatPart>()?.InstanceId==b.request.InstanceId).ToArray();
            Assert.Greater(habitat.Length,0,"Expected owners must exist in real generated source.");
            if(remove)foreach(var e in habitat)Assert.IsTrue(zone.RemoveEntity(e));
            var at=zone.GetEntityCell(cargo);MovePlayer(zone,at.X,at.Y);Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,zone).Success);
            StandBy(b.zone,b.recipient);int money=TradeSystem.GetDrams(player);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
            int reward=TradeSystem.GetDrams(player)-money;
            if(remove)Assert.AreEqual(d.RewardDrams,reward);else Assert.Greater(reward,d.RewardDrams);
        }

        [TestCase("sumphold-oil")][TestCase("wellmeet-filters")]
        public void FullWorldReloadRetainsCarriedCargoAndCompletionWithoutRegeneration(string id)
        {
            var d=RegionalSituations.Find(id);var b=Bind(id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            var source=manager.GetZone(d.SourceZoneId);var cargo=Source(source,id);var at=source.GetEntityCell(cargo);
            MovePlayer(source,at.X,at.Y);Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,source).Success);
            string playerId=player.ID,cargoId=cargo.ID;var notes=RegionalSituationNotes.Read(player).ToArray();
            RoundTripWorld(playerId);
            CollectionAssert.AreEqual(notes,RegionalSituationNotes.Read(player));
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==cargoId));
            Assert.IsFalse(manager.GetZone(d.SourceZoneId).GetAllEntities().Any(e=>e.ID==cargoId));
            b=Bind(id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
            int money=TradeSystem.GetDrams(player);RoundTripWorld(playerId);
            b=Bind(id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.Completed);
            Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));Assert.AreEqual(money,TradeSystem.GetDrams(player));
            RegionalSituations.OnZoneGenerated(manager.GetZone(d.SourceZoneId),manager);
            Assert.IsFalse(manager.GetZone(d.SourceZoneId).GetAllEntities().Any(e=>e.ID==cargoId));
        }
        private void RoundTripWorld(string playerId)
        {
            using(var stream=new MemoryStream())
            {
                // The session writes queued entity bodies and runs native load
                // hooks; the manager subrecord alone contains unresolved refs.
                var saved=GameSessionState.Capture("regional-adversarial", "test", manager, null, player);
                saved.Save(new SaveWriter(stream));stream.Position=0;
                var restored=GameSessionState.Load(new SaveReader(stream,factory));
                manager=restored.ZoneManager;
                Assert.AreEqual(playerId,restored.Player.ID);
            }
            playerZone=manager.CachedZones.Values.Single(z=>z.GetAllEntities().Any(e=>e.ID==playerId));
            player=playerZone.GetAllEntities().Single(e=>e.ID==playerId);
            StoryletPart.LocalPlayer=player;manager.SetActiveZone(playerZone);SettlementRuntime.ActiveZone=playerZone;
        }
        [TestCase(false)][TestCase(true)]
        public void ActualMarkedPeatDamageDisturbsHabitatAndHeatReleasesNativeGas(bool heat)
        {
            var d=RegionalSituations.Find("sumphold-oil");var zone=manager.GetZone(d.SourceZoneId);
            var habitat=zone.GetAllEntities().First(e=>e.HasPart<RegionalHabitatPart>());
            var burn=habitat.GetPart<BurnOffGasPart>();Assert.NotNull(burn);Assert.AreEqual("marsh-gas",burn.GasId);
            var at=zone.GetEntityCell(habitat);StandBy(zone,habitat);
            var gasType=typeof(GasRegistry);var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            var tableField=gasType.GetField("_byId",flags);var initializedField=gasType.GetField("_initialized",flags);
            var table=(System.Collections.IDictionary)tableField.GetValue(null);var snapshot=new System.Collections.Hashtable();
            foreach(System.Collections.DictionaryEntry item in table)snapshot[item.Key]=item.Value;
            bool was=(bool)initializedField.GetValue(null);var rng=BurnOffGasPart.TestRng;
            try
            {
                GasRegistry.InitializeFromJsonSources(UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>("Content/Data/GasDefinitions").Select(a=>a.text));
                Assert.Greater(GasRegistry.Count,0);Assert.AreEqual(100,burn.Chance,"Shipped marked peat must deterministically release its gas.");
                int prior=GasDensity(zone,at.X,at.Y);BurnOffGasPart.TestRng=new Random(64);
                var damage=new Damage(burn.DamagePer+habitat.GetPart<DestructiblePart>().Hardness+10);damage.AddAttribute(heat?"Heat":"Bludgeon");
                // Terrain has Destructible.HP, not creature Hitpoints.
                // Use the same dispatcher as native structural spell damage.
                int beforeHP=habitat.GetPart<DestructiblePart>().HP;
                DestructionSystem.RouteDamage(habitat,damage,player,zone);
                Assert.Less(habitat.GetPart<DestructiblePart>().HP,beforeHP);
                Assert.IsTrue(habitat.GetPart<RegionalHabitatPart>().Disturbed);
                if(heat)Assert.Greater(GasDensity(zone,at.X,at.Y),prior);else Assert.AreEqual(prior,GasDensity(zone,at.X,at.Y));
            }
            finally
            {
                table.Clear();foreach(System.Collections.DictionaryEntry item in snapshot)table[item.Key]=item.Value;
                initializedField.SetValue(null,was);BurnOffGasPart.TestRng=rng;
            }
        }
        private static int GasDensity(Zone zone,int x,int y)=>zone.GetCell(x,y).Objects.Sum(e=>e.GetPart<GasPoolPart>()?.Density??0);
        [TestCase(false)][TestCase(true)]
        public void KnownDestroyedRecoveryCannotKeepAdvertisingAvailableWork(bool destroy)
        {
            var d=RegionalSituations.Find("sumphold-oil");var b=Bind(d.Id);StandBy(b.zone,b.recipient);
            var source=manager.GetZone(d.SourceZoneId);var cargo=Source(source,d.Id);
            if(destroy)Assert.IsTrue(source.RemoveEntity(cargo));
            Assert.AreEqual(destroy?QuestCueState.None:QuestCueState.Available,b.request.GetCueState(player,b.zone));
            Assert.AreEqual(!destroy,b.request.TryAct(player,b.zone,"read"));
        }
        [Test] public void RecoverySourceReplacedAfterAcceptanceStillAllowsUnpaidRelease()
        {
            var d=RegionalSituations.Find("sumphold-oil");var b=Bind(d.Id);StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            var at=WorldMap.FromZoneID(d.SourceZoneId);manager.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.Village,"Changed source"));
            Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone));
            int money=TradeSystem.GetDrams(player);Assert.IsTrue(b.request.TryAct(player,b.zone,"release"));
            Assert.IsFalse(b.request.Accepted);Assert.AreEqual(money,TradeSystem.GetDrams(player));
        }
        private sealed class Probe:Part
        {
            public override string Name=>"RegionalSituationProbe";
            public string Event;public Action<GameEvent> Run;public int Calls;
            public override bool HandleEvent(GameEvent e){if(e.ID==Event){Calls++;Run?.Invoke(e);}return true;}
        }
        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); Assert.NotNull(d, id);
            var z = manager.GetZone(d.RecipientZoneId);
            var matches = z.GetAllEntities().Where(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Native recipient binding " + id);
            return (z, matches[0], matches[0].GetPart<RegionalRequestPart>());
        }

        private static Entity Source(Zone zone, string id)
        {
            string sourceId = RegionalSituations.SourceId(RegionalSituations.Find(id), Seed);
            var owners = zone.GetAllEntities().Where(e => e.ID == sourceId).ToArray();
            Assert.AreEqual(1, owners.Length, "Actual generated primary source for " + id);
            return owners[0];
        }

        private void StandBy(Zone zone, Entity owner)
        {
            var at = zone.GetEntityCell(owner); Assert.NotNull(at);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y)
                .Select(p => zone.GetCell(p.x, p.y)).FirstOrDefault(c => c != null && !c.BlocksMovement());
            Assert.NotNull(standing, "Native recipient needs a standing frontage.");
            MovePlayer(zone, standing.X, standing.Y);
        }

        private void MovePlayer(Zone zone, int x, int y)
        {
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, x, y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }

        private void Carry(string blueprint, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = factory.CreateEntity(blueprint); Assert.NotNull(item, blueprint);
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item), blueprint);
            }
        }

        private static int Units(Entity owner, string blueprint) => owner.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint && ReferenceEquals(e.GetPart<PhysicsPart>()?.InInventory, owner))
            .Sum(e => Math.Max(0, e.GetPart<StackerPart>()?.StackCount ?? 1));
    }
}
