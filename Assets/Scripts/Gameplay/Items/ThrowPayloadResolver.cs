using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One committed flask: a deduplicated direct dose, then bounded ground inputs.
    /// The caller has already committed custody/payment; exceptions are never swallowed here.</summary>
    public static class ThrowPayloadResolver
    {
        static readonly int[] OffsetX = { 0, 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] OffsetY = { 0, -1, -1, 0, 1, 1, 1, 0, -1 };
        public static bool HasTonicPayload(Entity item) => item?.GetPart<TonicPart>()?.HasThrowablePayload() == true;

        /// <summary>Item-only policy shared by Examine and the throw preview; no world queries.</summary>
        public static string DescribeGroundPolicy(Entity item)
        {
            if (!HasTonicPayload(item)) return "";
            var policy = GroundPolicy.For(item);
            var lines = new List<string>(4);
            if (policy.Exposure > 0)
                lines.Add(policy.Exposure + " contact units of " + policy.Liquid
                    + " film total, lasting up to " + FiniteLiquidFilmPart.LifetimeRounds
                    + " local turns. Each entry spends one; too little to collect. Blocked portions are lost.");
            if (policy.Heat) lines.Add("Adds 1 heat per splash cell for existing ground reactions.");
            if (policy.Cold) lines.Add("Adds 1 cold per splash cell for existing ground reactions.");
            if (policy.Charge) lines.Add("Adds 1 charge per splash cell for existing ground reactions.");
            if (policy.Heat || policy.Cold || policy.Charge)
                lines.Add("These inputs do not create a lasting trap on bare ground.");
            if (lines.Count == 0) return "Direct targets only; no ground residue.";
            return string.Join(" ", lines);
        }

        /// <summary>Stable center/clockwise footprint. Known-only never consults unseen owners.</summary>
        public static void CollectFootprint(Zone zone, Cell center, bool focused, List<Cell> into, bool knownOnly = false)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            if (zone == null || center?.ParentZone != zone) return;
            for (int i = 0; i < (focused ? 1 : 9); i++)
            {
                var cell = zone.GetCell(center.X + OffsetX[i], center.Y + OffsetY[i]);
                if (cell == null || Blocked(cell, knownOnly)) continue;
                if (OffsetX[i] != 0 && OffsetY[i] != 0
                    && Blocked(zone.GetCell(center.X + OffsetX[i], center.Y), knownOnly)
                    && Blocked(zone.GetCell(center.X, center.Y + OffsetY[i]), knownOnly)) continue;
                into.Add(cell);
            }
        }
        static bool Blocked(Cell cell, bool knownOnly)
        {
            if (cell == null) return true;
            if (knownOnly && (!cell.Explored || !cell.IsVisible)) return false;
            foreach (var owner in cell.Occupants)
            {
                if (owner == null || owner.HasTag("Creature")
                    || knownOnly && owner.GetPart<RenderPart>()?.Visible != true) continue;
                var profile = ThrowProfile.For(owner);
                if (profile.Intercepts && profile.Sealed) return true;
            }
            return false;
        }

        public static void Resolve(Entity actor, Entity item, Zone zone, Cell burstCell,
            Entity directImpact, Cell directContact, bool focused, Random rng, string throwSerial)
        {
            if (!HasTonicPayload(item) || zone == null || burstCell?.ParentZone != zone)
                throw new InvalidOperationException("A committed tonic needs its current payload and burst cell.");
            var policy = GroundPolicy.For(item);
            var cells = UnityEngine.Pool.ListPool<Cell>.Get();
            var owners = UnityEngine.Pool.ListPool<Entity>.Get();
            var seen = UnityEngine.Pool.HashSetPool<Entity>.Get();
            try
            {
                CollectFootprint(zone, burstCell, focused, cells);
                // Contact with a solid owner is independent of its approach-side floor burst.
                if (CurrentAt(directImpact, zone, directContact) && Eligible(directImpact, actor))
                { seen.Add(directImpact); owners.Add(directImpact); }
                foreach (var cell in cells)
                    foreach (var owner in cell.Occupants)
                        if (Current(owner, zone) && Eligible(owner, actor) && seen.Add(owner)) owners.Add(owner);

                int applied = 0;
                var tonic = item.GetPart<TonicPart>();
                foreach (var owner in owners)
                {
                    if (!Current(owner, zone)) continue;
                    bool atContact = owner == directImpact && CurrentAt(owner, zone, directContact);
                    if (!atContact && !TouchesAny(owner, zone, cells)) continue;
                    if (owner.HasTag("Creature")
                        ? tonic.ApplyTo(owner, actor, zone, rng, consumeItem: false, showUseMessage: false)
                        : ApplyScenery(item, owner, actor, zone)) applied++;
                }

                // Distribution follows the original logical slots. Occluded allocations are lost,
                // never shifted into a surviving cell after a wall or map edge clipped them.
                int deposited = 0;
                int slots = focused ? 1 : policy.Exposure;
                for (int i = 0; i < slots; i++)
                {
                    var cell = zone.GetCell(burstCell.X + OffsetX[i], burstCell.Y + OffsetY[i]);
                    if (cell == null || !cells.Contains(cell) || Blocked(cell, false)) continue;
                    if (policy.Exposure > 0)
                    {
                        int amount = focused ? policy.Exposure : 1;
                        FiniteLiquidFilmPart.Create(zone, cell, policy.Liquid, amount, actor?.ID, throwSerial);
                        deposited += amount;
                    }
                }
                foreach (var cell in cells)
                {
                    if (Blocked(cell, false)) continue;
                    if (policy.Heat) ZoneTileStateSystem.AddHeat(zone, cell.X, cell.Y, 1, actor, "ThrownTonic");
                    if (policy.Cold) ZoneTileStateSystem.AddCold(zone, cell.X, cell.Y, 1, actor, "ThrownTonic");
                    if (policy.Charge) ZoneTileStateSystem.AddCharge(zone, cell.X, cell.Y, 1, actor, "ThrownTonic");
                }
                if (deposited > 0 || policy.Heat || policy.Cold || policy.Charge)
                    ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
                Diag.Record("liquid", "ThrownPayloadResolved", actor, item,
                    new { throwSerial, focused, directTargets = applied, deposited, liquid = policy.Liquid,
                        heat = policy.Heat, cold = policy.Cold, charge = policy.Charge });
                string result = applied > 0 ? "splashing " + applied + " target" + (applied == 1 ? "" : "s")
                    : deposited > 0 ? "leaving a thin " + policy.Liquid + " film" : "with no direct effect";
                MessageLog.Add(item.GetDisplayName() + " shatters, " + result + ".");
            }
            finally
            {
                UnityEngine.Pool.HashSetPool<Entity>.Release(seen);
                UnityEngine.Pool.ListPool<Entity>.Release(owners);
                UnityEngine.Pool.ListPool<Cell>.Release(cells);
            }
        }
        static bool Eligible(Entity owner, Entity actor) => owner.HasTag("Creature") || AbilityTargeting.IsElementalTarget(owner, actor);
        static bool Current(Entity owner, Zone zone) => owner != null && owner.SpatialZone == zone
            && zone.GetEntityCell(owner)?.Objects.Contains(owner) == true && !CombatSystem.IsDeathHandled(owner)
            && owner.GetStatValue("Hitpoints", 1) > 0 && owner.GetPart<DestructiblePart>()?.Gone != true;
        static bool CurrentAt(Entity owner, Zone zone, Cell cell)
            => cell?.ParentZone == zone && Current(owner, zone) && cell.Occupants.Contains(owner);
        static bool TouchesAny(Entity owner, Zone zone, List<Cell> cells)
        {
            foreach (var occupied in zone.GetOccupiedCells(owner)) if (cells.Contains(occupied)) return true;
            return false;
        }
        static bool ApplyScenery(Entity item, Entity target, Entity source, Zone zone)
        {
            bool applied = false;
            var status = item.GetPart<StatusTonicPart>();
            if (status != null) applied |= ApplyElement(TonicEffectFactory.Create(status.EffectName,
                status.EffectDuration, status.EffectDamageDice, status.EffectMagnitude, source), target, source, zone);
            var brew = item.GetPart<BrewItemPart>();
            if (brew != null) foreach (var entry in brew.GetEffects())
                applied |= ApplyElement(TonicEffectFactory.Create(entry.Property, 0, "", entry.Potency, source), target, source, zone);
            return applied;
        }
        static bool ApplyElement(Effect effect, Entity target, Entity source, Zone zone)
        {
            if (!(effect is BurningEffect || effect is FrozenEffect || effect is WetEffect
                || effect is AcidicEffect || effect is ElectrifiedEffect || effect is CharredEffect)) return false;
            return ObjectStatusMatrix.Evaluate(effect, target) == ObjectStatusVerdict.Applies && target.ApplyEffect(effect, source, zone);
        }
        struct GroundPolicy
        {
            public string Liquid;
            public int Exposure;
            public bool Heat, Cold, Charge;
            float acid, water;
            public static GroundPolicy For(Entity item)
            {
                var result = new GroundPolicy();
                var status = item.GetPart<StatusTonicPart>();
                if (status != null) result.Add(status.EffectName, status.EffectMagnitude);
                var brew = item.GetPart<BrewItemPart>();
                if (brew != null) foreach (var entry in brew.GetEffects()) result.Add(entry.Property, entry.Potency);
                float magnitude = result.acid > 0 ? result.acid : result.water;
                result.Liquid = result.acid > 0 ? "acid" : result.water > 0 ? "water" : "";
                result.Exposure = magnitude > 0 ? Math.Max(1, Math.Min(9, (int)Math.Floor(9 * Math.Min(1, magnitude)))) : 0;
                return result;
            }
            void Add(string name, float magnitude)
            {
                // Canonical factory owns aliases; instant-only or unknown families deliberately
                // supply no ground matter. This queries a fresh effect without applying it.
                var effect = TonicEffectFactory.Create(name, 0, "", magnitude, null);
                float dose = float.IsNaN(magnitude) || float.IsInfinity(magnitude) || magnitude <= 0 ? 1 : magnitude;
                if (effect is AcidicEffect) acid = Math.Max(acid, dose);
                else if (effect is WetEffect) water = Math.Max(water, dose);
                else if (effect is BurningEffect) Heat = true;
                else if (effect is FrozenEffect) Cold = true;
                else if (effect is ElectrifiedEffect) Charge = true;
            }
        }
    }
}
