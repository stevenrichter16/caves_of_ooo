using System;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    public enum SpreadHuntPhase { Watching, Pursuing, Searching, Escaped, Exhausted, Aborted, PreyGone, Feeding, Fed }
    /// <summary>One admitted local pair. This idle role never promotes its quarry
    /// into a generic KillGoal and never routes toward a hidden live coordinate.</summary>
    public sealed class SpreadPredatorPart : Part
    {
        public override string Name => "SpreadPredator";
        public bool Configured, HasLastSeen;
        public string ZoneID, PreyID;
        public Entity Prey;
        public int HomeX, HomeY, LastSeenX, LastSeenY;
        public int PursuitRemaining, SearchRemaining;
        public SpreadHuntPhase Phase;
        // Saved exact owner admitted only from our own native strike's fresh receipt.
        public Entity Corpse;
        public string CorpseID;
        public int CorpseX, CorpseY, FeedProgress;
        public const int FeedingProgressActions = 2;
        public const int MaximumPursuitActions = 24, MaximumSearchActions = 6, Leash = 12;

        /// <summary>Admit one original local pair. Cold generation may leave
        /// CurrentZone null; any foreign context is refused. Live actions require
        /// both brains wired to this exact zone. This never alters grazer food.</summary>
        public bool Configure(Zone zone, Entity prey)
        {
            if (Configured || ParentEntity?.GetPart<SpreadPredatorPart>() != this
                || ParentEntity.BlueprintName != "Furrowstalker" || prey?.BlueprintName != "ReedbackGrazer"
                || ReferenceEquals(ParentEntity, prey) || !Eligible(ParentEntity, zone, out var hunterBrain, allowUnwired: true)
                || !Eligible(prey, zone, out var preyBrain, allowUnwired: true) || hunterBrain.HasGoalOtherThan("BoredGoal")
                || preyBrain.HasGoalOtherThan("BoredGoal") || hunterBrain.Target != null || preyBrain.Target != null
                || ParentEntity.HasPart<SpreadGrazerPart>() || prey.HasPart<SpreadPredatorPart>()
                || BrainPart.ArePartyAligned(ParentEntity, prey)) return RejectAdmission("pair-ineligible", prey);
            var grazer = prey.GetPart<SpreadGrazerPart>();
            var at = zone.GetEntityCell(ParentEntity); var there = zone.GetEntityCell(prey);
            if (grazer == null || grazer.ParentEntity != prey || grazer.Hunter != null
                || AIHelpers.ChebyshevDistance(at.X, at.Y, there.X, there.Y) > Leash) return RejectAdmission("grazer-or-range-ineligible", prey);
            // No callback between admission and both saved reciprocal writes.
            Prey = prey; PreyID = prey.ID; ZoneID = zone.ZoneID; HomeX = at.X; HomeY = at.Y;
            PursuitRemaining = MaximumPursuitActions; SearchRemaining = MaximumSearchActions;
            Phase = SpreadHuntPhase.Watching; Configured = true;
            grazer.Hunter = ParentEntity; grazer.HuntZoneID = zone.ZoneID; grazer.FlightRemaining = 0;
            Record("SpreadHuntAdmission", "pair-admitted", prey);
            return true;
        }
        internal static bool Eligible(Entity actor, Zone zone, out BrainPart brain, bool allowUnwired = false)
        {
            if (!(SpreadActorContext.Actor(actor, zone, out brain) && (brain.CurrentZone == zone || (allowUnwired && brain.CurrentZone == null))
                && !CombatSystem.IsDeathHandled(actor) && brain.PartyLeader == null && brain.PartyMembers.Count == 0
                && !actor.HasTag("Player") && !actor.HasTag("Pet") && !actor.HasTag("Companion")
                && !actor.HasPart<TraderPart>() && !actor.HasPart<ConversationPart>()
                && !actor.HasPart<SpreadTerritoryPart>() && !actor.HasPart<SpreadCollectorPart>())) return false;
            for (int i = 0; i < actor.Parts.Count; i++)
                if (actor.Parts[i] is AIBehaviorPart) return false;
            return true;
        }
        internal bool IsCurrentPair(SpreadGrazerPart grazer, Zone zone)
        {
            return Configured && !Terminal && Phase != SpreadHuntPhase.Feeding && ValidSavedBounds(zone) && ZoneID == zone?.ZoneID
                && ParentEntity?.GetPart<SpreadPredatorPart>() == this && ParentEntity.BlueprintName == "Furrowstalker"
                && Eligible(ParentEntity, zone, out _) && Eligible(Prey, zone, out _)
                && Prey.ID == PreyID && Prey.BlueprintName == "ReedbackGrazer"
                && grazer != null && Prey.GetPart<SpreadGrazerPart>() == grazer && grazer.ParentEntity == Prey
                && grazer.Hunter == ParentEntity && grazer.HuntZoneID == ZoneID
                && !BrainPart.ArePartyAligned(ParentEntity, Prey);
        }
        bool ValidSavedBounds(Zone zone) => zone != null
            && PursuitRemaining >= 0 && PursuitRemaining <= MaximumPursuitActions
            && SearchRemaining >= 0 && SearchRemaining <= MaximumSearchActions
            && zone.InBounds(HomeX, HomeY)
            && (!HasLastSeen || zone.InBounds(LastSeenX, LastSeenY))
            && IsDefinedPhase(Phase);
        static bool IsDefinedPhase(SpreadHuntPhase phase)
        {
            // Explicit saved states avoid boxing/reflection on every pair check.
            switch (phase)
            {
                case SpreadHuntPhase.Watching:
                case SpreadHuntPhase.Pursuing:
                case SpreadHuntPhase.Searching:
                case SpreadHuntPhase.Escaped:
                case SpreadHuntPhase.Exhausted:
                case SpreadHuntPhase.Aborted:
                case SpreadHuntPhase.PreyGone:
                case SpreadHuntPhase.Feeding:
                case SpreadHuntPhase.Fed: return true;
                default: return false;
            }
        }
        bool Terminal => Phase == SpreadHuntPhase.Escaped || Phase == SpreadHuntPhase.Exhausted
            || Phase == SpreadHuntPhase.Aborted || Phase == SpreadHuntPhase.PreyGone || Phase == SpreadHuntPhase.Fed;
        bool RejectAdmission(string reason, Entity prey)
        {
            Record("SpreadHuntAdmission", reason, prey);
            return false;
        }
        void Record(string kind, string reason, Entity target)
        {
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", kind, ParentEntity, target, payload: new
                { reason, phase = Phase.ToString(), pursuitRemaining = PursuitRemaining,
                    searchRemaining = SearchRemaining, feedProgress = FeedProgress });
        }
        void Finish(SpreadHuntPhase outcome, string reason, bool record = true)
        {
            var target = Corpse ?? Prey;
            Phase = outcome;
            // Only this reciprocal pairing is ours to release; no goals,
            // personal hostility or unrelated food reservation is cleared.
            ReleasePrey();
            Prey = null; Corpse = null;
            if (record) Record("SpreadHuntOutcome", reason, target);
        }
        void ReleasePrey()
        {
            var grazer = Prey?.GetPart<SpreadGrazerPart>();
            if (grazer != null && grazer.ParentEntity == Prey && grazer.Hunter == ParentEntity)
            { grazer.Hunter = null; grazer.FlightRemaining = 0; }
        }
        internal bool TakeIdleAction(BrainPart brain, Zone zone)
        {
            if (!Configured || Terminal) return false;
            if (ParentEntity?.GetPart<SpreadPredatorPart>() != this || brain?.ParentEntity != ParentEntity) return true;
            if (!Eligible(ParentEntity, zone, out var actual) || actual != brain || ZoneID != zone.ZoneID)
            { Finish(SpreadHuntPhase.Aborted, "actor-context-invalid"); return false; }
            if (!ValidSavedBounds(zone)) { Finish(SpreadHuntPhase.Aborted, "saved-bounds-invalid"); return true; }
            // Normal immediate danger retains existing acquisition/flee. This
            // branch takes no hunt/feed action and never consumes its allowance.
            var hostile = SpreadActorContext.Nearest(ParentEntity, zone, Math.Min(brain.SightRadius, 20),
                e => e != Prey && FactionManager.IsHostile(ParentEntity, e));
            if (hostile != null || (brain.Target != null && brain.Target != Prey)) return false;
            int hp = ParentEntity.GetStatValue("Hitpoints"); int max = ParentEntity.GetStat("Hitpoints")?.Max ?? 0;
            var at = zone.GetEntityCell(ParentEntity);
            bool hurt = max > 0 && (float)hp / max < brain.FleeThreshold;
            if (hurt || AIHelpers.ChebyshevDistance(at.X, at.Y, HomeX, HomeY) > Leash)
            { Finish(SpreadHuntPhase.Aborted, hurt ? "health-retreat" : "outside-home-leash"); return true; }
            if (Phase == SpreadHuntPhase.Feeding) return Feed(brain, zone);
            if (!SpreadActorContext.Actor(Prey, zone, out _) || CombatSystem.IsDeathHandled(Prey))
            { Finish(SpreadHuntPhase.PreyGone, "prey-unavailable"); return true; }
            if (!IsCurrentPair(Prey.GetPart<SpreadGrazerPart>(), zone))
            { Finish(SpreadHuntPhase.Aborted, "pair-invalid"); return true; }
            if (PursuitRemaining <= 0) { Finish(SpreadHuntPhase.Exhausted, "pursuit-budget-spent"); return true; }
            PursuitRemaining--;
            var there = zone.GetEntityCell(Prey);
            bool visible = AIHelpers.ChebyshevDistance(at.X, at.Y, there.X, there.Y) <= Math.Min(brain.SightRadius, Leash)
                && AIHelpers.HasLineOfSight(zone, at.X, at.Y, there.X, there.Y);
            if (visible)
            {
                bool firstSight = !HasLastSeen, reacquired = Phase == SpreadHuntPhase.Searching;
                HasLastSeen = true; LastSeenX = there.X; LastSeenY = there.Y;
                SearchRemaining = MaximumSearchActions; Phase = SpreadHuntPhase.Pursuing; brain.CurrentState = AIState.Chase;
                if (firstSight || reacquired) Record("SpreadHuntTransition", firstSight ? "quarry-seen" : "quarry-reacquired", Prey);
                if (AIHelpers.ChebyshevDistance(at.X, at.Y, LastSeenX, LastSeenY) == 1)
                {
                    var quarry = Prey; var spawner = quarry.GetPart<CorpsePart>();
                    var previous = spawner?.CreatedCorpse;
                    int deathX = there.X, deathY = there.Y;
                    CombatSystem.PerformMeleeAttack(ParentEntity, quarry, zone, brain.Rng);
                    if (CombatSystem.IsDeathHandled(quarry))
                        BindOwnKill(brain, zone, quarry, spawner, previous, deathX, deathY);
                    return true;
                }
            }
            else
            {
                brain.CurrentState = AIState.Idle;
                if (!HasLastSeen) return true;
                bool lostSight = Phase != SpreadHuntPhase.Searching;
                Phase = SpreadHuntPhase.Searching;
                if (lostSight) Record("SpreadHuntTransition", "quarry-lost-sight", Prey);
                if (SearchRemaining <= 0) { Finish(SpreadHuntPhase.Escaped, "last-seen-search-spent"); return true; }
                SearchRemaining--;
            }
            if (at.X != LastSeenX || at.Y != LastSeenY)
                AIHelpers.TryApproachWithPathfinding(ParentEntity, zone, at.X, at.Y, LastSeenX, LastSeenY);
            // No child is pushed after a move. Hidden coordinates are not used.
            if (!visible && SearchRemaining <= 0) Finish(SpreadHuntPhase.Escaped, "last-seen-search-spent");
            return true;
        }
        void BindOwnKill(BrainPart brain, Zone zone, Entity quarry, CorpsePart spawner,
            Entity previous, int x, int y)
        {
            var created = spawner?.CreatedCorpse;
            if (ParentEntity?.GetPart<SpreadPredatorPart>() != this || !Configured || Terminal
                || Phase == SpreadHuntPhase.Feeding || Prey != quarry || quarry.ID != PreyID
                || !Eligible(ParentEntity, zone, out var current) || current != brain || ZoneID != zone.ZoneID
                || quarry.GetPart<CorpsePart>() != spawner || spawner?.ParentEntity != quarry
                || created == null || created == previous || !MealOwner(created, zone, x, y))
            { Finish(SpreadHuntPhase.PreyGone, "own-kill-no-valid-meal"); return; }
            ReleasePrey(); Prey = null;
            Corpse = created; CorpseID = created.ID; CorpseX = x; CorpseY = y;
            FeedProgress = 0; Phase = SpreadHuntPhase.Feeding;
            Record("SpreadHuntTransition", "own-kill-meal-claimed", created);
        }
        bool MealOwner(Entity meal, Zone zone, int x, int y)
        {
            if (!SpreadActorContext.Ground(meal, zone) || meal.BlueprintName != "ReedbackGrazerCorpse"
                || meal.HasTag("Creature") || !meal.HasTag("Corpse") || meal.HasPart<SpatialFootprintPart>()
                || meal.GetProperty("SourceBlueprint") != "ReedbackGrazer" || meal.GetProperty("SourceID") != PreyID
                || meal.GetProperty("KillerID") != ParentEntity.ID || meal.GetProperty("KillerBlueprint") != "Furrowstalker") return false;
            var cell = zone.GetEntityCell(meal); var at = zone.GetEntityCell(ParentEntity);
            return cell.X == x && cell.Y == y && at != null
                && AIHelpers.ChebyshevDistance(at.X, at.Y, x, y) <= 1;
        }
        bool CurrentMeal(Zone zone) => FeedProgress >= 0 && FeedProgress <= FeedingProgressActions
            && Corpse != null && Corpse.ID == CorpseID && MealOwner(Corpse, zone, CorpseX, CorpseY);
        bool Feed(BrainPart brain, Zone zone)
        {
            if (!CurrentMeal(zone)) { Finish(SpreadHuntPhase.Aborted, "meal-no-longer-current"); return true; }
            brain.CurrentState = AIState.Idle;
            var meal = Corpse;
            if (FeedProgress < FeedingProgressActions)
            {
                FeedProgress++;
                // Gesture follows committed progress while both exact physical
                // owners still exist. No consumption/callback-driven extra action.
                EntityVisualHooks.EmitInteraction(ParentEntity, meal, zone);
            }
            else
            {
                var cell = zone.GetEntityCell(meal);
                // Spend the claim before publishing removal dirtiness. This cannot
                // be repeated, and never creates replacement meat or restores HP.
                Finish(SpreadHuntPhase.Fed, "meal-consumed", record: false);
                if (!zone.RemoveEntity(meal)) Phase = SpreadHuntPhase.Aborted;
                else ZoneRenderHooks.MarkCellDirty(cell, "FurrowstalkerFed");
                Record("SpreadHuntOutcome", Phase == SpreadHuntPhase.Fed ? "meal-consumed" : "meal-removal-refused", meal);
            }
            return true;
        }
        /// <summary>Read-only local posture for the existing Look/examine reader.
        /// Never exposes quarry coordinates or promises a kill/harvest.</summary>
        public string DescribeState()
        {
            var zone = ParentEntity?.SpatialZone;
            if (!Configured || ParentEntity?.GetPart<SpreadPredatorPart>() != this
                || ParentEntity.BlueprintName != "Furrowstalker" || ZoneID != zone?.ZoneID
                || !Eligible(ParentEntity, zone, out var brain) || !ValidSavedBounds(zone)
                || brain.InConversation || brain.HasGoalOtherThan("BoredGoal")
                || (brain.Target != null && brain.Target != Prey)) return null;
            if (Phase == SpreadHuntPhase.Fed) return "It has finished feeding.";
            if (Terminal) return "It has abandoned this hunt and no longer follows the trail.";
            if (Phase == SpreadHuntPhase.Feeding) return CurrentMeal(zone) ? "It crouches over its own kill, feeding a little at a time. The remains can still be taken." : null;
            if (!IsCurrentPair(Prey?.GetPart<SpreadGrazerPart>(), zone)) return null;
            if (Phase == SpreadHuntPhase.Searching) return "It noses along a fading trail.";
            if (Phase == SpreadHuntPhase.Pursuing) return "It runs low to the ground after its quarry.";
            return "It watches from a low crouch, waiting for a nearby grazer to move.";
        }
    }
}
