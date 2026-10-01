using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual examined plants give players fiction and actions, never
    /// the editorial constraints used while authoring them.</summary>
    public sealed class BiomeCropPlayerTextTests : CultivatedCropTestBase
    {
        static readonly string[] EditorialPhrases={"player-facing", "no new theology", "existing refueling mechanic",
            "warning remains true", "no new mind-control", "no promise of", "deliberately original", "canon slope ecology",
            "not a manufactured", "not permanent bio-light", "not a Choir sawmill", "never an infinite", "not a petrified god fragment"};
        static string EditorialPhrase(string text)=>EditorialPhrases.FirstOrDefault(p=>text?.IndexOf(p,StringComparison.OrdinalIgnoreCase)>=0);
        [Test] public void ActualSpeciesExamineTextIsInWorldAndWarnsOfHarmfulSelfApplication()
        {
            var errors=new List<string>();
            foreach(var species in BiomeCropCatalog.All)
            {
                foreach(var id in new[]{species.SeedBlueprint,species.CropBlueprint,species.YieldBlueprint})
                {
                    string text=Factory.CreateEntity(id)?.GetPart<ExaminablePart>()?.Text;
                    if(string.IsNullOrWhiteSpace(text))errors.Add(id+": missing examine text");
                    if(EditorialPhrase(text) is string phrase)errors.Add(id+": editorial text: "+phrase);
                    if(species.UtilityKind=="ThrownStatus"&&(text?.IndexOf("yourself",StringComparison.OrdinalIgnoreCase)<0))
                        errors.Add(id+": applying harmful sap to yourself is not disclosed");
                }
                if(species.UtilityKind=="ThrownStatus"&&species.UseText.IndexOf("yourself",StringComparison.OrdinalIgnoreCase)<0)
                    errors.Add(species.Id+": catalogue omits self-application hazard");
            }
            Assert.IsEmpty(errors,string.Join("\n",errors));
        }
        [Test] public void EditorialCounterCheckRejectsDesignNotesButKeepsUsefulInstructions()
        {
            foreach(string phrase in EditorialPhrases)Assert.NotNull(EditorialPhrase("A root. "+phrase+"."),phrase);
            Assert.Null(EditorialPhrase("Apply the pulp to stop bleeding. Plant its seed in prepared soil and Conjure Rain nearby."));
        }
    }
}
