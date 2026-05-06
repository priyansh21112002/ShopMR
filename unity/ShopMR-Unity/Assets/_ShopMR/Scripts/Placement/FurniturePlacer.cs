using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ShopMR.Networking;
using ShopMR.Core;
using ShopMR.Catalog;

namespace ShopMR.Placement
{
    /// <summary>
    /// Manages the furniture placement flow:
    /// 1. Listens for OnProductSelected from CatalogPanelController
    /// 2. Loads the model prefab via product.model_url (falls back to placeholder cube)
    /// 3. Spawns a translucent ghost preview that follows the controller/mouse raycast
    /// 4. Confirms placement on trigger press (Quest) or left-click/Space (Editor)
    /// 5. Cancels placement on B button (Quest) or Escape (Editor)
    /// </summary>
    public class FurniturePlacer : MonoBehaviour
    {
        public static FurniturePlacer Instance { get; private set; }

        [Header("References")]
        [Tooltip("Drag PlaceablePlaceholder.prefab here. Used as fallback when model_url is missing.")]
        public GameObject placeholderPrefab;

        [Tooltip("Optional: parent transform for placed objects (keeps hierarchy tidy). Auto-created if null.")]
        public Transform placedRoot;

        [Header("Ghost Preview")]
        [Tooltip("Material applied to the ghost preview (should be transparent URP/Lit).")]
        public Material ghostMaterial;
        [Range(0.1f, 1f)] public float ghostAlpha = 0.5f;

        [Header("Raycast")]
        public float maxRayDistance = 10f;
        public LayerMask placementLayers = ~0;

        [Header("Input (Editor)")]
        public KeyCode editorConfirmKey = KeyCode.Space;
        public KeyCode editorCancelKey = KeyCode.Escape;

        [Header("Rotation")]
        [Tooltip("Speed of Y-axis rotation via right thumbstick (degrees per second).")]
        public float rotationSpeed = 120f;

        [Header("Category Color Tints (placeholder fallback)")]
        public CategoryColor[] categoryColors = new CategoryColor[]
        {
            new CategoryColor { category = "sofa",  color = new Color(0.4f, 0.6f, 0.9f) },
            new CategoryColor { category = "chair", color = new Color(0.9f, 0.5f, 0.3f) },
            new CategoryColor { category = "table", color = new Color(0.6f, 0.4f, 0.2f) },
            new CategoryColor { category = "lamp",  color = new Color(0.9f, 0.85f, 0.4f) },
            new CategoryColor { category = "shelf", color = new Color(0.5f, 0.7f, 0.5f) },
            new CategoryColor { category = "bed",   color = new Color(0.7f, 0.4f, 0.7f) },
            new CategoryColor { category = "desk",  color = new Color(0.5f, 0.5f, 0.5f) },
            new CategoryColor { category = "decor", color = new Color(0.9f, 0.4f, 0.5f) },
        };

        [System.Serializable]
        public struct CategoryColor
        {
            public string category;
            public Color color;
        }

        [Header("Per-Product Tinting")]
        [Tooltip("Tint strength on real models (0 = original color, 1 = full tint). Recommended 0.6-0.85 for low-poly models sharing a single material.")]
        [Range(0f, 1f)] public float productTintStrength = 0.75f;

