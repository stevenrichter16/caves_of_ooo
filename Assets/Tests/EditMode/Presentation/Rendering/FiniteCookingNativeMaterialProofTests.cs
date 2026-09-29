using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Retained live observer failure is the RED. These native paired controls
    // demonstrate its mistaken source-vs-owned-material premise using real coals.
    public sealed class FiniteCookingNativeMaterialProofTests
    {
        [TestCase(false)] [TestCase(true)]
        public void ActualCoalsRequireOwnedSubmittedPaletteWhilePreservingPersistentSource(bool foreignPalette)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=f.Add("SpreadCookingCoals");f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner,out var valid),valid.Failure);
                var entry=SpreadCookingCoalsLibrary.Load().Find("spread-cooking-coals-hot");Assert.NotNull(entry);
                var view=f.View(owner);var renderer=view.GetComponent<Renderer>();var owned=renderer.sharedMaterial;var source=entry.Material;
                Assert.AreEqual(entry.Id,valid.ModelId);Assert.AreSame(entry.Mesh,valid.ExpectedMesh);Assert.AreSame(entry.Mesh,view.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(source,valid.ExpectedMaterial);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,source);
                Assert.AreSame(owned,valid.SubmittedMaterial);Assert.AreNotSame(source,owned,"A real surface owns its material clone: the old native observer equality must fail here.");
                var sourceColor=source.GetColor("_BaseColor");var sourceMap=source.GetTexture("_BaseMap");Material foreign=null;
                try
                {
                    if(foreignPalette){foreign=new Material(owned);foreign.SetColor("_BaseColor",Color.magenta);renderer.sharedMaterial=foreign;}
                    bool accepted=presenter.TryGetApprovedStyle(owner,out var proof);
                    Assert.AreEqual(!foreignPalette,accepted);
                    if(foreignPalette)Assert.AreEqual("submitted-palette-mismatch",proof.Failure);
                    else {Assert.AreSame(entry.Mesh,proof.SubmittedMesh);Assert.AreSame(renderer.sharedMaterial,proof.SubmittedMaterial);Assert.AreSame(source,proof.ExpectedMaterial);}
                    Assert.AreEqual(sourceColor,source.GetColor("_BaseColor"));Assert.AreSame(sourceMap,source.GetTexture("_BaseMap"));
                }
                finally {renderer.sharedMaterial=owned;if(foreign!=null)Object.DestroyImmediate(foreign);}
                Assert.True(presenter.TryGetApprovedStyle(owner,out var restored),restored.Failure);Assert.AreSame(owned,restored.SubmittedMaterial);
            }
        }
    }
}
