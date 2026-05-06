using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FixCategoryButtons
{
    public static string Execute()
    {
        int c = 0;

        // 1. Shrink CategoryButton prefab to fit 9 buttons in ~770px
        //    9 × 78 + 8 × 8 = 766px — fits comfortably
        string path = "Assets/_ShopMR/Prefabs/CategoryButton.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(78, 36);
            c++;
        }

        var col = root.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size = new Vector3(78, 36, 1);
            c++;
        }

        var tmp = root.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            c++;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        // 2. Update already-instantiated mock buttons in the scene
        var catRow = GameObject.Find("CatalogCanvas/CatalogPanel/CategoryRow");
        if (catRow != null)
        {
            foreach (Transform child in catRow.transform)
            {
                var childRT = child.GetComponent<RectTransform>();
                if (childRT != null) childRT.sizeDelta = new Vector2(78, 36);

                var childCol = child.GetComponent<BoxCollider>();
                if (childCol != null) childCol.size = new Vector3(78, 36, 1);

                var childTMP = child.GetComponentInChildren<TMP_Text>();
                if (childTMP != null)
                {
                    childTMP.fontSize = 15;
                    childTMP.fontStyle = FontStyles.Bold;
                }
            }

            // Tighten spacing
            var hlg = catRow.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = 6;
                c++;
            }
        }

        // 3. Also update the CategoryRow height to match
        if (catRow != null)
        {
            var rowRT = catRow.GetComponent<RectTransform>();
            if (rowRT != null)
            {
                rowRT.sizeDelta = new Vector2(-24, 42);
                c++;
            }
        }

        return $"Resized category buttons to 78x36. Applied {c} changes.";
    }
}
