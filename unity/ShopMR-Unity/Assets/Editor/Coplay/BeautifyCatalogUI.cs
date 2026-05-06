using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Beautifies the ShopMR Catalog UI for optimal readability on Quest 3 passthrough.
/// Generates rounded-corner sprite assets, then applies a modern dark-glass theme
/// to the CatalogPanel, ProductCard prefab, and CategoryButton prefab.
/// </summary>
public class BeautifyCatalogUI
{
    private const string SPRITE_DIR = "Assets/_ShopMR/Sprites/UI";

    // ── Theme palette (optimized for MR passthrough contrast) ──
    static readonly Color PanelBg        = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    static readonly Color HeaderBg       = new Color(0.10f, 0.12f, 0.18f, 0.95f);
    static readonly Color CardBg         = new Color(0.12f, 0.12f, 0.16f, 0.94f);
    static readonly Color CardBgHover    = new Color(0.18f, 0.22f, 0.32f, 0.96f);
    static readonly Color CardBgPressed  = new Color(0.12f, 0.16f, 0.28f, 0.98f);
    static readonly Color CatBtnBg       = new Color(0.14f, 0.16f, 0.22f, 0.88f);
    static readonly Color CatBtnHover    = new Color(0.22f, 0.35f, 0.65f, 0.95f);
    static readonly Color CatBtnPressed  = new Color(0.18f, 0.28f, 0.55f, 0.98f);
    static readonly Color ScrollTrack    = new Color(0.08f, 0.08f, 0.12f, 0.60f);
    static readonly Color ScrollHandle   = new Color(0.40f, 0.50f, 0.75f, 0.70f);
    static readonly Color AccentBlue     = new Color(0.35f, 0.60f, 1.00f, 1.00f);
    static readonly Color TextWhite      = new Color(0.95f, 0.95f, 0.97f, 1.00f);
    static readonly Color TextSubtle     = new Color(0.60f, 0.62f, 0.70f, 1.00f);
    static readonly Color PriceGreen     = new Color(0.40f, 0.92f, 0.55f, 1.00f);
    static readonly Color ScrollBg       = new Color(0.08f, 0.08f, 0.12f, 0.50f);
    static readonly Color DividerColor   = new Color(0.25f, 0.28f, 0.38f, 0.40f);

    public static string Execute()
    {
        // 1. Generate rounded-corner sprites
        var panelSprite   = GenerateRoundedRect("UIRoundedPanel",   128, 128, 24, PanelBg);
        var cardSprite    = GenerateRoundedRect("UIRoundedCard",     64,  64, 12, Color.white);
        var buttonSprite  = GenerateRoundedRect("UIRoundedButton",   64,  64, 24, Color.white);  // pill-ish
        var handleSprite  = GenerateRoundedRect("UIRoundedHandle",   16,  64,  8, Color.white);

        AssetDatabase.Refresh();

        // Re-load sprites after refresh
        panelSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/UIRoundedPanel.png");
        cardSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/UIRoundedCard.png");
        buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/UIRoundedButton.png");
        handleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/UIRoundedHandle.png");

        int changes = 0;

        // 2. Beautify scene CatalogPanel
        changes += BeautifyPanel(panelSprite);

        // 3. Beautify ProductCard prefab
        changes += BeautifyProductCard(cardSprite);

        // 4. Beautify CategoryButton prefab
        changes += BeautifyCategoryButton(buttonSprite);

        // 5. Beautify scrollbar
        changes += BeautifyScrollbar(handleSprite);

        AssetDatabase.SaveAssets();

        return $"Applied {changes} UI changes. Generated 4 rounded-rect sprites. Theme: frosted dark glass for MR passthrough.";
    }

    // ═══════════════════════════════════════════════════════════
    //  SPRITE GENERATION
    // ═══════════════════════════════════════════════════════════

