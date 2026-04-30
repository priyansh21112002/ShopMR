using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopMR.Networking;

namespace ShopMR.Catalog
{
    /// <summary>
    /// Behavior for a single product card in the catalog panel.
    /// Bind UI fields in the Inspector via the prefab.
    /// </summary>
    public class ProductCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image categoryStripe;     // colored bar on the left
        [SerializeField] private Button cardButton;

        public ProductData Product { get; private set; }
        public event Action<ProductData> OnSelected;

        private static readonly System.Collections.Generic.Dictionary<string, Color> CategoryColors =
            new System.Collections.Generic.Dictionary<string, Color>
            {
                { "sofa",   new Color(0.30f, 0.55f, 0.95f) }, // blue
                { "chair",  new Color(0.40f, 0.80f, 0.45f) }, // green
                { "table",  new Color(0.70f, 0.50f, 0.30f) }, // brown
                { "lamp",   new Color(0.95f, 0.85f, 0.30f) }, // yellow
                { "shelf",  new Color(0.65f, 0.65f, 0.65f) }, // gray
                { "bed",    new Color(0.65f, 0.40f, 0.85f) }, // purple
                { "desk",   new Color(0.95f, 0.55f, 0.25f) }, // orange
                { "decor",  new Color(0.95f, 0.45f, 0.65f) }, // pink
            };

        private void Awake()
        {
            if (cardButton != null)
                cardButton.onClick.AddListener(HandleClick);
        }

        public void Bind(ProductData product)
        {
            Product = product;
            if (product == null) return;

            if (nameText != null)     nameText.text = product.name;
            if (categoryText != null) categoryText.text = product.category?.ToUpper() ?? "";
            if (priceText != null)    priceText.text = $"${product.price:0.00}";

            if (categoryStripe != null)
            {
                string key = product.category?.ToLower() ?? "";
                categoryStripe.color = CategoryColors.TryGetValue(key, out var c)
                    ? c
                    : new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void HandleClick()
        {
            if (Product == null) return;
            Debug.Log($"[ProductCard] Selected: {Product.product_id} | {Product.name}");
            OnSelected?.Invoke(Product);
        }
    }
}