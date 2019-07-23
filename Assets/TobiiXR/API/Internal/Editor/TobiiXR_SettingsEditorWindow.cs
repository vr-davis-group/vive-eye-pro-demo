// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tobii.XR
{
    public class TobiiXR_SettingsEditorWindow : EditorWindow
    {
        private static readonly string TobiiXR_SettingsAssetPath = PathHelper.PathCombine("Internal", "Resources", typeof(TobiiXR_Settings).Name + ".asset");
        private TobiiXR_Settings _settings;

        private readonly ProviderTypeDropDownData _standaloneDropDownData = new ProviderTypeDropDownData(BuildTargetGroup.Standalone);
        private readonly ProviderTypeDropDownData _androidDropDownData = new ProviderTypeDropDownData(BuildTargetGroup.Android);

        [InitializeOnLoadMethod]
        public static void OnProjectLoadedInEditor()
        {
            EditorApplication.delayCall += () =>
            {
                var config = LoadOrCreateDefaultConfiguration();
                EditorUtils.UpdateCompilerFlags(config);
            };
        }

        [MenuItem("Window/Tobii XR/Settings", priority = 10000)]
        public static void ShowWindow()
        {
            GetWindow<TobiiXR_SettingsEditorWindow>("Tobii Settings").Show();
        }

        private void OnEnable()
        {
            _settings = LoadOrCreateDefaultConfiguration();

            _standaloneDropDownData.SetSelectedType(_settings.EyeTrackingProviderTypeStandAlone);
            _androidDropDownData.SetSelectedType(_settings.EyeTrackingProviderTypeAndroid);

            EditorUtils.UpdateCompilerFlags(_settings);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Change settings used to initialize Tobii XR. More info at the site below:");
            if (GUILayout.Button("Open Tobii Settings documentation website"))
            {
                Application.OpenURL("https://developer.tobii.com/vr/develop/unity/documentation/tobii-settings/");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            EditorGUI.BeginChangeCheck();
            var fieldOfUseDropDown = EditorGUILayout.EnumPopup(new GUIContent("Field of use", "In what field is your application intended to be used."), _settings.FieldOfUse); //, "For what field of use is your application intended.");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_settings, "Field of use changed");
                _settings.FieldOfUse = (FieldOfUse)fieldOfUseDropDown;
                SetDirty(_settings);
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Focused Object Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            EditorGUIUtility.labelWidth = 250;
            EditorGUI.BeginChangeCheck();
            var layer = EditorGUILayout.MaskField(new GUIContent("LayerMask", "What layers should be considered for finding gaze focusable objects."), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_settings.LayerMask), InternalEditorUtility.layers);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_settings, "Layers changed");
                _settings.LayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(layer);
                SetDirty(_settings);
            }

            EditorGUI.BeginChangeCheck();
            var expectedNumberOfObjects = EditorGUILayout.IntField(new GUIContent("Expected Number of Objects", "How many gaze focusable objects do we expect to be in direct line of sight simultaneously."), _settings.ExpectedNumberOfObjects);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_settings, "ExpectedNumberOfObjects changed");
                _settings.ExpectedNumberOfObjects = expectedNumberOfObjects;
                SetDirty(_settings);
            }

            EditorGUI.BeginChangeCheck();
            var howLongToKeepCandidatesInSeconds = EditorGUILayout.FloatField(new GUIContent("How Long To Keep Candidates In Seconds", "How long do we keep gaze focusable objects in memory."), _settings.HowLongToKeepCandidatesInSeconds);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_settings, "HowLongToKeepCandidatesInSeconds changed");
                _settings.HowLongToKeepCandidatesInSeconds = howLongToKeepCandidatesInSeconds;
                SetDirty(_settings);
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Eye Tracking Data Provider", EditorStyles.boldLabel);

            _standaloneDropDownData.ShowDropDown(_settings, ref _settings.EyeTrackingProviderTypeStandAlone);
            _androidDropDownData.ShowDropDown(_settings, ref _settings.EyeTrackingProviderTypeAndroid);

            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Separator();
            _standaloneDropDownData.ShowSettings();

            EditorGUILayout.Separator();
            _androidDropDownData.ShowSettings();

        }

        private static void SetDirty(UnityEngine.Object obj)
        {
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        private static TobiiXR_Settings LoadOrCreateDefaultConfiguration()
        {
            bool resourceExists;
            var settings = TobiiXR_Settings.CreateDefaultSettings(out resourceExists);

            if (!resourceExists)
            {
                var sdkPath = Path.GetDirectoryName(PathHelper.FindPathToClass(typeof(TobiiXR)));
                var filePath = PathHelper.PathCombine(sdkPath, TobiiXR_SettingsAssetPath);
                var assetPath = filePath.Replace(Application.dataPath, "Assets");

                if (File.Exists(filePath))
                {
                    AssetDatabase.Refresh();
                    settings = AssetDatabase.LoadAssetAtPath<TobiiXR_Settings>(assetPath);
                    return settings;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(settings, assetPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        public class ProviderTypeDropDownData
        {
            public string TypeString
            {
                get { return TypeStrings[SelectedType]; }
            }

            public readonly string[] TypeStrings;
            private readonly string[] TypeStringDisplayNames;

            public int SelectedType;
            private IProviderSettings _providerSettings;
            private readonly BuildTargetGroup _targetGroup;
            private string _gazeModifierWrappedProvider = null;

            public ProviderTypeDropDownData(BuildTargetGroup targetGroup)
            {
                var providerNames = EditorUtils.EyetrackingProviderTypes(targetGroup)
                    .Select(x => {
                        var att = x.GetCustomAttributes(typeof(ProviderDisplayNameAttribute), false).FirstOrDefault() as ProviderDisplayNameAttribute;
                        return new
                        {
                            TypeName = x.FullName,
                            DisplayName = att != null ? att.Name : x.FullName
                        };
                    })
                    .OrderBy(x => x.DisplayName)
                    .ToList();

                TypeStrings = providerNames.Select(x => x.TypeName).ToArray();
                TypeStringDisplayNames = providerNames.Select(x => x.DisplayName).ToArray();

                _targetGroup = targetGroup;
            }

            public void SetSelectedType(string type)
            {
                SelectedType = Array.IndexOf(TypeStrings, type);
                _providerSettings = GetProviderSettingsFor(type);
                if (_providerSettings != null) _providerSettings.Init();
            }

            public bool ShowDropDown(ref string eyeTrackingProviderTypeString, string label = null)
            {
                label = label == null ? _targetGroup.ToString() : label;

                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.Popup(label, SelectedType, TypeStringDisplayNames);
                if (EditorGUI.EndChangeCheck())
                {
                    SelectedType = selected;
                    eyeTrackingProviderTypeString = TypeString;

                    _providerSettings = GetProviderSettingsFor(eyeTrackingProviderTypeString);
                    if (_providerSettings != null) _providerSettings.Init();
                    return true;
                }

                return false;
            }

            public void ShowDropDown(TobiiXR_Settings settings, ref string eyeTrackingProviderTypeString)
            {
                var changed = ShowDropDown(ref eyeTrackingProviderTypeString);
                if (changed)
                {
                    Undo.RecordObject(settings, _targetGroup.ToString() + " Provider changed");
                    TobiiXR_SettingsEditorWindow.SetDirty(settings);
                    EditorUtils.UpdateCompilerFlags(settings);
                }
                var type = AssemblyUtils.EyetrackingProviderType(eyeTrackingProviderTypeString);
                if (type == typeof(GazeModifier.GazeModifierProvider))
                {
                    var wrappedProvider = GazeModifier.GazeModifierSettings.CreateDefaultSettings().EyetrackingProviderType;
                    if (wrappedProvider != _gazeModifierWrappedProvider)
                    {
                        _gazeModifierWrappedProvider = wrappedProvider;
                        EditorUtils.UpdateCompilerFlags(settings);
                    }
                }
                else
                {
                    _gazeModifierWrappedProvider = null;
                }
            }

            public void ShowSettings()
            {
                if (_providerSettings != null) _providerSettings.ShowSettingsGUI();
            }

            private static IProviderSettings GetProviderSettingsFor(string providerTypeString)
            {
                var providerType = AssemblyUtils.EyetrackingProviderType(providerTypeString);
                if (providerType == null) return null;

                var editorType = EditorUtils.ProviderSettingsType(providerType);

                if (editorType == null) return null;
                return (IProviderSettings)Activator.CreateInstance(editorType);
            }
        }
    }
}