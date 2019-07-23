// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using System;
using System.Diagnostics;
using Tobii.StreamEngine;
using UnityEngine;

namespace Tobii.XR
{
    internal static class StreamEngineUtils
    {
        internal static Vector3 ToVector3(this TobiiVector3 vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }
    }

    public class StreamEngineTracker
    {
        private tobii_wearable_data_callback_t _wearableDataCallback; // Needed to prevent GC from removing callback
        private StreamEngineConnection _connection;
        private Stopwatch _stopwatch = new Stopwatch();

        private bool _isReconnecting;
        private float _reconnectionTimestamp;
        private static bool _convergenceDistanceSupported;

        public TobiiXR_EyeTrackingData LocalLatestData { get; private set; }

        public tobii_wearable_data_t LocalWearableData { get; private set; }

        public bool ReceivedDataThisFrame { get; private set; }

        public StreamEngineContext Context
        {
            get
            {
                if (_connection == null) return null;
                return _connection.Context;
            }
        }

        public StreamEngineTracker(StreamEngineTracker_Description description = null, StreamEngineConnection connection = null)
        {
            if (description == null)
            {
                description = new StreamEngineTracker_Description();
            }

            if (connection == null)
            {
                connection = new StreamEngineConnection(new InteropWrapper());
            }

            LocalLatestData = new TobiiXR_EyeTrackingData();

            _connection = connection;

            if (TryConnectToTracker(_connection, _stopwatch, description) == false)
            {
                throw new Exception("Failed to connect to tracker");
            }

            _wearableDataCallback = OnWearableData;
            if (SubscribeToWearableData(_connection.Context.Device, _wearableDataCallback) == false)
            {
                throw new Exception("Failed to subscribe to tracker");
            }

            CheckForCapabilities(_connection.Context.Device);
        }

        public void Tick()
        {
            ReceivedDataThisFrame = false;

            if (_isReconnecting)
            {
                // do not try to reconnect more than once every 500 ms
                if (Time.unscaledTime - _reconnectionTimestamp < 0.5f) return;

                var connected = _connection.TryReconnect();
                _isReconnecting = !connected;
                return;
            }
            var result = ProcessCallback(_connection.Context.Device, _stopwatch);
            if (result == tobii_error_t.TOBII_ERROR_CONNECTION_FAILED)
            {
                UnityEngine.Debug.Log("Reconnecting...");
                _reconnectionTimestamp = Time.unscaledTime;
                _isReconnecting = true;
            }
        }

        public void Destroy()
        {
            if (_connection == null) return;
            if (_connection.Context == null) return;

            var url = _connection.Context.Url;
            _connection.Close();

            UnityEngine.Debug.Log(string.Format("Disconnected from {0}", url));

            _stopwatch = null;
        }

        private void OnWearableData(ref tobii_wearable_data_t data)
        {
            LocalWearableData = data;
            CopyEyeTrackingData(LocalLatestData, ref data);
            ReceivedDataThisFrame = true;
        }

        private static tobii_error_t ProcessCallback(IntPtr deviceContext, Stopwatch stopwatch)
        {
            StartStopwatch(stopwatch);
            var result = Interop.tobii_device_process_callbacks(deviceContext);
            var milliseconds = StopStopwatch(stopwatch);

            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to process callback. Error {0}", result));
            }

            if (milliseconds > 1)
            {
                UnityEngine.Debug.LogWarning(string.Format("Process callbacks took {0}ms", milliseconds));
            }

            return result;
        }

        private static void CopyEyeTrackingData(TobiiXR_EyeTrackingData latestDataLocalSpace, ref tobii_wearable_data_t data)
        {
            latestDataLocalSpace.GazeRay.IsValid = data.gaze_direction_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID && data.gaze_origin_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            latestDataLocalSpace.GazeRay.Origin.x = data.gaze_origin_combined_mm_xyz.x * -1 / 1000f;
            latestDataLocalSpace.GazeRay.Origin.y = data.gaze_origin_combined_mm_xyz.y / 1000f;
            latestDataLocalSpace.GazeRay.Origin.z = data.gaze_origin_combined_mm_xyz.z / 1000f;
            latestDataLocalSpace.GazeRay.Direction.x = data.gaze_direction_combined_normalized_xyz.x * -1;
            latestDataLocalSpace.GazeRay.Direction.y = data.gaze_direction_combined_normalized_xyz.y;
            latestDataLocalSpace.GazeRay.Direction.z = data.gaze_direction_combined_normalized_xyz.z;

            if (_convergenceDistanceSupported)
            {
                latestDataLocalSpace.ConvergenceDistance = data.convergence_distance_mm / 1000f;
                latestDataLocalSpace.ConvergenceDistanceIsValid = data.convergence_distance_validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            }
            else
            {

                if (data.left.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_INVALID || data.right.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_INVALID)
                {
                    latestDataLocalSpace.ConvergenceDistanceIsValid = false;
                }
                else
                {
                    var convergenceDistance_mm = Convergence.CalculateDistance(
                        data.left.gaze_origin_mm_xyz.ToVector3(),
                        data.left.gaze_direction_normalized_xyz.ToVector3(),
                        data.right.gaze_origin_mm_xyz.ToVector3(),
                        data.right.gaze_direction_normalized_xyz.ToVector3()
                        );
                    latestDataLocalSpace.ConvergenceDistance = convergenceDistance_mm / 1000f;
                    latestDataLocalSpace.ConvergenceDistanceIsValid = true;
                }

            }

            latestDataLocalSpace.IsLeftEyeBlinking = data.left.eye_openness_validity == tobii_validity_t.TOBII_VALIDITY_INVALID || Mathf.Approximately(data.left.eye_openness, 0f);
            latestDataLocalSpace.IsRightEyeBlinking = data.right.eye_openness_validity == tobii_validity_t.TOBII_VALIDITY_INVALID || Mathf.Approximately(data.right.eye_openness, 0f);
        }

        private static long StopStopwatch(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private static void StartStopwatch(Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        private static bool TryConnectToTracker(StreamEngineConnection connection, Stopwatch stopwatch, StreamEngineTracker_Description description)
        {
            StartStopwatch(stopwatch);

            if (connection.Open(description) == false)
            {
                return false;
            }

            var elapsedTime = StopStopwatch(stopwatch);

            UnityEngine.Debug.Log(string.Format("Connected to SE tracker: {0} and it took {1}ms", connection.Context.Url, elapsedTime));
            return true;
        }

        private static bool SubscribeToWearableData(IntPtr context, tobii_wearable_data_callback_t wearableDataCallback)
        {
            var result = Interop.tobii_wearable_data_subscribe(context, wearableDataCallback);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.LogError("Failed to subscribe to wearable stream." + result);
            return false;
        }

        private static void CheckForCapabilities(IntPtr context)
        {
            bool supported;
            Interop.tobii_capability_supported(context, tobii_capability_t.TOBII_CAPABILITY_COMPOUND_STREAM_WEARABLE_CONVERGENCE_DISTANCE, out supported);
            _convergenceDistanceSupported = false;
        }
    }
}