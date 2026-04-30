using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class PreviewCatalogPanel
{
    public static string Execute()
    {
        // Create a temporary world-space canvas for preview
        var canvasGO = new GameObject("_PreviewCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800, 600);
        canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.position = new Vector3(0, 1.2f, 1.5f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Load and instantiate the CatalogPanel prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/CatalogPanel.prefab");
        if (prefab == null) return "ERROR: CatalogPanel prefab not found.";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGO.transform);
        var rt = instance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return "Preview canvas created. Capture _PreviewCanvas to view.";
    }
}
