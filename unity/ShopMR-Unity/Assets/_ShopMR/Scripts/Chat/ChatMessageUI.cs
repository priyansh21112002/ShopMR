using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopMR.Chat
{
    /// <summary>
    /// Controls a single message bubble in the chat history.
    /// Adjusts alignment and background color based on sender role.
    /// </summary>
    public class ChatMessageUI : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text messageText;
        public Image bubbleBackground;

        // Colors for user vs assistant bubbles
        private static readonly Color UserBubbleColor = new Color(0.15f, 0.30f, 0.55f, 1f);      // darker blue
        private static readonly Color AssistantBubbleColor = new Color(0.22f, 0.22f, 0.25f, 1f);  // darker grey

        /// <summary>
        /// Configure this bubble with message content and sender role.
        /// </summary>
        public void SetMessage(string text, bool isUser)
        {
            if (messageText != null)
            {
                messageText.text = text;
                messageText.alignment = isUser
                    ? TextAlignmentOptions.TopRight
                    : TextAlignmentOptions.TopLeft;
            }

            if (bubbleBackground != null)
            {
                bubbleBackground.color = isUser ? UserBubbleColor : AssistantBubbleColor;
            }
        }
    }
}
