using UnityEngine;

public class CleanupTintPreview
{
    public static string Execute()
    {
        var preview = GameObject.Find("_TintPreview");
        if (preview != null)
        {
            Object.DestroyImmediate(preview);
            return "Cleaned up _TintPreview object.";
        }
        return "No _TintPreview found to clean up.";
    }
}
