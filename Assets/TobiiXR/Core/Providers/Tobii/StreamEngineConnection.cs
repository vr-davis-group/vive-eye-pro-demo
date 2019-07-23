using System;
using System.Collections.Generic;
using Tobii.StreamEngine;

namespace Tobii.XR
{
    public class StreamEngineConnection
    {
        private readonly IStreamEngineInterop _interop;

        public StreamEngineConnection(IStreamEngineInterop interop)
        {
            _interop = interop;
        }

        public StreamEngineContext Context { get; private set; }

        public bool Open(StreamEngineTracker_Description description)
        {
            IntPtr apiContext;
            if (Context != null) throw new InvalidOperationException("There is already an instantiated connection");

            if (CreateApiContext(_interop, out apiContext) == false) return false;

            List<string> connectedDevices;
            if (GetAvailableTrackers(_interop, apiContext, out connectedDevices) == false)
            {
                DestroyApiContext(_interop, apiContext);
                return false;
            }

            IntPtr deviceContext;
            string hmdEyeTrackerUrl;
            if (GetFirstSupportedTracker(_interop, apiContext, connectedDevices, description, out deviceContext, out hmdEyeTrackerUrl) == false)
            {
                DestroyApiContext(_interop, apiContext);
                return false;
            }

            Context = new StreamEngineContext(apiContext, deviceContext, hmdEyeTrackerUrl);
            return true;
        }

        public void Close()
        {
            if (Context == null) return;

            DestroyDeviceContext(_interop, Context.Device);
            DestroyApiContext(_interop, Context.Api);

            Context = null;
        }

        public bool TryReconnect()
        {
            if (Context == null) throw new InvalidOperationException("No valid connection to retry");
            return ReconnectToDevice(_interop, Context.Device);
        }

        private static bool CreateDeviceContext(IStreamEngineInterop interop, string url, IntPtr apiContext, string[] licenseKeys, out IntPtr deviceContext)
        {
            var licenseResults = new List<tobii_license_validation_result_t>();
            var result = interop.tobii_device_create_ex(apiContext, url, licenseKeys, licenseResults, out deviceContext);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            for (int i = 0; i < licenseKeys.Length; i++)
            {
                var licenseResult = licenseResults[i];
                if (licenseResult == tobii_license_validation_result_t.TOBII_LICENSE_VALIDATION_RESULT_OK) continue;

                UnityEngine.Debug.LogError("License " + licenseKeys[i] + " failed. Return code " + licenseResult);
            }

            UnityEngine.Debug.LogError(string.Format("Failed to create device context for {0}. {1}", url, result));
            return false;
        }

        private static void DestroyDeviceContext(IStreamEngineInterop interop, IntPtr deviceContext)
        {
            if (deviceContext == IntPtr.Zero) return;

            var result = interop.tobii_device_destroy(deviceContext);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to destroy device context. Error {0}", result));
            }
        }

        private static bool CreateApiContext(IStreamEngineInterop interop, out IntPtr apiContext)
        {
            var result = interop.tobii_api_create(out apiContext, null);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.LogError("Failed to create api context. " + result);
            apiContext = IntPtr.Zero;
            return false;
        }

