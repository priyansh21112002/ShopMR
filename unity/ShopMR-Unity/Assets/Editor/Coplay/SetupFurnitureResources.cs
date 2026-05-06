using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupFurnitureResources
{
    // Maps: resourceName -> source prefab filename (without .prefab)
    private static readonly Dictionary<string, string> ProductPrefabMapping = new Dictionary<string, string>
    {
        // Sofas (prod-001, 002, 003)
        { "sofa_modern_leather",    "sofa_001" },
        { "sofa_scandinavian",      "sofa_001" },
        { "sofa_velvet_sectional",  "sofa_001" },
        // Chairs (prod-004, 005, 006)
        { "chair_ergonomic",        "kitchen_chair_001" },
        { "chair_accent",           "lounge_chair_001" },
        { "chair_rattan",           "lounge_chair_001" },
        // Tables (prod-007, 008, 009)
        { "table_oak_dining",       "kitchen_table_001" },
        { "table_glass_coffee",     "coffee_table_001" },
        { "table_marble_side",      "coffee_table_001" },
        // Lamps (prod-010, 011)
        { "lamp_arc_floor",         "lamp_001" },
        { "lamp_ceramic_table",     "lamp_002" },
        // Shelves (prod-012, 013)
        { "shelf_industrial",       "closet_001" },
        { "shelf_floating",         "dresser_001" },
        // Beds (prod-014, 015)
        { "bed_upholstered",        "bed_001" },
        { "bed_minimalist",         "bed_001" },
        // Desks (prod-016, 017)
        { "desk_standing",          "office_table_001" },
        { "desk_writing",           "office_table_001" },
        // Decor (prod-018, 019, 020)
        { "decor_planter",          "flower_001" },
        { "decor_wall_art",         "tv_wall_001" },
        { "decor_area_rug",         "box_001" },
    };

    private const string SOURCE_PREFAB_DIR = "Assets/ithappy/Furniture_FREE/Prefabs";
    private const string TARGET_DIR = "Assets/_ShopMR/Resources/Furniture";

    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Create target directory
        if (!AssetDatabase.IsValidFolder("Assets/_ShopMR/Resources"))
            AssetDatabase.CreateFolder("Assets/_ShopMR", "Resources");
        if (!AssetDatabase.IsValidFolder(TARGET_DIR))
            AssetDatabase.CreateFolder("Assets/_ShopMR/Resources", "Furniture");
        results.AppendLine($"Target folder: {TARGET_DIR}");

        int created = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var kvp in ProductPrefabMapping)
        {
            string resourceName = kvp.Key;
            string sourceName = kvp.Value;
            string sourcePath = $"{SOURCE_PREFAB_DIR}/{sourceName}.prefab";
            string targetPath = $"{TARGET_DIR}/{resourceName}.prefab";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null)
            {
                results.AppendLine($"  SKIP {resourceName} (already exists)");
                skipped++;
                continue;
            }

            // Load source prefab
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                results.AppendLine($"  FAIL {resourceName} — source not found: {sourcePath}");
                failed++;
                continue;
            }

            // Create prefab variant
            // Instantiate, save as new prefab variant, then destroy temp instance
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                results.AppendLine($"  FAIL {resourceName} — could not instantiate {sourceName}");
                failed++;
                continue;
            }

            instance.name = resourceName;

            // Save as prefab variant (maintains link to source)
            PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            Object.DestroyImmediate(instance);

            results.AppendLine($"  OK   {resourceName} ← {sourceName}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        results.AppendLine($"\nSummary: {created} created, {skipped} skipped, {failed} failed (total mappings: {ProductPrefabMapping.Count})");

        // 2. Generate SQL update statements
        results.AppendLine("\n===== SQL UPDATE STATEMENTS =====");
        results.AppendLine("-- Run these against your ShopMR database to link products to 3D models:");

        var sqlMapping = new Dictionary<string, string>
        {
            { "prod-001", "Furniture/sofa_modern_leather" },
            { "prod-002", "Furniture/sofa_scandinavian" },
            { "prod-003", "Furniture/sofa_velvet_sectional" },
            { "prod-004", "Furniture/chair_ergonomic" },
            { "prod-005", "Furniture/chair_accent" },
            { "prod-006", "Furniture/chair_rattan" },
            { "prod-007", "Furniture/table_oak_dining" },
            { "prod-008", "Furniture/table_glass_coffee" },
            { "prod-009", "Furniture/table_marble_side" },
            { "prod-010", "Furniture/lamp_arc_floor" },
            { "prod-011", "Furniture/lamp_ceramic_table" },
            { "prod-012", "Furniture/shelf_industrial" },
            { "prod-013", "Furniture/shelf_floating" },
            { "prod-014", "Furniture/bed_upholstered" },
            { "prod-015", "Furniture/bed_minimalist" },
            { "prod-016", "Furniture/desk_standing" },
            { "prod-017", "Furniture/desk_writing" },
            { "prod-018", "Furniture/decor_planter" },
            { "prod-019", "Furniture/decor_wall_art" },
            { "prod-020", "Furniture/decor_area_rug" },
        };

        foreach (var kv in sqlMapping)
        {
            results.AppendLine($"UPDATE products SET model_url = '{kv.Value}' WHERE product_id = '{kv.Key}';");
        }

        // 3. Verify Resources.Load works for each
        results.AppendLine("\n===== RESOURCES.LOAD VERIFICATION =====");
        int loadOk = 0;
        int loadFail = 0;
        foreach (var kv in sqlMapping)
        {
            string resourcePath = kv.Value; // e.g. "Furniture/sofa_modern_leather"
            var loaded = Resources.Load<GameObject>(resourcePath);
            if (loaded != null)
            {
                loadOk++;
            }
            else
            {
                results.AppendLine($"  LOAD FAIL: Resources.Load(\"{resourcePath}\") returned null");
                loadFail++;
            }
        }
        results.AppendLine($"Resources.Load: {loadOk}/{sqlMapping.Count} passed");

        return results.ToString();
    }
}
