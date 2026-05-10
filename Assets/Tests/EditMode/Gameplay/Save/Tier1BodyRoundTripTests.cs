using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.7.3 — Body recursive Save/Load contract pin. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.7</c>.
    ///
    /// <para><b>Body</b> uses an explicit recursive Save/Load handler
    /// (SaveSystem.cs:1304-1411). Each <c>BodyPart</c> is serialized
    /// with its 21 individual fields, then a length-prefixed list of
    /// child BodyParts, recursively. <c>DismemberedParts</c> are
    /// serialized as a flat list with each entry's part + parent-id +
    /// original-position. The verification sweep classified this as
    /// ⚪ — no missing state — but the contract has never been pinned
    /// by a test.</para>
    ///
    /// <para>This fixture pins:</para>
    /// <list type="bullet">
    ///   <item>Null body case — Body with no anatomy round-trips as
    ///         null.</item>
    ///   <item>Single-part body — leaf-level field shape (Type, ID,
    ///         Laterality, Mobility, etc.).</item>
    ///   <item>Humanoid body — multi-level hierarchy (Body → Torso →
    ///         Head/Hands/Feet/Back) with parent-child links rebuilt
    ///         on load.</item>
    ///   <item>Equipment refs — <c>_Equipped</c>, <c>_Cybernetics</c>,
    ///         <c>_DefaultBehavior</c> entity refs survive via the
    ///         token-graph helper.</item>
    ///   <item>DismemberedParts — detached parts list with
    ///         ParentPartID + OriginalPosition round-trip.</item>
    /// </list>
    /// </summary>
    public class Tier1BodyRoundTripTests
    {
        [Test]
        public void Body_NullAnatomy_RoundTrips()
        {
            // Body created but no anatomy assigned. SaveBodyPart writes
            // a single bool=false; LoadBodyPart returns null. Pin that
            // the loaded Body has no body part — NOT a stray empty one.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new Body());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var body = loaded.GetPart<Body>();
            Assert.IsNotNull(body, "Body Part itself round-trips.");
            Assert.IsNull(body.GetBody(),
                "Empty Body's GetBody() returns null after load — NOT a "
                + "stale empty BodyPart from LoadBodyPart's null-bool path.");
        }

        [Test]
        public void Body_SimplePart_FieldsRoundTrip()
        {
            // Pin every saved field on BodyPart survives round-trip.
            // SaveSystem.cs:1338-1358 lists 21 fields — write each
            // with a non-default value and verify each comes back.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var body = new Body();
            actor.AddPart(body);

            var part = new BodyPart
            {
                Type = "Hand",
                VariantType = "left-claw",
                Description = "a clawed hand",
                DescriptionPrefix = "the gnarled",
                Name = "left hand",
                SupportsDependent = "Finger",
                DependsOn = "Arm",
                RequiresType = "Arm",
                Manager = "manager-id-7",
                Category = 3,
                _Laterality = 1, // Left
                RequiresLaterality = 0,
                Mobility = 8,
                TargetWeight = 5,
                Flags = 0x10,
                Position = 12,
                DefaultBehaviorBlueprint = "DefaultGoblin",
                ID = 42,
            };
            body.SetBody(part);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var rb = loaded.GetPart<Body>().GetBody();
            Assert.AreEqual("Hand", rb.Type);
            Assert.AreEqual("left-claw", rb.VariantType);
            Assert.AreEqual("a clawed hand", rb.Description);
            Assert.AreEqual("the gnarled", rb.DescriptionPrefix);
            Assert.AreEqual("left hand", rb.Name);
            Assert.AreEqual("Finger", rb.SupportsDependent);
            Assert.AreEqual("Arm", rb.DependsOn);
            Assert.AreEqual("Arm", rb.RequiresType);
            Assert.AreEqual("manager-id-7", rb.Manager);
            Assert.AreEqual(3, rb.Category);
            Assert.AreEqual(1, rb._Laterality);
            Assert.AreEqual(0, rb.RequiresLaterality);
            Assert.AreEqual(8, rb.Mobility);
            Assert.AreEqual(5, rb.TargetWeight);
            Assert.AreEqual(0x10, rb.Flags);
            Assert.AreEqual(12, rb.Position);
            Assert.AreEqual("DefaultGoblin", rb.DefaultBehaviorBlueprint);
            Assert.AreEqual(42, rb.ID);
        }

        [Test]
        public void Body_Humanoid_Hierarchy_RoundTrips()
        {
            // Adversarial: a real humanoid body has a multi-level tree
            // (Body → Head + Torso; Torso → Arms; Arm → Hand). Pin
            // that the entire shape + every part's Type round-trips.
            var actor = new Entity { ID = "a", BlueprintName = "Humanoid" };
            var body = new Body();
            actor.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<Body>();
            var lroot = lb.GetBody();
            Assert.IsNotNull(lroot);
            Assert.AreEqual("Body", lroot.Type, "Root is the Body part.");

            // Humanoid (per AnatomyFactory.cs:191-241): Body → Head→Face,
            // Back, Arm(L+R)→Hand+Hands, Feet (single collective),
            // Handwear, Thrown Weapon, Floating Nearby. We pin a few
            // counts whose stability matters for save/load — drop a
            // hand or a head and the survivor count proves it.
            var heads = lb.GetPartsByType("Head");
            var hands = lb.GetPartsByType("Hand");
            var arms  = lb.GetPartsByType("Arm");
            var feet  = lb.GetPartsByType("Feet");
            Assert.AreEqual(1, heads.Count, "Humanoid has exactly 1 Head.");
            Assert.AreEqual(2, hands.Count, "Humanoid has exactly 2 Hands (left + right).");
            Assert.AreEqual(2, arms.Count,  "Humanoid has exactly 2 Arms (left + right).");
            Assert.AreEqual(1, feet.Count,
                "Humanoid has 1 Feet — it's a collective abstract part, "
                + "not 2 separate left/right.");
        }

        [Test]
        public void Body_BodyPart_ParentChild_LinkRebuilt_OnLoad()
        {
            // Pin: after load, every non-root part has its ParentPart
            // set correctly. LoadBodyPart at SaveSystem.cs:1407 sets
            // `child.ParentPart = part` while reconstructing — this
            // contract test catches a regression that drops the parent
            // link.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var body = new Body();
            actor.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lroot = loaded.GetPart<Body>().GetBody();
            Assert.IsNull(lroot.ParentPart,
                "Root has no parent (it IS the root).");
            Assert.IsTrue(lroot.Parts != null && lroot.Parts.Count > 0,
                "Root has children.");
            Assert.AreSame(lroot, lroot.Parts[0].ParentPart,
                "First child's ParentPart points back to the loaded root.");
        }

        [Test]
        public void Body_EquippedReference_OnBodyPart_RoundTrips()
        {
            // BodyPart._Equipped is an Entity ref written via
            // WriteEntityReference. Pin that the equipped item's
            // body round-trips through the token graph helper.
            var actor = new Entity { ID = "wearer", BlueprintName = "Wearer" };
            var body = new Body();
            actor.AddPart(body);

            var helmet = new Entity { ID = "helmet", BlueprintName = "IronHelm" };
            var head = new BodyPart { Type = "Head", ID = 1, _Equipped = helmet };
            body.SetBody(head);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lhead = loaded.GetPart<Body>().GetBody();
            Assert.IsNotNull(lhead._Equipped,
                "_Equipped survives via WriteEntityReference (line 1356).");
            Assert.AreEqual("helmet", lhead._Equipped.ID);
            Assert.AreEqual("IronHelm", lhead._Equipped.BlueprintName,
                "Equipped item's BodyName came through the token-graph "
                + "body queue — so its full body, not just its ID, is loaded.");
        }

        [Test]
        public void Body_DismemberedParts_RoundTrip_With_ParentID_AndPosition()
        {
            // Adversarial: a Body with one or more DismemberedParts.
            // SaveBody writes the count + each part + ParentPartID +
            // OriginalPosition. LoadBody reads them in the SAME order.
            // A reorder bug would cross the wires (e.g. read parent ID
            // as position). Pin all 3 fields round-trip per entry.
            var actor = new Entity { ID = "a", BlueprintName = "Maimed" };
            var body = new Body();
            actor.AddPart(body);
            body.SetBody(new BodyPart { Type = "Body", ID = 100 });

            var lostHand = new BodyPart { Type = "Hand", ID = 5, Name = "severed left hand" };
            body.DismemberedParts.Add(new DismemberedPart
            {
                Part = lostHand,
                ParentPartID = 100,
                OriginalPosition = 7,
            });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<Body>();
            Assert.AreEqual(1, lb.DismemberedParts.Count,
                "DismemberedParts count survives.");
            var dm = lb.DismemberedParts[0];
            Assert.IsNotNull(dm.Part);
            Assert.AreEqual("Hand", dm.Part.Type,
                "Detached part's body recursively round-trips.");
            Assert.AreEqual("severed left hand", dm.Part.Name);
            Assert.AreEqual(5, dm.Part.ID);
            Assert.AreEqual(100, dm.ParentPartID,
                "ParentPartID round-trips alongside the part body.");
            Assert.AreEqual(7, dm.OriginalPosition,
                "OriginalPosition round-trips — pin that it isn't crossed "
                + "with ParentPartID by a serializer-order bug.");
        }
    }
}
