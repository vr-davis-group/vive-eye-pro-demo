// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    using UnityEngine;
    using System;
    using System.Runtime.InteropServices;

    public static class Interop
    {
        public const string g2om_lib = "tobii_g2om";

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_context_create")]
        public static extern G2OM_Error G2OM_ContextCreate(out IntPtr context, uint initialCapacity);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_process")]
        public static extern G2OM_Error G2OM_Process(IntPtr context, ref G2OM_DeviceData deviceData, ref G2OM_RaycastResult raycastResult, uint candidatesCount, G2OM_Candidate[] candidates, G2OM_CandidateResult[] candidateResults);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_context_destroy")]
        public static extern G2OM_Error G2OM_ContextDestroy(ref IntPtr context);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_set_max_thread_count")]
        public static extern G2OM_Error G2OM_SetMaxThreadCount(IntPtr context, G2OM_ThreadCount threadCount);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_get_version")]
        public static extern G2OM_Error G2OM_GetVersion(out G2OM_Version version);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_get_worldspace_corner_of_candidate")]
        public static extern G2OM_Error G2OM_GetWorldspaceCornerOfCandidate(ref G2OM_Candidate candidate, uint numberOfCorners, G2OM_Vector3[] cornersOfCandidateInWorldSpace);

        [DllImport(g2om_lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "g2om_get_candidate_search_pattern")]
        public static extern G2OM_Error G2OM_GetCandidateSearchPattern(IntPtr context, ref G2OM_Vector3 worldSpaceUpDirection, ref G2OM_Vector3 worldSpaceRightDirection, ref G2OM_DeviceData deviceData, uint numberOfRays, G2OM_Ray[] mutatedRays);
    }

    public enum G2OM_Error
    {
        Ok = 0,
        NullPointerPassed = -1,
        G2OMInternalError = -10,
        InvalidIndex = -20,
        IndexOutOfBounds = -30,
        NotImplemented = -1000,
    }

    public enum G2OM_ThreadCount
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_DeviceData
    {
        public float timestamp;          // The real world timestamp. Must always be equal or higher than previous timestamp
        public G2OM_GazeRay combined;       // TODO: Clarify what space they should be in
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_GazeRay
    {
        public G2OM_Ray ray;
        private byte is_valid;    // To be combined to one variable

        public bool IsValid
        {
            get
            {
                return is_valid.ToBool();
            }
        }

        public G2OM_GazeRay(G2OM_Ray gazeRay, bool isValid)
        {
            ray = gazeRay;
            is_valid = isValid.ToByte();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_RaycastResult
    {
        public G2OM_Raycast combined;   // What object id did the combined ray hit
        public G2OM_Raycast left;       // What object id did the left ray hit
        public G2OM_Raycast right;      // What object id did the right ray hit
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Raycast
    {
        private byte did_raycast_hit_a_candidate;
        private UInt64 candidate_id;

        public G2OM_Raycast(bool didRaycastHitACandidate, int candidateId)
        {
            did_raycast_hit_a_candidate = didRaycastHitACandidate.ToByte();
            candidate_id = (UInt64)candidateId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Candidate
    {
        private UInt64 id;
        public G2OM_Vector3 aabbMaxLocalSpace;
        public G2OM_Vector3 aabbMinLocalSpace;
        public G2OM_Matrix4x4 worldToLocalMatrix;
        public G2OM_Matrix4x4 localToWorldMatrix;

        public int Id
        {
            get
            {
                return (int)id;
            }
        }

        public G2OM_Candidate(int candidateId, Vector3 max, Vector3 min, Matrix4x4 worldToLocal, Matrix4x4 localToWorld)
        {
            id = (UInt64)candidateId;
            aabbMaxLocalSpace = new G2OM_Vector3(max);
            aabbMinLocalSpace = new G2OM_Vector3(min);
            worldToLocalMatrix = new G2OM_Matrix4x4(worldToLocal);
            localToWorldMatrix = new G2OM_Matrix4x4(localToWorld);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Matrix4x4
    {
        public float m00;
        public float m10;
        public float m20;
        public float m30;
        public float m01;
        public float m11;
        public float m21;
        public float m31;
        public float m02;
        public float m12;
        public float m22;
        public float m32;
        public float m03;
        public float m13;
        public float m23;
        public float m33;

        public G2OM_Matrix4x4(Matrix4x4 matrix)
        {
            m00 = matrix.m00;
            m01 = matrix.m01;
            m02 = matrix.m02;
            m03 = matrix.m03;
            m10 = matrix.m10;
            m11 = matrix.m11;
            m12 = matrix.m12;
            m13 = matrix.m13;
            m20 = matrix.m20;
            m21 = matrix.m21;
            m22 = matrix.m22;
            m23 = matrix.m23;
            m30 = matrix.m30;
            m31 = matrix.m31;
            m32 = matrix.m32;
            m33 = matrix.m33;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Vector3
    {
        public float x;
        public float y;
        public float z;

        public G2OM_Vector3(Vector3 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        public Vector3 Vector
        {
            get
            {
                return new Vector3(x, y, z);
            }
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_CandidateResult
    {
        private UInt64 id;
        public float score;                             // A score between, 0 -> 1

        public G2OM_GazeRay adjustedCombinedRay;     // An adjusted ray that intersects the collider data provided.

        public G2OM_GazeRay adjustedLeftRay;         // An adjusted ray that intersects the collider data provided.

        public G2OM_GazeRay adjustedRightRay;        // An adjusted ray that intersects the collider data provided.

        public int Id
        {
            get
            {
                return (int)id;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Ray
    {
        public G2OM_Vector3 origin;
        public G2OM_Vector3 direction;

        public G2OM_Ray(Vector3 rayOrigin, Vector3 rayDirection)
        {
            origin = new G2OM_Vector3(rayOrigin);
            direction = new G2OM_Vector3(rayDirection);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct G2OM_Version
    {
        public uint major;
        public uint minor;
        public uint buildVersion;
    }

    public static class G2OM_ExtensionMethods
    {
        public static byte ToByte(this bool b)
        {
            return b ? (byte)1 : (byte)0;
        }

        public static bool ToBool(this byte b)
        {
            return b == 1;
        }

        public static Ray ToUnityRay(this G2OM_Ray ray)
        {
            return new Ray(ray.origin.Vector, ray.direction.Vector);
        }

        public static Ray ToUnityRay(this G2OM_GazeRay gazeRay)
        {
            return gazeRay.ray.ToUnityRay();
        }
    }

    public enum Corners
    {
        FLL = 0,    // Front Lower Left
        FUL,        // Front Upper Left
        FUR,        // Front Upper Right
        FLR,        // Front Lower Right
        BLL,        // Back Lower Left
        BUL,        // Back Upper Left
        BUR,        // Back Upper Right
        BLR,        // Back Lower Right
        NumberOfCorners
    }
}