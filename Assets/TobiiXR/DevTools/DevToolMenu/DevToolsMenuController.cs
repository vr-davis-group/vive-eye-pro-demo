// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using Tobii.XR.GazeModifier;
using Tobii.XR.GazeVisualizer;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Tobii.XR.DevTools
{
    public class DevToolsMenuController : MonoBehaviour
    {
        private G2OMVisualisation _debugVisualization;

#pragma warning disable 649
        [SerializeField] private GameObject _gazeModifierViz;

        [SerializeField] private GameObject _unGazeModifierViz;

        [SerializeField] private GameObject _openMenuButton;
        [SerializeField] private GameObject _toolkitMenu;

        [SerializeField] private GameObject _gazeModifierTools;

        [SerializeField] private DevToolsUITriggerGazeToggleButton _enableButton;
        [SerializeField] private DevToolsUITriggerGazeToggleButton _visualizeButton;
        [SerializeField] private DevToolsUITriggerGazeSlider _percentileSlider;
#pragma warning restore 649

        private GazeModifierSettings _settings;
        private bool _visualizersActive = true;
        private bool runonce = false;
        private bool started = false;

        public bool _startWithVisualizers = true;

        [HideInInspector] public bool UsingGazeModifier;
       
        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();   // Wait a sec so the default G2OM instance can be instantiated in the 

            bool settingsExists;
            _settings = GazeModifierSettings.CreateDefaultSettings(out settingsExists);
            UsingGazeModifier = TobiiXR.Provider is GazeModifierProvider;                   
            _settings.EnableGazeModifier = UsingGazeModifier && _settings.EnableGazeModifier; 
            string providerString = TobiiXR.Provider.ToString();
            if (UsingGazeModifier)
            {
                providerString = _settings.EyetrackingProviderType;
            }

            _unGazeModifierViz.GetComponentInChildren<CustomProviderVisualizer>().ChangeProvider(providerString);
            _unGazeModifierViz.GetComponentInChildren<GazeVisualizer.GazeVisualizer>().ScaleAffectedByPrecision = false;

            var cameraTransform = CameraHelper.GetCameraTransform();
            _debugVisualization = cameraTransform.gameObject.AddComponent<G2OMVisualisation>();

            _visualizersActive = _startWithVisualizers;
            SetGazeVisualizer(_visualizersActive);
            SetMasterGazeModifier(_settings.EnableGazeModifier);
            started = true;
            EnsureCorrectVisualizer();
        }

        void LateUpdate()
        {
            if (started)
            {
                EnsureCorrectVisualizer();

                if (_toolkitMenu.activeInHierarchy)
                {
                    CheckMenuSettings();
                }
            }
        }

        public void ShowMenu(bool set)
        {
            _toolkitMenu.SetActive(set);
            _openMenuButton.SetActive(!set);
        }

        public void SetPercentile(int percentile)
        {
            if (_settings == null || !UsingGazeModifier)
            {
                return;
            }

            //Mathf.RoundToInt(value * 100f);
            _settings.SelectedPercentileIndex = percentile;
            if (_toolkitMenu.activeInHierarchy &&
                _percentileSlider.Value != _settings.SelectedPercentileIndex)
            {
                _percentileSlider.SetSliderTo(percentile);
            }
        }

        public void SetMasterGazeModifier(bool set)
        {
            if (_settings == null || !UsingGazeModifier)
            {
                return;
            }
            _settings.EnableGazeModifier = set;
            SetGazeVisualizer(_visualizersActive);
        }

        public void SetGazeVisualizer(bool set)
        {
            _visualizersActive = set;
        }

        private void EnsureCorrectVisualizer()
        {
            if (_visualizersActive && !_settings.EnableGazeModifier)
            {
                _gazeModifierViz.SetActive(false);
                _unGazeModifierViz.SetActive(true);
            }
            else if (_visualizersActive && _settings.EnableGazeModifier)
            {
                _gazeModifierViz.SetActive(true);
                _unGazeModifierViz.SetActive(false);
            }
            else if (!_visualizersActive)
            {
                _gazeModifierViz.SetActive(false);
                _unGazeModifierViz.SetActive(false);
            }
        }

        public void SetG2OMDebugView(bool set)
        {
            _debugVisualization.SetVisualization(set);
        }

        // makes sure that the settings from the Tobii Settings Unity window matches the debug window's settings
        private void CheckMenuSettings()
        {
            if (!runonce)
            {
                // make menu match our tobii xr settings when first opened
                runonce = true;
                _percentileSlider._sliderGraphics.UpdateValueText(
                    _settings.SelectedPercentileIndex); //force color change
                if (!UsingGazeModifier)
                {
                    DisableGazeModifierMenu();
                }
            }

            if (_settings.EnableGazeModifier)
            {
                _enableButton.ToggleOn();
            }
            else
            {
                _enableButton.ToggleOff();
            }

            if (_visualizersActive)
            {
                _visualizeButton.ToggleOn();
            }
            else
            {
                _visualizeButton.ToggleOff();
            }

            if (UsingGazeModifier && _percentileSlider.Value != _settings.SelectedPercentileIndex)
            {
                _percentileSlider.SetSliderTo(_settings.SelectedPercentileIndex);
            }
        }

        private void DisableGazeModifierMenu()
        {
            Color greyedOut = new Color(0, 0, 0, .2f);
            DevToolsUIGazeToggleButtonGraphics[] buttonGraphics = _gazeModifierTools.GetComponentsInChildren<DevToolsUIGazeToggleButtonGraphics>(true);
            DevToolsUIGazeSliderGraphics[] sliderGraphics = _gazeModifierTools.GetComponentsInChildren<DevToolsUIGazeSliderGraphics>(true);
            DevToolsUITriggerGazeToggleButton[] buttonScripts = _gazeModifierTools.GetComponentsInChildren<DevToolsUITriggerGazeToggleButton>(true);
            DevToolsUITriggerGazeSlider[] sliderScripts = _gazeModifierTools.GetComponentsInChildren<DevToolsUITriggerGazeSlider>(true);

            foreach (DevToolsUIGazeToggleButtonGraphics s in buttonGraphics)
            {
                s.StopAllCoroutines();
                s.enabled = false;
            }
            foreach (DevToolsUIGazeSliderGraphics s in sliderGraphics)
            {
                s.StopAllCoroutines();
                s.enabled = false;
            }
            foreach (DevToolsUITriggerGazeToggleButton s in buttonScripts)
            {
                s.enabled = false;
            }
            foreach (DevToolsUITriggerGazeSlider s in sliderScripts)
            {
                s.enabled = false;
            }

            SpriteRenderer[] sprites = _gazeModifierTools.GetComponentsInChildren<SpriteRenderer>(true);
            Image[] images = _gazeModifierTools.GetComponentsInChildren<Image>(true);
            BoxCollider[] colliders = _gazeModifierTools.GetComponentsInChildren<BoxCollider>(true);
            Text[] texts = _gazeModifierTools.GetComponentsInChildren<Text>(true);

            foreach (SpriteRenderer s in sprites)
            {
                s.color = greyedOut;
            }
            foreach (Image i in images)
            {
                if (i.name != "Background")
                {
                    i.color = greyedOut;
                }
            }
            foreach (BoxCollider b in colliders)
            {
                b.enabled = false;
            }
            foreach (Text t in texts)
            {
                t.color = greyedOut;
            }
        }
    }
}
