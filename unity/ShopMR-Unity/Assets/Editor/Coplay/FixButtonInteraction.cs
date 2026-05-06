using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Fixes Button.targetGraphic on both ProductCard and CategoryButton prefabs.
/// Without targetGraphic, ColorTint transition has no target → no hover/press feedback.
/// Also ensures scrollbar interactability and other interaction polish.
/// </summary>
public class FixButtonInteraction
{
    public static string Execute()
    {
        int fixes = 0;

        // ── Fix ProductCard prefab ──
        fixes += FixPrefabTargetGraphic(
            "Assets/_ShopMR/Prefabs/ProductCard.prefab",
            "ProductCard",
            highlightedColor: new Color(0.18f, 0.22f, 0.30f, 0.98f),
            pressedColor: new Color(0.14f, 0.18f, 0.28f, 1.0f)
        );

        // ── Fix CategoryButton prefab ──
        fixes += FixPrefabTargetGraphic(
            "Assets/_ShopMR/Prefabs/CategoryButton.prefab",
            "CategoryButton",
            highlightedColor: new Color(0.22f, 0.35f, 0.65f, 0.98f),
            pressedColor: new Color(0.18f, 0.28f, 0.55f, 1.0f)
        );

        // ── Ensure CatalogPanel BoxCollider doesn't block card clicks ──
        // The panel BoxCollider is used by ISDK for surface detection, 
        // but GraphicRaycaster handles per-element clicks, so it's fine.

        // ── Verify scrollbar handle has raycastTarget = true ──
        var handle = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Scrollbar Vertical/Sliding Area/Handle");
        if (handle != null)
        {
            var img = handle.GetComponent<Image>();
            if (img != null && !img.raycastTarget)
            {
                img.raycastTarget = true;
                EditorUtility.SetDirty(handle);
                fixes++;
                Debug.Log("[FixInteraction] Enabled raycastTarget on scrollbar handle");
            }
        }

        AssetDatabase.SaveAssets();
        return $"Fixed {fixes} interaction issues. Button.targetGraphic now wired to root Image on both prefabs.";
    }

    static int FixPrefabTargetGraphic(string path, string rootName, Color highlightedColor, Color pressedColor)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning($"[FixInteraction] Not found: {path}"); return 0; }

        int c = 0;
        var root = PrefabUtility.LoadPrefabContents(path);

        var btn = root.GetComponent<Button>();
        var img = root.GetComponent<Image>();

        if (btn != null && img != null)
        {
            // Wire targetGraphic to the root Image
            btn.targetGraphic = img;

            // Set proper ColorTint colors (absolute colors, not ratios)
            // Since ColorTint multiplies normalColor * Image.color, and we set normalColor=white,
            // the Image.color IS the normal state. Highlighted/Pressed should be direct colors.
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(
                highlightedColor.r / Mathf.Max(img.color.r, 0.01f),
                highlightedColor.g / Mathf.Max(img.color.g, 0.01f),
                highlightedColor.b / Mathf.Max(img.color.b, 0.01f),
                1f
            );
            colors.pressedColor = new Color(
                pressedColor.r / Mathf.Max(img.color.r, 0.01f),
                pressedColor.g / Mathf.Max(img.color.g, 0.01f),
                pressedColor.b / Mathf.Max(img.color.b, 0.01f),
                1f
            );
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            c++;
            Debug.Log($"[FixInteraction] {rootName}: wired targetGraphic + hover/press colors");
        }
        else
        {
            Debug.LogWarning($"[FixInteraction] {rootName}: Button or Image missing");
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return c;
    }
}
