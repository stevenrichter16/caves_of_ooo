using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Short wall-clock gain ramp. Zero remains immediate; preferences are never rewritten.</summary>
    public sealed class WorldAudioGainEnvelope
    {
        public const float Duration=.35f;
        float target,start,age=Duration;public float Value{get;private set;}
        public WorldAudioGainEnvelope(float initial){Value=target=start=float.IsFinite(initial)?Mathf.Clamp01(initial):0;}
        public void Advance(float requested,float delta,bool enabled=true)
        {
            if(!float.IsFinite(requested))return;requested=Mathf.Clamp01(requested);
            if(requested<=0){Value=target=start=0;age=Duration;return;}
            if(requested!=target){target=requested;start=Value;age=0;}
            if(!enabled||!float.IsFinite(delta)||delta<=0)return;
            age=Mathf.Min(Duration,age+Mathf.Min(delta,30));Value=Mathf.Lerp(start,target,age/Duration);
        }
    }
}
