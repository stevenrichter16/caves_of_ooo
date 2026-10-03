using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Original compact voxel source for regional gear. Builds assets only;
    /// no scene, native item or gameplay state is created or changed.</summary>
    public static class EquipmentDiscoveryKitBuilder
    {
        static readonly List<Vector3> vertices = new List<Vector3>(2048);
        static readonly List<Vector2> uvs = new List<Vector2>(2048);
        static readonly List<int> triangles = new List<int>(3072);
        static readonly Vector3[] corners = { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) };
        static readonly int[] faces = { 0,3,2,1, 5,6,7,4, 4,7,3,0, 1,2,6,5, 3,7,6,2, 4,0,1,5 };
        [MenuItem("Caves of Ooo/Art/Build Equipment Discovery Kit")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Build the kit while the editor is idle.");
            var material = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath)?.WorldMaterial;
            if (material == null) throw new InvalidOperationException("Native ring palette required.");
            Directory.CreateDirectory(EquipmentDiscoveryArtLibrary.Folder); AssetDatabase.Refresh();
            var entries = new List<EquipmentDiscoveryArtLibrary.Entry>();
            foreach (string id in EquipmentDiscoveryArtLibrary.ModelIds) {
                vertices.Clear(); uvs.Clear(); triangles.Clear(); Build(id);
                string path = EquipmentDiscoveryArtLibrary.Folder + "/" + id;
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + ".asset");
                if (mesh == null) { mesh = new Mesh { name = id }; AssetDatabase.CreateAsset(mesh, path + ".asset"); } else mesh.Clear();
                mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                if (EquipmentDiscoveryArtLibrary.IsWorn(id)) {
                    var weights = new BoneWeight[vertices.Count]; for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
                    mesh.boneWeights = weights; mesh.bindposes = new[] { Matrix4x4.identity };
                }
                EditorUtility.SetDirty(mesh); var root = new GameObject(id); GameObject prefab;
                try { root.AddComponent<MeshFilter>().sharedMesh = mesh; root.AddComponent<MeshRenderer>().sharedMaterial = material; prefab = PrefabUtility.SaveAsPrefabAsset(root, path + ".prefab"); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                entries.Add(new EquipmentDiscoveryArtLibrary.Entry {
                    Id = id, Slot = EquipmentDiscoveryArtLibrary.IsWorn(id) ? "Body" : "", Prefab = prefab, Mesh = mesh,
                    Spec = new SpawnRing3DCatalog.Model { id = id, sourceBlueprint = EquipmentDiscoveryArtLibrary.Blueprint(id), path = path + ".prefab", kind = "entity", materialFamily = "ring-palette", rigFamily = "none", boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size, triangles = triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>() }
                });
            }
            string libraryPath = EquipmentDiscoveryArtLibrary.Folder + "/Library.asset";
            var library = AssetDatabase.LoadAssetAtPath<EquipmentDiscoveryArtLibrary>(libraryPath);
            if (library == null) { library = ScriptableObject.CreateInstance<EquipmentDiscoveryArtLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
            library.Material = material; library.Entries = entries.ToArray(); library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
            Debug.Log("[EquipmentDiscoveryArt] Built 18 original component, assembled, carried and fitted forms.");
        }
        static void Build(string id)
        {
            string form = id.Substring(EquipmentDiscoveryArtLibrary.Prefix.Length);
            if (form == "groundwire-screen") { Screen(); return; }
            if (form == "kilnfelt-apron") { Apron(false); return; }
            if (form == "worn-kilnfelt-apron") { Apron(true); return; }
            if (form.StartsWith("head-", StringComparison.Ordinal)) { Head(form.Substring(5), 0, true); return; }
            string[] parts = form.Split('-');
            if (parts.Length != 4 || parts[0] != "forged") throw new ArgumentException("Unknown regional equipment form: " + id);
            bool willow = parts[2] == "willow", serrated = parts[3] == "serrated";
            float grip = willow ? -.23f : -.20f;
            Box(0,.075f,grip,.065f,.09f,willow ? .45f : .39f,willow ? 10 : 8);
            Box(0,.075f,-.37f,.087f,.11f,.055f,12);
            for (int i=0;i<3;i++) Box(0,.086f,grip-.07f+i*.058f,.084f,.114f,.024f,serrated ? 12 : 19);
            Box(0,.075f,.005f,.18f,.105f,.045f,12);
            Head(parts[1], parts[1] == "counterweight" ? .19f : .23f, false);
            if (serrated) for(int i=0;i<3;i++) Box(.061f,.085f,-.13f+i*.067f,.055f,.105f,.032f,13);
            else Box(-.073f,.071f,-.072f,.041f,.09f,.12f,19);
        }
        static void Head(string form, float z, bool component)
        {
            if (form == "peatmallet") {
                Box(0,.15f,z,.38f,.25f,.21f,8); Box(-.18f,.15f,z,.055f,.29f,.24f,10); Box(.18f,.15f,z,.055f,.29f,.24f,10);
                Box(-.078f,.15f,z,.038f,.272f,.228f,12); Box(.078f,.15f,z,.038f,.272f,.228f,12); Box(0,.291f,z,.075f,.025f,.07f,13);
                if(component) Box(0,.07f,z-.15f,.09f,.12f,.10f,8);
            } else if (form == "cinderhook") {
                // A stepped extraction hook and broad cutting toe, unlike a symmetric battleaxe.
                Box(0,.082f,z,.10f,.145f,.24f,12); Box(.13f,.079f,z+.016f,.24f,.13f,.25f,12);
                Box(.268f,.071f,z-.032f,.06f,.10f,.17f,13); Box(.17f,.072f,z-.14f,.21f,.105f,.075f,13);
                Box(-.095f,.08f,z+.026f,.105f,.11f,.065f,8); Box(.115f,.153f,z+.012f,.045f,.025f,.13f,20);
                if(component) Box(0,.077f,z-.165f,.076f,.12f,.09f,12);
            } else if (form == "counterweight") {
                float length = component ? .68f : .45f;
                Box(0,.075f,z,.10f,.11f,length,12); Box(-.046f,.087f,z,.025f,.095f,length-.04f,13);
                Box(0,.08f,z+length*.5f,.064f,.095f,.055f,13);
                Box(0,.079f,z-length*.5f-.025f,.23f,.12f,.045f,12);
                Box(.11f,.075f,z-length*.5f-.025f,.074f,.13f,.075f,29);
                if(component) Box(0,.07f,z-length*.5f-.084f,.053f,.095f,.076f,12);
                else { Box(0,.079f,-.39f,.14f,.125f,.065f,29); Box(0,.148f,-.39f,.055f,.035f,.043f,13); }
            } else throw new ArgumentException("Unknown equipment head: " + form);
        }
        static void Screen()
        {
            // Open copper lattice with a wood rim and separate insulated rear grip.
            Box(-.29f,.12f,0,.08f,.19f,.72f,8); Box(.29f,.12f,0,.08f,.19f,.72f,8);
            Box(0,.12f,-.34f,.58f,.19f,.075f,8); Box(0,.12f,.34f,.58f,.19f,.075f,8);
            for(int i=0;i<5;i++) Box(-.21f+i*.105f,.165f,0,.025f,.035f,.62f,29);
            for(int i=0;i<6;i++) Box(0,.184f,-.26f+i*.104f,.50f,.03f,.025f,51);
            Box(-.30f,.12f,-.22f,.035f,.22f,.045f,13); Box(.30f,.12f,.22f,.035f,.22f,.045f,13);
            Box(0,.042f,0,.26f,.07f,.075f,19); Box(-.10f,.06f,0,.048f,.12f,.075f,19); Box(.10f,.06f,0,.048f,.12f,.075f,19);
        }
        static void Apron(bool worn)
        {
            if (!worn) {
                Box(0,.076f,0,.53f,.14f,.57f,20); Box(0,.158f,-.04f,.47f,.045f,.47f,13);
                Box(0,.190f,.045f,.40f,.035f,.22f,20); Box(-.18f,.214f,.045f,.036f,.016f,.25f,10); Box(.18f,.214f,.045f,.036f,.016f,.25f,10);
                Box(0,.222f,-.14f,.11f,.027f,.052f,12); Box(-.22f,.083f,.325f,.047f,.08f,.10f,10); Box(.22f,.083f,.325f,.047f,.08f,.10f,10);
            } else {
                // Front is negative Z, matching the existing original humanoid rig.
                Box(0,-.105f,-.185f,.47f,.72f,.055f,20); Box(0,.12f,-.213f,.36f,.27f,.031f,13);
                Box(0,-.24f,-.224f,.45f,.23f,.03f,13); Box(0,-.40f,-.21f,.49f,.07f,.038f,20);
                Box(-.155f,.28f,-.015f,.055f,.045f,.40f,10); Box(.155f,.28f,-.015f,.055f,.045f,.40f,10);
                Box(0,-.075f,.176f,.48f,.058f,.034f,10); Box(-.23f,-.075f,0,.035f,.058f,.36f,10); Box(.23f,-.075f,0,.035f,.058f,.36f,10);
                Box(.09f,-.085f,-.23f,.09f,.068f,.025f,12); Box(-.15f,-.24f,-.244f,.028f,.23f,.016f,10); Box(.15f,-.24f,-.244f,.028f,.23f,.016f,10);
            }
        }
        static void Box(float x,float y,float z,float w,float h,float d,int paint)
        {
            var center = new Vector3(x,y,z); var scale = new Vector3(w,h,d); var uv = new Vector2((paint%16+.5f)/16f,(paint/16+.5f)/8f);
            for(int face=0;face<6;face++) {
                int start=vertices.Count; for(int v=0;v<4;v++) {vertices.Add(center+Vector3.Scale(corners[faces[face*4+v]],scale));uvs.Add(uv);}
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
            }
        }
    }
}
