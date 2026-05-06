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
        public static CatalogPanelController Instance { get; private set; }

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
        [SerializeField] private float spawnDistance = 0.8f;
        [SerializeField] private float verticalOffset = -0.1f;

        [Header("Toggle Input")]
        [Tooltip("PC toggle key for in-Editor testing.")]
        [SerializeField] private KeyCode pcToggleKey = KeyCode.Tab;

        [Header("Behavior")]
        [SerializeField] private bool startHidden = true;

        private readonly List<ProductCardUI> spawnedCards = new List<ProductCardUI>();
        private readonly List<Button> spawnedCategoryButtons = new List<Button>();
        private string activeCategory = null; // null = "All"

        // CanvasGroup used for hide/show so that this script's Update() keeps running
        private CanvasGroup canvasGroup;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;

        /// <summary>Fired when the user taps a product card.</summary>
        public event Action<ProductData> OnProductSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Ensure a CanvasGroup exists for non-destructive show/hide
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

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

        // Debounce to prevent rapid toggling
        private float lastToggleTime;
        private const float TOGGLE_COOLDOWN = 0.4f;

        private void Update()
        {
            // PC toggle
            if (Input.GetKeyDown(pcToggleKey))
                Toggle();

            // Quest A button toggle via OVRInput (most reliable) + legacy fallback
            #if !UNITY_EDITOR
            bool questA = false;
            try { questA = OVRInput.GetDown(OVRInput.Button.One); } catch { }
            if (!questA) questA = Input.GetKeyDown(KeyCode.JoystickButton0);
            if (questA && Time.time - lastToggleTime > TOGGLE_COOLDOWN)
                Toggle();
            #endif

            #if UNITY_EDITOR
            // Editor: number keys 1-9,0 select visible product cards directly
            // (bypasses UI event system which Meta ISDK can block)
            if (IsVisible)
                HandleEditorNumberKeySelection();

            // Editor: physics-raycast click on card BoxColliders
            if (IsVisible && Input.GetMouseButtonDown(0))
                HandleEditorMouseClick();
            #endif
        }

        #if UNITY_EDITOR
        private void HandleEditorNumberKeySelection()
        {
            int index = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1)) index = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) index = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) index = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) index = 3;
            else if (Input.GetKeyDown(KeyCode.Alpha5)) index = 4;
            else if (Input.GetKeyDown(KeyCode.Alpha6)) index = 5;
            else if (Input.GetKeyDown(KeyCode.Alpha7)) index = 6;
            else if (Input.GetKeyDown(KeyCode.Alpha8)) index = 7;
            else if (Input.GetKeyDown(KeyCode.Alpha9)) index = 8;
            else if (Input.GetKeyDown(KeyCode.Alpha0)) index = 9;

            if (index >= 0 && index < spawnedCards.Count)
            {
                var card = spawnedCards[index];
                if (card != null && card.Product != null)
                {
                    Debug.Log($"[CatalogPanel] Editor key-select #{index + 1}: {card.Product.name}");
                    HandleCardSelected(card.Product);
                }
            }
        }

        private void HandleEditorMouseClick()
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                // Check if we hit a ProductCardUI's BoxCollider
                var cardUI = hit.collider.GetComponent<ProductCardUI>();
                if (cardUI != null && cardUI.Product != null)
                {
                    Debug.Log($"[CatalogPanel] Editor mouse-click on: {cardUI.Product.name}");
                    HandleCardSelected(cardUI.Product);
                }
            }
        }
        #endif

        // ---------- Visibility ----------

        public void Toggle()
        {
            lastToggleTime = Time.time;
            if (IsVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (canvasGroup == null) return;
            PositionInFrontOfHead();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            // Re-enable physics collider so ISDK interactions work
            SetCollidersEnabled(true);
            Debug.Log("[CatalogPanel] Shown");
        }

        public void Hide()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            // Disable physics collider so placement raycasts pass through
            SetCollidersEnabled(false);
            Debug.Log("[CatalogPanel] Hidden");
        }

        private void SetCollidersEnabled(bool enabled)
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
                col.enabled = enabled;
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

            // Move the Canvas root (parent), not this child panel,
            // because this panel uses stretch anchors inside the canvas.
            Transform canvasRoot = transform.parent != null ? transform.parent : transform;
            canvasRoot.position = pos;
            canvasRoot.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ---------- Catalog data binding ----------

        private void HandleCatalogLoaded(bool success)
        {
            if (!success)
            {
                if (statusText != null) statusText.text = "Backend not connected. Start the server to browse catalog.";
                // Clear any previously spawned cards
                foreach (var c in spawnedCards) if (c != null) Destroy(c.gameObject);
                spawnedCards.Clear();
                foreach (var b in spawnedCategoryButtons) if (b != null) Destroy(b.gameObject);
                spawnedCategoryButtons.Clear();
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

            Debug.Log($"[CatalogPanel] RefreshCards: spawned {spawnedCards.Count} cards into {(cardContainer != null ? cardContainer.name : "null")}");
            #if UNITY_EDITOR
            if (spawnedCards.Count > 0)
            {
                Debug.Log("[CatalogPanel] Editor tip: Press 1-9 to select a product directly while catalog is open");
            }
            #endif
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