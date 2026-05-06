using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class SetupStage3Placement
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // ===== 1. Create Materials folder if needed =====
        string matFolder = "Assets/_ShopMR/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            AssetDatabase.CreateFolder("Assets/_ShopMR", "Materials");
            results.AppendLine("Created Materials folder");
        }

        // ===== 2. Create PlaceholderMat (opaque URP/Lit, white) =====
        string placeholderMatPath = matFolder + "/PlaceholderMat.mat";
        Material placeholderMat;
        if (AssetDatabase.LoadAssetAtPath<Material>(placeholderMatPath) == null)
        {
            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            placeholderMat = new Material(urpLitShader);
            placeholderMat.name = "PlaceholderMat";
            placeholderMat.color = Color.white;
            // Ensure opaque
            placeholderMat.SetFloat("_Surface", 0); // 0 = Opaque
            placeholderMat.renderQueue = (int)RenderQueue.Geometry;
            AssetDatabase.CreateAsset(placeholderMat, placeholderMatPath);
            results.AppendLine("Created PlaceholderMat.mat");
        }
        else
        {
            placeholderMat = AssetDatabase.LoadAssetAtPath<Material>(placeholderMatPath);
            results.AppendLine("PlaceholderMat.mat already exists");
        }

        // ===== 3. Create GhostMat (transparent URP/Lit, white alpha 0.5) =====
        string ghostMatPath = matFolder + "/GhostMat.mat";
        Material ghostMat;
        if (AssetDatabase.LoadAssetAtPath<Material>(ghostMatPath) == null)
        {
            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            ghostMat = new Material(urpLitShader);
            ghostMat.name = "GhostMat";
            // Set transparent surface
            ghostMat.SetFloat("_Surface", 1); // 1 = Transparent
            ghostMat.SetFloat("_Blend", 0);   // 0 = Alpha blend
            ghostMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            ghostMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            ghostMat.SetFloat("_ZWrite", 0);
            ghostMat.SetFloat("_AlphaClip", 0);
            ghostMat.renderQueue = (int)RenderQueue.Transparent;
            ghostMat.SetOverrideTag("RenderType", "Transparent");
            ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostMat.color = new Color(1f, 1f, 1f, 0.5f);
            AssetDatabase.CreateAsset(ghostMat, ghostMatPath);
            results.AppendLine("Created GhostMat.mat (transparent)");
        }
        else
        {
            ghostMat = AssetDatabase.LoadAssetAtPath<Material>(ghostMatPath);
            results.AppendLine("GhostMat.mat already exists");
        }

        // ===== 4. Create PlaceablePlaceholder prefab =====
        string prefabFolder = "Assets/_ShopMR/Prefabs";
        string prefabPath = prefabFolder + "/PlaceablePlaceholder.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            // Create temporary cube in scene
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PlaceablePlaceholder";
            cube.transform.position = Vector3.zero;
            cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Assign PlaceholderMat
            var rend = cube.GetComponent<Renderer>();
            if (rend != null && placeholderMat != null)
                rend.sharedMaterial = placeholderMat;

            // Add PlaceableFurniture component
            cube.AddComponent<ShopMR.Placement.PlaceableFurniture>();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
            Object.DestroyImmediate(cube);
            results.AppendLine($"Created PlaceablePlaceholder prefab at {prefabPath}");
        }
        else
        {
            results.AppendLine("PlaceablePlaceholder prefab already exists");
        }

        // ===== 5. Add FurniturePlacer to GameManager in scene =====
        var gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            results.AppendLine("ERROR: GameManager not found in scene!");
            return results.ToString();
        }

        // Check if FurniturePlacer already exists on GameManager or as a child
        var existingPlacer = gameManager.GetComponentInChildren<ShopMR.Placement.FurniturePlacer>(true);
        if (existingPlacer == null)
        {
            // Create as child of GameManager
            var placerGO = new GameObject("FurniturePlacer");
            placerGO.transform.SetParent(gameManager.transform);
            placerGO.transform.localPosition = Vector3.zero;

            var placer = placerGO.AddComponent<ShopMR.Placement.FurniturePlacer>();

            // Wire references
            var placeholderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (placeholderPrefab != null)
                placer.placeholderPrefab = placeholderPrefab;

            var ghostMatAsset = AssetDatabase.LoadAssetAtPath<Material>(ghostMatPath);
            if (ghostMatAsset != null)
                placer.ghostMaterial = ghostMatAsset;

            placer.ghostAlpha = 0.5f;
            placer.maxRayDistance = 10f;

            EditorUtility.SetDirty(placerGO);
            results.AppendLine("Created FurniturePlacer child on GameManager and wired references");
        }
        else
        {
            results.AppendLine("FurniturePlacer already exists in scene");
            // Ensure references are wired
            bool changed = false;
            if (existingPlacer.placeholderPrefab == null)
            {
                existingPlacer.placeholderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                changed = true;
            }
            if (existingPlacer.ghostMaterial == null)
            {
                existingPlacer.ghostMaterial = AssetDatabase.LoadAssetAtPath<Material>(ghostMatPath);
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(existingPlacer.gameObject);
                results.AppendLine("Updated FurniturePlacer references");
            }
        }

        // ===== 6. Save scene =====
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
