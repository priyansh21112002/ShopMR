using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixCanvasScale
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // Fix CatalogCanvas scale: 800x600 sizeDelta at scale 1 = 800m x 600m
        // At scale 0.001, it becomes 0.8m x 0.6m — correct for a floating MR panel
        var catalogCanvas = GameObject.Find("CatalogCanvas");
        if (catalogCanvas != null)
        {
            var rt = catalogCanvas.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector3 oldScale = rt.localScale;
                rt.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                EditorUtility.SetDirty(catalogCanvas);
                results.AppendLine($"Fixed CatalogCanvas scale: {oldScale} → (0.001, 0.001, 0.001)");
                results.AppendLine($"  Canvas size: {rt.sizeDelta} → world size: {rt.sizeDelta.x * 0.001f}m x {rt.sizeDelta.y * 0.001f}m");
            }
        }
        else
        {
            results.AppendLine("WARNING: CatalogCanvas not found");
        }

        // Save scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
