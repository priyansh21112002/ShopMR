using UnityEngine;
using ShopMR.Networking;

namespace ShopMR.Core
{
    /// <summary>
    /// Lightweight in-app debug menu for changing backend URL at runtime.
    /// PC: press F1 to toggle.
    /// Quest: press both Start (left menu) + B button to toggle (held for 1s).
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        [SerializeField] private bool startOpen = false;
        [SerializeField] private KeyCode pcToggleKey = KeyCode.F1;

        private bool isOpen;
        private string urlInput = "";
        private string statusMessage = "";
        private Vector2 scrollPos;

        // Quest button hold timing
        private float bothButtonsHeldTime = 0f;
        private const float HOLD_THRESHOLD = 1.0f;

        private void Start()
        {
            isOpen = startOpen;
            if (APIClient.Instance != null)
                urlInput = APIClient.Instance.BaseUrl;
        }

        private void Update()
        {
            // PC toggle
            if (Input.GetKeyDown(pcToggleKey))
                Toggle();

            // Quest toggle: hold left menu + B for 1 second
            // OVRInput would be ideal, but to avoid dependency we use Input.GetButton
            // with the OVR-mapped button names (works when Meta XR is in the project).
            #if !UNITY_EDITOR
            bool menuPressed = Input.GetKey(KeyCode.JoystickButton7) || Input.GetKey(KeyCode.Menu);
            bool bPressed = Input.GetKey(KeyCode.JoystickButton1);
            if (menuPressed && bPressed)
            {
                bothButtonsHeldTime += Time.deltaTime;
                if (bothButtonsHeldTime >= HOLD_THRESHOLD)
                {
                    Toggle();
                    bothButtonsHeldTime = 0f;
                }
            }
            else
            {
                bothButtonsHeldTime = 0f;
            }
            #endif
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (isOpen && APIClient.Instance != null)
                urlInput = APIClient.Instance.BaseUrl;
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(2f, 2f, 1f));

            GUILayout.BeginArea(new Rect(20, 20, 500, 520), GUI.skin.box);
            GUILayout.Label("=== ShopMR Debug Menu ===", GUI.skin.box);

            GUILayout.Space(8);
            GUILayout.Label("Backend Base URL:");
            urlInput = GUILayout.TextField(urlInput ?? "", GUILayout.Height(28));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Height(32)))
            {
                if (APIClient.Instance != null)
                {
                    APIClient.Instance.SetBaseUrl(urlInput);
                    statusMessage = $"URL set to {urlInput}";
                }
            }
            if (GUILayout.Button("Reset Default", GUILayout.Height(32)))
            {
                if (APIClient.Instance != null)
                {
                    APIClient.Instance.ResetBaseUrl();
                    urlInput = APIClient.Instance.BaseUrl;
                    statusMessage = "Reset to default URL";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Session Info:", GUI.skin.box);
            if (SessionManager.Instance != null)
            {
                GUILayout.Label($"Session ID: {SessionManager.Instance.SessionId ?? "(none)"}");
                GUILayout.Label($"Variant: {SessionManager.Instance.Variant ?? "(none)"}");
                GUILayout.Label($"Active: {SessionManager.Instance.IsSessionActive}");

                if (GUILayout.Button("Restart Session", GUILayout.Height(32)))
                {
                    _ = SessionManager.Instance.StartNewSession();
                    statusMessage = "Restarting session...";
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Catalog:", GUI.skin.box);
            var cat = ShopMR.Catalog.CatalogManager.Instance;
            if (cat != null)
            {
                GUILayout.Label($"Loaded: {cat.HasFetched} | Fetching: {cat.IsFetching}");
                GUILayout.Label($"Total products: {cat.AllProducts.Count}");
                GUILayout.Label($"Categories: {(cat.AllProducts.Count > 0 ? string.Join(", ", cat.Categories) : "(none)")}");

                if (GUILayout.Button("Fetch Catalog Now", GUILayout.Height(32)))
                {
                    _ = cat.FetchCatalog();
                    statusMessage = "Fetching catalog...";
                }
            }
            else
            {
                GUILayout.Label("CatalogManager not in scene.");
            }

            GUILayout.Space(8);
            if (!string.IsNullOrEmpty(statusMessage))
                GUILayout.Label($"Status: {statusMessage}");

            GUILayout.Space(4);
            GUILayout.Label("Close: F1 (PC) / hold Menu+B 1s (Quest)");
            GUILayout.EndArea();
        }
    }
}