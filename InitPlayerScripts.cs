
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.Takato.VideoPlayer
{
    public class InitPlayerScripts : UdonSharpBehaviour
    {
        public UdonSyncVideoPlayer Player;
        public LoopToggle LoopToggle;
        void Start()
        {
            Player.Init();
            LoopToggle.Init();
        }
    }
}