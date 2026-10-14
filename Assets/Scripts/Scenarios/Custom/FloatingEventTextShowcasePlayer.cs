using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Scenario-only repetition clock; never serialized as gameplay state.</summary>
    public sealed class FloatingEventTextShowcasePlayer : MonoBehaviour
    {
        Zone zone; Entity defender,attacker,patient;
        float remaining=.8f;
        public bool Paused;
        public int SuccessfulPasses {get;private set;}
        public void Initialize(Zone at,Entity player,Entity opponent,Entity statusTarget)
        { zone=at;defender=player;attacker=opponent;patient=statusTarget; }
        public bool ShowNow()
        {
            bool success=FloatingEventTextShowcase.Resolve(zone,defender,attacker,patient);
            if(success)SuccessfulPasses++;
            else { Paused=true;Debug.LogWarning("Floating text showcase paused: actors are no longer in their prepared state."); }
            remaining=3f;return success;
        }
        void Update()
        {
            if(Paused || zone==null)return;
            remaining-=Time.unscaledDeltaTime;
            if(remaining<=0)ShowNow();
        }
    }
}
