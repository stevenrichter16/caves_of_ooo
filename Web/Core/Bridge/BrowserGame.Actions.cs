using System;
using System.Linq;
using Newtonsoft.Json;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        bool faulted;
        sealed class Intent { public string kind,id,command,value,token; public string[] ids; public bool focused,acceptNeutralAttack; public int x,y,dx,dy,index,revision; }
        static readonly string[] FreeActions={"Examine","Chat","OpenContainer","Read","ReadDocument","ViewRecipe","PreviewRepair"};
        Entity Owned(string id) => player.GetPart<InventoryPart>()?.Objects.Concat(player.GetPart<InventoryPart>().GetAllEquipped()).FirstOrDefault(e=>e.ID==id);
        bool Adjacent(Entity entity)
        {
            if(entity==null||entity.GetPart<RenderPart>()?.Visible==false||!DestructionSystem.IsWithinStrikeReach(player,entity,zone))return false;
            foreach(var cell in zone.GetOccupiedCells(entity))if(cell.IsVisible)return true;
            return false;
        }
        Entity Near(string id) => zone.GetAllEntities().FirstOrDefault(e=>e.ID==id&&Adjacent(e));
        public string Dispatch(string requestJson)
        {
            if(!IsReady)throw new InvalidOperationException("Begin or continue an expedition first.");
            if(requestJson==null||requestJson.Length>8192){error="Invalid command.";return Snapshot();}
            Intent a;try{a=JsonConvert.DeserializeObject<Intent>(requestJson);}catch(JsonException){error="Invalid command.";return Snapshot();}
            if(a==null||a.revision!=revision){error="That action belonged to an older view.";RecordControl(a);return Snapshot();}
            error="";
            try
            {
                bool query=a.kind=="look"||a.kind=="panel"||a.kind=="close"||a.kind=="select"||a.kind=="throwPreview"||a.kind=="abilityPreview"||a.kind=="ritePreview"
                    ||(a.kind=="abilityManage"&&a.command=="details")||(a.kind=="equipment"&&a.command!="confirm");
                if(a.kind!="abilityConfirm")pendingAbility=null;
                if(a.kind!="equipment"||a.command!="confirm")pendingEquipment=null;
                if((Dead||faulted)&&!query){error=Dead?"This expedition has ended. Continue a checkpoint or begin again.":"Reload a checkpoint before taking another turn.";return Snapshot();}
                if(ConversationManager.IsActive&&a.kind!="choice"&&a.kind!="close"&&a.kind!="panel") {error="Finish the conversation first.";return Snapshot();}
                switch(a.kind)
                {
                    case "step": if(panel!=""){error="Close the current view first.";break;} Step(a.x,a.y);break;
                    case "wait": if(panel=="")EndTurn();break;
                    case "look": Look(a.x,a.y);break;
                    case "panel": if(new[]{"inventory","abilities","journal","character","nearby","map","skills","crafting"}.Contains(a.value)){panel=a.value;if(panel=="skills"){EnsureSkills();skillModel.Open(player);}}break;
                    case "close":
                        if(ConversationManager.IsActive&&ConversationManager.CurrentNode?.AllowEscape==false){error="Choose a reply to continue.";break;}
                        ConversationManager.EndConversation();pendingThrow=null;panel="";selected=trader=container=null;break;
                    case "select":selected=Owned(a.id);if(selected!=null)panel="item";else error="That item is not in your pack.";break;
                    case "item":Item(a);break;
                    case "skill":SkillAction(a);break;
                    case "craft":Craft(a);break;
                    case "pickup": Loot(a,false);break;
                    case "world":WorldAction(a);break;
                    case "container":Loot(a,true);break;
                    case "choice":
                        if(!ConversationManager.IsActive||a.index<0||a.index>=ConversationManager.VisibleChoices.Count){error="That reply is no longer available.";break;}
                        ConversationManager.SelectChoice(a.index);
                        if(ConversationManager.PendingTradePartner!=null){trader=ConversationManager.PendingTradePartner;ConversationManager.PendingTradePartner=null;ConversationManager.EndConversation();panel="trade";}
                        else if(ConversationManager.PendingAttackTarget!=null){var victim=ConversationManager.PendingAttackTarget;ConversationManager.PendingAttackTarget=null;ConversationManager.EndConversation();if(Adjacent(victim)){CombatSystem.PerformMeleeAttack(player,victim,zone,rng);EndTurn();}panel="";}
                        else panel=ConversationManager.IsActive?"conversation":"";break;
                    case "buy":case "sell":Trade(a);break;
                    case "ability":Ability(a);break;
                    case "abilityManage":ManageAbility(a);break;
                    case "abilityPreview":PreviewAbility(a);break;
                    case "abilityConfirm":ConfirmAbility(a);break;
                    case "ritePreview":ReadRite(a);break;
                    case "equipment":Equipment(a);break;
                    case "stairs":Stairs(a.value=="up");break;
                    case "travel":Travel(a.value);break;
                    case "throw":ThrowAction(a);break;
                    case "throwPreview":PreviewThrow(a);break;
                    case "recover":RecoverThrow(a);break;
                    default:error="That action is not available.";break;
                }
            }
            catch(ArgumentException e){error=e.Message;}
            catch(Exception e){faulted=true;error="The expedition paused after an unexpected error. Reload your checkpoint.";Console.Error.WriteLine(e);}
            finally {RecordControl(a);revision++;}
            return Snapshot();
        }
        void Step(int dx,int dy)
        {
            if(dx<-1||dx>1||dy<-1||dy>1||(dx==0&&dy==0)){error="Choose a single step in one direction.";return;}
            var start=zone.GetEntityCell(player);if(start==null)return;
            if(OnWorldMap)
            {
                var (wx,wy)=WorldMap.ZoneCellToWorldCell(start.X+dx,start.Y+dy);
                if(!zones.WorldMap.InBounds(wx,wy)){error="That is the edge of the world map.";return;}
            }
            var move=MovementSystem.TryMoveDetailed(player,zone,dx,dy);var blocked=move.BlockedBy;
            if(move.Moved||move.ActionPerformed){EndTurn();return;}
            if(blocked!=null&&blocked.HasTag("Creature")&&(FactionManager.IsHostile(player,blocked)||blocked.GetPart<SpreadTerritoryPart>()?.IsEnforcingAgainst(player,zone)==true))
            {CombatSystem.PerformMeleeAttack(player,blocked,zone,rng);EndTurn();return;}
            if(DestructionSystem.IsBreakable(blocked)){DestructionSystem.StrikeStructure(blocked,player,zone,rng);EndTurn();return;}
            if(blocked==null)
            {
                var dir=ZoneTransitionSystem.GetTransitionDirection(start.X,start.Y,dx,dy);
                if(dir.HasValue){var r=ZoneTransitionSystem.TransitionPlayer(player,zone,dir.Value,start.X,start.Y,zones,zones.WorldMap);if(r.Success){EnterZone(r);EndTurn();return;}}
            }
            if(player.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true){EndTurn();return;}
            if(blocked!=null&&blocked.HasTag("Creature")){lookX=start.X+dx;lookY=start.Y+dy;selected=blocked;panel="look";}
        }
        void Look(int x,int y)
        {
            var cell=zone.GetCell(x,y);
            if(cell==null||!cell.IsVisible){error="You cannot see that place.";return;}
            lookX=x;lookY=y;selected=WorldInteractionSystem.ResolveTarget(cell,player);panel="look";
        }
        void Item(Intent a)
        {
            var item=Owned(a.id);if(item==null){error="That item is not yours to use.";return;}
            var consumption=SelfConsumptionTurnProof.Capture(player,item,a.command);
            bool success=false;
            switch(a.command)
            {
                case "Drop":success=InventorySystem.Drop(player,item,zone);break;
                case "Equip":Equipment(new Intent{command="choose",id=item.ID});return;
                case "Unequip":success=InventorySystem.UnequipItem(player,item);break;
                default:
                    if(!InventorySystem.GetActions(player,item).Any(x=>x.Command==a.command)){error="That item cannot do that.";return;}
                    var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item,a.command),player,zone);
                    success=result.Success||result.ActionCommitted;if(!success)error=result.ErrorMessage;break;
            }
            if(success)
            {
                bool paid = new[]{"FillWaterskin","DrinkWaterskin","Cook","LightTorch","ExtinguishTorch","Harvest"}.Contains(a.command)
                    || consumption.ShouldSpendTurn(true) || (a.command==SoddenDressingPart.ApplyCommand&&item.HasPart<SoddenDressingPart>())
                    || a.command==GrimoireChargePart.ReinkCommand || a.command==BotanicalProcessingPart.Command
                    || LiquidVesselService.IsLiquidCommand(a.command)||WorldResourceActions.IsCommand(a.command)
                    || CombatUtilityActions.IsCommand(a.command)||PreparationActions.IsCommand(a.command)||SecondExplorationActions.IsCommand(a.command);
                panel="inventory";selected=null;if(paid)EndTurn();
            }
        }
        void WorldAction(Intent a)
        {
            var target=Near(a.id);if(target==null){error="Move beside it to interact.";return;}
            var action=WorldInteractionSystem.GatherActions(target,player).FirstOrDefault(x=>x.Command==a.command);
            if(action==null){error="That action is no longer available.";return;}
            if(a.command=="Chat")
            {if(ConversationManager.StartConversation(target,player))panel="conversation";return;}
            if(DispatchPaidWorld(target,a.command))return;
            var ev=GameEvent.New("InventoryAction");ev.SetParameter("Actor",(object)player);ev.SetParameter("Item",(object)target);ev.SetParameter("Zone",(object)zone);ev.SetParameter("Command",a.command);
            var receiver=action.FireOnActor?player:target;bool handled=!receiver.FireEvent(ev)||ev.Handled;ev.Release();
            if(a.command=="OpenContainer"&&handled){container=target;panel="container";return;}
            if(ConversationManager.IsActive){panel="conversation";return;}
            if(handled&&a.command=="Examine")panel="journal";
            if(!handled)error="You cannot do that right now.";
        }
        void Trade(Intent a)
        {
            if(trader==null||!Adjacent(trader)){error="The trader is out of reach.";return;}
            var item=a.kind=="buy"?TradeSystem.GetTraderStock(trader).FirstOrDefault(e=>e.ID==a.id):Owned(a.id);
            if(item==null){error="That item is no longer available.";return;}
            string reason;bool success=a.kind=="buy"?TradeSystem.BuyFromTrader(player,trader,item,out reason):TradeSystem.SellToTrader(player,trader,item,out reason);
            if(!success)error=reason; // The native trade menu does not advance turns.
        }
        void Ability(Intent a)
        {
            var ability=OwnedAbility(a.id);
            if(ability==null||!ability.IsUsable){error="That ability is not ready.";return;}
            if(IsThrowingAbility(ability))
            {
                if(!ThrowingRules.TryBeginSelection(player,ability.Command,out var request)){error="That throwing skill is not available.";return;}
                throwingMode=request.Kind.ToString();panel="throwing";return;
            }
            var plan=PlanAbility(a,true);if(plan==null)return;
            if(plan.Neutral){error="Review Vault and explicitly confirm attacking this non-hostile target.";return;}
            ExecuteAbility(plan);
        }
        void RecordControl(Intent a)
        {
            if(a==null||!new[]{"ability","abilityManage","abilityPreview","abilityConfirm","ritePreview","equipment"}.Contains(a.kind))return;
            CavesOfOoo.Diagnostics.Diag.Record("event",string.IsNullOrEmpty(error)?"BrowserActionRouted":"BrowserActionRejected",actor:player,
                payload:new{action=a.kind,command=a.command,ability=a.id,reason=error});
        }
        void ClearControlReviews(){pendingAbility=null;pendingEquipment=null;inspectedAbility=equipmentItem=riteReading=null;}
        void Stairs(bool up)
        {
            var here=zone.GetEntityCell(player);
            if(here==null)return;
            if(!(up?here.HasObjectWithPart<StairsUpPart>():here.HasObjectWithPart<StairsDownPart>()))
            {
                if(up&&CanEnterWorldMap()){Travel("world");return;}
                if(!up&&OnWorldMap){Travel("local");return;}
                error="Stand on the "+(up?"up":"down")+" stairs first.";return;
            }
            var r=ZoneTransitionSystem.TransitionPlayerVertical(player,zone,!up,here.X,here.Y,zones);
            if(r.Success){EnterZone(r);EndTurn();}else error="The way is blocked.";
        }
    }
}
