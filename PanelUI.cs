using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.Takato.VideoPlayer
{
    /// <summary>
    /// 
    /// </summary>
    public class PanelUI : UdonSharpBehaviour
    {
        public UdonSyncVideoPlayer UdonSyncVideoPlayer;
        public GameObject autoResyncFilled;
        public TextMeshProUGUI resyncText;
        public InputField autoResyncRateInput;
        public Text ownerText;
        public Slider videoTimeBar;
        public Text videoTimeBarText;

        public void AutoResyncToggle(bool value)
        {
            if (Utilities.IsValid(autoResyncFilled))
            {
                autoResyncFilled.SetActive(value);
            }
        }

        public void AutoResyncSet()
        {
            if (Utilities.IsValid(autoResyncRateInput))
            {
                if (!string.IsNullOrEmpty(autoResyncRateInput.text.Trim()))
                    return;

                int temp;
                int.TryParse(autoResyncRateInput.text, out temp);
                UdonSyncVideoPlayer.AutoResyncSet(temp);
                autoResyncRateInput.text = string.Empty;
                
            }
        }
        public void AutoResyncRateInput(int value)
        {
            if (Utilities.IsValid(autoResyncRateInput))
            {
                ((Text)autoResyncRateInput.placeholder).text= value.ToString();
           
            }
        }

        public void SetOwnerText(string displayName)
        {
            if (Utilities.IsValid(ownerText))
            {
                ownerText.text = displayName;
            }
        }

        public void SetVideoTimeBarValue(int value)
        {
            if (Utilities.IsValid(videoTimeBar))
            {
                videoTimeBar.value = value;
            }   
        }

        public void SetVideoTimeBarMaxValue(float value)
        {
            if (Utilities.IsValid(videoTimeBar))
            {
                videoTimeBar.maxValue = value;
            }
        }

        public void VideoTimeBarInteractable(bool value)
        {
            if (Utilities.IsValid(videoTimeBar))
            {
                videoTimeBar.interactable = value;
            }
        }

        public void SetResyncText(string value)
        {
            if (Utilities.IsValid(resyncText))
            {
                resyncText.text = value;
            }
        }
    }
}
