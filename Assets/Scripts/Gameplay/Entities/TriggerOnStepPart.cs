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
