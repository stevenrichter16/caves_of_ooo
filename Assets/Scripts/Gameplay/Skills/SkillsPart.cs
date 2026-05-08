using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Per-entity manager Part for skill ownership. Mirrors Qud's
    /// <c>Skills</c> (XRL.World.Parts/Skills.cs:16-235) — holds a
    /// <see cref="List{T}"/> of <see cref="BaseSkillPart"/> the entity has
    /// learned, with reflection-based add-by-name and add-by-instance entry
    /// points.
    ///
    /// <para><b>Persistence:</b> the SkillList is NOT manually
    /// serialized — individual <see cref="BaseSkillPart"/> instances
    /// are persisted by the existing Part-system save/load. On load,
    /// <see cref="OnAfterLoad"/> rebuilds the list from
    /// <c>ParentEntity.Parts</c> by filtering for BaseSkillPart instances.
    /// Mirrors <see cref="CavesOfOoo.Core.MutationsPart.OnAfterLoad"/>
    /// — same precedent, same rationale (avoid double-serialization;
    /// keep the source of truth in the entity's Parts list).</para>
    ///
    /// <para><b>Diag:</b> emits <c>skill/Added</c> on successful AddSkill,
    /// <c>skill/Removed</c> on successful RemoveSkill. Channel default-on
    /// per <see cref="Diag.DefaultOnCategories"/> (8th channel after
    /// event/effect/damage/turn/furniture/trade/quest). Payload includes
    /// skillClass + displayName + source/cause for AI-debug-substrate
    /// queries via <c>diag_query category=skill</c>.</para>
    /// </summary>
    public class SkillsPart : Part
    {
        public override string Name => "Skills";

        /// <summary>
        /// Convenience cache of which BaseSkillPart instances the entity
        /// owns. Source of truth is <c>ParentEntity.Parts</c>; this list
        /// is rebuilt on load (see <see cref="OnAfterLoad"/>) and kept in
        /// sync via <see cref="AddSkill(BaseSkillPart, Entity, string)"/>
        /// / <see cref="RemoveSkill"/>.
        /// </summary>
        public List<BaseSkillPart> SkillList = new List<BaseSkillPart>();

        // ── Save/load (delegated to Part system) ─────────────────────────

        public override void OnAfterLoad(SaveReader reader)
        {
            // Rebuild SkillList from the entity's Parts collection.
            // Mirrors MutationsPart.OnAfterLoad:83-96 — Part instances
            // are deserialized by the save system; we just filter +
            // index into our convenience list.
            SkillList.Clear();
            if (ParentEntity == null) return;
            for (int i = 0; i < ParentEntity.Parts.Count; i++)
            {
                if (ParentEntity.Parts[i] is BaseSkillPart skill && !SkillList.Contains(skill))
                    SkillList.Add(skill);
            }
        }

        // ── AddSkill ─────────────────────────────────────────────────────

        /// <summary>
        /// Add a skill instance to the actor. Attaches the skill as a
        /// Part on <c>ParentEntity</c> (which gives it ParentEntity ref
        /// + Initialize + HandleEvent participation), calls the skill's
        /// <see cref="BaseSkillPart.AddSkill"/> hook, and emits a
        /// <c>skill/Added</c> diag record.
        ///
        /// <para>If the entity already owns a skill of the same type
        /// (matched by GetType()), this returns <c>false</c> without
        /// double-attaching — duplicate-skill semantics aren't
        /// supported in v1 (Qud handles ranked skills similarly via
        /// MutationsPart's existing/return path; we don't have ranked
        /// skills yet).</para>
        /// </summary>
        /// <param name="skill">The skill instance to add.</param>
        /// <param name="source">
        /// Optional acquisition path tag for the diag record:
        /// <c>"purchase"</c> (BuySkillAction in ST.6),
        /// <c>"wish"</c> (debug command),
        /// <c>"npc-teach"</c> (water-ritual learning, future).
        /// Null for "unspecified" — common in test fixtures.
        /// </param>
        /// <returns><c>true</c> if added; <c>false</c> if duplicate or
        /// if the skill's <see cref="BaseSkillPart.AddSkill"/> hook
        /// returned false (in which case the part is rolled back).</returns>
        public bool AddSkill(BaseSkillPart skill, string source = null)
        {
            if (skill == null || ParentEntity == null) return false;

            // Duplicate check by Type (mirrors MutationsPart.AddMutation
            // duplicate-detection at MutationsPart.cs:108-125).
            for (int i = 0; i < SkillList.Count; i++)
            {
                if (SkillList[i].GetType() == skill.GetType())
                    return false;
            }

            // Attach as a Part — this sets skill.ParentEntity + calls
            // Initialize. Order matters: the AddSkill lifecycle hook
            // below assumes ParentEntity is set.
            ParentEntity.AddPart(skill);
            SkillList.Add(skill);

            if (!skill.AddSkill(ParentEntity))
            {
                // Rollback: hook signaled setup failure. Remove the part
                // and return false. Don't emit a diag record — the skill
                // was never actually active.
                SkillList.Remove(skill);
                ParentEntity.RemovePart(skill);
                return false;
            }

            // WSP3.5 — Activated-ability lifecycle. If the skill declares
            // an ActivatedAbilitySpec, register it on the actor's
            // ActivatedAbilitiesPart and stash the resulting Guid on the
            // skill instance for later cleanup (see RemoveSkill).
            // Passive skills return null from DeclareActivatedAbility;
            // we silently skip them.
            var spec = skill.DeclareActivatedAbility(ParentEntity);
            if (spec != null && !string.IsNullOrEmpty(spec.Command))
            {
                var abilities = ParentEntity.GetPart<ActivatedAbilitiesPart>();
                if (abilities != null)
                {
                    skill.ActivatedAbilityID = abilities.AddAbility(
                        displayName: spec.DisplayName ?? skill.DisplayName,
                        command: spec.Command,
                        abilityClass: string.IsNullOrEmpty(spec.Class) ? "Skills" : spec.Class,
                        targetingMode: spec.TargetingMode,
                        range: spec.Range,
                        sourceMutationClass: "");
                    var entry = abilities.GetAbility(skill.ActivatedAbilityID);
                    if (entry != null)
                        entry.MaxCooldown = spec.Cooldown;
                }
            }

            EmitAddedDiag(skill, source);
            return true;
        }

        /// <summary>
        /// Convenience overload: resolve a skill class name to a Type via
        /// reflection, instantiate, and add. Mirrors Qud's
        /// <c>Skills.AddSkill(string Class)</c>
        /// (XRL.World.Parts/Skills.cs:86-92).
        ///
        /// <para>Resolution strategy: try <c>Type.GetType(className)</c>
        /// first (handles fully-qualified names), then fall back to
        /// <c>Type.GetType("CavesOfOoo.Skills." + className)</c> (the
        /// CoO convention namespace for concrete skill classes).
        /// Returns <c>false</c> if the class can't be resolved or
        /// isn't a BaseSkillPart.</para>
        /// </summary>
        public bool AddSkill(string skillClassName, string source = null)
        {
            if (string.IsNullOrWhiteSpace(skillClassName)) return false;
            BaseSkillPart skill = CreateSkillByClassName(skillClassName);
            if (skill == null) return false;
            return AddSkill(skill, source);
        }

        // ── RemoveSkill ──────────────────────────────────────────────────

        /// <summary>
        /// Remove a skill from the actor. Calls the skill's
        /// <see cref="BaseSkillPart.RemoveSkill"/> hook (so the skill
        /// can undo its passive effects / deregister abilities), detaches
        /// the Part, and emits a <c>skill/Removed</c> diag record.
        /// </summary>
        /// <param name="skill">The skill instance to remove.</param>
        /// <param name="cause">
        /// Optional removal-reason tag for diag:
        /// <c>"unlearn"</c> (player-initiated),
        /// <c>"effect"</c> (skill-stripping curse),
        /// <c>"unequip"</c> (gear-granted skill removed via SkillOnEquip).
        /// Null for "unspecified".
        /// </param>
        /// <returns><c>true</c> if removed; <c>false</c> if the skill
        /// wasn't in the list to begin with (idempotent — see counter-check
        /// test <c>RemoveSkill_OnEntityWithoutSkill_DoesNothing</c>).</returns>
        public bool RemoveSkill(BaseSkillPart skill, string cause = null)
        {
            if (skill == null || ParentEntity == null) return false;
            if (!SkillList.Contains(skill)) return false;

            // Capture display name BEFORE remove hook + detachment, since
            // DisplayName lookup may depend on the entity / registry state.
            string skillClass = skill.GetType().Name;
            string displayName = skill.DisplayName;

            skill.RemoveSkill(ParentEntity);

            // WSP3.5 — Activated-ability cleanup. If this skill registered
            // an ability when it was added, drop it from the actor's
            // ActivatedAbilitiesPart so the hotbar slot frees up and the
            // command stops dispatching.
            if (skill.ActivatedAbilityID != System.Guid.Empty)
            {
                var abilities = ParentEntity.GetPart<ActivatedAbilitiesPart>();
                if (abilities != null)
                    abilities.RemoveAbility(skill.ActivatedAbilityID);
                skill.ActivatedAbilityID = System.Guid.Empty;
            }

            SkillList.Remove(skill);
            ParentEntity.RemovePart(skill);

            EmitRemovedDiag(skillClass, displayName, cause);
            return true;
        }

        /// <summary>
        /// WSP3.5 — Route an activated-ability command (e.g. "CommandConk")
        /// to the owning skill's <see cref="BaseSkillPart.OnCommand"/>
        /// override. Looks up the skill whose registered ability has a
        /// matching <c>Command</c> string, checks usability (cooldown
        /// elapsed), invokes <c>OnCommand</c>, and applies the cooldown.
        ///
        /// <para>Mirrors Qud's <c>CommandEvent</c> dispatch from
        /// <c>InputManager</c> → <c>FireEvent</c> on the actor's parts;
        /// here we centralize the lookup so the input layer doesn't need
        /// to know which skill handles which command.</para>
        ///
        /// <para>Returns <c>true</c> if a skill consumed the command
        /// (whether or not OnCommand succeeded internally); <c>false</c>
        /// if no owned skill claims the command or its cooldown blocks
        /// activation. Callers can use the bool to fall through to
        /// alternative dispatch paths (mutations, items, etc.).</para>
        /// </summary>
        public bool TryRouteSkillCommand(string command, Zone zone = null,
            System.Random rng = null)
        {
            if (string.IsNullOrEmpty(command) || ParentEntity == null) return false;
            var abilities = ParentEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return false;

            for (int i = 0; i < SkillList.Count; i++)
            {
                var skill = SkillList[i];
                if (skill == null || skill.ActivatedAbilityID == System.Guid.Empty) continue;
                var ability = abilities.GetAbility(skill.ActivatedAbilityID);
                if (ability == null || ability.Command != command) continue;

                // Cooldown gate. Returning false here lets callers know
                // the command was recognized but blocked — UI can show a
                // "still cooling down" message instead of "unknown command".
                if (!ability.IsUsable) return false;

                // Cold-eye finding 🟡 #3: don't silently fall back to a
                // wall-clock-seeded Random when the caller passed null.
                // Production paths (input handlers, scenarios, AI) MUST
                // thread a deterministic rng through. If a caller passes
                // null we still synthesize one here so the skill's
                // OnCommand can run, BUT we no longer hide that — the
                // first-class fix is for callers to thread their own.
                var ctx = new SkillEventContext
                {
                    Attacker = ParentEntity, Defender = ParentEntity,
                    Zone = zone, Rng = rng ?? new System.Random(),
                };
                skill.OnCommand(ctx);

                // Apply cooldown after successful invocation. (If
                // OnCommand wants to suppress the cooldown — e.g. on a
                // failed targeting popup — it can manually set
                // ability.CooldownRemaining = 0 before returning.)
                ability.CooldownRemaining = ability.MaxCooldown;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Route GameEvent-style command dispatch (the same path that
        /// mutations use via their per-mutation HandleEvent overrides)
        /// to the skill's <see cref="BaseSkillPart.OnCommand"/>. Without
        /// this override, the InputHandler's
        /// <c>ResolveAbilityCommand</c> path —
        /// <c>FireEvent(GameEvent.New(ability.Command))</c> — fires into
        /// the void for skills, because skills' command handling lives
        /// behind the parallel <see cref="TryRouteSkillCommand"/>
        /// mechanism that no production code calls.
        ///
        /// <para><b>Symptom of the missing override:</b> player presses
        /// the keybind for Slam / Conk / Berserk / Shank / HookAndDrag,
        /// gets the "choose a direction" prompt, presses a direction —
        /// and sees "The rite fails to resolve." in the log. The
        /// <c>cmd.Handled</c> flag stayed false because no Part
        /// answered the GameEvent — mutations only handle THEIR own
        /// command names; skills had no GameEvent listener at all.</para>
        ///
        /// <para><b>The fix:</b> on every event fired on the parent
        /// entity, check if any owned skill's registered ability has a
        /// matching <c>Command</c> string. If yes, route through
        /// <see cref="TryRouteSkillCommand"/> with the event's Zone +
        /// RNG parameters; mark the event Handled on success. The
        /// existing <see cref="TryRouteSkillCommand"/> already handles
        /// the cooldown gate + ctx construction + OnCommand invocation
        /// + cooldown reset, so this override is a thin adapter.</para>
        ///
        /// <para>Returns <c>true</c> when no skill claimed the event
        /// (so other Parts can still see it); returns <c>false</c> when
        /// a skill consumed it (mirrors how mutations stop propagation
        /// after they handle their command).</para>
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.ID)) return true;

            // Fast-path early-out for events that obviously aren't skill
            // commands. CoO's command convention is "CommandX" (e.g.
            // CommandSlam, CommandConk, CommandShank, CommandHookAndDrag,
            // CommandAxeBerserk). Skipping non-Command events keeps the
            // per-event dispatch overhead negligible — the FireEvent loop
            // hits this method on EVERY event the entity receives.
            if (!e.ID.StartsWith("Command", System.StringComparison.Ordinal))
                return true;

            // Pull Zone + RNG from the event params so the skill's
            // OnCommand sees the same Zone the InputHandler passed.
            // The InputHandler's ResolveAbilityCommand sets these via
            // cmd.SetParameter (line 2809-2810 of InputHandler.cs).
            Zone zone = e.GetParameter<Zone>("Zone");
            System.Random rng = e.GetParameter<System.Random>("RNG");

            // Route to the existing skill-command dispatcher. Returns
            // true if a skill consumed the command. Cooldown failures
            // return false — that's the correct semantic (the player
            // already saw the cooldown gate in TryActivateAbility, but
            // a defense-in-depth path here means the event still goes
            // unhandled, which surfaces "The rite fails to resolve."
            // — better than silently swallowing the input).
            if (TryRouteSkillCommand(e.ID, zone, rng))
            {
                e.Handled = true;
                return false; // stop propagation — the skill consumed it
            }
            return true; // not a skill command (or cooldown blocked) — let other Parts try
        }

        // ── Queries ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the actor owns a skill whose runtime class
        /// matches the given name. Used by purchase gating
        /// (Requires/Exclusion checks in ST.6) and by UI rendering.
        /// </summary>
        public bool HasSkill(string skillClassName)
        {
            if (string.IsNullOrWhiteSpace(skillClassName)) return false;
            for (int i = 0; i < SkillList.Count; i++)
            {
                if (SkillList[i].GetType().Name == skillClassName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Find an owned skill by its runtime class name; returns null
        /// if not owned.
        /// </summary>
        public BaseSkillPart GetSkill(string skillClassName)
        {
            if (string.IsNullOrWhiteSpace(skillClassName)) return null;
            for (int i = 0; i < SkillList.Count; i++)
            {
                if (SkillList[i].GetType().Name == skillClassName)
                    return SkillList[i];
            }
            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static BaseSkillPart CreateSkillByClassName(string className)
        {
            Type type = ResolveSkillType(className);
            if (type == null) return null;
            if (type.IsAbstract) return null;
            if (!typeof(BaseSkillPart).IsAssignableFrom(type)) return null;

            try
            {
                return (BaseSkillPart)Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Three-tier type-resolution strategy for the string-class
        /// AddSkill overload:
        /// <list type="number">
        ///   <item><c>Type.GetType(className)</c> — handles fully-qualified
        ///         names from mscorlib + the calling assembly.</item>
        ///   <item><c>Type.GetType("CavesOfOoo.Skills." + className)</c> —
        ///         handles unqualified short names per the production
        ///         convention namespace.</item>
        ///   <item>Walk every loaded assembly looking for the type. Required
        ///         because test stubs live in the test assembly (not the
        ///         calling production assembly) and future mod-loaded
        ///         skills will live in mod assemblies. Mirrors the all-
        ///         assembly walk in MutationsPart.CreateMutationByName but
        ///         broader (MutationsPart only searches its own assembly,
        ///         which works for production but breaks for test stubs).</item>
        /// </list>
        /// </summary>
        private static Type ResolveSkillType(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return null;

            Type t = Type.GetType(className);
            if (t != null) return t;

            t = Type.GetType("CavesOfOoo.Skills." + className);
            if (t != null) return t;

            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(className);
                if (t != null) return t;
            }

            // Last resort: nested-type fallback. Walk every type in every
            // loaded assembly and match by short Name. Required because
            // test stubs nested inside fixture classes (e.g.
            // BuySkillActionTests+SoftCorePower) aren't found by
            // asm.GetType(shortName) — that only finds top-level types.
            // Production content classes are always top-level so this
            // path only matters for tests + future modded skills that
            // ship as nested types. O(N types in domain) — acceptable
            // because it's only hit on miss and only when string-overload
            // AddSkill resolves a name that didn't match a top-level type.
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; } // Skip assemblies that fail reflection (rare).
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].Name == className
                        && !types[i].IsAbstract
                        && typeof(BaseSkillPart).IsAssignableFrom(types[i]))
                    {
                        return types[i];
                    }
                }
            }
            return null;
        }

        private void EmitAddedDiag(BaseSkillPart skill, string source)
        {
            if (!Diag.IsChannelEnabled("skill")) return;
            Diag.Record(
                category: "skill",
                kind: "Added",
                target: ParentEntity,
                payload: new
                {
                    skillClass = skill.GetType().Name,
                    displayName = skill.DisplayName,
                    source = source ?? ""
                });
        }

        private void EmitRemovedDiag(string skillClass, string displayName, string cause)
        {
            if (!Diag.IsChannelEnabled("skill")) return;
            Diag.Record(
                category: "skill",
                kind: "Removed",
                target: ParentEntity,
                payload: new
                {
                    skillClass,
                    displayName,
                    cause = cause ?? ""
                });
        }
    }
}
