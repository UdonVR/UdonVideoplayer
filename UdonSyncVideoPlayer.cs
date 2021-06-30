using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using TMPro;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components;

namespace UdonVR.Takato.VideoPlayer
{
    public class UdonSyncVideoPlayer : UdonSharpBehaviour
    {
        public VRCAVProVideoPlayer avProVideoPlayer;
        public VRCUnityVideoPlayer unityVideoPlayer;

        [HideInInspector] public
        BaseVRCVideoPlayer videoPlayer;
        public VRCUrl videoURL;
        public bool autoPlay;
        public VRCUrlInputField videoURLInputField;
        public MasterOnly MasterOnlyScript;
        //public MeshRenderer aVProRenderTextureSource;
        //public MeshRenderer screenMesh;
        //public RenderTexture vRCUnityRenderTexture;
        //private RenderTexture _AVProRenderTexture;
        //public PanelController PanelController;

        public Text videoTime;
        public Slider videoTimeBar;
        public Text masterText;
        public Text ownerText;
        public float syncFrequency = 5;
        public float syncThreshold = 1;



        private float _lastSyncTime = 0;
        private float _delayTime;
        [UdonSynced] private float _videoStartNetworkTime = 0;
        private float _videoStartTime = 0;
        [UdonSynced] private VRCUrl _syncedURL;
        private VRCUrl _loadedVideoURL;
        [UdonSynced] private int _videoNumber = 0;
        private int _loadedVideoNumber = 0;
        [UdonSynced] private bool _ownerPlaying = false;
        [UdonSynced] private bool _ownerPaused = false;
        private bool _paused;
        private bool _waitForSync = false;
        private string _videoDuration = "3:00:000";
        private string _timeFormat = @"m\:ss";
        private bool _isTooLong;
        private bool _forcePlay = false;
        private int _retries;
        private int _deserialCount = 0;
        private int ErrorCheck = 0;
        private bool _newVideo = true;
        private const int VRCUNITY_PLAYER_MODE = 1;
        private const int AVPRO_PLAYER_MODE = 0;
        [Range(0,1)]
        public int defaultVideoPlayer = AVPRO_PLAYER_MODE;
        [UdonSynced]private int _currentVideoMode = AVPRO_PLAYER_MODE;
        private int _localVideoMode = AVPRO_PLAYER_MODE;

        private bool _debug = false;

        public GameObject[] ErrorScreens;

        public Image AVPro_fill;
        public Image VRCUnity_fill;

        #region Auto resync Var
        [Header("Resync")]
        [Range(0.001f, 0.1f)]
        public float ResyncTime = .1f;
        public int autoResyncMinutes = 5;
        public GameObject AutoResyncFill;
        public InputField autoResyncRateInput;
        public TextMeshProUGUI resyncText;

        private bool _resyncedVideo;
        private bool _reloadedVideo;
        private bool _forceResync;
        [HideInInspector]
        public bool autoResync;
        private float _autoResyncRate;
        private float _autoResyncTime;
        #endregion

        public LoopToggle LT;

