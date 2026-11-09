using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bridge.Samples
{
    public static class BridgeSmokeBuild
    {
        public static void BuildWebGl()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Camera", typeof(Camera));
            new GameObject("Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            // Optional second sample: no compile-time dependency when BasicUsage is imported alone.
            var beginnerType = Type.GetType("Bridge.Samples.BridgeBeginner, Bridge.Beginner");
            if (beginnerType != null)
            {
                var beginner = new SerializedObject(new GameObject("Bridge Beginner").AddComponent(beginnerType));
                beginner.FindProperty("Source").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/BridgeBeginner/factory.bridge.json.txt");
                beginner.ApplyModifiedPropertiesWithoutUndo();
            }
            const string scenePath = "Assets/BridgeSmoke.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            string output = Path.GetFullPath(Path.Combine("..", "webgl-smoke"));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            { scenes = new[] { scenePath }, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("IL2CPP build failed: " + report.summary.result);
        }
    }
}
