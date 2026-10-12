using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SkillBrowserPolishTests
    {
        Entity actor;
        [SetUp] public void Setup()
        {
            SkillRegistry.ResetForTests();SkillRegistry.EnsureInitialized();
            actor=new Entity {ID="polish"};actor.AddPart(new ActivatedAbilitiesPart());actor.AddPart(new SkillsPart());
            actor.Statistics["SP"]=new Stat {Owner=actor,Name="SP",BaseValue=10};
            actor.Statistics["Agility"]=new Stat {Owner=actor,Name="Agility",BaseValue=18};
        }
        [TearDown] public void Cleanup()=>SkillRegistry.ResetForTests();
        [Test] public void TextGraphicRequiresRenderer_AndFitsShortButtonLabels()
        {
            var go=new GameObject("text",typeof(RectTransform),typeof(SkillScreenText));
            try { Assert.NotNull(go.GetComponent<CanvasRenderer>(),"Without this all labels disappear in native screenshots."); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ColdDescriptionsKeepMinusAndDegreeMeaning()
        {
            string wrapped=string.Join(" ",SkillScreenText.Wrap("−300J at 20°; requires ≥15",60));
            Assert.AreEqual("-300J at 20 degrees; requires >=15",wrapped);
        }
        [Test] public void HiddenPrerequisiteDoesNotLeakThroughDetailProseOrFailure()
        {
            SkillRegistry.TryGetPowerByClass("Acrobatics_Tumble",out var hidden);hidden.Hidden=true;
            SkillRegistry.TryGetPowerByClass("Acrobatics_Vault",out var vault);vault.Requires=hidden.Class;
            var b=new SkillBrowserModel();b.Open(actor);b.SelectClass(vault.Class);
            StringAssert.DoesNotContain("Tumble",b.Requirements);StringAssert.DoesNotContain("Tumble",b.Details);
            Assert.False(b.ReviewPurchase());StringAssert.DoesNotContain("Tumble",b.Notice);
            actor.GetPart<SkillsPart>().AddSkill(hidden.Class);b.Refresh();
            StringAssert.Contains("Tumble",b.Details);
        }
        [Test] public void HiddenParentDoesNotHideBookOwnedChild()
        {
            SkillRegistry.TryGetSkillByClass("AcrobaticsSkill",out var root);root.Hidden=true;
            actor.GetPart<SkillsPart>().AddSkill("Acrobatics_Vault");
            var b=new SkillBrowserModel();b.Open(actor);
            Assert.True(b.SelectClass("Acrobatics_Vault"));Assert.True(b.CanAssign);
            Assert.AreEqual("Unknown discipline",b.Current.Name);
            Assert.False(b.SelectClass("Acrobatics_Tumble"));
        }
        [Test] public void AssignmentMovesExistingBindingAndReplacesOnlyChosenSlot()
        {
            var skills=actor.GetPart<SkillsPart>();skills.AddSkill("Acrobatics_Vault");skills.AddSkill("Acrobatics_Tumble");
            var b=new SkillBrowserModel();b.Open(actor);b.SelectClass("Acrobatics_Vault");Assert.True(b.Assign(7));
            StringAssert.Contains("Shortcut: 8",b.Details);
            b.SelectClass("Acrobatics_Tumble");Assert.True(b.Assign(8));
            b.SelectClass("Acrobatics_Vault");Assert.True(b.Assign(8));
            var abilities=actor.GetPart<ActivatedAbilitiesPart>();Assert.Null(abilities.GetAbilityBySlot(7));
            Assert.AreEqual("Acrobatics_Vault",abilities.GetAbilityBySlot(8).SourcePowerClass);
            Assert.AreEqual(2,abilities.AbilityList.Count);Assert.AreEqual(10,actor.GetStatValue("SP"));
            Assert.False(b.Assign(-1));Assert.False(b.Assign(10));
        }
        [Test] public void LongWordsAndAllDescriptionLinesSurvivePaging()
        {
            string word=new string('x',113);var lines=SkillScreenText.Wrap(word,45);
            Assert.AreEqual(word,string.Join("",lines));Assert.True(lines.All(l=>l.Length<=45));
            var many=SkillScreenText.Wrap(string.Join("\n",Enumerable.Range(1,71).Select(i=>"Line "+i)),45);
            string paged=string.Join("\n",Enumerable.Range(0,4).Select(i=>SkillScreenText.Page(many,i,18)));
            StringAssert.Contains("Line 71",paged);Assert.AreEqual(71,paged.Split('\n').Length);
        }
        [Test] public void ShortLabelsAnnounceClippingButDetailsKeepFullWords()
        {
            string name="A deliberately very long named ability";
            StringAssert.EndsWith("...",SkillScreenText.Fit(name,12,1));
            Assert.AreEqual("Vault",SkillScreenText.Fit("Vault",12,1));
            Assert.AreEqual(name,string.Join(" ",SkillScreenText.Wrap(name,12)));
        }
        [Test] public void RegistryWithoutPowersIsStillBrowsable()
        {
            SkillRegistry.TryGetSkillByClass("AcrobaticsSkill",out var root);root.Powers=null;
            var b=new SkillBrowserModel();b.Open(actor);Assert.True(b.SelectClass(root.Class));Assert.AreEqual(1,b.Current.Rows.Count);
        }
        [Test] public void SwitchingSelectionCancelsPendingPurchase()
        {
            var b=new SkillBrowserModel();b.Open(actor);b.SelectClass("AcrobaticsSkill");Assert.True(b.ReviewPurchase());
            b.SelectClass("Acrobatics_Vault");Assert.False(b.ConfirmPurchase());Assert.AreEqual(10,actor.GetStatValue("SP"));
        }
        [Test] public void ORRequirementsRetainActualAndRequiredValues()
        {
            SkillRegistry.TryGetPowerByClass("Acrobatics_Vault",out var vault);vault.Attribute="Agility|Strength,Ego";vault.Minimum="19|16,12";
            var b=new SkillBrowserModel();b.Open(actor);b.SelectClass(vault.Class);
            StringAssert.Contains("Agility 18 / 19\nOR\nStrength 0 / 16\nEgo 0 / 12",b.Requirements);
            Assert.False(b.ReviewPurchase());
        }
        [Test] public void ObfuscatedSelectionNeverRevealsRawDescription()
        {
            SkillRegistry.TryGetPowerByClass("Acrobatics_Vault",out var vault);vault.Obfuscated=true;vault.Minimum="99";vault.Description="SECRET_PAYLOAD";
            var b=new SkillBrowserModel();b.Open(actor);b.SelectClass(vault.Class);
            Assert.AreEqual("???",b.Selected.DisplayName);StringAssert.DoesNotContain("SECRET_PAYLOAD",b.Details);
            Assert.IsEmpty(b.Prerequisites);Assert.IsEmpty(b.Requirements);Assert.False(b.ReviewPurchase());
        }
    }
}
