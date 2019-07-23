// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;
using Tobii.XR.DevTools;
using UnityEngine;

namespace Tobii.XR.GazeModifier
{
    [CreateAssetMenu]
    public class GazeModifierSettings : ScriptableObject, IModifierSettings
    {
        private const string _displayNameNoPercentile = "None";
        private const string _displayNamePrefix = "P";
        private readonly IList<IDisableGazeModifier> _disablers = new List<IDisableGazeModifier>();
        private bool _enableGazeModifier = true;
        private string[] _percentileStrings = { _displayNameNoPercentile };
        private string _eyetrackingProviderType = typeof(NoseDirectionProvider).FullName;
        private int _selectedPercetileIndex;

        public static readonly string GazeModifierSettingsPath = typeof(GazeModifierSettings).Name;
        public static Func<GazeModifierSettings> LoadDefaultSettings = () => Resources.Load<GazeModifierSettings>(GazeModifierSettingsPath);

        [HideInInspector]
        public string EyetrackingProviderType
        {
            get { return _eyetrackingProviderType; }
            set { _eyetrackingProviderType = value; }
        }

        public int SelectedPercentileIndex
        {
            get { return _selectedPercetileIndex; }
            set
            {
                _selectedPercetileIndex = Mathf.Clamp(value,0,NumberOfPercentiles-1);
                SelectedPercentileString = _percentileStrings[_selectedPercetileIndex];
            }
        }

        public int NumberOfPercentiles { get; private set; }

        public string SelectedPercentileString { get; private set; }

        public IList<string> PercentileStrings { get { return _percentileStrings; } }

        public GazeModifierSettings() : this(new PercentileRepository())
        {
           
        }

        public GazeModifierSettings(IPercentileRepository percentileRepository)
        {
            LoadDefaultSettings = () => Resources.Load<GazeModifierSettings>(GazeModifierSettingsPath);
            SetPercentileRepository(percentileRepository);
        }

        public static GazeModifierSettings CreateDefaultSettings(out bool resourceExists)
        {
            var configuration = LoadDefaultSettings != null ? LoadDefaultSettings() : null;
            resourceExists = configuration != null;
            var ret = resourceExists ? configuration : CreateInstance<GazeModifierSettings>();
            return ret;
        }

        public static GazeModifierSettings CreateDefaultSettings()
        {
            var exists = false;
            return CreateDefaultSettings(out exists);
        }

        public void AddDisabler(IDisableGazeModifier disabler)
        {
            _disablers.Add(disabler);
        }

        public bool EnableGazeModifier
        {
            get { return _enableGazeModifier && !_disablers.Any(d => d.Disable); }
            set
            {
                _enableGazeModifier = value;
            }
        }

        private void SetPercentileRepository(IPercentileRepository repoo)
        {
            _percentileStrings = repoo.LoadAll()
                .Concat(new List<PercentileData>() { new PercentileData(0, 0, 0, 1) })
                .Select(v => v.Percentile)
                .Distinct()
                .OrderBy(p => p)
                .Select(v => v > 0 ? _displayNamePrefix + v : _displayNameNoPercentile)
                .ToArray();

            NumberOfPercentiles = _percentileStrings.Length;
        }

    }
}

