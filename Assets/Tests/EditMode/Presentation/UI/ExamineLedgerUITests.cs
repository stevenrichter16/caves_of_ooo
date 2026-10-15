using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class ExamineLedgerUITests
    {
        GameObject host; AnnouncementUI ui;
        [SetUp] public void Setup()
        {
            host = new GameObject("Ledger UI test"); host.AddComponent<Grid>();
            ui = host.AddComponent<AnnouncementUI>(); ui.Tilemap = Tiles("fg"); ui.BgTilemap = Tiles("bg");
        }
        [TearDown] public void Cleanup() { ui.Close(); Object.DestroyImmediate(host); }
        Tilemap Tiles(string name) { var go = new GameObject(name); go.transform.SetParent(host.transform); return go.AddComponent<Tilemap>(); }
        int Field(string name) => (int)typeof(AnnouncementUI).GetField(name, BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
        [Test] public void SectionHeadingHasBreathingRoomButPlainProseDoesNotGainBlankRows()
        {
            ui.OpenDocument(new CavesOfOoo.Core.AnnouncementDocument("Well", sections:new[]{new CavesOfOoo.Core.AnnouncementSection("Current state","A cracked lining.")}));
            CollectionAssert.AreEqual(new[]{"CURRENT STATE","","A cracked lining."},ui.VisibleLines);
            ui.OpenDocument(new CavesOfOoo.Core.AnnouncementDocument("Well", sections:new[]{new CavesOfOoo.Core.AnnouncementSection("Description","A stone well.")}));
            CollectionAssert.AreEqual(new[]{"A stone well."},ui.VisibleLines);
        }
        [Test] public void FooterPointerPreviousNextAndBackMatchTheirLabels()
        {
            ui.Open(string.Join("\n",Enumerable.Range(0,100).Select(i=>"Line "+i)));ui.GoToPage(1);
            var method=typeof(AnnouncementUI).GetMethod("HandlePointerCell",BindingFlags.NonPublic|BindingFlags.Instance);
            Assert.NotNull(method,"Footer hints need unambiguous hit regions.");
            int x=Field("_worldOriginX"), y=Field("_worldTopY")-Field("_popupH")+3;
            method.Invoke(ui,new object[]{x+2,y});Assert.AreEqual(0,ui.CurrentPage);
            int next=ui.FooterText.IndexOf("[Right]");Assert.Greater(next,0);
            method.Invoke(ui,new object[]{x+2+next,y});Assert.AreEqual(1,ui.CurrentPage);
            method.Invoke(ui,new object[]{x-1,y});Assert.True(ui.IsOpen);Assert.AreEqual(1,ui.CurrentPage);
            int close=ui.FooterText.IndexOf("[Esc]");Assert.Greater(close,0);
            method.Invoke(ui,new object[]{x+2+close,y});Assert.False(ui.IsOpen);
        }
        [Test] public void GenericNotice_UsesBronzeFrameAndNormalProportionHudGlyphs()
        {
            ui.Open("A readable notice.");
            var corner = new Vector3Int(Field("_worldOriginX"),Field("_worldTopY"),0);
            var color = ui.Tilemap.GetColor(corner);
            Assert.That(color.r, Is.GreaterThan(color.b + .15f), "C's warm bronze frame");
            Assert.True(ui.Tilemap.GetUsedTilesCount() > 0);
            bool found=false; foreach(var p in ui.Tilemap.cellBounds.allPositionsWithin) if(ui.Tilemap.GetTile(p)==HudPixelFont.GetTile('A')) found=true;
            Assert.True(found, "Body uses the normal proportion HUD font");
        }
        [Test] public void FooterIsInsideAnOpaqueFrame_AndShortNoticeCollapses()
        {
            ui.Open("A short notice.");
            foreach(var p in ui.Tilemap.cellBounds.allPositionsWithin)
                if(ui.Tilemap.HasTile(p)) Assert.True(ui.BgTilemap.HasTile(p), "No footer text floats outside the panel at " + p);
            Assert.That(Field("_popupH"), Is.LessThan(20));
        }
        [Test] public void LongThenShortThenClose_DoesNotLeaveOldTiles()
        {
            ui.Open("short"); int fg=ui.Tilemap.GetUsedTilesCount(),bg=ui.BgTilemap.GetUsedTilesCount();
            ui.Open(string.Join("\n", Enumerable.Range(0,140).Select(i=>"paragraph " + i)));
            for(int page=0;page<ui.PageCount;page++)
            {
                ui.GoToPage(page);
                foreach(var map in new[]{ui.Tilemap,ui.BgTilemap}) foreach(var p in map.cellBounds.allPositionsWithin)
                    if(map.HasTile(p)){Assert.That(p.x,Is.InRange(0,79));Assert.That(p.y,Is.InRange(4,44));}
            }
            ui.Open("short");Assert.AreEqual(fg,ui.Tilemap.GetUsedTilesCount());Assert.AreEqual(bg,ui.BgTilemap.GetUsedTilesCount());
            ui.Close();Assert.AreEqual(0,ui.Tilemap.GetUsedTilesCount());Assert.AreEqual(0,ui.BgTilemap.GetUsedTilesCount());
        }
    }
}
