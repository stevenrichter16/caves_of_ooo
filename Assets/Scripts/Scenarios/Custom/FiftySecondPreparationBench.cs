using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Data;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Detached authored-item/command/owner-clock/save witness. Fixed supplies,
    /// crop stages and stations do not claim ordinary discovery, keyboard input or balance.</summary>
    [Scenario(name: "Second Preparation Audit", category: "Crafting", description: "Finite preparation, equipment choices, crop work and mineral infusions.")]
    public sealed class FiftySecondPreparationBench : IScenario
    {
        public const int ExpectedCases = 18;
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public readonly List<object> Observations = new List<object>();
        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear(); Observations.Clear();
            using (var scope = new DetachedScope())
            {
                ForgePart.Factory = AlchemyStillPart.Factory = SeedPart.Factory = CropSystem.Factory = ctx.Factory;
                EnhancementFactory.ForceReinitialize(); BrewRuleRegistry.EnsureInitialized();
                var zone = new Zone("Overworld.0.0.0"); SettlementRuntime.ActiveZone = zone;
                foreach (var cell in zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
                var actor = ctx.Factory.CreateEntity("Player"); var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain);
                actor.GetStat("Speed").BaseValue = 100; var pack = actor.GetPart<InventoryPart>(); pack.MaxWeight = 1000;
                Require(zone.AddEntity(actor, 10, 10), "actor placement");
                var turns = new TurnManager(); turns.AddEntity(actor); Require(turns.ProcessUntilPlayerTurn() == actor, "owner scheduling");
                MessageLog.TickProvider = () => turns.TickCount;
                Entity Carry(string bp, int count = 1) { var e = ctx.Factory.CreateEntity(bp); Require(e != null, bp); if (e.GetPart<StackerPart>() is StackerPart s) s.StackCount = count; Require(pack.AddObject(e), "carry " + bp); return e; }
                Entity Place(string bp, int x, int y) { var e = ctx.Factory.CreateEntity(bp); Require(e != null && zone.AddEntity(e, x, y), "place " + bp); return e; }
                Entity Packed(string bp) => pack.Objects.Single(e => e.BlueprintName == bp);
                int Count(string bp) => pack.Objects.Where(e => e.BlueprintName == bp).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
                bool Act(Entity item, string command)
                {
                    Require(PreparationActions.IsCommand(command), "paid command classification");
                    int before = turns.TickCount; bool handled = InventorySystem.PerformAction(actor, item, command, zone);
                    if (handled) { turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "retained owner"); Require(turns.TickCount == before + 10, "one paid ordinary owner action"); }
                    else Require(turns.TickCount == before, "refusal spends no action");
                    return handled;
                }
                string Id(Entity e) => Uri.EscapeDataString(e.ID);
                int Intake() { var e = GameEvent.New("GetRespiratoryPerformance"); e.SetParameter("Intake", (object)100); actor.FireEvent(e); int n = e.GetParameter<int>("Intake"); e.Release(); return n; }
                int Hurt(string type) { var hp = actor.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 100; hp.Penalty = 0; var d = new Damage(10); d.AddAttribute(type); CombatSystem.ApplyDamage(actor, d, null, zone); return 100 - hp.Value; }
                Place("TinkersForge", 9, 10); Place("AlchemyStill", 9, 11);
                bool prepared = Act(Carry("SalvagedTimber"), "PrepareRecipe|shape_haft") && Act(Carry("KnotflaxCord"), "PrepareRecipe|braid_binding") && Act(Carry("Tepuibone"), "PrepareRecipe|shape_head");
                Require(prepared, "three physical recipes");
                bool forged = WeaponForgingService.TryForge(actor, ctx.Factory, Packed("TepuiboneHeadComponent"), Packed("FieldHaftComponent"), Packed("PlainCordBindingComponent"), out var weapon, out var reason);
                Require(forged, reason); turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "forge owner action");
                Check("17_materials_make_native_modest_assembly", weapon.GetPart<MeleeWeaponPart>().BaseDamage == "1d3" && weapon.GetPart<MeleeWeaponPart>().MaxStrengthBonus == 1 && Count("Tepuibone") == 0,
                    new { weapon = weapon.ID, damage = weapon.GetPart<MeleeWeaponPart>().BaseDamage, strengthCap = weapon.GetPart<MeleeWeaponPart>().MaxStrengthBonus });
                Check("16_salvage_recovers_only_recorded_head_and_haft", Act(weapon, "SalvageForgedWeapon") && Count("TepuiboneHeadComponent") == 1 && Count("FieldHaftComponent") == 1 && Count("PlainCordBindingComponent") == 0 && Count("ForgedWeapon") == 0,
                    new { head = Count("TepuiboneHeadComponent"), haft = Count("FieldHaftComponent"), binding = Count("PlainCordBindingComponent") });
                var head = Carry("SteelBladeComponent").GetPart<WeaponComponentPart>(); var oak = Carry("OakHaftComponent").GetPart<WeaponComponentPart>(); var leather = Carry("LeatherBindingComponent").GetPart<WeaponComponentPart>();
                var normal = WeaponForgingService.PreviewForge(head, oak, leather); var braced = WeaponForgingService.PreviewForge(head, Carry("BracedHaftComponent").GetPart<WeaponComponentPart>(), leather);
                Check("18_braced_haft_is_a_hit_for_strength_trade", braced.MaxStrengthBonus == 5 && braced.HitBonus == normal.HitBonus - 1, new { normalCap = normal.MaxStrengthBonus, bracedCap = braced.MaxStrengthBonus, normalHit = normal.HitBonus, bracedHit = braced.HitBonus });
                var guard = WeaponForgingService.PreviewForge(head, oak, Carry("GuardLashingComponent").GetPart<WeaponComponentPart>());
                Check("19_guard_lashing_trades_penetration_for_hit", guard.HitBonus == normal.HitBonus + 1 && guard.PenBonus == normal.PenBonus - 1, new { hit = guard.HitBonus, penetration = guard.PenBonus });
                var hood = Carry("FilterHood"); Require(InventorySystem.Equip(actor, hood), "hood equip"); int intake = Intake(); Require(InventorySystem.UnequipItem(actor, hood), "hood remove");
                Check("20_filter_hood_uses_real_body_and_removes_cleanly", intake == 50 && Intake() == 100 && actor.GetPart<GasMaskPart>() == null, new { equippedIntake = intake, carriedIntake = Intake() });
                var apron = Carry("AcidworkerApron"); Require(InventorySystem.Equip(actor, apron), "apron equip"); int acid = Hurt("Acid"), heat = Hurt("Heat"); Require(InventorySystem.UnequipItem(actor, apron), "apron remove");
                Check("21_acid_apron_typed_defense_has_dv_and_speed_cost", acid == 5 && heat == 10 && apron.GetPart<ArmorPart>().DV == -1 && apron.GetPart<ArmorPart>().SpeedPenalty == 5, new { acid, heat, dv = apron.GetPart<ArmorPart>().DV, speedPenalty = apron.GetPart<ArmorPart>().SpeedPenalty });
                var cloak = Carry("ColdwardCloak"); Require(InventorySystem.Equip(actor, cloak), "cloak equip"); int cold = Hurt("Cold"); Require(InventorySystem.UnequipItem(actor, cloak), "cloak remove");
                Check("22_cold_cloak_typed_defense_has_dv_cost", cold == 5 && cloak.GetPart<ArmorPart>().DV == -1 && actor.GetStatValue("ColdResistance") == 0, new { cold, dv = cloak.GetPart<ArmorPart>().DV });
                var torch = Carry("Torch"); torch.GetPart<LightSourcePart>().Enabled = false; torch.GetPart<FuelPart>().FuelMass = 40; var oil = Carry("LampOil", 2);
                Check("23_refuel_pays_whole_oil_and_caps_without_lighting", Act(oil, "RefuelTorch|" + Id(torch)) && torch.GetPart<FuelPart>().FuelMass == 50 && Count("LampOil") == 1 && !torch.GetPart<LightSourcePart>().Enabled, new { fuel = torch.GetPart<FuelPart>().FuelMass, oil = Count("LampOil") });
                Check("refill_full_torch_is_free", !Act(oil, "RefuelTorch|" + Id(torch)) && Count("LampOil") == 1, new { tick = turns.TickCount, oil = Count("LampOil") });
                Require(Act(Carry("KnotflaxCord"), "PrepareRecipe|bandage"), "bandage preparation"); var bandage = Packed("KnotflaxBandage"); Require(actor.ApplyEffect(new BleedingEffect()), "controlled bleeding"); int hpBefore = actor.GetStatValue("Hitpoints");
                bool treated = InventorySystem.PerformAction(actor, bandage, "ApplyTonic", zone); Require(treated, "bandage application"); turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "bandage payment");
                Check("24_bandage_spends_one_and_cures_only_bleeding", !actor.HasEffect<BleedingEffect>() && Count("KnotflaxBandage") == 0 && actor.GetStatValue("Hitpoints") == hpBefore, new { beforeHP = hpBefore, afterHP = actor.GetStatValue("Hitpoints"), count = Count("KnotflaxBandage") });
                Require(Act(Carry("MendleafSprig", 2), "PrepareRecipe|concentrate_mendleaf"), "mendleaf preparation");
                bool brewed = BrewingService.TryBrew(actor, ctx.Factory, new[] { Packed("ConcentratedMendleaf") }, out var healing, out _, out reason); Require(brewed, reason); turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "brew owner action");
                Check("25_concentrated_mendleaf_makes_one_two_die_brew", Count("MendleafSprig") == 0 && healing.GetPart<TonicPart>().Healing == "2d4", new { healing = healing.GetPart<TonicPart>().Healing, source = Count("MendleafSprig") });
                Require(Act(Carry("GroveRed"), "PrepareRecipe|detox_grove_red"), "grove preparation"); var pulp = Packed("CleansedGrovePulp"); string properties = pulp.GetPart<ReagentPart>().PropertiesRaw;
                Require(BrewingService.TryBrew(actor, ctx.Factory, new[] { pulp }, out var safe, out _, out reason), reason); turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "pulp brew action");
                Check("26_detox_sacrifices_toxic_and_makes_two_die_healing", properties == "vital:2" && safe.GetPart<TonicPart>().Healing == "2d4" && Count("GroveRed") == 0, new { properties, healing = safe.GetPart<TonicPart>().Healing });
                Entity Bed(int x, int y) { var bed = Place("Grass", x, y); bed.SetTag("Plantable"); bed.AddPart(new CultivatedSoilPart()); return bed; }
                Bed(11, 10); Bed(10, 11); var cropOwner = Place("KnotflaxCrop", 11, 10); var crop = cropOwner.GetPart<CropPart>(); Require(CropTime.Reconcile(crop, zone, turns.TickCount), "crop clock"); var sludge = Carry("InertSludge"); int stage = crop.TicksPerStage;
                Check("27_compost_pays_sludge_without_free_water_or_growth", Act(cropOwner, "CompostCrop|" + Id(sludge)) && crop.Composted && crop.TicksPerStage == (stage * 3 + 3) / 4 && crop.TicksInStage == 0 && crop.MoistureTicks == 0 && Count("InertSludge") == 0,
                    new { originalStage = stage, stage = crop.TicksPerStage, crop.Composted, crop.MoistureTicks, crop.TicksInStage });
                Check("29_transplant_moves_exact_owner_and_keeps_compost", Act(cropOwner, "TransplantCrop|10|11") && zone.GetEntityPosition(cropOwner) == (10, 11) && crop.Composted && crop.MoistureTicks == 0,
                    new { cropOwner.ID, x = zone.GetEntityPosition(cropOwner).x, y = zone.GetEntityPosition(cropOwner).y, crop.Composted });
                crop.MoistureTicks = crop.TicksPerStage * 2; turns.AdvanceClock(crop.MoistureTicks * CropTime.WorldTicksPerUnit); Require(CropTime.Reconcile(crop, zone, turns.TickCount) && crop.GrowthStage == 2, "controlled elapsed moist growth");
                Check("28_seed_choice_gives_triple_seed_no_cord", Act(cropOwner, "HarvestCropSeeds") && zone.GetEntityCell(cropOwner) == null
                    && zone.GetCell(10, 11).Objects.Where(e => e.BlueprintName == "KnotflaxSeed").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == 3
                    && !zone.GetCell(10, 11).Objects.Any(e => e.BlueprintName == "KnotflaxCord"), new { seeds = 3, produce = 0, controlledElapsedGrowth = true });
                var dagger = Carry("Dagger"); var salt = Carry("PaleSalt", 2);
                Check("30_physical_infusion_has_finite_cost_without_bits", Act(salt, "InfuseMineral|" + Id(dagger)) && dagger.GetPart<EnhancementPaleSalt>() != null && Count("PaleSalt") == 1 && !actor.HasPart<BitLockerPart>(),
                    new { dagger.ID, minerals = Count("PaleSalt"), enhancements = ItemEnhancing.CountEnhancements(dagger) });
                Check("duplicate_infusion_is_free", !Act(salt, "InfuseMineral|" + Id(dagger)) && Count("PaleSalt") == 1 && ItemEnhancing.CountEnhancements(dagger) == 1, new { minerals = Count("PaleSalt"), enhancements = ItemEnhancing.CountEnhancements(dagger) });
                var manager = new OverworldZoneManager(null, 814); manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
                var state = GameSessionState.Capture(RunId, "preparation-bench", manager, turns, actor); GameSessionState loaded;
                using (var stream = new MemoryStream()) { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, ctx.Factory)); }
                var restored = loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == dagger.ID);
                Check("replacement_save_keeps_payment_enhancement_and_harvest", loaded.Player != actor && restored != dagger && restored.GetPart<EnhancementPaleSalt>() != null
                    && !loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Any(e => e.ID == cropOwner.ID)
                    && loaded.Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "PaleSalt").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == 1,
                    new { replacementPlayer = loaded.Player != actor, replacementItem = restored != dagger, restoredEnhancements = ItemEnhancing.CountEnhancements(restored) });
            }
        }
        static void Require(bool ok, string label) { if (!ok) throw new InvalidOperationException("Preparation witness: " + label); }
        void Check(string name, bool passed, object observation) { Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + name); Observations.Add(new { name, passed, state = observation }); }
        sealed class DetachedScope : IDisposable
        {
            readonly TurnManager active = TurnManager.Active; readonly Entity world = TurnManager.World;
            readonly Zone zone = SettlementRuntime.ActiveZone; readonly SettlementManager settlement = SettlementManager.Current;
            readonly EntityFactory forge = ForgePart.Factory, still = AlchemyStillPart.Factory, seed = SeedPart.Factory, crop = CropSystem.Factory;
            readonly Action<string> onMessage = MessageLog.OnMessage; readonly Func<int> tick = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries(); readonly List<string> notices = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<Action> restore = new List<Action>();
            readonly List<SpellFxSequence> spells = SpellFxBus.Drain(); readonly List<AsciiFxRequest> ascii = AsciiFxBus.Drain();
            readonly Queue<AsciiFxRequest> queue;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                foreach (var type in new[] { typeof(EnhancementFactory), typeof(BrewRuleRegistry) })
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (field.IsLiteral) continue; object value = field.GetValue(null);
                        if (value is IDictionary map) { var rows = map.Keys.Cast<object>().Select(k => (k, map[k])).ToArray(); restore.Add(() => { map.Clear(); foreach (var row in rows) map.Add(row.k, row.Item2); }); }
                        else if (value is IList list) { var rows = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var row in rows) list.Add(row); }); }
                        else if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
                    }
                TurnManager.World = null; MessageLog.OnMessage = null; typeof(SettlementManager).GetProperty("Current").SetValue(null, null);
            }
            public void Dispose()
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request); foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell);
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                ForgePart.Factory = forge; AlchemyStillPart.Factory = still; SeedPart.Factory = seed; CropSystem.Factory = crop;
                SettlementRuntime.ActiveZone = zone; TurnManager.World = world; typeof(TurnManager).GetProperty("Active").SetValue(null, active); typeof(SettlementManager).GetProperty("Current").SetValue(null, settlement);
                MessageLog.Restore(messages, notices, flash, serial); MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tick;
            }
        }
    }
}
