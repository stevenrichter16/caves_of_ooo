using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Rendering
{
    /// <summary>Owned 10x5-cell geometry patches. Native recipes and current
    /// permanent water are the only geometry inputs; FOV/light remain shader data.
    /// Prefab meshes/material assets are borrowed and never edited.</summary>
    internal sealed class SpawnRing3DGroundPatches : IDisposable
    {
        private const int PatchWidth = 10, PatchHeight = 5;
        private const int Columns = Zone.Width / PatchWidth, Rows = Zone.Height / PatchHeight;
        private const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;
        private readonly NativeZone3DRenderSurface surface;
        private readonly SpawnRing3DLibrary library;
        private readonly VoxelWorldPresentation voxel;
        private readonly SpreadNativeStyle3DLibrary nativeStyles;
        private readonly SpawnRing3DCatalog catalog;
        private readonly Patch[] patches = new Patch[Columns * Rows];
        private readonly bool[] marked = new bool[Columns * Rows];
        private readonly bool[] builtWater = new bool[Zone.Width * Zone.Height];
        private readonly Mesh[] waterMeshes = new Mesh[16];
        private readonly Dictionary<string, Model> models = new Dictionary<string, Model>(StringComparer.Ordinal);
        private readonly Dictionary<string, Model> styledModels = new Dictionary<string, Model>(StringComparer.Ordinal);
        private readonly List<CombineInstance> worldPieces = new List<CombineInstance>(256);
        private readonly List<CombineInstance> waterPieces = new List<CombineInstance>(128);
        private readonly Material worldMaterial, waterMaterial, pilotGroundMaterial, gladeMaterial, gladeSource;
        private readonly List<CombineInstance> gladePieces = new List<CombineInstance>(256);
        private readonly List<Vector3> pilotVertices = new List<Vector3>(200);
        private readonly List<Vector2> pilotUvs = new List<Vector2>(200);
        private readonly List<int> pilotTriangles = new List<int>(300);
        private Zone currentZone;
        private bool disposed;
        public int GroundBuildCount { get; private set; }
        public int PatchCount => disposed ? 0 : patches.Length;

        private sealed class Patch
        {
            public GameObject Root, Geometry;
            public Mesh WorldMesh, WaterMesh, PilotGroundMesh, GladeMesh;
            public MeshRenderer WorldRenderer,WaterRenderer,PilotGroundRenderer,GladeRenderer;
            public Dictionary<Entity,SpawnRing3DRecipe> Contributions;
            public HashSet<Entity> CultivatedOwners;
            public ulong Fingerprint;
            public int Revision;
            public bool Built;
        }
        private sealed class Model
        {
            public Matrix4x4 RootTransform;
            public Fragment[] Fragments;
            public bool Ground;
        }
        private readonly struct Fragment
        {
            public readonly Mesh Mesh;
            public readonly int Submesh;
            public readonly Matrix4x4 Relative;
            public readonly bool Water, Glade;
            public readonly Material SourceMaterial;
            public Fragment(Mesh mesh, int submesh, Matrix4x4 relative, bool water, bool glade, Material material)
            { Mesh = mesh; Submesh = submesh; Relative = relative; Water = water; Glade = glade; SourceMaterial = material; }
        }

        public SpawnRing3DGroundPatches(NativeZone3DRenderSurface surface, SpawnRing3DLibrary library, MultiCellPilot3DLibrary pilot = null, VoxelWorldPresentation voxel = null, ReferenceGladeVoxelLibrary glade = null,SpreadNativeStyle3DLibrary nativeStyles = null)
        {
            if (surface == null || surface.ContentRoot == null || library == null)
                throw new ArgumentException("A live native surface and ring library are required.");
            this.surface = surface; this.library = library; catalog = library.Definition;
            this.voxel = voxel;this.nativeStyles=nativeStyles;
            // Validate borrowed material families before allocating owned geometry.
            worldMaterial = surface.MaterialFor(library.WorldMaterial);
            waterMaterial = surface.MaterialFor(library.WaterMaterial);
            if (pilot != null) pilotGroundMaterial = surface.MaterialFor(pilot.GroundMaterial);
            if (glade != null) {glade.Validate();gladeSource=glade.Material;gladeMaterial=surface.MaterialFor(gladeSource);}
            try
            {
                for (int i = 0; i < waterMeshes.Length; i++) waterMeshes[i] = SpawnRing3DWaterMesh.Create(i);
                for (int i = 0; i < patches.Length; i++)
                {
                    var root = Child("Ring ground patch " + (i % Columns) + "," + (i / Columns), surface.ContentRoot);
                    patches[i] = new Patch { Root = root }; marked[i] = true;
                }
            }
            catch { Dispose(); throw; }
        }

        // A batch records each exact owner only after its geometry commits.
        // A neighboring submitted renderer alone cannot satisfy this query.
        internal bool TryGetApprovedStyle(Entity owner,SpawnRing3DRecipe recipe,SpreadBiomeStyleCatalog.Contract expected,
            MaterialPropertyBlock properties,List<Material> materials,out SpreadBiomeStyleEvidence evidence)
        {
            evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"uncommitted-batch-owner",true,expected.Mesh,expected.Material);
            var cell=currentZone?.GetEntityCell(owner);if(disposed||cell==null||!cell.Objects.Contains(owner))return false;
            var cache=nativeStyles?.ForOwner(currentZone,recipe)!=null?styledModels:models;
            var patch=patches[Index(cell.X,cell.Y)];
            if(patch==null||!patch.Built||patch.Contributions==null||!patch.Contributions.TryGetValue(owner,out var committed)
                ||!ReferenceEquals(committed.Owner,owner)||committed.ModelId!=recipe.ModelId||committed.Position!=recipe.Position||committed.QuarterTurns!=recipe.QuarterTurns
                ||!cache.TryGetValue(recipe.ModelId,out var model)||model.Fragments.Length!=expected.Materials.Length)return false;
            SpreadBiomeStylePieceEvidence[] pieces=expected.Materials.Length>1?new SpreadBiomeStylePieceEvidence[expected.Materials.Length]:null;Mesh firstMesh=null;Material firstMaterial=null;
            for(int slot=0;slot<model.Fragments.Length;slot++){
                var fragment=model.Fragments[slot];var borrowed=expected.Materials[slot];
                if(fragment.Mesh!=expected.Mesh||fragment.Submesh!=slot||fragment.SourceMaterial!=borrowed)
                {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"committed-source-mismatch",true,expected.Mesh,borrowed);return false;}
                var submitted=fragment.Glade?patch.GladeMesh:fragment.Water?patch.WaterMesh:patch.WorldMesh;
                var renderer=fragment.Glade?patch.GladeRenderer:fragment.Water?patch.WaterRenderer:patch.WorldRenderer;
                if(submitted==null||submitted.vertexCount==0||submitted.subMeshCount!=1||submitted.GetIndexCount(0)==0||renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.forceRenderingOff||renderer.GetComponent<MeshFilter>()?.sharedMesh!=submitted)
                {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-mesh-mismatch",true,expected.Mesh,borrowed,submitted);return false;}
                if(!SpreadBiomeStyleCatalog.PaletteMatches(renderer,surface.MaterialFor(borrowed),borrowed,properties,materials,out var material))
                {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-palette-mismatch",true,expected.Mesh,borrowed,submitted,material);return false;}
                if(slot==0){firstMesh=submitted;firstMaterial=material;}if(pieces!=null)pieces[slot]=new SpreadBiomeStylePieceEvidence(expected.Mesh,borrowed,submitted,material,slot,0);
            }
            evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,null,true,expected.Mesh,expected.Material,firstMesh,firstMaterial,pieces);return true;
        }

        internal bool HasCultivatedSoil(Entity owner)
        {var cell=currentZone?.GetEntityCell(owner);return !disposed&&cell!=null&&patches[Index(cell.X,cell.Y)]?.CultivatedOwners?.Contains(owner)==true;}
        public GameObject RootFor(int x, int y) => !disposed && InBounds(x,y) ? patches[Index(x,y)]?.Root : null;
        public int Revision(int x, int y) => !disposed && InBounds(x,y) ? patches[Index(x,y)]?.Revision ?? 0 : 0;
        /// <summary>Only successful committed patch geometry is reported here.
        /// The presenter additionally gates this by visibility/presentation state.</summary>
        public bool HasWater(int x, int y) => !disposed && InBounds(x,y) && builtWater[y * Zone.Width + x];

        /// <summary>Include cardinal neighbours because water corner/rim geometry
        /// depends on those cells, including across a patch boundary.</summary>
        public void Mark(int x, int y)
        {
            if (disposed || !InBounds(x,y)) return;
            MarkOne(x,y); MarkOne(x-1,y); MarkOne(x+1,y); MarkOne(x,y-1); MarkOne(x,y+1);
        }
        private void MarkOne(int x,int y) { if (InBounds(x,y)) marked[Index(x,y)] = true; }

        /// <summary>Null scans all patch fingerprints; a supplied set scans only
        /// marked patches. Existing reconcile marks survive an empty supplied set.
        /// Unchanged geometry keeps roots, meshes and revisions even after a full
        /// FOV/light refresh. No LINQ or per-cell temporary collections.</summary>
        public void Refresh(Zone zone, Dictionary<Entity, SpawnRing3DRecipe> recipes, HashSet<int> dirtyCells = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(SpawnRing3DGroundPatches));
            if (zone == null || recipes == null || !catalog.SupportsZone(zone) || surface.ContentRoot == null)
                throw new ArgumentException("Current supported native zone, recipes and live surface are required.");
            if (!ReferenceEquals(currentZone, zone) || dirtyCells == null)
                for (int i = 0; i < marked.Length; i++) marked[i] = true;
            if (dirtyCells != null)
                foreach (int key in dirtyCells) if (key >= 0 && key < Zone.Width * Zone.Height) Mark(key % Zone.Width, key / Zone.Width);
            currentZone = zone;
            string fallback = FallbackModel(zone);
            for (int i = 0; i < patches.Length; i++)
            {
                if (!marked[i]) continue;
                var patch = patches[i];
                if (patch.Root == null) throw new InvalidOperationException("Owned ring patch was destroyed before refresh.");
                ulong fingerprint = Fingerprint(zone, recipes, i, fallback);
                if (!patch.Built || patch.Fingerprint != fingerprint)
                    Rebuild(zone, recipes, i, fallback, fingerprint);
                marked[i] = false;
            }
        }

        private ulong Fingerprint(Zone zone, Dictionary<Entity, SpawnRing3DRecipe> recipes, int index, string fallback)
        {
            ulong hash = Offset; BoundsFor(index, out int startX, out int startY);
            for (int y = startY; y < startY + PatchHeight; y++) for (int x = startX; x < startX + PatchWidth; x++)
            {
                Mix(ref hash, (uint)(y * Zone.Width + x)); bool ground = false; int contributions = 0;
                var objects = zone.GetCell(x,y).Objects;
                for (int n = 0; n < objects.Count; n++)
                {
                    Entity owner = objects[n];
                    if (!recipes.TryGetValue(owner, out var recipe) || !recipe.Batched || recipe.Transient) continue;
                    ValidateRecipe(owner, recipe);
                    var spec = catalog.FindModel(recipe.ModelId);
                    if (spec == null || spec.kind == "actor" || spec.kind == "felling-component" || spec.kind == "tile-overlay")
                        throw new InvalidOperationException("Unsupported batched ring model: " + recipe.ModelId);
                    ground |= spec.kind == "ground"; contributions++;
                    Mix(ref hash, recipe.ModelId); Mix(ref hash, recipe.Position);
                    Mix(ref hash, (uint)recipe.QuarterTurns);
                    Mix(ref hash, IsPilotGround(owner) ? 1u : 0u);
                    Mix(ref hash,nativeStyles?.ForOwner(zone,recipe)!=null?1u:0u);
                    Mix(ref hash,RepairCultivationRecipes.HasCultivatedSoil(zone,owner)?1u:0u);
                    Mix(ref hash,ConnectedSpread3DLibrary.ResolveReserveBed(zone,owner)??"");
                }
                Mix(ref hash, (uint)contributions);
                if (!ground && fallback != null) { Mix(ref hash, fallback); Mix(ref hash, Village3DProjection.CellCentre(x,y)); }
                bool water = SpawnRing3DRecipes.HasPermanentWater(zone,x,y);
                Mix(ref hash, water ? (uint)(DryMask(zone,x,y) + 1) : 0u);
            }
            return hash;
        }

        private void Rebuild(Zone zone, Dictionary<Entity, SpawnRing3DRecipe> recipes, int index, string fallback, ulong fingerprint)
        {
            Patch patch = patches[index]; worldPieces.Clear(); waterPieces.Clear(); gladePieces.Clear(); pilotVertices.Clear(); pilotUvs.Clear(); pilotTriangles.Clear();
            BoundsFor(index, out int startX, out int startY);
            Matrix4x4 toLocal = patch.Root.transform.worldToLocalMatrix;
            var contributions=new Dictionary<Entity,SpawnRing3DRecipe>();var cultivated=new HashSet<Entity>();
            // Stage all fragments before touching the currently published patch.
            for (int y = startY; y < startY + PatchHeight; y++) for (int x = startX; x < startX + PatchWidth; x++)
            {
                bool ground = false; var objects = zone.GetCell(x,y).Objects;
                for (int n = 0; n < objects.Count; n++)
                {
                    Entity owner = objects[n];
                    if (!recipes.TryGetValue(owner, out var recipe) || !recipe.Batched || recipe.Transient) continue;
                    ValidateRecipe(owner, recipe); Model model = GetModel(recipe.ModelId,recipe); ground |= model.Ground;
                    contributions.Add(owner,recipe);
                    if (IsPilotGround(owner)) AppendPilotGround(x, y, toLocal); else Append(model, recipe.Position, x, y, toLocal,recipe.QuarterTurns);
                    // Add sparse furrows above the same underlying ground mesh.
                    // No native owner or terrain model is replaced or fabricated.
                    if(RepairCultivationRecipes.HasCultivatedSoil(zone,owner))
                    {Append(GetModel(ConnectedSpread3DLibrary.ResolveReserveBed(zone,owner)??RepairCultivationRecipes.SoilModel),recipe.Position,x,y,toLocal);cultivated.Add(owner);}
                }
                if (!ground && fallback != null) Append(GetModel(fallback), Village3DProjection.CellCentre(x,y), x, y, toLocal);
                if (SpawnRing3DRecipes.HasPermanentWater(zone,x,y))
                    waterPieces.Add(new CombineInstance { mesh = waterMeshes[DryMask(zone,x,y)], subMeshIndex = 0,
                        transform = toLocal * Matrix4x4.Translate(Village3DProjection.CellCentre(x,y,catalog.tileState.height)) });
            }
            GameObject geometry = null; Mesh newWorld = null, newWater = null, newPilotGround = null, newGlade = null;
            MeshRenderer newWorldRenderer=null,newWaterRenderer=null,newPilotRenderer=null,newGladeRenderer=null;
            try
            {
                geometry = Child("Patch geometry", patch.Root.transform); geometry.SetActive(false);
                newWorld = Combine(worldPieces, "Ring patch palette");
                newWater = Combine(waterPieces, "Ring patch water");
                newPilotGround = BuildPilotGround();
                newGlade = Combine(gladePieces,"Reference glade patch palette");
                if (newWorld != null) newWorldRenderer=AddRenderer(geometry.transform, newWorld, worldMaterial);
                if (newWater != null) newWaterRenderer=AddRenderer(geometry.transform, newWater, waterMaterial);
                if (newPilotGround != null) newPilotRenderer=AddRenderer(geometry.transform, newPilotGround, pilotGroundMaterial);
                if (newGlade != null) newGladeRenderer=AddRenderer(geometry.transform,newGlade,gladeMaterial);
                surface.PrepareModel(geometry, transient:false);
                if (patch.Geometry != null) patch.Geometry.SetActive(false);
                DestroyOwned(patch.Geometry); DestroyOwned(patch.WorldMesh); DestroyOwned(patch.WaterMesh); DestroyOwned(patch.PilotGroundMesh); DestroyOwned(patch.GladeMesh);
                patch.Geometry = geometry; patch.WorldMesh = newWorld; patch.WaterMesh = newWater; patch.PilotGroundMesh = newPilotGround; patch.GladeMesh = newGlade;
                patch.WorldRenderer=newWorldRenderer;patch.WaterRenderer=newWaterRenderer;patch.PilotGroundRenderer=newPilotRenderer;patch.GladeRenderer=newGladeRenderer;patch.Contributions=contributions;patch.CultivatedOwners=cultivated;
                geometry = null; newWorld = newWater = newPilotGround = newGlade = null;
                patch.Geometry.SetActive(true); patch.Fingerprint = fingerprint; patch.Built = true; patch.Revision++; GroundBuildCount++;
                for (int y = startY; y < startY + PatchHeight; y++) for (int x = startX; x < startX + PatchWidth; x++)
                    builtWater[y * Zone.Width + x] = SpawnRing3DRecipes.HasPermanentWater(zone,x,y);
            }
            catch { DestroyOwned(geometry); DestroyOwned(newWorld); DestroyOwned(newWater); DestroyOwned(newPilotGround); DestroyOwned(newGlade); throw; }
            finally { worldPieces.Clear(); waterPieces.Clear(); gladePieces.Clear(); pilotVertices.Clear(); pilotUvs.Clear(); pilotTriangles.Clear(); }
        }

        private bool IsPilotGround(Entity owner)
            => pilotGroundMaterial != null && owner.BlueprintName == "TepuiStone" && MultiCellPilotRuntime.IsActive(currentZone);
        private void AppendPilotGround(int x, int y, Matrix4x4 toLocal)
        {
            int start = pilotVertices.Count; float south = Zone.Height - 1 - y;
            AddGroundVertex(new Vector3(x,0,south),toLocal); AddGroundVertex(new Vector3(x,0,south+1),toLocal);
            AddGroundVertex(new Vector3(x+1,0,south+1),toLocal); AddGroundVertex(new Vector3(x+1,0,south),toLocal);
            pilotTriangles.Add(start); pilotTriangles.Add(start+1); pilotTriangles.Add(start+2);
            pilotTriangles.Add(start); pilotTriangles.Add(start+2); pilotTriangles.Add(start+3);
        }
        private void AddGroundVertex(Vector3 world, Matrix4x4 toLocal)
        { pilotVertices.Add(toLocal.MultiplyPoint3x4(world)); pilotUvs.Add(new Vector2(world.x/Zone.Width,world.z/Zone.Height)); }
        private Mesh BuildPilotGround()
        {
            if (pilotVertices.Count == 0) return null;
            var mesh = new Mesh { name = "Pilot world-grain ground", hideFlags = HideFlags.DontSave };
            try { mesh.SetVertices(pilotVertices); mesh.SetUVs(0,pilotUvs); mesh.SetTriangles(pilotTriangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh; }
            catch { DestroyOwned(mesh); throw; }
        }
        private void Append(Model model, Vector3 position, int x, int y, Matrix4x4 toLocal, int quarterTurns=0)
        {
            Quaternion rotation = Quaternion.Euler(0, model.Ground ? GroundQuarterTurns(x,y)*90 : quarterTurns*90, 0);
            Matrix4x4 placement = toLocal * Matrix4x4.TRS(position, rotation, Vector3.one) * model.RootTransform;
            for (int i = 0; i < model.Fragments.Length; i++)
            {
                var fragment = model.Fragments[i];
                var combine = new CombineInstance { mesh = fragment.Mesh, subMeshIndex = fragment.Submesh, transform = placement * fragment.Relative };
                (fragment.Glade ? gladePieces : fragment.Water ? waterPieces : worldPieces).Add(combine);
            }
        }
        private Model GetModel(string id,SpawnRing3DRecipe? recipe=null)
        {
            var entry=recipe.HasValue?nativeStyles?.ForOwner(currentZone,recipe.Value):null;
            var cache=entry!=null?styledModels:models;
            if (cache.TryGetValue(id, out var cached)) return cached;
            var spec = catalog.FindModel(id); var prefab = entry?.Prefab ?? library.FindModel(id);
            if (spec == null || prefab == null || (spec.kind != "ground" && spec.kind != "entity"))
                throw new InvalidOperationException("Unavailable static ring model: " + id);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var fragments = new List<Fragment>();
            foreach (var renderer in renderers)
            {
                if (!(renderer is MeshRenderer)) throw new InvalidOperationException("A batched model contains a nonstatic renderer: " + id);
                var filter = renderer.GetComponent<MeshFilter>(); Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (voxel != null) mesh = voxel.Resolve(mesh);
                if (mesh == null || !mesh.isReadable || mesh.vertexCount == 0 || mesh.subMeshCount == 0)
                    throw new InvalidOperationException("Batched ring model needs a readable nonempty static mesh: " + id);
                var materials = renderer.sharedMaterials;
                if (materials.Length != mesh.subMeshCount) throw new InvalidOperationException("Ambiguous ring material/submesh slots: " + id);
                Matrix4x4 relative = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int i = 0; i < materials.Length; i++)
                {
                    bool water = materials[i] == library.WaterMaterial;
                    bool glade = gladeSource != null && materials[i] == gladeSource;
                    if (!water && !glade && materials[i] != library.WorldMaterial)
                        throw new InvalidOperationException("Unregistered static ring material family: " + id);
                    if (mesh.GetTopology(i) != MeshTopology.Triangles || mesh.GetIndexCount(i) == 0)
                        throw new InvalidOperationException("Batched ring mesh has an empty or nontriangle submesh: " + id);
                    if (renderer.enabled && ActiveUnder(renderer.transform, prefab.transform)) fragments.Add(new Fragment(mesh, i, relative, water, glade, materials[i]));
                }
            }
            if (fragments.Count == 0) throw new InvalidOperationException("Batched ring model has no active drawable fragments: " + id);
            cached = new Model { Ground = spec.kind == "ground", Fragments = fragments.ToArray(),
                RootTransform = Matrix4x4.TRS(Vector3.zero, prefab.transform.localRotation, prefab.transform.localScale) };
            cache.Add(id, cached); return cached;
        }
        private static bool ActiveUnder(Transform child, Transform root)
        {
            for (var current = child; current != null; current = current.parent)
            { if (!current.gameObject.activeSelf) return false; if (current == root) return true; }
            return false;
        }
        private static Mesh Combine(List<CombineInstance> pieces, string name)
        {
            if (pieces.Count == 0) return null;
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
            try
            {
                // Unity copies/transforms the supplied vertices and their normal,
                // UV, tangent and color channels; it never edits source meshes.
                mesh.CombineMeshes(pieces.ToArray(), mergeSubMeshes:true, useMatrices:true, hasLightmapData:false);
                if (mesh.vertexCount == 0) throw new InvalidOperationException("Combined ring patch is empty.");
                mesh.RecalculateBounds(); return mesh;
            }
            catch { DestroyOwned(mesh); throw; }
        }
        private static MeshRenderer AddRenderer(Transform parent, Mesh mesh, Material material)
        {
            var go = Child(mesh.name, parent); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial = material;return renderer;
        }
        private string FallbackModel(Zone zone)
        {
            // These native compositions explicitly own every real floor cell.
            // If native ground is removed, fallback grass would fabricate a
            // surface. Ginmere's normal mouth retains its native stone landing.
            if(ReferenceGladePlan.IsActive(zone)||OverwritCompositionPlan.IsWildernessZone(zone.ZoneID)||GinmereCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||CathedralCompositionPlan.IsSupportedZone(zone.ZoneID)||StillleafCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||OlderdeepCompositionPlan.IsSupportedZone(zone.ZoneID)||WellmeetCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||CinderholdCompositionPlan.IsSupportedZone(zone.ZoneID)||SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||DrownedLedgerCompositionPlan.IsSupportedZone(zone.ZoneID)||MarrowstyeCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||FirstTentCompositionPlan.IsSupportedZone(zone.ZoneID)||LastCounterCompositionPlan.IsSupportedZone(zone.ZoneID)
                ||GantryCompositionPlan.IsSupportedZone(zone.ZoneID)||TineCompositionPlan.IsSupportedZone(zone.ZoneID)||QuillholdCompositionPlan.IsSupportedZone(zone.ZoneID)||TallyCompositionPlan.IsSupportedZone(zone.ZoneID))return null;
            if(SpreadPresentationScope.IsActive(zone))return ReferenceGladeVoxelLibrary.ModelId("ground",0);
            bool stone = MultiCellPilotRuntime.IsActive(zone) || zone.ZoneID == "Overworld.2.5.0" || zone.ZoneID == "Overworld.3.5.0" || zone.ZoneID == "Overworld.4.5.0";
            var binding = catalog.FindBlueprint(stone ? "TepuiStone" : "Grass");
            if (binding?.models == null || binding.models.Length == 0) throw new InvalidOperationException("Clean ring ground fallback is unavailable.");
            return binding.models[0];
        }
        private static void ValidateRecipe(Entity owner, SpawnRing3DRecipe recipe)
        {
            if (!ReferenceEquals(owner, recipe.Owner) || string.IsNullOrEmpty(recipe.ModelId) || recipe.Failure != null
                || !Village3DProjection.Finite(recipe.Position.x) || !Village3DProjection.Finite(recipe.Position.y) || !Village3DProjection.Finite(recipe.Position.z))
                throw new InvalidOperationException("Invalid current native batching recipe.");
        }
        private static int DryMask(Zone zone, int x, int y)
        {
            int mask = 0;
            if (!SpawnRing3DRecipes.HasPermanentWater(zone,x,y-1)) mask |= 1;
            if (!SpawnRing3DRecipes.HasPermanentWater(zone,x+1,y)) mask |= 2;
            if (!SpawnRing3DRecipes.HasPermanentWater(zone,x,y+1)) mask |= 4;
            if (!SpawnRing3DRecipes.HasPermanentWater(zone,x-1,y)) mask |= 8;
            return mask;
        }
        internal static int GroundQuarterTurns(int x, int y)
        {
            unchecked { uint hash = (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu; hash ^= hash >> 16; hash *= 0x7feb352du; hash ^= hash >> 15; return (int)(hash & 3); }
        }
        private static void Mix(ref ulong hash, string text)
        { unchecked { for (int i = 0; i < text.Length; i++) hash = (hash ^ text[i]) * Prime; hash = (hash ^ 0xffff) * Prime; } }
        private static void Mix(ref ulong hash, uint value) { unchecked { hash = (hash ^ value) * Prime; } }
        private static void Mix(ref ulong hash, Vector3 value)
        { Mix(ref hash, unchecked((uint)value.x.GetHashCode())); Mix(ref hash, unchecked((uint)value.y.GetHashCode())); Mix(ref hash, unchecked((uint)value.z.GetHashCode())); }
        private static bool InBounds(int x,int y) => x >= 0 && x < Zone.Width && y >= 0 && y < Zone.Height;
        private static int Index(int x,int y) => y / PatchHeight * Columns + x / PatchWidth;
        private static void BoundsFor(int index,out int x,out int y) { x = index % Columns * PatchWidth; y = index / Columns * PatchHeight; }
        private static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name) { layer = NativeZone3DRenderSurface.WorldLayer, hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false); return go;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            for (int i = 0; i < patches.Length; i++)
            {
                var patch = patches[i]; if (patch == null) continue;
                DestroyOwned(patch.WorldMesh); DestroyOwned(patch.WaterMesh); DestroyOwned(patch.PilotGroundMesh); DestroyOwned(patch.GladeMesh); DestroyOwned(patch.Root); patches[i] = null;
            }
            for (int i = 0; i < waterMeshes.Length; i++) { DestroyOwned(waterMeshes[i]); waterMeshes[i] = null; }
            models.Clear(); styledModels.Clear(); worldPieces.Clear(); waterPieces.Clear(); gladePieces.Clear(); pilotVertices.Clear(); pilotUvs.Clear(); pilotTriangles.Clear(); currentZone = null;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