        private static void DestroyApiContext(IStreamEngineInterop interop, IntPtr apiContext)
        {
            if (apiContext == IntPtr.Zero) return;

            var result = interop.tobii_api_destroy(apiContext);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to destroy api context. Error {0}", result));
            }
        }

        private static bool GetAvailableTrackers(IStreamEngineInterop interop, IntPtr apiContext, out List<string> connectedDevices)
        {
            var result = interop.tobii_enumerate_local_device_urls(apiContext, out connectedDevices);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError("Failed to enumerate connected devices. " + result);
                return false;
            }

            if (connectedDevices.Count >= 1) return true;

            UnityEngine.Debug.LogWarning("No connected eye trackers found.");
            return false;
        }

        private static bool GetFirstSupportedTracker(IStreamEngineInterop interop, IntPtr apiContext, IList<string> connectedDevices, StreamEngineTracker_Description description, out IntPtr deviceContext, out string deviceUrl)
        {
            var index = -1;
            deviceContext = IntPtr.Zero;
            deviceUrl = "";

            for (var i = 0; i < connectedDevices.Count; i++)
            {
                var connectedDeviceUrl = connectedDevices[i];
                if (CreateDeviceContext(interop, connectedDeviceUrl, apiContext, description.License, out deviceContext) == false) return false;

                tobii_device_info_t info;
                var result = interop.tobii_get_device_info(deviceContext, out info);
                if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
                {
                    DestroyDeviceContext(interop, deviceContext);
                    UnityEngine.Debug.LogWarning("Failed to get device info. " + result);
                    return false;
                }

                var integrationType = info.integration_type.ToLowerInvariant();
                if (integrationType != description.SupportedIntegrationType)
                {
                    DestroyDeviceContext(interop, deviceContext);
                    continue;
                }

                index = i;
                deviceUrl = connectedDeviceUrl;
                break;
            }

            if (index != -1) return true;

            UnityEngine.Debug.LogWarning(string.Format("Failed to find Tobii eye trackers of integration type {0}", description.SupportedIntegrationType));
            DestroyDeviceContext(interop, deviceContext);
            return false;
        }

        private static bool ReconnectToDevice(IStreamEngineInterop interop, IntPtr deviceContext)
        {
            var nativeContext = deviceContext;
            var result = interop.tobii_device_reconnect(nativeContext);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR) return false;

            UnityEngine.Debug.Log("Reconnected.");
            return true;
        }
    }

    public interface IStreamEngineInterop
    {
        tobii_error_t tobii_api_create(out IntPtr apiContext, tobii_custom_log_t logger);
        tobii_error_t tobii_api_destroy(IntPtr apiContext);
        tobii_error_t tobii_device_create_ex(IntPtr apiContext, string url, string[] licenseKeys, List<tobii_license_validation_result_t> licenseResults, out IntPtr deviceContext);
        tobii_error_t tobii_device_destroy(IntPtr deviceContext);
        tobii_error_t tobii_device_reconnect(IntPtr nativeContext);
        tobii_error_t tobii_enumerate_local_device_urls(IntPtr apiContext, out List<string> connectedDevices);
        tobii_error_t tobii_get_device_info(IntPtr deviceContext, out tobii_device_info_t info);
    }

    internal class InteropWrapper : IStreamEngineInterop
    {
        public tobii_error_t tobii_api_create(out IntPtr apiContext, tobii_custom_log_t logger)
        {
            return Interop.tobii_api_create(out apiContext, logger);
        }

        public tobii_error_t tobii_api_destroy(IntPtr apiContext)
        {
            return Interop.tobii_api_destroy(apiContext);
        }

        public tobii_error_t tobii_device_create_ex(IntPtr apiContext, string url, string[] licenseKeys, List<tobii_license_validation_result_t> licenseResults, out IntPtr deviceContext)
        {
            return Interop.tobii_device_create_ex(apiContext, url, licenseKeys, licenseResults, out deviceContext);
        }

        public tobii_error_t tobii_device_destroy(IntPtr deviceContext)
        {
            return Interop.tobii_device_destroy(deviceContext);
        }

        public tobii_error_t tobii_device_reconnect(IntPtr nativeContext)
        {
            return Interop.tobii_device_reconnect(nativeContext);
        }

        public tobii_error_t tobii_enumerate_local_device_urls(IntPtr apiContext, out List<string> connectedDevices)
        {
            return Interop.tobii_enumerate_local_device_urls(apiContext, out connectedDevices);
        }

        public tobii_error_t tobii_get_device_info(IntPtr deviceContext, out tobii_device_info_t info)
        {
            return Interop.tobii_get_device_info(deviceContext, out info);
        }
    }
}