using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Finite newly-poured water cools an owned hearth through ApplyHeat.
    /// The dose is paid before commit; only surplus becomes recoverable ground liquid.
    /// See Docs/FINITE-HEARTH-COOLING.md for the controlled evaporation tariff.</summary>
    public static class FireDouse
    {
        public const float DegreesPerUnit = 40f;

        /// <summary>True for a liquid that can put a fire out: one that is not itself fuel.</summary>
        public static bool Douses(string liquidId)
        {
            var def = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(liquidId) : null;
            if (def == null || def.Combustibility > 0) return false;
            // Water and the liquids that behave as water on the ground (bog mire, brine). A liquid that does not burn but is not water-like (lava, acid) is not a way to put a fire out.
            return def.Id == "water" || def.GroundReactionFamily == "water";
        }

        /// <summary>The player's own lit hearth standing in the cell, if any.</summary>
        public static Entity LitHearthAt(Zone zone, int x, int y)
        {
            if (zone == null || !zone.InBounds(x, y)) return null;
            foreach (var e in zone.GetOccupants(x, y))
            {
                if (e == null || e.HasTag("Creature") || !HeldFixtureActions.IsPlayerHeld(e)) continue;
                var fire = e.GetPart<CampfirePart>(); var heat = e.GetPart<ThermalPart>();
                var glow = e.GetPart<HearthGlowPart>();
                if (fire == null || !fire.FiniteCooking || heat == null || glow == null) continue;
                // Lit is what the hearth itself calls burning: hot enough to cook with fuel left. A spent hearth with coals still hot is already dark and is not "put out".
                if (glow.Burning()) return e;
            }
            return null;
        }

        /// <summary>A menu quote binds the displayed dose, not just the flask's volume.
        /// Exact references are checked again within the transaction; no heat changes
        /// until commit because ApplyHeat may have irreversible world consequences.</summary>
        internal sealed class Quote
        {
            internal readonly Entity Hearth;
            internal readonly int Used, Residual;
            internal readonly string Key;
            readonly Zone zone;
            readonly int x, y;
            readonly ThermalPart heat;
            readonly FuelPart fuel;
            readonly HearthGlowPart glow;
            readonly CampfirePart fire;
            readonly InstallablePart installation;
            readonly InstallationMarkerPart marker;
            readonly PhysicsPart physics;
            readonly Part[] parts;
            readonly float before, ambient, capacity, fuelBefore, joules;
            readonly string liquid;
            readonly int amount;
            internal Quote(Zone zone, int x, int y, Entity hearth, string liquid, int amount, int used, float joules)
            {
                this.zone=zone; this.x=x; this.y=y; Hearth=hearth; this.liquid=liquid; this.amount=amount;
                Used=used; Residual=amount-used; this.joules=joules;
                heat=hearth.GetPart<ThermalPart>(); fuel=hearth.GetPart<FuelPart>(); glow=hearth.GetPart<HearthGlowPart>();
                fire=hearth.GetPart<CampfirePart>(); installation=hearth.GetPart<InstallablePart>(); marker=hearth.GetPart<InstallationMarkerPart>();
                physics=hearth.GetPart<PhysicsPart>(); parts=hearth.Parts.ToArray();
                before=heat.Temperature; ambient=heat.AmbientTemperature; capacity=heat.HeatCapacity; fuelBefore=fuel.FuelMass;
                Key=string.Concat(new[] { hearth.ID, marker.InstallationId, marker.Origin, installation.Mode, installation.State,
                    before.ToString("R",CultureInfo.InvariantCulture), ambient.ToString("R",CultureInfo.InvariantCulture),
                    capacity.ToString("R",CultureInfo.InvariantCulture), fuelBefore.ToString("R",CultureInfo.InvariantCulture),
                    used.ToString(CultureInfo.InvariantCulture) }.Select(value => (value ?? "").Length.ToString(CultureInfo.InvariantCulture)+":"+value));
            }
            bool CurrentTarget()
            {
                return zone.GetEntityCell(Hearth)==zone.GetCell(x,y) && Hearth.GetPart<ThermalPart>()==heat && heat.ParentEntity==Hearth
                    && Hearth.GetPart<FuelPart>()==fuel && fuel.ParentEntity==Hearth && Hearth.GetPart<HearthGlowPart>()==glow && glow.ParentEntity==Hearth
                    && Hearth.GetPart<CampfirePart>()==fire && fire.ParentEntity==Hearth
                    && Hearth.GetPart<InstallablePart>()==installation && installation.ParentEntity==Hearth
                    && Hearth.GetPart<InstallationMarkerPart>()==marker && marker.ParentEntity==Hearth
                    && Hearth.GetPart<PhysicsPart>()==physics && physics?.ParentEntity==Hearth
                    && physics.InInventory==null && physics.Equipped==null && HeldFixtureActions.IsPlayerHeld(Hearth);
            }
            internal bool Current()
            {
                return CurrentTarget() && Hearth.Parts.SequenceEqual(parts) && parts.All(p=>p.ParentEntity==Hearth)
                    && physics!=null && physics.InInventory==null && physics.Equipped==null
                    && heat.Temperature==before && heat.AmbientTemperature==ambient && heat.HeatCapacity==capacity
                    && fuel.FuelMass==fuelBefore && TryQuote(zone,x,y,liquid,amount,out var now) && now?.Key==Key;
            }
            internal string Hint => (before + joules/(capacity>0?capacity:1) < CookingService.MinimumFiniteCookingTemperature
                ? "Puts out your hearth; keeps fuel. " : "Cools your hearth; burns lower. ")
                + Used + " evaporate; " + Residual + " recoverable (plus older water).";
            internal void Publish(Entity actor)
            {
                if (!Current()) { Diag.Record("furniture","HearthCoolingTargetChanged",actor,Hearth,new { liquid,amount,Used,Residual }); return; }
                var dose=GameEvent.New("ApplyHeat"); dose.SetParameter("Joules",(object)joules);
                dose.SetParameter("Radiant",(object)false); dose.SetParameter("Zone",(object)zone);
                try { Hearth.FireEvent(dose); }
                finally { dose.Release(); if (CurrentTarget()) glow.SyncLight(); }
                if(!CurrentTarget())
                {
                    MessageLog.Add("The cooling dose is spent; your hearth changed before its result could be checked.");
                    Diag.Record("furniture","HearthCoolingOutcomeChanged",actor,Hearth,new { liquid,amount,Used,Residual });
                    return;
                }
                bool cooled=WorldResourceActions.Finite(heat.Temperature) && heat.Temperature<before;
                bool extinguished=cooled && heat.Temperature<CookingService.MinimumFiniteCookingTemperature;
                MessageLog.Add(cooled ? (extinguished ? "The water hisses and your hearth goes out in a cloud of steam. It keeps its fuel." : "The water hisses on your hearth. It burns lower.")
                    : "The poured cooling dose is spent, but your hearth does not cool.");
                Diag.Record("furniture",cooled?"HearthDoused":"HearthCoolingRefused",actor,Hearth,
                    new { liquidId=liquid, amount, evaporated=Used, recoverable=Residual, before, after=heat.Temperature, fuel=fuel.FuelMass, extinguished });
            }
        }

        internal static bool TryQuote(Zone zone, int x, int y, string liquid, int amount, out Quote quote)
        {
            quote=null;
            if(amount<=0 || !Douses(liquid) || zone==null || !zone.InBounds(x,y)) return true;
            Entity hearth=null;
            foreach(var e in zone.GetOccupants(x,y))
            {
                if(e==null || e.HasTag("Creature") || !HeldFixtureActions.IsPlayerHeld(e)
                    || e.GetPart<CampfirePart>()?.FiniteCooking!=true || e.GetPart<HearthGlowPart>()?.Burning()!=true) continue;
                if(hearth!=null) return false;
                hearth=e;
            }
            if(hearth==null) return true;
            var heat=hearth.GetPart<ThermalPart>();
            if(!WorldResourceActions.Finite(heat.AmbientTemperature) || !WorldResourceActions.Finite(heat.HeatCapacity)) return false;
            double remaining=(double)heat.Temperature-heat.AmbientTemperature;
            if(remaining<=0) return true;
            int used=(int)Math.Min(amount,Math.Ceiling(remaining/DegreesPerUnit));
            double cool=Math.Min((double)used*DegreesPerUnit,remaining);
            double joules=-cool*(heat.HeatCapacity>0?heat.HeatCapacity:1d);
            if(double.IsNaN(joules) || double.IsInfinity(joules) || joules < -float.MaxValue || (float)joules==0f) return false;
            quote=new Quote(zone,x,y,hearth,liquid,amount,used,(float)joules);
            return true;
        }

        public static string Hint(Zone zone, int x, int y, string liquidId, int amount)
            => TryQuote(zone,x,y,liquidId,amount,out var quote) ? quote?.Hint : null;
    }
}
