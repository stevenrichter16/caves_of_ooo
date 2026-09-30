using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Owned, bounded contact color field derived from current visible
    /// static kit geometry. It neither casts gameplay light nor changes source
    /// meshes, native owners, remembered cells, actors or equipment.</summary>
    internal sealed class ReferenceGladeGroundContact:IDisposable
    {
        internal const float MaximumAttenuation=.38f;
        // 52 glade variants plus the 56 approved Spread environment variants.
        private const int MaximumSources=108,MaximumContributors=4096;
        private const ulong Offset=14695981039346656037UL,Prime=1099511628211UL;
        private static readonly int FieldId=Shader.PropertyToID("_GroundContact"),StrengthId=Shader.PropertyToID("_GroundContactStrength");
        private sealed class Source
        {public ReferenceGladeContactGeometry.Footprint[] Bases;public bool Ground;public uint Index;}
        private readonly Dictionary<string,Source> sources=new Dictionary<string,Source>(StringComparer.Ordinal);
        private readonly List<ReferenceGladeContactGeometry.Placed> placed=new List<ReferenceGladeContactGeometry.Placed>(4096);
        private readonly byte[] pixels=new byte[ReferenceGladeContactGeometry.Width*ReferenceGladeContactGeometry.Height];
        private readonly Color32[] upload=new Color32[ReferenceGladeContactGeometry.Width*ReferenceGladeContactGeometry.Height];
        private Material material;private Texture2D mask;private bool disposed,valid,hasSignature;
        private ulong signature;
        internal ReferenceGladeGroundContact(ReferenceGladeVoxelLibrary library,Material ownedMaterial,SpreadEnvironment3DLibrary environment=null)
        {
            if(library==null||ownedMaterial==null||ReferenceEquals(library.Material,ownedMaterial)
                ||!ownedMaterial.HasProperty(FieldId)||!ownedMaterial.HasProperty(StrengthId))
                throw new ArgumentException("Contact field requires its surface-owned palette clone.");
            library.Validate();
            if(library.Entries.Length>MaximumSources)throw new ArgumentException("Contact source budget exceeded.");
            // Library validation guarantees identity transforms, one real readable
            // mesh and the expected material. No unverified prefab scale is assumed.
            for(int i=0;i<library.Entries.Length;i++)
            {var e=library.Entries[i];sources.Add(e.Id,new Source{Bases=ExtractFootprints(e.Mesh),Ground=e.Spec.kind=="ground",Index=(uint)i});}
            if(environment!=null)
            {
                environment.Validate();if(environment.Material!=library.Material||sources.Count+environment.Entries.Length>MaximumSources)
                    throw new ArgumentException("Invalid additional approved contact source family.");
                foreach(var e in environment.Entries)sources.Add(e.Id,new Source{Bases=ExtractFootprints(e.Mesh),Ground=e.Spec.kind=="ground",Index=(uint)sources.Count});
            }
            material=ownedMaterial;
            try
            {
                mask=new Texture2D(ReferenceGladeContactGeometry.Width,ReferenceGladeContactGeometry.Height,TextureFormat.RGBA32,false,true)
                {name="Owned native glade ground contact",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                Upload();material.SetTexture(FieldId,mask);material.SetFloat(StrengthId,0);
            }
            catch{Dispose();throw;}
        }
        // Also used by paired native geometry probes. Arrays are borrowed/read;
        // the pure helper owns all winding, exclusion and rectangle validation.
        internal static ReferenceGladeContactGeometry.Footprint[] ExtractFootprints(Mesh mesh)
        {
            if(mesh==null||!mesh.isReadable||mesh.vertexCount>ReferenceGladeContactGeometry.MaximumMeshVertices)
                throw new ArgumentException("Readable bounded contact source mesh required.");
            ulong indices=0;for(int i=0;i<mesh.subMeshCount;i++)indices+=mesh.GetIndexCount(i);
            if(indices>(ulong)ReferenceGladeContactGeometry.MaximumMeshVertices*6)
                throw new ArgumentException("Contact source index budget exceeded.");
            var vertices=mesh.vertices;var packed=new float[vertices.Length*3];
            for(int i=0;i<vertices.Length;i++){packed[i*3]=vertices[i].x;packed[i*3+1]=vertices[i].y;packed[i*3+2]=vertices[i].z;}
            return ReferenceGladeContactGeometry.Extract(packed,mesh.triangles);
        }
        internal void Refresh(Zone zone,IReadOnlyDictionary<Entity,SpawnRing3DRecipe> recipes,bool fullReveal)
        {
            using (PerformanceMarkers.Zone.ContactRefresh.Auto())
            {
            if(disposed)return;
            if(!SpreadPresentationScope.IsActive(zone)||recipes==null){Clear();return;}
            placed.Clear();ulong next=Offset;int count=0;
            foreach(var pair in recipes)
            {
                var recipe=pair.Value;
                if(!recipe.Batched||recipe.Transient||recipe.Failure!=null||!ReferenceEquals(pair.Key,recipe.Owner)
                    ||recipe.ModelId==null||!sources.TryGetValue(recipe.ModelId,out var shape)||shape.Bases.Length==0)continue;
                var cell=zone.GetEntityCell(pair.Key);
                if(cell==null||!cell.Objects.Contains(pair.Key)||pair.Key.GetPart<RenderPart>()?.Visible!=true||!fullReveal&&!cell.IsVisible)continue;
                if(++count>MaximumContributors||placed.Count+shape.Bases.Length>ReferenceGladeContactGeometry.MaximumFootprints){Clear();return;}
                // Exactly the same root yaw as actual ground batching. Validated
                // kit prefabs have no additional local scale/origin transform.
                int quarter=shape.Ground?SpawnRing3DGroundPatches.GroundQuarterTurns(cell.X,cell.Y):recipe.QuarterTurns;
                Mix(ref next,shape.Index);Mix(ref next,unchecked((uint)recipe.Position.x.GetHashCode()));
                Mix(ref next,unchecked((uint)recipe.Position.z.GetHashCode()));Mix(ref next,unchecked((uint)quarter));
                foreach(var footprint in shape.Bases)placed.Add(new ReferenceGladeContactGeometry.Placed(footprint,recipe.Position.x,recipe.Position.z,quarter));
            }
            if(hasSignature&&next==signature)return;
            using (PerformanceMarkers.Zone.ContactRasterize.Auto())
                valid=ReferenceGladeContactGeometry.Rasterize(placed,pixels);
            signature=next;hasSignature=true;Upload();
            if(!valid&&material!=null)material.SetFloat(StrengthId,0);
            }
        }
        internal void SetEnabled(bool enabled)
        {if(!disposed&&material!=null)material.SetFloat(StrengthId,enabled&&valid?MaximumAttenuation:0);}
        private void Clear()
        {
            placed.Clear();hasSignature=false;valid=false;Array.Clear(pixels,0,pixels.Length);Upload();
            if(material!=null)material.SetFloat(StrengthId,0);
        }
        private void Upload()
        {using (PerformanceMarkers.Zone.ContactUpload.Auto())
            {if(mask==null)return;for(int i=0;i<pixels.Length;i++)upload[i]=new Color32(pixels[i],0,0,255);mask.SetPixels32(upload);mask.Apply(false,false);}}
        private static void Mix(ref ulong hash,uint value){unchecked{hash=(hash^value)*Prime;}}
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            // Release our binding before the owning surface destroys its clone.
            // A foreign/borrowed material was rejected before any allocation.
            if(material!=null){material.SetFloat(StrengthId,0);material.SetTexture(FieldId,null);}material=null;
            if(mask!=null){if(Application.isPlaying)Object.Destroy(mask);else Object.DestroyImmediate(mask);}mask=null;
            sources.Clear();placed.Clear();valid=false;hasSignature=false;
        }
    }
}
