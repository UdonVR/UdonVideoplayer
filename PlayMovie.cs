
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.Takato.VideoPlayer
{
    public class PlayMovie : UdonSharpBehaviour
    {
        public UdonSyncVideoPlayer Player;
        public VRCUrl URL;

        public override void Interact()
        {
            Player.TakeOwner();
            Player.ChangeVideoUrlVRC(URL);
        }
    }

}