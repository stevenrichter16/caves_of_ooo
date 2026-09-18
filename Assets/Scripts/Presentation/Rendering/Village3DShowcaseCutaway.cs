using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Rendering
{
    /// <summary>Standalone art-scene control. Toggles only owned descendant art
    /// objects; it never calls gameplay roof, visibility, entity or save APIs.</summary>
    [ExecuteAlways]
    public sealed class Village3DShowcaseCutaway : MonoBehaviour
    {
        [SerializeField] GameObject[] roofs=Array.Empty<GameObject>();
        [SerializeField] GameObject[] interiors=Array.Empty<GameObject>();
        [SerializeField] bool cutaway;
        public bool IsCutaway=>cutaway;

        /// <summary>Copy an owned scene's roof/interior groups and apply the initial
        /// art state. Invalid references are refused before replacing configuration.</summary>
        public void Configure(GameObject[] roofObjects,GameObject[] interiorObjects,bool initiallyOpen)
        {
            var unique=new HashSet<GameObject>();
            Validate(roofObjects,unique);Validate(interiorObjects,unique);
            roofs=roofObjects==null?Array.Empty<GameObject>():(GameObject[])roofObjects.Clone();
            interiors=interiorObjects==null?Array.Empty<GameObject>():(GameObject[])interiorObjects.Clone();
            SetCutaway(initiallyOpen);
        }
        void Validate(GameObject[] targets,HashSet<GameObject> unique)
        {
            if(targets==null)return;
            foreach(var target in targets)
                if(target!=null&&(!Owns(target)||!unique.Add(target)))
                    throw new ArgumentException("Showcase targets must be unique descendants of the art root.");
        }
        bool Owns(GameObject target)=>target!=null&&target!=gameObject&&target.transform.IsChildOf(transform);
        public void Toggle()=>SetCutaway(!cutaway);
        /// <summary>Apply the art-only roof cutaway; repeated calls repair the same
        /// local visibility state. Reparented/removed objects are no longer controlled.</summary>
        public void SetCutaway(bool open){cutaway=open;Apply();}
        void OnEnable()=>Apply();
        void Apply()
        {
            SetOwned(roofs,!cutaway);SetOwned(interiors,cutaway);
        }
        void SetOwned(GameObject[] objects,bool visible)
        {
            if(objects==null)return;
            foreach(var target in objects)if(Owns(target)&&target.activeSelf!=visible)target.SetActive(visible);
        }
        void Update()
        {
            if(Application.isPlaying&&Keyboard.current!=null&&Keyboard.current.rKey.wasPressedThisFrame)Toggle();
        }
    }
}
