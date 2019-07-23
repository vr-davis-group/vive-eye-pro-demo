// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    public interface IEyeTrackingProvider
    {
        TobiiXR_EyeTrackingData EyeTrackingData { get; }

        void Tick();

        void Destroy();
    }
}