        /// <summary>
        /// Per-product color tints keyed by product_id.
        /// Ensures duplicate-mesh products (e.g. three sofas sharing sofa_001)
        /// are visually distinguishable at runtime.
        /// Colors are chosen to match the real-world product descriptions.
        /// </summary>
        private static readonly Dictionary<string, Color> ProductTintMap = new Dictionary<string, Color>
        {
            // Sofas (all share sofa_001) — high contrast needed
            { "prod-001", new Color(0.30f, 0.30f, 0.32f) },  // charcoal grey leather
            { "prod-002", new Color(0.92f, 0.82f, 0.62f) },  // warm beige fabric
            { "prod-003", new Color(0.12f, 0.15f, 0.50f) },  // deep navy velvet

            // Chairs (lounge_chair_001 shared by prod-005, prod-006)
            { "prod-004", new Color(0.20f, 0.20f, 0.22f) },  // black mesh office
            { "prod-005", new Color(0.95f, 0.78f, 0.12f) },  // mustard yellow
            { "prod-006", new Color(0.95f, 0.90f, 0.75f) },  // natural rattan / cream

            // Tables (coffee_table_001 shared by prod-008, prod-009)
            { "prod-007", new Color(0.78f, 0.58f, 0.30f) },  // warm oak
            { "prod-008", new Color(0.90f, 0.78f, 0.30f) },  // gold metal frame
            { "prod-009", new Color(0.18f, 0.18f, 0.20f) },  // black marble base

            // Lamps (unique meshes — subtle tints)
            { "prod-010", new Color(0.82f, 0.82f, 0.85f) },  // brushed nickel
            { "prod-011", new Color(0.88f, 0.50f, 0.28f) },  // terracotta ceramic

            // Shelves (unique meshes)
            { "prod-012", new Color(0.28f, 0.24f, 0.20f) },  // dark iron + reclaimed wood
            { "prod-013", new Color(0.65f, 0.48f, 0.28f) },  // walnut

            // Beds (bed_001 shared by prod-014, prod-015) — needs clear distinction
            { "prod-014", new Color(0.58f, 0.58f, 0.65f) },  // cool grey upholstery
            { "prod-015", new Color(0.90f, 0.80f, 0.55f) },  // warm light pine

            // Desks (office_table_001 shared by prod-016, prod-017)
            { "prod-016", new Color(0.82f, 0.72f, 0.38f) },  // warm bamboo
            { "prod-017", new Color(0.95f, 0.95f, 0.95f) },  // clean white

            // Decor (unique meshes)
            { "prod-018", new Color(0.95f, 0.95f, 0.92f) },  // matte white planter
            { "prod-019", new Color(0.22f, 0.38f, 0.75f) },  // blue + gold abstract
            { "prod-020", new Color(0.80f, 0.72f, 0.48f) },  // natural jute
        };

        // URP Lit shader property IDs - cached once
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        // State
        private bool isPlacing;
        private ProductData currentProduct;
        private GameObject ghostInstance;
        private GameObject sourcePrefab;
        private readonly List<PlaceableFurniture> placedItems = new List<PlaceableFurniture>();

        // Cached controller reference (avoids GameObject.Find every frame)
        private Transform cachedRightController;

        // Trigger debounce (prevents immediate confirm on same frame as catalog click)
        private float placementStartTime;
        private const float INPUT_DEBOUNCE = 0.3f;

        // Confirm trigger debounce (prevents repeated confirms while trigger is held)
        private bool triggerWasDown;

        // Manual rotation tracked across frames (right thumbstick Y-axis rotation)
        private float ghostYRotation;

        // Public events
        public System.Action<PlaceableFurniture> OnFurniturePlaced;
        public System.Action OnPlacementCancelled;

        // Public accessors
        public bool IsPlacing => isPlacing;
        public int PlacedCount => placedItems.Count;
        public IReadOnlyList<PlaceableFurniture> PlacedItems => placedItems;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to catalog selection
            if (CatalogPanelController.Instance != null)
            {
                CatalogPanelController.Instance.OnProductSelected += BeginPlacement;
                Debug.Log("[FurniturePlacer] Subscribed to CatalogPanelController.OnProductSelected");
            }
            else
            {
                StartCoroutine(WaitForCatalogController());
            }

            if (placedRoot == null)
            {
                var go = new GameObject("PlacedFurniture");
                placedRoot = go.transform;
            }
        }

