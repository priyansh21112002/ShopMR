using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Saves the current scene to the correct build path and updates build settings.
/// The scene was accidentally saved to Assets/MainMR.unity but builds use
/// Assets/_ShopMR/Scenes/MainMR.unity.
/// </summary>
public class FixScenePath
{
    public static string Execute()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        string currentPath = activeScene.path;
        string correctPath = "Assets/_ShopMR/Scenes/MainMR.unity";

        string result = $"Current scene path: {currentPath}\n";
        result += $"Build settings path: {correctPath}\n";

        if (currentPath != correctPath)
        {
            // Save the current scene to the correct build path
            bool saved = EditorSceneManager.SaveScene(activeScene, correctPath);
            result += saved
                ? $"SUCCESS: Saved scene to {correctPath}\n"
                : $"FAILED: Could not save to {correctPath}\n";

            // Open the scene from the correct path
            if (saved)
            {
                EditorSceneManager.OpenScene(correctPath);
                result += $"Opened scene from {correctPath}\n";
            }
        }
        else
        {
            // Already at correct path, just save
            EditorSceneManager.SaveScene(activeScene);
            result += "Scene already at correct path. Saved.\n";
        }

        return result;
    }
}
