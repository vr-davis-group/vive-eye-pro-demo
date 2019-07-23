// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    public interface IGazeModifierProvider : IEyeTrackingProvider
    {
        TobiiXR_EyeTrackingData AccuracyOnlyModifiedEyeTrackingData { get; }
        float MaxPrecisionAngleDegrees { get; }
    }
}
