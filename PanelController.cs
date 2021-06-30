
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonVR.Takato.VideoPlayer
{
    /// <summary>
    /// 
    /// </summary>
    public class PanelController : UdonSharpBehaviour 
    {
        public PanelUI[] panels;

       public void AutoResyncToggle(bool value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.AutoResyncToggle(value);
            }
        }       
        public void AutoResyncRateInput(int value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.AutoResyncRateInput(value);
            }
        }

        public void SetOwnerText(string displayName)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.SetOwnerText(displayName);
            }
        }

        public void SetVideoTimeBarMaxValue(float value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.SetVideoTimeBarMaxValue(value);
            }
        }

        public void SetVideoTimeBarValue(int value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.SetVideoTimeBarValue(value);
            }
        }

        public void VideoTimeBarInteractable(bool value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.VideoTimeBarInteractable(value);
            }
        }

        public void SetResyncText(string value)
        {
            foreach (PanelUI Panel in panels)
            {
                Panel.SetResyncText(value);
            }
        }
    }
}
