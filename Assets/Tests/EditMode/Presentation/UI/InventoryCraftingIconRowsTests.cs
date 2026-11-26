using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        [TestCase(false, "SteelBladeComponent", "FrostLichen", "IronSpikeComponent")]
        [TestCase(true, "FrostLichen", "SteelBladeComponent", "SparkRoot")]
        public void CraftListPaintsOnlyTheDisclosedInputAndKeepsItsHit(bool brew, string blueprint,
            string otherModeBlueprint, string hiddenBlueprint)
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                using var r = new Rig();
                var item = r.Carry(blueprint);
                r.Carry(otherModeBlueprint);
                var satchel = r.Factory.CreateEntity("FieldSatchel");
                var hidden = r.Factory.CreateEntity(hiddenBlueprint);
                Assert.True(satchel.GetPart<ContainerPart>().AddItem(hidden));
                Assert.True(r.Pack.AddObject(satchel));
                var sprite = InventoryItemIcons.For(item);
                Assert.NotNull(sprite);

                r.Open();
                r.Tab(4);
                if (brew) r.ActivatePainted(r.FindHit("CraftMode", 1));

                int row = r.CraftRow(item);
                Rect hitBounds = Bounds(r.FindHit("CraftRow", row));
                var icons = Icons();
                Assert.That(icons, Has.Length.EqualTo(1), "Only the revealed carried input should have an icon.");
                var icon = icons[0];
                Assert.AreSame(sprite, icon.sprite);
                Assert.False(icon.raycastTarget);
                Canvas.ForceUpdateCanvases();
                var content = (RectTransform)Get(r.UI, "_ledgerContent");
                var iconLocal = RectTransformUtility.CalculateRelativeRectTransformBounds(content, icon.rectTransform);
                var iconBounds = new Rect(iconLocal.min.x - content.rect.xMin, content.rect.yMax - iconLocal.max.y,
                    iconLocal.size.x, iconLocal.size.y);
                Assert.True(hitBounds.Overlaps(iconBounds));
                Rect nameWithIcon = NameBounds();
                Assert.GreaterOrEqual(nameWithIcon.xMin, iconBounds.xMax);
                int hitIndex = r.HitAtScreen(r.ToScreen(iconBounds.center));
                Assert.GreaterOrEqual(hitIndex, 0);
                var hit = r.Hits()[hitIndex];
                Assert.AreEqual("CraftRow", Kind(hit));
                Assert.AreEqual(row, Index(hit));
                Call(r.UI, "ActivateLedgerHit", hit);
                Assert.True(CraftingMarkPart.IsMarked(item));

                InventoryIconSettings.Enabled = false;
                Call(r.UI, "Render");
                Assert.IsEmpty(Icons());
                Assert.AreEqual(hitBounds, Bounds(r.FindHit("CraftRow", row)));
                Assert.Less(NameBounds().xMin, nameWithIcon.xMin);
                Assert.True(CraftingMarkPart.IsMarked(item));
                Assert.AreSame(satchel, hidden.GetPart<PhysicsPart>().InInventory);
                Assert.False(r.UI.ConsumePendingEverydayTurn());

                Image[] Icons() => r.UI.GetComponentsInChildren<Image>(true)
                    .Where(image => image.isActiveAndEnabled && image.sprite?.texture == sprite.texture).ToArray();

                Rect NameBounds() => r.BoundsOf(r.Labels().Single(label => label.Content.Contains(item.GetDisplayName())
                    && r.BoundsOf(label).Overlaps(hitBounds)));
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }
    }
}
