using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using ShopMR.Networking;
using ShopMR.Core;
using ShopMR.Catalog;
using ShopMR.Placement;

namespace ShopMR.Chat
{
    /// <summary>
    /// Manages the entire chat UI — toggling visibility, sending messages,
    /// displaying history, and managing the input field.
    /// </summary>
    public class ChatPanelController : MonoBehaviour
    {
        public static ChatPanelController Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject chatMessageBubblePrefab;

        [Header("UI References")]
        [SerializeField] private Transform chatContent;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Settings")]
        [SerializeField] private float spawnDistance = 1.2f;
        [SerializeField] private int maxHistoryMessages = 20;

        [Header("Toggle Input")]
        [SerializeField] private KeyCode pcToggleKey = KeyCode.T;

        // CanvasGroup for hide/show (never SetActive(false))
        private CanvasGroup canvasGroup;
        private Transform headTransform;

        // Conversation history sent to backend
        private readonly List<ChatMessageData> history = new List<ChatMessageData>();

        // Debounce toggle
        private float lastToggleTime;
        private const float TOGGLE_COOLDOWN = 0.4f;

        // Sending state
        private bool isSending;

        // Grip button edge detection
        private bool wasGripDown;

        // VR keyboard helper (manages Quest system keyboard)
        private VRKeyboardHelper keyboardHelper;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Immediately hide in Awake to prevent any frame where it's visible
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            SetCollidersEnabled(false);
        }

        private void Start()
        {
            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;

            // Wire button events
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);

                // Setup VR keyboard helper — it fully manages the Quest system keyboard
                keyboardHelper = inputField.GetComponent<VRKeyboardHelper>();
                if (keyboardHelper == null)
                    keyboardHelper = inputField.gameObject.AddComponent<VRKeyboardHelper>();

