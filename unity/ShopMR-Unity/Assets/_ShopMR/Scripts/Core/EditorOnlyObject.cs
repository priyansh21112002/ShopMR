using UnityEngine;

namespace ShopMR.Core
{
    /// <summary>
    /// Disables this GameObject on non-Editor platforms (Quest builds).
    /// Attach to objects like _EditorTestFloor that are only useful in the Editor.
    /// </summary>
    public class EditorOnlyObject : MonoBehaviour
    {
        private void Awake()
        {
#if !UNITY_EDITOR
            gameObject.SetActive(false);
            Debug.Log($"[EditorOnlyObject] Disabled '{gameObject.name}' (not in Editor)");
#endif
        }
    }
}
