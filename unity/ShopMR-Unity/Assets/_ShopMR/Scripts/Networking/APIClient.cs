using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ShopMR.Networking
{
    public class APIClient : MonoBehaviour
    {
        public static APIClient Instance { get; private set; }

        [Header("Backend Configuration")]
        [Tooltip("Your Mac's local IP + port, e.g. http://192.168.1.42:8000")]
        [SerializeField] private string baseUrl = "http://REPLACE_WITH_YOUR_IP:8000";

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

        // ─── Generic HTTP ────────────────────────────────

        private async Task<string> GetRaw(string endpoint)
        {
            string url = $"{baseUrl}{endpoint}";
            Debug.Log($"[API] GET {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[API] GET failed: {request.error}");
                    return null;
                }

                return request.downloadHandler.text;
            }
        }

        private async Task<string> PostRaw(string endpoint, string jsonBody)
        {
            string url = $"{baseUrl}{endpoint}";
            Debug.Log($"[API] POST {url}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[API] POST failed: {request.error} | {request.downloadHandler.text}");
                    return null;
                }

                return request.downloadHandler.text;
            }
        }

        // ─── Session ─────────────────────────────────────

        public async Task<SessionStartResponse> StartSession(string deviceId = null)
        {
            var req = new SessionStartRequest
            {
                device_id = deviceId ?? SystemInfo.deviceUniqueIdentifier
            };
            string json = await PostRaw("/api/sessions/start", JsonUtility.ToJson(req));
            if (json == null) return null;
            return JsonUtility.FromJson<SessionStartResponse>(json);
        }

        public async Task<SessionEndResponse> EndSession(string sessionId)
        {
            var req = new SessionEndRequest { session_id = sessionId };
            string json = await PostRaw("/api/sessions/end", JsonUtility.ToJson(req));
            if (json == null) return null;
            return JsonUtility.FromJson<SessionEndResponse>(json);
        }

        // ─── Products ────────────────────────────────────

        public async Task<ProductListResponse> GetProducts(string category = null, int limit = 20)
        {
            string endpoint = $"/api/products/?limit={limit}";
            if (!string.IsNullOrEmpty(category))
                endpoint += $"&category={category}";
            string json = await GetRaw(endpoint);
            if (json == null) return null;
            return JsonUtility.FromJson<ProductListResponse>(json);
        }

        public async Task<ProductData> GetProduct(string productId)
        {
            string json = await GetRaw($"/api/products/{productId}");
            if (json == null) return null;
            return JsonUtility.FromJson<ProductData>(json);
        }

        // ─── Events ──────────────────────────────────────

        public async Task TrackEvent(string sessionId, string eventType, string productId = null)
        {
            var evt = new EventSingleJson
            {
                session_id = sessionId,
                event_type = eventType,
                product_id = productId
            };
            await PostRaw("/api/events/single", JsonUtility.ToJson(evt));
        }

        // ─── Recommendations ─────────────────────────────

        public async Task<RecommendationResponse> GetRecommendations(
            string sessionId, string currentProductId = null, int limit = 6)
        {
            var req = new RecommendationRequest
            {
                session_id = sessionId,
                current_product_id = currentProductId,
                limit = limit
            };
            string json = await PostRaw("/api/recommendations/get", JsonUtility.ToJson(req));
            if (json == null) return null;
            return JsonUtility.FromJson<RecommendationResponse>(json);
        }

        // ─── Chat ────────────────────────────────────────

        public async Task<ChatResponse> SendChatMessage(
            string sessionId, string message, ChatMessageData[] history = null)
        {
            var req = new ChatRequest
            {
                session_id = sessionId,
                message = message,
                conversation_history = history
            };
            string json = await PostRaw("/api/chat/message", JsonUtility.ToJson(req));
            if (json == null) return null;
            return JsonUtility.FromJson<ChatResponse>(json);
        }
    }
}
