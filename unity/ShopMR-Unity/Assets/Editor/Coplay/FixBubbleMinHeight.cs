using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixBubbleMinHeight
{
    [MenuItem("ShopMR/Fix Bubble Min Height")]
    public static string Execute()
    {
        // Load the ChatMessageBubble prefab
        string prefabPath = "Assets/_ShopMR/Prefabs/ChatMessageBubble.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return "ERROR: ChatMessageBubble prefab not found";

        // Open prefab for editing
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Fix the LayoutElement on the root - increase min height for better readability
        var layoutElement = prefabRoot.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.minHeight = 50f; // increased from 40 to 50
        }

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        return "SUCCESS: ChatMessageBubble minHeight increased to 50";
    }
}
