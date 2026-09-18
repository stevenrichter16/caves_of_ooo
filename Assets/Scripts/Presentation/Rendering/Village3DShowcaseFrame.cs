using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Standalone art camera only. Preserve the 37.5 by 25.5 art view
    /// with a centered viewport, without stretching at other Game-view sizes.</summary>
    [ExecuteAlways,RequireComponent(typeof(Camera))]
    public sealed class Village3DShowcaseFrame : MonoBehaviour
    {
        public const float Width=37.5f,Height=25.5f;
        Camera ownedCamera;
        int previousWidth=-1,previousHeight=-1;
        public static Rect CalculateViewport(int width,int height)
        {
            if(width<=0||height<=0)throw new ArgumentOutOfRangeException(nameof(width));
            float outputAspect=(float)width/height,targetAspect=Width/Height;
            if(outputAspect>targetAspect){float w=targetAspect/outputAspect;return new Rect((1-w)*.5f,0,w,1);}
            float h=outputAspect/targetAspect;return new Rect(0,(1-h)*.5f,1,h);
        }
        void OnEnable(){ownedCamera=GetComponent<Camera>();previousWidth=previousHeight=-1;RefreshFrame();}
        void Update()=>RefreshFrame();
        void RefreshFrame()
        {
            if(ownedCamera==null)ownedCamera=GetComponent<Camera>();
            var target=ownedCamera.targetTexture;
            int width=target!=null?target.width:Screen.width,height=target!=null?target.height:Screen.height;
            if(width>0&&height>0&&(width!=previousWidth||height!=previousHeight))ApplyViewport(width,height);
        }
        /// <summary>Explicit dimensions also support an owned RenderTexture capture.
        /// Only this component's camera is modified; no global screen or camera state.</summary>
        public void ApplyViewport(int width,int height)
        {
            Rect view=CalculateViewport(width,height);
            if(ownedCamera==null)ownedCamera=GetComponent<Camera>();
            ownedCamera.orthographic=true;ownedCamera.orthographicSize=Height*.5f;
            ownedCamera.rect=view;ownedCamera.ResetAspect();previousWidth=width;previousHeight=height;
        }
    }
}
