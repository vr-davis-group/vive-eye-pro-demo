﻿// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    public interface IG2OM_Context
    {
        bool Setup(int expectedNumberOfObjects);

        G2OM_Error GetCandidateSearchPattern(ref G2OM_DeviceData deviceData, G2OM_Ray[] rays);

        bool Process(ref G2OM_DeviceData deviceData, ref G2OM_RaycastResult raycastResult, int candidateCount, G2OM_Candidate[] candidates, G2OM_CandidateResult[] candidateResults);

        bool Destroy();
    }
}