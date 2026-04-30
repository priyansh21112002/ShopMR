using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class BuildCatalogPanelPrefab
{
    public static string Execute()
    {
        string prefabDir = "Assets/_ShopMR/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/_ShopMR", "Prefabs");

        // Find TMP font
        TMP_FontAsset font = null;
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            if (candidate != null) { font = candidate; break; }
        }
        if (font == null) return "ERROR: No TMP font asset found.";

        // ============================================================
        // PART A: CategoryButton prefab
        // ============================================================
        GameObject catBtnRoot = new GameObject("CategoryButton");
        RectTransform catBtnRT = catBtnRoot.AddComponent<RectTransform>();
        catBtnRT.sizeDelta = new Vector2(140, 50);

        Image catBtnImg = catBtnRoot.AddComponent<Image>();
        catBtnImg.color = new Color(60f/255f, 70f/255f, 90f/255f, 230f/255f);
        catBtnImg.raycastTarget = true;

        Button catBtn = catBtnRoot.AddComponent<Button>();
        catBtn.targetGraphic = catBtnImg;
        catBtn.transition = Selectable.Transition.ColorTint;
        ColorBlock catBtnColors = catBtn.colors;
        catBtnColors.normalColor = Color.white;
        catBtnColors.highlightedColor = new Color(180f/255f, 220f/255f, 255f/255f, 1f);
        catBtnColors.pressedColor = new Color(120f/255f, 180f/255f, 230f/255f, 1f);
        catBtnColors.selectedColor = Color.white;
        catBtnColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        catBtn.colors = catBtnColors;

        BoxCollider catBtnCol = catBtnRoot.AddComponent<BoxCollider>();
        catBtnCol.size = new Vector3(140, 50, 1);
        catBtnCol.center = Vector3.zero;

        // Child text
        GameObject catBtnTextGO = new GameObject("Text (TMP)");
        catBtnTextGO.transform.SetParent(catBtnRoot.transform, false);
        RectTransform catBtnTextRT = catBtnTextGO.AddComponent<RectTransform>();
        catBtnTextRT.anchorMin = Vector2.zero;
        catBtnTextRT.anchorMax = Vector2.one;
        catBtnTextRT.offsetMin = Vector2.zero;
        catBtnTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI catBtnTMP = catBtnTextGO.AddComponent<TextMeshProUGUI>();
        catBtnTMP.font = font;
        catBtnTMP.text = "ALL";
        catBtnTMP.fontSize = 22;
        catBtnTMP.fontStyle = FontStyles.Bold;
        catBtnTMP.color = Color.white;
        catBtnTMP.alignment = TextAlignmentOptions.Center;
        catBtnTMP.raycastTarget = false;

        string catBtnPath = prefabDir + "/CategoryButton.prefab";
        GameObject catBtnPrefab = PrefabUtility.SaveAsPrefabAsset(catBtnRoot, catBtnPath);
        Object.DestroyImmediate(catBtnRoot);

        if (catBtnPrefab == null) return "ERROR: Failed to save CategoryButton prefab.";

        // ============================================================
        // PART B: CatalogPanel prefab
        // ============================================================

        // --- Root: CatalogPanel ---
        GameObject panelRoot = new GameObject("CatalogPanel");
        RectTransform panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(800, 600);

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(20f/255f, 20f/255f, 25f/255f, 245f/255f);
        panelBg.raycastTarget = true;

        BoxCollider panelCol = panelRoot.AddComponent<BoxCollider>();
        panelCol.size = new Vector3(800, 600, 1);
        panelCol.center = Vector3.zero;

        // --- TitleText ---
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(panelRoot.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(-40, 60); // -40 = 20px inset each side
        titleRT.offsetMin = new Vector2(20, titleRT.offsetMin.y);
        titleRT.offsetMax = new Vector2(-20, titleRT.offsetMax.y);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = font;
        titleTMP.text = "ShopMR Catalog";
        titleTMP.fontSize = 42;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        // Fix title rect: top-stretch, height=60, 20px left/right inset, 10px from top
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.offsetMin = new Vector2(20, -70); // bottom edge: -(10 + 60) = -70
        titleRT.offsetMax = new Vector2(-20, -10); // top edge: -10

        // --- CategoryRow ---
        GameObject catRowGO = new GameObject("CategoryRow");
        catRowGO.transform.SetParent(panelRoot.transform, false);
        RectTransform catRowRT = catRowGO.AddComponent<RectTransform>();
        catRowRT.anchorMin = new Vector2(0, 1);
        catRowRT.anchorMax = new Vector2(1, 1);
        catRowRT.pivot = new Vector2(0.5f, 1f);
        catRowRT.offsetMin = new Vector2(20, -140); // bottom: -(80 + 60) = -140
        catRowRT.offsetMax = new Vector2(-20, -80);  // top: -80

        HorizontalLayoutGroup hlg = catRowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childScaleWidth = false;
        hlg.childScaleHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // --- CardScrollView ---
        // Build from scratch: ScrollView root -> Viewport -> Content, + Scrollbar Vertical
        GameObject scrollGO = new GameObject("CardScrollView");
        scrollGO.transform.SetParent(panelRoot.transform, false);
        RectTransform scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = new Vector2(20, 60);   // left=20, bottom=60
        scrollRT.offsetMax = new Vector2(-20, -160); // right=-20, top=-160

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(15f/255f, 15f/255f, 20f/255f, 1f);
        scrollBg.raycastTarget = true;

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportRT.pivot = new Vector2(0, 1);

        Image viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(1, 1, 1, 0); // transparent
        Mask viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scrollRect.viewport = viewportRT;

        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0); // ContentSizeFitter manages height

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childScaleWidth = false;
        vlg.childScaleHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // Vertical Scrollbar
        GameObject scrollbarGO = new GameObject("Scrollbar Vertical");
        scrollbarGO.transform.SetParent(scrollGO.transform, false);
        RectTransform scrollbarRT = scrollbarGO.AddComponent<RectTransform>();
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 0.5f);
        scrollbarRT.sizeDelta = new Vector2(20, 0);
        scrollbarRT.anchoredPosition = Vector2.zero;

        Image scrollbarBgImg = scrollbarGO.AddComponent<Image>();
        scrollbarBgImg.color = new Color(0.1f, 0.1f, 0.12f, 1f);

        Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar sliding area
        GameObject slidingAreaGO = new GameObject("Sliding Area");
        slidingAreaGO.transform.SetParent(scrollbarGO.transform, false);
        RectTransform slidingAreaRT = slidingAreaGO.AddComponent<RectTransform>();
        slidingAreaRT.anchorMin = Vector2.zero;
        slidingAreaRT.anchorMax = Vector2.one;
        slidingAreaRT.offsetMin = new Vector2(10, 10);
        slidingAreaRT.offsetMax = new Vector2(-10, -10);

        // Scrollbar handle
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(slidingAreaGO.transform, false);
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = new Vector2(-10, -10);
        handleRT.offsetMax = new Vector2(10, 10);

        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.4f, 0.4f, 0.45f, 1f);

        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3;

        // --- StatusText ---
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(panelRoot.transform, false);
        RectTransform statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0);
        statusRT.anchorMax = new Vector2(1, 0);
        statusRT.pivot = new Vector2(0.5f, 0f);
        statusRT.offsetMin = new Vector2(20, 10);  // left=20, bottom=10
        statusRT.offsetMax = new Vector2(-20, 50);  // right=-20, top=50 (height=40)
        TextMeshProUGUI statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.font = font;
        statusTMP.text = "Loading\u2026";
        statusTMP.fontSize = 22;
        statusTMP.color = new Color(180f/255f, 180f/255f, 180f/255f, 1f);
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.raycastTarget = false;

        // ============================================================
        // Attach CatalogPanelController and bind fields
        // ============================================================
        var controller = panelRoot.AddComponent<ShopMR.Catalog.CatalogPanelController>();

        // Load the ProductCard prefab reference
        var productCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabDir + "/ProductCard.prefab");
        var productCardUI = productCardPrefab != null
            ? productCardPrefab.GetComponent<ShopMR.Catalog.ProductCardUI>()
            : null;

        // Load the CategoryButton prefab reference
        var catBtnPrefabLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(catBtnPath);
        var catBtnComponent = catBtnPrefabLoaded != null
            ? catBtnPrefabLoaded.GetComponent<Button>()
            : null;

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        so.FindProperty("cardPrefab").objectReferenceValue = productCardUI;
        so.FindProperty("cardContainer").objectReferenceValue = contentGO.transform;
        so.FindProperty("categoryButtonContainer").objectReferenceValue = catRowGO.transform;
        so.FindProperty("categoryButtonPrefab").objectReferenceValue = catBtnComponent;
        so.FindProperty("statusText").objectReferenceValue = statusTMP;
        so.FindProperty("titleText").objectReferenceValue = titleTMP;
        so.FindProperty("spawnDistance").floatValue = 1.5f;
        so.FindProperty("verticalOffset").floatValue = -0.1f;
        so.FindProperty("pcToggleKey").intValue = (int)KeyCode.Tab;
        so.FindProperty("startHidden").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Save as prefab
        string catalogPanelPath = prefabDir + "/CatalogPanel.prefab";
        GameObject catalogPanelPrefab = PrefabUtility.SaveAsPrefabAsset(panelRoot, catalogPanelPath);
        Object.DestroyImmediate(panelRoot);

        AssetDatabase.Refresh();

        string result = "";
        result += catBtnPrefab != null
            ? $"CategoryButton prefab saved to {catBtnPath}. "
            : "ERROR: CategoryButton prefab failed. ";
        result += catalogPanelPrefab != null
            ? $"CatalogPanel prefab saved to {catalogPanelPath}. "
            : "ERROR: CatalogPanel prefab failed. ";
        result += $"ProductCardUI ref: {(productCardUI != null ? "bound" : "MISSING")}. ";
        result += $"CategoryButton ref: {(catBtnComponent != null ? "bound" : "MISSING")}. ";
        result += $"Font: {font.name}";

        return result;
    }
}
