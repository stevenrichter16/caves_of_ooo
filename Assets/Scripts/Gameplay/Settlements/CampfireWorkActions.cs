using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class CampfireWorkActions
    {
        const string DryPrefix = "DryGear|";
        internal const string FeedFullCommand = "FeedCookingFireFull", StokeCommand = "StokeCookingFire";
        internal static bool IsCommand(string command) => command == "FeedCookingFire" || command == "RelightCookingFire" || command == FeedFullCommand || command == StokeCommand
            || command?.StartsWith(DryPrefix, StringComparison.Ordinal) == true;
        internal static bool Source(Entity actor, CampfirePart fire, Zone zone)
        {
            var owner = fire?.ParentEntity; var heat = owner?.GetPart<ThermalPart>();
            return WorldResourceActions.Nearby(actor, owner, zone) && owner.GetPart<CampfirePart>() == fire && !owner.GetPart<PhysicsPart>().Takeable
                && !owner.HasTag("Creature") && heat?.ParentEntity == owner && WorldResourceActions.Finite(heat.Temperature);
        }
        internal static bool Fuel(CampfirePart fire, out FuelPart fuel)
        {
            fuel = fire.ParentEntity.GetPart<FuelPart>();
            return fire.FiniteCooking && fuel?.ParentEntity == fire.ParentEntity && WorldResourceActions.Finite(fuel.FuelMass)
                && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.FuelMass >= 0 && fuel.MaxFuel > 0 && fuel.FuelMass <= fuel.MaxFuel;
        }
        internal static bool Hot(CampfirePart fire) => fire.ParentEntity.GetPart<ThermalPart>().Temperature >= CookingService.MinimumFiniteCookingTemperature
            && (!fire.FiniteCooking || Fuel(fire, out var fuel) && fuel.FuelMass > 0);
        /// <summary>Salvaged timber carried, counting every stack.</summary>
        internal static int TimberCarried(Entity actor)
            => actor.GetPart<InventoryPart>()?.Objects.Where(e => e.BlueprintName == "SalvagedTimber" && WorldResourceActions.Carried(actor, e, false)).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) ?? 0;
        /// <summary>How many timber feed the fire to full (each is ten fuel, the last may overfill) without feeding more than is carried.</summary>
        internal static int FeedCount(FuelPart fuel, int carried)
            => carried <= 0 || fuel.FuelMass >= fuel.MaxFuel ? 0 : Math.Min(carried, (int)Math.Ceiling((fuel.MaxFuel - fuel.FuelMass) / 10f - 1e-4f));
        static Entity Timber(Entity actor) => actor.GetPart<InventoryPart>()?.Objects.FirstOrDefault(e => e.BlueprintName == "SalvagedTimber" && WorldResourceActions.Carried(actor, e, false));
        static bool Gear(Entity actor, Entity gear) => WorldResourceActions.Carried(actor, gear) && gear.GetEffect<WetEffect>() is WetEffect wet
            && WorldResourceActions.Finite(wet.Moisture) && wet.Moisture > 0 && !(gear.GetEffect<FrozenEffect>()?.Cold > 0);
        /// <summary>
        /// "What can I do here?": one plain line of what the rows of this fire would offer the viewer right now (feed it, light it, dry wet gear), or null when it would offer nothing.
        /// It is a read of the same conditions the rows use, never a promise; the rows themselves are the authority.
        /// </summary>
        internal static string Affordance(Entity actor, CampfirePart fire, Zone zone)
        {
            if (actor == null || fire == null || zone == null || !Source(actor, fire, zone) || !Fuel(fire, out var fuel)) return null;
            var parts = new System.Collections.Generic.List<string>();
            int feedN = FeedCount(fuel, TimberCarried(actor)); if (feedN >= 1) parts.Add("feed it (" + feedN + " timber)");
            if (StoredTimberFeed.Choices(actor, fire, zone).Any()) parts.Add("feed it from your camp box");
            var oilChoices = LiquidHearthFuel.Choices(actor, fire, zone).ToArray();
            if (oilChoices.Any(choice => !LiquidHearthFuel.IsStoredCommand(choice.Key))) parts.Add("feed it from a carried oil flask");
            if (oilChoices.Any(choice => LiquidHearthFuel.IsStoredCommand(choice.Key))) parts.Add("feed it from an oil flask in your adjacent camp box");
            if (fuel.FuelMass > 0 && !Hot(fire) && IgnitionSources.Find(actor, zone, fire.ParentEntity) != null) parts.Add("light it");
            if (Hot(fire) && actor.GetPart<InventoryPart>() is InventoryPart pack)
            {
                int wet = 0; foreach (var gear in pack.Objects) if (Gear(actor, gear)) wet++;
                if (wet > 0) parts.Add("dry " + wet + " wet " + (wet == 1 ? "thing" : "things"));
            }
            return parts.Count == 0 ? null : "Here you can: " + string.Join(", ", parts) + ".";
        }

        internal static void AddActions(Entity actor, CampfirePart fire, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Source(actor, fire, zone)) return;
            if (Hot(fire) && actor.GetPart<InventoryPart>() is InventoryPart pack)
                foreach (var gear in pack.Objects) if (Gear(actor, gear))
                    actions.AddAction("DryGear", "dry " + gear.GetDisplayName() + " (one quarter)", DryPrefix + Uri.EscapeDataString(gear.ID), '\0', 17, time: ActionTime.OneAction);
            if (!Fuel(fire, out var fuel)) return;
            int feedN = FeedCount(fuel, TimberCarried(actor));
            if (feedN >= 1)
                actions.AddActionWithHint("FeedCookingFire", "feed coals (1 salvaged timber, up to 10 fuel)", "FeedCookingFire", "Adds one timber to the fire. Cold coals still need a flame to light.", 'f', 18, time: ActionTime.OneAction);
            if (feedN >= 2)
                actions.AddActionWithHint("FeedCookingFireFull", "feed coals to full (" + feedN + " timber, +" + (int)Math.Min(fuel.MaxFuel - fuel.FuelMass, 10 * feedN) + " fuel)", FeedFullCommand, "Adds timber until the fire is full, all in one action. It does not light it.", 'u', 18, time: ActionTime.OneAction);
            var flame = IgnitionSources.Find(actor, zone, fire.ParentEntity);
            // A fire that is already burning is not relit: the light rows appear only for a fire that is out (cold, or out of fuel), so nothing offers to spend a strike on a flame.
            bool burning = Hot(fire);
            // A flint striker pays a strike for every lighting, so the row says so and how many are left.
            string strike = flame?.GetPart<FlintStrikerPart>() is FlintStrikerPart striker ? " (1 strike, " + striker.Strikes + " left)" : "";
            if (!burning && fuel.FuelMass > 0 && flame != null)
                actions.AddActionWithHint("RelightCookingFire", "relight coals from flame" + strike, "RelightCookingFire", "Lights the coals from a flame you carry or stand beside." + (strike.Length > 0 ? " Spends 1 strike." : ""), 'l', 18, time: ActionTime.OneAction);
            else if (!burning)
                // A fire that is out says what stops it from being lit instead of offering nothing.
                actions.AddDisabled("RelightCookingFire", "relight coals from flame", fuel.FuelMass <= 0 ? "no_fuel" : "no_flame",
                    fuel.FuelMass <= 0 ? "The coals have no fuel. Feed them first." : "You have no flame to light it from: a burning fire beside you, a lit torch in hand, or a flint striker." + (FlintStrikerPart.WornHint(actor) ?? ""), '\0', 18);
            if (feedN >= 1 && flame != null && !burning)
                actions.AddActionWithHint("StokeCookingFire", "stoke: feed " + feedN + " timber and light it" + strike, StokeCommand,
                    "Adds all the timber the fire will take, then lights it from a flame you carry." + (strike.Length > 0 ? " Spends 1 strike." : ""), 't', 18, time: ActionTime.OneAction);
        }
        /// <summary>
        /// Feed to full, or stoke (feed to full, then light it from a flame in reach), as one action in the caller's transaction: every timber, the fuel, the strike and the heat
        /// return together on any failure. Stoke refuses when it could do neither half; feed to full refuses with no timber or no room.
        /// </summary>
        static bool FeedFullOrStoke(Entity actor, CampfirePart fire, FuelPart fuel, Zone zone, string command, InventoryTransaction tx)
        {
            var owner = fire.ParentEntity; var pack = actor.GetPart<InventoryPart>(); var heat = owner.GetPart<ThermalPart>(); bool stoke = command == StokeCommand;
            int count = FeedCount(fuel, TimberCarried(actor)); float fuelBefore = fuel.FuelMass, heatBefore = heat.Temperature; int fed = 0;
            if (!stoke && count < 1) return WorldResourceActions.Reject(actor, owner, command, "timber_or_fuel_room_unavailable");
            Entity flame = stoke ? IgnitionSources.Find(actor, zone, owner) : null;
            if (stoke && flame == null) return WorldResourceActions.Reject(actor, owner, command, "fuel_or_ignition_unavailable");
            if (count >= 1)
            {
                var receipt = InventoryTransferSnapshot.Capture(pack);
                tx.Do(null, () => { receipt.Restore(); fuel.FuelMass = fuelBefore; });
                if (!receipt.Apply(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        var timber = Timber(actor);
                        if (timber == null || !tx.TryClaim(timber, actor, command) || !pack.TryConsumeOne(timber)) return false;
                        fed++;
                    }
                    return true;
                }) || !receipt.ClaimChanges(tx, actor, command)) return false;
                if (!Source(actor, fire, zone) || !Fuel(fire, out var current) || current != fuel || fuel.FuelMass != fuelBefore) return WorldResourceActions.Reject(actor, owner, command, "fire_changed_during_payment");
                fuel.FuelMass = Math.Min(fuel.MaxFuel, fuelBefore + 10 * fed);
            }
            bool lit = false;
            if (stoke && fuel.FuelMass > 0 && heat.Temperature < CookingService.MinimumFiniteCookingTemperature)
            {
                if (!tx.TryClaim(flame, actor, command)) return false;
                if (!IgnitionSources.Consume(actor, flame, tx, "coals")) return WorldResourceActions.Reject(actor, owner, command, "fuel_or_ignition_unavailable");
                tx.Do(() => heat.Temperature = 450, () => heat.Temperature = heatBefore); lit = true;
            }
            if (fed == 0 && !lit) return WorldResourceActions.Reject(actor, owner, command, "nothing_to_do");
            int piled = fed;
            tx.AfterCommit(() =>
            {
                owner.GetPart<HearthGlowPart>()?.SyncLight(); ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(owner), "CookingFed");
                MessageLog.Add((piled > 0 ? "You pile " + piled + " salvaged timber on the coals" : "You tend the coals") + (lit ? " and light them from the flame." : ". The fuel still needs flame."));
            });
            return true;
        }

        internal static bool TryAct(Entity actor, CampfirePart fire, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            if (!Source(actor, fire, zone)) return WorldResourceActions.Reject(actor, fire?.ParentEntity, command, "invalid_fire_context");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = fire.ParentEntity;
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return false;
                if (command.StartsWith(DryPrefix, StringComparison.Ordinal))
                {
                    var fields = command.Split('|'); if (fields.Length != 2) return WorldResourceActions.Reject(actor, owner, command, "malformed_selection");
                    var gear = WorldResourceActions.ExactCarried(actor, fields[1]);
                    if (!Hot(fire) || !Gear(actor, gear)) return WorldResourceActions.Reject(actor, gear, command, "heat_or_dryable_gear_unavailable");
                    if (!tx.TryClaim(gear, actor, command)) return false;
                    var wet = gear.GetEffect<WetEffect>(); var effects = gear.GetPart<StatusEffectsPart>(); float before = wet.Moisture;
                    string cause = wet.LastRemovalCause; int index = 0;
                    for (int i = 0; i < effects.EffectCount; i++) if (effects.GetAllEffects()[i] == wet) index = i;
                    float removed = Math.Min(before, 0.25f);
                    tx.Do(null, () =>
                    {
                        var current = gear.GetEffect<WetEffect>();
                        if (current != null) current.Moisture = Math.Min(1f, current.Moisture + removed);
                        else { wet.Moisture = before; effects.RestoreRemovedEffectForInventoryUndo(wet, index); }
                        wet.LastRemovalCause = cause;
                    });
                    wet.Moisture = before - removed; if (wet.Moisture == 0) effects.RemoveEffect(wet);
                    tx.AfterCommit(() => MessageLog.Add("You dry " + gear.GetDisplayName() + " beside the heat."));
                }
                else
                {
                    if (!Fuel(fire, out var fuel)) return WorldResourceActions.Reject(actor, owner, command, "invalid_finite_fuel");
                    if (command == FeedFullCommand || command == StokeCommand)
                    {
                        if (!FeedFullOrStoke(actor, fire, fuel, zone, command, tx)) return false;
                    }
                    else if (command == "FeedCookingFire")
                    {
                        var timber = Timber(actor);
                        if (timber == null || fuel.FuelMass >= fuel.MaxFuel) return WorldResourceActions.Reject(actor, owner, command, "timber_or_fuel_room_unavailable");
                        if (!tx.TryClaim(timber, actor, command)) return false;
                        var pack = actor.GetPart<InventoryPart>(); var receipt = InventoryTransferSnapshot.Capture(pack); float before = fuel.FuelMass;
                        tx.Do(null, () => { receipt.Restore(); fuel.FuelMass = before; });
                        if (!receipt.Apply(() => pack.TryConsumeOne(timber)) || !receipt.ClaimChanges(tx, actor, command)) return false;
                        if (!Source(actor, fire, zone) || !Fuel(fire, out var current) || current != fuel || fuel.FuelMass != before) return WorldResourceActions.Reject(actor, owner, command, "fire_changed_during_payment");
                        fuel.FuelMass = Math.Min(fuel.MaxFuel, before + 10);
                        tx.AfterCommit(() => MessageLog.Add("You feed one salvaged timber to the coals. The fuel still needs flame."));
                    }
                    else
                    {
                        var heat = owner.GetPart<ThermalPart>(); var source = IgnitionSources.Find(actor, zone, owner);
                        if (heat.Temperature >= CookingService.MinimumFiniteCookingTemperature && fuel.FuelMass > 0) return WorldResourceActions.Reject(actor, owner, command, "already_burning");
                        if (fuel.FuelMass <= 0 || source == null) return WorldResourceActions.Reject(actor, owner, command, "fuel_or_ignition_unavailable");
                        if (!tx.TryClaim(source, actor, command)) return false;
                        if (!IgnitionSources.Consume(actor, source, tx, "coals")) return WorldResourceActions.Reject(actor, owner, command, "fuel_or_ignition_unavailable");
                        float before = heat.Temperature; tx.Do(() => heat.Temperature = 450, () => heat.Temperature = before);
                        tx.AfterCommit(() => { owner.GetPart<HearthGlowPart>()?.SyncLight(); ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(owner), "CookingRelit"); MessageLog.Add("You relight the cooking coals from the flame."); });
                    }
                }
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }

    internal static class StoredTimberFeed
    {
        const string Timber = "SalvagedTimber";
        const string Prefix = "FeedStoredTimber|";

        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        internal static System.Collections.Generic.IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor, CampfirePart fire, Zone zone)
        {
            if (!RouteWorkMenu.ActorReady(actor, zone) || !CampfireWorkActions.Source(actor, fire, zone)
                || !CampfireWorkActions.Fuel(fire, out var fuel)) yield break;
            var hearth = fire.ParentEntity;
            var hearthCell = zone.GetEntityCell(hearth);
            var actorCell = zone.GetEntityCell(actor);
            if (!RouteWorkMenu.Visible(hearthCell) || hearth.HasEffect<BrokenEffect>()) yield break;
            var heat = hearth.GetPart<ThermalPart>();
            var maximum = fuel.MaxFuel;
            var before = fuel.FuelMass;
            var filled = Math.Min(maximum, before + 10);
            double gain = (double)filled - before;
            if (gain <= 0 || gain > 10) yield break;
            var temperature = heat.Temperature;
            var sameHearth = RouteWorkMenu.Identity(hearth);
            var factory = ForgePart.Factory;
            var template = MaterialProcessTemplate.Capture(factory, Timber);
            if (template == null) yield break;

            foreach (var box in InventorySystem.GetDepositContainers(actor, zone))
            {
                var stock = MaterialProcessStock.Resolve(actor, box, zone);
                if (stock == null) continue;
                var timber = stock.Container.Contents.FirstOrDefault(item => item.BlueprintName == Timber
                    && stock.Contains(item) && template.Snapshot(item, allowInstances: true) != null);
                if (timber == null) continue;
                var payload = template.Snapshot(timber, allowInstances: true);
                var physics = timber.GetPart<PhysicsPart>();
                var stack = timber.GetPart<StackerPart>();
                int count = stack?.StackCount ?? 1;
                string timberId = timber.ID;
                var boxCell = zone.GetEntityCell(box);

                bool FireCurrent(float expected) => ForgePart.Factory == factory && RouteWorkMenu.ActorReady(actor, zone)
                    && CampfireWorkActions.Source(actor, fire, zone) && sameHearth()
                    && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(hearth) == hearthCell
                    && RouteWorkMenu.Visible(hearthCell) && !hearth.HasEffect<BrokenEffect>() && hearth.GetPart<ThermalPart>() == heat
                    && heat.Temperature == temperature && CampfireWorkActions.Fuel(fire, out var current)
                    && current == fuel && fuel.MaxFuel == maximum && fuel.FuelMass == expected;
                bool Before() => FireCurrent(before) && stock.Contains(timber) && payload.Matches()
                    && (stack?.StackCount ?? 1) == count;
                bool After() => FireCurrent(filled) && stock.Current() && payload.Matches()
                    && (count > 1 ? stock.Contains(timber) && stack.StackCount == count - 1
                        : stock.ExcludesPaidItem(timber) && timber.SpatialZone == null
                            && physics.InInventory == null && physics.Equipped == null && (stack?.StackCount ?? 1) == 1);
                bool Refundable() => count == 1 ? stock.ExcludesPaidItem(timber)
                    : stock.CanRefundRemainingUnit(timber);
                string key = Prefix + Uri.EscapeDataString(box.ID) + "|" + Uri.EscapeDataString(timberId);
                string fuelText = gain.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = hearthCell, Key = key, Claims = new[] { box, timber },
                    Label = "feed from camp box at " + boxCell.X + "," + boxCell.Y + " (+" + fuelText + " fuel)",
                    Hint = "Uses one salvaged timber directly from your installed camp box. One action. Cold coals stay cold.",
                    Before = Before, After = After,
                    Apply = tx =>
                    {
                        if (!MaterialProcessService.ReserveInput(timber, tx) || !stock.Consume(timber, tx, false, Refundable)
                            || !FireCurrent(before)) return false;
                        tx.Do(() => fuel.FuelMass = filled, () =>
                        {
                            if (hearth.GetPart<FuelPart>() == fuel && fuel.ParentEntity == hearth
                                && WorldResourceActions.Finite(fuel.FuelMass) && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.MaxFuel >= 0)
                                fuel.FuelMass = (float)Math.Max(0, Math.Min(fuel.MaxFuel, fuel.FuelMass - gain));
                        });
                        return true;
                    },
                    Publish = () =>
                    {
                        hearth.GetPart<HearthGlowPart>()?.SyncLight();
                        ZoneRenderHooks.MarkCellDirty(hearthCell, "StoredTimberFed");
                        MessageLog.Add("You feed one salvaged timber from the camp box to the coals.");
                    }
                };
            }
        }
    }
}
