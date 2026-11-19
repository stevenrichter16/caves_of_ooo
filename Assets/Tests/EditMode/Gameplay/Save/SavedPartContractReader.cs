using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Reads the saved shape of every Part in the game assembly so that removing, renaming, moving or retyping a
    /// saved Part or field becomes a failing test (Docs/MACHINE-ARCHITECTURE-SPEC.md, contract C1). Saves persist the
    /// assembly-qualified Part type name and reflect public fields by name, so an unresolvable type name desyncs the
    /// rest of the entity's stream; this contract is the guard. Additions are never violations.
    /// </summary>
    internal static class SavedPartContractReader
    {
        static readonly Type[] CustomSerialized =
            { typeof(StatusEffectsPart), typeof(BitLockerPart), typeof(InventoryPart), typeof(ActivatedAbilitiesPart), typeof(Body), typeof(BrainPart) };

        static readonly MethodInfo FieldsOf = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", BindingFlags.NonPublic | BindingFlags.Static);

        internal static bool IsCustom(Type type)
            => typeof(ISaveSerializable).IsAssignableFrom(type) || CustomSerialized.Any(c => c.IsAssignableFrom(type));

        internal static FieldInfo[] SerializedFields(Type type)
            => (FieldInfo[])FieldsOf.Invoke(null, new object[] { type, null });

        internal static IEnumerable<Type> AllParts()
            => typeof(Part).Assembly.GetTypes().Where(t => typeof(Part).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

        static readonly MethodInfo SerializableTypeCheck = typeof(SaveGraphSerializer).GetMethod("CanSerializeType", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>The nested payload class behind a field type (array, list, hash set or nullable of a serialisable class), or null.</summary>
        static Type PayloadClass(Type type, out bool collection)
        {
            collection = false; type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsArray) { collection = true; type = type.GetElementType(); }
            else if (type.IsGenericType && type.GetGenericArguments().Length == 1 && (typeof(System.Collections.IList).IsAssignableFrom(type) || type.GetGenericTypeDefinition() == typeof(HashSet<>)))
            { collection = true; type = type.GetGenericArguments()[0]; }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(Entity) || type.IsAbstract || typeof(Part).IsAssignableFrom(type)
                || typeof(Delegate).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type) || !type.IsClass || type.GetConstructor(Type.EmptyTypes) == null) return null;
            return type;
        }

        static Type EnumOf(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsArray) type = type.GetElementType();
            else if (type.IsGenericType && type.GetGenericArguments().Length == 1) type = type.GetGenericArguments()[0];
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsEnum ? type : null;
        }

        static void EmitField(StringBuilder sb, string path, FieldInfo field, HashSet<Type> seen)
        {
            sb.Append("  ").Append(path).Append(':').Append(field.FieldType).Append('\n');
            var enumType = EnumOf(field.FieldType);
            if (enumType != null)   // enums serialise as their integer value, so renumbering a member silently corrupts saved data
                sb.Append("  ").Append(path).Append("#enum:").Append(string.Join(",", Enum.GetNames(enumType).Select(n => n + "=" + Convert.ToInt32(Enum.Parse(enumType, n))).OrderBy(x => x, StringComparer.Ordinal))).Append('\n');
            var nested = PayloadClass(field.FieldType, out bool collection);
            if (nested == null || seen.Contains(nested)) return;
            var next = new HashSet<Type>(seen) { nested };
            foreach (var inner in SerializedFields(nested).OrderBy(f => f.Name, StringComparer.Ordinal))
                EmitField(sb, path + (collection ? "[]." : ".") + inner.Name, inner, next);
        }

        static string Hooks(Type type)
        {
            var hooks = new List<string>();
            foreach (var name in new[] { "OnBeforeSave", "OnAfterSave" })
            {
                var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(SaveWriter) }, null);
                if (method != null && method.DeclaringType != typeof(Part)) hooks.Add(name);
            }
            return string.Join(",", hooks);
        }

        /// <summary>Deterministic text. Header "# parts=N", then one block per Part: "Part FullName | reflected|custom", followed (reflected
        /// only) by "  field:Type" lines in ordinal order; nested payload classes are expanded as "  field[].inner:Type" paths, enum fields add a
        /// "  field#enum:A=0,B=1" table, and a Part that overrides OnBeforeSave/OnAfterSave adds "  #hooks:..." (a hidden wire format).</summary>
        internal static string Snapshot()
        {
            var parts = AllParts().ToList(); var sb = new StringBuilder();
            sb.Append("# parts=").Append(parts.Count).Append('\n');
            foreach (var type in parts)
            {
                bool custom = IsCustom(type);
                sb.Append("Part ").Append(type.FullName).Append(" | ").Append(custom ? "custom" : "reflected").Append('\n');
                string hooks = Hooks(type); if (hooks.Length > 0) sb.Append("  #hooks:").Append(hooks).Append('\n');
                if (custom) continue;
                foreach (var field in SerializedFields(type).OrderBy(f => f.Name, StringComparer.Ordinal)) EmitField(sb, field.Name, field, new HashSet<Type>());
            }
            return sb.ToString();
        }

        sealed class Block { public string Kind; public Dictionary<string, string> Fields = new Dictionary<string, string>(); }
        static Dictionary<string, Block> Parse(string text)
        {
            var map = new Dictionary<string, Block>(); Block current = null;
            foreach (var raw in (text ?? "").Split('\n'))
            {
                var line = raw.TrimEnd('\r'); if (line.Length == 0 || line[0] == '#') continue;
                if (line.StartsWith("Part ", StringComparison.Ordinal))
                {
                    var rest = line.Substring(5); int bar = rest.LastIndexOf(" | ", StringComparison.Ordinal);
                    current = new Block { Kind = bar < 0 ? "" : rest.Substring(bar + 3) };
                    map[bar < 0 ? rest : rest.Substring(0, bar)] = current;
                }
                else if (current != null && line.StartsWith("  ", StringComparison.Ordinal))
                {
                    var f = line.Substring(2); int colon = f.IndexOf(':'); if (colon > 0) current.Fields[f.Substring(0, colon)] = f.Substring(colon + 1);
                }
            }
            return map;
        }

        static Dictionary<string, string> EnumTable(string text)
            => (text ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=')).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);

        /// <summary>Violations only: a golden Part missing (removed, renamed or moved); a golden field (including a nested payload field) missing; a
        /// field whose type changed; an enum member removed or renumbered (appending is fine); a save hook added, removed or changed; a reflected Part
        /// that became custom or the reverse. A Part or field added since is never a violation.</summary>
        internal static IReadOnlyList<string> Compare(string golden, string current)
        {
            var violations = new List<string>(); var g = Parse(golden); var c = Parse(current);
            foreach (var part in g)
            {
                if (!c.TryGetValue(part.Key, out var now)) { violations.Add("missing Part " + part.Key); continue; }
                if (part.Value.Kind != now.Kind) violations.Add("serialization kind changed for " + part.Key + ": " + part.Value.Kind + " -> " + now.Kind);
                part.Value.Fields.TryGetValue("#hooks", out var goldenHooks); now.Fields.TryGetValue("#hooks", out var currentHooks);
                if ((goldenHooks ?? "") != (currentHooks ?? "")) violations.Add("save hooks changed for " + part.Key + ": '" + goldenHooks + "' -> '" + currentHooks + "'");
                foreach (var field in part.Value.Fields)
                {
                    if (field.Key == "#hooks") continue;
                    if (!now.Fields.TryGetValue(field.Key, out var type)) { violations.Add("missing field " + part.Key + "." + field.Key); continue; }
                    if (field.Key.EndsWith("#enum", StringComparison.Ordinal))
                    {
                        var was = EnumTable(field.Value); var is_ = EnumTable(type);
                        foreach (var member in was)
                            if (!is_.TryGetValue(member.Key, out var number)) violations.Add("enum member removed " + part.Key + "." + field.Key + ": " + member.Key);
                            else if (number != member.Value) violations.Add("enum member renumbered " + part.Key + "." + field.Key + ": " + member.Key + "=" + member.Value + " -> " + number);
                    }
                    else if (type != field.Value) violations.Add("retyped field " + part.Key + "." + field.Key + ": " + field.Value + " -> " + type);
                }
            }
            return violations;
        }

        /// <summary>Hygiene for any Part added after the golden (section 3 of the spec): game namespace, public parameterless
        /// constructor, every public data field serializable, and no positional-authority field names.</summary>
        internal static IReadOnlyList<string> Hygiene(Type partType)
        {
            var problems = new List<string>();
            if (partType.Namespace != "CavesOfOoo.Core") problems.Add("namespace " + partType.Namespace + " must be CavesOfOoo.Core");
            if (partType.GetConstructor(Type.EmptyTypes) == null) problems.Add("no public parameterless constructor");
            var saved = new HashSet<string>(SerializedFields(partType).Select(f => f.Name));
            foreach (var field in partType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.IsInitOnly || field.IsStatic || field.IsDefined(typeof(NonSerializedAttribute), false) || field.Name == nameof(Part.ParentEntity)) continue;
                if (!saved.Contains(field.Name)) problems.Add("public field " + field.Name + " is not serializable");
                if (field.Name == "ZoneID" || field.Name == "StationX" || field.Name == "StationY"
                    || field.Name.EndsWith("ZoneID", StringComparison.Ordinal) || field.Name.EndsWith("ReceiverID", StringComparison.Ordinal))
                    problems.Add("positional authority field " + field.Name);
            }
            return problems;
        }
    }
}
