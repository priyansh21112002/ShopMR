using UnityEngine;
using ShopMR.Networking;

namespace ShopMR.Core
{
    public class EventTracker : MonoBehaviour
    {
        public static EventTracker Instance { get; private set; }

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

        public async void TrackView(string productId)
        {
            if (!SessionManager.Instance.IsSessionActive) return;
            await APIClient.Instance.TrackEvent(
                SessionManager.Instance.SessionId, "view", productId);
            Debug.Log($"[Event] view: {productId}");
        }

        public async void TrackPlace(string productId)
        {
            if (!SessionManager.Instance.IsSessionActive) return;
            await APIClient.Instance.TrackEvent(
                SessionManager.Instance.SessionId, "place", productId);
            Debug.Log($"[Event] place: {productId}");
        }

        public async void TrackPurchase(string productId)
        {
            if (!SessionManager.Instance.IsSessionActive) return;
            await APIClient.Instance.TrackEvent(
                SessionManager.Instance.SessionId, "purchase", productId);
            Debug.Log($"[Event] purchase: {productId}");
        }

        public async void TrackRecommendClick(string productId)
        {
            if (!SessionManager.Instance.IsSessionActive) return;
            await APIClient.Instance.TrackEvent(
                SessionManager.Instance.SessionId, "recommend_click", productId);
            Debug.Log($"[Event] recommend_click: {productId}");
        }

        public async void TrackChatMessage(string productId)
        {
            if (!SessionManager.Instance.IsSessionActive) return;
            await APIClient.Instance.TrackEvent(
                SessionManager.Instance.SessionId, "chat_message", productId);
            Debug.Log($"[Event] chat_message: {productId}");
        }
    }
}