    static Sprite GenerateRoundedRect(string name, int w, int h, int radius, Color fillColor)
    {
        Directory.CreateDirectory(SPRITE_DIR);
        string path = $"{SPRITE_DIR}/{name}.png";

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float alpha = RoundedRectSDF(x, y, w, h, radius);
                pixels[y * w + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Configure as sliced sprite
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // Set 9-slice border (radius on all sides)
            int border = radius + 2;
            importer.spriteBorder = new Vector4(border, border, border, border);

            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float RoundedRectSDF(int px, int py, int w, int h, int r)
    {
        // Compute signed distance from a rounded rect, return 0-1 alpha
        float cx = Mathf.Abs(px - (w - 1) * 0.5f) - ((w - 1) * 0.5f - r);
        float cy = Mathf.Abs(py - (h - 1) * 0.5f) - ((h - 1) * 0.5f - r);

        float dx = Mathf.Max(cx, 0f);
        float dy = Mathf.Max(cy, 0f);
        float dist = Mathf.Sqrt(dx * dx + dy * dy) - r;

        // Anti-alias edge (1.5 pixel smoothing)
        return Mathf.Clamp01(0.5f - dist / 1.5f);
    }

    // ═══════════════════════════════════════════════════════════
    //  CATALOG PANEL (scene objects)
    // ═══════════════════════════════════════════════════════════

    static int BeautifyPanel(Sprite panelSprite)
    {
        int c = 0;
        var panel = GameObject.Find("CatalogCanvas/CatalogPanel");
        if (panel == null) { Debug.LogWarning("[BeautifyUI] CatalogPanel not found"); return 0; }

        // Panel background
        var panelImg = panel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = PanelBg;
            panelImg.pixelsPerUnitMultiplier = 1f;
            c++;
        }

        // ── Title ──
        var titleGO = FindChild(panel.transform, "TitleText");
        if (titleGO != null)
        {
            var titleTMP = titleGO.GetComponent<TMP_Text>();
            if (titleTMP != null)
            {
                titleTMP.fontSize = 38;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.color = TextWhite;
                titleTMP.characterSpacing = 2f;
                titleTMP.text = "<color=#5A9CFF>Shop</color>MR";
                c++;
            }

            // Resize title area and add bottom padding
            var titleRT = titleGO.GetComponent<RectTransform>();
            if (titleRT != null)
            {
                titleRT.anchoredPosition = new Vector2(0, -12);
                titleRT.sizeDelta = new Vector2(-40, 52);
                c++;
            }
        }

        // ── Category Row ──
        var catRow = FindChild(panel.transform, "CategoryRow");
        if (catRow != null)
        {
            var catRT = catRow.GetComponent<RectTransform>();
            if (catRT != null)
            {
                catRT.anchoredPosition = new Vector2(0, -70);
                catRT.sizeDelta = new Vector2(-30, 48);
                c++;
            }
            var hlg = catRow.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = 8;
                hlg.padding = new RectOffset(5, 5, 0, 0);
                c++;
            }
        }

        // ── Card ScrollView ──
        var scrollGO = FindChild(panel.transform, "CardScrollView");
        if (scrollGO != null)
        {
            var scrollImg = scrollGO.GetComponent<Image>();
            if (scrollImg != null)
            {
                scrollImg.color = ScrollBg;
                c++;
            }
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            if (scrollRT != null)
            {
                scrollRT.anchoredPosition = new Vector2(0, -40);
                scrollRT.sizeDelta = new Vector2(-24, -190);
                c++;
            }
        }

        // ── Content layout ──
        var content = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Viewport/Content");
        if (content != null)
        {
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 6;
                vlg.padding = new RectOffset(4, 4, 4, 4);
                c++;
            }
        }

        // ── Status text ──
        var statusGO = FindChild(panel.transform, "StatusText");
        if (statusGO != null)
        {
            var statusTMP = statusGO.GetComponent<TMP_Text>();
            if (statusTMP != null)
            {
                statusTMP.fontSize = 18;
                statusTMP.color = TextSubtle;
                statusTMP.fontStyle = FontStyles.Italic;
                c++;
            }
            var statusRT = statusGO.GetComponent<RectTransform>();
            if (statusRT != null)
            {
                statusRT.anchoredPosition = new Vector2(0, 8);
                statusRT.sizeDelta = new Vector2(-30, 32);
                c++;
            }
        }

