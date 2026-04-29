using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveMainMRScene
{
    public static string Execute()
    {
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
            "Assets/_ShopMR/Scenes/MainMR.unity");
        return "Scene saved.";
    }
}
