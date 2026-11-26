using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Existing ordinary actions keep this first RED batch compile-safe before Parts exist.
    public abstract class FiftySecondPreparationFixture : FiftyWorldFixture
    {
        EntityFactory oldForge, oldStill;
        readonly List<Action> restores = new List<Action>();
        [SetUp] public void PreparationSetup()
        {
            oldForge = ForgePart.Factory; oldStill = AlchemyStillPart.Factory;
            ForgePart.Factory = AlchemyStillPart.Factory = Factory;
            SnapshotStatics(typeof(EnhancementFactory)); SnapshotStatics(typeof(BrewRuleRegistry)); SnapshotStatics(typeof(RepairRecipeRegistry));
            EnhancementFactory.ForceReinitialize(); BrewRuleRegistry.EnsureInitialized(); RepairRecipeRegistry.LoadDefaults();
            foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
        }
        [TearDown] public void PreparationTeardown()
        {
            ForgePart.Factory = oldForge; AlchemyStillPart.Factory = oldStill;
            for (int i = restores.Count - 1; i >= 0; i--) restores[i](); restores.Clear();
        }
        void SnapshotStatics(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.IsLiteral) continue;
                var value = field.GetValue(null);
                if (value is IDictionary dictionary)
                {
                    var rows = dictionary.Keys.Cast<object>().Select(k => (k, dictionary[k])).ToArray();
                    restores.Add(() => { dictionary.Clear(); foreach (var row in rows) dictionary.Add(row.k, row.Item2); });
                }
                else if (value is IList list)
                {
                    var rows = list.Cast<object>().ToArray(); restores.Add(() => { list.Clear(); foreach (var row in rows) list.Add(row); });
                }
                else if (!field.IsInitOnly) restores.Add(() => field.SetValue(null, value));
            }
        }
        protected Entity Stock(string bp, int count = 1)
        { var e = Carry(bp); if (e.GetPart<StackerPart>() is StackerPart s) s.StackCount = count; else Assert.AreEqual(1, count); return e; }
        protected Entity Forge() => Place("TinkersForge", 9, 10);
        protected Entity Still() => Place("AlchemyStill", 9, 10);
        protected Entity Weapon()
        {
            Assert.True(WeaponForgingService.TryForge(Actor, Factory, Stock("SteelBladeComponent"), Stock("OakHaftComponent"),
                Stock("LeatherBindingComponent"), out var weapon, out var reason), reason); return weapon;
        }
        protected Entity Plant(int stage = 0, int x = 11, int y = 10)
        {
            Bed(x, y); var e = Place("KnotflaxCrop", x, y); e.GetPart<CropPart>().GrowthStage = stage;
            Assert.True(CropTime.Reconcile(e.GetPart<CropPart>(), Zone, Clock.TickCount)); return e;
        }
        protected bool Prepare(Entity input, string recipe) => Act(input, "PrepareRecipe|" + recipe);
        protected bool Refuel(Entity oil, Entity torch) => Act(oil, "RefuelTorch|" + Id(torch));
        protected bool Infuse(Entity mineral, Entity target) => Act(mineral, "InfuseMineral|" + Id(target));
        protected bool Compost(Entity crop, Entity sludge) => Act(crop, "CompostCrop|" + Id(sludge));
        protected bool Transplant(Entity crop, int x, int y) => Act(crop, "TransplantCrop|" + x + "|" + y);
        protected int PackCount(string bp) => Pack.Objects.Where(e => e.BlueprintName == bp).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        protected Entity Packed(string bp) => Pack.Objects.Single(e => e.BlueprintName == bp);
        protected bool Composted(Entity crop)
        { var field = typeof(CropPart).GetField("Composted"); Assert.NotNull(field, "Missing saved one-time compost state"); return (bool)field.GetValue(crop.GetPart<CropPart>()); }
        protected GameSessionState RoundTrip()
        {
            var manager = new OverworldZoneManager(null, 812);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(Clock.TickCount, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "preparation-audit", manager, Clock, Actor));
        }
        protected int Intake()
        { var e = GameEvent.New("GetRespiratoryPerformance"); e.SetParameter("Intake", (object)100); Actor.FireEvent(e); int value = e.GetParameter<int>("Intake"); e.Release(); return value; }
        protected int BeforeDamage(string type)
        { var d = new Damage(10); if (type != null) d.AddAttribute(type); var e = GameEvent.New("BeforeTakeDamage"); e.SetParameter("Damage", (object)d); Actor.FireEvent(e); e.Release(); return d.Amount; }
        protected int Hurt(string type, int amount = 10)
        {
            var hp = Actor.GetStat("Hitpoints"); hp.Max = 200; hp.BaseValue = 200; hp.Penalty = 0;
            var damage = new Damage(amount); if (type != null) damage.AddAttribute(type);
            CombatSystem.ApplyDamage(Actor, damage, null, null); return 200 - hp.Value;
        }
    }

    public sealed class FiftySecondPreparationTests : FiftySecondPreparationFixture
    {
        [TestCase("absent", "forge-required")]
        [TestCase("faulted", "forge-needs-repair")]
        [TestCase("unseen fault", "forge-required")]
        [TestCase("hidden owner", "forge-required")]
        public void UnavailableForgeRecoveryExplainsWhatToDoAndKeepsFieldCutting(string nearby, string code)
        {
            var weapon = Weapon();
            var cutter = Stock("Dagger");
            if (nearby != "absent")
            {
                var rig = Place("AbandonedFieldWorkRig");
                Zone.GetEntityCell(rig).IsVisible = nearby != "unseen fault";
                rig.GetPart<RenderPart>().Visible = nearby != "hidden owner";
            }
            int now = Clock.TickCount;
            var rows = Actions(weapon);
            var recovery = rows.Single(row => row.Name == "SalvageForgedWeapon");
            Assert.IsTrue(recovery.IsDisabled);
            Assert.AreEqual(code, recovery.DisabledCode);
            StringAssert.Contains(nearby == "faulted" ? "needs repair" : "working forge", recovery.DisabledReason);
            Assert.That(rows.Single(row => IsFieldRecovery(row.Command)).IsDisabled, Is.False);
            Assert.False(Act(weapon, recovery.Command));
            Assert.AreEqual(now, Clock.TickCount);
            CollectionAssert.AreEquivalent(new[] { weapon, cutter }, Pack.Objects);
        }

        [Test]
        public void ForgeRecoveryTracksAReadyRigAndRefusesWhenItMovesOutOfReach()
        {
            var weapon = Weapon();
            var rig = Place("AbandonedFieldWorkRig");
            var recovery = Actions(weapon).Single(row => row.Name == "SalvageForgedWeapon");
            Assert.That(recovery.IsDisabled, Is.True);

            Stock("PreparedLiningClay", 2);
            Assert.That(rig.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.True);
            recovery = Actions(weapon).Single(row => row.Name == "SalvageForgedWeapon");
            Assert.That(recovery.IsDisabled, Is.False);
            Assert.That(Zone.MoveEntity(Actor, 13, 10), Is.True);
            Assert.That(Act(weapon, recovery.Command), Is.False);
            Assert.That(Actions(weapon).Single(row => row.Name == "SalvageForgedWeapon").DisabledCode, Is.EqualTo("forge-required"));
            Assert.That(Pack.Objects.Single(), Is.SameAs(weapon));

            Assert.That(Zone.MoveEntity(Actor, 10, 10), Is.True);
            Assert.That(Act(weapon, Actions(weapon).Single(row => row.Name == "SalvageForgedWeapon").Command), Is.True);
            Assert.AreEqual(1, PackCount("SteelBladeComponent"));
            Assert.AreEqual(1, PackCount("OakHaftComponent"));
        }

        [Test]
        public void AWorkingForgeKeepsRecoveryAvailableBesideAFaultyRig()
        {
            var weapon = Weapon();
            Place("AbandonedFieldWorkRig", 9, 10);
            Place("TinkersForge", 11, 10);
            Assert.That(Actions(weapon).Single(row => row.Name == "SalvageForgedWeapon").IsDisabled, Is.False);
        }

        [Test] public void SalvageReturnsRecordedHeadAndHaftButNotBindingOrWeapon()
        {
            Forge(); var weapon = Weapon(); Assert.True(Act(weapon, "SalvageForgedWeapon"));
            Assert.AreEqual(1, PackCount("SteelBladeComponent")); Assert.AreEqual(1, PackCount("OakHaftComponent"));
            Assert.AreEqual(0, PackCount("LeatherBindingComponent")); Assert.AreEqual(0, PackCount("ForgedWeapon"));
            Assert.False(Act(weapon, "SalvageForgedWeapon")); Assert.AreEqual(1, PackCount("SteelBladeComponent"));
        }
        [Test] public void SalvageUsesPersistedAssemblyAfterReplacementSave()
        {
            Forge(); var weapon = Weapon(); string id = weapon.ID; var loaded = RoundTrip();
            Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            var saved = Pack.Objects.Single(e => e.ID == id); Assert.AreNotSame(weapon, saved);
            Assert.True(Act(saved, "SalvageForgedWeapon")); Assert.AreEqual(1, PackCount("SteelBladeComponent")); Assert.AreEqual(1, PackCount("OakHaftComponent"));
        }

        [TestCase("LeatherBindingComponent", "OakHaftComponent")]
        [TestCase("PlainCordBindingComponent", "FieldHaftComponent")]
        public void ACarriedDaggerRecoversTheRecordedPeatMalletHeadAwayFromAForge(string binding, string haft)
        {
            const string headBlueprint = "PeatMalletHeadComponent";
            var head = Stock(headBlueprint);
            var expected = head.GetPart<WeaponComponentPart>();
            Assert.True(WeaponForgingService.TryForge(Actor, Factory, head, Stock(haft),
                Stock(binding), out var weapon, out var reason), reason);
            Assert.AreEqual(headBlueprint, weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            var dagger = Stock("Dagger");
            var daggerParts = dagger.Parts.ToArray();
            Assert.NotNull(dagger.GetPart<CuttingToolPart>());
            Assert.AreEqual(0, PackCount(headBlueprint));
            var remainder = Pack.Objects.Where(item => item != weapon).ToArray();

            Assert.False(ForgePart.IsNearForge(Actor, Zone));
            Assert.False(Act(weapon, "SalvageForgedWeapon"));
            var rows = Actions(weapon);
            var cut = rows.Single(row => row.Command.StartsWith("CutBindingForHead|", StringComparison.Ordinal));
            Assert.AreEqual(ActionTime.OneAction, cut.Time);
            StringAssert.Contains("lose haft, binding, mods", cut.Display);
            Assert.True(Act(weapon, cut.Command));

            var recovered = Packed(headBlueprint);
            var component = recovered.GetPart<WeaponComponentPart>();
            Assert.AreNotSame(head, recovered);
            Assert.AreEqual("Blade", component.Slot);
            Assert.AreEqual(expected.BaseDamage, component.BaseDamage);
            Assert.AreEqual(expected.PenBonus, component.PenBonus);
            Assert.AreEqual(expected.HitBonus, component.HitBonus);
            Assert.AreEqual(expected.Attributes, component.Attributes);
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(recovered));
            Assert.IsNull(weapon.GetPart<PhysicsPart>().InInventory);
            CollectionAssert.AreEquivalent(remainder.Append(recovered).ToArray(), Pack.Objects);
            CollectionAssert.AreEqual(daggerParts, dagger.Parts);
            Assert.AreSame(Actor, dagger.GetPart<PhysicsPart>().InInventory);
            Assert.False(Act(weapon, cut.Command));
            Assert.AreEqual(1, PackCount(headBlueprint));
        }

        [Test]
        public void AnAdvancedLashingStillNeedsTheExistingForgeRecovery()
        {
            Assert.True(WeaponForgingService.TryForge(Actor, Factory, Stock("PeatMalletHeadComponent"),
                Stock("FieldHaftComponent"), Stock("GuardLashingComponent"), out var weapon, out var reason), reason);
            Stock("Dagger");
            Assert.IsFalse(Actions(weapon).Any(row => IsFieldRecovery(row.Command)));

            Forge();
            Assert.IsTrue(Act(weapon, "SalvageForgedWeapon"));
            Assert.AreEqual(1, PackCount("PeatMalletHeadComponent"));
            Assert.AreEqual(1, PackCount("FieldHaftComponent"));
            Assert.AreEqual(0, PackCount("GuardLashingComponent"));
        }

        [Test]
        public void FieldHeadRecoveryDoesNotSubstituteADifferentCutterAfterTheQuote()
        {
            var weapon = Weapon();
            Assert.False(Actions(weapon).Any(row => IsFieldRecovery(row.Command)));
            var dagger = Stock("Dagger");
            var quote = Actions(weapon).Single(row => IsFieldRecovery(row.Command));
            Pack.RemoveObject(dagger);
            var otherDagger = Stock("Dagger");
            Assert.AreNotSame(dagger, otherDagger);
            Assert.False(Act(weapon, quote.Command));
            Assert.True(Pack.Objects.Contains(weapon));
            Assert.AreEqual(0, PackCount("SteelBladeComponent"));
            Assert.True(Act(weapon, Actions(weapon).Single(row => IsFieldRecovery(row.Command)).Command));
        }

        [Test]
        public void FieldHeadRecoveryRollsBackAnOutputMergeWhenTheExactCutterIsDamagedLate()
        {
            var weapon = Weapon();
            var existingHead = Stock("SteelBladeComponent");
            var dagger = Stock("Dagger");
            Actor.AddPart(new BreakFieldCutter { Cutter = dagger });
            Assert.False(Act(weapon, Actions(weapon).Single(row => IsFieldRecovery(row.Command)).Command));
            Assert.AreEqual(1, PackCount("SteelBladeComponent"));
            Assert.AreSame(existingHead, Packed("SteelBladeComponent"));
            Assert.AreSame(Actor, weapon.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(0, dagger.GetStatValue("Hitpoints"));
        }

        [Test]
        public void FieldHeadRecoveryReturnsAnIndependentlyInjuredWeaponWithoutHealingIt()
        {
            var weapon = Weapon();
            Stock("Dagger");
            int injured = weapon.GetStatValue("Hitpoints") - 1;
            Actor.AddPart(new BreakFieldCutter { Cutter = weapon, Hitpoints = injured });
            Assert.False(Act(weapon, Actions(weapon).Single(row => IsFieldRecovery(row.Command)).Command));
            Assert.AreSame(Actor, weapon.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(injured, weapon.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, PackCount("SteelBladeComponent"));
        }

        static bool IsFieldRecovery(string command) => command.StartsWith("CutBindingForHead|", StringComparison.Ordinal);

        sealed class BreakFieldCutter : Part
        {
            internal Entity Cutter;
            internal int Hitpoints;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && IsFieldRecovery(e.GetStringParameter("Command")))
                    Cutter.GetStat("Hitpoints").BaseValue = Hitpoints;
                return true;
            }
        }

        [TestCase("SalvagedTimber", "shape_haft", "FieldHaftComponent")]
        [TestCase("KnotflaxCord", "braid_binding", "PlainCordBindingComponent")]
        [TestCase("Tepuibone", "shape_head", "TepuiboneHeadComponent")]
        public void ShapeOneRealMaterialUnitIntoItsExactAffordableComponent(string input, string recipe, string output)
        {
            var raw = Stock(input, 2); Assert.True(Actions(raw).Any(a => a.Command == "PrepareRecipe|" + recipe));
            Assert.True(Prepare(raw, recipe)); Assert.AreEqual(1, PackCount(input)); Assert.AreEqual(1, PackCount(output));
            Assert.LessOrEqual(Packed(output).GetPart<CommercePart>().Value, raw.GetPart<CommercePart>().Value);
            Assert.LessOrEqual(InventoryPart.GetItemWeight(Packed(output)), InventoryPart.GetItemWeight(raw));
        }
        [Test] public void ThreePreparedPartsForgeAnActualModestCudgel()
        {
            Assert.True(Prepare(Stock("SalvagedTimber"), "shape_haft")); Assert.True(Prepare(Stock("KnotflaxCord"), "braid_binding")); Assert.True(Prepare(Stock("Tepuibone"), "shape_head"));
            Assert.True(WeaponForgingService.TryForge(Actor, Factory, Packed("TepuiboneHeadComponent"), Packed("FieldHaftComponent"), Packed("PlainCordBindingComponent"), out var item, out var reason), reason);
            var melee = item.GetPart<MeleeWeaponPart>(); Assert.AreEqual("1d3", melee.BaseDamage); Assert.AreEqual(-1, melee.PenBonus); Assert.AreEqual(1, melee.MaxStrengthBonus); StringAssert.Contains("Cudgel", melee.Attributes);
            Assert.AreEqual(0, PackCount("TepuiboneHeadComponent")); Assert.AreEqual(0, PackCount("FieldHaftComponent"));
        }
        [Test] public void BracedHaftTradesOneHitForTwoMoreStrengthContribution()
        {
            var head = Stock("SteelBladeComponent").GetPart<WeaponComponentPart>(); var binding = Stock("LeatherBindingComponent").GetPart<WeaponComponentPart>();
            var oak = WeaponForgingService.PreviewForge(head, Stock("OakHaftComponent").GetPart<WeaponComponentPart>(), binding);
            var braced = WeaponForgingService.PreviewForge(head, Stock("BracedHaftComponent").GetPart<WeaponComponentPart>(), binding);
            Assert.AreEqual(3, oak.MaxStrengthBonus); Assert.AreEqual(5, braced.MaxStrengthBonus); Assert.AreEqual(oak.HitBonus - 1, braced.HitBonus);
        }
        [Test] public void GuardLashingTradesOnePenetrationForOneHitOverLeather()
        {
            var head = Stock("SteelBladeComponent").GetPart<WeaponComponentPart>(); var haft = Stock("OakHaftComponent").GetPart<WeaponComponentPart>();
            var leather = WeaponForgingService.PreviewForge(head, haft, Stock("LeatherBindingComponent").GetPart<WeaponComponentPart>());
            var guard = WeaponForgingService.PreviewForge(head, haft, Stock("GuardLashingComponent").GetPart<WeaponComponentPart>());
            Assert.AreEqual(leather.HitBonus + 1, guard.HitBonus); Assert.AreEqual(leather.PenBonus - 1, guard.PenBonus); Assert.AreEqual(leather.BaseDamage, guard.BaseDamage);
        }
        [Test] public void EquippedFilterHoodReducesIntakeAndGasOnlyUntilUnequipped()
        {
            var hood = Stock("FilterHood"); Assert.AreEqual(100, Intake()); Assert.True(InventorySystem.Equip(Actor, hood));
            Assert.AreEqual(50, Intake()); Assert.AreEqual(9, BeforeDamage("Gas")); Assert.AreEqual(10, BeforeDamage(null));
            Assert.True(InventorySystem.UnequipItem(Actor, hood)); Assert.AreEqual(100, Intake()); Assert.AreEqual(10, BeforeDamage("Gas"));
        }
        [TestCase("AcidworkerApron", "Acid", "Heat", "AcidResistance")]
        [TestCase("ColdwardCloak", "Cold", "Acid", "ColdResistance")]
        public void RegionalProtectionIsTypedAndStillHasADamageFloor(string bp, string type, string other, string stat)
        {
            var gear = Stock(bp); Assert.True(InventorySystem.Equip(Actor, gear)); Assert.AreEqual(50, Actor.GetStatValue(stat));
            Assert.AreEqual(5, Hurt(type)); Assert.AreEqual(10, Hurt(other)); Assert.AreEqual(10, Hurt(null)); Assert.AreEqual(1, Hurt(type, 1));
            Assert.AreEqual(-1, gear.GetPart<ArmorPart>().DV); Assert.True(InventorySystem.UnequipItem(Actor, gear)); Assert.AreEqual(0, Actor.GetStatValue(stat));
        }
        [TestCase("FilterHood")] [TestCase("AcidworkerApron")] [TestCase("ColdwardCloak")]
        public void NewClothingActuallyAdmitsFiniteLeatherRepair(string bp)
        {
            var gear = Stock(bp); int av = gear.GetPart<ArmorPart>().AV; Assert.True(gear.GetPart<CompositionPart>().Contains("Leather"));
            Assert.True(gear.ApplyEffect(new BrokenEffect())); Assert.True(gear.HasEffect<BrokenEffect>()); Stock("LeatherBindingComponent");
            Assert.True(Act(gear, RepairablePart.RepairCommand)); Assert.False(gear.HasEffect<BrokenEffect>()); Assert.AreEqual(av, gear.GetPart<ArmorPart>().AV); Assert.AreEqual(0, PackCount("LeatherBindingComponent"));
        }
        [TestCase(0, 25)] [TestCase(40, 50)]
        public void OilRefillsOneUnlitTorchWithExactCappedFuel(float before, float expected)
        {
            var torch = Stock("Torch"); torch.GetPart<LightSourcePart>().Enabled = false; torch.GetPart<FuelPart>().FuelMass = before;
            var oil = Stock("LampOil", 2); Assert.True(Actions(oil).Any(a => a.Command == "RefuelTorch|" + Id(torch)));
            Assert.True(Refuel(oil, torch)); Assert.AreEqual(expected, torch.GetPart<FuelPart>().FuelMass); Assert.AreEqual(1, PackCount("LampOil")); Assert.False(torch.GetPart<LightSourcePart>().Enabled);
        }
        [Test] public void CordBandageStopsOnlyBleedingAndDoesNotHeal()
        {
            Assert.True(Prepare(Stock("KnotflaxCord"), "bandage")); var bandage = Packed("KnotflaxBandage");
            Assert.True(Actor.ApplyEffect(new BleedingEffect())); Assert.True(Actor.ApplyEffect(new PoisonedEffect()));
            int hp = Actor.GetStatValue("Hitpoints"); Assert.True(Act(bandage, "ApplyTonic"));
            Assert.False(Actor.HasEffect<BleedingEffect>()); Assert.True(Actor.HasEffect<PoisonedEffect>()); Assert.AreEqual(hp, Actor.GetStatValue("Hitpoints")); Assert.AreEqual(0, PackCount("KnotflaxBandage"));
        }
        [Test] public void TwoMendleafBecomeOneRealTwoDieHealingBrew()
        {
            Still(); Assert.True(Prepare(Stock("MendleafSprig", 3), "concentrate_mendleaf")); Assert.AreEqual(1, PackCount("MendleafSprig"));
            Assert.True(BrewingService.TryBrew(Actor, Factory, new[] { Packed("ConcentratedMendleaf") }, out var brew, out _, out var reason), reason);
            Assert.AreEqual("2d4", brew.GetPart<TonicPart>().Healing); Assert.AreEqual(0, PackCount("ConcentratedMendleaf"));
        }
        [Test] public void CleansedGrovePulpLosesToxicAndMakesActualTwoDieHealing()
        {
            Still(); var raw = Stock("GroveRed", 2); Assert.True(Prepare(raw, "detox_grove_red")); Assert.AreEqual(1, PackCount("GroveRed"));
            var pulp = Packed("CleansedGrovePulp"); StringAssert.DoesNotContain("toxic", pulp.GetPart<ReagentPart>().PropertiesRaw);
            Assert.True(BrewingService.TryBrew(Actor, Factory, new[] { pulp }, out var brew, out _, out var reason), reason); Assert.AreEqual("2d4", brew.GetPart<TonicPart>().Healing);
            Assert.True(BrewingService.TryBrew(Actor, Factory, new[] { raw }, out var toxic, out _, out reason), reason); Assert.True(string.IsNullOrEmpty(toxic.GetPart<TonicPart>()?.Healing));
        }
        [Test] public void CompostConsumesSludgeOnceAndShortensActualMoistGrowthWithoutWater()
        {
            var crop = Plant(); var p = crop.GetPart<CropPart>(); int original = p.TicksPerStage; var sludge = Stock("InertSludge", 2);
            Assert.True(Compost(crop, sludge)); Assert.AreEqual((original * 3 + 3) / 4, p.TicksPerStage); Assert.True(Composted(crop)); Assert.AreEqual(1, PackCount("InertSludge")); Assert.AreEqual(0, p.MoistureTicks);
            CropSystem.OnTickEnd(Zone); Assert.AreEqual(0, p.TicksInStage); Assert.False(Compost(crop, sludge));
            p.MoistureTicks = p.TicksPerStage * 2; for (int i = 0; i < p.TicksPerStage * 2; i++) CropSystem.OnTickEnd(Zone);
            Assert.AreEqual(2, p.GrowthStage); Assert.AreEqual(1, PackCount("InertSludge"));
        }
        [Test] public void SeedFocusedHarvestSpendsThePlantForTripleSeedsAndNoProduce()
        {
            var crop = Plant(2); Assert.True(Actions(crop).Any(a => a.Command == "HarvestCropSeeds")); Assert.True(Act(crop, "HarvestCropSeeds"));
            Assert.AreEqual(3, Count("KnotflaxSeed")); Assert.AreEqual(0, Count("KnotflaxCord")); Assert.IsNull(Zone.GetEntityCell(crop)); Assert.True(CultivatedSoilPart.IsCultivated(Zone, Zone.GetCell(11, 10)));
            Assert.False(Act(crop, "HarvestCropSeeds")); Assert.AreEqual(3, Count("KnotflaxSeed"));
        }
        [Test] public void TransplantMovesTheExactYoungOwnerWithAllGrowthState()
        {
            var crop = Plant(1); Bed(10, 11); var p = crop.GetPart<CropPart>(); p.TicksInStage = 3; p.MoistureTicks = 7; p.GrowthWetTickRemainder = 4;
            string id = crop.ID; int last = p.LastGrowthWorldTick; Assert.True(Transplant(crop, 10, 11));
            Assert.AreSame(crop, Zone.GetCell(10, 11).Objects.Single(e => e.HasPart<CropPart>())); Assert.AreEqual(id, crop.ID); Assert.AreEqual(1, p.GrowthStage); Assert.AreEqual(3, p.TicksInStage); Assert.AreEqual(7, p.MoistureTicks); Assert.AreEqual(4, p.GrowthWetTickRemainder); Assert.AreEqual(last, p.LastGrowthWorldTick);
            Assert.False(Zone.GetCell(11, 10).HasObjectWithPart<CropPart>());
        }
        [TestCase("PaleSalt", "EnhancementPaleSalt")] [TestCase("ChoirIron", "EnhancementChoirIron")] [TestCase("GlowQuartz", "EnhancementGlowQuartz")]
        public void PhysicalMineralInfusionSpendsOneUnitWithoutDeveloperBits(string mineralName, string enhancement)
        {
            Forge(); Assert.False(Actor.HasPart<BitLockerPart>()); var target = Stock("Dagger"); var mineral = Stock(mineralName, 2);
            Assert.True(Actions(mineral).Any(a => a.Command == "InfuseMineral|" + Id(target))); Assert.True(Infuse(mineral, target));
            Assert.NotNull(target.GetPart(enhancement)); Assert.AreEqual(1, PackCount(mineralName)); Assert.False(Actor.HasPart<BitLockerPart>());
            Assert.False(Infuse(mineral, target)); Assert.AreEqual(1, PackCount(mineralName)); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(target));
        }
        [Test] public void PreparationStateAndMineralEnhancementSurviveReplacementSave()
        {
            Forge(); var crop = Plant(); Assert.True(Compost(crop, Stock("InertSludge"))); var item = Stock("Dagger"); Assert.True(Infuse(Stock("PaleSalt"), item));
            var loaded = RoundTrip(); var savedCrop = loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e => e.ID == crop.ID);
            Assert.True(Composted(savedCrop)); Assert.AreEqual(crop.GetPart<CropPart>().TicksPerStage, savedCrop.GetPart<CropPart>().TicksPerStage);
            var savedItem = loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == item.ID); Assert.NotNull(savedItem.GetPart<EnhancementPaleSalt>()); Assert.AreEqual(0, loaded.Player.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "PaleSalt"));
        }
    }
}
