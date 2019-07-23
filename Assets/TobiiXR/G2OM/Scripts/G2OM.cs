// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.G2OM
{
    using System.Collections.Generic;
    using UnityEngine;

    public class G2OM_Description
    {
        public const int DefaultExpectedNumberOfObjects = 10;
        public const float DefaultCandidateMemoryInSeconds = 1f;
        public const int DefaultLayerMask = ~0;

        public int ExpectedNumberOfObjects = DefaultExpectedNumberOfObjects;
        public float HowLongToKeepCandidatesInSeconds = DefaultCandidateMemoryInSeconds;

        public LayerMask LayerMask = DefaultLayerMask;
        public IG2OM_ObjectFinder ObjectFinder = new G2OM_ObjectFinder();
        public IG2OM_ColliderDataProvider ColliderDataProvider = new G2OM_ColliderDataProvider();
        public IG2OM_ObjectDistinguisher Distinguisher = new G2OM_ObjectDistinguisher<IGazeFocusable>();
        public IG2OM_PostTicker PostTicker = new G2OM_PostTicker();
        public IG2OM_Context Context = new G2OM_Context();
    }

    public struct InternalCandidate
    {
        public GameObject GameObject;
        public float Timestamp;
    }

    public struct FocusedCandidate
    {
        public GameObject GameObject;

        // This is combined
        public bool IsRayValid;
        public Vector3 Origin;
        public Vector3 Direction;
    }

    public class G2OM
    {
        public static G2OM Create(G2OM_Description description)
        {
            return new G2OM(description);
        }

        public static G2OM Create()
        {
            return new G2OM(new G2OM_Description());
        }

        public float MaxHistoryInSeconds { get { return _howLongToKeepCandidatesInMemory; } }

        public int TotalNumberOfGazeObjects { get { return _internalCandidates.Count; } }

        public int NumberOfGazeFocusedObjects { get { return _gazeFocusedObjects.Count; } }

        public List<FocusedCandidate> GazeFocusedObjects { get { return _gazeFocusedObjects; } }

        private readonly IG2OM_Context _context;
        private readonly IG2OM_ObjectFinder _objectFinder;
        private readonly IG2OM_ColliderDataProvider _colliderDataProvider;
        private readonly IG2OM_ObjectDistinguisher _objectDistinguisher;
        private readonly IG2OM_PostTicker _postTicker;

        private readonly Dictionary<int, InternalCandidate> _internalCandidates;

        private readonly float _howLongToKeepCandidatesInMemory;

        private readonly Dictionary<int, GameObject> _newCandidates;
        private readonly List<int> _keysToRemove;

        private readonly List<FocusedCandidate> _gazeFocusedObjects;

        private G2OM_DeviceData _deviceData;
        private G2OM_Candidate[] _nativeCandidates;
        private G2OM_CandidateResult[] _nativeCandidatesResult;

        private G2OM(G2OM_Description description)
        {
            var expectedNumberOfCandidates = description.ExpectedNumberOfObjects;

            _context = description.Context;
            _context.Setup(expectedNumberOfCandidates); // TODO: What if this fails???

            _howLongToKeepCandidatesInMemory = description.HowLongToKeepCandidatesInSeconds;

            _objectFinder = description.ObjectFinder;
            _objectFinder.Setup(_context, description.LayerMask);

            _colliderDataProvider = description.ColliderDataProvider;
            _objectDistinguisher = description.Distinguisher;
            _postTicker = description.PostTicker;

            _newCandidates = new Dictionary<int, GameObject>(expectedNumberOfCandidates);
            _internalCandidates = new Dictionary<int, InternalCandidate>(expectedNumberOfCandidates);

            _gazeFocusedObjects = new List<FocusedCandidate>(expectedNumberOfCandidates);
            _keysToRemove = new List<int>(expectedNumberOfCandidates);

            _nativeCandidates = new G2OM_Candidate[expectedNumberOfCandidates];
            _nativeCandidatesResult = new G2OM_CandidateResult[expectedNumberOfCandidates];
        }

        public void Tick(G2OM_DeviceData deviceData)
        {
            _deviceData = deviceData;
            var now = _deviceData.timestamp;

            // Get, add and remove candidates
            _objectFinder.GetRelevantGazeObjects(ref _deviceData, _newCandidates, _objectDistinguisher);

            AddNewCandidates(now, _internalCandidates, _newCandidates);

            RemoveOldCandidates(now, _internalCandidates, _howLongToKeepCandidatesInMemory, _keysToRemove);

            // Ensure capacity is enough
            if (_internalCandidates.Count > _nativeCandidates.Length)
            {
                _nativeCandidates = new G2OM_Candidate[_internalCandidates.Count];
                _nativeCandidatesResult = new G2OM_CandidateResult[_internalCandidates.Count];
            }

            // Prepare structs to be sent to G2OM
            _colliderDataProvider.GetColliderData(_internalCandidates, _nativeCandidates);

            var raycastResult = _objectFinder.GetRaycastResult(ref _deviceData, _objectDistinguisher);

            _context.Process(ref _deviceData, ref raycastResult, _internalCandidates.Count, _nativeCandidates, _nativeCandidatesResult); // TODO: What to do if this call fails??

            // Process the result from G2OM
            UpdateListOfFocusedCandidates(_nativeCandidatesResult, _internalCandidates, _gazeFocusedObjects, _objectFinder);

            _postTicker.TickComplete(_gazeFocusedObjects);
        }

        public void Clear()
        {
            _internalCandidates.Clear();
        }

        public void Destroy()
        {
            _context.Destroy();
        }

        public G2OM_Candidate[] GetCandidates()
        {
            var array = new G2OM_Candidate[_internalCandidates.Count];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _nativeCandidates[i];
            }

            return array;
        }

        public G2OM_CandidateResult[] GetCandidateResult()
        {
            var array = new G2OM_CandidateResult[_internalCandidates.Count];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _nativeCandidatesResult[i];
            }

            return array;
        }

        public G2OM_DeviceData GetDeviceData()
        {
            return _deviceData;
        }

        public GameObject GetGameObjectForCandidate(int id)
        {
            return _internalCandidates[id].GameObject;
        }

        private static void AddNewCandidates(float now, Dictionary<int, InternalCandidate> allCandidates, Dictionary<int, GameObject> newCandidates)
        {
            foreach (var newCandidate in newCandidates)
            {
                var id = newCandidate.Key;
                var candidate = allCandidates.ContainsKey(id) ? allCandidates[id] : new InternalCandidate { GameObject = newCandidate.Value };
                candidate.Timestamp = now;
                allCandidates[id] = candidate;
            }
        }

        private static void RemoveOldCandidates(float now, Dictionary<int, InternalCandidate> allCandidates, float maxHistory, List<int> keysToRemove)
        {
            keysToRemove.Clear();

            foreach (var candidate in allCandidates)
            {
                var internalCandidate = candidate.Value;
                var diff = now - internalCandidate.Timestamp;
                if (diff <= maxHistory && internalCandidate.GameObject != null) continue;

                keysToRemove.Add(candidate.Key);
            }

            foreach (var key in keysToRemove)
            {
                allCandidates.Remove(key);
            }
        }

        private static void UpdateListOfFocusedCandidates(G2OM_CandidateResult[] result, Dictionary<int, InternalCandidate> allCandidates, List<FocusedCandidate> focusedObjects, IG2OM_ObjectFinder objectFinder)
        {
            focusedObjects.Clear();

            for (int i = 0; i < allCandidates.Count; i++)
            {
                var candidate = result[i];

                if (candidate.score <= Mathf.Epsilon) break;

                var gazeFocusedObject = new FocusedCandidate
                {
                    GameObject = allCandidates[candidate.Id].GameObject,
                    IsRayValid = candidate.adjustedCombinedRay.IsValid,
                    Direction = candidate.adjustedCombinedRay.ray.direction.Vector,
                    Origin = candidate.adjustedCombinedRay.ray.origin.Vector,
                };

                focusedObjects.Add(gazeFocusedObject);
            }
        }
    }
}