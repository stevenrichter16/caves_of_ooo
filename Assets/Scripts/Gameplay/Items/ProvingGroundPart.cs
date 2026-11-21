using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The proving post (Docs/MACHINE-PROOFING.md, hand-testing): a test bench that only a scenario stands in the world. Beside it you choose an enemy that does an element's damage and it is called a few
    /// cells away, hostile to you and in the turn order like any creature (each one stamped, so only what the post called is sent away); you can send them all away, and mend yourself (full health,
    /// harmful statuses cleared, helpful ones such as a coat kept) between trials. It is stateless: what it called is read off the zone, so it needs nothing saved and adds nothing to the ordinary game.
    /// </summary>
    public sealed class ProvingGroundPart : Part, IDescribeState
    {
        public override string Name => "ProvingGround";

        public const string Mark = "ProvingGroundCall", CallPrefix = "CallProvingFoe|", Mend = "MendProvingSubject", Dismiss = "DismissProvingFoes";
        public const int MaxCalled = 6, MinDistance = 3, MaxDistance = 9;

        /// <summary>The enemies it can call: blueprint, element id (a <see cref="ProofingRegistry"/> id), the name the menu shows and what they do. Each element has two, one a caster and one that fights close.</summary>
        public static readonly (string Blueprint, string Element, string Name, string Does)[] Foes =
        {
            ("MarlbackCindercaller", "heat", "Marlback cindercaller", "spits embers"), ("CharredHusk", "heat", "charred husk", "burns what it touches"),
            ("IceWight", "cold", "ice wight", "throws ice lances"), ("RuneCultist", "cold", "rune cultist", "calls frost"),
            ("MarlbackStormbinder", "electric", "Marlback stormbinder", "arcs lightning"), ("VaultSentinel", "electric", "vault sentinel", "shocks"),
            ("MarlbackSoursprayer", "acid", "Marlback soursprayer", "sprays acid"), ("CaveSlime", "acid", "cave slime", "corrodes"),
        };

        static string Label(string element) => ProofingRegistry.ById(element)?.Label ?? element;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;
        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone; int called = CalledIn(zone).Count;
            return "Stand beside it to call an enemy that burns, freezes, shocks or corrodes, send them away, or mend yourself. " + (called == 0 ? "None are called." : called + " called.");
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == null || !(command.StartsWith(CallPrefix, StringComparison.Ordinal) || command == Mend || command == Dismiss)) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Act(actor, zone, command)) { e.Handled = true; return false; }
            }
            return true;
        }

        static List<Entity> CalledIn(Zone zone)
        {
            var list = new List<Entity>(); if (zone == null) return list;
            foreach (var e in zone.GetAllEntities()) if (e != null && e.GetProperty(Mark) == "1") list.Add(e);
            return list;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            int called = CalledIn(zone).Count;
            for (int i = 0; i < Foes.Length; i++)
            {
                var foe = Foes[i]; string label = "call a " + foe.Name + " (" + Label(foe.Element) + ": " + foe.Does + ")";
                if (called >= MaxCalled) actions.AddDisabled("ProvingGround", label, "too-many", MaxCalled + " are already called; send some away first.", i == 0 ? 'c' : '\0', 20 - i);
                else actions.AddActionWithHint("ProvingGround", label, CallPrefix + foe.Blueprint, "Calls it a few cells away, hostile to you. Test your " + Label(foe.Element) + " proofing against it.", i == 0 ? 'c' : '\0', 20 - i, false, ActionTime.Free);
            }
            actions.AddActionWithHint("ProvingGround", "mend yourself (full health, harmful statuses cleared)", Mend, "Restores your health and clears freezing, burning, shock, corrosion and the like. A coat or a lasting proofing stays.", 'm', 8, false, ActionTime.Free);
            if (called == 0) actions.AddDisabled("ProvingGround", "send away everything called", "none-called", "Nothing has been called.", 'x', 7);
            else actions.AddActionWithHint("ProvingGround", "send away everything called (" + called + ")", Dismiss, "Removes what this post called and nothing else.", 'x', 7, false, ActionTime.Free);
        }

        bool Act(Entity actor, Zone zone, string command)
        {
            if (actor == null || zone == null || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return false;
            if (command == Mend) return MendSubject(actor);
            if (command == Dismiss) return DismissAll(zone);
            string blueprint = command.Substring(CallPrefix.Length); var foe = Foes.FirstOrDefault(f => f.Blueprint == blueprint);
            if (foe.Blueprint == null || CalledIn(zone).Count >= MaxCalled) return false;
            return CallOne(actor, zone, foe.Blueprint, foe.Name);
        }

        bool CallOne(Entity actor, Zone zone, string blueprint, string name)
        {
            var factory = HarvestablePart.Factory ?? MaterialReactionResolver.Factory; var creature = factory?.CreateEntity(blueprint); if (creature == null) return false;
            var at = zone.GetEntityCell(actor); if (at == null) return false;
            for (int r = MinDistance; r <= MaxDistance; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                        int x = at.X + dx, y = at.Y + dy; if (!zone.InBounds(x, y)) continue; var cell = zone.GetCell(x, y); if (cell == null || !FreeAndPlain(cell)) continue;
                        var brain = creature.GetPart<BrainPart>(); if (brain != null) { brain.CurrentZone = zone; brain.Rng = new Random(); brain.StartingCellX = x; brain.StartingCellY = y; }
                        if (!zone.AddEntity(creature, x, y)) continue;
                        creature.Properties[Mark] = "1"; brain?.SetPersonallyHostile(actor); TurnManager.Active?.AddEntity(creature);
                        MessageLog.Add("A " + name + " answers the post.");
                        Diag.Record("event", "ProvingFoeCalled", actor, creature, new { blueprint, x, y });
                        return true;
                    }
            MessageLog.Add("There is no clear ground near enough to call it."); return false;
        }

        /// <summary>A cell with nothing solid, no creature and no hazard terrain, so the foe stands on plain ground.</summary>
        static bool FreeAndPlain(Cell cell)
        {
            foreach (var o in cell.Objects)
            {
                if (o == null) continue;
                if (o.HasTag("Creature") || o.GetPart<PhysicsPart>()?.Solid == true || o.HasPart<TileStateSourcePart>() || o.HasPart<LiquidPoolPart>()) return false;
            }
            return true;
        }

        static bool DismissAll(Zone zone)
        {
            var called = CalledIn(zone); foreach (var e in called) { TurnManager.Active?.RemoveEntity(e); zone.RemoveEntity(e); }
            MessageLog.Add(called.Count == 0 ? "Nothing was called." : "You send " + called.Count + " away."); return true;
        }

        static readonly Type[] HarmfulStatuses = { typeof(FrozenEffect), typeof(BurningEffect), typeof(ElectrifiedEffect), typeof(AcidicEffect), typeof(StunnedEffect), typeof(PoisonedEffect), typeof(BleedingEffect), typeof(SmolderingEffect), typeof(CharredEffect) };

        static bool MendSubject(Entity actor)
        {
            var hp = actor.GetStat("Hitpoints"); if (hp != null) hp.BaseValue = hp.Max;
            var status = actor.GetPart<StatusEffectsPart>();
            if (status != null) foreach (var effect in status.GetAllEffects().ToList()) if (HarmfulStatuses.Contains(effect.GetType()) || (effect.GetEffectType() & Effect.TYPE_NEGATIVE) != 0) status.RemoveEffect(effect);
            MessageLog.Add("You are mended."); return true;
        }
    }
}
