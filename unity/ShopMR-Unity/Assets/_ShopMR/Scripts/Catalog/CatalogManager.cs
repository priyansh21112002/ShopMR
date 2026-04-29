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
                return false;
            }

            IsFetching = true;
            Debug.Log($"[CatalogManager] Fetching products (limit={fetchLimit}) from {APIClient.Instance.BaseUrl} ...");

            try
            {
                ProductListResponse resp = await APIClient.Instance.GetProducts(category: null, limit: fetchLimit);

                if (resp == null || resp.products == null)
                {
                    Debug.LogError("[CatalogManager] Fetch failed — null response.");
                    HasFetched = false;
                    OnCatalogLoaded?.Invoke(false);
                    return false;
                }

                BuildCache(resp.products);
                HasFetched = true;

                Debug.Log($"[CatalogManager] ✅ Loaded {allProducts.Count} products across {productsByCategory.Count} categories: " +
                          $"{string.Join(", ", productsByCategory.Keys)}");

                // Print a sample for verification
                foreach (var p in allProducts.Take(3))
                {
                    Debug.Log($"  • {p.product_id} | {p.name} | {p.category} | ${p.price}");
                }

                OnCatalogLoaded?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CatalogManager] Exception during fetch: {ex.Message}\n{ex.StackTrace}");
                HasFetched = false;
                OnCatalogLoaded?.Invoke(false);
                return false;
            }
            finally
            {
                IsFetching = false;
            }
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
