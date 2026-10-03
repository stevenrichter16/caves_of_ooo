using System;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    public enum SpreadHuntPhase { Watching, Pursuing, Searching, Escaped, Exhausted, Aborted, PreyGone, Feeding, Fed }
    public enum SpreadMeatDiversionPhase { None, Approaching, Feeding, Consumed, Aborted }
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
        // Fresh admission opts in. Missing legacy fields retain false/None.
        public bool MeatDiversionEnabled, MeatDiversionAttempted;
        public SpreadMeatDiversionPhase MeatDiversionPhase;
        public Entity MeatDiversionTarget;
        public string MeatDiversionTargetID;
        public int MeatDiversionX, MeatDiversionY, MeatApproachRemaining, MeatFeedProgress;
        public const int MeatNoticeRadius = 6, MaximumMeatApproachActions = 6, MeatFeedingProgressActions = 2;
        private const int MaximumMeatPathNodes = (MeatNoticeRadius * 2 + 1) * (MeatNoticeRadius * 2 + 1);
        private bool meatActionInProgress;

        /// <summary>Admit one original local pair. Cold generation may leave
        /// CurrentZone null; any foreign context is refused. Live actions require
        /// both brains wired to this exact zone. This never alters grazer food.</summary>
        public bool Configure(Zone zone, Entity prey, bool enableMeatDiversion = false)
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
            MeatDiversionEnabled = enableMeatDiversion;
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
            if (MeatActive) EndMeatDiversion(false, reason);
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
            if (meatActionInProgress) return true;
            if (!Configured || Terminal) return false;
            if (ParentEntity?.GetPart<SpreadPredatorPart>() != this || brain?.ParentEntity != ParentEntity) return true;
            if (!Eligible(ParentEntity, zone, out var actual) || actual != brain || ZoneID != zone.ZoneID)
            { Finish(SpreadHuntPhase.Aborted, "actor-context-invalid"); return false; }
            if (!ValidSavedBounds(zone)) { Finish(SpreadHuntPhase.Aborted, "saved-bounds-invalid"); return true; }
            // Normal immediate danger retains existing acquisition/flee. This
            // branch takes no hunt/feed action and never consumes its allowance.
            var hostile = SpreadActorContext.Nearest(ParentEntity, zone, Math.Min(brain.SightRadius, 20),
                e => e != Prey && FactionManager.IsHostile(ParentEntity, e));
            if (hostile != null || (brain.Target != null && brain.Target != Prey))
            { if (MeatActive) EndMeatDiversion(false, "threat-priority"); return false; }
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
            if (TryMeatDiversion(brain, zone)) return true;
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
        bool MeatActive => MeatDiversionPhase == SpreadMeatDiversionPhase.Approaching
            || MeatDiversionPhase == SpreadMeatDiversionPhase.Feeding;

        // Higher goals skip BoredGoal. Observe their ordinary action boundary
        // without executing another action or changing anyone else's goal stack.
        public override bool HandleEvent(GameEvent e)
        {
            if (!meatActionInProgress && MeatActive && (e.ID == "BeginTakeAction" || e.ID == "TakeTurn" || e.ID == "Died"))
            {
                var zone = ParentEntity?.SpatialZone;
                var brain = ParentEntity?.GetPart<BrainPart>();
                if (e.ID == "Died" || !MeatDiversionEnabled || !Configured || Terminal
                    || !Eligible(ParentEntity, zone, out var actual) || actual != brain || ZoneID != zone?.ZoneID
                    || brain.InConversation || brain.HasGoalOtherThan("BoredGoal")
                    || (brain.Target != null && brain.Target != Prey))
                    EndMeatDiversion(false, "behavior-priority");
            }
            return true;
        }
        public override void Remove()
        { if (MeatActive) EndMeatDiversion(false, "role-removed"); }

        void MeatRecord(string kind, string reason, Entity target)
        {
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", kind, ParentEntity, target, payload: new
            { reason, phase = MeatDiversionPhase.ToString(), remainingApproach = MeatApproachRemaining,
                feedProgress = MeatFeedProgress, attempted = MeatDiversionAttempted });
        }
        void EndMeatDiversion(bool consumed, string reason)
        {
            var target = MeatDiversionTarget;
            MeatDiversionAttempted = true;
            MeatDiversionPhase = consumed ? SpreadMeatDiversionPhase.Consumed : SpreadMeatDiversionPhase.Aborted;
            MeatDiversionTarget = null;
            MeatRecord("SpreadMeatDiversionOutcome", reason, target);
        }
        bool ValidMeatState(Zone zone)
        {
            if (MeatApproachRemaining < 0 || MeatApproachRemaining > MaximumMeatApproachActions
                || MeatFeedProgress < 0 || MeatFeedProgress > MeatFeedingProgressActions) return false;
            if (MeatDiversionPhase == SpreadMeatDiversionPhase.None)
                return !MeatDiversionAttempted && MeatDiversionTarget == null && MeatFeedProgress == 0;
            if (MeatDiversionPhase == SpreadMeatDiversionPhase.Consumed || MeatDiversionPhase == SpreadMeatDiversionPhase.Aborted)
                return MeatDiversionAttempted && MeatDiversionTarget == null;
            return MeatActive && MeatDiversionAttempted && zone.InBounds(MeatDiversionX, MeatDiversionY)
                && MeatDiversionTarget != null && MeatDiversionTarget.ID == MeatDiversionTargetID;
        }
        bool SafeMeatCell(Zone zone, Cell cell)
        {
            if (cell == null || AIHelpers.ChebyshevDistance(HomeX, HomeY, cell.X, cell.Y) > Leash) return false;
            var tile = zone.TileState.Get(cell.X, cell.Y);
            if (tile != null && (tile.Heat > 0 || tile.Cold > 0 || tile.Charge > 0
                || !string.IsNullOrEmpty(tile.Cloud) || tile.Coatings.Count > 0)) return false;
            foreach (var owner in cell.Occupants)
                if (owner.HasPart<TriggerOnStepPart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>()
                    || owner.HasEffect<BurningEffect>() || owner.GetPart<ThermalPart>()?.IsAflame == true) return false;
            return true;
        }
        bool PublicMeat(Entity item, BrainPart brain, Zone zone, bool bound)
        {
            if (!SpreadActorContext.Ground(item, zone) || (item.BlueprintName != "RawMeat" && item.BlueprintName != "DriedMeat")
                || !item.HasTag("Item") || item.HasTag("Creature") || item.HasTag("Solid")
                || item.HasTag("Owned") || item.HasTag("Quest") || item.HasTag("QuestItem") || item.HasTag("Unique")
                || item.HasTag("Essential") || item.HasTag("NoTake") || item.HasTag("NoTrade")
                || item.Properties.ContainsKey("Owner") || item.Properties.ContainsKey("OwnerID") || item.Properties.ContainsKey("QuestID")
                || item.HasPart<SpatialFootprintPart>() || item.HasPart<ContainerPart>()
                || item.GetPart<ReserveYieldPart>()?.Released == false) return false;
            var physics = item.GetPart<PhysicsPart>(); var stack = item.GetPart<StackerPart>();
            var food = item.GetPart<FoodPart>(); var handling = item.GetPart<HandlingPart>();
            if (!physics.Takeable || physics.Solid || food?.ParentEntity != item
                || (stack != null && (stack.ParentEntity != item || stack.StackCount <= 0))
                || (handling != null && (handling.ParentEntity != item || !handling.Carryable))) return false;
            var there = zone.GetEntityCell(item); var here = zone.GetEntityCell(ParentEntity);
            return here != null && SafeMeatCell(zone, there)
                && (!bound || (item == MeatDiversionTarget && item.ID == MeatDiversionTargetID
                    && there.X == MeatDiversionX && there.Y == MeatDiversionY))
                && AIHelpers.ChebyshevDistance(here.X, here.Y, there.X, there.Y) <= Math.Min(brain.SightRadius, MeatNoticeRadius)
                && AIHelpers.HasLineOfSight(zone, here.X, here.Y, there.X, there.Y);
        }
        FindPath MeatPath(Zone zone, Entity target)
        {
            var here = zone.GetEntityCell(ParentEntity); var there = zone.GetEntityCell(target);
            if (here == null || there == null) return null;
            var path = FindPath.Search(zone, here.X, here.Y, there.X, there.Y, maxNodes: MaximumMeatPathNodes,
                actor: ParentEntity, contactTarget: target);
            if (!path.Usable) return null;
            int x = here.X, y = here.Y;
            foreach (var step in path.Steps)
            { x += step.dx; y += step.dy; if (!SafeMeatCell(zone, zone.GetCell(x, y))) return null; }
            return path;
        }
        bool TryMeatDiversion(BrainPart brain, Zone zone)
        {
            if (!MeatDiversionEnabled) return false;
            if (!ValidMeatState(zone))
            {
                MeatApproachRemaining = MeatFeedProgress = 0;
                EndMeatDiversion(false, "invalid-saved-state"); return true;
            }
            if (MeatDiversionAttempted && !MeatActive) return false;
            meatActionInProgress = true;
            try
            {
                if (!MeatDiversionAttempted)
                {
                    var here = zone.GetEntityCell(ParentEntity); int radius = Math.Min(brain.SightRadius, MeatNoticeRadius);
                    Entity best = null; int distance = int.MaxValue;
                    for (int y = Math.Max(0, here.Y - radius); y <= Math.Min(Zone.Height - 1, here.Y + radius); y++)
                        for (int x = Math.Max(0, here.X - radius); x <= Math.Min(Zone.Width - 1, here.X + radius); x++)
                            foreach (var item in zone.GetCell(x, y).Objects)
                            {
                                int d = AIHelpers.ChebyshevDistance(here.X, here.Y, x, y);
                                if (d > distance || (d == distance && best != null && string.CompareOrdinal(item.ID, best.ID) >= 0)
                                    || !PublicMeat(item, brain, zone, false) || (d > 1 && MeatPath(zone, item) == null)) continue;
                                best = item; distance = d;
                            }
                    if (best == null) return false;
                    var at = zone.GetEntityCell(best);
                    MeatDiversionAttempted = true; MeatDiversionTarget = best; MeatDiversionTargetID = best.ID;
                    MeatDiversionX = at.X; MeatDiversionY = at.Y;
                    MeatApproachRemaining = MaximumMeatApproachActions; MeatFeedProgress = 0;
                    MeatDiversionPhase = SpreadMeatDiversionPhase.Approaching;
                    MeatRecord("SpreadMeatDiversionAdmission", "visible-public-meat", best);
                }
                if (!PublicMeat(MeatDiversionTarget, brain, zone, true))
                { EndMeatDiversion(false, "meat-no-longer-current"); return true; }
                var meat = MeatDiversionTarget; var cell = zone.GetEntityCell(meat);
                if (SpatialQuery.Distance(zone, ParentEntity, meat) > 1)
                {
                    if (MeatApproachRemaining == 0) { EndMeatDiversion(false, "approach-spent"); return true; }
                    MeatApproachRemaining--; brain.CurrentState = AIState.Chase;
                    var path = MeatPath(zone, meat);
                    if (path != null && path.Steps.Count > 0)
                        MovementSystem.TryMoveDetailed(ParentEntity, zone, path.Steps[0].dx, path.Steps[0].dy);
                    if (MeatApproachRemaining == 0 && SpatialQuery.Distance(zone, ParentEntity, meat) > 1)
                        EndMeatDiversion(false, "approach-spent");
                    return true;
                }
                brain.CurrentState = AIState.Idle; MeatDiversionPhase = SpreadMeatDiversionPhase.Feeding;
                if (MeatFeedProgress < MeatFeedingProgressActions)
                {
                    MeatFeedProgress++;
                    MeatRecord("SpreadMeatDiversionProgress", "feeding", meat);
                    EntityVisualHooks.EmitInteraction(ParentEntity, meat, zone);
                    return true;
                }
                // Spend the commitment before publishing any removal/dirty hook.
                MeatDiversionPhase = SpreadMeatDiversionPhase.Consumed; MeatDiversionTarget = null;
                var stack = meat.GetPart<StackerPart>();
                bool consumed = stack != null && stack.StackCount > 1;
                if (consumed) stack.StackCount--;
                else consumed = zone.RemoveEntity(meat);
                if (!consumed) MeatDiversionPhase = SpreadMeatDiversionPhase.Aborted;
                else ZoneRenderHooks.MarkCellDirty(cell, "FurrowstalkerMeatConsumed");
                MeatRecord("SpreadMeatDiversionOutcome", consumed ? "one-meat-consumed" : "meat-removal-refused", meat);
                return true;
            }
            finally { meatActionInProgress = false; }
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
            if (MeatDiversionEnabled && MeatActive && ValidMeatState(zone) && PublicMeat(MeatDiversionTarget, brain, zone, true))
                return MeatDiversionPhase == SpreadMeatDiversionPhase.Feeding
                    ? "It crouches over the meat, feeding a little at a time."
                    : "It noses toward meat left on the ground.";
            if (MeatDiversionEnabled && MeatDiversionPhase == SpreadMeatDiversionPhase.Consumed
                && IsCurrentPair(Prey?.GetPart<SpreadGrazerPart>(), zone))
                return "It has finished the offered meat and turned back to its hunt.";
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
