using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class ImportTMPEssentials
{
    public static string Execute()
    {
        // In Unity 6, TMP essentials can be imported via TMP_PackageResourceImporter
        // But the simplest approach is to call the internal import method
        
        // Check if already imported
        var settings = Resources.Load("TMP Settings");
        if (settings != null)
        {
            return "TMP Essentials already imported.";
        }

        // Try to find and invoke the import method
        try
        {
            // Unity 6 approach: use TMPro.EditorUtilities
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type importerType = null;
            
            foreach (var asm in assemblies)
            {
                try
                {
                    importerType = asm.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporter");
                    if (importerType != null) break;
                    
                    importerType = asm.GetType("TMPro.TMP_PackageResourceImporter");
                    if (importerType != null) break;
                }
                catch { }
            }

            if (importerType != null)
            {
                var importMethod = importerType.GetMethod("ImportResources", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (importMethod != null)
                {
                    importMethod.Invoke(null, null);
                    AssetDatabase.Refresh();
                    return "TMP Essentials imported via TMP_PackageResourceImporter.";
                }
            }
            
            // Fallback: use the Window menu approach
            // In Unity 6, TMP resources are in the package - copy them manually
            string packagePath = "Packages/com.unity.ugui";
            string[] guids = AssetDatabase.FindAssets("LiberationSans SDF", new[] { packagePath });
            
            if (guids.Length == 0)
            {
                // Try broader search
                guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            }
            
            string fontInfo = $"Found {guids.Length} font asset GUIDs. ";
            foreach (var guid in guids)
            {
                fontInfo += AssetDatabase.GUIDToAssetPath(guid) + "; ";
            }
            
            return $"Could not auto-import TMP Essentials. Font search results: {fontInfo}" +
                   "Please manually import via: Window > TextMeshPro > Import TMP Essential Resources";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}\n{ex.StackTrace}";
        }
    }
}
