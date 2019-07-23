// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tobii.XR.GazeModifier
{
    [ProviderSettings(typeof(GazeModifierProvider))]
    public class GazeProviderSettingsUI : IProviderSettings
    {
        private static readonly string GazeModifierSettingsAssetPath =
            PathHelper.PathCombine("Resources", typeof(GazeModifierSettings).Name + ".asset");

        private GazeModifierSettings Settings;

        private readonly TobiiXR_SettingsEditorWindow.ProviderTypeDropDownData _typeDropDown =
            new TobiiXR_SettingsEditorWindow.ProviderTypeDropDownData(BuildTargetGroup.Standalone);

        private static int _space = 3;

        public void Init()
        {
            Settings = LoadOrCreateDefaultConfiguration();
            _typeDropDown.SetSelectedType(Settings.EyetrackingProviderType);
        }

        private static GazeModifierSettings LoadOrCreateDefaultConfiguration()
        {
            bool resourceExists;
            var settings = GazeModifierSettings.CreateDefaultSettings(out resourceExists);

            if (resourceExists) return settings;

            var sdkPath = Path.GetDirectoryName(PathHelper.FindPathToClass(typeof(GazeModifierSettings)));
            var filePath = PathHelper.PathCombine(sdkPath, GazeModifierSettingsAssetPath);
            var assetPath = filePath.Replace(Application.dataPath, "Assets");

            Debug.Log(assetPath);

            if (File.Exists(filePath))
            {
                AssetDatabase.Refresh();
                settings = AssetDatabase.LoadAssetAtPath<GazeModifierSettings>(assetPath);
                return settings;
            }

            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();

            return settings;
        }

        public void ShowSettingsGUI()
        {
            EditorGUILayout.LabelField("GazeModifierProvider Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            var providerType = Settings.EyetrackingProviderType;
            var previousProvider = providerType;

            _typeDropDown.SetSelectedType(previousProvider); // set this again, just in case it's been changed somewhere

            var valueChanged = _typeDropDown.ShowDropDown(ref providerType, "Wrapped Provider:");
            if (valueChanged)
            {
                if (providerType == typeof(GazeModifierProvider).FullName)
                {
                    Debug.LogError(
                        "Cannot select gaze emulator provider for gaze emulator. Please select another provider");
                    Settings.EyetrackingProviderType = previousProvider;
                    _typeDropDown.SetSelectedType(previousProvider);
                }
                else
                {
                    Settings.EyetrackingProviderType = providerType;
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            GUILayout.Space(_space);
            GUILayout.BeginHorizontal();
            Settings.EnableGazeModifier = GUILayout.Toggle(Settings.EnableGazeModifier, "GazeModifier enabled");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Percentile:");
            var selectedPercentileIndex = (int) (GUILayout.HorizontalSlider(Settings.SelectedPercentileIndex, 0,
                Settings.NumberOfPercentiles - 1));
            EditorGUILayout.LabelField(Settings.SelectedPercentileString, GUILayout.MaxWidth(50));

            GUILayout.EndHorizontal();

            GUILayout.Space(_space);
            EditorStyles.textArea.wordWrap = true;
            if (GUILayout.Button("Open Gaze Modifier documentation website"))
            {
                Application.OpenURL("https://developer.tobii.com/vr/?develop/unity/tools/gaze-modifier/");
            }
            GUILayout.Space(_space);

            EditorGUILayout.Separator();

            if (EditorGUI.EndChangeCheck() || valueChanged)
            {
                Settings.SelectedPercentileIndex = selectedPercentileIndex;
                Undo.RecordObject(Settings, "Gaze Emulator Settings changed");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }
    }
}