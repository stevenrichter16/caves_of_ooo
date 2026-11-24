namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to campfire entities that provides:
    /// - Color flicker between red and yellow (the campfire itself looks alive)
    /// - Proximity ambient message (one-shot per zone visit)
    /// Ember particles are handled by CampfireEmberRenderer (world-space, not grid-locked).
    /// </summary>
    public class CampfirePart : Part, IDescribeState, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "Campfire";
        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<CampfirePart>() == this
            && (PortableKindling.OwnsReceipt(ParentEntity, command) || FiniteCooking && ReagentFireFuel.IsCommand(command));

        // "What can I do here?": what the fire's rows would offer whoever is looking, in both examine readers.
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;
        public string DescribeState(DescribeContext context) => FiniteCooking ? CampfireWorkActions.Affordance(context.Viewer, this, context.Zone ?? ParentEntity?.SpatialZone) : null;

        /// <summary>How to tend the camp hearth (the owner, 2026-11-21). Only a finite cooking fire explains itself; the village's old campfires keep their own rows. The "right now" line is built from the same rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (!FiniteCooking) return;
            var fuel = ParentEntity?.GetPart<FuelPart>();
            bool standing = !PackableFixturePart.IsInertKit(ParentEntity) && ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null;
            if (!standing) guide.How.Add("Once it is set down, press C beside it to feed it, light it, cook on it, dry wet gear and rest.");
            else
            {
                guide.How.Add("Press C beside it. Feed coals with salvaged timber (or feed to full); light it with relight coals, or stoke to feed and light in one action. Once it burns, dry wet gear on it.");
                guide.How.Add("Cook raw food by choosing cook this stack beside a fire from the food in your pack.");
            }
            guide.Needs.Add("to feed it: salvaged timber (each timber is 10 fuel" + (fuel != null && fuel.MaxFuel > 0 ? "; it holds up to " + fuel.MaxFuel.ToString("G5", System.Globalization.CultureInfo.InvariantCulture) : "") + ")");
            guide.Needs.Add("optional fuel: one dry, stable combustible reagent; pitchpod resin gives " + ReagentFireFuel.FuelPerTier + " fuel, lamp-oil gives " + 3 * ReagentFireFuel.FuelPerTier + ", up to capacity. Feeding does not light it; volatile mixtures are unsuitable");
            guide.Needs.Add("to light it: a flame to light it from (a burning fire beside you, a lit torch in your hand, or a flint striker)");
            guide.Limits.Add("It burns its fuel while it is lit and goes out when the fuel does. Taking it up puts it out.");
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>New finite cooking sources opt in explicitly. Existing authored
        /// stations and older saves retain their established cooking/rest policy.</summary>
        public bool FiniteCooking = false;

        /// <summary>Authored rest service. Defaults true, including saves written
        /// before this field existed; cooking-only workspots explicitly opt out.</summary>
        public bool AllowRest = true;

        private int _renderFrameCounter;
        private bool _proximityMessageShown;

        public override bool HandleEvent(GameEvent e)
        {
            // ThermalPart already invalidates the finite 150-degree boundary.
            // Actual destructive fire is independent of residual cooking heat.
            if (FiniteCooking && (e.ID == "EffectApplied" || e.ID == "EffectRemoved")
                && e.GetParameter<Entity>("Target") == ParentEntity && e.GetParameter<Effect>("Effect") is BurningEffect)
            {
                var owner = ParentEntity; var zone = owner?.SpatialZone;
                if (owner?.GetPart<CampfirePart>() == this && WorldResourceActions.Ground(owner, zone))
                {
                    var cell = zone.GetEntityCell(owner);
                    if (ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell)) ZoneRenderHooks.MarkCellDirty(cell, "FiniteCampfire.Burning");
                }
            }
            if (e.ID == "Render")
                return HandleRender(e);
            if (e.ID == "EndTurn")
                return HandleEndTurn(e);

            // BIOME-OVERHAUL A4: campfires are the field reprieve —
            // free rest, gated only on nearby hostiles (RestSystem).
            if (e.ID == "GetInventoryActions")
            {
                var queryingActor = e.GetParameter<Entity>("Actor");
                var queryingZone = e.GetParameter<Zone>("Zone") ?? queryingActor?.SpatialZone ?? SettlementRuntime.ActiveZone;
                CampfireWorkActions.AddActions(queryingActor, this, queryingZone, e.GetParameter<InventoryActionList>("Actions"));
                ReagentFireFuel.AddActions(queryingActor, this, queryingZone, e.GetParameter<InventoryActionList>("Actions"));
                PortableKindling.AddActions(queryingActor, ParentEntity, queryingZone, e.GetParameter<InventoryActionList>("Actions"));
                if (!AllowRest || !WorldResourceActions.Nearby(queryingActor, ParentEntity, queryingZone)) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Rest", "rest", "RestAtCampfire", 'r', 20);
                actions?.AddAction("RestNextBand", "rest until the next time of day", "RestUntilNextBand", 's', 19);
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (ReagentFireFuel.IsCommand(command))
                {
                    if (!ReagentFireFuel.TryAct(e.GetParameter<Entity>("Actor"), this, e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone,
                        command, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                    e.Handled = true; return false;
                }
                if (CampfireWorkActions.IsCommand(command))
                {
                    if (!CampfireWorkActions.TryAct(e.GetParameter<Entity>("Actor"), this, e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone,
                        command, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                    e.Handled = true; return false;
                }
                if (command != "RestAtCampfire" && command != "RestUntilNextBand") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;
                if (!AllowRest)
                {
                    MessageLog.Add("This is not a resting place.");
                    CavesOfOoo.Diagnostics.Diag.Record("furniture", "RestBlocked", actor: actor,
                        target: ParentEntity, payload: new { site = "campfire", reason = "rest_not_offered" });
                    return true;
                }

                Zone zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
                if (!WorldResourceActions.Nearby(actor, ParentEntity, zone) || ParentEntity.GetPart<CampfirePart>() != this
                    || ParentEntity.GetPart<PhysicsPart>().Takeable)
                {
                    CavesOfOoo.Diagnostics.Diag.Record("furniture", "RestBlocked", actor: actor, target: ParentEntity,
                        payload: new { site = "campfire", reason = "invalid_rest_context" });
                    return true;
                }
                if (command == "RestUntilNextBand")
                {
                    if (SpatialQuery.Distance(zone, actor, ParentEntity) > 1)
                    {
                        MessageLog.Add("Stand beside the fire to rest.");
                        CavesOfOoo.Diagnostics.Diag.Record("furniture", "RestBlocked", actor: actor,
                            payload: new { site = "campfire", reason = "out_of_reach" });
                        return true;
                    }
                    if (!RestSystem.TryRestUntilNextBandWithTransaction(actor, zone, "campfire", out _, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                }
                else if (!RestSystem.TryRestWithTransaction(actor, zone, "campfire", out _, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true;
                return false;
            }

            return true;
        }

        private bool HandleRender(GameEvent e)
        {
            // A finite fire is drawn as its own art (the coals) and keeps no flicker, except the camp hearth: it burns in fire colors only while it is actually burning, so a lit
            // hearth looks lit and a cold one stays dark.
            if (FiniteCooking && !(ParentEntity?.GetPart<HearthGlowPart>()?.Burning() ?? false)) return true;
            _renderFrameCounter++;
            e.SetParameter("ColorString", _renderFrameCounter % 6 == 0 ? "&Y" : "&R");
            return true;
        }

        private bool HandleEndTurn(GameEvent e)
        {
            if (FiniteCooking || _proximityMessageShown || ParentEntity == null)
                return true;

            Zone zone = SettlementRuntime.ActiveZone;
            if (zone == null)
                return true;

            Cell fireCell = zone.GetEntityCell(ParentEntity);
            if (fireCell == null)
                return true;

            Entity player = FindPlayer(zone);
            if (player == null)
                return true;

            Cell playerCell = zone.GetEntityCell(player);
            if (playerCell == null)
                return true;

            if (IsAdjacent(fireCell.X, fireCell.Y, playerCell.X, playerCell.Y))
            {
                _proximityMessageShown = true;
                MessageLog.Add("The campfire crackles warmly.");
            }

            return true;
        }

        // Focused description only: no action gathering, callbacks, hidden/remote
        // state, heat change or fuel use. Readiness does not claim active burning.
        internal string DescribeFiniteCooking(Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            if (!FiniteCooking || owner == null || !ReferenceEquals(owner.GetPart<CampfirePart>(), this)
                || zone == null || !ReferenceEquals(SettlementRuntime.ActiveZone, zone)
                || cell == null || cell.ParentZone != zone || !cell.IsVisible
                || owner.SpatialZone != zone || !ReferenceEquals(zone.GetEntityCell(owner), cell)
                || !cell.Objects.Contains(owner)) return null;
            var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible
                || physics == null || physics.ParentEntity != owner
                || physics.InInventory != null || physics.Equipped != null) return null;
            var thermal = owner.GetPart<ThermalPart>();
            var fuel = owner.GetPart<FuelPart>();
            if (thermal == null || thermal.ParentEntity != owner || fuel == null || fuel.ParentEntity != owner
                || float.IsNaN(thermal.Temperature) || float.IsInfinity(thermal.Temperature)
                || float.IsNaN(fuel.FuelMass) || float.IsInfinity(fuel.FuelMass))
                return "Cooking: unavailable; usable heat or fuel is missing.";
            if (thermal.Temperature < CookingService.MinimumFiniteCookingTemperature)
                return "Cooking: no longer hot enough to prepare food.";
            if (fuel.FuelMass <= 0)
                return "Cooking: still hot, but no usable cooking fuel remains.";
            return "Cooking: enough stored heat remains. Use carried raw food's Cook action while beside this place.";
        }

        public void ResetProximityMessage()
        {
            _proximityMessageShown = false;
        }

        private static Entity FindPlayer(Zone zone)
        {
            if (zone == null)
                return null;

            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].HasTag("Player"))
                    return entities[i];
            }
            return null;
        }

        private static bool IsAdjacent(int x1, int y1, int x2, int y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx <= 1 && dy <= 1 && (dx + dy > 0);
        }
    }
}
