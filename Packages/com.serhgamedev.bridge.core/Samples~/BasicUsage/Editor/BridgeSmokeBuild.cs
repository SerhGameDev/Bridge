using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace Bridge.Samples
{
    public static class BridgeSmokeBuild
    {
        public static void BuildWebGl()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
