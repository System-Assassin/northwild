using Northwild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Northwild/Scenes/Prototype.unity";

    [MenuItem("Northwild/Create HDRP Prototype Scene")]
    public static void CreatePrototypeScene()
    {
        NorthwildHDRPProjectSetup.EnsureConfigured(false);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject bootstrap = new GameObject("Northwild Game");
        bootstrap.AddComponent<NorthwildGame>();
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        Selection.activeGameObject = bootstrap;
        AssetDatabase.SaveAssets();
        Debug.Log("Northwild HDRP prototype scene created. Press Play to enter the wilderness.");
    }

    [MenuItem("Northwild/Play HDRP Prototype")]
    public static void PlayPrototype()
    {
        if (!System.IO.File.Exists(ScenePath))
            CreatePrototypeScene();
        else
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}
