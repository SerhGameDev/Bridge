using System;
using System.IO;
using Bridge.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Bridge.Unity.Demo.Editor
{
    public static class RuntimeDemoBuild
    {
        [MenuItem("Bridge/Create Runtime Demo Scene")]
        public static void CreateScene() => Scene(false);
        static string Scene(bool smoke)
        {
            const string folder="Assets/BridgeRuntimeDemo/Generated";
            Directory.CreateDirectory(folder);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=new Vector3(6,5,-10);camera.transform.LookAt(Vector3.zero);
            new GameObject("Light").AddComponent<Light>().type=LightType.Directional;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var prefab=PrefabUtility.SaveAsPrefabAsset(cube,folder+"/Cube.prefab");UnityEngine.Object.DestroyImmediate(cube);
            var doc=BridgeDocument.Create("demo");doc.AddBlock("settings","simulation.settings",BridgeValue.Parse("{\"speed\":1,\"gravity\":9.81,\"privateNote\":\"not published\"}"));
            File.WriteAllText(folder+"/factory.bridge.json.txt",doc.ToJson());AssetDatabase.Refresh();
            var bootstrap=new GameObject("Bridge Runtime").AddComponent<BridgeBootstrap>();bootstrap.CubePrefab=prefab;bootstrap.Config=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/factory.bridge.json.txt");bootstrap.SelfTest=smoke;
            var path=folder+"/SceneDemo.unity";EditorSceneManager.SaveScene(scene,path);return path;
        }
        public static void BuildWindows() => Build(false);
        public static void BuildWebGL() => Build(true);
        static void Build(bool web)
        {
            var target=web?BuildTarget.WebGL:BuildTarget.StandaloneWindows64;
            var named=web?NamedBuildTarget.WebGL:NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(named,web?ScriptingImplementation.IL2CPP:ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(named,web?ManagedStrippingLevel.High:ManagedStrippingLevel.Low);
            PlayerSettings.runInBackground=true;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../",web?"webgl-runtime":"windows/Bridge.Runtime.Demo.exe"));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Scene(web)},target=target,locationPathName=output,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Player build failed: "+report.summary.result);
        }
    }
}
