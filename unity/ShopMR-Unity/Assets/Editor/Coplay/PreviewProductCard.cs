using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class PreviewProductCard
{
    public static string Execute()
    {
        // Create a temporary world-space canvas for preview
        var canvasGO = new GameObject("_PreviewCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800, 200);
        canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.position = new Vector3(0, 1.2f, 1.5f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Load and instantiate the prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/ProductCard.prefab");
        if (prefab == null) return "ERROR: Prefab not found.";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGO.transform);
        var rt = instance.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;

        return "Preview canvas created at _PreviewCanvas. Use capture_scene_object to view, then delete _PreviewCanvas.";
    }
}
