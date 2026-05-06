using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ShopMR.Networking;

namespace ShopMR.Catalog
{
    /// <summary>
    /// Fetches and caches the product catalog from the backend.
    /// Provides query helpers (by category, by id) for UI consumers.
    /// Falls back to an embedded catalog if the API is unreachable.
    /// </summary>
    public class CatalogManager : MonoBehaviour
    {
        public static CatalogManager Instance { get; private set; }

        [Header("Fetch Settings")]
        [Tooltip("Fetch products automatically when the session becomes active.")]
        [SerializeField] private bool autoFetchOnSessionStart = true;

        [Tooltip("Maximum products to fetch (backend supports up to ~50).")]
        [SerializeField] private int fetchLimit = 50;

        // Cached state
        private List<ProductData> allProducts = new List<ProductData>();
        private Dictionary<string, ProductData> productsById = new Dictionary<string, ProductData>();
        private Dictionary<string, List<ProductData>> productsByCategory = new Dictionary<string, List<ProductData>>();

        public IReadOnlyList<ProductData> AllProducts => allProducts;
        public IReadOnlyList<string> Categories => productsByCategory.Keys.ToList();
        public bool HasFetched { get; private set; }
        public bool IsFetching { get; private set; }

        /// <summary>Fired once the catalog finishes loading (success or failure).</summary>
        public event Action<bool> OnCatalogLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (!autoFetchOnSessionStart) return;

            // Wait until session is active (SessionManager fetches it on its own Start)
            float timeout = 15f;
            float elapsed = 0f;
            while ((Core.SessionManager.Instance == null || !Core.SessionManager.Instance.IsSessionActive)
                   && elapsed < timeout)
            {
                await Task.Yield();
                elapsed += Time.deltaTime;
            }

            if (Core.SessionManager.Instance == null || !Core.SessionManager.Instance.IsSessionActive)
            {
                Debug.LogWarning("[CatalogManager] No active session after timeout — fetching anyway.");
            }

            await FetchCatalog();
        }

        /// <summary>
        /// Fetch all products from the backend. Safe to call multiple times — replaces the cache.
        /// Falls back to embedded catalog if the API is unreachable or returns empty.
        /// </summary>
        public async Task<bool> FetchCatalog()
        {
            if (IsFetching)
            {
                Debug.Log("[CatalogManager] Fetch already in progress, skipping.");
                return false;
            }

            if (APIClient.Instance == null)
            {
                Debug.LogError("[CatalogManager] APIClient.Instance is null. Is GameManager in the scene?");
                HasFetched = false;
                OnCatalogLoaded?.Invoke(false);
                return false;
            }

            IsFetching = true;
            Debug.Log($"[CatalogManager] Fetching products (limit={fetchLimit}) from {APIClient.Instance.BaseUrl} ...");

            try
            {
                ProductListResponse resp = await APIClient.Instance.GetProducts(category: null, limit: fetchLimit);

                if (resp != null && resp.products != null && resp.products.Length > 0)
                {
                    BuildCache(resp.products);
                    HasFetched = true;

                    Debug.Log($"[CatalogManager] ✅ Loaded {allProducts.Count} products across {productsByCategory.Count} categories: " +
                              $"{string.Join(", ", productsByCategory.Keys)}");

                    foreach (var p in allProducts.Take(3))
                    {
                        Debug.Log($"  • {p.product_id} | {p.name} | {p.category} | ${p.price}");
                    }

                    OnCatalogLoaded?.Invoke(true);
                    return true;
                }

                // API returned null/empty — no fallback, stay empty
                Debug.LogWarning("[CatalogManager] Fetch returned no products — catalog will remain empty.");
                HasFetched = false;
                OnCatalogLoaded?.Invoke(false);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CatalogManager] API unreachable ({ex.Message}) — catalog will remain empty until backend is started.");
            }
            finally
            {
                IsFetching = false;
            }