        private void Start()
        {
            //videoPlayer.Loop = false;
            //vRCUnityRenderTexture = (RenderTexture)screenMesh.sharedMaterial.GetTexture("_MainTex");
            if (defaultVideoPlayer == AVPRO_PLAYER_MODE)
            {
                SetVideoModeAvPro();
            }
            else
            {
                SetVideoModeVRCUnity();
            }
            
            AutoResyncInit();
            if (Networking.IsMaster && autoPlay)
            {
                _syncedURL = videoURL;
                //videoURLInputField.SetUrl(videoURL);
                //OnURLChanged();
                _videoNumber = 1;
                _delayTime = Time.time + 1f;
                _forcePlay = true;
            }
            if (Networking.LocalPlayer != null)
            {
                masterText.text = Networking.GetOwner(masterText.gameObject).displayName;
                ownerText.text = Networking.GetOwner(gameObject).displayName;
                if (Networking.LocalPlayer.displayName == "Takato" || Networking.LocalPlayer.displayName == "Takato65" || Networking.LocalPlayer.displayName == "child of the beast")
                    _debug = true;
            }

            if (Networking.IsMaster) LT.Init();
        }
        public void ChangeVideoPlayerAvPro()
        {
            if (Networking.IsOwner(gameObject))
            {
                videoPlayer.Stop();
                SetVideoModeAvPro();
                if (_ownerPlaying)
                    RetrySyncedVideo();
            }
        }
        public void ChangeVideoPlayerVRCUnity()
        {
            if (Networking.IsOwner(gameObject))
            {
                videoPlayer.Stop();
                SetVideoModeVRCUnity();
                if (_ownerPlaying)
                    RetrySyncedVideo();
            }
        }
        void SetVideoModeAvPro()
        {
            Debug.Log("[UdonVR] Changing Player to AvPro");
            AVPro_fill.enabled = true;
            VRCUnity_fill.enabled = false;
            videoPlayer = avProVideoPlayer;
            _currentVideoMode = AVPRO_PLAYER_MODE;
            _localVideoMode = AVPRO_PLAYER_MODE;

            //screenMesh.sharedMaterial.SetTexture("_MainTex", aVProRenderTextureSource.sharedMaterial.GetTexture("_MainTex"));
        }

        void SetVideoModeVRCUnity()
        {
            Debug.Log("[UdonVR] Changing Player to VRCUnity");
            AVPro_fill.enabled = false;
            VRCUnity_fill.enabled = true;
            videoPlayer = unityVideoPlayer;
            _currentVideoMode = VRCUNITY_PLAYER_MODE;
            _localVideoMode = VRCUNITY_PLAYER_MODE;

            //screenMesh.sharedMaterial.SetTexture("_MainTex", vRCUnityRenderTexture);
        }

        #region URL_Methods

        public void ChangeVideoUrlVRC(VRCUrl url)
        {
            if (url.Get() != "" && url != _syncedURL)
            {
                _syncedURL = url;
                ChangeVideoUrl();
            }
        }

        public void ChangeVideoUrl()
        {
            //When the Owner changes the URL
            //Debug.Log("[UdonSyncVideoPlayer] URL Changed Start");
            if (Networking.IsOwner(gameObject))
            {
                //Debug.Log("[UdonSyncVideoPlayer] URL Changed Owner");

                //Set as new Video
                _newVideo = true;

                // Attempt to get a start time from YouTube links with t= or start=
                string urlStr = _syncedURL.Get();

                _videoStartTime = 0f;
                if (urlStr.Contains("youtu.be/") || urlStr.Contains("youtube.com/watch"))
                {
                    int startIndex;
                    startIndex = urlStr.IndexOf("?t=");

                    if (startIndex == -1) startIndex = urlStr.IndexOf("&t=");
                    if (startIndex == -1) startIndex = urlStr.IndexOf("&start=");
                    if (startIndex == -1) startIndex = urlStr.IndexOf("?start=");

                    if (startIndex != -1)
                    {
                        char[] urlArr = urlStr.ToCharArray();
                        int numIndex = urlStr.IndexOf('=', startIndex) + 1;
                        string timeStr = "";

                        while (numIndex < urlArr.Length)
                        {
                            char currentChar = urlArr[numIndex];
                            if (!char.IsNumber(currentChar))
                                break;

                            timeStr += currentChar;
                            ++numIndex;
                        }

                        if (timeStr.Length > 0)
                        {
                            int secondsCount;
                            if (int.TryParse(timeStr, out secondsCount))
                                _videoStartTime = secondsCount;
                        }
                    }
                }

                videoTimeBar.interactable = false;
                _videoNumber = _videoNumber + 1;
                _loadedVideoNumber = _videoNumber;
                videoPlayer.Stop();

                videoPlayer.LoadURL(_syncedURL);

                _ownerPlaying = false;
                _ownerPaused = false;
                _videoStartNetworkTime = float.MaxValue;

                videoURLInputField.SetUrl(VRCUrl.Empty);
                Debug.Log(string.Format("[UdonSyncVideoPlayer] Video URL Changed to {0}", _syncedURL));
            }
        }

