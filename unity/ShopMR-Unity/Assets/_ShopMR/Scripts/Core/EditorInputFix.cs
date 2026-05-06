using UnityEngine;
using UnityEngine.EventSystems;

namespace ShopMR.Core
{
    /// <summary>
    /// Configures the correct input module per platform:
    /// - Editor: StandaloneInputModule (mouse clicks)
    /// - Quest:  OVRInputModule (controller ray + trigger)
    /// Attach to the EventSystem GameObject.
    /// </summary>
    public class EditorInputFix : MonoBehaviour
    {
        void Awake()
        {
            var modules = GetComponents<BaseInputModule>();

#if UNITY_EDITOR
            // Editor: use StandaloneInputModule for mouse, disable everything else
            bool hasStandalone = false;
            foreach (var m in modules)
            {
                string typeName = m.GetType().Name;
                if (typeName == "StandaloneInputModule")
                {
                    m.enabled = true;
                    hasStandalone = true;
                    Debug.Log($"[EditorInputFix] Enabled {typeName}");
                }
                else
                {
                    m.enabled = false;
                    Debug.Log($"[EditorInputFix] Disabled {typeName} (editor mode)");
                }
            }
            if (!hasStandalone)
            {
                gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[EditorInputFix] Added StandaloneInputModule");
            }
#else
            // Quest: use OVRInputModule for controller ray + trigger, disable others
            bool hasOVR = false;
            foreach (var m in modules)
            {
                string typeName = m.GetType().Name;
                if (typeName == "OVRInputModule")
                {
                    m.enabled = true;
                    hasOVR = true;
                    Debug.Log($"[EditorInputFix] Enabled {typeName} (Quest mode)");
                }
                else
                {
                    m.enabled = false;
                    Debug.Log($"[EditorInputFix] Disabled {typeName} (Quest mode)");
                }
            }
            if (!hasOVR)
            {
                Debug.LogWarning("[EditorInputFix] OVRInputModule not found on EventSystem — UI interaction will not work");
            }
#endif
        }
    }
}
