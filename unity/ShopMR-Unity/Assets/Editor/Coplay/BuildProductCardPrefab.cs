using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class BuildProductCardPrefab
{
    public static string Execute()
    {
        // Ensure TMP Essentials are imported
        string tmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        if (!File.Exists(tmpSettingsPath))
        {
            // Try to import TMP essentials
            var importMethod = typeof(TMPro.TMP_Settings).Assembly.GetType("TMPro.TMP_PackageUtilities");
            if (importMethod == null)
            {
                // Unity 6 uses a different path - try the TMP_PackageResourceImporter
                var windowType = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                    .FirstOrDefault(t => t.Name == "TMP_PackageResourceImporterWindow");
                
                // If we can't auto-import, just note it and continue - the font might still be available
                Debug.Log("[BuildProductCard] TMP Settings not found at expected path, will try to use default font.");
            }
        }

        // Find TMP font asset - don't rely on TMP_Settings.defaultFontAsset which can NPE
        TMP_FontAsset defaultFont = null;
        string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (var guid in fontGuids)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(guid);
            var candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (candidate != null)
            {
                defaultFont = candidate;
                Debug.Log($"[BuildProductCard] Using font: {fontPath}");
                break;
            }
        }

        if (defaultFont == null)
        {
            return "ERROR: No TMP font asset found. Please import TMP Essentials first (Window > TextMeshPro > Import TMP Essential Resources).";
        }

        // ==============================
        // Create ProductCard as a standalone GameObject (not under a canvas yet)
        // We'll save it directly as a prefab
        // ==============================

        // Create the root ProductCard
        GameObject cardRoot = new GameObject("ProductCard");
        RectTransform cardRT = cardRoot.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(760, 110);

        // Add Image (panel background)
        Image cardBg = cardRoot.AddComponent<Image>();
        cardBg.color = new Color(40f/255f, 40f/255f, 45f/255f, 230f/255f);
        cardBg.raycastTarget = true;

        // Add Button
        Button cardButton = cardRoot.AddComponent<Button>();
        cardButton.targetGraphic = cardBg;
        cardButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = cardButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(180f/255f, 220f/255f, 255f/255f, 1f);
        colors.pressedColor = new Color(120f/255f, 180f/255f, 230f/255f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        cardButton.colors = colors;

        // Add BoxCollider for ray hits
        BoxCollider collider = cardRoot.AddComponent<BoxCollider>();
        collider.size = new Vector3(760, 110, 1);
        collider.center = Vector3.zero;

        // ==============================
        // CategoryStripe (left colored bar)
        // ==============================
        GameObject stripeObj = new GameObject("CategoryStripe");
        stripeObj.transform.SetParent(cardRoot.transform, false);
        RectTransform stripeRT = stripeObj.AddComponent<RectTransform>();
        // Anchor to left side, stretch vertically
        stripeRT.anchorMin = new Vector2(0, 0);
        stripeRT.anchorMax = new Vector2(0, 1);
        stripeRT.pivot = new Vector2(0, 0.5f);
        stripeRT.anchoredPosition = new Vector2(0, 0);
        stripeRT.sizeDelta = new Vector2(12, 0); // width=12, height stretches
        Image stripeImg = stripeObj.AddComponent<Image>();
        stripeImg.color = new Color(0.3f, 0.55f, 0.95f); // default blue, overridden at runtime
        stripeImg.raycastTarget = false;

        // ==============================
        // NameText
        // ==============================
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(cardRoot.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        // Position: left side, upper portion
        nameRT.anchorMin = new Vector2(0, 0.5f);
        nameRT.anchorMax = new Vector2(0, 1f);
        nameRT.pivot = new Vector2(0, 1f);
        nameRT.anchoredPosition = new Vector2(24, 0); // 24px from left (past stripe)
        nameRT.sizeDelta = new Vector2(540, 0); // height stretches from anchor
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.font = defaultFont;
        nameTMP.text = "Product Name";
        nameTMP.fontSize = 32;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.enableWordWrapping = false;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;
        nameTMP.raycastTarget = false;

        // ==============================
        // CategoryText
        // ==============================
        GameObject catObj = new GameObject("CategoryText");
        catObj.transform.SetParent(cardRoot.transform, false);
        RectTransform catRT = catObj.AddComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 0);
        catRT.anchorMax = new Vector2(0, 0.5f);
        catRT.pivot = new Vector2(0, 0);
        catRT.anchoredPosition = new Vector2(24, 0);
        catRT.sizeDelta = new Vector2(540, 0);
        TextMeshProUGUI catTMP = catObj.AddComponent<TextMeshProUGUI>();
        catTMP.font = defaultFont;
        catTMP.text = "CATEGORY";
        catTMP.fontSize = 20;
        catTMP.color = new Color(180f/255f, 180f/255f, 180f/255f, 1f);
        catTMP.alignment = TextAlignmentOptions.Left;
        catTMP.raycastTarget = false;

        // ==============================
        // PriceText
        // ==============================
        GameObject priceObj = new GameObject("PriceText");
        priceObj.transform.SetParent(cardRoot.transform, false);
        RectTransform priceRT = priceObj.AddComponent<RectTransform>();
        // Anchor to right side, vertically centered
        priceRT.anchorMin = new Vector2(1, 0);
        priceRT.anchorMax = new Vector2(1, 1);
        priceRT.pivot = new Vector2(1, 0.5f);
        priceRT.anchoredPosition = new Vector2(-10, 0); // 10px inset from right
        priceRT.sizeDelta = new Vector2(180, 0); // height stretches
        TextMeshProUGUI priceTMP = priceObj.AddComponent<TextMeshProUGUI>();
        priceTMP.font = defaultFont;
        priceTMP.text = "$0.00";
        priceTMP.fontSize = 36;
        priceTMP.fontStyle = FontStyles.Bold;
        priceTMP.color = new Color(150f/255f, 230f/255f, 150f/255f, 1f);
        priceTMP.alignment = TextAlignmentOptions.Right;
        priceTMP.raycastTarget = false;

        // ==============================
        // Attach ProductCardUI script and bind fields
        // ==============================
        var cardUI = cardRoot.AddComponent<ShopMR.Catalog.ProductCardUI>();

        // Use SerializedObject to bind the private serialized fields
        SerializedObject so = new SerializedObject(cardUI);
        so.FindProperty("nameText").objectReferenceValue = nameTMP;
        so.FindProperty("categoryText").objectReferenceValue = catTMP;
        so.FindProperty("priceText").objectReferenceValue = priceTMP;
        so.FindProperty("categoryStripe").objectReferenceValue = stripeImg;
        so.FindProperty("cardButton").objectReferenceValue = cardButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ==============================
        // Save as prefab
        // ==============================
        string prefabDir = "Assets/_ShopMR/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets/_ShopMR", "Prefabs");
        }

        string prefabPath = prefabDir + "/ProductCard.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardRoot, prefabPath);

        // Clean up the temp scene object
        Object.DestroyImmediate(cardRoot);

        AssetDatabase.Refresh();

        if (prefab != null)
        {
            return $"SUCCESS: ProductCard prefab saved to {prefabPath}. " +
                   $"Children: CategoryStripe, NameText, CategoryText, PriceText. " +
                   $"Components: Image, Button, BoxCollider, ProductCardUI (fields bound). " +
                   $"Font: {defaultFont.name}";
        }
        else
        {
            return "ERROR: Failed to save prefab.";
        }
    }
}
