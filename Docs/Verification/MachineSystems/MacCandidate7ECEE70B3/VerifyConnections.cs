using System;
using System.Linq;
using Mono.Cecil;

static class VerifyConnections
{
    const string Core = "CavesOfOoo.Core.";
    static int checks;

    static void Require(bool result, string description)
    {
        if (!result) throw new InvalidOperationException(description);
        ++checks;
        Console.WriteLine(description);
    }

    static System.Collections.Generic.IEnumerable<TypeDefinition> All(TypeDefinition type)
    {
        yield return type;
        foreach (var child in type.NestedTypes)
            foreach (var nested in All(child)) yield return nested;
    }

    static bool IsPart(TypeDefinition type)
    {
        for (var parent = type.BaseType; parent != null; )
        {
            if (parent.FullName == Core + "Part") return true;
            var local = type.Module.GetType(parent.FullName);
            if (local == null) return false;
            parent = local.BaseType;
        }
        return false;
    }

    static string[] Fields(TypeDefinition type) => type.Fields
        .Where(f => !f.IsStatic).Select(f => f.Name + ":" + f.FieldType.FullName)
        .OrderBy(s => s, StringComparer.Ordinal).ToArray();

    static bool Calls(TypeDefinition type, string name, string method) =>
        All(type).SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
            .Select(i => i.Operand as MethodReference)
            .Any(m => m != null && m.DeclaringType.FullName == Core + name && m.Name == method);

    static void Main(string[] paths)
    {
        using (var current = AssemblyDefinition.ReadAssembly(paths[0]))
        using (var previous = AssemblyDefinition.ReadAssembly(paths[1]))
        {
            var types = current.MainModule.Types.SelectMany(All).ToDictionary(t => t.FullName);
            var oldParts = previous.MainModule.Types.SelectMany(All).Where(IsPart).ToDictionary(t => t.FullName);
            var newParts = types.Values.Where(IsPart).ToDictionary(t => t.FullName);
            Require(oldParts.Count == 437, "Prior package has 437 runtime Part types");
            Require(newParts.Count == oldParts.Count + 2 &&
                newParts.Keys.Except(oldParts.Keys).OrderBy(x => x).SequenceEqual(
                    new[] { Core + "GritMaterialPart", Core + "SiftableStockPart" }),
                "Only GritMaterial and SiftableStock extend the runtime Part set");
            Require(oldParts.All(p => newParts.ContainsKey(p.Key) &&
                Fields(p.Value).SequenceEqual(Fields(newParts[p.Key]))),
                "All 437 prior runtime Part instance field shapes match");
            var grit = types[Core + "GritMaterialPart"];
            var sift = types[Core + "SiftableStockPart"];
            Require(Fields(grit).Length == 0, "Grit capability has no saved instance fields");
            Require(Fields(sift).SequenceEqual(new[] { "Contents:System.String" }),
                "Siftable stock saves its declared constituent manifest");
            Require(Calls(grit, "ZoneTileState", "WriteResidue"),
                "Grit writes the shared tile residue");
            Require(Calls(sift, "MaterialProcessActions", "Handle"),
                "Sifting dispatches through shared material actions");
            Require(Calls(sift, "MaterialProcessTemplate", "Capture"),
                "Size partition admits canonical outputs through the material template");
            Require(Calls(types[Core + "MaterialProcessPlan"], "SiftableStockPart", "Definition"),
                "Shared process plan resolves transient screening definition");
            Require(Calls(types[Core + "MaterialProcessPlan"], "MaterialScreeningWorkReceipt", "Resolve"),
                "Shared process plan checks the actual screening tool");
            Require(types[Core + "RepairCultivationSite"].Methods.Any(m => m.Name=="TryInstallOptionalSpoil"),
                "Packaged allotment has the optional finite spoil placement");
            Console.WriteLine("CHECKS " + checks);
        }
    }
}
