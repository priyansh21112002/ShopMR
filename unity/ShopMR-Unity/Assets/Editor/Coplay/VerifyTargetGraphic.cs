using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class VerifyTargetGraphic
{
    public static string Execute()
    {
        var result = "";

        // Check ProductCard
        var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/ProductCard.prefab");
        if (card != null)
        {
            var btn = card.GetComponent<Button>();
            var img = card.GetComponent<Image>();
            result += $"ProductCard: Button.targetGraphic = {(btn.targetGraphic != null ? btn.targetGraphic.name + " (" + btn.targetGraphic.GetType().Name + ")" : "NULL")}";
            result += $" | Image exists: {img != null}";
            result += $" | Same object: {(btn.targetGraphic == img)}";
            result += $" | Image.raycastTarget: {(img != null ? img.raycastTarget.ToString() : "N/A")}\n";
        }

        // Check CategoryButton
        var catBtn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ShopMR/Prefabs/CategoryButton.prefab");
        if (catBtn != null)
        {
            var btn = catBtn.GetComponent<Button>();
            var img = catBtn.GetComponent<Image>();
            result += $"CategoryButton: Button.targetGraphic = {(btn.targetGraphic != null ? btn.targetGraphic.name + " (" + btn.targetGraphic.GetType().Name + ")" : "NULL")}";
            result += $" | Image exists: {img != null}";
            result += $" | Same object: {(btn.targetGraphic == img)}";
            result += $" | Image.raycastTarget: {(img != null ? img.raycastTarget.ToString() : "N/A")}";
        }

        return result;
    }
}
