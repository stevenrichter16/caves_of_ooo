using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadSidebarParagraphTests
 {
  [TestCase("\n")][TestCase("\r\n")][TestCase("\r")]
  public void ExplicitParagraphsAndBlankLineRemainDistinctRows(string newline)
  {
   var lines=SidebarTextFormatter.WrapWithPrefixes("First"+newline+newline+"Last",14,":: ","   ");
   CollectionAssert.AreEqual(new[]{":: First","   ","   Last"},lines);
   Assert.False(lines.Any(s=>s.Contains("\n")||s.Contains("\r")));
  }
  [Test]public void LongUnbrokenWordsKeepEveryCharacterAndRespectWidth()
  {
   const string word="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
   var lines=SidebarTextFormatter.WrapWithPrefixes(word,12,":: ","   ");
   Assert.That(lines.All(s=>s.Length<=12));Assert.AreEqual(word,string.Concat(lines.Select(s=>s.Substring(3))));
  }
  [Test]public void LogParagraphRowsKeepOneMessageIdentityAndOrdering()
  {
   var lines=SidebarTextFormatter.FormatLog(new[]{new SidebarLogEntry("start\nfinish",9,1,23)},12);
   CollectionAssert.AreEqual(new[]{":: start","   finish"},lines.Select(x=>x.Text));
   Assert.AreEqual(0,lines[0].RowIndexWithinEntry);Assert.AreEqual(1,lines[1].RowIndexWithinEntry);
   Assert.That(lines.All(x=>x.EntryNewestSerial==23&&x.EntryRowCount==2));
  }
  [Test]public void FocusUsesTheSameParagraphBoundariesWithoutControlGlyphs()
  {
   var snapshot=new LookSnapshot(1,1,"Name","",new[]{"Afflicted:\n- first\n- last"},null,null);
   var lines=SidebarTextFormatter.FormatFocus(snapshot,31,11);
   CollectionAssert.AreEqual(new[]{"Name","Afflicted:","- first","- last"},lines);
  }
  [TestCase(null)][TestCase("")][TestCase("   ")]
  public void EmptyContentRemainsOneBoundedPrefix(string value)
  {CollectionAssert.AreEqual(new[]{":: "},SidebarTextFormatter.WrapWithPrefixes(value,12,":: ","   "));}
  [Test]public void ShortSingleLineRetainsExistingAppearance()
  {CollectionAssert.AreEqual(new[]{":: quiet turn"},SidebarTextFormatter.WrapWithPrefixes("quiet turn",31,":: ","   "));}
 }
}
