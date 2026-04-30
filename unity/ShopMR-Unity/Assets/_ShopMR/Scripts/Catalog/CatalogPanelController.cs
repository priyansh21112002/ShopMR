using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopMR.Networking;
using ShopMR.Core;

namespace ShopMR.Catalog
{
    /// <summary>
    /// Controls the floating catalog panel: visibility, category filtering, card spawning.
    /// Subscribe to OnProductSelected to react when the user taps a card.
    /// </summary>
    public class CatalogPanelController : MonoBehaviour
    {
        [Header("Panel Root")]
        [Tooltip("The root GameObject to show/hide. Should be a child world-space Canvas.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Card Spawning")]
        [SerializeField] private ProductCardUI cardPrefab;
        [Tooltip("Parent transform under a ScrollRect Content where cards will be instantiated.")]
        [SerializeField] private Transform cardContainer;

        [Header("Category Filter")]
        [Tooltip("Parent transform where category filter buttons will be instantiated.")]
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private Button categoryButtonPrefab;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text titleText;

        [Header("Positioning")]
        [Tooltip("Camera the panel positions itself in front of when shown. Auto-detected if null.")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private float spawnDistance = 1.5f;
        [SerializeField] private float verticalOffset = -0.1f;

        [Header("Toggle Input")]
        [Tooltip("PC toggle key for in-Editor testing.")]
        [SerializeField] private KeyCode pcToggleKey = KeyCode.Tab;

        [Header("Behavior")]
        [SerializeField] private bool startHidden = true;

        private readonly List<ProductCardUI> spawnedCards = new List<ProductCardUI>();
        private readonly List<Button> spawnedCategoryButtons = new List<Button>();
        private string activeCategory = null; // null = "All"

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        /// <summary>Fired when the user taps a product card.</summary>
        public event Action<ProductData> OnProductSelected;

        private void Start()
        {
            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;

            if (statusText != null) statusText.text = "Loading catalog…";
            if (titleText != null)  titleText.text  = "ShopMR Catalog";

            if (CatalogManager.Instance != null)
            {
                CatalogManager.Instance.OnCatalogLoaded += HandleCatalogLoaded;
                if (CatalogManager.Instance.HasFetched)
                    HandleCatalogLoaded(true);
            }
            else
            {
                Debug.LogWarning("[CatalogPanel] CatalogManager.Instance is null at Start.");
            }

            if (startHidden) Hide();
        }

        private void OnDestroy()
        {
            if (CatalogManager.Instance != null)
                CatalogManager.Instance.OnCatalogLoaded -= HandleCatalogLoaded;
        }

        private void Update()
        {
            // PC toggle
            if (Input.GetKeyDown(pcToggleKey))
                Toggle();

            // Quest right A button toggle (Joystick button 0 maps to A on most setups)
            #if !UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.JoystickButton0))
                Toggle();
            #endif
        }

        // ---------- Visibility ----------

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (panelRoot == null) return;
            PositionInFrontOfHead();
            panelRoot.SetActive(true);
            Debug.Log("[CatalogPanel] Shown");
        }

        public void Hide()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
            Debug.Log("[CatalogPanel] Hidden");
        }

        private void PositionInFrontOfHead()
        {
            if (headTransform == null) return;

            // Project forward onto horizontal plane to keep panel upright
            Vector3 fwd = headTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 pos = headTransform.position + fwd * spawnDistance;
            pos.y = headTransform.position.y + verticalOffset;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ---------- Catalog data binding ----------

        private void HandleCatalogLoaded(bool success)
        {
            if (!success)
            {
                if (statusText != null) statusText.text = "Failed to load catalog.";
                return;
            }

            BuildCategoryButtons();
            RefreshCards();
        }

        private void BuildCategoryButtons()
        {
            // Clear old
            foreach (var b in spawnedCategoryButtons) if (b != null) Destroy(b.gameObject);
            spawnedCategoryButtons.Clear();

            if (categoryButtonContainer == null || categoryButtonPrefab == null) return;

            var categories = new List<string> { null }; // null = All
            categories.AddRange(CatalogManager.Instance.Categories.OrderBy(c => c));

            foreach (var cat in categories)
            {
                Button btn = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = cat == null ? "All" : cat.ToUpper();

                string capturedCat = cat;
                btn.onClick.AddListener(() => SetCategoryFilter(capturedCat));
                spawnedCategoryButtons.Add(btn);
            }
        }

        public void SetCategoryFilter(string category)
        {
            activeCategory = category;
            RefreshCards();
        }

        private void RefreshCards()
        {
            // Clear old
            foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
            spawnedCards.Clear();

            if (CatalogManager.Instance == null || cardPrefab == null || cardContainer == null) return;

            IReadOnlyList<ProductData> products = string.IsNullOrEmpty(activeCategory)
                ? CatalogManager.Instance.AllProducts
                : CatalogManager.Instance.GetByCategory(activeCategory);

            foreach (var p in products)
            {
                ProductCardUI card = Instantiate(cardPrefab, cardContainer);
                card.Bind(p);
                card.OnSelected += HandleCardSelected;
                spawnedCards.Add(card);
            }

            if (statusText != null)
            {
                string filterLabel = string.IsNullOrEmpty(activeCategory) ? "all" : activeCategory;
                statusText.text = $"Showing {spawnedCards.Count} {filterLabel} item(s)";
            }
        }

        private void HandleCardSelected(ProductData product)
        {
            if (product == null) return;

            // Track view event
            if (EventTracker.Instance != null)
                EventTracker.Instance.TrackView(product.product_id);

            OnProductSelected?.Invoke(product);
        }
    }
}