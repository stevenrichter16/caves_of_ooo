#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class NativeSpellFxImportTests
    {
        private static readonly string[] Ids = {
            "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Hydromancy_JetBlast",
            "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm",
            "Hydromancy_ConjureRain"
        };

        private static ScriptableObject Library()
        {
            var library = Resources.Load<ScriptableObject>("SpellFx3D/Library");
            Assert.NotNull(library, "Seven real Blender spell entries must be available in a player build.");
            return library;
        }

        private static T Read<T>(object owner, string name)
        {
            Assert.NotNull(owner);
            var field = owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, "Missing imported contract field " + name);
            return (T)field.GetValue(owner);
        }

        private static object Entry(string id)
        {
            var entries = Read<IEnumerable>(Library(), "Entries").Cast<object>().ToArray();
            return entries.Single(e => Read<string>(e, "SpellId") == id);
        }

        [Test]
        public void ImportedLibrary_HasExactlyTheSixStartingSpellsAndLearnableRain()
        {
            var entries = Read<IEnumerable>(Library(), "Entries").Cast<object>().ToArray();
            CollectionAssert.AreEquivalent(Ids, entries.Select(e => Read<string>(e, "SpellId")));
            var material = Read<Material>(Library(), "Material");
            Assert.NotNull(material);
            Assert.AreEqual("CavesOfOoo/Village3D/Palette", material.shader.name);
            Assert.IsTrue(material.HasProperty("_FogLight"));
            Assert.IsTrue(material.HasProperty("_Transient"));
        }

        [TestCaseSource(nameof(Ids))]
        public void ImportedPieces_HaveActualFiniteMeshAndMotionSamples(string id)
        {
            var pieces = Read<IEnumerable>(Entry(id), "Pieces").Cast<object>().ToArray();
            Assert.IsNotEmpty(pieces, id);
            foreach (var piece in pieces)
            {
                var mesh = Read<Mesh>(piece, "Mesh");
                Assert.NotNull(mesh, id);
                Assert.Greater(mesh.vertexCount, 2);
                Assert.GreaterOrEqual(mesh.triangles.Length, 3);
                foreach (var vertex in mesh.vertices)
                    Assert.IsTrue(Finite(vertex.x) && Finite(vertex.y) && Finite(vertex.z));
                var poses = Read<IEnumerable>(piece, "Poses").Cast<object>().ToArray();
                Assert.AreEqual(111, poses.Length, id + " must retain the 100-fps authored samples.");
                foreach (var pose in poses)
                {
                    var scale = Read<Vector3>(pose, "Scale");
                    Assert.IsTrue(Finite(scale.x) && Finite(scale.y) && Finite(scale.z));
                    Assert.GreaterOrEqual(Mathf.Min(scale.x, scale.y, scale.z), -0.000001f);
                }
                Assert.Less(Read<Vector3>(poses.Last(), "Scale").sqrMagnitude, .000001f,
                    "No imported transient fragment may survive the last authored sample.");
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void ImportedCast_ActuallyMovesNativeBonesAndReturnsToRestWithoutRootMotion(string id)
        {
            var clip = Read<AnimationClip>(Entry(id), "CastClip");
            Assert.NotNull(clip, id);
            Assert.GreaterOrEqual(clip.length, .65f);
            var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            Assert.NotNull(village);
            var instance = Object.Instantiate(village.FindModel(village.PlayerModelId));
            try
            {
                var animator = instance.GetComponentInChildren<Animator>(true);
                Assert.NotNull(animator);
                animator.enabled = false;
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.IsNotEmpty(bindings, id);
                foreach (var binding in bindings)
                {
                    Assert.AreEqual(typeof(Transform), binding.type);
                    Assert.IsFalse(string.IsNullOrEmpty(binding.path), "Caster root must not be keyed.");
                    Assert.NotNull(animator.transform.Find(binding.path), id + ": " + binding.path);
                }
                var bones = instance.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
                clip.SampleAnimation(animator.gameObject, 0);
                var start = bones.Select(b => b.localRotation).ToArray();
                var rootPosition = instance.transform.position;
                var rootRotation = instance.transform.rotation;
                clip.SampleAnimation(animator.gameObject, .22f);
                Assert.Greater(bones.Select((bone, index) => Quaternion.Angle(start[index], bone.localRotation)).Max(), 5f,
                    "An imported clip existing is insufficient; the actual game skeleton must move.");
                clip.SampleAnimation(animator.gameObject, .66f);
                Assert.Less(bones.Select((bone, index) => Quaternion.Angle(start[index], bone.localRotation)).Max(), .2f);
                Assert.AreEqual(rootPosition, instance.transform.position);
                Assert.AreEqual(rootRotation, instance.transform.rotation);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
#endif
