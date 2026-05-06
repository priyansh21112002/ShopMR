using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script that spawns duplicate-mesh products side-by-side with their
/// per-product tints applied, so you can visually verify the color differences.
/// </summary>
public class PreviewProductTints
{
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private const float TINT_STRENGTH = 0.75f;

    // Same tint map as FurniturePlacer (updated vivid values)
    private static readonly Dictionary<string, Color> ProductTintMap = new Dictionary<string, Color>
    {
        { "prod-001", new Color(0.30f, 0.30f, 0.32f) },
        { "prod-002", new Color(0.92f, 0.82f, 0.62f) },
        { "prod-003", new Color(0.12f, 0.15f, 0.50f) },
        { "prod-005", new Color(0.95f, 0.78f, 0.12f) },
        { "prod-006", new Color(0.95f, 0.90f, 0.75f) },
        { "prod-008", new Color(0.90f, 0.78f, 0.30f) },
        { "prod-009", new Color(0.18f, 0.18f, 0.20f) },
        { "prod-014", new Color(0.58f, 0.58f, 0.65f) },
        { "prod-015", new Color(0.90f, 0.80f, 0.55f) },
        { "prod-016", new Color(0.82f, 0.72f, 0.38f) },
        { "prod-017", new Color(0.95f, 0.95f, 0.95f) },
    };

    private struct TintPreviewItem
    {
        public string productId;
        public string label;
        public string resourcePath;
    }

    private static readonly TintPreviewItem[][] PreviewGroups = new TintPreviewItem[][]
    {
        // Sofas (all share sofa_001)
        new TintPreviewItem[]
        {
            new TintPreviewItem { productId = "prod-001", label = "Charcoal Leather", resourcePath = "Furniture/sofa_modern_leather" },
            new TintPreviewItem { productId = "prod-002", label = "Beige Fabric", resourcePath = "Furniture/sofa_scandinavian" },
            new TintPreviewItem { productId = "prod-003", label = "Navy Velvet", resourcePath = "Furniture/sofa_velvet_sectional" },
        },
        // Beds (share bed_001)
        new TintPreviewItem[]
        {
            new TintPreviewItem { productId = "prod-014", label = "Grey Upholstered", resourcePath = "Furniture/bed_upholstered" },
            new TintPreviewItem { productId = "prod-015", label = "Pine Minimalist", resourcePath = "Furniture/bed_minimalist" },
        },
        // Desks (share office_table_001)
        new TintPreviewItem[]
        {
            new TintPreviewItem { productId = "prod-016", label = "Bamboo Standing", resourcePath = "Furniture/desk_standing" },
            new TintPreviewItem { productId = "prod-017", label = "White Writing", resourcePath = "Furniture/desk_writing" },
        },
    };

    public static string Execute()
    {
        // Clean up any previous preview
        var old = GameObject.Find("_TintPreview");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("_TintPreview");
        root.transform.position = new Vector3(0f, 0f, 3f);

        float groupZ = 0f;
        int totalSpawned = 0;

        foreach (var group in PreviewGroups)
        {
            float itemX = 0f;
            foreach (var item in group)
            {
                var prefab = Resources.Load<GameObject>(item.resourcePath);
                if (prefab == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = $"{item.productId} ({item.label})";
                instance.transform.SetParent(root.transform);
                instance.transform.localPosition = new Vector3(itemX, 0f, groupZ);

                // Apply tint via MaterialPropertyBlock (same logic as FurniturePlacer)
                if (ProductTintMap.TryGetValue(item.productId, out Color tintColor))
                {
                    foreach (var rend in instance.GetComponentsInChildren<Renderer>())
                    {
                        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                        for (int i = 0; i < rend.sharedMaterials.Length; i++)
                        {
                            rend.GetPropertyBlock(mpb, i);
                            Color original = Color.white;
                            if (rend.sharedMaterials[i] != null && rend.sharedMaterials[i].HasProperty(BaseColorProp))
                                original = rend.sharedMaterials[i].GetColor(BaseColorProp);

                            Color blended = Color.Lerp(original, tintColor, TINT_STRENGTH);
                            blended.a = original.a;
                            mpb.SetColor(BaseColorProp, blended);
                            rend.SetPropertyBlock(mpb, i);
                        }
                    }
                }

                itemX += 3.5f;
                totalSpawned++;
            }
            groupZ += 4f;
        }

        Selection.activeGameObject = root;

        return $"Spawned {totalSpawned} tinted items under '_TintPreview' with strength={TINT_STRENGTH}. Check Scene view.";
    }
}