        private IEnumerator WaitForCatalogController()
        {
            float timeout = 10f;
            float t = 0f;
            while (CatalogPanelController.Instance == null && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (CatalogPanelController.Instance != null)
            {
                CatalogPanelController.Instance.OnProductSelected += BeginPlacement;
                Debug.Log("[FurniturePlacer] Subscribed to CatalogPanelController.OnProductSelected (deferred)");
            }
            else
            {
                Debug.LogWarning("[FurniturePlacer] CatalogPanelController not found after timeout.");
            }
        }

        private void OnDestroy()
        {
            if (CatalogPanelController.Instance != null)
            {
                CatalogPanelController.Instance.OnProductSelected -= BeginPlacement;
            }
        }

        private void Update()
        {
            if (isPlacing)
            {
                HandleRotationInput();
                UpdateGhostPosition();
                HandleInput();
            }
            else
            {
                HandleRemoveInput();
            }
        }

        // ===== Placement flow =====

        public void BeginPlacement(ProductData product)
        {
            if (product == null) return;

            CancelPlacement(); // clear any existing ghost

            currentProduct = product;

            // Resolve prefab: try Resources.Load(product.model_url), fall back to placeholder
            sourcePrefab = ResolvePrefab(product);
            if (sourcePrefab == null)
            {
                Debug.LogWarning($"[FurniturePlacer] No prefab resolved for {product.name}. Aborting placement.");
                return;
            }

            // Spawn ghost - use per-product tint so duplicate meshes are visually distinct
            ghostInstance = Instantiate(sourcePrefab);
            ghostInstance.name = $"Ghost_{product.name}";
            Color ghostTint = GetProductTint(product);
            MakeGhost(ghostInstance, ghostTint);

            isPlacing = true;
            placementStartTime = Time.time;
            triggerWasDown = false;

            // Initialize rotation facing away from the user (toward where they're looking)
            if (Camera.main != null)
            {
                Vector3 fwd = Camera.main.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f)
                    ghostYRotation = Quaternion.LookRotation(fwd.normalized, Vector3.up).eulerAngles.y;
                else
                    ghostYRotation = 0f;
            }
            else
            {
                ghostYRotation = 0f;
            }

            // Hide catalog so user can see the room
            if (CatalogPanelController.Instance != null)
                CatalogPanelController.Instance.Hide();

            Debug.Log($"[FurniturePlacer] Began placing {product.name} (using {(sourcePrefab == placeholderPrefab ? "placeholder" : "real model")})");
        }

        private GameObject ResolvePrefab(ProductData product)
        {
            // Try real model from Resources/<model_url>
            if (!string.IsNullOrEmpty(product.model_url))
            {
                var loaded = Resources.Load<GameObject>(product.model_url);
                if (loaded != null) return loaded;
                Debug.Log($"[FurniturePlacer] Resources.Load failed for '{product.model_url}', falling back to placeholder.");
            }
            return placeholderPrefab;
        }

        private void HandleRotationInput()
        {
#if UNITY_EDITOR
            // Editor: Q/E keys or mouse scroll to rotate
            if (Input.GetKey(KeyCode.Q)) ghostYRotation -= rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) ghostYRotation += rotationSpeed * Time.deltaTime;
            ghostYRotation += Input.mouseScrollDelta.y * 15f;
#else
            // Quest: right controller thumbstick X-axis rotates object on Y-axis
            // PrimaryThumbstick = right controller (dominant hand)
            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (Mathf.Abs(thumbstick.x) > 0.15f) // deadzone
            {
                ghostYRotation += thumbstick.x * rotationSpeed * Time.deltaTime;
            }
#endif
        }

        private void UpdateGhostPosition()
        {
            if (ghostInstance == null) return;

            Ray ray = GetPointerRay();
            Vector3 placementPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers))
            {
                ghostInstance.SetActive(true);
                placementPoint = hit.point;
            }
            else
            {
                // No physics surface hit — project controller ray onto the floor plane (y=0)
                // This is the common case in MR passthrough where no scene mesh is loaded.
                ghostInstance.SetActive(true);
                placementPoint = RayFloorIntersection(ray, 0f);
            }

            ghostInstance.transform.position = placementPoint;
            // Apply manual Y rotation controlled by right thumbstick
            ghostInstance.transform.rotation = Quaternion.Euler(0f, ghostYRotation, 0f);
        }

        /// <summary>
        /// Projects a ray onto a horizontal plane at the given Y height.
        /// Returns a sensible default if the ray is parallel to or pointing away from the plane.
        /// </summary>
        private Vector3 RayFloorIntersection(Ray ray, float floorY)
        {
            // Plane normal is up, so we need ray going downward to hit it
            float denom = -ray.direction.y; // dot(direction, -up)
            if (denom > 0.001f)
            {
                float t = (ray.origin.y - floorY) / denom;
                if (t > 0f && t < maxRayDistance)
                {
                    return ray.origin + ray.direction * t;
                }
            }
            // Fallback: place at a fixed distance along controller ray projected to floor
            Vector3 fwd = ray.origin + ray.direction * 2f;
            fwd.y = floorY;
            return fwd;
        }

        private Ray GetPointerRay()
        {
#if UNITY_EDITOR
            // Editor: use mouse
            if (Camera.main != null)
                return Camera.main.ScreenPointToRay(Input.mousePosition);
            return new Ray(Vector3.zero, Vector3.forward);
#else
            // Quest: use right controller forward ray (cached to avoid Find every frame)
            if (cachedRightController == null)
            {
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    // Use the tracked controller anchor from OVRCameraRig
                    var rightHand = cameraRig.rightHandAnchor;
                    if (rightHand != null)
                    {
                        foreach (var t in rightHand.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == "RightControllerAnchor")
                            {
                                cachedRightController = t;
                                break;
                            }
                        }
                    }
                }
                // Fallback search if OVRCameraRig approach didn't work
                if (cachedRightController == null)
                {
                    var allTransforms = FindObjectsOfType<Transform>(true);
                    foreach (var t in allTransforms)
                    {
                        if (t.name == "RightControllerAnchor")
                        {
                            cachedRightController = t;
                            break;
                        }
                    }
                }
            }
            if (cachedRightController != null)
            {
                return new Ray(cachedRightController.position, cachedRightController.forward);
            }
            // Last fallback: head gaze (should not happen with controllers connected)
            if (Camera.main != null)
                return new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            return new Ray(Vector3.zero, Vector3.forward);
