using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.7.4 — ActivatedAbilitiesPart Save/Load contract pin (🟡).
    /// See <c>Docs/SAVE-LOAD-AUDIT.md §SL.7</c>.
    ///
    /// <para>The verification sweep flagged this 🟡 because the
    /// <c>SlotAssignments</c> array has a fixed length tied to
    /// <see cref="ActivatedAbilitiesPart.SlotCount"/> (currently 10),
    /// while save writes the runtime length and load allocates a
    /// fresh array of <c>SlotCount</c> regardless. If the constant
    /// ever changes, old saves with a different slot count must
    /// either pad or truncate cleanly without losing data.</para>
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Empty Part round-trips with a zero-filled
    ///         <c>SlotAssignments[SlotCount]</c>.</item>
    ///   <item>AbilityList round-trips each <see cref="ActivatedAbility"/>'s
    ///         8 public fields including the <c>Guid ID</c> identity.</item>
    ///   <item><c>AbilityByGuid</c> dictionary is rebuilt from the
    ///         AbilityList post-load (cache derived, not saved).</item>
    ///   <item><c>CooldownRemaining</c> and <c>MaxCooldown</c>
    ///         survive — these are the AI-relevant timer fields.</item>
    ///   <item><c>SlotAssignments</c> survive at length
    ///         <see cref="ActivatedAbilitiesPart.SlotCount"/>; per-slot
    ///         ability assignments preserved.</item>
    /// </list>
    /// </summary>
    public class Tier1ActivatedAbilitiesTests
    {
        [Test]
        public void ActivatedAbilities_Empty_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new ActivatedAbilitiesPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var part = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(0, part.AbilityList.Count, "No abilities.");
            Assert.AreEqual(0, part.AbilityByGuid.Count, "No id-cache.");
            Assert.AreEqual(ActivatedAbilitiesPart.SlotCount,
                part.SlotAssignments.Length,
                "SlotAssignments always allocated to SlotCount on load — "
                + "regardless of whether the saved array was empty.");
        }

        [Test]
        public void ActivatedAbilities_OneAbility_AllFieldsRoundTrip()
        {
            // Pin every public field on ActivatedAbility round-trips.
            // WritePublicFields walks reflection, so a future field
            // addition that's NOT public would silently drop on save.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);

            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Slam",
                Command = "CommandSlam",
                Class = "CudgelTree",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                CooldownRemaining = 4,
                MaxCooldown = 6,
                SourceMutationClass = "",
            };
            part.AbilityList.Add(ability);
            part.AbilityByGuid[ability.ID] = ability;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(1, lp.AbilityList.Count);
            var la = lp.AbilityList[0];
            Assert.AreEqual(ability.ID, la.ID, "Guid ID identity round-trips.");
            Assert.AreEqual("Slam", la.DisplayName);
            Assert.AreEqual("CommandSlam", la.Command);
            Assert.AreEqual("CudgelTree", la.Class);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, la.TargetingMode);
            Assert.AreEqual(1, la.Range);
            Assert.AreEqual(4, la.CooldownRemaining,
                "CooldownRemaining is critical for resume-mid-cooldown — "
                + "if this drops to 0, the player gets a free recast on "
                + "load. Catches a regression.");
            Assert.AreEqual(6, la.MaxCooldown);
        }

        [Test]
        public void ActivatedAbilities_AbilityByGuid_IsRebuilt_FromAbilityList()
        {
            // AbilityByGuid is a derived index, NOT serialized. Pin
            // that the load handler rebuilds it from the AbilityList
            // (line 1290 in SaveSystem). If the rebuild step drops,
            // GetAbility(id) returns null even though the ability is
            // in AbilityList.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);
            var idA = part.AddAbility("Slam",     "CommandSlam",   "Cudgel");
            var idB = part.AddAbility("Decapitate","CommandDecap", "Axe");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(2, lp.AbilityList.Count);
            Assert.IsNotNull(lp.GetAbility(idA),
                "GetAbility(idA) hits AbilityByGuid — must be rebuilt.");
            Assert.IsNotNull(lp.GetAbility(idB));
            Assert.AreEqual("Slam", lp.GetAbility(idA).DisplayName);
        }

        // ── SlotAssignments edge cases (the 🟡 surface) ──────────

        [Test]
        public void ActivatedAbilities_SlotAssignments_AllEmpty_RoundTrips()
        {
            // Default state: SlotAssignments is `new Guid[10]`, all
            // Guid.Empty. Round-trip must preserve exactly that —
            // length and per-slot Empty.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new ActivatedAbilitiesPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(ActivatedAbilitiesPart.SlotCount,
                lp.SlotAssignments.Length);
            for (int i = 0; i < lp.SlotAssignments.Length; i++)
                Assert.AreEqual(Guid.Empty, lp.SlotAssignments[i],
                    $"Slot {i} starts empty.");
        }

        [Test]
        public void ActivatedAbilities_SlotAssignments_PartiallyFilled_RoundTrip()
        {
            // Real-world: player has 3 abilities bound to slots 0, 2, 5.
            // Other 7 slots are Guid.Empty. Pin all 10 entries survive
            // exactly — a buggy load that miscounts would cross slot
            // assignments.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);
            var idSlam = part.AddAbility("Slam", "CommandSlam", "Cudgel");
            var idDecap = part.AddAbility("Decapitate", "CommandDecap", "Axe");
            var idShank = part.AddAbility("Shank", "CommandShank", "ShortBlades");

            // AddAbility auto-assigns to the first free slot. Override
            // the assignments to a sparse pattern: 0=Slam, 2=Decap, 5=Shank
            part.SlotAssignments[0] = idSlam;
            part.SlotAssignments[1] = Guid.Empty;
            part.SlotAssignments[2] = idDecap;
            for (int i = 3; i < 5; i++) part.SlotAssignments[i] = Guid.Empty;
            part.SlotAssignments[5] = idShank;
            for (int i = 6; i < ActivatedAbilitiesPart.SlotCount; i++)
                part.SlotAssignments[i] = Guid.Empty;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(idSlam,    lp.SlotAssignments[0]);
            Assert.AreEqual(Guid.Empty, lp.SlotAssignments[1]);
            Assert.AreEqual(idDecap,   lp.SlotAssignments[2]);
            Assert.AreEqual(Guid.Empty, lp.SlotAssignments[3]);
            Assert.AreEqual(Guid.Empty, lp.SlotAssignments[4]);
            Assert.AreEqual(idShank,   lp.SlotAssignments[5]);
            for (int i = 6; i < ActivatedAbilitiesPart.SlotCount; i++)
                Assert.AreEqual(Guid.Empty, lp.SlotAssignments[i],
                    $"Slot {i} unbound.");
        }

        [Test]
        public void ActivatedAbilities_SlotAssignments_AllFilled_RoundTrip()
        {
            // Boundary: SlotCount-1 simultaneously assigned, 1 empty.
            // Catches an off-by-one in the slot loop on load.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);

            var ids = new Guid[ActivatedAbilitiesPart.SlotCount - 1];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = part.AddAbility($"Skill{i}", $"Cmd{i}", $"Class{i}");
                part.SlotAssignments[i] = ids[i];
            }
            // Last slot intentionally empty.

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < ids.Length; i++)
                Assert.AreEqual(ids[i], lp.SlotAssignments[i], $"Slot {i}");
            Assert.AreEqual(Guid.Empty,
                lp.SlotAssignments[ActivatedAbilitiesPart.SlotCount - 1],
                "Last slot stayed empty post-round-trip.");
        }
    }
}
