// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using UnityEngine;
using System.Collections.Generic;

namespace Tobii.XR.GazeModifier
{
    [ProviderDisplayName("Gaze Modifier Provider")]
    public class GazeModifierProvider : IGazeModifierProvider
    {
        private  IEyeTrackingProvider _provider;
        private readonly IEnumerable<IGazeModifier> _modifiers;
        private readonly IModifierSettings _settings;
        private readonly ICameraHelper _camera;
        private TobiiXR_EyeTrackingData _eyetrackingData = new TobiiXR_EyeTrackingData();
        private readonly EyeTrackingDataHelper _dataHelper = new EyeTrackingDataHelper();
        private AccuracyModifier _accuracyModifier;
        private PrecisionModifier _precisionModifier;

        public TobiiXR_EyeTrackingData EyeTrackingData { get { return _eyetrackingData; } }

        public GazeModifierProvider() : this(TobiiXR_Settings.GetProvider(AssemblyUtils.EyetrackingProviderType(GazeModifierSettings.CreateDefaultSettings().EyetrackingProviderType)), GazeModifierSettings.CreateDefaultSettings(),new CameraHelper())
        {
           
        }
        public GazeModifierProvider(IEyeTrackingProvider provider, ICameraHelper camera = null) : this(provider, GazeModifierSettings.CreateDefaultSettings(), camera, null)
        {
          
        }

        public GazeModifierProvider(IEyeTrackingProvider provider, IModifierSettings settings, ICameraHelper camera, IEnumerable<IGazeModifier> modifiers = null)
        {
            var repo = new PercentileRepository();
            _provider = provider;
            _settings = settings;
            _camera = camera ?? new CameraHelper();
            _accuracyModifier = new AccuracyModifier(repo, _settings);
            _precisionModifier = new PrecisionModifier(repo, _settings);
            _modifiers = modifiers ?? new List<IGazeModifier>() { _accuracyModifier, _precisionModifier, new TrackabilityModifier(repo, _settings) };
        }


        public void Tick()
        {
            _provider.Tick();
            _dataHelper.CopyGazeDirection(_provider.EyeTrackingData, EyeTrackingData);
            if (_settings.EnableGazeModifier)
            {
                foreach (var gazeModifier in _modifiers)
                {
                    _eyetrackingData = gazeModifier.Modify(EyeTrackingData, _camera.Forward);
                }
            }
        }

        public void Destroy()
        {
            if (_provider == null) return;

            _provider.Destroy();
            _provider = null;
        }

        public TobiiXR_EyeTrackingData AccuracyOnlyModifiedEyeTrackingData
        {
            get
            {
                return _accuracyModifier != null ? _accuracyModifier.Modify(_provider.EyeTrackingData, _camera.Forward) : new TobiiXR_EyeTrackingData();
            }
        }

        public float MaxPrecisionAngleDegrees
        {
            get
            {
                return _precisionModifier != null ? _precisionModifier.GetMaxAngle(_provider.EyeTrackingData.GazeRay.Direction,_camera.Forward): 0;
            }
        }
    }
}
