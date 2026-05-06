using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixTargetGraphic
{
    public static string Execute()
    {
        int fixes = 0;
        fixes += FixPrefab("Assets/_ShopMR/Prefabs/ProductCard.prefab");
        fixes += FixPrefab("Assets/_ShopMR/Prefabs/CategoryButton.prefab");
        AssetDatabase.SaveAssets();
        return $"Fixed targetGraphic on {fixes} prefab(s) via SerializedObject.";
    }

    static int FixPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return 0;

        var btn = prefab.GetComponent<Button>();
        var img = prefab.GetComponent<Image>();

        if (btn == null || img == null)
        {
            Debug.LogWarning($"[FixTargetGraphic] {path}: Button or Image not found on root");
            return 0;
        }

        // Use SerializedObject to properly set the reference
        var so = new SerializedObject(btn);
        var targetProp = so.FindProperty("m_TargetGraphic");
        if (targetProp != null)
        {
            targetProp.objectReferenceValue = img;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            Debug.Log($"[FixTargetGraphic] {path}: Set Button.targetGraphic → Image ({img.GetType().Name})");
            return 1;
        }

        Debug.LogWarning($"[FixTargetGraphic] {path}: m_TargetGraphic property not found");
        return 0;
    }
}
