// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    using System.Collections.Generic;
    using UnityEngine;

    public class G2OM_ObjectFinder : IG2OM_ObjectFinder
    {
        public bool ShouldDrawDebugRays;
        private const int RaysPerSecond = 900;
        private const int MinimumRaysPerFrame = 3;
        private const int MaxRaysPerFrame = 15;
        private const float RaycastLength = float.MaxValue;

        private readonly G2OM_Ray[] _rays = new G2OM_Ray[MaxRaysPerFrame];
        private LayerMask _layerMask = ~0;
        private IG2OM_Context _context;
        private float _previousTimestamp;

        public G2OM_ObjectFinder(float now = 0)
        {
            _previousTimestamp = now;
        }

        public void GetRelevantGazeObjects(ref G2OM_DeviceData deviceData, Dictionary<int, GameObject> foundObjects, IG2OM_ObjectDistinguisher distinguisher)
        {
            var result = _context.GetCandidateSearchPattern(ref deviceData, _rays);
            if(result != G2OM_Error.Ok) {
                Debug.LogError("G2OM_GetMutatedRays failed with error: " + result);
                return;
            }

            var numberOfRaysThisFrame = GetNumberOfRays(deviceData.timestamp - _previousTimestamp);
            _previousTimestamp = deviceData.timestamp;

#if UNITY_EDITOR
            if (ShouldDrawDebugRays) DrawDebugRays(ref deviceData, _rays, numberOfRaysThisFrame);
#endif

            FindObjects(_rays, foundObjects, distinguisher, _layerMask, numberOfRaysThisFrame);
        }

        // TODO: Is it possible to remove the redudant raycasts?
        public G2OM_RaycastResult GetRaycastResult(ref G2OM_DeviceData deviceData,
            IG2OM_ObjectDistinguisher distinguisher)
        {
            var raycastResult = new G2OM_RaycastResult();

            GameObject go;
            var result = FindGameObject(ref deviceData.combined, _layerMask, out go);
            if (result)
            {
                var id = go.GetInstanceID();
                var hitACandidate = distinguisher.IsGameObjectGazeFocusable(id, go);
                raycastResult.combined = new G2OM_Raycast(hitACandidate, id);
            }

            return raycastResult;
        }

        public void Setup(IG2OM_Context context, LayerMask layerMask)
        {
            _layerMask = layerMask;
            _context = context;
        }

        private static void DrawDebugRays(ref G2OM_DeviceData deviceData, G2OM_Ray[] rays, int numberOfRaysThisFrame)
        {
            var numberOfInitialRays = 0;
            if (deviceData.combined.IsValid) numberOfInitialRays++;

            for (int i = 0; i < numberOfInitialRays; i++)
            {
                Debug.DrawRay(rays[i].origin.Vector, rays[i].direction.Vector * 100, Color.red);
            }

            for (int i = numberOfInitialRays; i < numberOfRaysThisFrame; i++)
            {
                if(rays[i].direction.Vector.sqrMagnitude == 0) break;

                Debug.DrawRay(rays[i].origin.Vector, rays[i].direction.Vector * 100, Color.green);
            }
        }

        private static int GetNumberOfRays(float dt)
        {
            var rays = Mathf.CeilToInt(RaysPerSecond * dt);
            return Mathf.Clamp(rays, MinimumRaysPerFrame, MaxRaysPerFrame);
        }

        private static void FindObjects(G2OM_Ray[] rays, Dictionary<int, GameObject> foundObjects,
            IG2OM_ObjectDistinguisher distinguisher, LayerMask layerMask, int numberOfRaysThisFrame)
        {
            foundObjects.Clear();

            for(int i = 0; i < numberOfRaysThisFrame; i++)
            {
                if(rays[i].direction.Vector.sqrMagnitude == 0) break;

                GameObject go;
                if (FindGameObject(rays[i], layerMask, out go) == false) continue;

                var id = go.GetInstanceID();
                if (foundObjects.ContainsKey(id)) continue;

                if (distinguisher.IsGameObjectGazeFocusable(id, go) == false) continue;

                foundObjects.Add(id, go);
            }
        }

        private static bool FindGameObject(ref G2OM_GazeRay gazeRay, LayerMask layerMask, out GameObject gameObject)
        {
            gameObject = null;
            return gazeRay.IsValid && FindGameObject(gazeRay.ray, layerMask, out gameObject);
        }

        private static bool FindGameObject(G2OM_Ray ray, LayerMask layerMask, out GameObject gameObject)
        {
            gameObject = null;
            RaycastHit hit;
            if (Physics.Raycast(ray.origin.Vector, ray.direction.Vector, out hit, RaycastLength, layerMask) == false) return false;

            gameObject = hit.collider.gameObject;
            return true;
        }
    }
}