using System;
using System.Linq;
using Mono.Cecil;
class DumpWellSupply
{
    static void Main(string[] args)
    {
        using (var assembly = AssemblyDefinition.ReadAssembly(args[0]))
        {
            var type = assembly.MainModule.Types.Single(t => t.FullName == "CavesOfOoo.Core.WellDrinkingService");
            var method = type.Methods.Single(m => m.Name == "SourceAvailable");
            Console.WriteLine(method.FullName);
            foreach (var instruction in method.Body.Instructions) Console.WriteLine(instruction);
        }
    }
}
