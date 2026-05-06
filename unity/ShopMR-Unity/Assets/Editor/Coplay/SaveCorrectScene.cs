using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveCorrectScene
{
    public static string Execute()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"Saved: {scene.path}";
    }
}
