
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
        private InputField _FrameSkipFeild;

        private void Start()
        {
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

            if (UpdateRealtimeGI)
            {
                FrameSkipUI.SetActive(true);
                _FrameSkipFeild = FrameSkipUI.GetComponentInChildren<InputField>();
            }
        }
        public void SetHide()
        {
            if (!_IsOn)
            {
                SetEmission();
            } else
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

            } else
            {
                _Frame++;
            }
        }

        public void FrameUp()
        {
            FrameSkip++;
            _FrameSkipFeild.text = FrameSkip.ToString();
        }

        public void FrameDown()
        {
            if (FrameSkip <= 0) return;
            FrameSkip--;
            _FrameSkipFeild.text = FrameSkip.ToString();
        }

        public void FrameSet()
        {
            int _var = 0;
            int.TryParse(_FrameSkipFeild.text, out _var);
            if (_var <= 0) _var = 0;
            FrameSkip = _var;
            _FrameSkipFeild.text = FrameSkip.ToString();
        }
    }
}