        public void OnURLChanged()
        {//When the Owner changes the URL
            //Debug.Log("[UdonSyncVideoPlayer] URL Changed Start");
            if (Networking.IsOwner(gameObject))
            {
                //Debug.Log("[UdonSyncVideoPlayer] URL Changed Owner");
                if (videoURLInputField.GetUrl().Get().Trim() != "" && videoURLInputField.GetUrl() != _syncedURL)
                {
                    _syncedURL = videoURLInputField.GetUrl();
                    ChangeVideoUrl();
                }
            }
        }

        #endregion URL_Methods

        public bool EnableTimeBar()
        {
            return _paused && !_isTooLong;
        }

        public override void OnOwnershipTransferred()
        {
            ownerText.text = Networking.GetOwner(gameObject).displayName;
            //Debug.Log($"[UdonSyncVideoPlayer] Owner changed to {Networking.LocalPlayer.displayName}");
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            masterText.text = Networking.GetOwner(masterText.gameObject).displayName;
        }

        private void DebugLog()
        {
            Debug.Log($"[UdonSyncVideoPlayer] ==========================================");
            Debug.Log($"[UdonSyncVideoPlayer] _loadedVideoNumber>> {_videoNumber}");
            Debug.Log($"[UdonSyncVideoPlayer] _ownerPlaying>> {_ownerPlaying}");
            Debug.Log($"[UdonSyncVideoPlayer] _ownerPaused>> {_ownerPaused}");
            Debug.Log($"[UdonSyncVideoPlayer] _paused>> {_paused}");
            Debug.Log($"[UdonSyncVideoPlayer] _waitForSync>> {_waitForSync}");
            Debug.Log($"[UdonSyncVideoPlayer] _syncedURL>> {_syncedURL}");
            Debug.Log($"[UdonSyncVideoPlayer] videoPlayer.IsPlaying>> {videoPlayer.IsPlaying}");
            Debug.Log($"[UdonSyncVideoPlayer] videoPlayer.IsReady>> {videoPlayer.IsReady}");
            Debug.Log($"[UdonSyncVideoPlayer] videoPlayer Owner>> {Networking.GetOwner(gameObject).displayName}");
            Debug.Log($"[UdonSyncVideoPlayer] _forcePlay>> {_forcePlay}");
            //Debug.Log($"[UdonSyncVideoPlayer] {vRCUnityRenderTexture.}");
            Debug.Log($"[UdonSyncVideoPlayer] ==========================================");
        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                SyncVideoIfTime();
            }
            else
            {
                if (_waitForSync)
                {
                    if (_ownerPlaying)
                    {
                        videoPlayer.Play();
                        _waitForSync = false;
                        SyncVideo();
                    }
                }
                else
                {
                    SyncVideoIfTime();
                }
            }
            if (_ownerPlaying && !_ownerPaused)
            {
                if (!_isTooLong)
                {
                    //videoTime.text = string.Format("{0:N2}/{1:N2}",videoPlayer.GetTime(),videoPlayer.GetDuration());
                    if (TimeSpan.MaxValue.TotalSeconds >= videoPlayer.GetTime())
                        videoTimeBar.value = videoPlayer.GetTime();
                }
                if (autoResync)
                {
                    if (_autoResyncTime >= _autoResyncRate)
                    {
                        _autoResyncTime = 0;
                        _forceResync = true;
                        SyncVideo();
                    }
                    else
                        _autoResyncTime += Time.deltaTime;
                }
            }

            if (_debug)
            {
                if (Input.GetKeyDown(KeyCode.P))
                    DebugLog();
            }
            if (Networking.IsMaster)
            {
                if (_forcePlay && autoPlay && Time.time > _delayTime)
                {
                    Debug.Log($"[UdonSyncVideoPlayer] Auto Play URL {_syncedURL}");
                    videoPlayer.PlayURL(_syncedURL);
                    _delayTime = Time.time + 5f;
                    _retries += 1;
                    if (_retries > 5)
                        _forcePlay = false;
                }
            }
            else
            {
                if (_forcePlay && Time.time > _delayTime)
                {
                    Debug.Log($"[UdonSyncVideoPlayer] Watcher Load URL {_syncedURL}");
                    videoPlayer.LoadURL(_syncedURL);
                    _delayTime = Time.time + 7f;
                    _retries += 1;
                    if (_retries > 5)
                        _forcePlay = false;
                }
            }
        }

