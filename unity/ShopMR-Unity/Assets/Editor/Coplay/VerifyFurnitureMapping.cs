using UnityEngine;
using UnityEditor;

public class VerifyFurnitureMapping
{
    // All 20 model_url values from backend mapped to Unity Resources paths
    private static readonly string[] ExpectedResourcePaths = new string[]
    {
        "Furniture/sofa_modern_leather",
        "Furniture/sofa_scandinavian",
        "Furniture/sofa_velvet_sectional",
        "Furniture/chair_ergonomic",
        "Furniture/chair_accent",
        "Furniture/chair_rattan",
        "Furniture/table_oak_dining",
        "Furniture/table_glass_coffee",
        "Furniture/table_marble_side",
        "Furniture/lamp_arc_floor",
        "Furniture/lamp_ceramic_table",
        "Furniture/shelf_industrial",
        "Furniture/shelf_floating",
        "Furniture/bed_upholstered",
        "Furniture/bed_minimalist",
        "Furniture/desk_standing",
        "Furniture/desk_writing",
        "Furniture/decor_planter",
        "Furniture/decor_wall_art",
        "Furniture/decor_area_rug",
    };

    public static string Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("===== UNITY RESOURCES.LOAD VERIFICATION =====");
        sb.AppendLine("Testing all 20 backend model_url paths against Unity Resources...\n");

        int passed = 0;
        int failed = 0;

        foreach (var path in ExpectedResourcePaths)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                // Check it's not a cube placeholder
                var meshFilter = prefab.GetComponent<MeshFilter>();
                string meshName = meshFilter != null ? meshFilter.sharedMesh?.name ?? "null" : "no MeshFilter";
                bool isCube = meshName == "Cube";

                sb.AppendLine($"  OK  {path,-40} | mesh: {meshName}{(isCube ? " ⚠ PLACEHOLDER CUBE" : "")}");
                passed++;
            }
            else
            {
                sb.AppendLine($"  FAIL {path,-40} | Resources.Load returned null!");
                failed++;
            }
        }

        sb.AppendLine($"\nResult: {passed}/20 passed, {failed} failed");

        if (failed == 0)
            sb.AppendLine("\n✅ ALL backend model_url values resolve to valid Unity prefabs!");
        else
            sb.AppendLine($"\n❌ {failed} prefab(s) could not be loaded. Fix the Resources/Furniture/ folder.");

        // Also verify the placeholder prefab exists
        var placeholder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/PlaceablePlaceholder.prefab");
        sb.AppendLine($"\nPlaceholder fallback: {(placeholder != null ? "EXISTS" : "MISSING")}");

        return sb.ToString();
    }
}
