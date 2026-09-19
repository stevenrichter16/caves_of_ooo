using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// R4 dedicated adversarial sweep for <see cref="MaterialUseDescription"/>
    /// (ADVERSARIAL_TESTING.md gate; the per-invariant UI suite is
    /// MaterialGuidanceInventoryTests). Surfaces probed: boundary inputs,
    /// cross-actor ownership, stack boundaries, consumer parity, save/load
    /// reach, purity/idempotency and anti-drift of the advertised names.
    ///
    /// <para>These target the pure helper directly. The helper is the single
    /// source of every sentence the player reads, so a lie here is a lie on
    /// screen. Each test says which buggy implementation it would catch.</para>
    /// </summary>
    public sealed class MaterialGuidanceAdversarialTests
    {
        private static readonly string[] Materials = { "FireClay", "SilverSand", "WardOil" };
        private EntityFactory factory;
        private Entity player;
        private InventoryPart Inventory => player.GetPart<InventoryPart>();

        [SetUp]
        public void SetUp()
        {
            factory = new EntityFactory();
            var content = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.That(content, Is.Not.Null);
            factory.LoadBlueprints(content.text);
            player = factory.CreateEntity("Player");
            Assert.That(Inventory, Is.Not.Null, "the real player blueprint carries an inventory");
        }

        private Entity Carry(Entity owner, string blueprint, int count = 1)
        {
            var item = factory.CreateEntity(blueprint);
            var stack = item.GetPart<StackerPart>();
            if (stack != null) stack.StackCount = count;
            Assert.That(owner.GetPart<InventoryPart>().AddObject(item), Is.True, blueprint + " is carried");
            return item;
        }

        private static bool ConsumerHasGuide(Entity actor, string guide)
        {
            var m = typeof(SettlementManager).GetMethod("HasInventoryItem",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(m, Is.Not.Null, "the repair consumer's own guide gate");
            return (bool)m.Invoke(null, new object[] { actor, guide });
        }

        private static string GuideFor(string material)
        {
            switch (material)
            {
                case "FireClay":   return SettlementRepairDefinitions.OvenBuildersGuideBlueprint;
                case "SilverSand": return SettlementRepairDefinitions.WellMaintenanceManualBlueprint;
                default:           return SettlementRepairDefinitions.LanternOilRecipeBlueprint;
            }
        }

        // ════════════════════════════════════════════════════════════
        //   Boundary inputs — a crash here is a crash on menu open
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_NullActor_ReturnsFalseWithoutThrowing()
        {
            var clay = factory.CreateEntity("FireClay");
            Assert.That(MaterialUseDescription.TryDescribe(null, clay, factory, out var text), Is.False);
            Assert.That(text, Is.Null, "no partial text escapes a refusal");
        }

        [Test]
        public void Adversarial_NullItem_ReturnsFalse()
        {
            Assert.That(MaterialUseDescription.TryDescribe(player, null, factory, out var text), Is.False);
            Assert.That(text, Is.Null);
        }

        [Test]
        public void Adversarial_ActorWithoutAnInventory_ReturnsFalse()
        {
            // A buggy helper that dereferenced the inventory before checking
            // would throw here; one that skipped the carriage gate would
            // describe an item nobody is carrying.
            var bare = new Entity { ID = "no-pack", BlueprintName = "Bare" };
            var clay = factory.CreateEntity("FireClay");
            Assert.That(MaterialUseDescription.TryDescribe(bare, clay, factory, out _), Is.False);
        }

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_NullFactory_StillNamesTheGuideTheWayThePlayerSeesIt(string material)
        {
            // The factory only supplies the guide's display name. Without it
            // the helper falls back to a literal — which must not drift from
            // the name a real guide actually shows in the inventory.
            var item = Carry(player, material);
            Assert.That(MaterialUseDescription.TryDescribe(player, item, null, out var text), Is.True);
            string seen = factory.CreateEntity(GuideFor(material)).GetDisplayName();
            StringAssert.Contains(seen, text, "fallback literal drifted from the real guide name");
        }

        // ════════════════════════════════════════════════════════════
        //   Ownership — describe only what this actor can actually spend
        // ════════════════════════════════════════════════════════════

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_MaterialInSomeoneElsesPack_IsNotDescribedForThePlayer(string material)
        {
            // Cross-actor: the repair consumer spends from the PLAYER's pack.
            // Advertising another actor's goods would promise a repair the
            // player cannot perform.
            var trader = factory.CreateEntity("Player");
            trader.ID = "other-actor";
            var theirs = Carry(trader, material);
            Assert.That(MaterialUseDescription.TryDescribe(player, theirs, factory, out _), Is.False);
            Assert.That(MaterialUseDescription.TryDescribe(trader, theirs, factory, out _), Is.True,
                "counter-check: the actual owner does get the description");
        }

        [Test]
        public void Adversarial_UncarriedMaterial_IsNotDescribed()
        {
            // A ground or freshly-created copy is not spendable.
            var loose = factory.CreateEntity("FireClay");
            Assert.That(MaterialUseDescription.TryDescribe(player, loose, factory, out _), Is.False);
        }

        [Test]
        public void Adversarial_EmptyStackStillListed_IsNotDescribed()
        {
            // Stack boundary: a zero-count stack can linger in Objects, but
            // CanConsumeOne refuses it — so the repair would refuse too.
            var clay = Carry(player, "FireClay");
            var stack = clay.GetPart<StackerPart>();
            Assume.That(stack, Is.Not.Null, "fire clay ships a stack contract");
            stack.StackCount = 0;
            Assert.That(MaterialUseDescription.TryDescribe(player, clay, factory, out _), Is.False);
            stack.StackCount = 1;
            Assert.That(MaterialUseDescription.TryDescribe(player, clay, factory, out _), Is.True,
                "counter-check: the same row with one unit is described");
        }

        // ════════════════════════════════════════════════════════════
        //   Consumer parity — "(carried)" must mean the repair would pass
        // ════════════════════════════════════════════════════════════

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_CarriedGuideClaim_MatchesTheRepairConsumerExactly(string material)
        {
            // The strongest truth check available: across every guide
            // placement, the "(carried)" / "(missing)" suffix must agree with
            // SettlementManager's own private gate. A helper "improved" to
            // search nested containers would say carried while the repair
            // still refused — a lie the player acts on.
            string guide = GuideFor(material);
            var item = Carry(player, material);

            void Check(string state)
            {
                Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out var text), Is.True, state);
                bool claimsCarried = text.Contains("(carried)");
                Assert.That(claimsCarried, Is.EqualTo(ConsumerHasGuide(player, guide)),
                    state + ": the description and the repair disagree about the guide");
            }

            Check("no guide");

            var sack = factory.CreateEntity("Sack");
            var nested = factory.CreateEntity(guide);
            Assert.That(sack.GetPart<ContainerPart>().AddItem(nested), Is.True);
            Assert.That(Inventory.AddObject(sack), Is.True);
            Check("guide inside a carried sack");

            var other = factory.CreateEntity("Player");
            other.ID = "guide-holder";
            Carry(other, guide);
            Check("guide in another actor's pack");

            var top = Carry(player, guide);
            Check("guide carried at the top level");
            Assert.That(ConsumerHasGuide(player, guide), Is.True,
                "counter-check: the matrix really reaches the carried state");

            Assert.That(Inventory.RemoveObject(top), Is.True);
            Check("guide removed again");
        }

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_TheMaterialItselfIsNotItsOwnGuide(string material)
        {
            // A guide is a book, not a repair good: asking for guidance on
            // the guide must not produce material guidance.
            var guide = Carry(player, GuideFor(material));
            Assert.That(MaterialUseDescription.TryDescribe(player, guide, factory, out _), Is.False);
        }

        // ════════════════════════════════════════════════════════════
        //   No universal claims — lookalikes are not repair goods
        // ════════════════════════════════════════════════════════════

        [TestCase("LampOil")]
        [TestCase("PaleSalt")]
        public void Adversarial_ALookalikeMaterial_GetsNoRepairGuidance(string lookalike)
        {
            // The plan names these explicitly: alchemy LampOil and mineral
            // PaleSalt are NOT interchangeable with ward oil / silver sand.
            // A helper keyed on a broad tag or a display-name fragment would
            // advertise a repair these can never perform.
            var item = Carry(player, lookalike);
            Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out _), Is.False);
        }

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_ARenamedRepairGood_IsStillRecognizedByItsBlueprint(string material)
        {
            // Identity is the blueprint, never the name: a relabelled real
            // repair good still repairs, so it must still be described.
            var item = Carry(player, material);
            item.GetPart<RenderPart>().DisplayName = "unmarked parcel";
            Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out var text), Is.True);
            StringAssert.Contains("unmarked parcel", text, "the header names the item as shown");
        }

        // ════════════════════════════════════════════════════════════
        //   Save/load reach and purity
        // ════════════════════════════════════════════════════════════

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_AReloadedMaterial_IsDescribedIdentically(string material)
        {
            // A saved-and-restored copy must be the same repair good. The
            // helper keys on BlueprintName; a reflection round-trip that
            // dropped or altered it would silently strip the guidance.
            var original = Carry(player, material);
            Assert.That(MaterialUseDescription.TryDescribe(player, original, factory, out var before), Is.True);
            Assert.That(Inventory.RemoveObject(original), Is.True);

            var restored = PartRoundTripHelper.RoundTripEntity(original);
            Assert.That(restored.BlueprintName, Is.EqualTo(material), "identity survives the save");
            Assert.That(Inventory.AddObject(restored), Is.True);
            Assert.That(MaterialUseDescription.TryDescribe(player, restored, factory, out var after), Is.True);
            Assert.That(after, Is.EqualTo(before));
        }

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void Adversarial_DescribingIsPure_RepeatableAndSpendsNothing(string material)
        {
            // Examining must never consume, move or flag anything — a helper
            // that performed a repair "dry run" through the real consumer
            // would spend a unit here.
            var item = Carry(player, material, count: 5);
            int objects = Inventory.Objects.Count;
            int props = player.Properties.Count, ints = player.IntProperties.Count;

            Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out var first), Is.True);
            Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out var second), Is.True);

            Assert.That(second, Is.EqualTo(first), "the same inputs say the same thing");
            Assert.That(item.GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(5), "no unit spent");
            Assert.That(Inventory.Objects.Count, Is.EqualTo(objects));
            Assert.That(player.Properties.Count, Is.EqualTo(props), "no flag written");
            Assert.That(player.IntProperties.Count, Is.EqualTo(ints), "no flag written");
            StringAssert.Contains("One measure", first,
                "a stack of five still describes the per-repair cost, not the stack");
        }

        [Test]
        public void Adversarial_OnlyFireClayAdvertisesTheBell()
        {
            // The bell line is fire clay's second use and nobody else's.
            foreach (var material in Materials)
            {
                var item = Carry(player, material);
                Assert.That(MaterialUseDescription.TryDescribe(player, item, factory, out var text), Is.True);
                bool bell = text.Contains("bell");
                Assert.That(bell, Is.EqualTo(material == "FireClay"), material + " bell claim");
            }
        }
    }
}
