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
        int _openingExpectedSeed;
        string _openingApproach="bypass";
        bool OpeningBootstrap=>_openingApproach=="bootstrap";
        bool OpeningReads=>_openingApproach!="unread";
        bool OpeningProgression=>_openingApproach=="progression";
        static readonly string[] BootstrapOpeningChecks = { "ordinary_start", "bootstrap_starting_stats", "bootstrap_starting_inventory", "bootstrap_starting_skills_hotbar", "bootstrap_free_selection" };
        static readonly string[] BaseOpeningChecks = { "ordinary_start", "opening_finite_cache", "opening_manual_learned", "opening_glade_vault", "opening_yard_observed", "opening_saved_return" };
        string[] OpeningChecks=>OpeningBootstrap?BootstrapOpeningChecks:(_connectedBuild=="warden"?BaseOpeningChecks.Concat(OpeningReads
            ?new[]{"opening_warden_equipment","opening_earned_vault_assigned"}
            :new[]{"opening_warden_equipment"}):BaseOpeningChecks).Concat(OpeningProgression
                ?new[]{"opening_finite_progression","opening_earned_deflect_purchase","opening_earned_defense_fight"}:Array.Empty<string>()).ToArray();
        const string OpeningIntent = "Actual selected build, ordinary native cellar detour, original finite manual pickup/read, glade Vault, southern yard inspection or honest source refusal, return and F5/F6. No grants, actor transfers, AI suppression or resource edits.";
        const string OpeningLimits = "Script-guided route; not unaided discovery, a full combat strategy comparison, or Deck performance. A yard may be declined by source admission. Supplies inside may deliberately be left for a later expedition. Native routing can fail against live enemies; failures are retained.";
        string OpeningAuditIntent=>OpeningBootstrap
            ?"Actual launcher seed and native build-picker selection; verify ordinary starting stats, finite pack and equipped ownership, skill ranks and ready hotbar. No grants or world mutations by the evaluator."
            :OpeningIntent;
        string OpeningAuditLimits=>OpeningBootstrap
            ?"Bootstrap only: no expedition, optional-site admission, earned progression, combat, save/reload, performance or physical Deck claim. Starting data is compared with its authored source; content balance remains separately tested."
            :OpeningLimits;
        public void InitializeOpening(ScenarioContext context, string build, int expectedSeed, string approach="bypass")
        { _opening = true; _openingExpectedSeed=expectedSeed; _openingApproach=approach; Initialize(context, connectedBuild: build); }
        IEnumerator OpeningChooseBuild()
        {
            var menu=(StartingBuildMenuController)Field(_input,"_buildMenuController");
            Require(menu.IsOpen,"real opening build picker");
            int index=menu.Model.Options.ToList().FindIndex(b=>b.Id==_connectedBuild);
            Require(index>=0&&index<9,"requested real build");
            var build=menu.Model.Options[index];
            bool bootMenuUsed=_keys.Any(row=>(string)Newtonsoft.Json.Linq.JObject.FromObject(row)["keys"]==nameof(Key.N));
            int selectionTick=Tick,selectionEnergy=Energy,selectionWorld=WorldClock.CurrentTick;
            bool freshSkills=Player.GetPart<SkillsPart>().SkillList.Count==0&&Player.GetPart<ActivatedAbilitiesPart>().AbilityList.Count==0;
            yield return Tap(Shortcut("Digit"+(index+1)));Require(menu.SelectedIndex==index,"native requested build selected");
            _observations.Add(new{phase="opening_selected_picker",requestedBuild=_connectedBuild,selectedBuild=menu.Model.Options[menu.SelectedIndex].Id,
                expectedSeed=_openingExpectedSeed,actualSeed=Manager.WorldSeed,entry=bootMenuUsed?"boot-menu N":"direct fresh-game picker",selectionTick,selectionEnergy,selectionWorld});
            yield return Capture("opening-00-build-picker");
            yield return Tap(Key.Enter);yield return ConnectedCloseAndStabilize();
            _connectedOriginalDaggerId=StartingBuildService.PrimaryHandWeapon(Player)?.ID;
            _variantWeaponId=_connectedOriginalDaggerId;
            _districtOriginalCalm=Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.SingleOrDefault(a=>a.Command=="CommandCalm")?.ID??Guid.Empty;
            _districtPreparing=true;
            Check("ordinary_start",Player.GetProperty(StartingBuildService.PropertyName)==_connectedBuild
                &&_openingExpectedSeed!=0&&Manager.WorldSeed==_openingExpectedSeed
                &&Zone.ZoneID==GleanersDistrict.SurfaceID&&Manager.Exploration.Version>=17
                &&Player.GetStatValue("Hitpoints")==40&&Packed("HealingTonic")==2
                &&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(Player));
            if(OpeningBootstrap)
            {
                OpeningCheckStartingKit(build);
                Check("bootstrap_free_selection",freshSkills&&!menu.IsOpen&&State=="Normal"
                    &&Tick==selectionTick&&Energy==selectionEnergy&&WorldClock.CurrentTick==selectionWorld);
            }
        }
        void OpeningCheckStartingKit(StartingBuildDef build)
        {
            var inventory=Player.GetPart<InventoryPart>();
            var equipped=inventory.EquippedItems.Values.Distinct().ToArray();
            var owned=inventory.Objects.Concat(equipped).Distinct().ToArray();
            var skills=Player.GetPart<SkillsPart>();
            var abilities=Player.GetPart<ActivatedAbilitiesPart>();
            var expectedSkills=build.IsClassic?StartingSpellKit.SpellClasses:build.Skills.ToArray();
            var expectedItems=build.IsClassic
                ?NewGameLoadout.Items.Select(i=>new StartingBuildItem{Blueprint=i.Blueprint,Count=i.Count,Equip=i.Blueprint=="Dagger"}).ToList()
                :build.Items.ToList();
            // This common kit is granted by GameBootstrap before the picker, not by a build.
            expectedItems.AddRange(new[]{new StartingBuildItem{Blueprint="WateringGrimoire",Count=1},
                new StartingBuildItem{Blueprint="CandyCarrotSeed",Count=4},new StartingBuildItem{Blueprint="EmberwheatSeed",Count=2}});
            var names=new[]{"Strength","Agility","Toughness","Ego"};
            var expectedAttributes=build.IsClassic
                ?names.Select(name=>_input.EntityFactory.Blueprints["Player"].Stats[name].Value).ToArray()
                :new[]{build.Attributes.Strength,build.Attributes.Agility,build.Attributes.Toughness,build.Attributes.Ego};
            string primary=expectedItems.FirstOrDefault(i=>i.Equip&&_input.EntityFactory.Blueprints[i.Blueprint].Parts.ContainsKey("MeleeWeapon"))?.Blueprint;
            var active=skills.SkillList.Select(skill=>new{skill,spec=skill.DeclareActivatedAbility(Player)}).Where(row=>row.spec!=null&&!string.IsNullOrEmpty(row.spec.Command)).ToArray();
            _observations.Add(new{phase="bootstrap_actual_start",requestedBuild=_connectedBuild,actualBuild=Player.GetProperty(StartingBuildService.PropertyName),
                expectedSeed=_openingExpectedSeed,actualSeed=Manager.WorldSeed,player=Player.ID,zone=Zone.ZoneID,x=At.X,y=At.Y,stats=Stats(Player),gear=Gear(Player),
                tick=Tick,energy=Energy,worldTick=WorldClock.CurrentTick,drams=TradeSystem.GetDrams(Player),expectedAttributes,expectedSkills,expectedItems,
                primary=StartingBuildService.PrimaryHandWeapon(Player)?.BlueprintName,
                skills=skills.SkillList.Select(s=>new{className=s.GetType().Name,s.Rank,s.ActivatedAbilityID}).ToArray(),
                abilities=abilities.AbilityList.Select(a=>new{a.ID,a.Command,a.SourcePowerClass,a.CooldownRemaining,a.MaxCooldown}).ToArray(),
                hotbar=Enumerable.Range(0,ActivatedAbilitiesPart.SlotCount).Select(slot=>new{slot,id=abilities.SlotAssignments[slot],command=abilities.GetAbilityBySlot(slot)?.Command}).ToArray()});
            Check("bootstrap_starting_stats",names.Select(name=>Player.GetStat(name).BaseValue).SequenceEqual(expectedAttributes)
                &&Player.GetStatValue("Hitpoints")==40&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Level")==1
                &&Player.GetStatValue("Experience")==0&&Player.GetStatValue("SP")==0&&TradeSystem.GetDrams(Player)==50);
            var actualCounts=owned.GroupBy(e=>e.BlueprintName).Select(g=>g.Key+":"+g.Sum(Units)).OrderBy(s=>s).ToArray();
            var expectedCounts=expectedItems.GroupBy(i=>i.Blueprint).Select(g=>g.Key+":"+g.Sum(i=>i.EffectiveCount)).OrderBy(s=>s).ToArray();
            Check("bootstrap_starting_inventory",actualCounts.SequenceEqual(expectedCounts)&&owned.All(e=>Owns(Player,e))
                &&owned.Select(e=>e.ID).Distinct().Count()==owned.Length&&!inventory.Objects.Intersect(equipped).Any()
                &&equipped.Select(e=>e.BlueprintName).OrderBy(s=>s).SequenceEqual(expectedItems.Where(i=>i.Equip).Select(i=>i.Blueprint).OrderBy(s=>s))
                &&Player.GetPart<Body>().GetParts().Where(p=>p.Equipped!=null).Select(p=>p.Equipped).Distinct().OrderBy(e=>e.ID).SequenceEqual(equipped.OrderBy(e=>e.ID))
                &&StartingBuildService.PrimaryHandWeapon(Player)?.BlueprintName==primary);
            Check("bootstrap_starting_skills_hotbar",skills.SkillList.Select(s=>s.GetType().Name).SequenceEqual(expectedSkills)
                &&skills.SkillList.All(s=>s.Rank==1)&&abilities.AbilityList.Count==active.Length&&abilities.AbilityByGuid.Count==active.Length
                &&active.Select((row,slot)=>row.skill.ActivatedAbilityID!=Guid.Empty&&abilities.GetAbilityBySlot(slot)?.ID==row.skill.ActivatedAbilityID
                    &&abilities.GetAbilityBySlot(slot)?.Command==row.spec.Command&&abilities.GetAbilityBySlot(slot)?.SourcePowerClass==row.skill.GetType().Name
                    &&abilities.GetAbilityBySlot(slot)?.CooldownRemaining==0).All(matches=>matches)
                &&abilities.SlotAssignments.Skip(active.Length).All(id=>id==Guid.Empty));
        }
        IEnumerator OpeningJourney()
        {
            if(OpeningBootstrap)
            {
                _notes.Add("Bootstrap-only run ended after the real selection and starting-state checks; no expedition or optional-site admission was exercised.");
                yield return Capture("opening-01-bootstrap-glade");
                yield break;
            }
            BeginTiming("opening-native-expedition");
            _notes.Add("Opening approach: "+_openingApproach);
            yield return Capture("opening-01-glade");
            if(_connectedBuild=="warden")
            {
                Check("opening_warden_equipment",StartingBuildService.PrimaryHandWeapon(Player)?.BlueprintName=="LongSword"&&BladeDefense.FindLongBlade(Player)!=null
                    &&Player.GetPart<SkillsPart>().HasSkill("LongBlades_EnGarde")&&!Player.GetPart<SkillsPart>().HasSkill("LongBlades_Deflect")&&!Player.GetPart<SkillsPart>().HasSkill("LongBlades_Riposte"));
                yield return OpeningInspectSkill("LongBlades_Deflect",false);
            }
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
            if(_connectedBuild=="warden"&&OpeningReads)
            {
                yield return OpeningInspectSkill("Acrobatics_Vault",true);
                Check("opening_earned_vault_assigned",Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9)?.Command=="CommandVault"
                    &&!Player.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill")&&Player.GetStatValue("SP")==sp);
            }
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
            if(OpeningProgression)yield return OpeningEarnedPurchaseJourney();
            var oldPlayer=Player;string id=Player.ID,stats=Stats(Player),gear=Gear(Player),skills=OpeningSkillState();
            string progression=OpeningProgressionDigest();
            int savedX=At.X,savedY=At.Y,savedTick=Tick,savedEnergy=Energy,savedWorld=WorldClock.CurrentTick;
            string file=SaveFile(),oldHash=HashFile(file);yield return Tap(Key.F5);yield return Settled();
            Require(HashFile(file)!=oldHash&&MessageLog.GetLast()=="Game saved.","new successful opening checkpoint");string savedHash=HashFile(file);
            var step=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>c!=null&&Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));
            Require(step!=null,"ordinary unsaved step");yield return StepTo(step.X,step.Y);
            Require(Tick>savedTick&&HashFile(file)==savedHash,"unsaved movement without replacing Quick checkpoint");yield return Reload(oldPlayer);
            Require(Player.ID==id&&At.X==savedX&&At.Y==savedY&&Stats(Player)==stats&&Gear(Player)==gear&&OpeningSkillState()==skills
                &&Tick==savedTick&&Energy==savedEnergy&&WorldClock.CurrentTick==savedWorld&&HashFile(file)==savedHash
                &&OpeningProgressionDigest()==progression,"exact current opening state restored");
            var savedCrate=Manager.CachedZones[GleanersCellarBuilder.ZoneID].GetReadOnlyEntities().Single(e=>e.ID==crateId);
            Check("opening_saved_return",Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")==OpeningReads&&CountGraphId(manualId)==1
                &&savedCrate.GetPart<ContainerPart>().Contents.Count==0&&Zone.ZoneID==GleanersDistrict.SurfaceID);
            yield return Capture("opening-06-restored-return");
            EndTiming();
        }
        string OpeningSkillState()=>string.Join("|",Player.GetPart<SkillsPart>().SkillList.Select(s=>s.GetType().Name+":"+s.Rank))
            +"/"+string.Join("|",Player.GetPart<ActivatedAbilitiesPart>().SlotAssignments)
            +"/"+string.Join("|",Player.GetPart<ActivatedAbilitiesPart>().AbilityList.Select(a=>a.ID+":"+a.CooldownRemaining))
            +"/"+(Player.GetPart<RiposteAllowancePart>()?.Spent??false);
        IEnumerator OpeningInspectSkill(string name,bool assign,bool purchase=false)
        {
            int tick=Tick,energy=Energy,sp=Player.GetStatValue("SP");yield return Tap(Key.X);var ui=_input.SkillsScreenUI;
            Require(State=="SkillsScreenOpen"&&ui.IsOpen,"ordinary Skills screen");
            int discipline=ui.Model.Disciplines.FindIndex(d=>d.Rows.Any(r=>r.Class==name));Require(discipline>=0,"real visible skill row");
            for(int n=0;ui.Model.DisciplineIndex!=discipline;n++){Require(n<20,"bounded skill discipline navigation");yield return Tap(ui.Model.DisciplineIndex<discipline?Key.E:Key.Q);}
            int index=ui.Model.Current.Rows.FindIndex(r=>r.Class==name);
            for(int n=0;ui.Model.SkillIndex!=index;n++){Require(n<50,"bounded skill navigation");yield return Tap(ui.Model.SkillIndex<index?Key.DownArrow:Key.UpArrow);}
            yield return Capture("opening-skill-"+name);
            if(purchase)
            {
                Require(sp>=1&&!Player.GetPart<SkillsPart>().HasSkill(name),"earned point and unowned skill");
                var slots=Player.GetPart<ActivatedAbilitiesPart>().SlotAssignments.ToArray();
                yield return Tap(Key.Enter);Require(ui.IsReviewingPurchase,"ordinary earned purchase review");
                yield return Capture("opening-earned-purchase-review");
                yield return Tap(Key.Enter);Require(!ui.IsReviewingPurchase&&!Player.GetPart<SkillsPart>().HasSkill(name)&&Player.GetStatValue("SP")==sp,"default cancel preserves earned point");
                yield return Tap(Key.Enter);yield return Tap(Key.RightArrow);yield return Tap(Key.Enter);
                Require(Player.GetPart<SkillsPart>().SkillList.Single(s=>s.GetType().Name==name).Rank==1&&Player.GetStatValue("SP")==sp-1,"native one-point purchase");
                yield return Tap(Key.Enter);
                Require(!ui.IsReviewingPurchase&&Player.GetStatValue("SP")==sp-1&&Player.GetPart<ActivatedAbilitiesPart>().SlotAssignments.SequenceEqual(slots),"repeat confirmation cannot spend absent second point or change slots");
                yield return Capture("opening-earned-deflect-owned");
            }
            else if(assign){yield return Tap(Key.I);Require(ui.IsAssigning,"native assignment review");for(int n=0;n<9;n++)yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);}
            else{yield return Tap(Key.Enter);Require(!ui.IsReviewingPurchase&&Player.GetStatValue("SP")==sp,"no unearned starting point");}
            yield return Tap(Key.Escape);Require(State=="Normal"&&Tick==tick&&Energy==energy&&Player.GetStatValue("SP")==sp-(purchase?1:0),"menu preserves time and only confirmed purchase spends SP");
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
