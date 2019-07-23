// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using Tobii.XR.DevTools;

namespace Tobii.XR.GazeModifier
{
    public interface IModifierSettings
    {
        string EyetrackingProviderType { get; }
      
        int SelectedPercentileIndex { get; }

        void AddDisabler(IDisableGazeModifier disabler);

        bool EnableGazeModifier { get; set; }

        int NumberOfPercentiles { get; }

        string SelectedPercentileString { get; }
    }
}