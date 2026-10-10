using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CavesOfOoo.Core
{
    public static partial class SaveGraphSerializer
    {
        private sealed class SerializableFieldWriter
        {
            internal readonly byte[] EncodedName;
            internal readonly Action<object, SaveWriter> WriteCurrentValue;
            internal SerializableFieldWriter(FieldInfo field)
            {
                EncodedName = EncodeMetadataString(field.Name);
                WriteCurrentValue = DirectFieldWriter(field) ?? ReflectionFieldWriter(field);
            }
        }

        private static readonly Dictionary<Type, SerializableFieldWriter[]> PublicWriterCache = new Dictionary<Type, SerializableFieldWriter[]>();
        private static readonly Dictionary<Type, SerializableFieldWriter[]> EffectWriterCache = new Dictionary<Type, SerializableFieldWriter[]>();
        private static readonly Dictionary<Type, SerializableFieldWriter[]> GoalWriterCache = new Dictionary<Type, SerializableFieldWriter[]>();
        private static readonly Dictionary<FieldInfo, SerializableFieldWriter> FieldWriterCache = new Dictionary<FieldInfo, SerializableFieldWriter>();
        private static readonly Dictionary<Type, byte[]> EncodedTypeNameCache = new Dictionary<Type, byte[]>();

        private static SerializableFieldWriter[] GetSerializableFieldWriters(Type type, Func<FieldInfo, bool> include)
        {
            // Arbitrary predicates can close over mutable values. Only the fixed
            // serializer categories have reusable field-list plans.
            var cache = include == null ? PublicWriterCache
                : ReferenceEquals(include, EffectFieldFilter) ? EffectWriterCache
                : ReferenceEquals(include, GoalFieldFilter) ? GoalWriterCache : null;
            if (cache == null)
            {
                // Match the existing metadata API: caller-supplied predicates
                // execute outside our lock and may make other metadata queries.
                var selected = GetSerializablePublicFields(type, include);
                lock (FieldCacheGate) return CreateFieldWriterPlan(selected);
            }
            lock (FieldCacheGate)
            {
                if (cache.TryGetValue(type, out var existing)) return existing;
                var plan = CreateFieldWriterPlan(GetSerializablePublicFields(type, include));
                cache.Add(type, plan);
                return plan;
            }
        }

        private static SerializableFieldWriter[] CreateFieldWriterPlan(FieldInfo[] fields)
        {
            var plan = new SerializableFieldWriter[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                if (!FieldWriterCache.TryGetValue(fields[i], out var fieldWriter))
                {
                    fieldWriter = new SerializableFieldWriter(fields[i]);
                    FieldWriterCache.Add(fields[i], fieldWriter);
                }
                plan[i] = fieldWriter;
            }
            return plan;
        }

        private static byte[] EncodeMetadataString(string value)
        {
            // Use the existing writer itself so null flags, UTF-8 fallback and
            // 7-bit byte lengths cannot drift from the format being preserved.
            using (var stream = new MemoryStream())
            {
                new SaveWriter(stream).WriteString(value);
                return stream.ToArray();
            }
        }

        private static void WriteTypeName(Type type, SaveWriter writer)
        {
            if (writer.UseLegacyFieldWriters) { writer.WriteString(GetTypeName(type)); return; }
            byte[] bytes;
            lock (FieldCacheGate)
            {
                if (!EncodedTypeNameCache.TryGetValue(type, out bytes))
                {
                    bytes = EncodeMetadataString(GetTypeName(type));
                    EncodedTypeNameCache.Add(type, bytes);
                }
            }
            writer.WriteBytes(bytes);
        }

        private static Action<object, SaveWriter> ReflectionFieldWriter(FieldInfo field)
        {
            // Classify the scalar type once even when a new/unrecognized field
            // needs reflection to read it. Nested framing stays on the proven path.
            Type type = field.FieldType;
            if (type == typeof(int)) return (obj, writer) => writer.Write((int)field.GetValue(obj));
            if (type == typeof(long)) return (obj, writer) => writer.Write((long)field.GetValue(obj));
            if (type == typeof(float)) return (obj, writer) => writer.Write((float)field.GetValue(obj));
            if (type == typeof(double)) return (obj, writer) => writer.Write((double)field.GetValue(obj));
            if (type == typeof(bool)) return (obj, writer) => writer.Write((bool)field.GetValue(obj));
            if (type == typeof(char)) return (obj, writer) => writer.Write((char)field.GetValue(obj));
            if (type == typeof(string)) return (obj, writer) => writer.WriteString((string)field.GetValue(obj));
            if (type == typeof(Guid)) return (obj, writer) => writer.WriteGuid((Guid)field.GetValue(obj));
            if (type.IsEnum) return (obj, writer) => writer.Write(Convert.ToInt32(field.GetValue(obj)));
            if (type == typeof(Entity)) return (obj, writer) => writer.WriteEntityReference((Entity)field.GetValue(obj));
            return (obj, writer) => WriteFieldValue(type, field.GetValue(obj), writer);
        }

        private static Action<object, SaveWriter> DirectFieldWriter(FieldInfo field)
        {
            // Ordinary C# accessors work on Mono and AOT without expression/IL
            // compilation. Match declaring type as well as name: derived fields
            // may shadow a base field. Newly added fields automatically fall back.
            if (field.DeclaringType == typeof(RenderPart))
            {
                switch (field.Name)
                {
                    case nameof(RenderPart.DisplayName): return (o, w) => w.WriteString(((RenderPart)o).DisplayName);
                    case nameof(RenderPart.RenderString): return (o, w) => w.WriteString(((RenderPart)o).RenderString);
                    case nameof(RenderPart.ColorString): return (o, w) => w.WriteString(((RenderPart)o).ColorString);
                    case nameof(RenderPart.BackgroundColor): return (o, w) => w.WriteString(((RenderPart)o).BackgroundColor);
                    case nameof(RenderPart.GlyphVariants): return (o, w) => w.WriteString(((RenderPart)o).GlyphVariants);
                    case nameof(RenderPart.DetailColor): return (o, w) => w.WriteString(((RenderPart)o).DetailColor);
                    case nameof(RenderPart.TileColor): return (o, w) => w.WriteString(((RenderPart)o).TileColor);
                    case nameof(RenderPart.Tile): return (o, w) => w.WriteString(((RenderPart)o).Tile);
                    case nameof(RenderPart.VisualID): return (o, w) => w.WriteString(((RenderPart)o).VisualID);
                    case nameof(RenderPart.VisualVariant): return (o, w) => w.WriteString(((RenderPart)o).VisualVariant);
                    case nameof(RenderPart.VisualFacing): return (o, w) => w.Write((int)((RenderPart)o).VisualFacing);
                    case nameof(RenderPart.RenderLayer): return (o, w) => w.Write(((RenderPart)o).RenderLayer);
                    case nameof(RenderPart.Visible): return (o, w) => w.Write(((RenderPart)o).Visible);
                }
            }
            if (field.DeclaringType == typeof(PhysicsPart))
            {
                switch (field.Name)
                {
                    case nameof(PhysicsPart.Solid): return (o, w) => w.Write(((PhysicsPart)o).Solid);
                    case nameof(PhysicsPart.Weight): return (o, w) => w.Write(((PhysicsPart)o).Weight);
                    case nameof(PhysicsPart.Takeable): return (o, w) => w.Write(((PhysicsPart)o).Takeable);
                    case nameof(PhysicsPart.Category): return (o, w) => w.WriteString(((PhysicsPart)o).Category);
                    case nameof(PhysicsPart.InInventory): return (o, w) => w.WriteEntityReference(((PhysicsPart)o).InInventory);
                    case nameof(PhysicsPart.Equipped): return (o, w) => w.WriteEntityReference(((PhysicsPart)o).Equipped);
                }
            }
            if (field.DeclaringType == typeof(ThermalPart))
            {
                switch (field.Name)
                {
                    case nameof(ThermalPart.Temperature): return (o, w) => w.Write(((ThermalPart)o).Temperature);
                    case nameof(ThermalPart.FlameTemperature): return (o, w) => w.Write(((ThermalPart)o).FlameTemperature);
                    case nameof(ThermalPart.VaporTemperature): return (o, w) => w.Write(((ThermalPart)o).VaporTemperature);
                    case nameof(ThermalPart.FreezeTemperature): return (o, w) => w.Write(((ThermalPart)o).FreezeTemperature);
                    case nameof(ThermalPart.BrittleTemperature): return (o, w) => w.Write(((ThermalPart)o).BrittleTemperature);
                    case nameof(ThermalPart.HeatCapacity): return (o, w) => w.Write(((ThermalPart)o).HeatCapacity);
                    case nameof(ThermalPart.AmbientDecayRate): return (o, w) => w.Write(((ThermalPart)o).AmbientDecayRate);
                    case nameof(ThermalPart.AmbientTemperature): return (o, w) => w.Write(((ThermalPart)o).AmbientTemperature);
                }
            }
            return null;
        }
    }
}