        public void UpdateDisplay()
        {
            if (!_isTooLong && _videoDuration != "Streaming!")
                videoTime.text = TimeSpan.FromSeconds(videoTimeBar.value).ToString(_timeFormat) + "/" + _videoDuration;
            else
                videoTime.text = _videoDuration;

            if (videoTimeBar.interactable && _ownerPaused)
            {
                videoPlayer.SetTime(videoTimeBar.value);
                TakeOwner();
            }
        }

        #region Syncing Methods

        public void ResyncReset()
        {
            Debug.LogWarning("[UdonVR] Playing synced: ResyncReset()");
            resyncText.text = "Resync";
            //PanelController.SetResyncText("Resync");
            _resyncedVideo = false;
        }

        public void ReloadReset()
        {
            Debug.LogWarning("[UdonVR] Playing synced: ReloadReset()");
            resyncText.text = _resyncedVideo ? "Reload" : "Resync";
            //PanelController.SetResyncText(_resyncedVideo ? "Reload" : "Resync");
            _reloadedVideo = false;
        }

        public void ResyncVideo()
        {
            Debug.LogWarning("[UdonVR] Playing synced: ResyncVideo");
            if (_resyncedVideo)
            {
                Debug.LogWarning("[UdonVR] Playing synced: _resyncedVideo");
                if (!_reloadedVideo)
                {
                    Debug.LogWarning("[UdonVR] Playing synced: !_reloadedVideo");
                    _reloadedVideo = true;
                    //PanelController.SetResyncText("Wait");
                    resyncText.text = "Wait";

                    SendCustomEventDelayedSeconds("ReloadReset", 6f);
                    RetrySyncedVideo();
                }
            }
            else
            {
                Debug.LogWarning("[UdonVR] Playing synced: !_resyncedVideo");
                _resyncedVideo = true;

                resyncText.text = _reloadedVideo ? "Wait" : "Reload";
                //PanelController.SetResyncText(_reloadedVideo ? "Wait" : "Reload");
                _forceResync = true;
                SyncVideo();
                SendCustomEventDelayedSeconds("ResyncReset", 3f);
            }
        }

        public void RetrySyncedVideo()
        {
            Debug.Log("[UdonVR] Playing synced: RetrySyncedVideo()");
            videoPlayer.LoadURL(_syncedURL);
            SyncVideo();
            _loadedVideoURL = _syncedURL;
            Debug.Log(string.Format("[UdonSyncVideoPlayer] Playing synced: {0}", _syncedURL));

            //Turn on forcePlay and set delayTime for repeat tries
            _delayTime = Time.time + 7f;
            _forcePlay = true;
            _retries = 0;
        }

        public void SyncVideo()
        {//SyncVideo Event: Check if Offset is greater than syncThreshold (video is too far out of sync)
            //Get Offset Time
            float offsetTime;
            offsetTime = Mathf.Clamp(Convert.ToSingle(Networking.GetServerTimeInSeconds()) - _videoStartNetworkTime, 0, videoPlayer.GetDuration());

            if (_forceResync)
                offsetTime -= ResyncTime;

            if (_forceResync || Mathf.Abs(videoPlayer.GetTime() - offsetTime) > syncThreshold)
            {//Resync video time and log new value
                videoPlayer.SetTime(offsetTime);
                _forceResync = false;
                //Debug.Log(string.Format("[UdonSyncVideoPlayer] Syncing Video to {0:N2}", offsetTime));
            }
        }
        public void ForceSyncVideo()
        {
            _forceResync = true;
            SyncVideo();
        }

