#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class NativeSpellReadabilityImportGuardTests
    {
        static Type Builder => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a=>a.GetType("CavesOfOoo.Editor.NativeSpellFxAssetBuilder",false)).First(t=>t!=null);
        static void Validate(string material, string colors, string semantics)
        {
            var method=Builder.GetMethod("ValidateExport",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method,"Validate incoming Blender data before any owned asset is overwritten.");
            const string pose="{\"position\":[0,0,0],\"rotation\":[0,0,0,1],\"scale\":[1,1,1]}";
            var json="{\"materials\":["+material+"],\"meshes\":[{\"id\":\"piece\",\"vertices\":[0,0,0,1,0,0,0,1,0],\"normals\":[0,0,1,0,0,1,0,0,1],\"triangles\":[0,1,2],\"materialIndex\":0"+colors+"}],\"studies\":[{\"id\":\"ember_spit\",\"pieces\":[{\"id\":\"piece\",\"meshId\":\"piece\","+semantics+",\"poses\":["+string.Join(",",Enumerable.Repeat(pose,111))+"]}]}]}";
            var data=JsonUtility.FromJson(json,Builder.GetNestedType("Export",BindingFlags.NonPublic));
            try { method.Invoke(null,new[]{data}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        const string Solid="{\"rgbaSrgb\":[1,0.5,0.1,1],\"emission\":0.8}";
        const string Halo="{\"rgbaSrgb\":[1,0.5,0.1,1],\"emission\":0.2,\"glow\":true}";
        const string Gradient=",\"vertexColors\":[1,1,1,0,1,1,1,1,1,1,1,0.4]";
        const string Semantics="\"role\":\"TargetImpact\",\"anchor\":\"Target\",\"condition\":\"Always\",\"visualBounds\":\"AirborneDecoration\"";
        [Test] public void ValidSolidAndGradientGlowPassPreflightWithoutImportingAssets()
        { Assert.DoesNotThrow(()=>Validate(Solid,"",Semantics)); Assert.DoesNotThrow(()=>Validate(Halo,Gradient,Semantics)); }
        [TestCase("emission")] [TestCase("gradient-count")] [TestCase("gradient-bounds")]
        [TestCase("missing-gradient")] [TestCase("flat-gradient")] [TestCase("unknown-bounds")]
        public void MalformedNewArtMetadataIsRejectedBeforeAssetReplacement(string fault)
        {
            var mat=Halo;var colors=Gradient;var semantics=Semantics;
            if(fault=="emission")mat=Halo.Replace(":0.2",":1.2");
            if(fault=="gradient-count")colors=",\"vertexColors\":[1,1,1,0]";
            if(fault=="gradient-bounds")colors=Gradient.Replace(",0.4]",",1.4]");
            if(fault=="missing-gradient")colors="";
            if(fault=="flat-gradient")colors=",\"vertexColors\":[1,1,1,1,1,1,1,1,1,1,1,1]";
            if(fault=="unknown-bounds")semantics=Semantics.Replace("AirborneDecoration","NotABoundsKind");
            Assert.Throws<InvalidOperationException>(()=>Validate(mat,colors,semantics));
            Assert.DoesNotThrow(()=>Validate(Halo,Gradient,Semantics));
        }
    }
}
#endif
