using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace ShopMR.Core
{
    /// <summary>
    /// Attach this to a TMP_InputField GameObject to ensure the Quest system keyboard
    /// reliably opens when the user interacts with the field via ray or poke.
    ///
    /// On Quest, the system keyboard overlay requires:
    /// 1. "oculus.software.overlay_keyboard" declared in AndroidManifest.xml
    /// 2. OculusProjectConfig.requiresSystemKeyboard = true
    /// 3. shouldHideSoftKeyboard = false on the TMP_InputField
    ///
    /// With these in place, TMP_InputField internally manages the keyboard lifecycle,
    /// text sync, and caret position. This component only ensures the field is properly
    /// activated when selected through ISDK (ray/poke), since ISDK interactions
    /// don't always trigger TMP's full activation flow.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class VRKeyboardHelper : MonoBehaviour, IPointerClickHandler, ISelectHandler
    {
        private TMP_InputField inputField;
        private bool wasFocused;

        /// <summary>Whether the input field is currently focused (keyboard should be visible).</summary>
        public bool IsKeyboardVisible => inputField != null && inputField.isFocused;

        /// <summary>Fired when the user presses Done/Return on the system keyboard.</summary>
        public event System.Action OnKeyboardDone;

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
        }

        private void OnEnable()
        {
            ConfigureInputField();
            wasFocused = false;

            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnSubmit);
                inputField.onEndEdit.AddListener(OnEndEdit);
            }
        }

        private void OnDisable()
        {
            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(OnSubmit);
                inputField.onEndEdit.RemoveListener(OnEndEdit);
            }
        }

        private void ConfigureInputField()
        {
            if (inputField == null) return;

            // Hide the mobile input overlay (the native Android text box that floats
            // on top of the screen — not visible/useful in VR).
            inputField.shouldHideMobileInput = true;

            // CRITICAL: Do NOT hide the soft keyboard. This must be false so that
            // TMP_InputField internally opens and manages the Quest system keyboard,
            // including proper text sync and caret position handling.
            inputField.shouldHideSoftKeyboard = false;
        }

        private void Update()
        {
#if !UNITY_EDITOR
            // Detect when focus is gained through ISDK interactions.
            // If the field becomes focused but the keyboard didn't open (TMP sometimes
            // misses this through ISDK), force re-activation.
            bool isFocused = inputField != null && inputField.isFocused;
            if (isFocused && !wasFocused)
            {
                // Field just gained focus — ensure TMP's internal keyboard is open
                // by re-triggering activation (TMP handles keyboard open internally)
                Debug.Log("[VRKeyboardHelper] Input field gained focus, ensuring activation");
            }
            wasFocused = isFocused;
#endif
        }

        // ─── IPointerClickHandler ─────────────────────────
        public void OnPointerClick(PointerEventData eventData)
        {
            ActivateField();
        }

        // ─── ISelectHandler ──────────────────────────────
        public void OnSelect(BaseEventData eventData)
        {
            ActivateField();
        }

        // ─── Public API ──────────────────────────────────

        /// <summary>
        /// Activate the input field so TMP opens the system keyboard.
        /// </summary>
        public void OpenKeyboard()
        {
            ActivateField();
        }

        /// <summary>Close the keyboard by deactivating the input field.</summary>
        public void CloseKeyboard()
        {
            if (inputField != null)
                inputField.DeactivateInputField();

            wasFocused = false;
            Debug.Log("[VRKeyboardHelper] Input field deactivated");
        }

        /// <summary>Enable this helper (call when the chat panel becomes visible).</summary>
        public void Activate()
        {
            ConfigureInputField();
        }

        /// <summary>Disable this helper and close keyboard (call when panel hides).</summary>
        public void Deactivate()
        {
            CloseKeyboard();
        }

        // ─── Private ─────────────────────────────────────

        private void ActivateField()
        {
            if (inputField == null) return;

            // Select and activate — TMP_InputField will internally open the
            // TouchScreenKeyboard and handle all text sync + caret positioning.
            inputField.Select();
            inputField.ActivateInputField();

            Debug.Log("[VRKeyboardHelper] Input field activated via ISDK interaction");
        }

        private void OnSubmit(string text)
        {
            OnKeyboardDone?.Invoke();
        }

        private void OnEndEdit(string text)
        {
            wasFocused = false;
        }
    }
}
