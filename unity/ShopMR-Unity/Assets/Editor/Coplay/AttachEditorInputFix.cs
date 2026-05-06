using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AttachEditorInputFix
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        var eventSystemGO = GameObject.Find("EventSystem");
        if (eventSystemGO == null)
            return "ERROR: EventSystem not found";

        // Attach EditorInputFix if not already present
        var fix = eventSystemGO.GetComponent<ShopMR.Core.EditorInputFix>();
        if (fix == null)
        {
            fix = eventSystemGO.AddComponent<ShopMR.Core.EditorInputFix>();
            EditorUtility.SetDirty(eventSystemGO);
            results.AppendLine("Added EditorInputFix to EventSystem");
        }
        else
        {
            results.AppendLine("EditorInputFix already present");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved: {scene.path}");
        return results.ToString();
    }
}
