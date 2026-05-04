namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base for entities that fire a one-shot effect when a
    /// creature steps onto their cell. Mirrors Qud's
    /// <c>XRL.World.Parts.Tinkering_Mine</c> event handler
    /// (Tinkering_Mine.cs:428 — <c>HandleEvent(ObjectEnteredCellEvent E)</c>).
    ///
    /// <para><b>Subscription.</b> Listens for the <c>"EntityEnteredCell"</c>
    /// event fired by <see cref="MovementSystem.FireCellEnteredEvents"/> on
    /// every non-mover occupant of a destination cell after a successful
    /// move.</para>
    ///
    /// <para><b>Faction filter.</b> If <see cref="TriggerFaction"/> is set,
    /// actors whose faction string matches are ignored — this is how
    /// rune-laying NPCs avoid tripping their own runes. Null = trigger on
    /// anyone (including the player).</para>
    ///
    /// <para><b>Consumption.</b> When <see cref="ConsumeOnTrigger"/> is true
    /// (the default for single-use runes) the entity removes itself from the
    /// zone after dispatching <see cref="OnTrigger"/>. Snapshot iteration in
    /// <see cref="MovementSystem.FireCellEnteredEvents"/> makes this safe —
    /// the removal does not mutate the in-flight occupant list.</para>
    /// </summary>
    public abstract class TriggerOnStepPart : Part
    {
        /// <summary>
        /// Faction string of the entity that laid this trigger. Actors whose
        /// faction equals this are NOT triggered (prevents friendly-fire on
        /// laid runes). Null = everyone triggers. Mirrors Qud's
        /// <c>Tinkering_Mine.Owner</c> filter without needing an owner
        /// reference.
        /// </summary>
        public string TriggerFaction;

        /// <summary>
        /// When true, the trigger entity is removed from the zone after
        /// firing. Matches Qud's mines, which despawn on detonation.
        /// Defaults to true.
        /// </summary>
        public bool ConsumeOnTrigger = true;

        /// <summary>
        /// Subclass payload. Called once when a non-self, non-faction-mate
        /// entity steps onto this trigger's cell. <paramref name="zone"/> is
        /// the zone the trigger and actor share; never null.
        /// </summary>
        protected abstract void OnTrigger(Entity actor, Zone zone);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EntityEnteredCell")
                return true;

            var actor = e.GetParameter<Entity>("Actor");
            if (actor == null || actor == ParentEntity)
                return true;

            // Faction gate — prevents runes from triggering on their layer's
            // allies. Checked by tag string rather than FactionManager.GetFeeling
            // because mines/runes are dumb traps: they don't care about
            // feeling-graph transitive relationships, just "was this laid by
            // my faction".
            if (!string.IsNullOrEmpty(TriggerFaction))
            {
                var actorFaction = FactionManager.GetFaction(actor);
                if (actorFaction == TriggerFaction)
                    return true;
            }

            var cell = e.GetParameter<Cell>("Cell");
            var zone = cell?.ParentZone;
            if (zone == null)
                return true;

            OnTrigger(actor, zone);

            if (ConsumeOnTrigger)
                zone.RemoveEntity(ParentEntity);

            return true;
        }
    }

    /// <summary>
    /// Rune of Flame: fire damage + <see cref="SmolderingEffect"/> on the
    /// stepper. The flame-school rune in the M6 trio.
    /// </summary>
    public class RuneFlameTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Blueprint-tunable.</summary>
        public int Damage = 8;

        /// <summary>Duration of the applied SmolderingEffect.</summary>
        public int SmolderDuration = 5;

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"The {ParentEntity.GetDisplayName()} flares beneath {actor.GetDisplayName()}!");
            CombatSystem.ApplyDamage(actor, Damage, ParentEntity, zone);
            actor.ApplyEffect(new SmolderingEffect(SmolderDuration), ParentEntity, zone);
        }
    }

    /// <summary>
    /// Rune of Frost: cold damage + <see cref="FrozenEffect"/> on the
    /// stepper.
    /// </summary>
    public class RuneFrostTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Blueprint-tunable.</summary>
        public int Damage = 6;

        /// <summary>Cold intensity passed to FrozenEffect.</summary>
        public float Cold = 1.0f;

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"The {ParentEntity.GetDisplayName()} bites {actor.GetDisplayName()} with cold!");
            CombatSystem.ApplyDamage(actor, Damage, ParentEntity, zone);
            actor.ApplyEffect(new FrozenEffect(Cold), ParentEntity, zone);
        }
    }

    /// <summary>
    /// Rune of Poison: light damage + <see cref="PoisonedEffect"/> DOT on
    /// the stepper.
    /// </summary>
    public class RunePoisonTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Blueprint-tunable.</summary>
        public int Damage = 3;

        /// <summary>Duration of the applied PoisonedEffect.</summary>
        public int PoisonDuration = 5;

        /// <summary>Poison tick damage dice string.</summary>
        public string PoisonDice = "1d3";

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"The {ParentEntity.GetDisplayName()} hisses as {actor.GetDisplayName()} steps on it!");
            CombatSystem.ApplyDamage(actor, Damage, ParentEntity, zone);
            actor.ApplyEffect(new PoisonedEffect(PoisonDuration, PoisonDice), ParentEntity, zone);
        }
    }

    // ============================================================
    // Trap Furniture (mechanical, dungeon-themed step triggers)
    // ============================================================
    //
    // These extend TriggerOnStepPart with the same fire-once-on-step
    // semantics as the magical rune subclasses above, but flavored as
    // mechanical floor traps. The decision to keep them as separate
    // classes (rather than reusing rune subclasses with a re-themed
    // blueprint) is so future trap-specific mechanics — rearm cooldown,
    // hidden-until-detected, multi-cell tripwire — have a class to
    // hang onto without polluting the rune classes.

    /// <summary>
    /// Spike trap: piercing damage on step. The simplest trap — no
    /// status effects, just a sharp spike that stabs the unwary.
    /// </summary>
    public class SpikeTrapTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Default 12 — mid-range,
        /// enough to threaten low-HP scouts but survivable for tanks.</summary>
        public int Damage = 12;

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"{actor.GetDisplayName()} springs the {ParentEntity.GetDisplayName()}! Spikes pierce flesh.");

            // Use the typed-Damage overload so future Piercing-resistance
            // content (none today) can hook in without an int-overload
            // bypass. Mirrors the BurningEffect/AcidicEffect routing fix
            // shipped in fix/effect-damage-attributes.
            var dmg = new Damage(Damage);
            dmg.AddAttribute("Piercing");
            CombatSystem.ApplyDamage(actor, dmg, ParentEntity, zone);
        }
    }

    /// <summary>
    /// Fire trap: heat damage + <see cref="BurningEffect"/> on the stepper.
    /// Mechanical version of <see cref="RuneFlameTriggerPart"/> — same
    /// outputs, different flavor (gout-of-flame trigger plate).
    /// </summary>
    public class FireTrapTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Blueprint-tunable.</summary>
        public int Damage = 8;

        /// <summary>Burning intensity applied. Default 1.5 — enough to
        /// produce a few damage ticks even on resistant creatures.</summary>
        public float BurnIntensity = 1.5f;

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"{actor.GetDisplayName()} steps on the {ParentEntity.GetDisplayName()}! Flames erupt.");

            // Typed Damage with "Fire" attribute so HeatResistance routes.
            var dmg = new Damage(Damage);
            dmg.AddAttribute("Fire");
            CombatSystem.ApplyDamage(actor, dmg, ParentEntity, zone);

            actor.ApplyEffect(new BurningEffect(BurnIntensity, source: ParentEntity), ParentEntity, zone);
        }
    }

    /// <summary>
    /// TripWire: a multi-segment line trap. Each cell of the wire is its
    /// own entity carrying a <see cref="TripWireTriggerPart"/>. All
    /// segments belonging to the same wire share a non-empty
    /// <see cref="WireGroupId"/>. When ANY segment is stepped on, the
    /// segment scans the zone for siblings with the same group id, deals
    /// damage at every sibling cell, and removes all segments in the
    /// group (one-shot, line-coordinated).
    ///
    /// <para>Why N segments instead of one multi-cell entity: CoO
    /// entities live at one cell. A multi-cell-entity abstraction is
    /// out of scope; the per-segment + group-id pattern works without it.
    /// See Docs/TIER2-CLOSEOUT.md §verification-sweep for the
    /// architecture-constraint discussion.</para>
    ///
    /// <para>Faction filter: the inherited <see cref="TriggerFaction"/>
    /// only protects the segment whose cell the actor STEPS ON; other
    /// segments still detonate (line-of-3 wire stretched across an
    /// alley fires when ANY actor crosses it, regardless of the
    /// stepped-on segment's faction tag). Acceptable v1 — closer to
    /// real tripwire physics than per-segment faction gating. Documented
    /// as a ⚪ self-review finding.</para>
    /// </summary>
    public class TripWireTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt at this segment's cell on detonation.
        /// Default 10. Each segment damages whoever's at its cell when
        /// the wire trips, so a 3-segment wire stretched across cells
        /// (5,5)/(6,5)/(7,5) striking when an actor steps on (6,5)
        /// deals damage to whatever's at all three cells.</summary>
        public int Damage = 10;

        /// <summary>Damage attribute attached to the strike. Default
        /// "Piercing" — wires are taut and cutting. Empty string =
        /// untyped damage.</summary>
        public string DamageAttribute = "Piercing";

        /// <summary>Group id linking segments into one wire. All
        /// segments with the same WireGroupId fire together when any
        /// one is tripped. Empty/whitespace = "no group" — the segment
        /// fires only its own cell (degrades gracefully to a 1-cell
        /// trap if a content author forgets to set the group).</summary>
        public string WireGroupId = "";

        public TripWireTriggerPart()
        {
            // Cold-eye Finding 4 (post-fix): the base class self-removes
            // ParentEntity AFTER OnTrigger returns. Our OnTrigger removes
            // sibling segments manually but skips ParentEntity (see
            // `if (seg == ParentEntity) continue;` in OnTrigger), so the
            // base class's removal is the single source-of-truth for
            // ParentEntity cleanup. This avoids the prior double-remove
            // foot-gun if a future maintainer toggles this field.
            ConsumeOnTrigger = true;
        }

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            // Cold-eye Finding 2: emit the trigger flavor BEFORE the damage
            // loop so it lands ahead of any "X is killed by Y!" lines that
            // ApplyDamage→HandleDeath may produce. Cold-eye Finding 3:
            // interpolate ParentEntity.GetDisplayName() so a content author
            // can rename the wire (e.g. "razor wire", "brass tripthread")
            // without needing a code change. Single line for the whole
            // detonation matches the line-strike concept (one wire, one
            // sound), distinct from per-segment trap message-per-step.
            MessageLog.Add($"The {ParentEntity.GetDisplayName()} snaps taut!");

            // Snapshot all sibling segments in the zone (including self).
            // GetAllEntities allocates a fresh List<Entity> — fine for a
            // one-shot detonation event; not a hot path.
            var allSegments = new System.Collections.Generic.List<Entity>();
            string groupId = WireGroupId;
            bool hasGroup = !string.IsNullOrWhiteSpace(groupId);
            foreach (var e in zone.GetAllEntities())
            {
                var seg = e.GetPart<TripWireTriggerPart>();
                if (seg == null) continue;
                if (hasGroup && seg.WireGroupId != groupId) continue;
                if (!hasGroup && e != ParentEntity) continue;
                allSegments.Add(e);
            }

            // Damage whoever's at each segment's cell + remove the
            // segment from the zone. Snapshot Cell.Objects via ToArray
            // since CombatSystem.ApplyDamage may trigger a Death handler
            // that mutates the list (e.g. corpse spawn).
            //
            // Cold-eye Finding 4: skip ParentEntity in the manual cleanup
            // and let the base class consume it via the standard
            // ConsumeOnTrigger=true path. This makes the contract robust
            // to a future maintainer flipping ConsumeOnTrigger — the base
            // class will single-remove ParentEntity, and we never
            // double-call RemoveEntity on the same segment. To keep
            // backwards-compat for unit tests that pin "all segments
            // removed", we still need ParentEntity removed by the time
            // OnTrigger returns; ConsumeOnTrigger=true (set in ctor) +
            // base class's post-OnTrigger zone.RemoveEntity(ParentEntity)
            // handles that.
            foreach (var seg in allSegments)
            {
                var segCell = zone.GetEntityCell(seg);
                if (segCell == null) continue;
                var occupants = segCell.Objects.ToArray();
                foreach (var occ in occupants)
                {
                    if (occ == seg) continue;
                    var dmg = new Damage(Damage);
                    if (!string.IsNullOrEmpty(DamageAttribute))
                        dmg.AddAttribute(DamageAttribute);
                    CombatSystem.ApplyDamage(occ, dmg, ParentEntity, zone);
                }
                if (seg == ParentEntity) continue;
                zone.RemoveEntity(seg);
            }
        }
    }

    /// <summary>
    /// Pressure plate: a rearmable variant of the spike-trap idea — a
    /// floor plate that fires every time someone steps onto it, instead
    /// of consuming itself like <see cref="SpikeTrapTriggerPart"/>.
    ///
    /// <para><b>Why no cooldown.</b> <c>EntityEnteredCell</c> fires only
    /// on cell-CHANGE moves (verified during T2.1 sweep against
    /// <see cref="MovementSystem.FireCellEnteredEvents"/>), so a stationary
    /// actor doesn't re-trigger the plate. A player who deliberately
    /// steps ON-OFF-ON-OFF takes repeated damage; that's correct
    /// PressurePlate semantics, not a bug. If playtest later wants
    /// debouncing for puzzle-state plates, add a <c>_actorsAlreadyOnPlate</c>
    /// HashSet field that clears on TurnEnd. Don't reach for TurnManager
    /// coupling — see Docs/TIER2-CLOSEOUT.md §self-review.</para>
    /// </summary>
    public class PressurePlateTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on each trigger. Default 8 — lighter
        /// than one-shot traps because this fires repeatedly.</summary>
        public int Damage = 8;

        /// <summary>Damage attribute attached to the strike (e.g.
        /// "Piercing" for a spiked plate, "Bludgeoning" for a crushing
        /// plate). Empty string = untyped damage. Defaults to
        /// "Bludgeoning" — generic stomp/crush flavor.</summary>
        public string DamageAttribute = "Bludgeoning";

        public PressurePlateTriggerPart()
        {
            // Don't consume on trigger — the plate persists for repeated
            // stepping. EntityEnteredCell fires on cell-CHANGE only, so
            // this can't loop on a stationary actor; only re-stepping
            // produces a re-fire, which is the correct semantics.
            ConsumeOnTrigger = false;
        }

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            // Cold-eye Finding 2/3: emit log line BEFORE ApplyDamage so the
            // player narrative matches sibling traps (Spike/Fire/Bear emit
            // their trigger flavor first, then the death announcement from
            // HandleDeath comes second if the hit is lethal). Format mirrors
            // SpikeTrap's "{actor} springs the {ParentEntity}!" — actor-first,
            // exclamation, drama clause — so future renamed pressure plate
            // content (e.g. "spiked plate", "crushing plate") narrates
            // consistently with the rest of the trap family.
            MessageLog.Add($"{actor.GetDisplayName()} steps on the " +
                           $"{ParentEntity.GetDisplayName()}!");

            var dmg = new Damage(Damage);
            if (!string.IsNullOrEmpty(DamageAttribute))
                dmg.AddAttribute(DamageAttribute);
            CombatSystem.ApplyDamage(actor, dmg, ParentEntity, zone);
        }
    }

    /// <summary>
    /// Bear trap: heavy piercing damage, briefly stuns, and starts
    /// bleeding. The dangerous trap — a player who steps on this is
    /// pinned for one turn while bleeding through several more.
    /// </summary>
    public class BearTrapTriggerPart : TriggerOnStepPart
    {
        /// <summary>Damage dealt on trigger. Default 15 — highest of the
        /// three traps; the bear trap is the "ouch" payoff.</summary>
        public int Damage = 15;

        /// <summary>Duration in turns of the applied StunnedEffect.</summary>
        public int StunDuration = 1;

        /// <summary>Save-target DC for Bleeding cure roll.</summary>
        public int BleedSaveTarget = 14;

        /// <summary>Bleeding tick damage dice.</summary>
        public string BleedDice = "1d2";

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            MessageLog.Add($"{actor.GetDisplayName()} springs the {ParentEntity.GetDisplayName()}! Iron jaws clamp shut.");

            var dmg = new Damage(Damage);
            dmg.AddAttribute("Piercing");
            CombatSystem.ApplyDamage(actor, dmg, ParentEntity, zone);

            actor.ApplyEffect(new StunnedEffect(StunDuration), ParentEntity, zone);
            actor.ApplyEffect(new BleedingEffect(BleedSaveTarget, BleedDice), ParentEntity, zone);
        }
    }
}
