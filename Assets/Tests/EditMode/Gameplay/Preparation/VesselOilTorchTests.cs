using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class VesselOilTorchTests : FiftySecondPreparationFixture
    {
        const string Prefix = "RefuelVesselTorch|";
        const string StoredPrefix = "RefuelStoredVesselTorch|";
        Entity Torch(float fuel = 5)
        {
            var torch = Carry("Torch");
            torch.GetPart<LightSourcePart>().Enabled = false;
            torch.GetPart<FuelPart>().FuelMass = fuel;
            return torch;
        }
        string Row(Entity oil, Entity torch) => Actions(oil).Single(a =>
            a.Command.StartsWith(Prefix, StringComparison.Ordinal) && a.Command.Split('|')[2] == Id(torch)).Command;
        InventoryAction[] Rows(Entity oil) => Actions(oil).Where(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();

        (Entity box, Entity flask) StoreOil(int volume = 2)
        {
            var box = Place("CampBox");
            box.AddPart(new InstallablePart());
            box.AddPart(new InstallationMarkerPart { Origin = "Player", InstallationId = "player:" + box.ID, RetainZone = true });
            var flask = Flask(volume, "oil");
            Assert.True(InventorySystem.PutInContainer(Actor, box, flask));
            return (box, flask);
        }

        static bool IsStoredTorchRow(InventoryAction row) => !row.IsDisabled
            && row.Command?.StartsWith(StoredPrefix, StringComparison.Ordinal) == true;

        InventoryAction StoredTorchChoice()
        {
            var rows = CellMenu(11, 10).Where(IsStoredTorchRow).ToArray();
            Assert.That(rows, Has.Length.EqualTo(1), "The installed camp box should offer one-oil refill of the carried torch.");
            return rows[0];
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OilWithNoRepresentableGainWithinTariffIsNotOffered(bool stored)
        {
            var (box, flask) = stored ? StoreOil() : (null, Flask(2, "oil"));
            var torch = Torch(268435456f);
            var fuel = torch.GetPart<FuelPart>();
            fuel.MaxFuel = 268435520f;
            Assert.IsEmpty(stored ? Actions(box).Where(IsStoredTorchRow).ToArray() : Rows(flask));
            Assert.AreEqual(2, Units(flask));
            Assert.AreEqual(268435456f, fuel.FuelMass);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AReplacedFuelPartDoesNotRefundOilWhileItStillHoldsTheCredit(bool stored)
        {
            var (box, flask) = stored ? StoreOil() : (null, Flask(2, "oil"));
            var torch = Torch();
            var original = torch.GetPart<FuelPart>();
            var replacement = new FuelPart { FuelMass = 12, MaxFuel = 50, BurnRate = 1 };
            string command = stored ? StoredTorchChoice().Command : Row(flask, torch);
            Actor.AddPart(new Probe { Run = () =>
            {
                torch.RemovePart(original);
                torch.AddPart(replacement);
            } });
            Assert.False(Act(box ?? flask, command));
            Assert.That(torch.GetPart<FuelPart>(), Is.SameAs(replacement));
            Assert.AreEqual(12, replacement.FuelMass);
            Assert.AreEqual(30, original.FuelMass);
            Assert.AreEqual(1, Units(flask));
        }

        [Test]
        public void StoredTorchOilDoesNotRefundWhenLateFuelCannotRepresentTheRetraction()
        {
            var (box, flask) = StoreOil();
            var torch = Torch();
            var fuel = torch.GetPart<FuelPart>();
            string command = StoredTorchChoice().Command;
            Actor.AddPart(new Probe { Run = () => fuel.FuelMass = 1e20f });
            Assert.False(Act(box, command));
            Assert.AreEqual(1e20f, fuel.FuelMass);
            Assert.AreEqual(1, Units(flask));
        }

        [Test]
        public void StoredTorchOilPreservesBothExactOwnersAcrossSaveAndReload()
        {
            var (box, flask) = StoreOil();
            var torch = Torch();
            string boxID = box.ID, flaskID = flask.ID, torchID = torch.ID;
            Assert.True(ChooseRow(StoredTorchChoice(), 11, 10));
            var loaded = RoundTrip();
            Actor = loaded.Player;
            Zone = loaded.ZoneManager.ActiveZone;
            SettlementRuntime.ActiveZone = Zone;
            box = Zone.GetReadOnlyEntities().Single(item => item.ID == boxID);
            flask = box.GetPart<ContainerPart>().Contents.Single();
            torch = Pack.Objects.Single(item => item.ID == torchID);
            Assert.AreEqual(flaskID, flask.ID);
            Assert.AreEqual(1, Units(flask));
            Assert.AreEqual(30, torch.GetPart<FuelPart>().FuelMass);
            Assert.False(torch.GetPart<LightSourcePart>().Enabled);
            Assert.True(ChooseRow(StoredTorchChoice(), 11, 10));
            Assert.AreEqual(0, Units(flask));
            Assert.AreEqual(50, torch.GetPart<FuelPart>().FuelMass);
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
        }

        [TestCase(5, 2, 30)]
        [TestCase(45, 1, 50)]
        public void StoredTorchOilFromTheCampBoxSpendsOneUnitAndKeepsTheExactFlask(int before, int volume, int after)
        {
            var (box, flask) = StoreOil(volume);
            var contents = box.GetPart<ContainerPart>();
            var clay = Carry("FireClay");
            Assert.True(InventorySystem.PutInContainer(Actor, box, clay));
            var torch = Torch(before);
            var untouched = Torch(2);
            var carriedOil = Flask(3, "oil");
            var row = CellMenu(11, 10).Single(choice => IsStoredTorchRow(choice) && choice.Display.Contains($"{before} to {after}/50"));
            Assert.That(row.Display, Does.Contain("1 oil"));
            Assert.That(row.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(row.Via ?? WorldInteractionSystem.ResolveTarget(Zone.GetCell(11, 10), Actor), Is.SameAs(box));

            Assert.True(ChooseRow(row, 11, 10));
            Assert.That(contents.Contents, Is.EqualTo(new[] { flask, clay }));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.AreEqual(volume - 1, Units(flask));
            Assert.AreEqual(volume == 1 ? "" : "oil", flask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(3, Units(carriedOil));
            Assert.AreEqual(after, torch.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(2, untouched.GetPart<FuelPart>().FuelMass);
            Assert.False(torch.GetPart<LightSourcePart>().Enabled);
            Assert.That(Pack.Objects, Does.Contain(torch));
            Assert.Zero(Count("LampOil"));
            Assert.False(ChooseRow(row, 11, 10));
            Assert.AreEqual(volume - 1, Units(flask));
        }

        [TestCase("locked")]
        [TestCase("flask changed")]
        [TestCase("torch changed")]
        [TestCase("flask withdrawn")]
        public void StoredTorchOilRefusesTheStaleChoiceWithoutUsingAnAlternative(string change)
        {
            var (box, flask) = StoreOil();
            var torch = Torch();
            var carriedOil = Flask(3, "oil");
            var row = StoredTorchChoice();
            switch (change)
            {
                case "locked": box.GetPart<ContainerPart>().Locked = true; break;
                case "flask changed": flask.GetPart<LiquidVesselPart>().Volume++; break;
                case "torch changed": torch.GetPart<FuelPart>().FuelMass = 6; break;
                case "flask withdrawn": Assert.True(InventorySystem.TakeFromContainer(Actor, box, flask)); break;
            }
            int tick = Clock.TickCount;

            Assert.False(Act(box, row.Command));
            Assert.AreEqual(change == "flask changed" ? 3 : 2, Units(flask));
            Assert.AreEqual(3, Units(carriedOil));
            Assert.AreEqual(change == "torch changed" ? 6 : 5, torch.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(tick, Clock.TickCount);
            var holder = change == "flask withdrawn" ? Actor : box;
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(holder));
        }

        [TestCase("unowned")]
        [TestCase("nested")]
        [TestCase("stacked flask")]
        [TestCase("duplicate liquid")]
        [TestCase("reserved flask")]
        [TestCase("reserved torch")]
        [TestCase("torch also listed in box")]
        [TestCase("torch on fire")]
        public void StoredTorchOilCannotUseUnsupportedBoxStock(string change)
        {
            var (box, flask) = StoreOil();
            var torch = Torch();
            var contents = box.GetPart<ContainerPart>();
            switch (change)
            {
                case "unowned": box.GetPart<InstallationMarkerPart>().Origin = "Generated"; break;
                case "nested":
                    var nested = Factory.CreateEntity("WovenBasket");
                    Assert.True(contents.RemoveItem(flask));
                    Assert.True(nested.GetPart<ContainerPart>().AddItem(flask));
                    Assert.True(contents.AddItem(nested));
                    break;
                case "stacked flask":
                    var stack = flask.GetPart<StackerPart>();
                    if (stack == null) flask.AddPart(stack = new StackerPart());
                    stack.StackCount = 2;
                    break;
                case "duplicate liquid": flask.AddPart(new LiquidVesselPart { Volume = 2, LiquidId = "oil" }); break;
                case "reserved flask": flask.AddPart(new LocalPorterReservationPart()); break;
                case "reserved torch": torch.AddPart(new LocalPorterReservationPart()); break;
                case "torch also listed in box": contents.Contents.Add(torch); break;
                case "torch on fire":
                    if (!torch.HasPart<StatusEffectsPart>()) torch.AddPart(new StatusEffectsPart());
                    Assert.True(torch.ApplyEffect(new BurningEffect()));
                    break;
            }

            Assert.False(CellMenu(11, 10).Any(IsStoredTorchRow));
            Assert.AreEqual(2, Units(flask));
            Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StoredTorchOilRollbackPreservesIndependentChangesAndExactCustody(bool moveFlask)
        {
            var (box, flask) = StoreOil();
            var contents = box.GetPart<ContainerPart>();
            var torch = Torch();
            var liquid = flask.GetPart<LiquidVesselPart>();
            var fuel = torch.GetPart<FuelPart>();
            var hp = Actor.GetStat("Hitpoints");
            int health = hp.Value;
            var row = StoredTorchChoice();
            string lastMessage = MessageLog.GetLast();
            var tx = new InventoryTransaction();
            var action = GameEvent.New("InventoryAction");
            var ground = Zone.GetCell(10, 12);
            try
            {
                action.SetParameter("Actor", (object)Actor);
                action.SetParameter("Zone", (object)Zone);
                action.SetParameter("Command", row.Command);
                action.SetParameter("InventoryTransaction", (object)tx);
                box.FireEvent(action);
                Assert.True(action.Handled);
                Assert.AreEqual(1, liquid.Volume);
                Assert.AreEqual(30, fuel.FuelMass);
                Assert.AreEqual(lastMessage, MessageLog.GetLast());

                if (moveFlask)
                {
                    Assert.True(contents.RemoveItem(flask));
                    Assert.True(Zone.AddEntity(flask, ground.X, ground.Y));
                }
                else
                {
                    liquid.Volume++;
                    fuel.FuelMass -= 2;
                    hp.BaseValue -= 2;
                }
                Assert.Throws<InvalidOperationException>(tx.Commit);
                Assert.False(tx.ActionCommitted);
            }
            finally
            {
                action.Release();
                tx.Rollback();
            }

            Assert.True(tx.IsRolledBack);
            Assert.AreEqual(moveFlask ? 2 : 3, liquid.Volume);
            Assert.AreEqual("oil", liquid.LiquidId);
            Assert.AreEqual(moveFlask ? 5 : 3, fuel.FuelMass);
            Assert.AreEqual(moveFlask ? health : health - 2, hp.Value);
            Assert.False(torch.GetPart<LightSourcePart>().Enabled);
            Assert.That(torch.GetPart<FuelPart>(), Is.SameAs(fuel));
            Assert.That(flask.GetPart<LiquidVesselPart>(), Is.SameAs(liquid));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(moveFlask ? null : box));
            Assert.That(Zone.GetEntityCell(flask), Is.SameAs(moveFlask ? ground : null));
            Assert.That(contents.Contents.Contains(flask), Is.EqualTo(!moveFlask));
            Assert.AreEqual(lastMessage, MessageLog.GetLast());
        }

        [TestCase(5, 30)] [TestCase(45, 50)]
        public void OneOilFeedsOnlyTheChosenTorchWithVisibleCap(float before, float after)
        {
            var oil = Flask(2, "oil"); var torch = Torch(before); var other = Torch(2);
            string row = Row(oil, torch);
            StringAssert.Contains("1 oil", Actions(oil).Single(a => a.Command == row).Display);
            Assert.True(Act(oil, row)); Assert.AreEqual(1, Units(oil));
            Assert.AreEqual(after, torch.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(2, other.GetPart<FuelPart>().FuelMass);
            Assert.False(torch.GetPart<LightSourcePart>().Enabled); Assert.AreEqual(0, Count("LampOil"));
        }
        [Test] public void LastUnitLeavesTheSameCanonicalEmptyFlaskAndReadersExplainBothFuels()
        {
            var oil = Flask(1, "oil"); var torch = Torch(); string id = oil.ID;
            Assert.True(Act(oil, Row(oil, torch))); Assert.AreEqual(id, oil.ID);
            Assert.True(Pack.Objects.Contains(oil)); Assert.AreEqual(0, Units(oil));
            Assert.AreEqual("", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.True(ItemExamineService.TryDescribeDetails(torch, out var text)); StringAssert.Contains("flask", text);
        }
        [Test] public void UsefulRefillIsReadableAndPrecedesTheManyTransferRows()
        {
            var oil = Flask(3, "oil"); var torch = Torch(48.8000031f);
            Flask(0);
            var row = Actions(oil).Single(a => a.Command == Row(oil, torch));
            StringAssert.Contains("48.8 to 50/50", row.Display);
            Assert.AreEqual(ActionTime.OneAction, row.Time);
            var transfers = Actions(oil).Where(a => a.Command.StartsWith("Transfer")).ToArray();
            Assert.IsNotEmpty(transfers);
            Assert.Greater(row.Priority, transfers.Max(a => a.Priority));
        }
        [TestCase("water")] [TestCase("bog-mire")] [TestCase("empty")]
        [TestCase("stacked")] [TestCase("duplicate")]
        public void UnsupportedSourcesNeverOfferOrSpendFuel(string kind)
        {
            var oil = Flask(kind == "empty" ? 0 : 2, kind == "water" || kind == "bog-mire" ? kind : "oil"); var torch = Torch();
            if (kind == "stacked") { var s = oil.GetPart<StackerPart>(); if (s == null) oil.AddPart(s = new StackerPart()); s.StackCount = 2; }
            if (kind == "duplicate") oil.AddPart(new LiquidVesselPart { Volume = 2, LiquidId = "oil" });
            Assert.IsEmpty(Rows(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("full")] [TestCase("lit")] [TestCase("ground")] [TestCase("stacked")]
        [TestCase("duplicate-fuel")] [TestCase("nan")]
        public void UnqualifiedTorchesNeverOfferRefilling(string kind)
        {
            var oil = Flask(2, "oil"); var torch = Torch();
            if (kind == "full") torch.GetPart<FuelPart>().FuelMass = 50;
            if (kind == "lit") torch.GetPart<LightSourcePart>().Enabled = true;
            if (kind == "ground") { Pack.RemoveObject(torch); Zone.AddEntity(torch, 10, 10); }
            if (kind == "stacked") torch.GetPart<StackerPart>().StackCount = 2;
            if (kind == "duplicate-fuel") torch.AddPart(new FuelPart());
            if (kind == "nan") torch.GetPart<FuelPart>().FuelMass = float.NaN;
            Assert.IsEmpty(Rows(oil)); Assert.AreEqual(2, Units(oil));
        }
        [Test] public void MenusArePureAndAChangedQuotedTorchRefusesWithoutSpending()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            for (int i = 0; i < 4; i++) Assert.AreEqual(command, Row(oil, torch));
            torch.GetPart<FuelPart>().FuelMass = 6;
            Assert.False(Act(oil, command)); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(6, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void BeforeCallbackChangedSourceCannotRedeemOldQuote()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Event = "BeforeInventoryAction", Run = () => oil.GetPart<LiquidVesselPart>().Volume = 1 });
            Assert.False(Act(oil, command)); Assert.AreEqual(1, Units(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void FailedOuterCallbackRestoresOnlyPaymentAndPreservesInjury()
        {
            var oil = Flask(1, "oil"); var torch = Torch(); string command = Row(oil, torch); int hp = Actor.GetStat("Hitpoints").Value;
            Actor.AddPart(new Probe { Run = () => Actor.GetStat("Hitpoints").BaseValue -= 2, Fail = true });
            Assert.False(Act(oil, command)); Assert.AreEqual(1, Units(oil)); Assert.AreEqual("oil", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass); Assert.AreEqual(hp - 2, Actor.GetStat("Hitpoints").Value);
        }
        [Test] public void IndependentFuelAndOilChangesSurviveFailedCommit()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { oil.GetPart<LiquidVesselPart>().Volume++; torch.GetPart<FuelPart>().FuelMass -= 2; } });
            Assert.False(Act(oil, command)); Assert.AreEqual(3, Units(oil)); Assert.AreEqual(3, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReplacedLiquidStateIsPreservedWhileFuelIsUndone()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { oil.GetPart<LiquidVesselPart>().LiquidId = "water"; oil.GetPart<LiquidVesselPart>().Volume = 7; } });
            Assert.False(Act(oil, command)); Assert.AreEqual(7, Units(oil)); Assert.AreEqual("water", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ForcedDropRemainsDroppedAfterRollback()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { Pack.RemoveObject(oil); Zone.AddEntity(oil, 10, 10); } });
            Assert.False(Act(oil, command)); Assert.False(Pack.Objects.Contains(oil));
            Assert.True(Zone.GetCell(10, 10).Objects.Contains(oil)); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReentrantRefillCannotUseClaimedOwners()
        {
            var oil = Flask(3, "oil"); var torch = Torch(); string command = Row(oil, torch); bool nested = true;
            Actor.AddPart(new Probe { Run = () => nested = Act(oil, Row(oil, torch)) });
            Assert.True(Act(oil, command)); Assert.False(nested); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(30, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void SavedOilAndFuelRemainFiniteAndCanContinueAfterGraphReload()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string oilID = oil.ID, torchID = torch.ID;
            Assert.True(Act(oil, Row(oil, torch))); var loaded = RoundTrip();
            Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            oil = Pack.Objects.Single(e => e.ID == oilID); torch = Pack.Objects.Single(e => e.ID == torchID);
            Assert.AreEqual(1, Units(oil)); Assert.AreEqual(30, torch.GetPart<FuelPart>().FuelMass);
            Assert.True(Act(oil, Row(oil, torch))); Assert.AreEqual(0, Units(oil)); Assert.AreEqual(50, torch.GetPart<FuelPart>().FuelMass);
        }
        public sealed class Probe : Part
        {
            public string Event = "AfterInventoryAction"; public Action Run; public bool Fail; bool done;
            public override bool HandleEvent(GameEvent e)
            {
                if (!done && e.ID == Event) { done = true; Run?.Invoke(); if (Fail) throw new InvalidOperationException("oil fuel rollback probe"); }
                return true;
            }
        }
    }
}