        EditorUtility.SetDirty(panel);
        return c;
    }

    // ═══════════════════════════════════════════════════════════
    //  PRODUCT CARD PREFAB
    // ═══════════════════════════════════════════════════════════

    static int BeautifyProductCard(Sprite cardSprite)
    {
        string path = "Assets/_ShopMR/Prefabs/ProductCard.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[BeautifyUI] ProductCard.prefab not found"); return 0; }

        int c = 0;

        // Open prefab for editing
        var root = PrefabUtility.LoadPrefabContents(path);

        // Card background
        var cardImg = root.GetComponent<Image>();
        if (cardImg != null)
        {
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = CardBg;
            c++;
        }

        // Card button hover/press colors
        var btn = root.GetComponent<Button>();
        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(CardBgHover.r / CardBg.r, CardBgHover.g / CardBg.g, CardBgHover.b / CardBg.b, 1f);
            colors.pressedColor = new Color(CardBgPressed.r / CardBg.r, CardBgPressed.g / CardBg.g, CardBgPressed.b / CardBg.b, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            c++;
        }

        // Card size — slightly taller for breathing room
        var cardRT = root.GetComponent<RectTransform>();
        if (cardRT != null)
        {
            cardRT.sizeDelta = new Vector2(760, 100);
            c++;
        }

        // Update BoxCollider to match
        var boxCol = root.GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.size = new Vector3(760, 100, 1);
            c++;
        }

        // ── Category Stripe — wider, rounded ──
        var stripe = FindChild(root.transform, "CategoryStripe");
        if (stripe != null)
        {
            var stripeRT = stripe.GetComponent<RectTransform>();
            if (stripeRT != null)
            {
                stripeRT.sizeDelta = new Vector2(6, -12);
                stripeRT.anchoredPosition = new Vector2(6, 0);
                c++;
            }
        }

        // ── Name text — larger, better weight ──
        var nameGO = FindChild(root.transform, "NameText");
        if (nameGO != null)
        {
            var nameTMP = nameGO.GetComponent<TMP_Text>();
            if (nameTMP != null)
            {
                nameTMP.fontSize = 28;
                nameTMP.fontStyle = FontStyles.Bold;
                nameTMP.color = TextWhite;
                nameTMP.enableAutoSizing = false;
                nameTMP.overflowMode = TextOverflowModes.Ellipsis;
                c++;
            }
            var nameRT = nameGO.GetComponent<RectTransform>();
            if (nameRT != null)
            {
                nameRT.anchoredPosition = new Vector2(22, -2);
                nameRT.sizeDelta = new Vector2(480, 0);
                c++;
            }
        }

        // ── Category label ──
        var catGO = FindChild(root.transform, "CategoryText");
        if (catGO != null)
        {
            var catTMP = catGO.GetComponent<TMP_Text>();
            if (catTMP != null)
            {
                catTMP.fontSize = 18;
                catTMP.fontStyle = FontStyles.Normal;
                catTMP.color = TextSubtle;
                catTMP.characterSpacing = 1.5f;
                c++;
            }
            var catRT = catGO.GetComponent<RectTransform>();
            if (catRT != null)
            {
                catRT.anchoredPosition = new Vector2(22, 2);
                catRT.sizeDelta = new Vector2(480, 0);
                c++;
            }
        }

        // ── Price — bright green, prominent ──
        var priceGO = FindChild(root.transform, "PriceText");
        if (priceGO != null)
        {
            var priceTMP = priceGO.GetComponent<TMP_Text>();
            if (priceTMP != null)
            {
                priceTMP.fontSize = 32;
                priceTMP.fontStyle = FontStyles.Bold;
                priceTMP.color = PriceGreen;
                c++;
            }
            var priceRT = priceGO.GetComponent<RectTransform>();
            if (priceRT != null)
            {
                priceRT.anchoredPosition = new Vector2(-14, 0);
                priceRT.sizeDelta = new Vector2(180, 0);
                c++;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        return c;
    }

    // ═══════════════════════════════════════════════════════════
    //  CATEGORY BUTTON PREFAB
    // ═══════════════════════════════════════════════════════════

    static int BeautifyCategoryButton(Sprite buttonSprite)
    {
        string path = "Assets/_ShopMR/Prefabs/CategoryButton.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[BeautifyUI] CategoryButton.prefab not found"); return 0; }

        int c = 0;

        var root = PrefabUtility.LoadPrefabContents(path);

        // Button background
        var img = root.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = CatBtnBg;
            c++;
        }

        // Size — slightly smaller pill
        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(110, 40);
            c++;
        }

        // BoxCollider
        var col = root.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size = new Vector3(110, 40, 1);
            c++;
        }

        // Button colors
        var btn = root.GetComponent<Button>();
        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(
                CatBtnHover.r / Mathf.Max(CatBtnBg.r, 0.01f),
                CatBtnHover.g / Mathf.Max(CatBtnBg.g, 0.01f),
                CatBtnHover.b / Mathf.Max(CatBtnBg.b, 0.01f), 1f);
            colors.pressedColor = new Color(
                CatBtnPressed.r / Mathf.Max(CatBtnBg.r, 0.01f),
                CatBtnPressed.g / Mathf.Max(CatBtnBg.g, 0.01f),
                CatBtnPressed.b / Mathf.Max(CatBtnBg.b, 0.01f), 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            c++;
        }

        // Text styling
        var tmpText = root.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.fontSize = 18;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = TextWhite;
            tmpText.characterSpacing = 1f;
            c++;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        return c;
    }

    // ═══════════════════════════════════════════════════════════
    //  SCROLLBAR
    // ═══════════════════════════════════════════════════════════

    static int BeautifyScrollbar(Sprite handleSprite)
    {
        int c = 0;

        // Track
        var trackGO = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Scrollbar Vertical");
        if (trackGO != null)
        {
            var trackImg = trackGO.GetComponent<Image>();
            if (trackImg != null)
            {
                trackImg.color = ScrollTrack;
                c++;
            }
            var trackRT = trackGO.GetComponent<RectTransform>();
            if (trackRT != null)
            {
                trackRT.sizeDelta = new Vector2(8, 0);  // thinner
                c++;
            }
        }

        // Handle
        var handleGO = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Scrollbar Vertical/Sliding Area/Handle");
        if (handleGO != null)
        {
            var handleImg = handleGO.GetComponent<Image>();
            if (handleImg != null)
            {
                handleImg.sprite = handleSprite;
                handleImg.type = Image.Type.Sliced;
                handleImg.color = ScrollHandle;
                c++;
            }
        }

        if (trackGO != null) EditorUtility.SetDirty(trackGO);
        return c;
    }

    // ═══════════════════════════════════════════════════════════

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
        }
        return null;
    }
}