                // When user presses Done on the system keyboard, send the message
                keyboardHelper.OnKeyboardDone += OnSendButtonClicked;
            }

            // Clear status
            if (statusText != null)
                statusText.text = "";

            // Start hidden
            Hide();
        }

        private void Update()
        {
            // PC toggle key
            if (Input.GetKeyDown(pcToggleKey))
                Toggle();

            // Quest: Right hand grip button to toggle chat
            // Grip is unoccupied (A=catalog, B=cancel/remove, trigger=select/place, thumbstick=rotate)
            // Do NOT toggle during active furniture placement to avoid accidental opens
#if !UNITY_EDITOR
            bool rightGrip = false;
            bool isPlacing = FurniturePlacer.Instance != null && FurniturePlacer.Instance.IsPlacing;
            if (!isPlacing)
            {
                try
                {
                    // Use axis threshold for grip (it's analog on Quest controllers)
                    float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
                    if (gripValue > 0.8f && !wasGripDown)
                        rightGrip = true;
                    wasGripDown = gripValue > 0.8f;
                }
                catch { }
                if (rightGrip && Time.time - lastToggleTime > TOGGLE_COOLDOWN)
                    Toggle();
            }
            else
            {
                // Reset grip state during placement so it doesn't fire on release
                wasGripDown = false;
            }
#endif

            // Editor: Enter key sends message when input field is focused
#if UNITY_EDITOR
            if (IsVisible && Input.GetKeyDown(KeyCode.Return) && inputField != null && inputField.isFocused)
                OnSendButtonClicked();
#endif
            // Note: Keyboard text sync is handled by VRKeyboardHelper component
        }

        // ─── Visibility ───────────────────────────────────

        public void Toggle()
        {
            lastToggleTime = Time.time;
            if (IsVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (canvasGroup == null) return;

            // Cancel placement if active
            if (FurniturePlacer.Instance != null && FurniturePlacer.Instance.IsPlacing)
                FurniturePlacer.Instance.CancelPlacement();

            // Close catalog if open
            if (CatalogPanelController.Instance != null && CatalogPanelController.Instance.IsVisible)
                CatalogPanelController.Instance.Hide();

            PositionInFrontOfHead();

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            SetCollidersEnabled(true);

            // Activate keyboard after a short delay so the canvas is fully rendered
            StartCoroutine(ActivateKeyboardDelayed());

            Debug.Log("[ChatPanel] Shown");
        }

        private IEnumerator ActivateKeyboardDelayed()
        {
            // Wait for the grip button to be released first so it doesn't
            // interfere with keyboard activation on Quest
#if !UNITY_EDITOR
            float waitStart = Time.time;
            const float maxGripWait = 1.5f; // safety timeout
            while (Time.time - waitStart < maxGripWait)
            {
                float gripValue = 0f;
                try
                {
                    gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
                }
                catch { }
                if (gripValue < 0.2f)
                    break;
                yield return null;
            }
#endif

            // Wait for canvas layout to fully rebuild and become interactable
            yield return new WaitForSeconds(0.3f);

            // Activate VR keyboard helper and select the input field
            if (keyboardHelper != null)
            {
                keyboardHelper.Activate();
                // Short delay before opening to ensure UI is ready
                yield return new WaitForSeconds(0.1f);
                // Select and activate the input field — this will trigger the
                // system keyboard natively since shouldHideSoftKeyboard = false
                if (inputField != null)
                {
                    inputField.Select();
                    inputField.ActivateInputField();
                }
                // Also manually open as fallback for ISDK World Space edge cases
                keyboardHelper.OpenKeyboard();
            }
            else if (inputField != null)
            {
                // Fallback: just select the input field
                inputField.Select();
                inputField.ActivateInputField();
            }

            Debug.Log("[ChatPanel] Keyboard activated");
        }

        public void Hide()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            SetCollidersEnabled(false);

            // Dismiss keyboard via helper
            if (keyboardHelper != null)
                keyboardHelper.Deactivate();
            else if (inputField != null)
                inputField.DeactivateInputField();

            Debug.Log("[ChatPanel] Hidden");
        }

        private void SetCollidersEnabled(bool enabled)
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
                col.enabled = enabled;
        }

        private void PositionInFrontOfHead()
        {
            if (headTransform == null) return;

            Vector3 fwd = headTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            // Position panel at eye level, slightly below gaze
            Vector3 pos = headTransform.position + fwd * spawnDistance;
            pos.y = headTransform.position.y - 0.05f; // slightly below eye level

            // Move the Canvas root (parent), not this panel
            Transform canvasRoot = transform.parent != null ? transform.parent : transform;
            canvasRoot.position = pos;
            canvasRoot.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ─── Sending Messages ─────────────────────────────

        private void OnInputSubmit(string text)
        {
            SendMessage();
        }

        private void OnSendButtonClicked()
        {
            SendMessage();
        }

        /// <summary>
        /// Builds a system context message with accurate catalog information
        /// so the LLM has grounded facts about the product catalog.
        /// </summary>
        private string BuildCatalogContext()
        {
            if (CatalogManager.Instance == null || !CatalogManager.Instance.HasFetched)
                return null;

            var products = CatalogManager.Instance.AllProducts;
            var categories = CatalogManager.Instance.Categories;

            var sb = new StringBuilder();
            sb.AppendLine($"[SYSTEM CONTEXT - Accurate catalog data]");
            sb.AppendLine($"Total products in catalog: {products.Count}");
            sb.AppendLine($"Categories ({categories.Count}): {string.Join(", ", categories)}");
            sb.AppendLine("Product list:");
            foreach (var p in products)
            {
                sb.AppendLine($"- {p.name} ({p.category}) - ${p.price:F2} [ID: {p.product_id}]");
            }

            // Add context about placed furniture
            if (FurniturePlacer.Instance != null && FurniturePlacer.Instance.PlacedCount > 0)
            {
                sb.AppendLine($"\nCurrently placed in user's room ({FurniturePlacer.Instance.PlacedCount} items):");
                foreach (var item in FurniturePlacer.Instance.PlacedItems)
                {
                    if (item != null)
                        sb.AppendLine($"- {item.productName} (ID: {item.productId})");
                }
            }

            sb.AppendLine("\nIMPORTANT: Always use these exact numbers and product names. Do NOT guess or make up product counts or names.");
            sb.AppendLine("When the user wants to place, keep, try, or add a product to their room, confirm you are placing it and include the product ID in your response. The system will automatically trigger the placement mode.");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the conversation history with catalog context injected as a system message.
        /// </summary>
        private List<ChatMessageData> BuildHistoryWithContext()
        {
            var contextHistory = new List<ChatMessageData>();

            // Inject catalog context as the first system message
            string context = BuildCatalogContext();
            if (!string.IsNullOrEmpty(context))
            {
                contextHistory.Add(new ChatMessageData { role = "system", content = context });
            }

            // Add conversation history
            contextHistory.AddRange(history);
            return contextHistory;
        }

        private async void SendMessage()
        {
            if (isSending) return;
            if (inputField == null) return;

            string text = inputField.text;
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Add user message bubble
            AddMessageBubble(text, true);

            // 2. Add to history
            history.Add(new ChatMessageData { role = "user", content = text });
            TrimHistory();

            // 3. Clear input field
            inputField.text = "";

            // 4. Disable send + show status
            isSending = true;
            if (sendButton != null) sendButton.interactable = false;
            if (statusText != null)
            {
                statusText.text = "Thinking...";
                statusText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            // 5. Call API with catalog context injected
            try
            {
                string sessionId = SessionManager.Instance != null
                    ? SessionManager.Instance.SessionId
                    : "unknown";

                // Build history with catalog context so LLM has accurate product data
                var contextualHistory = BuildHistoryWithContext();

                ChatResponse response = await APIClient.Instance.SendChatMessage(
                    sessionId, text, contextualHistory);

                if (response != null && !string.IsNullOrEmpty(response.response))
                {
                    // 6. Add assistant bubble
                    AddMessageBubble(response.response, false);

                    // 7. Add to history
                    history.Add(new ChatMessageData { role = "assistant", content = response.response });
                    TrimHistory();

                    // 8. Track event
                    if (EventTracker.Instance != null)
                        EventTracker.Instance.TrackChatMessage(null);

                    // 9. Handle placement actions — if LLM mentions products with placement intent
                    TryHandlePlacementAction(response);
                }
                else
                {
                    // API returned null/empty
                    ShowError("No response — try again");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatPanel] SendMessage failed: {ex.Message}");
                ShowError("Error — try again");
            }
            finally
            {
                // 9. Re-enable send, clear status
                isSending = false;
                if (sendButton != null) sendButton.interactable = true;
                if (statusText != null && statusText.text == "Thinking...")
                    statusText.text = "";
            }
        }

        // ─── Placement Action Handling ────────────────────

        // Keywords that indicate the LLM is suggesting placement/trying a product
        private static readonly string[] PlacementIntentKeywords = new string[]
        {
            "place", "placing", "placed",
            "keep", "keeping",
            "add to room", "add it to", "adding to",
            "try it", "try this", "try placing",
            "put it", "putting",
            "let me place", "i'll place", "i will place",
            "here it is", "there you go",
            "set it up", "setting up"
        };

        /// <summary>
        /// Checks if the LLM response indicates a placement action and triggers it.
        /// Uses products_mentioned from the response and intent detection from text.
        /// </summary>
        private void TryHandlePlacementAction(ChatResponse response)
        {
            if (response == null) return;
            if (FurniturePlacer.Instance == null) return;
            if (CatalogManager.Instance == null || !CatalogManager.Instance.HasFetched) return;

            // Don't trigger if already placing
            if (FurniturePlacer.Instance.IsPlacing) return;

            // Check if the response mentions products
            string[] mentionedProducts = response.products_mentioned;
            if (mentionedProducts == null || mentionedProducts.Length == 0) return;

            // Check if the response text contains placement intent
            string responseLower = response.response.ToLower();
            bool hasPlacementIntent = false;

            foreach (var keyword in PlacementIntentKeywords)
            {
                if (responseLower.Contains(keyword))
                {
                    hasPlacementIntent = true;
                    break;
                }
            }

            // Also check if the user's last message had placement intent
            if (!hasPlacementIntent && history.Count >= 2)
            {
                string lastUserMsg = history[history.Count - 2].content.ToLower();
                string[] userIntentKeywords = { "place", "keep", "try", "add", "put", "show me in room", "want it", "i'll take", "get it" };
                foreach (var keyword in userIntentKeywords)
                {
                    if (lastUserMsg.Contains(keyword))
                    {
                        hasPlacementIntent = true;
                        break;
                    }
                }
            }

            if (!hasPlacementIntent) return;

            // Try to find and place the first mentioned product
            ProductData productToPlace = null;
            foreach (var productId in mentionedProducts)
            {
                if (string.IsNullOrEmpty(productId)) continue;

                // Try direct ID lookup
                productToPlace = CatalogManager.Instance.GetById(productId);
                if (productToPlace != null) break;

                // Try matching by name (in case products_mentioned contains names instead of IDs)
                foreach (var p in CatalogManager.Instance.AllProducts)
                {
                    if (p.name.ToLower().Contains(productId.ToLower()) ||
                        productId.ToLower().Contains(p.name.ToLower()))
                    {
                        productToPlace = p;
                        break;
                    }
                }
                if (productToPlace != null) break;
            }

            if (productToPlace != null)
            {
                Debug.Log($"[ChatPanel] Triggering placement from chat: {productToPlace.name} (ID: {productToPlace.product_id})");

                // Add a system bubble to inform the user
                AddMessageBubble($"📦 Placing \"{productToPlace.name}\" — point your controller at the floor to position it. " +
                    "Press trigger to confirm, B to cancel.", false);

                // Small delay then trigger placement (gives user time to read the message)
                StartCoroutine(DelayedPlacement(productToPlace));
            }
        }

        private IEnumerator DelayedPlacement(ProductData product)
        {
            yield return new WaitForSeconds(0.8f);

            // Hide chat panel to give user clear view for placement
            Hide();

            // Trigger placement
            if (FurniturePlacer.Instance != null && !FurniturePlacer.Instance.IsPlacing)
            {
                FurniturePlacer.Instance.BeginPlacement(product);
            }
        }

        private void ShowError(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            }
        }

        // ─── Message Bubbles ──────────────────────────────

        private void AddMessageBubble(string text, bool isUser)
        {
            if (chatMessageBubblePrefab == null || chatContent == null) return;

            GameObject bubble = Instantiate(chatMessageBubblePrefab, chatContent);
            var ui = bubble.GetComponent<ChatMessageUI>();
            if (ui != null)
                ui.SetMessage(text, isUser);

            // Scroll to bottom on next frame
            ScrollToBottom();
        }

        private async void ScrollToBottom()
        {
            // Wait a frame for layout to rebuild
            await Task.Yield();
            if (scrollRect != null)
                scrollRect.normalizedPosition = new Vector2(0, 0);
        }

        private void TrimHistory()
        {
            while (history.Count > maxHistoryMessages)
                history.RemoveAt(0);
        }
    }
}
