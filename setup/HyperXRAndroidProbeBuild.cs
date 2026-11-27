// Copy into Assets/Editor in a disposable checkout of the pinned Unity sample.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class HyperXRAndroidProbeBuild
{
    private const string EditorVersion = "6000.3.0f1";
    private const string ScenePath =
        "Assets/XRI_Examples/Scenes/Station_01_Setup_Isolated.unity";

    private static void ConfigureAndroid()
    {
        if (Application.unityVersion != EditorVersion)
            throw new BuildFailedException("Expected pinned Unity " + EditorVersion);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
            !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new BuildFailedException("Run with -buildTarget Android and Android Build Support installed.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new BuildFailedException("The pinned XRI sample scene was not found.");

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.buildAppBundle = false;
    }

    // Configure this method as Unity Build Automation's pre-export hook.
    public static void CloudPreExport()
    {
        ConfigureAndroid();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    public static void Build()
    {
        ConfigureAndroid();
        var path = Environment.GetEnvironmentVariable("HYPERXR_PROBE_APK");
        if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) ||
            !string.Equals(Path.GetExtension(path), ".apk", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Set HYPERXR_PROBE_APK to an absolute, new .apk output path.");
        if (File.Exists(path) || Directory.Exists(path))
            throw new BuildFailedException("Output already exists; choose a new path.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.Development,
        });
        if (report.summary.result != BuildResult.Succeeded || !File.Exists(path))
            throw new BuildFailedException("Unity Android probe did not produce an APK: " + report.summary.result);
        Debug.Log("Unity Android probe APK created (not yet admitted or run): " + path);
    }
}