            // No fallback — catalog stays empty until backend is available
            HasFetched = false;
            OnCatalogLoaded?.Invoke(false);
            return false;
        }

        private void BuildCache(ProductData[] products)
        {
            allProducts = products.ToList();
            productsById.Clear();
            productsByCategory.Clear();

            foreach (var p in allProducts)
            {
                if (string.IsNullOrEmpty(p.product_id)) continue;

                productsById[p.product_id] = p;

                string cat = string.IsNullOrEmpty(p.category) ? "uncategorized" : p.category.ToLower();
                if (!productsByCategory.ContainsKey(cat))
                    productsByCategory[cat] = new List<ProductData>();
                productsByCategory[cat].Add(p);
            }
        }

        // ---------- Fallback embedded catalog ----------

        private void LoadFallbackCatalog()
        {
            var fallback = new ProductData[]
            {
                MakeProduct("prod-001", "Modern Leather Sofa",     "sofa",  899.99f, "Furniture/sofa_modern_leather"),
                MakeProduct("prod-002", "Scandinavian Sofa",       "sofa",  749.99f, "Furniture/sofa_scandinavian"),
                MakeProduct("prod-003", "Velvet Sectional Sofa",   "sofa", 1299.99f, "Furniture/sofa_velvet_sectional"),
                MakeProduct("prod-004", "Ergonomic Office Chair",  "chair", 449.99f, "Furniture/chair_ergonomic"),
                MakeProduct("prod-005", "Accent Chair",            "chair", 349.99f, "Furniture/chair_accent"),
                MakeProduct("prod-006", "Rattan Chair",            "chair", 279.99f, "Furniture/chair_rattan"),
                MakeProduct("prod-007", "Oak Dining Table",        "table", 599.99f, "Furniture/table_oak_dining"),
                MakeProduct("prod-008", "Glass Coffee Table",      "table", 349.99f, "Furniture/table_glass_coffee"),
                MakeProduct("prod-009", "Marble Side Table",       "table", 249.99f, "Furniture/table_marble_side"),
                MakeProduct("prod-010", "Arc Floor Lamp",          "lamp",  189.99f, "Furniture/lamp_arc_floor"),
                MakeProduct("prod-011", "Ceramic Table Lamp",      "lamp",  129.99f, "Furniture/lamp_ceramic_table"),
                MakeProduct("prod-012", "Industrial Shelf",        "shelf", 399.99f, "Furniture/shelf_industrial"),
                MakeProduct("prod-013", "Floating Shelf",          "shelf", 149.99f, "Furniture/shelf_floating"),
                MakeProduct("prod-014", "Minimalist Bed",          "bed",   799.99f, "Furniture/bed_minimalist"),
                MakeProduct("prod-015", "Upholstered Bed",         "bed",   999.99f, "Furniture/bed_upholstered"),
                MakeProduct("prod-016", "Standing Desk",           "desk",  549.99f, "Furniture/desk_standing"),
                MakeProduct("prod-017", "Writing Desk",            "desk",  429.99f, "Furniture/desk_writing"),
                MakeProduct("prod-018", "Ceramic Planter",         "decor",  59.99f, "Furniture/decor_planter"),
                MakeProduct("prod-019", "Abstract Wall Art",       "decor", 179.99f, "Furniture/decor_wall_art"),
                MakeProduct("prod-020", "Area Rug",                "decor", 249.99f, "Furniture/decor_area_rug"),
            };

            BuildCache(fallback);
            HasFetched = true;
            IsFetching = false;
            Debug.Log($"[CatalogManager] ✅ Fallback catalog loaded: {allProducts.Count} products across {productsByCategory.Count} categories");
            OnCatalogLoaded?.Invoke(true);
        }

        private static ProductData MakeProduct(string id, string name, string category, float price, string modelUrl)
        {
            return new ProductData
            {
                product_id = id,
                name = name,
                category = category,
                price = price,
                description = $"{name} — {category}",
                model_url = modelUrl,
                is_active = true
            };
        }

        // ---------- Query helpers ----------

        public ProductData GetById(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            return productsById.TryGetValue(productId, out var p) ? p : null;
        }

        public IReadOnlyList<ProductData> GetByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return allProducts;
            string key = category.ToLower();
            return productsByCategory.TryGetValue(key, out var list)
                ? list
                : new List<ProductData>();
        }

        public int CountByCategory(string category)
        {
            return GetByCategory(category).Count;
        }
    }
}
