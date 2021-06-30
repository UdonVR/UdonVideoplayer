
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using UdonVR.Takato.VideoPlayer;

namespace UdonVR
{
    public class StutterDetector : UdonSharpBehaviour
    {
        public UdonSyncVideoPlayer VideoPlayer;
        public float Target = 2f;
        public GameObject TargetObj;
        public InputField Inputfeild;
        public GameObject ParentDebug;

        private bool isDebug = false;
        private Text[] DebugChildren;
        private int ChildCount = 0;
        private int CurrentChild = 0;
        private float OldTime = 10f;
        private float CurTime = 0;
        void Start()
        {
            if (ParentDebug != null)
            {
                DebugChildren = ParentDebug.transform.GetComponentsInChildren<Text>();
                ChildCount = DebugChildren.Length - 1;
                reset();
                isDebug = true;
            }
        }

        private void Update()
        {
            if (VideoPlayer.autoResync)
            {
                CurTime = Time.deltaTime;

                if (CurTime > OldTime * Target)
                {
                    VideoPlayer.ForceSyncVideo();
                }

                if (isDebug) DebugOut();

                OldTime = Time.deltaTime;
            }
        }

        public void reset()
        {
            if (TargetObj != null)
                TargetObj.SetActive(false);
        }

        public void SetTarget()
        {
            Target = float.Parse(Inputfeild.text);
        }

        private void DebugOut()
        {
            if (CurTime > OldTime * Target)
            {
                if (TargetObj != null)
                    TargetObj.SetActive(true);
            }
            DebugChildren[CurrentChild].text = CurTime.ToString();
            CurrentChild++;
            if (CurrentChild > ChildCount) CurrentChild = 0;
        }        
    }
}