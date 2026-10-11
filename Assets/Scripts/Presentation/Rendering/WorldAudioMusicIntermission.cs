using System;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>One bounded score rest after the currently selected calm composition ends.
    /// Other places and visible danger immediately cancel it; disabled audio does not spend it.</summary>
    public sealed class WorldAudioMusicIntermission
    {
        public const float Duration=12;
        string context;public float Remaining{get;private set;}
        public void Complete(string id)
        {if(string.IsNullOrWhiteSpace(id)||id=="danger")return;context=id;Remaining=Duration;}
        public bool WaitFor(string id)
        {if(id!=context||id=="danger"){Clear();return false;}return Remaining>0;}
        public void Advance(float delta,bool enabled)
        {if(enabled&&float.IsFinite(delta)&&delta>0)Remaining=Mathf.Max(0,Remaining-Mathf.Min(delta,30));}
        public void Clear(){context=null;Remaining=0;}
    }
}
