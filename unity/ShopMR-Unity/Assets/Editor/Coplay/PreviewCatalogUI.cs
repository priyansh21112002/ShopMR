using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Populates the catalog panel with mock product cards and category buttons
/// so we can preview the UI styling in edit mode.
/// </summary>
public class PreviewCatalogUI
{
    static readonly string[] Categories = { "All", "SOFA", "CHAIR", "TABLE", "LAMP", "SHELF", "BED", "DESK", "DECOR" };

    static readonly (string name, string category, string price)[] MockProducts = {
        ("Modern Leather Sofa",         "SOFA",  "$899.99"),
        ("Scandinavian Fabric Sofa",    "SOFA",  "$649.99"),
        ("Velvet Sectional Sofa",       "SOFA",  "$1299.99"),
        ("Ergonomic Office Chair",      "CHAIR", "$349.99"),
        ("Mid-Century Accent Chair",    "CHAIR", "$299.99"),
        ("Oak Dining Table",            "TABLE", "$599.99"),
        ("Glass Coffee Table",          "TABLE", "$249.99"),
        ("Arc Floor Lamp",              "LAMP",  "$149.99"),
        ("Industrial Bookshelf",        "SHELF", "$329.99"),
        ("Upholstered Platform Bed",    "BED",   "$749.99"),
    };

    static readonly Dictionary<string, Color> CategoryColors = new Dictionary<string, Color>
    {
        { "SOFA",  new Color(0.30f, 0.55f, 0.95f) },
        { "CHAIR", new Color(0.40f, 0.80f, 0.45f) },
        { "TABLE", new Color(0.70f, 0.50f, 0.30f) },
        { "LAMP",  new Color(0.95f, 0.85f, 0.30f) },
        { "SHELF", new Color(0.65f, 0.65f, 0.65f) },
        { "BED",   new Color(0.65f, 0.40f, 0.85f) },
        { "DESK",  new Color(0.95f, 0.55f, 0.25f) },
        { "DECOR", new Color(0.95f, 0.45f, 0.65f) },
    };

    public static string Execute()
    {
        // ── Populate category buttons ──
        var catRow = GameObject.Find("CatalogCanvas/CatalogPanel/CategoryRow");
        if (catRow != null)
        {
            // Clear existing children
            for (int i = catRow.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(catRow.transform.GetChild(i).gameObject);

            var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/CategoryButton.prefab");
            if (btnPrefab != null)
            {
                foreach (var cat in Categories)
                {
                    var btn = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab);
                    btn.transform.SetParent(catRow.transform, false);
                    btn.name = $"Cat_{cat}";
                    var tmp = btn.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = cat;
                }
            }
        }

        // ── Populate product cards ──
        var content = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Viewport/Content");
        if (content != null)
        {
            // Clear existing children
            for (int i = content.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.transform.GetChild(i).gameObject);

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/ProductCard.prefab");
            if (cardPrefab != null)
            {
                foreach (var (name, category, price) in MockProducts)
                {
                    var card = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
                    card.transform.SetParent(content.transform, false);
                    card.name = $"Mock_{name}";

                    // Populate texts
                    var nameGO = card.transform.Find("NameText");
                    if (nameGO != null)
                    {
                        var tmp = nameGO.GetComponent<TMP_Text>();
                        if (tmp != null) tmp.text = name;
                    }

                    var catGO = card.transform.Find("CategoryText");
                    if (catGO != null)
                    {
                        var tmp = catGO.GetComponent<TMP_Text>();
                        if (tmp != null) tmp.text = category;
                    }

                    var priceGO = card.transform.Find("PriceText");
                    if (priceGO != null)
                    {
                        var tmp = priceGO.GetComponent<TMP_Text>();
                        if (tmp != null) tmp.text = price;
                    }

                    var stripeGO = card.transform.Find("CategoryStripe");
                    if (stripeGO != null)
                    {
                        var img = stripeGO.GetComponent<Image>();
                        if (img != null && CategoryColors.TryGetValue(category, out var col))
                            img.color = col;
                    }
                }
            }
        }

        // Update status text
        var statusGO = GameObject.Find("CatalogCanvas/CatalogPanel/StatusText");
        if (statusGO != null)
        {
            var tmp = statusGO.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "Showing 10 all item(s)";
        }

        // Make panel visible (in case CanvasGroup alpha is 0)
        var panel = GameObject.Find("CatalogCanvas/CatalogPanel");
        if (panel != null)
        {
            var cg = panel.GetComponentInParent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        return $"Populated {Categories.Length} category buttons and {MockProducts.Length} product cards for preview.";
    }
}
