#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class BuildingBlockImportedArtTests
    {
        const string Root="Assets/Art3D/BuildingBlocks";
        static string[] Prefabs()=>Directory.Exists(Root+"/Prefabs")?Directory.GetFiles(Root+"/Prefabs","*.prefab"):Array.Empty<string>();
        [Test] public void CompleteKit_Has72DistinctVolumePrefabs()
        {
            var paths=Prefabs();Assert.That(paths.Length,Is.EqualTo(72));
            foreach(var path in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null,path);
                var meshes=prefab.GetComponentsInChildren<MeshFilter>(true);Assert.That(meshes.Length,Is.GreaterThan(0),path);
                Assert.That(meshes.Sum(m=>m.sharedMesh.triangles.Length),Is.GreaterThan(30),path);
                Assert.That(prefab.transform.localScale,Is.EqualTo(Vector3.one),path);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty,"Native occupancy owns collision: "+path);
                Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty,path);
            }
        }
        [Test] public void EveryFamily_HasFourVariants_AndNoDuplicateGuids()
        {
            var paths=Prefabs();Assert.That(paths.Length,Is.EqualTo(72));
            foreach(var family in paths.GroupBy(p=>Path.GetFileNameWithoutExtension(p).Substring(0,Path.GetFileNameWithoutExtension(p).LastIndexOf('-'))))
                Assert.That(family.Select(p=>Path.GetFileNameWithoutExtension(p).Split('-').Last()).OrderBy(x=>x),Is.EqualTo(new[]{"0","1","2","3"}),family.Key);
            Assert.That(paths.Select(AssetDatabase.AssetPathToGUID).Distinct().Count(),Is.EqualTo(paths.Length));
        }
        [Test] public void ImportedVolume_StaysInsideItsCell_AboveGround()
        {
            var paths=Prefabs();Assert.That(paths.Length,Is.EqualTo(72));
            foreach(var path in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var toRoot=prefab.transform.worldToLocalMatrix;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var p=toRoot*filter.transform.localToWorldMatrix*new Vector4(vertex.x,vertex.y,vertex.z,1);
                    Assert.That(Mathf.Abs(p.x),Is.LessThanOrEqualTo(.5001f),path);
                    Assert.That(Mathf.Abs(p.z),Is.LessThanOrEqualTo(.5001f),path);
                    Assert.That(p.y,Is.GreaterThanOrEqualTo(-.0001f),path);
                }
            }
        }
        [Test] public void Variants_DifferInGeometryOrPaint_NotJustFilename()
        {
            var paths=Prefabs();Assert.That(paths.Length,Is.EqualTo(72));
            var repeated=new System.Collections.Generic.List<string>();
            foreach(var family in paths.GroupBy(p=>Path.GetFileNameWithoutExtension(p).Substring(0,Path.GetFileNameWithoutExtension(p).LastIndexOf('-'))))
            {
                var fingerprints=new System.Collections.Generic.HashSet<string>();
                foreach(var path in family)
                {
                    var bytes=new System.Collections.Generic.List<byte>();
                    foreach(var filter in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true))
                    {
                        foreach(var v in filter.sharedMesh.vertices){bytes.AddRange(BitConverter.GetBytes(v.x));bytes.AddRange(BitConverter.GetBytes(v.y));bytes.AddRange(BitConverter.GetBytes(v.z));}
                        foreach(var uv in filter.sharedMesh.uv){bytes.AddRange(BitConverter.GetBytes(uv.x));bytes.AddRange(BitConverter.GetBytes(uv.y));}
                    }
                    using(var sha=System.Security.Cryptography.SHA256.Create())fingerprints.Add(Convert.ToBase64String(sha.ComputeHash(bytes.ToArray())));
                }
                if(fingerprints.Count!=4)repeated.Add(family.Key+":"+fingerprints.Count);
            }
            Assert.That(repeated,Is.Empty);
        }
        [Test] public void BuildingCells_KeepIndividualPrefabChildren()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Assemblies/BlockHouse.prefab");Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.transform.childCount,Is.GreaterThan(50));
            foreach(Transform child in prefab.transform)
            {
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(child.gameObject),Is.True,child.name);
                Assert.That(child.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThan(0),child.name);
            }
        }
    }
}
#endif
