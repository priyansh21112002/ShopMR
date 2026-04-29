using System;

namespace ShopMR.Networking
{
    // ─── Session ─────────────────────────────────────────
    [Serializable]
    public class SessionStartRequest
    {
        public string device_id;
    }

    [Serializable]
    public class SessionStartResponse
    {
        public string session_id;
        public string user_id;
        public string variant;
        public string message;
    }

    [Serializable]
    public class SessionEndRequest
    {
        public string session_id;
    }

    [Serializable]
    public class SessionEndResponse
    {
        public string session_id;
        public float duration_seconds;
        public string message;
    }

    // ─── Events ──────────────────────────────────────────
    [Serializable]
    public class EventSingleJson
    {
        public string session_id;
        public string event_type;
        public string product_id;
    }

    // ─── Products ────────────────────────────────────────
    [Serializable]
    public class ProductData
    {
        public string product_id;
        public string name;
        public string category;
        public float price;
        public string description;
        public string model_url;
        public string thumbnail_url;
        public ProductDimensions dimensions;
        public string[] tags;
        public bool is_active;
    }

    [Serializable]
    public class ProductDimensions
    {
        public float width;
        public float height;
        public float depth;
    }

    [Serializable]
    public class ProductListResponse
    {
        public ProductData[] products;
        public int total;
    }

    // ─── Recommendations ─────────────────────────────────
    [Serializable]
    public class RecommendationRequest
    {
        public string session_id;
        public string current_product_id;
        public int limit;
    }

    [Serializable]
    public class RecommendedProduct
    {
        public string product_id;
        public string name;
        public string category;
        public float price;
        public string thumbnail_url;
        public float score;
        public string reason;
    }

    [Serializable]
    public class RecommendationResponse
    {
        public string session_id;
        public string variant;
        public RecommendedProduct[] recommendations;
        public string model_version;
    }

    // ─── Chat ────────────────────────────────────────────
    [Serializable]
    public class ChatMessageData
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatRequest
    {
        public string session_id;
        public string message;
        public ChatMessageData[] conversation_history;
    }

    [Serializable]
    public class ChatResponse
    {
        public string session_id;
        public string response;
        public string[] products_mentioned;
        public string tool_used;
    }
}
