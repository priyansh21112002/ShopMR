using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixCanvasPosition
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // Move CatalogCanvas closer — 0.8m in front of camera at head height
        var canvas = GameObject.Find("CatalogCanvas");
        if (canvas != null)
        {
            // Camera is at (0, 1.6, 0), spawnDistance = 0.8, verticalOffset = -0.1
            // So initial pos = (0, 1.5, 0.8)
            canvas.transform.position = new Vector3(0f, 1.5f, 0.8f);
            canvas.transform.rotation = Quaternion.identity; // facing +Z = toward camera at origin
            EditorUtility.SetDirty(canvas);
            results.AppendLine($"CatalogCanvas moved to {canvas.transform.position}");

            // Also update the serialized spawnDistance on CatalogPanelController
            var controller = canvas.GetComponentInChildren<ShopMR.Catalog.CatalogPanelController>(true);
            if (controller != null)
            {
                // The spawnDistance default in code is now 0.8f
                // But the serialized value from before (1.5) might override it.
                // Use SerializedObject to force-set it.
                var so = new SerializedObject(controller);
                var prop = so.FindProperty("spawnDistance");
                if (prop != null)
                {
                    prop.floatValue = 0.8f;
                    so.ApplyModifiedProperties();
                    results.AppendLine($"spawnDistance set to 0.8");
                }
                else
                {
                    results.AppendLine("WARNING: spawnDistance property not found");
                }
            }
        }
        else
        {
            results.AppendLine("WARNING: CatalogCanvas not found");
        }

        // Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved: {scene.path}");

        return results.ToString();
    }
}
