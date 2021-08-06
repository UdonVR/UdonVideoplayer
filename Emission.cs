
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Video.Components.AVPro;


namespace UdonVR.Childofthebeast
{
    public class Emission : UdonSharpBehaviour
    {
        #if UNITY_ANDROID
            private bool isQuest = true;
        #else
            private bool isQuest = false;
        #endif
        public MeshRenderer ScreenMesh;
        public int Material_Index;
        public bool SharedMerial = false;
        public bool DefaultOff = false;
        public bool UpdateRealtimeGI = false;
        public GameObject[] ButtonFills;
        public int FrameSkip = 5;
        public GameObject FrameSkipUI;

        private Material _ScreenMaterial;
        private float _CurrentEmission = 1;
        private bool _IsOn = true;
        private int _Frame = 0;
        public InputField FrameSkipFeild;
        public Text FramSkipText;

        private void Start()
        {
            if (UpdateRealtimeGI && FrameSkipUI != null) FrameSkipUI.SetActive(true);
            if (!isQuest) InitPC();
            if (ScreenMesh == null) ScreenMesh = gameObject.GetComponent<MeshRenderer>();
            if (SharedMerial)
            {
                _ScreenMaterial = ScreenMesh.sharedMaterials[Material_Index];
            }
            else
            {
                _ScreenMaterial = ScreenMesh.materials[Material_Index];
            }

            _ScreenMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (DefaultOff) SetHide();
        }
        public void SetHide()
        {
            if (!_IsOn)
            {
                SetEmission();
            }
            else
            {
                _ScreenMaterial.SetFloat("_Emission", 0);
                _IsOn = false;
                if (ScreenMesh != null)
                    RendererExtensions.UpdateGIMaterials(ScreenMesh);
            }
        }
        public void SetOff()
        {
            if (_CurrentEmission != .1f)
            {
                ButtonFills[0].SetActive(true);
                ButtonFills[1].SetActive(false);
                ButtonFills[2].SetActive(false);
                _CurrentEmission = .1f;
                SetEmission();
            }
            else Set0();
        }
        public void Set1()
        {
            if (_CurrentEmission != 1f)
            {
                ButtonFills[1].SetActive(true);
                ButtonFills[0].SetActive(false);
                ButtonFills[2].SetActive(false);
                _CurrentEmission = 1f;
                SetEmission();
            }
            else Set0();
        }
        public void Set2()
        {
            if (_CurrentEmission != 2f)
            {
                ButtonFills[2].SetActive(true);
                ButtonFills[1].SetActive(false);
                ButtonFills[0].SetActive(false);
                _CurrentEmission = 2f;
                SetEmission();
            }
            else Set0();
        }
        public void Set0()
        {
            ButtonFills[2].SetActive(false);
            ButtonFills[1].SetActive(false);
            ButtonFills[0].SetActive(false);
            _CurrentEmission = 0f;
            _ScreenMaterial.SetFloat("_Emission", _CurrentEmission);
            _IsOn = false;
            RendererExtensions.UpdateGIMaterials(ScreenMesh);
        }

        public void SetEmission()
        {
            _ScreenMaterial.SetFloat("_Emission", _CurrentEmission);
            _IsOn = true;
        }
        private void Update()
        {
            if (_Frame >= FrameSkip)
            {
                if (ScreenMesh != null && _IsOn && UpdateRealtimeGI)
                    RendererExtensions.UpdateGIMaterials(ScreenMesh);
                _Frame = 0;

            }
            else
            {
                _Frame++;
            }
        }

        public void FrameUp()
        {
            FrameSkip++;
            UpdateFeild(FrameSkip.ToString());
        }

        public void FrameDown()
        {
            if (FrameSkip <= 0) return;
            FrameSkip--;
            UpdateFeild(FrameSkip.ToString());
        }

        public void FrameSet()
        {
            int _var = 0;
            int.TryParse(FrameSkipFeild.text, out _var);
            if (_var <= 0) _var = 0;
            FrameSkip = _var;
            UpdateFeild(FrameSkip.ToString());
        }

        private void UpdateFeild(string _str)
        {
            if (isQuest)
            {
                FramSkipText.text = _str;
            }
            else
            {
                FrameSkipFeild.text = _str;
            }
        }

        private void InitPC()
        {
            if (UpdateRealtimeGI && FrameSkipUI != null) FrameSkipFeild.interactable = true;
        }
    }
}