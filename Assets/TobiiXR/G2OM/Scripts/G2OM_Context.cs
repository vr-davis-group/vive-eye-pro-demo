// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    using System;
    using UnityEngine;

    public class G2OM_Context : IG2OM_Context
    {
        private IntPtr _context;
        
        private G2OM_Vector3 _up = new G2OM_Vector3(Vector3.up);
        private G2OM_Vector3 _right = new G2OM_Vector3(Vector3.right);

        public bool Setup(int expectedNumberOfObjects)
        {
            var result = Interop.G2OM_ContextCreate(out _context, (uint)expectedNumberOfObjects);
            if (result == G2OM_Error.Ok)
            {
                Debug.Log("Created G2OM context.");
                return true;
            }

            Debug.LogError("Failed to create G2OM context. Error code " + result);
            return false;
        }

        public bool Process(ref G2OM_DeviceData deviceData, ref G2OM_RaycastResult raycastResult, int candidateCount, G2OM_Candidate[] candidates, G2OM_CandidateResult[] candidateResults)
        {
            var result = Interop.G2OM_Process(_context, ref deviceData, ref raycastResult, (uint)candidateCount, candidates, candidateResults);
            if (result == G2OM_Error.Ok) return true;

            Debug.LogError("Failed to process G2OM. Error code " + result);
            return false;
        }

        public bool Destroy()
        {
            if (_context == IntPtr.Zero) return true;

            var result = Interop.G2OM_ContextDestroy(ref _context);
            _context = IntPtr.Zero;

            if (result == G2OM_Error.Ok)
            {
                Debug.Log("Destroyed G2OM context.");
                return true;
            }

            Debug.LogError("Failed to destroy G2OM context. Error code " + result);
            return false;
        }

        public G2OM_Error GetCandidateSearchPattern(ref G2OM_DeviceData deviceData, G2OM_Ray[] rays)
        {
            return Interop.G2OM_GetCandidateSearchPattern(_context, ref _up, ref _right, ref deviceData, (uint)rays.Length, rays);
        }
    }
}