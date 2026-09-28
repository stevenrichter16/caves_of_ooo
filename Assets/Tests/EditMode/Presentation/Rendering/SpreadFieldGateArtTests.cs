using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Run only after the actual child blueprint is published. Real
    /// DoorPart state/structural commands, imported models and current owner
    /// proof; no paid-key, generated-placement or pixel-readability claim.</summary>
    public sealed class SpreadFieldGateArtTests
    {
        const string Closed="spread-field-gate-closed", Open="spread-field-gate-open";
        const string Folder="SpreadFieldGate3D/";
        static GameObject Prefab(string id)=>Resources.Load<GameObject>(Folder+id);

        [TestCase(Closed)] [TestCase(Open)]
        public void TwoOriginalTimberStatesAreInertApprovedPersistentAssets(string id)
        {
            Assert.NotNull(Resources.Load<ScriptableObject>(Folder+"Library"),"Missing original field-gate library.");
            var root=Prefab(id);Assert.NotNull(root,id);
            Assert.AreEqual(Vector3.zero,root.transform.localPosition);
            Assert.AreEqual(Quaternion.identity,root.transform.localRotation);
            Assert.AreEqual(Vector3.one,root.transform.localScale);
            var filters=root.GetComponentsInChildren<MeshFilter>(true);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            Assert.AreEqual(1,filters.Length);Assert.AreEqual(1,renderers.Length);
            Assert.AreEqual(1,root.GetComponentsInChildren<Renderer>(true).Length);
            var mesh=filters[0].sharedMesh;Assert.NotNull(mesh);Assert.True(mesh.isReadable);
            Assert.Greater(mesh.vertexCount,100);Assert.AreEqual(1,mesh.subMeshCount);
            Assert.AreEqual(156,mesh.GetIndexCount(0)/3,"Only the reviewed13 source cuboids belong to either gate.");
            Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
            Assert.AreEqual(1,renderers[0].sharedMaterials.Length);
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,renderers[0].sharedMaterial);
            Assert.True(renderers[0].enabled);Assert.False(renderers[0].forceRenderingOff);
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Animator>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>(true),Is.Empty);
            var points=mesh.vertices.Select(filters[0].transform.TransformPoint).Select(root.transform.InverseTransformPoint).ToArray();
            Assert.That(points.Max(v=>v.y),Is.InRange(.7f,.85f));Assert.GreaterOrEqual(points.Min(v=>v.y),-.001f);
            Assert.True(points.All(v=>Mathf.Abs(v.x)<=.491f&&Mathf.Abs(v.z)<=.491f));
            float depth=points.Max(v=>v.z)-points.Min(v=>v.z);
            if(id==Open)Assert.Greater(depth,.65f,"Open leaf lies aside within the same cell.");
            else Assert.Less(depth,.25f,"Closed timber spans the aperture, not the travel axis.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ActualClosedOpenClosedCommandsPreserveOwnerAndEveryAuthoredAxis(int quarter)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var gate=AddGate(f,quarter);var part=gate.GetPart<DoorPart>();
                var cell=f.Zone.GetEntityCell(gate);var owners=f.Zone.GetReadOnlyEntities().ToArray();string id=gate.ID;
                f.Refresh();var closed=AssertView(f,gate,Closed,quarter);var closedMesh=closed.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.True(cell.BlocksMovement(f.Player));
                Assert.True(part.TrySetOpen(f.Player,f.Zone,true));Assert.AreEqual("/",gate.GetPart<RenderPart>().RenderString);
                f.Refresh(f.Dirty(gate));var open=AssertView(f,gate,Open,quarter);
                Assert.AreNotSame(closedMesh,open.GetComponentInChildren<MeshFilter>().sharedMesh);
                Assert.False(cell.BlocksMovement(f.Player));
                Assert.True(part.TrySetOpen(f.Player,f.Zone,false));Assert.AreEqual("+",gate.GetPart<RenderPart>().RenderString);
                f.Refresh(f.Dirty(gate));AssertView(f,gate,Closed,quarter);
                Assert.True(cell.BlocksMovement(f.Player));Assert.AreEqual(id,gate.ID);Assert.AreSame(cell,f.Zone.GetEntityCell(gate));
                Assert.AreSame(part,gate.GetPart<DoorPart>());Assert.AreEqual(quarter,part.QuarterTurns);
                CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());
            }
        }

        [Test]
        public void OrdinaryVillageDoorRetainsItsOriginalStoneStateModels()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var at=Approach(f);var owner=f.Add("VillageDoor",at.x,at.y);var door=owner.GetPart<DoorPart>();door.QuarterTurns=3;
                if(door.IsClosed)Assert.True(door.TrySetOpen(f.Player,f.Zone,true));
                f.Refresh();Assert.True(f.Find(owner,out _,out var open));StringAssert.StartsWith("stillleaf-open-door-",open);
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
                Assert.True(door.TrySetOpen(f.Player,f.Zone,false));f.Refresh(f.Dirty(owner));
                Assert.True(f.Find(owner,out _,out var closed));StringAssert.StartsWith("stillleaf-door-",closed);
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
                Assert.False(IsGate(open));Assert.False(IsGate(closed));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void StructuralDamageKeepsCurrentStateButActualDestructionLeavesNoGateView(bool open)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var gate=AddGate(f,1);var door=gate.GetPart<DoorPart>();var structure=gate.GetPart<DestructiblePart>();
                if(open)Assert.True(door.TrySetOpen(f.Player,f.Zone,true));
                var cell=f.Zone.GetEntityCell(gate);var original=f.Zone.GetReadOnlyEntities().Where(e=>!ReferenceEquals(e,gate)).ToArray();
                f.Refresh();var before=AssertView(f,gate,open?Open:Closed,1).GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(gate,9,f.Player,f.Zone));Assert.AreEqual(1,structure.HP);
                f.Refresh(f.Dirty(gate));var hurt=AssertView(f,gate,open?Open:Closed,1);
                Assert.AreSame(before,hurt.GetComponentInChildren<MeshFilter>().sharedMesh,"Partial structural damage does not invent a third art state.");
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(gate,1,f.Player,f.Zone));Assert.True(structure.Gone);
                Assert.Null(f.Zone.GetEntityCell(gate));f.Refresh(SpawnRing3DIntegrationFixture.Dirty(cell.X,cell.Y));
                Assert.False(f.Find(gate,out _,out _));Assert.False(f.Rendered(gate));
                Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(gate,out _));
                Assert.False(IsGate(SpawnRing3DRecipes.Resolve(f.Zone,gate,f.Library.Definition).ModelId));
                CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities(),"Actual destruction leaves the original ground, without invented rubble or stock.");
            }
        }

        [TestCase("hidden")] [TestCase("removed")] [TestCase("custom-visual")]
        [TestCase("wrong-quarter")] [TestCase("foreign-door-part")] [TestCase("solid-physics")]
        [TestCase("carried")] [TestCase("foreign-zone")]
        public void InvalidOrForeignOwnerCannotAcquireTheExactFieldGateForms(string fault)
        {
            using(var f=new SpawnRing3DIntegrationFixture(fault=="foreign-zone"?SpawnRing3DIntegrationFixture.Grove:"Overworld.11.10.0"))
            {
                var gate=AddGate(f,0);
                if(fault=="hidden")gate.GetPart<RenderPart>().Visible=false;
                if(fault=="removed")Assert.True(f.Zone.RemoveEntity(gate));
                if(fault=="custom-visual")gate.GetPart<RenderPart>().VisualID="unrelated-authored-model";
                if(fault=="wrong-quarter")gate.GetPart<DoorPart>().QuarterTurns=4;
                if(fault=="foreign-door-part")gate.GetPart<DoorPart>().ParentEntity=f.Player;
                if(fault=="solid-physics")gate.GetPart<PhysicsPart>().Solid=true;
                if(fault=="carried")gate.GetPart<PhysicsPart>().InInventory=f.Player;
                Assert.False(IsGate(SpawnRing3DRecipes.Resolve(f.Zone,gate,f.Library.Definition).ModelId),fault);
            }
        }

        static bool IsGate(string id)=>id==Closed||id==Open;
        static Entity AddGate(SpawnRing3DIntegrationFixture f,int quarter)
        {
            var owner=f.Factory.CreateEntity("SpreadFieldGate");
            Assert.NotNull(owner,"SOURCE PRECONDITION: publish the real child before interpreting any missing-binding RED.");
            var physics=owner.GetPart<PhysicsPart>();var door=owner.GetPart<DoorPart>();var structure=owner.GetPart<DestructiblePart>();
            Assert.NotNull(physics);Assert.NotNull(door);Assert.NotNull(structure);
            Assert.AreSame(owner,physics.ParentEntity);Assert.AreSame(owner,door.ParentEntity);Assert.AreSame(owner,structure.ParentEntity);
            Assert.False(physics.Solid);Assert.False(physics.Takeable);Assert.False(owner.HasTag("Solid"));
            Assert.AreEqual(10,structure.HP);Assert.AreEqual(10,structure.MaxHP);Assert.AreEqual(0,structure.Hardness);
            Assert.False(structure.Indestructible);Assert.IsEmpty(structure.WreckageBlueprint);
            Assert.IsEmpty(door.OwnerId);Assert.AreEqual("&w",owner.GetPart<RenderPart>().ColorString);
            var at=Approach(f);Assert.True(f.Zone.AddEntity(owner,at.x,at.y));door.QuarterTurns=quarter;
            if(!door.IsClosed)Assert.True(door.TrySetOpen(f.Player,f.Zone,false));
            Assert.AreEqual("+",owner.GetPart<RenderPart>().RenderString);return owner;
        }
        static (int x,int y) Approach(SpawnRing3DIntegrationFixture f)
        {
            for(int y=1;y<Zone.Height-1;y++)for(int x=2;x<Zone.Width-1;x++)
            {
                var cell=f.Zone.GetCell(x,y);var adjacent=f.Zone.GetCell(x-1,y);
                if(Plain(f,cell)&&Plain(f,adjacent)){Assert.True(f.Zone.MoveEntity(f.Player,x-1,y));return(x,y);}
            }
            Assert.Fail("No actual plain-ground adjacent placement; do not clear native content for this art fixture.");return(-1,-1);
        }
        static bool Plain(SpawnRing3DIntegrationFixture f,Cell cell)
        {var tile=f.Zone.TileState.Get(cell.X,cell.Y);return !cell.BlocksMovement(f.Player)&&(tile==null||tile.IsEmpty)&&cell.Objects.All(e=>e.BlueprintName=="Grass"||ReferenceEquals(e,f.Player));}
        static GameObject AssertView(SpawnRing3DIntegrationFixture f,Entity owner,string id,int quarter)
        {
            var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
            Assert.AreEqual(id,recipe.ModelId,recipe.Failure);Assert.IsNull(recipe.Failure);Assert.AreSame(owner,recipe.Owner);
            Assert.AreEqual(quarter,recipe.QuarterTurns);Assert.False(recipe.Batched);Assert.False(recipe.Transient);
            Assert.True(f.Find(owner,out var view,out var actual));Assert.AreEqual(id,actual);Assert.True(f.Rendered(owner));
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0,90*quarter,0),view.transform.localRotation),.01f);
            Assert.AreEqual(Village3DProjection.CellCentre(f.Zone.GetEntityCell(owner).X,f.Zone.GetEntityCell(owner).Y),view.transform.position);
            var original=Prefab(id);Assert.NotNull(original);var expected=original.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.AreSame(expected,view.GetComponentInChildren<MeshFilter>().sharedMesh);
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);
            Assert.AreEqual(id,proof.ModelId);Assert.AreEqual(1,proof.PieceCount);Assert.False(proof.Batched);
            Assert.AreSame(expected,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);
            Assert.That(view.GetComponentsInChildren<Animator>(true),Is.Empty);return view;
        }
    }
}
