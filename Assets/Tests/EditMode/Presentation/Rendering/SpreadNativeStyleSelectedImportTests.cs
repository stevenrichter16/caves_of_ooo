#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Explicit editor import boundaries, not importer execution in tests.
    /// All entrypoint calls below either validate without writes or must refuse
    /// before writes; disk and accepted object state are compared independently.</summary>
    public sealed class SpreadNativeStyleSelectedImportTests
    {
        static readonly string[] Pools = Enumerable.Range(0,4).Select(i=>"ring-spray-pool-"+i).ToArray();
        static MethodInfo Method(string name)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.SpreadNativeStyleBuilder")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Actual editor importer must be loaded.");
            var method=type.GetMethod(name,BindingFlags.Static|BindingFlags.Public);
            Assert.NotNull(method,"The bounded selected-import preflight must exist before repairing four outputs.");
            return method;
        }
        static void Invoke(string name,string[] selection)
        {
            try { Method(name).Invoke(null,new object[]{selection}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        [TestCase("null")][TestCase("empty")][TestCase("duplicate")][TestCase("unknown")]
        public void InvalidSelectionRefusesBeforeAnyPersistentWrite(string fault)
        {
            string[] selected=fault=="null"?null:fault=="empty"?Array.Empty<string>():fault=="duplicate"?new[]{Pools[0],Pools[0]}:new[]{"ring-spray-pool-999"};
            var before=Snapshot(); var library=SpreadNativeStyle3DLibrary.Load(); Assert.NotNull(library);
            var entries=library.Entries; var rows=entries.ToArray();
            try { Assert.Throws<ArgumentException>(()=>Invoke("RunSelected",selected)); }
            finally { Unchanged(before); Assert.AreSame(entries,library.Entries); for(int i=0;i<rows.Length;i++)Assert.AreSame(rows[i],library.Entries[i]); }
        }
        [Test] public void ValidSelectionPreflightIsReadOnlyAndRetainsEveryCurrentEntry()
        {
            var library=SpreadNativeStyle3DLibrary.Load(); Assert.NotNull(library); library.Validate();
            var before=Snapshot(); var entries=library.Entries; var rows=entries.ToArray();
            Invoke("ValidateSelectedImport",Pools);
            Unchanged(before); Assert.AreSame(entries,library.Entries); for(int i=0;i<rows.Length;i++)Assert.AreSame(rows[i],library.Entries[i]);
        }
        [Test] public void MalformedUnselectedEntryBlocksTheWholeSelectedImportWithoutWrites()
        {
            var library=SpreadNativeStyle3DLibrary.Load(); Assert.NotNull(library); library.Validate();
            var before=Snapshot(); var entries=library.Entries; var retained=entries.First(e=>!Pools.Contains(e.Id)); var mesh=retained.Mesh;
            try
            {
                retained.Mesh=null; library.InvalidateCaches();
                Assert.Throws<InvalidOperationException>(()=>Invoke("RunSelected",Pools),"A four-model repair cannot retain an unverified foreign/malformed neighbor.");
            }
            finally
            {
                retained.Mesh=mesh; library.InvalidateCaches(); library.Validate();
                Unchanged(before); Assert.AreSame(entries,library.Entries);
            }
        }
        static Dictionary<string,string> Snapshot()
        {
            var paths=Directory.GetFiles(SpreadNativeStyle3DLibrary.Folder,"*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Art3D/VoxelWorld","*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles("Assets/Resources/VoxelWorld","*",SearchOption.AllDirectories));
            var result=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var path in paths.OrderBy(x=>x,StringComparer.Ordinal))result.Add(path,Hash(path));
            return result;
        }
        static string Hash(string path)
        {
            using(var hash=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream));
        }
        static void Unchanged(Dictionary<string,string> before)
        {
            var after=Snapshot(); CollectionAssert.AreEquivalent(before.Keys,after.Keys,"No output files may be created or deleted by refused/read-only preflight.");
            foreach(var row in before)Assert.AreEqual(row.Value,after[row.Key],row.Key);
        }
    }
}
#endif
