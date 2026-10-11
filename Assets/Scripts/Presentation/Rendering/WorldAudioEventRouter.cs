using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Additive, disposable presentation listeners. Only current visible owners produce world cues.
    /// Generic damage is deliberately not a sound trigger: approved spell audio owns its impacts.</summary>
    public sealed class WorldAudioEventRouter:IDisposable
    {
        readonly WorldAudioPlayer audio;Zone zone;Entity player;bool visible,disposed;
        static readonly HashSet<string> StoneFloors=new HashSet<string>(StringComparer.Ordinal)
        {"StoneFloor","SandstoneFloor","LimestoneFloor","ShaleFloor","SlateFloor","QuartziteFloor","ObsidianFloor","RoadStone","TepuiStone"};
        public int AcceptedPlayerSteps{get;private set;}
        public WorldAudioEventRouter(WorldAudioPlayer playerAudio)
        {
            audio=playerAudio??throw new ArgumentNullException(nameof(playerAudio));
            EntityVisualHooks.MovedCallback+=Moved;WorldAudioCombat.Callback+=Combat;
            EntityVisualHooks.SelfUseCallback+=SelfUse;WorldAudioHooks.Callback+=Semantic;
        }
        public void SetWorld(Zone current,Entity listener,bool worldVisible)
        {
            if(disposed)return;
            if(!ReferenceEquals(zone,current)||!ReferenceEquals(player,listener))audio.ClearSounds();
            zone=current;player=listener;visible=worldVisible;
        }
        bool Current(Entity actor,Zone at,bool requireVisible,out Cell cell)
        {
            cell=null;if(disposed||actor==null||zone==null||at!=zone||player==null||zone.GetEntityCell(player)==null)return false;
            cell=zone.GetEntityCell(actor);if(cell==null||!cell.Objects.Contains(actor))return false;
            var physical=actor.GetPart<PhysicsPart>();if(physical==null||physical.ParentEntity!=actor||physical.InInventory!=null||physical.Equipped!=null)return false;
            if(!requireVisible)return true;
            var render=actor.GetPart<RenderPart>();return visible&&cell.Explored&&cell.IsVisible&&render?.Visible==true;
        }
        float Pan(int x)
        {var origin=zone?.GetEntityCell(player);return origin==null?0:(x-origin.X)/12f;}
        void Moved(Entity actor,Zone at,int ox,int oy,int nx,int ny,bool forced)
        {
            try
            {
                if(forced||(ox==nx&&oy==ny)||!Current(actor,at,true,out var cell)||cell.X!=nx||cell.Y!=ny)return;
                // Player locomotion is useful feedback; a whole settlement's idle patrols are not a footstep chorus.
                if(actor!=player)return;
                var cue=Footstep(cell);if(audio.TryPlay(cue,Pan(nx)))AcceptedPlayerSteps++;
            }
            catch(Exception){ }
        }
        static WorldAudioCue Footstep(Cell cell)
        {
            bool stone=false;
            for(int i=0;i<cell.Occupants.Count;i++)
            {
                var owner=cell.Occupants[i];if(owner==null)continue;
                if(owner.GetPart<LiquidPoolPart>()!=null)return WorldAudioCue.StepWater;
                if(!owner.HasTag("Terrain"))continue;
                var material=owner.GetPart<MaterialPart>();
                if(StoneFloors.Contains(owner.BlueprintName??"")||material?.MaterialID=="Stone"||material?.MaterialID=="Masonry"
                    ||material?.MaterialID=="Glass"||material?.HasMaterialTag("Metal")==true||owner.HasTag("Wall"))stone=true;
            }
            return stone?WorldAudioCue.StepStone:WorldAudioCue.StepSoft;
        }
        void Combat(WorldAudioCombatReceipt receipt)
        {
            try
            {
                var attempt=receipt.Attempt;
                if(!attempt.IsValid||!attempt.TargetWasVisible||attempt.Zone!=zone
                    ||!Current(attempt.Attacker,attempt.Zone,true,out _)||CombatSystem.IsDeathHandled(attempt.Attacker)
                    ||attempt.Attacker.GetStatValue("Hitpoints",0)<=0
                    ||!Current(attempt.Defender,attempt.Zone,true,out var contact)
                    ||contact.X!=attempt.Contact.X||contact.Y!=attempt.Contact.Y)return;
                // This callback is at the real HP decrement, before death removal.
                // A dead/removed-owner fallback would incorrectly bless a lethal
                // callback that first moved or hid the target.
                audio.TryPlay(receipt.Cue,Pan(contact.X));
            }
            catch(Exception){ }
        }
        static bool CombatCue(WorldAudioCue cue)
        {
            switch(cue)
            {
                case WorldAudioCue.Swing:case WorldAudioCue.Hit:
                case WorldAudioCue.SwingBlade:case WorldAudioCue.SwingBlunt:case WorldAudioCue.SwingNatural:
                case WorldAudioCue.HitCutting:case WorldAudioCue.HitPiercing:case WorldAudioCue.HitBlunt:case WorldAudioCue.HitNatural:return true;
                default:return false;
            }
        }
        void SelfUse(Entity actor,Zone at)
        {try{if(Current(actor,at,true,out var cell))audio.TryPlay(WorldAudioCue.Use,Pan(cell.X));}catch(Exception){ }}
        void Semantic(WorldAudioCue cue,Entity actor,Zone at,int x,int y)
        {
            try
            {
                if(CombatCue(cue))return; // Weapon sounds require the actual typed strike receipt.
                bool localInventory=cue==WorldAudioCue.Pickup||cue==WorldAudioCue.Equip||cue==WorldAudioCue.Use;
                if(localInventory&&actor!=player)return;
                if(!Current(actor,at,!localInventory,out _))return;
                var cell=zone.GetCell(x,y);if(cell==null||(!localInventory&&(!cell.Explored||!cell.IsVisible)))return;
                audio.TryPlay(cue,localInventory?0:Pan(x));
            }
            catch(Exception){ }
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;EntityVisualHooks.MovedCallback-=Moved;WorldAudioCombat.Callback-=Combat;
            EntityVisualHooks.SelfUseCallback-=SelfUse;WorldAudioHooks.Callback-=Semantic;zone=null;player=null;
        }
    }
}
