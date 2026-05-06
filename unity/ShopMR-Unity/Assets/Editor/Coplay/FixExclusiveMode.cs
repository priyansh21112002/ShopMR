using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixExclusiveMode
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        var eventSystemGO = GameObject.Find("EventSystem");
        if (eventSystemGO == null)
            return "ERROR: EventSystem not found";

        // Find PointableCanvasModule and disable exclusive mode
        var allComponents = eventSystemGO.GetComponents<Component>();
        foreach (var comp in allComponents)
        {
            if (comp.GetType().Name == "PointableCanvasModule")
            {
                var so = new SerializedObject(comp);
                var exclusiveProp = so.FindProperty("_exclusiveMode");
                if (exclusiveProp != null)
                {
                    exclusiveProp.boolValue = false;
                    so.ApplyModifiedProperties();
                    results.AppendLine("Set PointableCanvasModule._exclusiveMode = false");
                }
                else
                {
                    results.AppendLine("WARNING: _exclusiveMode property not found");
                }
            }
        }

        EditorUtility.SetDirty(eventSystemGO);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved: {scene.path}");

        return results.ToString();
    }
}
