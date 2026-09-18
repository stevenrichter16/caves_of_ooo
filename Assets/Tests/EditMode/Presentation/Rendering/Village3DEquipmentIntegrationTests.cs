using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual imported library + shipped entity blueprints. Reflection only
    /// adapts the new diagnostic surface so the pre-implementation run can fail
    /// assertions instead of poisoning the whole assembly with a missing API.
    /// These checks establish binding/ownership; screenshots establish visual tilt.</summary>
    public sealed class Village3DEquipmentIntegrationTests
    {
        static bool Find(Village3DPresenter presenter, Entity actor, Entity item, out GameObject view)
        {
            var method=typeof(Village3DPresenter).GetMethod("TryGetEquipmentView",BindingFlags.Instance|BindingFlags.Public);
            Assert.NotNull(method,"Missing read-only equipment view binding API.");
            object[] args={actor,item,null};bool found=(bool)method.Invoke(presenter,args);view=args[2] as GameObject;return found;
        }
        static int FallbackCount(Village3DPresenter presenter)
        {
            var property=typeof(Village3DPresenter).GetProperty("EquipmentFallbackCount");
            Assert.NotNull(property,"Missing explicit unsupported-equipment count.");return(int)property.GetValue(presenter);
        }
        static object Member(object value,string name)
        {
            var type=value.GetType();var property=type.GetProperty(name);if(property!=null)return property.GetValue(value);
            var field=type.GetField(name);Assert.NotNull(field,"Fallback entry lacks "+name);return field.GetValue(value);
        }
        static object[] Fallbacks(Village3DPresenter presenter,Entity actor,Entity item)
        {
            var property=typeof(Village3DPresenter).GetProperty("EquipmentFallbacks");
            Assert.NotNull(property,"Fallback diagnostic must identify native actor/item references, not just count invisible gear.");
            return((IEnumerable)property.GetValue(presenter)).Cast<object>()
                .Where(x=>ReferenceEquals(Member(x,"Actor"),actor)&&ReferenceEquals(Member(x,"Item"),item)).ToArray();
        }
        static Entity Clean(Entity actor)
        {
            var inventory=actor.GetPart<InventoryPart>();Assert.NotNull(inventory);
            foreach(var item in inventory.GetAllEquipped().ToArray())Assert.IsTrue(InventorySystem.UnequipItem(actor,item));
            foreach(var item in inventory.Objects.ToArray())Assert.IsTrue(inventory.RemoveObject(item));
            return actor;
        }
        static Entity Carry(Village3DIntegrationFixture f,Entity actor,string blueprint)
        {
            var item=f.Factory.CreateEntity(blueprint);Assert.NotNull(item,blueprint);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item));Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(item));return item;
        }
        static Entity Equip(Village3DIntegrationFixture f,Entity actor,string blueprint)
        {var item=Carry(f,actor,blueprint);Assert.IsTrue(InventorySystem.Equip(actor,item),blueprint);return item;}
        static GameObject Shown(Village3DIntegrationFixture f,Entity actor,Entity item,string model)
        {
            Assert.IsTrue(Find(f.Presenter,actor,item,out var view),"No view for actual equipped "+item.BlueprintName);
            Assert.NotNull(view);Assert.IsTrue(Village3DIntegrationFixture.Drawn(view));
            var library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(library);
            var prefab=library.FindModel(model);Assert.NotNull(prefab,model);
            var expected=prefab.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh).ToArray();
            if(VoxelWorldPresentation.IsEnabledFor(f.Zone))
            {
                var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);Assert.NotNull(catalog);catalog.Validate();
                expected=expected.Select(catalog.Resolve).ToArray();
            }
            CollectionAssert.AreEquivalent(expected,
                view.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh),"Wrong imported equipment geometry.");
            Assert.Greater(view.GetComponentsInChildren<MeshFilter>(true).Length,0);
            foreach(var renderer in view.GetComponentsInChildren<Renderer>(true))Assert.AreEqual(Village3DPresenter.WorldLayer,renderer.gameObject.layer);
            Assert.AreEqual(0,view.GetComponentsInChildren<Collider>(true).Length,"Attachments cannot create their own interaction targets.");
            return view;
        }
        static void NativeEquipped(Entity actor,Entity item)
        {
            Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.IsTrue(actor.GetPart<InventoryPart>().EquippedItems.Values.Any(x=>ReferenceEquals(x,item)));
            Assert.NotNull(actor.GetPart<InventoryPart>().FindEquippedBodyPart(item));
        }

        [Test] public void RealEquipAndUnequipRefreshFromBusWithoutAnUnrelatedWorldRefresh()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);var item=Carry(f,actor,"LongSword");f.Refresh();
                Assert.IsFalse(Find(f.Presenter,actor,item,out _),"Carried item was represented as equipped.");
                int before=EquipmentChangeBus.GlobalVersion;Assert.IsTrue(InventorySystem.Equip(actor,item));Assert.AreNotEqual(before,EquipmentChangeBus.GlobalVersion);
                Village3DIntegrationFixture.TickFrame(f.Presenter);
                var view=Shown(f,actor,item,"equipment-blade");NativeEquipped(actor,item);
                Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Village3DIntegrationFixture.TickFrame(f.Presenter);
                Assert.IsFalse(Find(f.Presenter,actor,item,out _));Village3DIntegrationFixture.Hidden(view);
                Assert.AreSame(actor,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
            }
        }
        [Test] public void TwoHandedNativeItemCreatesOneStableAttachmentAndClearsOnce()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);var item=Equip(f,actor,"Greatsword");f.Refresh();
                Assert.AreEqual(2,actor.GetPart<InventoryPart>().EquippedItems.Values.Count(x=>ReferenceEquals(x,item)));
                var view=Shown(f,actor,item,"equipment-blade");
                var expected=view.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                Assert.IsTrue(f.Presenter.TryGetOwnerView("$player",out _,out var ownerRoot));
                Assert.AreEqual(1,ownerRoot.GetComponentsInChildren<MeshFilter>(true).Count(x=>x.sharedMesh==expected));
                for(int i=0;i<3;i++){f.Refresh();Assert.IsTrue(Find(f.Presenter,actor,item,out var same));Assert.AreSame(view,same);}
                Assert.IsTrue(InventorySystem.UnequipItem(actor,item));f.Refresh();Assert.IsFalse(Find(f.Presenter,actor,item,out _));Village3DIntegrationFixture.Hidden(view);
            }
        }
        [Test] public void EqualBlueprintItemsStayWithTheirActualActorAndRejectForeignCacheReferences()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var a=Clean(f.Player);var b=Clean(f.View("north-guard-west").owner);
                var ia=Equip(f,a,"Dagger");var ib=Equip(f,b,"Dagger");f.Refresh();
                var va=Shown(f,a,ia,"equipment-blade");var vb=Shown(f,b,ib,"equipment-blade");Assert.AreNotSame(va,vb);
                Assert.IsFalse(Find(f.Presenter,a,ib,out _));Assert.IsFalse(Find(f.Presenter,b,ia,out _));
                a.GetPart<InventoryPart>().EquippedItems["foreign-cache-test"]=ib;EquipmentChangeBus.NotifyChanged(a);f.Refresh();
                Assert.IsFalse(Find(f.Presenter,a,ib,out _));Assert.AreSame(b,ib.GetPart<PhysicsPart>().Equipped);
                Assert.AreSame(ib,a.GetPart<InventoryPart>().EquippedItems["foreign-cache-test"],"Presenter must report bad native state without repairing gameplay.");
                Assert.AreSame(va,Shown(f,a,ia,"equipment-blade"));Assert.AreSame(vb,Shown(f,b,ib,"equipment-blade"));
                var fallback=Fallbacks(f.Presenter,a,ib);Assert.AreEqual(1,fallback.Length);Assert.IsNotEmpty((string)Member(fallback[0],"Reason"));
            }
        }
        [Test] public void ActualWeaponFamiliesUseVerifiedModelsAndOccupiedHandSockets()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);
                foreach(var pair in new[]{("Dagger","equipment-blade"),("Mace","equipment-club"),("Spear","equipment-staff"),("CryoLance","equipment-staff")})
                {
                    var item=Equip(f,actor,pair.Item1);f.Refresh();var view=Shown(f,actor,item,pair.Item2);
                    var part=actor.GetPart<InventoryPart>().FindEquippedBodyPart(item);
                    string expected=(part.GetLaterality()&Laterality.LEFT)!=0?"Equipment.Hand.L":"Equipment.Hand.R";
                    Assert.AreEqual(expected,view.transform.parent.name);
                    var renderer=view.GetComponentInChildren<Renderer>(true);
                    Assert.Greater(Mathf.Max(renderer.bounds.size.x,renderer.bounds.size.z),.4f,
                        pair.Item1+" must have a measurable overhead silhouette; bounds="+renderer.bounds.size
                        +" socketRotation="+view.transform.parent.rotation.eulerAngles+" localRotation="+view.transform.localRotation.eulerAngles
                        +". Visual direction still needs a screenshot.");
                    Assert.IsTrue(InventorySystem.UnequipItem(actor,item));f.Refresh();Assert.IsFalse(Find(f.Presenter,actor,item,out _));
                }
            }
        }
        [Test] public void HeadAndShieldSlotsAreSpecificAndBodyArmorNeverBecomesAHelmet()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);var head=Equip(f,actor,"IronHelmet");var shield=Equip(f,actor,"Buckler");
                var body=Equip(f,actor,"LeatherArmor");f.Refresh();
                Assert.AreEqual("Armor",ItemCategory.GetCategory(shield),"Authored buckler counter-check: Shields category is not present in this content.");
                Assert.AreEqual("Equipment.Head",Shown(f,actor,head,"equipment-helmet").transform.parent.name);
                var shieldView=Shown(f,actor,shield,"equipment-shield");Assert.IsTrue(shieldView.transform.parent.name.StartsWith("Equipment.Hand."));
                Assert.IsFalse(Find(f.Presenter,actor,body,out _));Assert.AreEqual(1,Fallbacks(f.Presenter,actor,body).Length);
                NativeEquipped(actor,head);NativeEquipped(actor,shield);NativeEquipped(actor,body);
            }
        }
        [Test] public void UnsupportedBackClothingIsDiagnosedOnceAndRemainsInNativeInventoryUI()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);f.Refresh();int before=FallbackCount(f.Presenter);
                var cloak=Equip(f,actor,"Cloak");f.Refresh();
                Assert.IsFalse(Find(f.Presenter,actor,cloak,out _),"A cloak cannot silently become the unrelated pack model.");
                Assert.AreEqual(before+1,FallbackCount(f.Presenter));var fallback=Fallbacks(f.Presenter,actor,cloak);Assert.AreEqual(1,fallback.Length);
                Assert.IsNotEmpty((string)Member(fallback[0],"Reason"));
                actor.GetPart<InventoryPart>().EquippedItems["duplicate-back-test"]=cloak;EquipmentChangeBus.NotifyChanged(actor);f.Refresh();
                Assert.AreEqual(before+1,FallbackCount(f.Presenter));Assert.AreEqual(1,Fallbacks(f.Presenter,actor,cloak).Length);
                var ui=InventoryScreenData.Build(actor);Assert.AreEqual(1,ui.Categories.SelectMany(x=>x.Items).Count(x=>ReferenceEquals(x.Item,cloak)&&x.IsEquipped));NativeEquipped(actor,cloak);
                actor.GetPart<InventoryPart>().EquippedItems.Remove("duplicate-back-test");Assert.IsTrue(InventorySystem.UnequipItem(actor,cloak));f.Refresh();Assert.AreEqual(before,FallbackCount(f.Presenter));
            }
        }
        [Test] public void RepeatedPresentationDoesNotChangeEquipmentOwnershipStatsOrNativeVersions()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);var weapon=Equip(f,actor,"Spear");var boots=Equip(f,actor,"IronshodBoots");f.Refresh();Shown(f,actor,weapon,"equipment-staff");
                var inventory=actor.GetPart<InventoryPart>();var carried=inventory.Objects.ToArray();var slots=inventory.EquippedItems.OrderBy(x=>x.Key).ToArray();
                var parts=actor.GetPart<Body>().GetParts();var bodyItems=parts.Select(x=>x._Equipped).ToArray();int penalty=actor.GetStat("Speed").Penalty;
                int bus=EquipmentChangeBus.GlobalVersion,version=f.Zone.EntityVersion;var members=f.Zone.GetReadOnlyEntities().ToArray();
                for(int i=0;i<8;i++)f.Refresh();
                CollectionAssert.AreEqual(carried,inventory.Objects);CollectionAssert.AreEqual(slots,inventory.EquippedItems.OrderBy(x=>x.Key));CollectionAssert.AreEqual(bodyItems,parts.Select(x=>x._Equipped));
                Assert.AreEqual(penalty,actor.GetStat("Speed").Penalty);Assert.AreEqual(bus,EquipmentChangeBus.GlobalVersion);Assert.AreEqual(version,f.Zone.EntityVersion);CollectionAssert.AreEquivalent(members,f.Zone.GetReadOnlyEntities());
                NativeEquipped(actor,weapon);NativeEquipped(actor,boots);Assert.IsFalse(Find(f.Presenter,actor,boots,out _));
            }
        }
        [Test] public void SaveLoadGraphRebuildUsesLoadedItemsAndReleasesOldReferences()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.Player);var item=Equip(f,actor,"LongSword");f.Refresh();var old=Shown(f,actor,item,"equipment-blade");
                var loaded=f.RoundTrip();f.BindLoaded(loaded);var restored=loaded.Player.GetPart<InventoryPart>().GetAllEquipped().Single(x=>x.ID==item.ID);
                Assert.AreNotSame(actor,loaded.Player);Assert.AreNotSame(item,restored);Assert.AreEqual(item.ID,restored.ID);
                Assert.IsFalse(Find(f.Presenter,actor,item,out _));Village3DIntegrationFixture.Hidden(old);
                Shown(f,loaded.Player,restored,"equipment-blade");NativeEquipped(loaded.Player,restored);
                Assert.IsFalse(Find(f.Presenter,loaded.Player,item,out _));Assert.AreEqual(1,loaded.Player.GetPart<InventoryPart>().EquippedItems.Values.Count(x=>ReferenceEquals(x,restored)));
            }
        }
        [Test] public void HiddenActorsSuppressEquipmentRenderersAndRestoreTheSameOwnedView()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.View("north-guard-west").owner);var item=Equip(f,actor,"Mace");f.Refresh();var view=Shown(f,actor,item,"equipment-club");
                var cell=f.Zone.GetEntityCell(actor);cell.IsVisible=false;f.Refresh();Village3DIntegrationFixture.Hidden(view);
                Assert.IsTrue(Find(f.Presenter,actor,item,out var cached));Assert.AreSame(view,cached,"Diagnostic view may exist while hidden; native FOV owns submission.");
                cell.IsVisible=true;f.Refresh();Assert.AreSame(view,Shown(f,actor,item,"equipment-club"));
                f.Presenter.SetPresentationVisible(false);Village3DIntegrationFixture.Hidden(view);NativeEquipped(actor,item);
            }
        }
        [Test] public void RemovingAnActorAndChangingZonesDisposeViewsWithoutUnequippingNativeGear()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var actor=Clean(f.View("north-guard-west").owner);var item=Equip(f,actor,"Spear");f.Refresh();var old=Shown(f,actor,item,"equipment-staff");var at=f.Zone.GetEntityPosition(actor);
                Assert.IsTrue(f.Zone.RemoveEntity(actor));f.Refresh();Assert.IsFalse(Find(f.Presenter,actor,item,out _));Village3DIntegrationFixture.Hidden(old);NativeEquipped(actor,item);
                Assert.IsTrue(f.Zone.AddEntity(actor,at.x,at.y));f.Refresh();var restored=Shown(f,actor,item,"equipment-staff");Assert.AreNotSame(old,restored);
                f.Presenter.Bind(new Zone("equipment-exit-control"),f.Source);Assert.IsFalse(Find(f.Presenter,actor,item,out _));Village3DIntegrationFixture.Hidden(restored);Assert.AreEqual(0,FallbackCount(f.Presenter));NativeEquipped(actor,item);
            }
        }
    }
}
