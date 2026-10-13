using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Rendering
{
    /// <summary>Event-driven skills browser. Opening/selecting never purchases or activates.
    /// Class identity survives refresh; all commits revalidate against live gameplay rules.</summary>
    public sealed class SkillBrowserModel
    {
        public sealed class Discipline
        {
            public string Class, Name;
            public readonly List<SkillsScreenRow> Rows = new List<SkillsScreenRow>();
        }
        public readonly List<Discipline> Disciplines = new List<Discipline>();
        public readonly List<string> Prerequisites = new List<string>();
        readonly Dictionary<string, string> remembered = new Dictionary<string, string>();
        public Entity Actor { get; private set; }
        public int DisciplineIndex { get; private set; }
        public int SkillIndex { get; private set; }
        public bool Overview { get; set; }
        public string PendingPurchase { get; private set; }
        public int PendingRank { get; private set; }
        public int PendingCost { get; private set; }
        public string PendingWarning => AbilityDetailsBuilder.PurchaseWarning(Actor,PendingPurchase);
        public string Details { get; private set; } = "";
        public string Requirements { get; private set; } = "";
        public string ActivationSummary { get; private set; } = "";
        public string Notice { get; private set; } = "";
        public int Points => Actor?.GetStat("SP")?.BaseValue ?? 0;
        public Discipline Current => Disciplines.Count == 0 ? null : Disciplines[DisciplineIndex];
        public SkillsScreenRow Selected => Current == null || Current.Rows.Count == 0 ? default : Current.Rows[SkillIndex];
        public bool CanAssign => OwnedAbility() != null;

        public void Open(Entity actor)
        {
            if (Actor != actor) { remembered.Clear(); DisciplineIndex = SkillIndex = 0; Overview = false; }
            Actor = actor; PendingPurchase = null; Notice = ""; Refresh();
        }
        public void Refresh()
        {
            string selected = Selected.Class;
            Disciplines.Clear();
            if (Actor != null)
            {
                var snapshot = SkillsScreenStateBuilder.Build(Actor);
                var rows = new Dictionary<string, SkillsScreenRow>();
                foreach (var row in snapshot.Rows) rows[row.Class] = row;
                foreach (var tree in SkillRegistry.GetAllSkills())
                {
                    if (!rows.TryGetValue(tree.Class, out var root)) continue;
                    bool hiddenRoot=(tree.Flags & SkillData.FLAG_HIDDEN) != 0 && root.State != SkillsScreenRowState.Owned;
                    if(hiddenRoot) root=new SkillsScreenRow(tree.Class,"Unknown discipline","",true,"",-1,SkillsScreenRowState.RequirementsNotMet,true);
                    var group = new Discipline { Class=tree.Class, Name=root.DisplayName };
                    group.Rows.Add(root);
                    if(tree.Powers!=null) foreach (var power in tree.Powers)
                        if (power!=null && rows.TryGetValue(power.Class, out var row)
                            && (!hiddenRoot || row.State==SkillsScreenRowState.Owned)
                            && ((power.Flags & SkillData.FLAG_HIDDEN) == 0 || row.State == SkillsScreenRowState.Owned)) group.Rows.Add(row);
                    if(!hiddenRoot || group.Rows.Count>1) Disciplines.Add(group);
                }
                Disciplines.Sort((a,b) => string.Compare(a.Name,b.Name,StringComparison.Ordinal));
            }
            DisciplineIndex = Math.Max(0, Math.Min(DisciplineIndex, Disciplines.Count-1));
            SkillIndex = 0;
            if (!SelectClass(selected)) UpdateDetails();
        }
        public bool SelectClass(string className)
        {
            if (string.IsNullOrEmpty(className)) return false;
            for (int t=0;t<Disciplines.Count;t++)
                for (int i=0;i<Disciplines[t].Rows.Count;i++)
                    if (Disciplines[t].Rows[i].Class == className)
                    { DisciplineIndex=t; SelectSkill(i); return true; }
            return false;
        }
        public void SelectDiscipline(int index)
        {
            if (Disciplines.Count == 0) return;
            DisciplineIndex=Math.Max(0,Math.Min(index,Disciplines.Count-1)); SkillIndex=0;
            if (remembered.TryGetValue(Current.Class,out var name))
                for(int i=0;i<Current.Rows.Count;i++) if(Current.Rows[i].Class==name) SkillIndex=i;
            Cancel(); Notice=""; UpdateDetails();
        }
        public void SelectSkill(int index)
        {
            if (Current == null) return;
            SkillIndex=Math.Max(0,Math.Min(index,Current.Rows.Count-1));
            remembered[Current.Class]=Selected.Class; Cancel(); Notice=""; UpdateDetails();
        }
        public string StateLabel(SkillsScreenRow row)
        {
            if (row.State==SkillsScreenRowState.Owned) return row.MaxRank>1 ? (row.CanUpgrade?"OWNED ":"MAX ")+row.OwnedRank+"/"+row.MaxRank : "OWNED";
            if (row.IsObfuscated) return "UNKNOWN";
            if (row.Cost<0) return "DISCOVERY";
            if (row.State==SkillsScreenRowState.Buyable) return "AVAILABLE";
            return row.State==SkillsScreenRowState.InsufficientSP ? "NEED SP" : "LOCKED";
        }
        public bool ReviewPurchase()
        {
            Cancel();
            if(Selected.IsObfuscated) { Notice="Requirements remain unknown."; return false; }
            var eligible=SkillPurchaseEligibility.Evaluate(Actor,Selected.Class);
            if (!eligible.Succeeded) { Notice=FailureForActor(Selected.DisplayName,eligible); return false; }
            PendingPurchase=Selected.Class; PendingRank=eligible.OldRank; PendingCost=eligible.Cost; return true;
        }
        public void Cancel() => PendingPurchase=null;
        public bool ConfirmPurchase()
        {
            if (PendingPurchase==null) return false;
            string name=Selected.DisplayName, pending=PendingPurchase; Cancel();
            var result=BuySkillAction.Execute(Actor,pending,PendingRank,PendingCost);
            Refresh();
            Notice=result.Succeeded ? (result.OldRank>0?"Upgraded ":"Learned ")+name+(result.MaxRank>1?" to rank "+result.NewRank:"")+". Spent "+result.CostPaid+" SP." : FailureForActor(name,result);
            if(result.Succeeded) MessageLog.Add(Notice);
            return result.Succeeded;
        }
        ActivatedAbility OwnedAbility()
        {
            if (Selected.State!=SkillsScreenRowState.Owned || Actor?.GetPart<SkillsPart>()?.HasSkill(Selected.Class)!=true) return null;
            var abilities=Actor.GetPart<ActivatedAbilitiesPart>();
            if(abilities!=null) foreach(var ability in abilities.AbilityList)
                if(ability.SourcePowerClass==Selected.Class) return ability;
            return null;
        }
        public string SlotName(int slot) => Actor?.GetPart<ActivatedAbilitiesPart>()?.GetAbilityBySlot(slot)?.DisplayName ?? "Empty";
        public int AssignedSlot => OwnedAbility() is ActivatedAbility ability ? Actor.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(ability.ID) : -1;
        public bool Assign(int slot)
        {
            var ability=OwnedAbility();
            if(ability==null || slot<0 || slot>=ActivatedAbilitiesPart.SlotCount) return false;
            Actor.GetPart<ActivatedAbilitiesPart>().AssignAbilityToSlot(ability.ID,slot);
            UpdateDetails();
            Notice="Assigned "+Selected.DisplayName+" to slot "+AbilityManagerStateBuilder.SlotToHotkey(slot)+".";
            return true;
        }
        void UpdateDetails()
        {
            Prerequisites.Clear(); Requirements=""; ActivationSummary="";
            if(Current==null) { Details="No skills available."; return; }
            Details=AbilityDetailsBuilder.BuildForSkill(Actor,Selected.Class) ?? "Details unavailable.";
            if(Selected.IsObfuscated) return;
            var spec=AbilityDetailsBuilder.Declaration(Selected.Class)?.DeclareActivatedAbility(Actor);
            ActivationSummary=spec==null?"PASSIVE": "ACTIVE / RANGE "+spec.Range+" / COOLDOWN "+spec.Cooldown;
            var owned=OwnedAbility();
            if(owned!=null) Details=AbilityDetailsBuilder.BuildForAbility(Actor,owned.ID);
            if(SkillRegistry.TryGetPowerByClass(Selected.Class,out var power))
            {
                if(!string.IsNullOrWhiteSpace(power.ParentSkillClass)) Prerequisites.Add(power.ParentSkillClass);
                if(!string.IsNullOrWhiteSpace(power.Requires)) foreach(var raw in power.Requires.Split(','))
                { var cls=raw.Trim(); if(cls.Length>0 && !Prerequisites.Contains(cls)) Prerequisites.Add(cls); }
                var lines=new StringBuilder();
                foreach(var cls in Prerequisites) lines.Append(Actor.GetPart<SkillsPart>()?.HasSkill(cls)==true ? "+ " : "- ").Append(SafeName(cls)).Append('\n');
                if(!string.IsNullOrWhiteSpace(power.Minimum))
                {
                    var attrs=power.Attribute.Split('|'); var mins=power.Minimum.Split('|');
                    for(int g=0;g<Math.Min(attrs.Length,mins.Length);g++)
                    {
                        if(g>0) lines.Append("OR\n");
                        var a=attrs[g].Split(',');var m=mins[g].Split(',');
                        for(int j=0;j<Math.Min(a.Length,m.Length);j++) lines.Append(a[j].Trim()).Append(' ').Append(Actor.GetStatValue(a[j].Trim())).Append(" / ").Append(m[j].Trim()).Append('\n');
                    }
                }
                Requirements=lines.ToString().TrimEnd();
            }
        }
        public string SafeName(string cls)
        {
            foreach(var group in Disciplines) foreach(var row in group.Rows) if(row.Class==cls) return row.DisplayName;
            return "Unknown requirement";
        }
        string FailureForActor(string name,BuySkillAction.Result r)
        {
            return r.Reason==BuySkillAction.FailureReason.MissingPrereq
                ? "You must learn "+AbilityDetailsBuilder.DisplayClassName(Actor,r.Detail)+" first." : Failure(name,r);
        }
        public void ExplainAssignmentUnavailable()
        { Notice=Selected.State==SkillsScreenRowState.Owned ? "This skill has no active ability to assign." : "Learn an active ability before assigning it."; }
        public static string Failure(string name, BuySkillAction.Result r)
        {
            switch(r.Reason)
            {
                case BuySkillAction.FailureReason.StaleReview: return "Purchase changed. Review the current rank and cost.";
                case BuySkillAction.FailureReason.AlreadyOwned: return "Already learned.";
                case BuySkillAction.FailureReason.InsufficientSP: return "Need "+r.Cost+" SP ("+r.SpBefore+" available).";
                case BuySkillAction.FailureReason.NotPurchasable: return "Learned through discovery; not purchased here.";
                case BuySkillAction.FailureReason.StatMinNotMet: return "Attribute requirement not met: "+r.Detail+".";
                case BuySkillAction.FailureReason.MissingPrereq:
                    string label=SkillRegistry.TryGetSkillByClass(r.Detail,out var root) ? root.Name
                        : SkillRegistry.TryGetPowerByClass(r.Detail,out var p) ? p.Name : "the parent skill tree";
                    return "You must learn "+label+" first.";
                case BuySkillAction.FailureReason.Exclusion: return "Conflicts with an already learned skill. See requirements.";
                default:return "Cannot learn this skill.";
            }
        }
    }
}
