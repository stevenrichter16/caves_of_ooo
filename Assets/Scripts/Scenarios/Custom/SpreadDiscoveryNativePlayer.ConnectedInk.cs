using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Actual learned rite, earned botanical ingredients, native desk
    /// preparation and re-inking. Never edits charges or invents a spell target.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        string _connectedBookId, _connectedInkBookId;
        int _connectedRimeCasts;
        static readonly string[] ConnectedInkChecks =
        {
            "connected_book_learned", "connected_first_lawful_rite", "connected_earned_ink_prepared",
            "connected_earned_vial_reinks_book", "connected_second_lawful_rite"
        };

        int ConnectedItemCount(string blueprint) => Player.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(Units);

        bool ConnectedRimeDirection(int x, int y, out int dx, out int dy, out List<Entity> targets)
        {
            var hostile = new HashSet<Entity>(Threats(Zone));
            foreach (var direction in Steps)
            {
                var found = SpellTargeting.GetCreaturesInCone(Zone, Player, x, y, direction.x, direction.y, 3);
                if (found.Count == 0 || found.Any(e => !hostile.Contains(e))) continue;
                dx = direction.x; dy = direction.y; targets = found; return true;
            }
            dx = dy = 0; targets = null; return false;
        }

        // The main journey calls this first beside the genuinely retrieved cellar
        // book while its original hostile guard is still alive. A later call uses
        // the same spell after useful travel and earned re-inking.
        IEnumerator ConnectedRimeCastOnHostile()
        {
            var book = Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == _connectedBookId);
            Require(book?.BlueprintName == "ShatteredRimeGrimoire" && Owns(Player, book), "actual earned rite book remains carried");
            var skills = Player.GetPart<SkillsPart>();
            Require(skills != null, "actual player skill authority");
            if (!skills.HasSkill("Rites_ShatteredRime"))
            {
                int tick = Tick, energy = Energy, sp = Player.GetStatValue("SP"), charges = book.GetPart<GrimoireChargePart>().Charges;
                yield return ItemAction(book, "ReadGrimoire"); yield return CloseNormal();
                Require(skills.HasSkill("Rites_ShatteredRime") && Player.GetStatValue("SP") == sp
                    && Tick == tick && Energy == energy && book.GetPart<GrimoireChargePart>().Charges == charges,
                    "native reading teaches the earned rite without granting ink, SP or a turn");
            }
            if (_connectedRimeCasts == 0) Check("connected_book_learned", skills.HasSkill("Rites_ShatteredRime") && Owns(Player, book));
            var abilities = Player.GetPart<ActivatedAbilitiesPart>();
            var ability = abilities?.AbilityList.SingleOrDefault(a => a.Command == "CommandShatteredRime");
            Require(ability != null && ability.CooldownRemaining == 0, "real rite ready after useful journey actions; no cooldown editing or waiting loop");
            int slot = Array.FindIndex(abilities.SlotAssignments, id => id == ability.ID);
            Require(slot >= 0 && slot < 10, "learned rite has an actual number-key binding");
            for (int step = 0; step < 80; step++)
            {
                if (ConnectedRimeDirection(At.X, At.Y, out int dx, out int dy, out var targets))
                {
                    var source = GrimoireInk.FindInked(Player);
                    Require(source != null && Owns(Player, source.ParentEntity), "actual any-inked-book cast authority");
                    var inkBefore = Player.GetPart<InventoryPart>().Objects.Where(e => e.HasPart<GrimoireChargePart>())
                        .ToDictionary(e => e, e => e.GetPart<GrimoireChargePart>().Charges);
                    var victims = targets.Select(e => new { id = e.ID, blueprint = e.BlueprintName, hp = e.GetStatValue("Hitpoints"), at = Zone.GetEntityPosition(e) }).ToArray();
                    int tick = Tick, energy = Energy; var origin = At; var zone = Zone;
                    yield return Tap(Shortcut(slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
                    Require(State == "AwaitingDirection" && Tick == tick && Energy == energy
                        && ConnectedRimeDirection(At.X, At.Y, out int freshX, out int freshY, out var freshTargets)
                        && freshX == dx && freshY == dy && freshTargets.SequenceEqual(targets), "native direction prompt preserves exact lawful target cone");
                    string marker = Mark("connected-rime-before-cast");
                    yield return Paid(Tap(Direction(dx, dy)), "local", "connected-native-rime-" + (_connectedRimeCasts + 1));
                    bool debit = inkBefore.All(pair => pair.Key.GetPart<GrimoireChargePart>().Charges
                        == pair.Value - (pair.Key == source.ParentEntity ? 1 : 0));
                    _connectedRimeCasts++;
                    if (_connectedRimeCasts == 1) _connectedInkBookId = source.ParentEntity.ID;
                    Check(_connectedRimeCasts == 1 ? "connected_first_lawful_rite" : "connected_second_lawful_rite",
                        debit && Zone == zone && At == origin && ability.CooldownRemaining > 0
                        && Window(marker).Any(e => e.Category == "spell" && e.Kind == "RiteCast" && e.ActorId == Player.ID));
                    _observations.Add(new { phase = "connected-native-rite", cast = _connectedRimeCasts,
                        learnedBook = book.ID, actualInkSupplier = source.ParentEntity.ID, supplierBlueprint = source.ParentEntity.BlueprintName,
                        before = inkBefore[source.ParentEntity], after = source.Charges, victims,
                        notes = "All initial cone targets were actual hostile creatures; empty cones cannot consume ink. No creature was created or made hostile for this audit." });
                    yield return Capture("connected-rime-" + _connectedRimeCasts); yield break;
                }
                var threats = Threats(Zone);
                Require(threats.Length > 0, "actual living hostile still exists for the bounded rite approach");
                var path = PathTo(c => threats.Any(e => SpatialQuery.DistanceToCell(Zone, e, c.X, c.Y) <= 5)
                    && ConnectedRimeDirection(c.X, c.Y, out _, out _, out _));
                Require(path != null && path.Count > 0, "native safe path reaches a cone with no neutral or allied victims");
                yield return StepTo(path[0].x, path[0].y);
            }
            throw new InvalidOperationException("Connected rite approach exceeded eighty ordinary steps; no target grant or AI suppression.");
        }

        IEnumerator ConnectedRimeOnVisitedSurface()
        {
            // Only locations physically entered earlier in this same native
            // journey qualify. Querying cached owners creates neither a site nor
            // a target; all travel and the actual hostile cone still use keys.
            var sites=_connectedVisitedSurfaces.Where(id=>Manager.CachedZones.TryGetValue(id,out var site)
                &&Threats(site).Length>0).OrderBy(id=>id==GleanersDistrict.SurfaceID?0:1)
                .ThenBy(id=>id,StringComparer.Ordinal).Take(4).ToArray();
            foreach(string id in sites)
            {
                yield return TravelSurface(id);
                var threats=Threats(Zone);if(threats.Length==0)continue;
                bool readyCone=ConnectedRimeDirection(At.X,At.Y,out _,out _,out _);
                var path=readyCone?null:PathTo(c=>threats.Any(e=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)<=5)
                    &&ConnectedRimeDirection(c.X,c.Y,out _,out _,out _));
                if(!readyCone&&(path==null||path.Count==0))
                { _notes.Add("RITE RETURN: existing hostile in visited "+id+" has no current conservative cone approach; no owner moved or replaced.");continue; }
                _notes.Add("RITE RETURN: original cellar guard died through ordinary play; revisiting actual hostile in previously entered "+id+".");
                yield return ConnectedRimeCastOnHostile();yield break;
            }
            throw new InvalidOperationException("No living reachable lawful rite target remains in the four previously visited surface sites; no new encounter or target is generated for the test.");
        }
        IEnumerator ConnectedInkJourney()
        {
            Require(_connectedRimeCasts == 1 && ConnectedItemCount("SootrootPulp") >= 2 && ConnectedItemCount("PitchpodResin") >= 1,
                "earned reserve/cellar products and actual first cast precede the botanical ink journey");
            yield return TravelSurface(MarrowstyeCompositionPlan.ZoneID);
            var desk = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "BotanicalInkDesk");
            yield return DistrictApproach(desk, 140);
            int roots = ConnectedItemCount("SootrootPulp"), resin = ConnectedItemCount("PitchpodResin"), vials = ConnectedItemCount("InkVial");
            int drams = TradeSystem.GetDrams(Player), workerDrams = TradeSystem.GetDrams(desk.GetPart<BotanicalInkDeskPart>().Worker);
            yield return Paid(WorldAction(desk, BotanicalInkDeskPart.PrepareCommand), "local", "connected-native-botanical-ink");
            Check("connected_earned_ink_prepared", ConnectedItemCount("SootrootPulp") == roots - 2
                && ConnectedItemCount("PitchpodResin") == resin - 1 && ConnectedItemCount("InkVial") == vials + 1
                && TradeSystem.GetDrams(Player) == drams - BotanicalInkDeskPart.Fee
                && TradeSystem.GetDrams(desk.GetPart<BotanicalInkDeskPart>().Worker) == workerDrams + BotanicalInkDeskPart.Fee);
            yield return Capture("connected-earned-botanical-ink");
            var book = Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == _connectedInkBookId);
            var ink = book.GetPart<GrimoireChargePart>(); int charge = ink.Charges;
            int gain = Math.Min(ink.ChargesPerVial, ink.MaxCharges - charge);
            Require(gain > 0 && Owns(Player, book), "the exact previously debited physical book has real missing charges");
            yield return Paid(ItemAction(book, GrimoireChargePart.ReinkCommand), "local", "connected-native-earned-reink");
            Check("connected_earned_vial_reinks_book", ink.Charges == charge + gain && ConnectedItemCount("InkVial") == vials
                && Owns(Player, book) && Player.GetPart<SkillsPart>().HasSkill("Rites_ShatteredRime"));
            _observations.Add(new { phase = "connected-native-reink", book = book.ID, blueprint = book.BlueprintName,
                before = charge, after = ink.Charges, gain, max = ink.MaxCharges, notes = "One earned vial consumed; restoration is capped by actual missing charges, not artificially drained to five." });
            yield return Capture("connected-book-reinked");

            bool cellarHasTarget=Manager.CachedZones.TryGetValue(GleanersCellarBuilder.ZoneID,out var cellar)
                &&Threats(cellar).Length>0;
            if(!cellarHasTarget)yield return ConnectedRimeOnVisitedSurface();
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            var down = Zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "stairs");
            yield return DistrictWalk(Zone.GetEntityCell(down), 100);
            yield return Paid(Tap(UnityEngine.InputSystem.Key.LeftShift, UnityEngine.InputSystem.Key.Period), "local", "connected-rite-return-cellar");
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "native return to the retained original cellar encounter");
            if(cellarHasTarget)yield return ConnectedRimeCastOnHostile();
            Require(_connectedRimeCasts==2,"earned re-inking supported an actual second lawful cast");
            // The owning journey observes and waters its retained crop here,
            // then leaves through the original stairs using normal input.
        }
    }
}
