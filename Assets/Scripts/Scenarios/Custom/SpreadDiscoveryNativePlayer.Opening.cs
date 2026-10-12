using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using CavesOfOoo.Rendering;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _opening;
        string _openingApproach="bypass";
        bool OpeningReads=>_openingApproach!="unread";
        static readonly string[] OpeningChecks = { "ordinary_start", "opening_finite_cache", "opening_manual_learned", "opening_glade_vault", "opening_yard_observed", "opening_saved_return" };
        const string OpeningIntent = "Actual selected build, ordinary native cellar detour, original finite manual pickup/read, glade Vault, southern yard inspection or honest source refusal, return and F5/F6. No grants, actor transfers, AI suppression or resource edits.";
        const string OpeningLimits = "Script-guided route; not unaided discovery, a full combat strategy comparison, or Deck performance. A yard may be declined by source admission. Supplies inside may deliberately be left for a later expedition. Native routing can fail against live enemies; failures are retained.";
        public void InitializeOpening(ScenarioContext context, string build, string approach="bypass")
        { _opening = true; _openingApproach=approach; Initialize(context, connectedBuild: build); }
        IEnumerator OpeningChooseBuild()
        {
            var menu=(StartingBuildMenuController)Field(_input,"_buildMenuController");
            Require(menu.IsOpen,"real opening build picker");
            int index=menu.Model.Options.ToList().FindIndex(b=>b.Id==_connectedBuild);
            Require(index>=0&&index<9,"requested real build");
            yield return Capture("opening-00-build-picker");
            yield return Tap(Shortcut("Digit"+(index+1)));Require(menu.SelectedIndex==index,"native requested build selected");yield return Tap(Key.Enter);yield return ConnectedCloseAndStabilize();
            _connectedOriginalDaggerId=StartingBuildService.PrimaryHandWeapon(Player)?.ID;
            _variantWeaponId=_connectedOriginalDaggerId;
            _districtOriginalCalm=Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.SingleOrDefault(a=>a.Command=="CommandCalm")?.ID??Guid.Empty;
            _districtPreparing=true;
            Check("ordinary_start",Player.GetProperty(StartingBuildService.PropertyName)==_connectedBuild
                &&Zone.ZoneID==GleanersDistrict.SurfaceID&&Manager.Exploration.Version>=17
                &&Player.GetStatValue("Hitpoints")==40&&Packed("HealingTonic")==2
                &&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(Player));
        }
        IEnumerator OpeningJourney()
        {
            BeginTiming("opening-native-expedition");
            _notes.Add("Opening approach: "+_openingApproach);
            yield return Capture("opening-01-glade");
            if(_openingApproach=="yard-first")
            {
                yield return TravelSurface("Overworld.11.11.0");
                yield return Capture("opening-yard-before-manual");
                Require(!Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"),"yard visited before capability acquisition");
                yield return TravelSurface(GleanersDistrict.SurfaceID);
            }
            yield return DistrictWalk(Zone.GetCell(28,19),100);
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","opening-cellar-down");
            Require(Zone.ZoneID==GleanersCellarBuilder.ZoneID,"native cellar");
            bool north=(unchecked((uint)Manager.WorldSeed)&1u)==0;
            int storeX=62+(int)(unchecked((uint)Manager.WorldSeed)%3u)*2;
            int Y(int y)=>north?y:24-y;
            var crate=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"supplies");string crateId=crate.ID;
            if(_openingApproach=="melee"||_openingApproach=="supplies")
            {
                _guard=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"guard");_guardId=_guard.ID;
                _guardAttempt=_guardDamage=_guardLethal=false;
                yield return FightGuard(false);
            }
            if(_openingApproach=="withdrawal")
            {
                yield return DistrictWalk(Zone.GetCell(40,Y(5)),70);
                yield return DistrictWalk(Zone.GetCell(40,12),70);
                yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","opening-withdraw-to-surface");
                yield return TravelSurface(KitchenBatchPart.KitchenZoneID);
                var bed=Zone.GetReadOnlyEntities().First(e=>e.HasPart<BedPart>()&&string.IsNullOrEmpty(e.GetPart<BedPart>().Owner));
                yield return DistrictWalk(Zone.GetEntityCell(bed),140);
                int restTick=Tick,restHp=Player.GetStatValue("Hitpoints");
                yield return WorldAction(bed,PlayerBedService.RestCommand);yield return CloseNormal();
                _notes.Add("Withdrawal cot: HP "+restHp+" -> "+Player.GetStatValue("Hitpoints")+", clock "+restTick+" -> "+Tick+". A refused rest remains visible; no safety override.");
                yield return Capture("opening-withdrawn-with-supplies-preserved");
                yield return TravelSurface(GleanersDistrict.SurfaceID);
                yield return DistrictWalk(Zone.GetCell(28,19),100);
                yield return Paid(Tap(Key.LeftShift,Key.Period),"local","opening-return-after-withdrawal");
            }
            var contents=crate.GetPart<ContainerPart>().Contents.ToArray();
            var manual=contents.Single(e=>e.BlueprintName=="DitchkeepersFootwork");string manualId=manual.ID;
            foreach(var p in new[]{(40,5),(45,5),(46,4),(46,2),(50,2),(50,5),(storeX,5),(storeX-1,6)})
                yield return DistrictWalk(Zone.GetCell(p.Item1,Y(p.Item2)),90);
            yield return Paid(DistrictTakeCache(crate,contents),"local","opening-earned-cache");
            Check("opening_finite_cache",Owns(Player,manual)&&DistrictClay()==2&&crate.GetPart<ContainerPart>().Contents.Count==0);
            int sp=Player.GetStatValue("SP"),tick=Tick;
            if(OpeningReads){yield return ItemAction(manual,"ReadGrimoire");yield return CloseNormal();}
            Check("opening_manual_learned",Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")==OpeningReads&&Owns(Player,manual)&&Player.GetStatValue("SP")==sp&&Tick==tick);
            yield return Capture("opening-02-earned-manual");
            if(_openingApproach=="vault-strike")yield return OpeningVaultStrike();
            foreach(var p in new[]{(storeX,5),(50,5),(50,2),(46,2),(46,4),(45,5),(40,5),(40,12)})
                yield return DistrictWalk(Zone.GetCell(p.Item1,Y(p.Item2)),90);
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","opening-cellar-up");
            Require(Zone.ZoneID==GleanersDistrict.SurfaceID,"native glade return");
            yield return DistrictWalk(Zone.GetCell(27,17),100);
            yield return Capture("opening-03-visible-crossing");
            if(OpeningReads&&Player.GetPart<ActivatedAbilitiesPart>().AbilityList.Any(a=>a.Command=="CommandVault"&&a.CooldownRemaining==0))yield return OpeningVault(1,0);
            else foreach(var p in new[]{(27,16),(28,16),(29,16),(29,17)})yield return DistrictWalk(Zone.GetCell(p.Item1,p.Item2),30);
            Check("opening_glade_vault",At.X==29&&At.Y==17&&Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")==OpeningReads);
            yield return Capture("opening-04-crossed");
            yield return TravelSurface("Overworld.11.11.0");
            var crossing=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(SpreadExplorationWorksites.RoleKey)=="crossing");
            if(crossing!=null)
            {
                yield return DistrictApproach(crossing,140);
                yield return WorldAction(crossing,"Examine");yield return ReadPages("opening-05-yard-crossing");yield return CloseNormal();
                var cp=Zone.GetEntityPosition(crossing);
                var cache=Zone.GetReadOnlyEntities().Single(e=>e.GetProperty(SpreadExplorationWorksites.RoleKey)==null&&e.HasPart<ContainerPart>()&&Math.Abs(Zone.GetEntityPosition(e).x-cp.x)+Math.Abs(Zone.GetEntityPosition(e).y-cp.y)==8);
                var center=Zone.GetEntityPosition(cache);
                int dx=Math.Abs(center.x-cp.x)==5?Math.Sign(center.x-cp.x):0;
                int dy=dx==0?Math.Sign(center.y-cp.y):0;
                yield return DistrictWalk(Zone.GetCell(cp.x-dx,cp.y-dy),140);
                bool ready=OpeningReads&&Player.GetPart<ActivatedAbilitiesPart>().AbilityList.Any(a=>a.Command=="CommandVault"&&a.CooldownRemaining==0);
                if(ready)yield return OpeningVault(dx,dy);
                else { _notes.Add("Yard crossing walked around: manual unread or Vault still cooling down after the glade. No clock padding or cooldown edit.");yield return DistrictWalk(Zone.GetCell(cp.x+dx,cp.y+dy),140); }
                Require(At.X==cp.x+dx&&At.Y==cp.y+dy,"actual yard crossing or walking alternative");
                yield return Capture("opening-05b-yard-crossed");
                _notes.Add("Yard reached through ordinary travel; its original stock deliberately left for a later expedition. Cache contents are not an observed pickup.");
            }
            else { _notes.Add("Actual yard source admission declined; no site or salvage fabricated.");yield return Capture("opening-05-no-admitted-yard"); }
            Check("opening_yard_observed",Zone.ZoneID=="Overworld.11.11.0"&&Manager.Exploration.DispositionFor(Zone.ZoneID)>0);
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            var oldPlayer=Player;yield return Tap(Key.F5);yield return Reload(oldPlayer);
            var savedCrate=Manager.CachedZones[GleanersCellarBuilder.ZoneID].GetReadOnlyEntities().Single(e=>e.ID==crateId);
            Check("opening_saved_return",Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")==OpeningReads&&CountGraphId(manualId)==1
                &&savedCrate.GetPart<ContainerPart>().Contents.Count==0&&Zone.ZoneID==GleanersDistrict.SurfaceID);
            yield return Capture("opening-06-restored-return");
            EndTiming();
        }
        IEnumerator OpeningVaultStrike()
        {
            var guard = DistrictOwner(Zone, GleanersCellarBuilder.RoleKey, "guard");
            Require(guard != null && guard.GetStatValue("Hitpoints") > 0, "live original cellar guard");
            yield return DistrictReach(c => SpatialQuery.DistanceToCell(Zone, guard, c.X, c.Y) == 2, 100);
            var selected = Zone.GetEntityCell(guard);
            Cell landing = null;
            Require(selected.IsVisible && VaultTargeting.TryPlan(Player, Zone, selected.X, selected.Y,
                out landing, out var target, out _ ) && target == guard, "native two-cell attack has clear landing");
            int lx = landing.X, ly = landing.Y, beforeHp = guard.GetStatValue("Hitpoints");
            var abilities = Player.GetPart<ActivatedAbilitiesPart>();
            var vault = abilities.AbilityList.Single(a => a.Command == "CommandVault");
            int slot = Array.FindIndex(abilities.SlotAssignments, id => id == vault.ID);
            Require(slot >= 0 && vault.CooldownRemaining == 0, "earned Vault strike ready");
            int dx = selected.X - At.X, dy = selected.Y - At.Y;
            yield return Tap(Shortcut(slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
            for (int i = 0; i < Math.Max(Math.Abs(dx), Math.Abs(dy)); i++)
                yield return Tap(Direction(i < Math.Abs(dx) ? Math.Sign(dx) : 0, i < Math.Abs(dy) ? Math.Sign(dy) : 0));
            yield return Capture("opening-vault-red-target-yellow-landing");
            yield return Paid(Tap(Key.Enter), "local", "opening-vault-strike");
            Require(At.X == lx && At.Y == ly && vault.CooldownRemaining > 0, "native strike lands at preview and pays cooldown");
            _notes.Add("Vault strike against original guard: HP " + beforeHp + " -> " + guard.GetStatValue("Hitpoints") + "; ordinary hit/miss rules retained.");
            yield return Capture("opening-vault-after-strike");
        }

        IEnumerator OpeningVault(int dx,int dy)
        {
            var abilities=Player.GetPart<ActivatedAbilitiesPart>();var vault=abilities.AbilityList.Single(a=>a.Command=="CommandVault");
            int slot=Array.FindIndex(abilities.SlotAssignments,id=>id==vault.ID);
            Require(slot>=0&&slot<10&&vault.CooldownRemaining==0,"earned Vault ready in native slot");
            yield return Tap(Shortcut(slot==9?"Digit0":"Digit"+(slot+1)));
            Require(State=="AwaitingDirection","native Vault direction");
            yield return Tap(Direction(dx,dy));yield return Tap(Direction(dx,dy));
            yield return Capture("opening-vault-cursor-preview");
            yield return Paid(Tap(Key.Enter),"local","opening-vault-crossing");
            Require(vault.CooldownRemaining>0,"actual Vault paid cooldown");
        }
    }
}
