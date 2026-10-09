using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _tacticalWater;
        const string TacticalWaterIntent = "Ordinary seed64 Stormcaller start and real map/ground travel to the tempering shelter. Inspect the actual authored water carrier, heat it with the starting Kindle, use ordinary movement/waits through cooldown for a second dose if needed, observe its finite paid self-douse, inspect the empty shell, and F5/unsaved-step/F6 its literal inventory. No item, HP, fire, target, hostility, position or AI grants.";
        const string TacticalWaterLimits = "One predeclared existing source witness and starting build, not blind discovery or all-seed balance. The source is queried only after ordinary travel generates it. Native death drops/electrical amplification are separately tested; this journey does not kill the carrier or claim an electrical follow-up. A cooling target, lethal fire tick or unsafe approach is an honest refusal, not a reason to alter the source. Screenshots require independent review.";
        static readonly string[] TacticalWaterChecks = { "ordinary_start", "tactical_water_source", "tactical_full_examine", "tactical_paid_douse", "tactical_whole_opportunity", "tactical_empty_examine", "tactical_saved_empty", "tactical_finish" };
        public void InitializeTacticalWater(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride)) throw new InvalidOperationException("Isolated launcher required before tactical-water bootstrap.");
            _tacticalWater = true; Initialize(context, connectedBuild: "stormcaller");
        }
        bool WaterRay(Entity target, int x, int y, out int dx, out int dy, bool requireVisible = true)
        {
            dx = dy = 0; var to = Zone.GetEntityCell(target);
            if (to == null || (requireVisible && !to.IsVisible) || target.GetStatValue("Hitpoints") <= 4 || !FactionManager.IsHostile(Player, target)) return false;
            int ox = to.X - x, oy = to.Y - y, distance = Math.Max(Math.Abs(ox), Math.Abs(oy));
            if (distance < 2 || distance > 5 || !(ox == 0 || oy == 0 || Math.Abs(ox) == Math.Abs(oy))) return false;
            dx = Math.Sign(ox); dy = Math.Sign(oy);
            // Pure equivalent of the native first-target rule; never call the
            // spell collector, which also publishes FX path observations.
            for (int step = 1; step <= distance; step++)
            {
                var c = Zone.GetCell(x + dx * step, y + dy * step); if (c == null) return false;
                var first = c.Occupants.FirstOrDefault(e => AbilityTargeting.IsCreatureTarget(e, Player))
                    ?? c.Occupants.FirstOrDefault(e => e != null && !e.HasTag("Creature") && AbilityTargeting.IsElementalTarget(e, Player));
                if (first != null) return first == target;
                if (c.IsSolid()) return false;
            }
            return false;
        }
        IEnumerator WaterCooldown(Entity carrier, Func<int> remaining)
        {
            for (int n = 0; remaining() > 0; n++)
            {
                Require(n < 8 && Zone.GetEntityCell(carrier) != null && carrier.GetStatValue("Hitpoints") > 4,
                    "bounded native cooldown with a living carrier");
                int distance = SpatialQuery.Distance(Zone, Player, carrier);
                if (distance >= 4 && WaterRay(carrier, At.X, At.Y, out _, out _) && Safe(Zone, At, ThreatClearance))
                {
                    yield return Paid(Tap(Key.Period), "local", "tactical-native-cooldown-wait");
                    continue;
                }
                int x = At.X, y = At.Y;
                var step = Steps.Select(d => Zone.GetCell(x + d.x, y + d.y))
                    .Where(c => c != null && Safe(Zone, c, ThreatClearance) && Zone.CanPlaceFootprint(Player, c.X, c.Y)
                        && (c.X == x || c.Y == y || Zone.CanPlaceFootprint(Player, c.X, y) || Zone.CanPlaceFootprint(Player, x, c.Y)))
                    .OrderByDescending(c => WaterRay(carrier, c.X, c.Y, out _, out _, requireVisible: false))
                    .ThenBy(c => Math.Abs(4 - SpatialQuery.DistanceToCell(Zone, carrier, c.X, c.Y)))
                    .ThenBy(c => c.Y).ThenBy(c => c.X).FirstOrDefault();
                Require(step != null, "an actual unblocked safe step exists during cooldown; no combat setup mutation");
                yield return StepTo(step.X, step.Y);
            }
        }
        IEnumerator WaterExamine(Entity target, string label)
        {
            int tick = Tick, energy = Energy; var cell = Zone.GetEntityCell(target);
            Require(cell?.IsVisible == true, "actual visible carrier can be inspected");
            yield return Tap(Key.L); Require(State == "LookMode", "real look cursor");
            var cursor = (WorldCursorState)Field(_input, "_worldCursorState");
            for (int n = 0; cursor.X != cell.X || cursor.Y != cell.Y; n++)
            { Require(n < 8, "bounded nearby look inspection"); yield return Tap(Direction(cell.X - cursor.X, cell.Y - cursor.Y)); }
            yield return Tap(Key.Enter); Require(State == "WorldActionMenuOpen", "real distant owner menu");
            string choose = WorldInteractionSystem.PickTargetCommandPrefix + target.ID;
            var offered = (System.Collections.Generic.List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            if (!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target) && !offered.Any(a => a.Command == choose)
                && offered.Any(a => a.Command == WorldInteractionSystem.PickCellCommand)) yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            if (!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target)
                || ((System.Collections.Generic.List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions")).Any(a => a.Command == choose)) yield return MenuAction(choose);
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "exact native carrier selected");
            yield return MenuAction("Examine"); yield return ReadPages(label); yield return CloseNormal();
            Require(Tick == tick && Energy == energy, "native inspection is free");
        }
        IEnumerator TacticalWaterJourney()
        {
            yield return TravelSurface("Overworld.15.10.0");
            var carrier = Zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "MarlbackCindercaller" && e.HasPart<TacticalSupplyPart>());
            Require(carrier != null, "actual predeclared seed64 fresh worksite carrier exists");
            var shell = carrier.GetPart<TacticalSupplyPart>().FindCarriedWater();
            Check("tactical_water_source", shell?.BlueprintName == "SunbladderShell" && shell.GetPart<WaterskinPart>().Charges == 1
                && shell.GetPart<PhysicsPart>().InInventory == carrier && carrier.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "FireMoss"));
            int dx = 0, dy = 0;
            for (int n = 0; !WaterRay(carrier, At.X, At.Y, out dx, out dy); n++)
            {
                Require(n < 160 && Zone.GetEntityCell(carrier) != null, "bounded ordinary approach to a live carrier");
                var path = PathTo(c => WaterRay(carrier, c.X, c.Y, out _, out _, requireVisible: false));
                Require(path != null && path.Count > 0, "real safe path reaches a legal Kindle ray");
                yield return StepTo(path[0].x, path[0].y);
            }
            yield return WaterExamine(carrier, "water-01-full-carrier");
            Check("tactical_full_examine", _readerText.Contains("sunbladder") && _readerText.Contains("turn"));
            var abilities = Player.GetPart<ActivatedAbilitiesPart>(); var kindle = abilities.AbilityList.Single(a => a.Command == "CommandKindle");
            int slot = Array.FindIndex(abilities.SlotAssignments, id => id == kindle.ID);
            Require(slot >= 0 && kindle.CooldownRemaining == 0 && shell.GetPart<WaterskinPart>().Charges == 1, "real starting Kindle and finite water remain ready");
            var heat = carrier.GetPart<ThermalPart>(); Require(heat != null, "actual native thermal carrier");
            var before = Zone.GetEntityPosition(carrier); CavesOfOoo.Diagnostics.Diag.Entry[] rows = null;
            for (int dose = 0; dose < 2; dose++)
            {
                if (dose > 0)
                {
                    // Creature capacity2 needs about950J from ambient. The first
                    // real600J Kindle is preheating; its6-turn cooldown still
                    // leaves enough heat after native5% per-turn decay for dose2.
                    yield return WaterCooldown(carrier, () => kindle.CooldownRemaining);
                    for (int n = 0; !WaterRay(carrier, At.X, At.Y, out dx, out dy); n++)
                    {
                        Require(n < 4 && Zone.GetEntityCell(carrier) != null, "bounded second-ray reposition before heat dissipates");
                        var path = PathTo(c => WaterRay(carrier, c.X, c.Y, out _, out _, requireVisible: false));
                        Require(path != null && path.Count > 0, "actual safe second Kindle approach");
                        yield return StepTo(path[0].x, path[0].y);
                    }
                    float threshold = heat.FlameTemperature - Math.Max(0, carrier.GetPart<MaterialPart>()?.Volatility ?? 0) * 100f;
                    Require(heat.Temperature + FireDose.Ignition / (heat.HeatCapacity > 0 ? heat.HeatCapacity : 1f) >= threshold
                        && !(carrier.GetEffect<WetEffect>()?.Moisture > .35f), "actual retained heat and dryness admit the second ignition dose");
                }
                Require(kindle.CooldownRemaining == 0 && shell.GetPart<WaterskinPart>().Charges == 1,
                    "native cooldown and original finite water are ready");
                before = Zone.GetEntityPosition(carrier);
                float priorHeat = heat.Temperature; int priorHp = carrier.GetStatValue("Hitpoints");
                string marker = Mark("tactical-water-before-kindle-" + (dose + 1));
                yield return Tap(Shortcut(slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
                Require(State == "AwaitingDirection" && WaterRay(carrier, At.X, At.Y, out dx, out dy), "exact current ordinary Kindle ray");
                yield return Paid(Tap(Direction(dx, dy)), "local", "tactical-native-kindle-" + (dose + 1));
                rows = Window(marker);
                Require(rows.Any(e => e.Kind == "SpellDamage" && e.ActorId == Player.ID && e.TargetId == carrier.ID),
                    "the real Kindle actually hit the intended carrier");
                _observations.Add(new { phase = "native-kindling-dose", dose = dose + 1, priorHeat, afterHeat = heat.Temperature,
                    priorHp, afterHp = carrier.GetStatValue("Hitpoints"), cooldown = kindle.CooldownRemaining,
                    remainingWater = shell.GetPart<WaterskinPart>().Charges, rows });
                if (shell.GetPart<WaterskinPart>().Charges == 0) break;
                Require(dose == 0 && Zone.GetEntityCell(carrier) != null && carrier.GetStatValue("Hitpoints") > 4
                    && !carrier.HasEffect<BurningEffect>(), "first dose genuinely preheated a surviving carrier; no free ignition or retry after death");
                yield return Capture("water-02-real-preheating");
            }
            Check("tactical_paid_douse", rows.Any(e => e.Kind == "SpellDamage" && e.ActorId == Player.ID && e.TargetId == carrier.ID)
                && rows.Count(e => e.Kind == "TacticalWaterUsed" && e.ActorId == carrier.ID && e.TargetId == shell.ID) == 1
                && shell.GetPart<WaterskinPart>().Charges == 0 && carrier.GetEffect<WetEffect>()?.Moisture > .2f && !carrier.HasEffect<BurningEffect>());
            Check("tactical_whole_opportunity", Zone.GetEntityPosition(carrier) == before
                && rows.Count(e => e.Kind == "Begin" && e.ActorId == carrier.ID) == 1
                && rows.Count(e => e.Kind == "End" && e.ActorId == carrier.ID) == 1
                && !rows.Any(e => e.ActorId == carrier.ID && (e.Kind == "HitRoll" || e.Kind == "MeleeAttackVetoed" || e.Kind == "SpellDamage")));
            _observations.Add(new { phase = "native-finite-water", carrier = carrier.ID, water = shell.ID, before, after = Zone.GetEntityPosition(carrier), rows });
            yield return Capture("water-02-paid-wet-carrier");
            yield return WaterExamine(carrier, "water-03-empty-carrier"); Check("tactical_empty_examine", _readerText.Contains("empty"));
            string carrierId = carrier.ID, shellId = shell.ID; var actor = Player; int tick = Tick, energy = Energy, x = At.X, y = At.Y;
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(SaveFile());
            var step = Steps.Select(d => Zone.GetCell(x + d.x, y + d.y)).First(c => Safe(Zone, c, ThreatClearance) && Zone.CanPlaceFootprint(Player, c.X, c.Y));
            yield return StepTo(step.X, step.Y); yield return Reload(actor);
            var restored = Owner(carrierId); var restoredShell = restored?.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == shellId);
            Check("tactical_saved_empty", restored != carrier && restoredShell != shell && restored?.HasPart<TacticalSupplyPart>() == true
                && restoredShell?.GetPart<WaterskinPart>().Charges == 0 && restoredShell.GetPart<PhysicsPart>().InInventory == restored
                && Tick == tick && Energy == energy && At.X == x && At.Y == y);
            Check("tactical_finish", State == "Normal" && !DevMode.Enabled && !Player.HasPart<BitLockerPart>() && !DebugInvincibility.IsEnabled(Player));
            yield return Capture("water-04-restored-empty-supply");
        }
    }
}
