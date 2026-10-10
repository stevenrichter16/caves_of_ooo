using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DescriptorEffectProbe : Effect
    {
        public override string DisplayName => "descriptor";
        public int Value = 61;
    }
    public sealed class DescriptorHookProbe : Part
    {
        public override string Name => "DescriptorHookProbe";
        public static readonly List<string> Calls = new List<string>();
        public int Value;
        public override void OnBeforeSave(SaveWriter writer) => Calls.Add("before:" + Value + ":" + Thread.CurrentThread.ManagedThreadId);
        public override void OnAfterSave(SaveWriter writer) => Calls.Add("after:" + Value + ":" + Thread.CurrentThread.ManagedThreadId);
    }
    public enum DescriptorSignedEnum { Zero }
    public enum DescriptorWideEnum : ulong { TooLarge = ulong.MaxValue }
    public class DescriptorNestedBase { public string Name = "base"; }
    public sealed class DescriptorNestedDerived : DescriptorNestedBase { public int Extra = 83; }
    public sealed class DescriptorFallbackPart : Part
    {
        public override string Name => "DescriptorFallback";
        public int? Optional;
        public DescriptorSignedEnum Signed;
        public DescriptorNestedBase Nested;
        public string Ενέργεια;
        public string ThisPublicFieldNameIntentionallyExceedsTheSingleByteUtf8LengthPrefixBoundaryToVerifyThatCachedMetadataUsesExactlyTheSameCanonicalBinaryWriterFraming = "long field";
        public string Added = "included";
        [NonSerialized] public string Excluded = "not serialized";
        public readonly int Readonly = 81;
        public Action Callback;
    }
    public sealed class DescriptorWideEnumPart : Part
    {
        public override string Name => "DescriptorWideEnum";
        public DescriptorWideEnum Value = DescriptorWideEnum.TooLarge;
    }
    public sealed class DescriptorCollectionPart : Part
    {
        public override string Name => "DescriptorCollection";
        public List<string> List;
        public HashSet<int> Set;
        public int?[] Array;
    }
    public sealed class DescriptorMutatingHookPart : Part
    {
        public override string Name => "DescriptorMutatingHook";
        public int Value = 17;
        public override void OnBeforeSave(SaveWriter writer) => Value++;
        public override void OnAfterSave(SaveWriter writer) => Value--;
    }

    public sealed class SteamDeckSaveDescriptorAdversarialTests
    {
        private const BindingFlags Hidden = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly PropertyInfo Legacy = typeof(SaveWriter).GetProperty("UseLegacyFieldWriters", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo Plan = typeof(SaveGraphSerializer).GetMethod("GetSerializableFieldWriters", Hidden);
        private static readonly MethodInfo Fields = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", Hidden);
        private static readonly Action<object, SaveWriter, Func<FieldInfo, bool>> WriteFields =
            (Action<object, SaveWriter, Func<FieldInfo, bool>>)Delegate.CreateDelegate(typeof(Action<object, SaveWriter, Func<FieldInfo, bool>>),
                typeof(SaveGraphSerializer).GetMethod("WritePublicFields", Hidden));

        private static byte[] Graph(Entity root, bool legacy)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); Legacy.SetValue(writer, legacy);
                writer.WriteEntityReference(root); writer.WriteQueuedEntityBodies(); return stream.ToArray();
            }
        }
        private static byte[] FieldBytes(object value, bool legacy, Func<FieldInfo, bool> include)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); Legacy.SetValue(writer, legacy);
                WriteFields(value, writer, include); return stream.ToArray();
            }
        }
        private static Entity With(Part part)
        {
            var owner = new Entity { ID = "descriptor-adversarial" }; owner.AddPart(part); return owner;
        }
        private static Entity Read(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var reader = new SaveReader(stream, null); var root = reader.ReadEntityReference(); reader.ReadEntityBodies(); return root;
            }
        }

        [Test]
        public void ArbitraryPredicateDoesNotExecuteUnderMetadataLock()
        {
            bool checkedWorker = false;
            Func<FieldInfo, bool> include = field =>
            {
                if (!checkedWorker)
                {
                    checkedWorker = true;
                    var worker = Task.Run(() => Fields.Invoke(null, new object[] { typeof(PhysicsPart), null }));
                    Assert.IsTrue(worker.Wait(1000), "Caller predicates must not hold a lock needed by another metadata query.");
                }
                return true;
            };
            Plan.Invoke(null, new object[] { typeof(RenderPart), include });
            Assert.IsTrue(checkedWorker);
        }

        [TestCase("render")] [TestCase("physics")] [TestCase("thermal")]
        public void EveryDirectFieldReadsItsCurrentValue(string kind)
        {
            Part part = kind == "render" ? (Part)new RenderPart() : kind == "physics" ? new PhysicsPart() : new ThermalPart();
            var owner = new Entity { ID = "all-fields-" + kind }; owner.AddPart(part);
            var fields = (FieldInfo[])Fields.Invoke(null, new object[] { part.GetType(), null });
            byte[] previous = Graph(owner, false);
            foreach (var field in fields)
            {
                object value = field.FieldType == typeof(string) ? (object)(field.Name + "\u03bb\ud83d\ude80")
                    : field.FieldType == typeof(bool) ? !(bool)field.GetValue(part)
                    : field.FieldType == typeof(int) ? -319
                    : field.FieldType == typeof(float) ? -137.25f
                    : field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, -7)
                    : new Entity { ID = field.Name + "-reference" };
                field.SetValue(part, value);
                byte[] current = Graph(owner, false);
                CollectionAssert.AreNotEqual(previous, current, field.Name + " must be read again after cache warmup.");
                CollectionAssert.AreEqual(Graph(owner, true), current, field.Name);
                previous = current;
            }
        }

        [Test]
        public void AliasedCyclicEntityReferencesAndTypeNamesRoundtrip()
        {
            var parent = new Entity { ID = "descriptor-parent" }; var child = new Entity { ID = "descriptor-child" };
            parent.AddPart(new PhysicsPart { InInventory = child, Equipped = child });
            child.AddPart(new PhysicsPart { InInventory = parent });
            byte[] bytes = Graph(parent, false); CollectionAssert.AreEqual(Graph(parent, true), bytes);
            using (var stream = new MemoryStream(bytes))
            {
                var reader = new SaveReader(stream, null); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                var physics = loaded.GetPart<PhysicsPart>(); Assert.AreSame(physics.InInventory, physics.Equipped);
                Assert.AreSame(loaded, physics.InInventory.GetPart<PhysicsPart>().InInventory);
            }
        }

        [Test]
        public void PartHooksKeepOrderAndCallerThreadInBothModes()
        {
            var owner = new Entity { ID = "descriptor-hooks" };
            owner.AddPart(new DescriptorHookProbe { Value = 1 }); owner.AddPart(new DescriptorHookProbe { Value = 2 });
            try
            {
                DescriptorHookProbe.Calls.Clear(); byte[] legacy = Graph(owner, true);
                string[] before = DescriptorHookProbe.Calls.ToArray(); DescriptorHookProbe.Calls.Clear();
                CollectionAssert.AreEqual(legacy, Graph(owner, false)); CollectionAssert.AreEqual(before, DescriptorHookProbe.Calls);
                int thread = Thread.CurrentThread.ManagedThreadId;
                CollectionAssert.AreEqual(new[] { "before:1:" + thread, "after:1:" + thread, "before:2:" + thread, "after:2:" + thread }, before);
            }
            finally { DescriptorHookProbe.Calls.Clear(); }
        }

        [TestCase("effect")] [TestCase("goal")]
        public void FixedCategoryPlansRetainTheirExclusions(string category)
        {
            object value = category == "effect" ? (object)new DescriptorEffectProbe { Duration = 33 } : new WaitGoal(41);
            string filterName = category == "effect" ? "EffectFieldFilter" : "GoalFieldFilter";
            var include = (Func<FieldInfo, bool>)typeof(SaveGraphSerializer).GetField(filterName, Hidden).GetValue(null);
            CollectionAssert.AreEqual(FieldBytes(value, true, include), FieldBytes(value, false, include));
            object first = Plan.Invoke(null, new object[] { value.GetType(), include });
            Assert.AreSame(first, Plan.Invoke(null, new object[] { value.GetType(), include }));
            Assert.AreNotSame(first, Plan.Invoke(null, new object[] { value.GetType(), null }));
            var selected = (FieldInfo[])Fields.Invoke(null, new object[] { value.GetType(), include });
            foreach (var field in selected)
                Assert.IsFalse(field.Name == "Owner" || field.Name == "ParentBrain" || field.Name == "ParentHandler"
                    || (category == "effect" && field.Name == "Duration"));
        }

        [Test]
        public void ArbitraryPredicatesDoNotReuseAnEarlierFilteredPlan()
        {
            bool namesOnly = false;
            Func<FieldInfo, bool> include = field => !namesOnly || field.FieldType == typeof(string);
            var value = new RenderPart(); var full = FieldBytes(value, false, include);
            namesOnly = true; var filtered = FieldBytes(value, false, include);
            CollectionAssert.AreNotEqual(full, filtered);
            CollectionAssert.AreEqual(FieldBytes(value, true, include), filtered);
        }

        [Test]
        public void NewlyDefinedFieldsUseFallbackWhileExcludedFieldsRemainExcluded()
        {
            var part = new DescriptorFallbackPart(); var owner = With(part);
            byte[] before = Graph(owner, false);
            part.Excluded = "changed"; part.Callback = () => { };
            CollectionAssert.AreEqual(before, Graph(owner, false));
            part.Added = "changed included";
            byte[] after = Graph(owner, false);
            CollectionAssert.AreNotEqual(before, after); CollectionAssert.AreEqual(Graph(owner, true), after);
            Assert.AreEqual(part.Added, Read(after).GetPart<DescriptorFallbackPart>().Added);
        }

        [Test]
        public void ShadowedBaseAndDerivedFieldsHaveSeparateCurrentReads()
        {
            var part = new SaveDescriptorDerivedRender(); var owner = With(part);
            ((RenderPart)part).DisplayName = "base value"; part.DisplayName = "derived value";
            byte[] before = Graph(owner, false); CollectionAssert.AreEqual(Graph(owner, true), before);
            ((RenderPart)part).DisplayName = "new base value";
            byte[] baseChanged = Graph(owner, false); CollectionAssert.AreNotEqual(before, baseChanged);
            CollectionAssert.AreEqual(Graph(owner, true), baseChanged);
            part.DisplayName = "new derived value";
            byte[] derivedChanged = Graph(owner, false); CollectionAssert.AreNotEqual(baseChanged, derivedChanged);
            CollectionAssert.AreEqual(Graph(owner, true), derivedChanged);
        }

        [TestCase(false)] [TestCase(true)]
        public void NullableAbsentAndPresentRetainTheirWireAndReadback(bool present)
        {
            var part = new DescriptorFallbackPart { Optional = present ? (int?)int.MinValue : null };
            var owner = With(part); byte[] bytes = Graph(owner, false);
            CollectionAssert.AreEqual(Graph(owner, true), bytes);
            Assert.AreEqual(part.Optional, Read(bytes).GetPart<DescriptorFallbackPart>().Optional);
        }

        [TestCase(false)] [TestCase(true)]
        public void NestedDeclaredAndConcreteTypeDiscriminatorIsPreserved(bool derived)
        {
            var part = new DescriptorFallbackPart { Nested = derived ? new DescriptorNestedDerived() : new DescriptorNestedBase() };
            var owner = With(part); byte[] bytes = Graph(owner, false);
            CollectionAssert.AreEqual(Graph(owner, true), bytes);
            var loaded = Read(bytes).GetPart<DescriptorFallbackPart>().Nested;
            Assert.AreEqual(part.Nested.GetType(), loaded.GetType()); Assert.AreEqual(part.Nested.Name, loaded.Name);
            if (derived) Assert.AreEqual(83, ((DescriptorNestedDerived)loaded).Extra);
        }

        [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void EnumIntegerBoundariesKeepExactConversion(int value)
        {
            var owner = With(new DescriptorFallbackPart { Signed = (DescriptorSignedEnum)value });
            byte[] bytes = Graph(owner, false); CollectionAssert.AreEqual(Graph(owner, true), bytes);
            Assert.AreEqual(value, (int)Read(bytes).GetPart<DescriptorFallbackPart>().Signed);
        }

        [TestCase(false)] [TestCase(true)]
        public void UnsupportedWideEnumRetainsLegacyOverflowFailure(bool legacy)
        {
            var owner = With(new DescriptorWideEnumPart());
            Assert.Throws<OverflowException>(() => Graph(owner, legacy));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void UnicodeMetadataAndStringValueLengthBoundariesStayCanonical(int shape)
        {
            string text = shape == 0 ? null : shape == 1 ? "" : shape == 2 ? "\u03bb\ud83d\ude80" : new string('\u03bb', 128);
            var owner = With(new DescriptorFallbackPart { Ενέργεια = text });
            byte[] bytes = Graph(owner, false); CollectionAssert.AreEqual(Graph(owner, true), bytes);
            var loaded = Read(bytes).GetPart<DescriptorFallbackPart>(); Assert.AreEqual(text, loaded.Ενέργεια);
            Assert.AreEqual("long field", loaded.ThisPublicFieldNameIntentionallyExceedsTheSingleByteUtf8LengthPrefixBoundaryToVerifyThatCachedMetadataUsesExactlyTheSameCanonicalBinaryWriterFraming);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CollectionNullEmptyAndPopulatedFramesRemainDistinct(int shape)
        {
            var part = new DescriptorCollectionPart();
            if (shape > 0)
            { part.List = new List<string>(); part.Set = new HashSet<int>(); part.Array = new int?[0]; }
            if (shape == 2)
            { part.List.Add(null); part.List.Add("\u03bb"); part.Set.Add(41); part.Set.Add(-7); part.Array = new int?[] { null, -9 }; }
            var owner = With(part); byte[] bytes = Graph(owner, false); CollectionAssert.AreEqual(Graph(owner, true), bytes);
            var loaded = Read(bytes).GetPart<DescriptorCollectionPart>();
            if (shape == 0) { Assert.IsNull(loaded.List); Assert.IsNull(loaded.Set); Assert.IsNull(loaded.Array); }
            else { CollectionAssert.AreEqual(part.List, loaded.List); CollectionAssert.AreEquivalent(part.Set, loaded.Set); CollectionAssert.AreEqual(part.Array, loaded.Array); }
        }

        [Test]
        public void HookMutationIsReadAfterBeforeSaveAndRestoredAfterWrite()
        {
            var part = new DescriptorMutatingHookPart(); var owner = With(part);
            byte[] bytes = Graph(owner, false); Assert.AreEqual(17, part.Value);
            Assert.AreEqual(18, Read(bytes).GetPart<DescriptorMutatingHookPart>().Value);
            CollectionAssert.AreEqual(Graph(owner, true), bytes); Assert.AreEqual(17, part.Value);
        }
    }
}