        public void SyncVideoIfTime()
        {
            if (_ownerPlaying)
            {
                if (!_ownerPaused)
                {
                    if (Time.realtimeSinceStartup - _lastSyncTime > syncFrequency)
                    {
                        _lastSyncTime = Time.realtimeSinceStartup;
                        SyncVideo();
                    }
                }
            }
        }

        #endregion Syncing Methods

        #region AutoResyncMethods
        public void AutoResyncToggle()
        {
            autoResync = !autoResync;
            AutoResyncFill.SetActive(autoResync);

            if (autoResync)
                _autoResyncTime = 0;

            //Mock code
            //PanelController.AutoResyncToggle(_autoResync);
        }

        private void AutoResyncInit()
        {
            _autoResyncRate = autoResyncMinutes * 60;
            autoResyncRateInput.text = autoResyncMinutes.ToString();
            //PanelController.AutoResyncRateInput(autoResyncMinutes);
        }
        public void AutoResyncSet(int value)
        {
            //int temp;
            //int.TryParse(autoResyncRateInput.text, out temp);

            autoResyncMinutes = Mathf.Max(1, value);
            _autoResyncRate = autoResyncMinutes * 60;
            autoResyncRateInput.text = autoResyncMinutes.ToString();
            
            //PanelController.AutoResyncRateInput(autoResyncMinutes);
        }
        public void AutoResyncDown()
        {
            autoResyncMinutes = Mathf.Max(1, autoResyncMinutes - 1);
            _autoResyncRate = autoResyncMinutes * 60;
            autoResyncRateInput.text = autoResyncMinutes.ToString();
            
            //PanelController.AutoResyncRateInput(autoResyncMinutes);
        }
        public void AutoResyncUp()
        {
            //_autoResyncMinutes = Mathf.Max(1, _autoResyncMinutes - 1);
            autoResyncMinutes++;
            _autoResyncRate = autoResyncMinutes * 60;
            autoResyncRateInput.text = autoResyncMinutes.ToString();
            
            //PanelController.AutoResyncRateInput(autoResyncMinutes);
        }
        #endregion

        public void TakeOwner()
        {
            if (Networking.IsMaster)
            {
                DoTakeOwner();
            }
            else if (MasterOnlyScript.syncedMasterOnly == false)
            {
                DoTakeOwner();
            }
        }

        private void DoTakeOwner()
        {
            //Debug.Log("[UdonSyncVideoPlayer] TakeOWner Called!");
            if (!Networking.IsOwner(gameObject))
            {
                //Debug.Log($"[UdonSyncVideoPlayer] Setting Owner to {Networking.LocalPlayer.displayName}");
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                ownerText.text = Networking.LocalPlayer.displayName;
                //PanelController.SetOwnerText(Networking.LocalPlayer.displayName);
            }
        }