#endif
        }

        private void HandleInput()
        {
            // Debounce to prevent immediate confirm on same frame as catalog click
            if (Time.time - placementStartTime < INPUT_DEBOUNCE) return;

            bool confirm = false;
            bool cancel = false;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(editorConfirmKey)) confirm = true;
            if (Input.GetKeyDown(editorCancelKey)) cancel = true;
#else
            // Right controller index trigger to confirm (PrimaryIndexTrigger = right hand)
            bool triggerIsDown = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
            // Only confirm on the rising edge (trigger just pressed, not held)
            if (triggerIsDown && !triggerWasDown) confirm = true;
            triggerWasDown = triggerIsDown;

            // B button on right controller to cancel
            if (OVRInput.GetDown(OVRInput.Button.Two)) cancel = true;
#endif

            if (confirm) ConfirmPlacement();
            else if (cancel) CancelPlacement();
        }

        /// <summary>
        /// When NOT in placement mode, B button removes the last placed item.
        /// </summary>
        private void HandleRemoveInput()
        {
            bool remove = false;

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                remove = true;
#else
            // B button on right controller to remove last placed item
            if (OVRInput.GetDown(OVRInput.Button.Two))
                remove = true;
#endif

            if (remove && placedItems.Count > 0)
            {
                RemoveLast();
            }
        }

        /// <summary>
        /// Remove the most recently placed furniture item.
        /// </summary>
        public void RemoveLast()
        {
            if (placedItems.Count == 0) return;

            var last = placedItems[placedItems.Count - 1];
            placedItems.RemoveAt(placedItems.Count - 1);

            if (last != null)
            {
                // Remove spatial anchor
                if (FurnitureAnchorManager.Instance != null)
                    FurnitureAnchorManager.Instance.RemoveAnchor(last.gameObject);

                Debug.Log($"[FurniturePlacer] Removed: {last.productName}");
                Destroy(last.gameObject);
            }
        }

        public void ConfirmPlacement()
        {
            if (!isPlacing || ghostInstance == null || currentProduct == null) return;

            // Spawn the real placed object at ghost's pose
            var placed = Instantiate(sourcePrefab, ghostInstance.transform.position, ghostInstance.transform.rotation, placedRoot);
            placed.name = $"Placed_{currentProduct.name}";

            // Apply per-product color tint so duplicate-mesh items are visually distinct
            if (sourcePrefab == placeholderPrefab)
            {
                // Placeholder cube: full category tint (opaque override)
                ApplyCategoryTint(placed, GetCategoryColor(currentProduct.category));
            }
            else
            {
                // Real 3D model: subtle per-product tint via MaterialPropertyBlock
                Color productColor = GetProductTint(currentProduct);
                ApplyProductTint(placed, productColor, productTintStrength);
            }

            // Attach behavior
            var pf = placed.GetComponent<PlaceableFurniture>();
            if (pf == null) pf = placed.AddComponent<PlaceableFurniture>();
            pf.Initialize(currentProduct);
            placedItems.Add(pf);

            // Track event via EventTracker
            if (EventTracker.Instance != null)
            {
                EventTracker.Instance.TrackPlace(currentProduct.product_id);
            }

            Debug.Log($"[FurniturePlacer] Placed {currentProduct.name} at {placed.transform.position}");

            // Anchor to spatial anchor for tracking stability
            if (FurnitureAnchorManager.Instance != null)
            {
                FurnitureAnchorManager.Instance.AnchorFurniture(
                    placed,
                    currentProduct.product_id,
                    currentProduct.name,
                    currentProduct.model_url,
                    currentProduct.category
                );
            }

            OnFurniturePlaced?.Invoke(pf);

            // Clean up ghost & state
            Destroy(ghostInstance);
            ghostInstance = null;
            isPlacing = false;
            currentProduct = null;
            sourcePrefab = null;
        }

        public void CancelPlacement()
        {
            if (ghostInstance != null) Destroy(ghostInstance);
            ghostInstance = null;
            if (isPlacing)
            {
                Debug.Log("[FurniturePlacer] Placement cancelled.");
                OnPlacementCancelled?.Invoke();
                // Re-show catalog when cancelled
                if (CatalogPanelController.Instance != null)
                    CatalogPanelController.Instance.Show();
            }
            isPlacing = false;
            currentProduct = null;
            sourcePrefab = null;
        }

        public void RemoveAll()
        {
            // Clean up spatial anchors
            if (FurnitureAnchorManager.Instance != null)
                FurnitureAnchorManager.Instance.RemoveAllAnchors();

            foreach (var p in placedItems)
                if (p != null) Destroy(p.gameObject);
            placedItems.Clear();
        }

        // ===== Helpers =====

        private Color GetCategoryColor(string category)
        {
            if (string.IsNullOrEmpty(category)) return Color.white;
            string c = category.ToLower();
            foreach (var cc in categoryColors)
                if (cc.category == c) return cc.color;
            return Color.white;
        }

        private void MakeGhost(GameObject go, Color tint)
        {
            tint.a = ghostAlpha;
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    if (ghostMaterial != null)
                        mats[i] = new Material(ghostMaterial);
                    else
                        mats[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mats[i].color = tint;
                }
                rend.materials = mats;
            }
            // Disable colliders on the ghost so it doesn't block its own raycast
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        private void ApplyCategoryTint(GameObject go, Color tint)
        {
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in rend.materials)
                    m.color = tint;
            }
        }

        /// <summary>
        /// Returns the per-product tint color from the static map.
        /// Falls back to a deterministic color if the product_id is unknown.
        /// </summary>
        private Color GetProductTint(ProductData product)
        {
            if (product == null) return Color.white;
            if (!string.IsNullOrEmpty(product.product_id) &&
                ProductTintMap.TryGetValue(product.product_id, out Color tint))
            {
                return tint;
            }
            // Fallback: derive a deterministic hue from the product name hash
            // so any future products still get a unique-ish color
            return DeriveColorFromName(product.name, product.category);
        }

        /// <summary>
        /// Applies a blended tint to real 3D models using MaterialPropertyBlock.
        /// This avoids creating material instances per object (GPU-efficient).
        /// The final color = lerp(originalBaseColor, tintColor, strength).
        /// </summary>
        private void ApplyProductTint(GameObject go, Color tintColor, float strength)
        {
            if (strength <= 0f) return;

            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();

                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    rend.GetPropertyBlock(mpb, i);

                    // Read existing base color from the material
                    Color original = Color.white;
                    if (rend.sharedMaterials[i] != null && rend.sharedMaterials[i].HasProperty(BaseColorProp))
                    {
                        original = rend.sharedMaterials[i].GetColor(BaseColorProp);
                    }

                    // Blend: keep the original texture appearance but shift the base color
                    Color blended = Color.Lerp(original, tintColor, strength);
                    blended.a = original.a; // preserve original alpha

                    mpb.SetColor(BaseColorProp, blended);
                    rend.SetPropertyBlock(mpb, i);
                }
            }

            Debug.Log($"[FurniturePlacer] Applied product tint ({tintColor}) at {strength:P0} strength to {go.name}");
        }

        /// <summary>
        /// Deterministic fallback color for products not in the static map.
        /// Uses a hash of the product name to pick a hue, with the category
        /// color as a saturation/brightness guide.
        /// </summary>
        private Color DeriveColorFromName(string name, string category)
        {
            if (string.IsNullOrEmpty(name)) return GetCategoryColor(category);

            // Stable hash to hue in [0,1]
            int hash = name.GetHashCode() & 0x7FFFFFFF;
            float hue = (hash % 360) / 360f;

            // Use category color's saturation/value as a baseline
            Color catColor = GetCategoryColor(category);
            Color.RGBToHSV(catColor, out _, out float s, out float v);

            // Clamp saturation/value to keep colors legible
            s = Mathf.Clamp(s, 0.3f, 0.7f);
            v = Mathf.Clamp(v, 0.5f, 0.85f);

            return Color.HSVToRGB(hue, s, v);
        }
    }
}
