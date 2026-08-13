namespace CavesOfOoo.Core
{
    /// <summary>
    /// Makes a non-living object breakable: a wall, a tree, a hedgerow, a
    /// barrel, a chest.
    ///
    /// <para><b>Destructibility is OPT-IN</b>, matching Qud, which gates its
    /// <c>Broken</c> effect on <c>HasTagOrProperty("Breakable")</c>
    /// (<c>XRL.World.Effects/Broken.cs:41-48</c>). An object without this
    /// Part cannot be destroyed at all. That is the safe direction: a
    /// staircase, a zone-edge wall or a quest fixture is protected by
    /// OMISSION, so nothing depends on somebody remembering to flag it.</para>
    ///
    /// <para><b>Why a Part rather than a Hitpoints stat.</b> CoO reads
    /// <c>Hitpoints</c> in creature-shaped places —
    /// <c>CombatSystem.ApplyDamage</c> treats a target that has it as a
    /// damageable creature and routes its death through
    /// <c>HandleDeath</c>, which awards XP, drops equipment from body
    /// parts, spawns a corpse, splatters blood and broadcasts the death to
    /// nearby NPCs. Giving a barrel a Hitpoints stat would opt it into all
    /// of that. Structural HP lives here instead, and
    /// <see cref="DestructionSystem"/> is its own path.</para>
    /// </summary>
    public sealed class DestructiblePart : Part
    {
        public override string Name => "Destructible";

        /// <summary>Structural hit points. Reaching 0 destroys the object.</summary>
        public int HP = 10;

        /// <summary>Starting/maximum HP, for damage-proportion checks and
        /// for a future "cracked" visual state.</summary>
        public int MaxHP = 10;

        /// <summary>
        /// Absolute veto. For things that are breakable-SHAPED — they have
        /// this Part because something generic gave it to them — but which
        /// must never actually break: a staircase, a quest door, a
        /// zone-edge wall.
        ///
        /// <para>Belt and braces alongside the opt-in rule above: the
        /// default protection is not having the Part, and this is for when
        /// an object needs the Part's other behaviour but not its
        /// mortality.</para>
        /// </summary>
        public bool Indestructible = false;

        /// <summary>
        /// What is left behind, if anything. A broken wall should read as
        /// broken rather than as pristine floor. Empty = nothing remains.
        /// </summary>
        public string WreckageBlueprint = "";

        /// <summary>Flat damage subtracted from every hit — a stone wall
        /// shrugs off a fist. Never reduces a hit below 1.</summary>
        public int Hardness = 0;

        public bool IsDestroyed => HP <= 0;

        /// <summary>The world-action command this Part's menu row fires.</summary>
        public const string BreakCommand = "Break";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions" && !Indestructible)
            {
                // Bumping into a thing already breaks it, but only if the
                // thing is in your way. A chest, a bush or a barrel you can
                // walk around is unreachable by that route, so the interact
                // key ('c') needs its own row — otherwise "attack anything"
                // is only true of obstacles.
                //
                // Priority 5: below Take (25) and Open (30) — smashing a
                // chest should never be the first thing the cursor lands on
                // when opening it is an option. Above Examine (0), because
                // it is at least an action.
                e.GetParameter<InventoryActionList>("Actions")
                    ?.AddAction("Break", "break", BreakCommand, 'k', 5);
            }
            return true;
        }
    }
}
