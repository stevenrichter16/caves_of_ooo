using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        string inspectedAbility,riteReading;
        static bool IsThrowingAbility(ActivatedAbility a) => a!=null&&new[]{"CommandMeasuredThrow","CommandDrivingThrow","CommandRecoveringStep"}.Contains(a.Command);
        ActivatedAbility OwnedAbility(string id) => Guid.TryParse(id,out var guid)?player.GetPart<ActivatedAbilitiesPart>()?.GetAbility(guid):null;
        // Native number keys move in this browser. Keep the facts, replace only its shortcut instruction.
        static string BrowserAbilityText(string text) => text==null?null:string.Join("\n",text.Split('\n').Where(line=>!line.StartsWith("Shortcut:",StringComparison.Ordinal)));
        string AbilityText(ActivatedAbility a) => BrowserAbilityText(AbilityDetailsBuilder.BuildForAbility(player,a.ID));
        object AbilityDetailsView()
        {
            var a=OwnedAbility(inspectedAbility);if(a==null)return null;
            return new{id=a.ID.ToString(),name=a.DisplayName,details=AbilityText(a),slot=player.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(a.ID),
                canUse=!Dead&&!faulted&&a.IsUsable,targeting=a.TargetingMode.ToString(),range=a.Range,cooldown=a.CooldownRemaining,
                rite=RitePreviewBuilder.OwnedRite(player,a.ID)!=null,throwing=IsThrowingAbility(a)};
        }
        void ManageAbility(Intent a)
        {
            var ability=OwnedAbility(a.id);if(ability==null){error="That ability is not yours.";return;}
            var part=player.GetPart<ActivatedAbilitiesPart>();
            switch(a.command)
            {
                case "details":break;
                case "assign":
                    if(a.index<0||a.index>=ActivatedAbilitiesPart.SlotCount){error="Choose a slot from 1 to 10.";return;}
                    part.AssignAbilityToSlot(ability.ID,a.index);break;
                case "unassign":
                    int slot=part.GetSlotForAbility(ability.ID);if(slot>=0)part.AssignAbilityToSlot(Guid.Empty,slot);break;
                default:error="That ability control is not available.";return;
            }
            inspectedAbility=a.id;panel="abilityDetails";
        }
        void ReadRite(Intent a)
        {
            var ability=OwnedAbility(a.id);
            var text=ability==null?null:RitePreviewBuilder.Build(player,zone,ability.ID,a.dx,a.dy);
            if(text==null){error="Choose an owned rite and a valid direction to read its marks.";return;}
            riteReading=BrowserAbilityText(text);panel="ritePreview";
        }
    }
}
