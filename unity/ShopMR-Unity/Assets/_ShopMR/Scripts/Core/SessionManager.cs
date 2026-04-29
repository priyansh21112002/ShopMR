using System.Threading.Tasks;
using UnityEngine;
using ShopMR.Networking;

namespace ShopMR.Core
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public string SessionId { get; private set; }
        public string UserId { get; private set; }
        public string Variant { get; private set; }
        public bool IsSessionActive { get; private set; }

        [Header("Status")]
        [SerializeField] private string statusDisplay = "Not connected";

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
            // Wait a frame for APIClient to initialize
            await Task.Yield();
            await StartNewSession();
        }

        public async Task StartNewSession()
        {
            Debug.Log("[Session] Starting new session...");
            statusDisplay = "Connecting...";

            var response = await APIClient.Instance.StartSession();

            if (response != null && !string.IsNullOrEmpty(response.session_id))
            {
                SessionId = response.session_id;
                UserId = response.user_id;
                Variant = response.variant;
                IsSessionActive = true;

                statusDisplay = $"Connected | {Variant}";
                Debug.Log($"[Session] Active | ID: {SessionId} | User: {UserId} | Variant: {Variant}");
            }
            else
            {
                statusDisplay = "Connection failed!";
                Debug.LogError("[Session] Failed to start session");
            }
        }

        public async void EndCurrentSession()
        {
            if (!IsSessionActive || string.IsNullOrEmpty(SessionId)) return;

            Debug.Log($"[Session] Ending session {SessionId}...");

            var response = await APIClient.Instance.EndSession(SessionId);

            if (response != null)
            {
                Debug.Log($"[Session] Ended | Duration: {response.duration_seconds}s");
            }

            IsSessionActive = false;
            statusDisplay = "Disconnected";
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) EndCurrentSession();
        }

        private void OnApplicationQuit()
        {
            EndCurrentSession();
        }
    }
}
