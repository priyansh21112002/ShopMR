using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AddControllerRay
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Add ControllerRayVisual to the GameManager (or create a dedicated GO)
        var existing = GameObject.Find("ControllerRayVisual");
        if (existing != null)
        {
            results.AppendLine("ControllerRayVisual already exists");
        }
        else
        {
            var rayGO = new GameObject("ControllerRayVisual");
            rayGO.AddComponent<ShopMR.Core.ControllerRayVisual>();
            EditorUtility.SetDirty(rayGO);
            results.AppendLine("Created ControllerRayVisual GameObject with script");
        }

        // 2. Fix spawn distance back to 0.8m per spec
        var panelGO = GameObject.Find("CatalogPanel");
        if (panelGO != null)
        {
            var controller = panelGO.GetComponent<ShopMR.Catalog.CatalogPanelController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var spawnDist = so.FindProperty("spawnDistance");
                if (spawnDist != null)
                {
                    spawnDist.floatValue = 0.8f;
                    results.AppendLine("Set spawnDistance = 0.8m (per spec)");
                }
                var vertOff = so.FindProperty("verticalOffset");
                if (vertOff != null)
                {
                    vertOff.floatValue = -0.1f;
                    results.AppendLine("Set verticalOffset = -0.1m (eye level)");
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
            }
        }

        // 3. Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
