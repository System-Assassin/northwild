using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[InitializeOnLoad]
public static class NorthwildHDRPProjectSetup
{
    private const string AssetFolder = "Assets/Northwild/HDRP";
    private const string PipelineAssetPath = AssetFolder + "/NorthwildHDRPAsset.asset";
    private static bool setupQueued;

    static NorthwildHDRPProjectSetup()
    {
        QueueSetup();
    }

    private static void QueueSetup()
    {
        if (setupQueued)
            return;
        setupQueued = true;
        EditorApplication.delayCall += ConfigureAfterImport;
    }

    private static void ConfigureAfterImport()
    {
        setupQueued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueSetup();
            return;
        }

        EnsureConfigured(false);
    }

    [MenuItem("Northwild/HDRP/Repair Project Setup")]
    public static void RepairProjectSetup()
    {
        EnsureConfigured(true);
    }

    public static void EnsureConfigured(bool showConfirmation)
    {
        EnsureFolder();
        HDRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(PipelineAssetPath);
        if (pipelineAsset == null)
        {
            pipelineAsset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            pipelineAsset.name = "Northwild HDRP Asset";
            AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
        }

        RenderPipelineSettings settings = pipelineAsset.currentPlatformRenderPipelineSettings;
        settings.supportVolumetrics = true;
        settings.supportVolumetricClouds = true;
        settings.supportMotionVectors = true;
        pipelineAsset.currentPlatformRenderPipelineSettings = settings;

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        EnsureGlobalSettings();

        EditorUtility.SetDirty(pipelineAsset);
        AssetDatabase.SaveAssets();

        if (showConfirmation)
            EditorUtility.DisplayDialog(
                "Northwild HDRP",
                "HDRP, volumetric clouds, linear color and the HDRP global settings are configured.",
                "OK");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Northwild", "HDRP");
    }

    private static void EnsureGlobalSettings()
    {
        Type settingsType = typeof(HDRenderPipelineAsset).Assembly.GetType(
            "UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings");
        MethodInfo ensure = settingsType == null
            ? null
            : settingsType.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);
        if (ensure != null)
        {
            try
            {
                ensure.Invoke(null, new object[] { true });
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogWarning("Unity is still importing HDRP global settings. When compilation finishes, use " +
                    "Northwild > HDRP > Repair Project Setup. " + exception.InnerException?.Message);
            }
        }
    }
}
