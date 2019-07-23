// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR;

namespace Tobii.XR
{
    public class HmdToWorldTransformer
    {
        struct Sample
        {
            public long timestamp_us;
            public Matrix4x4 matrix;
        }

        private const int _estimatedPrediction_us = 33000;
        private Transform _headTransform;
        private Sample[] _history;
        private int _writeIndex = 0;

        public HmdToWorldTransformer(int historyCapacity)
        {
            _history = new Sample[historyCapacity];
            _headTransform = CreateNewHmdOrigin(GetType().Name);
        }

        public void Tick()
        {
            _history[_writeIndex].timestamp_us = Stopwatch.GetTimestamp() / 10 + _estimatedPrediction_us;
            _history[_writeIndex].matrix = GetHmdTransformMatrix();
            _writeIndex = (_writeIndex + 1) % _history.Length;
        }

        // Use this function if reliable time synchronization for the eye tracking has not been setup.
        public Matrix4x4 GetTransformMatrixFrom(int framesBefore)
        {
            if (framesBefore >= _history.Length) throw new System.Exception(string.Format("History capacity is configured to {0} frames, but frame {1} was requested", _history.Length, framesBefore));
            var sample = _history[(_history.Length + _writeIndex - 1 - framesBefore) % _history.Length];
            if (sample.timestamp_us == 0) return GetHmdTransformMatrix();

            return sample.matrix;
        }

        // This function requires that the eye tracking timestamp is reliable over time (synchronized with host system)
        public Matrix4x4 GetTransformMatrixClosestTo(long timestamp_us)
        {
            var closestBeforeSample = _history[_writeIndex];
            var closestAfterSample = _history[(_history.Length + _writeIndex - 1) % _history.Length];
            if (closestAfterSample.timestamp_us == 0)
                return GetHmdTransformMatrix(); // Should only happen first frame

            foreach (Sample sample in _history)
            {
                if (sample.timestamp_us == 0) continue;
                if (sample.timestamp_us < timestamp_us)
                {
                    if (timestamp_us - sample.timestamp_us < timestamp_us - closestBeforeSample.timestamp_us) closestBeforeSample = sample;
                }
                else
                {
                    if (sample.timestamp_us - timestamp_us < closestAfterSample.timestamp_us - timestamp_us) closestAfterSample = sample;
                }
            }

            if (closestBeforeSample.timestamp_us == closestAfterSample.timestamp_us)
            {
                return closestAfterSample.matrix;
            }

            var weight = (float)(timestamp_us - closestBeforeSample.timestamp_us) / (float)(closestAfterSample.timestamp_us - closestBeforeSample.timestamp_us);
            var result = Lerp(closestBeforeSample.matrix, closestAfterSample.matrix, weight);

            return result;
        }

        public void Destroy()
        {
            if (_headTransform == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(_headTransform.gameObject);
            }
            else
            {
                Object.DestroyImmediate(_headTransform.gameObject);
            }
#else
            Object.Destroy(_headTransform.gameObject);
#endif
            _headTransform = null;
        }

        private Matrix4x4 GetHmdTransformMatrix()
        {
            if (_headTransform != null) return _headTransform.localToWorldMatrix;

            UnityEngine.Debug.Log("Missing HMD Origin, creating a new instance");
            _headTransform = CreateNewHmdOrigin(GetType().Name);
            return _headTransform != null ? _headTransform.localToWorldMatrix : Matrix4x4.identity;
        }

        private static Transform CreateNewHmdOrigin(string name)
        {
            var cameraTransform = CameraHelper.GetCameraTransform();
            if (cameraTransform == null) return null;

            var headTransform = new GameObject(string.Format("HmdOrigin_{0}", name)).transform;
            headTransform.parent = cameraTransform;
            headTransform.localScale = Vector3.one;
            headTransform.localRotation = Quaternion.identity;

            // This compensates for the main camera (XRNode.Eye) in Unity is not being in the hmd origin (XRNode.Head).
            headTransform.localPosition = InputTracking.GetLocalPosition(XRNode.CenterEye) - InputTracking.GetLocalPosition(XRNode.Head);

            return headTransform;
        }

        private static Matrix4x4 Lerp(Matrix4x4 from, Matrix4x4 to, float weight)
        {
            var result = new Matrix4x4();
            for (var i = 0; i < 16; i++) result[i] = Mathf.Lerp(from[i], to[i], weight);
            return result;
        }
    }
}