        private void SetUpTimeBar()
        {
            if (TimeSpan.MaxValue.TotalSeconds >= videoPlayer.GetDuration())
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(videoPlayer.GetDuration());
                if (Networking.IsOwner(gameObject))
                    _isTooLong = false;
                videoTimeBar.maxValue = videoPlayer.GetDuration();
                //PanelController.SetVideoTimeBarMaxValue(videoPlayer.GetDuration());

                if (timeSpan.TotalHours >= 1)
                    _timeFormat = @"h\:mm\:ss";
                else if (timeSpan.TotalMinutes >= 1)
                    _timeFormat = @"m\:ss";
                else
                    _timeFormat = @"%s";

                //Debug.Log($"[[UdonSyncVideoPlayer] Time Format is (({_timeFormat}))");
                _videoDuration = timeSpan.ToString(_timeFormat);
            }
            else
            {
                if (Networking.IsOwner(gameObject))
                    _isTooLong = true;
                videoTimeBar.maxValue = 1;
                //PanelController.SetVideoTimeBarMaxValue(1);
                _videoDuration = "Streaming!";
                videoTimeBar.value = 1;
                //PanelController.SetVideoTimeBarValue(1);
                
            }
        }

        #region OnVideo_Overrides

        public override void OnVideoLoop()
        {
            Debug.Log("[UdonSyncVideoPlayer] Video Looped");
            if (Networking.IsOwner(gameObject))
            {
                _videoStartNetworkTime = Convert.ToSingle(Networking.GetServerTimeInSeconds());
            }
        }

        public override void OnVideoReady()
        {
            //Debug.Log(string.Format("[UdonSyncVideoPlayer] OnVideoReady {0}", _syncedURL));
            if (Networking.IsOwner(gameObject))
            {//The Owner Plays the video when it's ready
                videoPlayer.Play();
                Debug.Log(string.Format("[UdonSyncVideoPlayer] Owner Play URL {0}", _syncedURL));
                SetUpTimeBar();
            }
            else
            {
                //If the Owner is playing the video, Play it and run SyncVideo
                if (_ownerPlaying)
                {
                    Debug.Log(string.Format("[UdonSyncVideoPlayer] Watcher Play URL {0}", _syncedURL));
                    videoPlayer.Play();
                    SetUpTimeBar();

                    SyncVideo();
                }
                else
                {
                    _waitForSync = true;
                }
                //Turn off forcePlay as video is ready for watcher
                _forcePlay = false;
            }
        }

        public override void OnVideoStart()
        {//Handle OnVideoStart for Owner and Watchers
            //Debug.Log(string.Format("[UdonSyncVideoPlayer] OnVideoStart {0}", _syncedURL));
            videoTimeBar.interactable = false;
            if (Networking.IsOwner(gameObject))
            {//The Owner saves the start time and sets playing to true
                if (!_ownerPaused && _newVideo)
                {
                    _videoStartNetworkTime = Convert.ToSingle(Networking.GetServerTimeInSeconds()) - _videoStartTime;
                    _newVideo = false;
                }
                _ownerPlaying = true;
                _forcePlay = false;
                _ownerPaused = false;
                SetUpTimeBar();
            }
            else
            {//The Watchers pause it and wait for sync
                if (!_ownerPlaying)
                {
                    videoPlayer.Pause();
                    _waitForSync = true;
                }
                SetUpTimeBar();
            }
        }

        public override void OnVideoEnd()
        {
            Debug.Log(string.Format("[UdonSyncVideoPlayer]Video ended URL: {0}", _syncedURL));
        }

        public override void OnVideoError(VideoError videoError)
        {//On Video Error, log what went wrong
            videoPlayer.Stop();
            //Turn off forcePlay since video has error
            _forcePlay = false;

            if (ErrorCheck == 0)
            {
                if (ErrorScreens.Length == 5)
                {
                    ErrorCheck = 2;
                }
                else ErrorCheck = 1;
            }

            if (ErrorCheck == 1)
            {
                Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", videoError.ToString(), _syncedURL));
            }
            else if (ErrorCheck == 2)
            {
                switch (videoError)
                {
                    case VideoError.Unknown:
                        Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", "[VP00]Unknown Error", _syncedURL));
                        if (ErrorScreens[0] != null) ErrorScreens[0].SetActive(true);
                        break;

                    case VideoError.InvalidURL:
                        Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", "[VP01]InvalidURL Error", _syncedURL));
                        if (ErrorScreens[1] != null) ErrorScreens[1].SetActive(true);
                        break;

                    case VideoError.AccessDenied:
                        Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", "[VP02]AccessDenied Error (Not on Whitelist)", _syncedURL));
                        if (ErrorScreens[2] != null) ErrorScreens[2].SetActive(true);
                        break;

                    case VideoError.PlayerError:
                        Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", "[VP03]PlayerError Error", _syncedURL));
                        if (ErrorScreens[3] != null) ErrorScreens[3].SetActive(true);
                        break;

                    case VideoError.RateLimited:
                        Debug.Log(string.Format("[UdonSyncVideoPlayer] Video Error {0} >> {1}", "[VP04]RateLimit Error", _syncedURL));
                        if (ErrorScreens[4] != null) ErrorScreens[4].SetActive(true);
                        break;

                    default:
                        break;
                }
            }
        }

        #endregion OnVideo_Overrides

        public override void OnPreSerialization()
        {
            _deserialCount = 0;
        }

        public override void OnDeserialization()
        {//Load new video when _videoNumber is changed
            if (!Networking.IsOwner(gameObject))
            {
                if (_deserialCount < 10)
                {
                    _deserialCount++;
                    return;
                }
                if (_localVideoMode != _currentVideoMode)
                {
                    videoPlayer.Stop();
                    if (_currentVideoMode == AVPRO_PLAYER_MODE)
                    {
                        SetVideoModeAvPro();
                    }
                    else
                    {
                        SetVideoModeVRCUnity();
                    }
                }
                if (_videoNumber != _loadedVideoNumber)
                {
                    videoPlayer.Stop();
                    if (_loadedVideoURL != _syncedURL)
                    {
                        _loadedVideoNumber = _videoNumber;
                        _loadedVideoURL = _syncedURL;
                        if (VRCUrl.Empty.Get() != _syncedURL.Get())
                        {
                            videoPlayer.LoadURL(_syncedURL);
                            SyncVideo();
                            Debug.Log(string.Format("[UdonSyncVideoPlayer] Playing synced: {0}", _syncedURL));

                            //Turn on forcePlay and set delayTime for repeat tries
                            _delayTime = Time.time + 7f;
                            _forcePlay = true;
                            _retries = 0;
                        }
                    }
                    else
                    {
                        Debug.Log("[UdonSyncVideoPlayer] synced Url is the same as last url, url is most likely too long to sync.");
                    }
                }
                if (_ownerPaused != _paused)
                {
                    _paused = _ownerPaused;
                    if (_ownerPaused)
                    {
                        videoPlayer.Pause();
                        if (!_isTooLong)
                            videoTimeBar.interactable = true;
                    }
                    else if (_ownerPlaying)
                    {
                        //videoTimeBar.interactable = false;
                        videoPlayer.Play();
                        SyncVideo();
                    }
                }
               
            }
        }

        public void StopVideo()
        {
            if (Networking.IsOwner(gameObject))
            {
                _videoStartNetworkTime = 0;
                _ownerPlaying = false;
                _ownerPaused = false;
                videoPlayer.Stop();
                _syncedURL = VRCUrl.Empty;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopVideoWatcher");
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StopVideo");
                //TakeOwner();
            }
        }

        public void StopVideoWatcher()
        {
            if (!Networking.IsOwner(gameObject))
            {
                videoPlayer.Stop();
            }
        }

        public void PlayVideo()
        {
            if (!videoPlayer.IsPlaying)
            {
                PauseVideo();
            }
        }

        public void PauseVideo()
        {
            if (Networking.IsOwner(gameObject))
            {
                if (videoPlayer.IsPlaying)
                {
                    videoPlayer.Pause();
                    if (!_isTooLong)
                        //PanelController.VideoTimeBarInteractable(true);
                        videoTimeBar.interactable = true;
                    //_ownerPlaying = false;
                    //_videoPausedNetworkTime = Convert.ToSingle(Networking.GetServerTimeInSeconds());
                    _ownerPaused = true;
                }
                else if (_videoStartNetworkTime != 0)
                {
                    videoTimeBar.interactable = false;
                    //PanelController.VideoTimeBarInteractable(false);

                    float videoCurrentTime = videoPlayer.GetTime();

                    if (!_isTooLong && videoTimeBar.value != videoCurrentTime)
                    {
                        videoCurrentTime = videoTimeBar.value;
                        _videoStartNetworkTime = Convert.ToSingle(Networking.GetServerTimeInSeconds()) - videoCurrentTime;
                        _lastSyncTime = Time.realtimeSinceStartup;
                        videoPlayer.Play();
                        SyncVideo();
                    }
                    else
                    {
                        _videoStartNetworkTime = Convert.ToSingle(Networking.GetServerTimeInSeconds()) - videoCurrentTime;
                        _lastSyncTime = Time.realtimeSinceStartup;
                        videoPlayer.Play();
                    }
                    //_ownerPlaying = true;
                    //_ownerPaused = false;
                }
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "PauseVideo");
                //TakeOwner();
            }
        }
    }
}