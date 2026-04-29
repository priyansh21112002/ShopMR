using UnityEngine;
using UnityEditor;

public class CreateFolderStructure
{
    public static string Execute()
    {
        string[] folders = new string[]
        {
            "Assets/_ShopMR",
            "Assets/_ShopMR/Scenes",
            "Assets/_ShopMR/Scripts",
            "Assets/_ShopMR/Scripts/Networking",
            "Assets/_ShopMR/Scripts/Catalog",
            "Assets/_ShopMR/Scripts/Placement",
            "Assets/_ShopMR/Scripts/Chat",
            "Assets/_ShopMR/Scripts/Core",
            "Assets/_ShopMR/Prefabs",
            "Assets/_ShopMR/Materials",
            "Assets/_ShopMR/UI"
        };

        int created = 0;
        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                int lastSlash = folder.LastIndexOf('/');
                string parent = folder.Substring(0, lastSlash);
                string name = folder.Substring(lastSlash + 1);
                AssetDatabase.CreateFolder(parent, name);
                created++;
            }
        }

        AssetDatabase.Refresh();
        return $"Created {created} folders. Folder structure is ready.";
    }
}
