using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixViewportMask
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // Find the Viewport under CardScrollView
        var viewport = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Viewport");
        if (viewport == null)
        {
            // Try alternative search
            var scrollView = GameObject.Find("CardScrollView");
            if (scrollView != null)
            {
                var vp = scrollView.transform.Find("Viewport");
                if (vp != null) viewport = vp.gameObject;
            }
        }

        if (viewport == null)
            return "ERROR: Viewport not found";

        // 1. Remove old Mask component
        var mask = viewport.GetComponent<Mask>();
        if (mask != null)
        {
            Object.DestroyImmediate(mask);
            results.AppendLine("Removed Mask component from Viewport");
        }

        // 2. Remove the Image component that was used by Mask (alpha=0, invisible)
        // Actually keep it — RectMask2D doesn't need it, but ScrollRect might need a graphic
        // for drag detection. Just make sure it's there.
        var img = viewport.GetComponent<Image>();
        if (img != null)
        {
            // Keep the image but make it invisible (for ScrollRect drag)
            img.color = new Color(1f, 1f, 1f, 0f);
            results.AppendLine("Kept Image (alpha=0) for ScrollRect drag detection");
        }

        // 3. Add RectMask2D — scissor-based clipping, works better with TMP in URP
        var rectMask = viewport.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            rectMask = viewport.AddComponent<RectMask2D>();
            results.AppendLine("Added RectMask2D to Viewport");
        }
        else
        {
            results.AppendLine("RectMask2D already present");
        }

        // 4. Also make the card background a bit brighter for better visibility
        string cardPrefabPath = "Assets/_ShopMR/Prefabs/ProductCard.prefab";
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath);
        if (cardPrefab != null)
        {
            // Open prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(cardPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            var cardImage = prefabRoot.GetComponent<Image>();
            if (cardImage != null)
            {
                // Brighten the card background from near-black to a visible dark grey
                cardImage.color = new Color(0.22f, 0.22f, 0.25f, 0.95f);
                results.AppendLine($"Brightened card background to (0.22, 0.22, 0.25, 0.95)");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        EditorUtility.SetDirty(viewport);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved: {scene.path}");

        return results.ToString();
    }
}
