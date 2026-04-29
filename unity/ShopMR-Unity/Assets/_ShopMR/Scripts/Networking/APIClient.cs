using System;
using System.Collections.Generic;
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
        [Tooltip("Default backend URL. Can be overridden at runtime via debug menu.")]
        [SerializeField] private string defaultBaseUrl = "http://10.181.182.134:8000";

        private const string PREF_KEY_BASE_URL = "ShopMR_BaseUrl";
        public string BaseUrl { get; private set; }

        public event Action<string> OnBaseUrlChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved URL or use default
            BaseUrl = PlayerPrefs.GetString(PREF_KEY_BASE_URL, defaultBaseUrl);
            Debug.Log($"[APIClient] Base URL: {BaseUrl}");
        }

        public void SetBaseUrl(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl)) return;
            newUrl = newUrl.TrimEnd('/');
            BaseUrl = newUrl;
            PlayerPrefs.SetString(PREF_KEY_BASE_URL, newUrl);
            PlayerPrefs.Save();
            Debug.Log($"[APIClient] Base URL changed to: {newUrl}");
            OnBaseUrlChanged?.Invoke(newUrl);
        }

        public void ResetBaseUrl()
        {
            SetBaseUrl(defaultBaseUrl);
        }

        // ---------- Generic HTTP ----------

        public async Task<string> GetRaw(string endpoint)
        {
            string url = BaseUrl + endpoint;
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 15;
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[APIClient] GET {url} failed: {req.error} | {req.downloadHandler.text}");
                    return null;
                }
                return req.downloadHandler.text;
            }
        }

        public async Task<string> PostRaw(string endpoint, string jsonBody)
        {
            string url = BaseUrl + endpoint;
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 15;

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[APIClient] POST {url} failed: {req.error} | {req.downloadHandler.text}");
                    return null;
                }
                return req.downloadHandler.text;
            }
        }

        // ---------- API Methods ----------

        public async Task<SessionStartResponse> StartSession(string deviceId = null)
        {
            var req = new SessionStartRequest { device_id = deviceId };
            string body = JsonUtility.ToJson(req);
            string resp = await PostRaw("/api/sessions/start", body);
            return resp != null ? JsonUtility.FromJson<SessionStartResponse>(resp) : null;
        }

        public async Task<SessionEndResponse> EndSession(string sessionId)
        {
            var req = new SessionEndRequest { session_id = sessionId };
            string body = JsonUtility.ToJson(req);
            string resp = await PostRaw("/api/sessions/end", body);
            return resp != null ? JsonUtility.FromJson<SessionEndResponse>(resp) : null;
        }

        public async Task<ProductListResponse> GetProducts(string category = null, int limit = 50)
        {
            string ep = $"/api/products/?limit={limit}";
            if (!string.IsNullOrEmpty(category)) ep += $"&category={UnityWebRequest.EscapeURL(category)}";
            string resp = await GetRaw(ep);
            return resp != null ? JsonUtility.FromJson<ProductListResponse>(resp) : null;
        }

        public async Task<ProductData> GetProduct(string productId)
        {
            string resp = await GetRaw($"/api/products/{productId}");
            return resp != null ? JsonUtility.FromJson<ProductData>(resp) : null;
        }

        public async Task TrackEvent(string sessionId, string eventType, string productId = null)
        {
            var ev = new EventSingleJson
            {
                session_id = sessionId,
                event_type = eventType,
                product_id = productId
            };
            string body = JsonUtility.ToJson(ev);
            await PostRaw("/api/events/single", body);
        }

        public async Task<RecommendationResponse> GetRecommendations(string sessionId, string currentProductId = null, int limit = 6)
        {
            var req = new RecommendationRequest
            {
                session_id = sessionId,
                current_product_id = currentProductId,
                limit = limit
            };
            string body = JsonUtility.ToJson(req);
            string resp = await PostRaw("/api/recommendations/get", body);
            return resp != null ? JsonUtility.FromJson<RecommendationResponse>(resp) : null;
        }

        public async Task<ChatResponse> SendChatMessage(string sessionId, string message, List<ChatMessageData> history = null)
        {
            var req = new ChatRequest
            {
                session_id = sessionId,
                message = message,
                conversation_history = history?.ToArray()
            };
            string body = JsonUtility.ToJson(req);
            string resp = await PostRaw("/api/chat/message", body);
            return resp != null ? JsonUtility.FromJson<ChatResponse>(resp) : null;
        }
    }
}