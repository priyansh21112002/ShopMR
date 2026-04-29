using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetupMainMRScene
{
    public static string Execute()
    {
        // Step 1: Save current scene as MainMR.unity in _ShopMR/Scenes/
        string targetPath = "Assets/_ShopMR/Scenes/MainMR.unity";
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, targetPath);

        // Ensure it's added to build settings
        var buildScenes = EditorBuildSettings.scenes;
        bool alreadyInBuild = false;
        foreach (var s in buildScenes)
        {
            if (s.path == targetPath) { alreadyInBuild = true; break; }
        }
        if (!alreadyInBuild)
        {
            var newScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
            for (int i = 0; i < buildScenes.Length; i++)
                newScenes[i] = buildScenes[i];
            newScenes[buildScenes.Length] = new EditorBuildSettingsScene(targetPath, true);
            EditorBuildSettings.scenes = newScenes;
        }

        // Step 2: Create GameManager GameObject
        var existing = GameObject.Find("GameManager");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var gameManager = new GameObject("GameManager");

        // Add the three components
        gameManager.AddComponent<ShopMR.Networking.APIClient>();
        gameManager.AddComponent<ShopMR.Core.SessionManager>();
        gameManager.AddComponent<ShopMR.Core.EventTracker>();

        // Step 3: Save scene again with the new GameManager
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        return $"Scene saved to {targetPath} (added to build settings: {!alreadyInBuild}). GameManager created with APIClient, SessionManager, EventTracker components.";
    }
}
