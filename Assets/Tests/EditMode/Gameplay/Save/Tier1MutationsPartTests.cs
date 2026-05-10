using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.7.6 — MutationsPart Save/Load contract pin (🟡 cleanup
    /// candidate). See <c>Docs/SAVE-LOAD-AUDIT.md §SL.7</c>.
    ///
    /// <para><b>The asymmetry:</b> The verification sweep flagged this
    /// as 🔴, but the bug is actually 🟡 (cleanup, not functional).
    /// SaveMutationsPart writes <c>MutationList</c> type names (line
    /// 1525), but LoadMutationsPart reads them and DISCARDS them
    /// (line 1542-1543: `for (int i = 0; i &lt; mutationCount; i++)
    /// reader.ReadString();`). The actual restoration happens in
    /// <see cref="MutationsPart.OnAfterLoad"/> which scans
    /// <c>ParentEntity.Parts</c> for <c>BaseMutation</c> instances.</para>
    ///
    /// <para><b>Why this is 🟡 not 🔴:</b> The on-disk save bytes
    /// include the mutation type names but they're functionally
    /// redundant — the same data is encoded in the entity's Parts
    /// list (each BaseMutation is itself a Part, serialized through
    /// the standard Part path). Mutations DO round-trip correctly via
    /// OnAfterLoad's Parts scan. The only cost is wasted bytes in
    /// the save file. <b>Recommended cleanup (deferred to a future
    /// commit):</b> remove the writeS/discard pair on both sides.</para>
    ///
    /// <para><b>Subtle correctness risk:</b> if a mutation is in
    /// MutationList at save time but NOT attached as a Part on the
    /// entity (which would be a contract violation in
    /// <c>AddMutation</c>/<c>RemoveMutation</c>), it would silently
    /// vanish on load. The tests here pin the AddMutation contract:
    /// MutationList is always in sync with ParentEntity.Parts.</para>
    /// </summary>
    public class Tier1MutationsPartTests
    {
        [Test]
        public void MutationsPart_Empty_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new MutationsPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var mp = loaded.GetPart<MutationsPart>();
            Assert.IsNotNull(mp);
            Assert.AreEqual(0, mp.MutationList.Count);
            Assert.AreEqual("", mp.StartingMutations);
            Assert.AreEqual(0, mp.MutationMods.Count);
            Assert.AreEqual(0, mp.MutationGeneratedEquipment.Count);
        }

        [Test]
        public void MutationsPart_StartingMutations_String_RoundTrips()
        {
            // StartingMutations is the only non-list scalar — pin
            // separately so a future serializer-order regression
            // surfaces here, not in a more complex test.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new MutationsPart { StartingMutations = "Telepathy,Quench" });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual("Telepathy,Quench",
                loaded.GetPart<MutationsPart>().StartingMutations);
        }

        [Test]
        public void MutationsPart_AddedMutation_Survives_ViaPartsScan()
        {
            // The key contract: mutations attached as Parts via
            // AddMutation are restored on load through OnAfterLoad's
            // Parts scan, NOT through the saved type-name list (which
            // is discarded). This is the test that pins MutationList
            // is in sync with ParentEntity.Parts.
            var actor = new Entity { ID = "mutant", BlueprintName = "Test" };
            var mp = new MutationsPart();
            actor.AddPart(mp);
            mp.AddMutation(new TelepathyMutation(), level: 2);

            // Pre-condition: the mutation is in BOTH MutationList AND
            // attached as a Part on the entity.
            Assert.AreEqual(1, mp.MutationList.Count,
                "Setup: AddMutation populates MutationList.");
            Assert.IsTrue(actor.Parts.Exists(p => p is BaseMutation),
                "Setup: AddMutation also attaches the mutation as a Part. "
                + "If this assertion fails, the test premise is wrong.");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lm = loaded.GetPart<MutationsPart>();
            Assert.AreEqual(1, lm.MutationList.Count,
                "Mutation rebuilt via OnAfterLoad's Parts scan, despite "
                + "the LoadMutationsPart handler at line 1542 reading + "
                + "discarding the saved type names.");
            Assert.IsInstanceOf<TelepathyMutation>(lm.MutationList[0],
                "Mutation type identity preserved through the Parts "
                + "deserialization path (NOT through the discarded "
                + "type-name save bytes).");
        }

        [Test]
        public void MutationsPart_MutationMods_RoundTrip()
        {
            // MutationMods are restored properly (NOT discarded like
            // MutationList type names). Pin a non-default tracker
            // round-trips its 5 public fields.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var mp = new MutationsPart();
            actor.AddPart(mp);
            var trackerId = Guid.NewGuid();
            mp.MutationMods.Add(new MutationModifierTracker
            {
                ID = trackerId,
                Bonus = 3,
                MutationClassName = "Telepathy",
                SourceType = MutationSourceType.Equipment,
                SourceName = "TelepathyHelm",
            });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lm = loaded.GetPart<MutationsPart>();
            Assert.AreEqual(1, lm.MutationMods.Count);
            var lt = lm.MutationMods[0];
            Assert.AreEqual(trackerId, lt.ID, "Guid ID survives.");
            Assert.AreEqual(3, lt.Bonus);
            Assert.AreEqual("Telepathy", lt.MutationClassName);
            Assert.AreEqual(MutationSourceType.Equipment, lt.SourceType);
            Assert.AreEqual("TelepathyHelm", lt.SourceName);
        }

        [Test]
        public void MutationsPart_MultipleMutationMods_AllSurvive()
        {
            // Adversarial: 3 trackers in MutationMods. Pin all survive
            // in order — catches a buggy load that miscounts and
            // crosses tracker fields.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var mp = new MutationsPart();
            actor.AddPart(mp);
            for (int i = 0; i < 3; i++)
            {
                mp.MutationMods.Add(new MutationModifierTracker
                {
                    Bonus = i + 1,
                    MutationClassName = $"Class{i}",
                    SourceName = $"Source{i}",
                });
            }

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lm = loaded.GetPart<MutationsPart>();
            Assert.AreEqual(3, lm.MutationMods.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i + 1, lm.MutationMods[i].Bonus,
                    $"Tracker {i} Bonus survives.");
                Assert.AreEqual($"Class{i}", lm.MutationMods[i].MutationClassName);
                Assert.AreEqual($"Source{i}", lm.MutationMods[i].SourceName);
            }
        }

        [Test]
        public void MutationsPart_DiscardedMutationListBytes_DontCorruptStream()
        {
            // Pin the most adversarial concern: even though Load reads
            // and discards the saved MutationList type names, the
            // stream-position bookkeeping must be exactly right.
            // If the reader skipped MORE or FEWER bytes than the
            // writer wrote, every subsequent field (MutationMods,
            // MutationGeneratedEquipment) would read garbage.
            //
            // Trigger: save WITH attached mutations (so type names
            // are written), then verify MutationMods + Equipment
            // survive after the discard step.
            var actor = new Entity { ID = "mutant", BlueprintName = "Test" };
            var mp = new MutationsPart();
            actor.AddPart(mp);
            mp.AddMutation(new TelepathyMutation(), level: 1);
            mp.MutationMods.Add(new MutationModifierTracker
            {
                Bonus = 7,
                MutationClassName = "MyMutation",
            });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lm = loaded.GetPart<MutationsPart>();

            // The saved type-name bytes were consumed without crossing
            // into the next field. Pin: MutationMods after the discard
            // step is intact.
            Assert.AreEqual(1, lm.MutationMods.Count,
                "MutationMods count survives — proves the discard loop "
                + "consumed exactly the right number of bytes.");
            Assert.AreEqual(7, lm.MutationMods[0].Bonus,
                "Tracker payload uncorrupted by the discard step. Stream "
                + "position bookkeeping is consistent.");
        }
    }
}
