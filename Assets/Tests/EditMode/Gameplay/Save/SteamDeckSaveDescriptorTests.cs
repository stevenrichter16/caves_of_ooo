using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SaveDescriptorNested
    {
        public string Text;
        public int Count;
    }

    public class SaveDescriptorProbePart : Part
    {
        public override string Name => "SaveDescriptorProbe";
        public int Number = -19;
        public long Wide = long.MaxValue;
        public float Float = -1.25f;
        public double Double = double.Epsilon;
        public bool Flag = true;
        public char Character = '\u03bb';
        public string Text = "\u03bb\ud83d\ude80";
        public Guid Identity = new Guid("11111111222233334444555555555555");
        public int? Optional;
        public Entity Reference;
        public int[] Numbers = { 1, -2, int.MaxValue };
        public List<Entity> References = new List<Entity>();
        public HashSet<string> Labels = new HashSet<string> { "a", "\u03bb" };
        public SaveDescriptorNested Nested = new SaveDescriptorNested { Text = "nested", Count = 42 };
        [NonSerialized] public int Omitted = 17;
        public readonly int Readonly = 23;
    }

    public sealed class SaveDescriptorDerivedRender : RenderPart
    {
        public new string DisplayName = "shadow";
        public int AddedField = 79;
    }

    public sealed class SteamDeckSaveDescriptorTests
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Func<Type, Func<FieldInfo, bool>, FieldInfo[]> Fields =
            (Func<Type, Func<FieldInfo, bool>, FieldInfo[]>)Delegate.CreateDelegate(
                typeof(Func<Type, Func<FieldInfo, bool>, FieldInfo[]>),
                typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", Static));
        private static readonly Action<Type, object, SaveWriter> Value =
            (Action<Type, object, SaveWriter>)Delegate.CreateDelegate(typeof(Action<Type, object, SaveWriter>),
                typeof(SaveGraphSerializer).GetMethod("WriteFieldValue", Static));
        private static readonly Action<object, SaveWriter, Func<FieldInfo, bool>> Current =
            (Action<object, SaveWriter, Func<FieldInfo, bool>>)Delegate.CreateDelegate(
                typeof(Action<object, SaveWriter, Func<FieldInfo, bool>>),
                typeof(SaveGraphSerializer).GetMethod("WritePublicFields", Static));

        private static byte[] Serialize(object value, bool legacy, Func<FieldInfo, bool> include = null)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream);
                if (legacy)
                {
                    // Independent legacy top-level field loop, retained in tests.
                    var fields = Fields(value.GetType(), include);
                    writer.Write(fields.Length);
                    foreach (var field in fields)
                    {
                        writer.WriteString(field.Name);
                        Value(field.FieldType, field.GetValue(value), writer);
                    }
                }
                else Current(value, writer, include);
                writer.WriteQueuedEntityBodies();
                return stream.ToArray();
            }
        }

        [TestCase("render")] [TestCase("physics")] [TestCase("thermal")]
        [TestCase("derived")] [TestCase("fallback")]
        public void CurrentFieldValuesRetainExactLegacyBytes(string kind)
        {
            object part = kind == "render" ? (object)new RenderPart { DisplayName = "\u03bb\ud83d\ude80", Tile = null, RenderLayer = -42 }
                : kind == "physics" ? new PhysicsPart { Solid = true, Weight = int.MaxValue, Category = "" }
                : kind == "thermal" ? new ThermalPart { Temperature = float.NaN, FlameTemperature = float.PositiveInfinity }
                : kind == "derived" ? new SaveDescriptorDerivedRender()
                : new SaveDescriptorProbePart();
            CollectionAssert.AreEqual(Serialize(part, true), Serialize(part, false));
        }

        [Test]
        public void DirectMutationAfterWarmupChangesBytesAndStillMatchesLegacy()
        {
            var part = new RenderPart { DisplayName = "first", Visible = true, RenderLayer = 3 };
            byte[] before = Serialize(part, false);
            part.DisplayName = null; part.Visible = false; part.RenderLayer = int.MinValue;
            byte[] after = Serialize(part, false);
            CollectionAssert.AreNotEqual(before, after);
            CollectionAssert.AreEqual(Serialize(part, true), after);
        }

        [Test]
        public void NullableCollectionsAndAliasedReferencesRetainFraming()
        {
            var child = new Entity { ID = "descriptor-child", BlueprintName = "probe" };
            var part = new SaveDescriptorProbePart { Reference = child };
            part.References.Add(child); part.References.Add(null); part.References.Add(child);
            byte[] absent = Serialize(part, false);
            CollectionAssert.AreEqual(Serialize(part, true), absent);
            part.Optional = int.MinValue; part.Nested = null; part.Numbers = null; part.Text = "";
            byte[] present = Serialize(part, false);
            CollectionAssert.AreNotEqual(absent, present);
            CollectionAssert.AreEqual(Serialize(part, true), present);
        }

        [Test]
        public void MutableIncludePredicateIsEvaluatedForEveryWrite()
        {
            var part = new SaveDescriptorProbePart(); bool includeText = false;
            Func<FieldInfo, bool> include = field => field.Name != "Text" || includeText;
            byte[] without = Serialize(part, false, include);
            CollectionAssert.AreEqual(Serialize(part, true, include), without);
            includeText = true;
            byte[] with = Serialize(part, false, include);
            CollectionAssert.AreNotEqual(without, with);
            CollectionAssert.AreEqual(Serialize(part, true, include), with);
        }

        [Test]
        public void ImmutableFieldWriterPlanIsReused()
        {
            var method = typeof(SaveGraphSerializer).GetMethod("GetSerializableFieldWriters", Static);
            Assert.NotNull(method, "Repeated owners need cached write plans, not repeated reflection/type classification.");
            object first = method.Invoke(null, new object[] { typeof(RenderPart), null });
            Assert.AreSame(first, method.Invoke(null, new object[] { typeof(RenderPart), null }));
            Assert.AreNotSame(first, method.Invoke(null, new object[] { typeof(ThermalPart), null }));
        }

        [Test]
        public void CommonPrimitiveFieldLoopsDoNotAllocateBoxesAfterWarmup()
        {
            long counterBefore = GC.GetAllocatedBytesForCurrentThread();
            var counterProbe = new byte[128 * 1024];
            long counterDelta = GC.GetAllocatedBytesForCurrentThread() - counterBefore;
            GC.KeepAlive(counterProbe);
            if (counterDelta < counterProbe.Length)
                Assert.Ignore("This runtime does not expose a usable per-thread allocation counter; use native profiler allocations.");
            var parts = new object[] { new RenderPart(), new PhysicsPart(), new ThermalPart() };
            using (var stream = new MemoryStream(2 * 1024 * 1024))
            {
                var writer = new SaveWriter(stream);
                foreach (var part in parts) Current(part, writer, null);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                    foreach (var part in parts) Current(part, writer, null);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.LessOrEqual(allocated, 2048,
                    "The warm common-field path must avoid reflection value boxing and repeated type metadata allocations.");
                Assert.Greater(stream.Length, 200000, "The work check must actually emit every field.");
            }
        }

        [Test]
        public void LegacyVerificationModeBelongsToEachWriter()
        {
            var property = typeof(SaveWriter).GetProperty("UseLegacyFieldWriters", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(property, "Paired graph measurements must select the legacy loop without a process-global switch.");
            using (var a = new MemoryStream()) using (var b = new MemoryStream())
            {
                var first = new SaveWriter(a); var second = new SaveWriter(b);
                property.SetValue(first, true);
                Assert.AreEqual(true, property.GetValue(first));
                Assert.AreEqual(false, property.GetValue(second));
            }
        }
    }
}
