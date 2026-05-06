using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AddMRFloorCollider
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // Remove old _EditorTestFloor if it exists (it had EditorOnlyObject which disabled on Quest)
        var oldFloor = GameObject.Find("_EditorTestFloor");
        if (oldFloor != null)
        {
            Object.DestroyImmediate(oldFloor);
            results.AppendLine("Removed old _EditorTestFloor");
        }

        // Create an invisible floor plane for physics raycasting in MR
        // This acts as the placement surface when Quest scene mesh is not available
        var existing = GameObject.Find("_MRFloorCollider");
        if (existing != null)
        {
            results.AppendLine("_MRFloorCollider already exists");
        }
        else
        {
            var floor = new GameObject("_MRFloorCollider");
            floor.transform.position = Vector3.zero;
            floor.transform.rotation = Quaternion.identity;

            // Large box collider as invisible floor (no renderer = invisible)
            var box = floor.AddComponent<BoxCollider>();
            box.size = new Vector3(50f, 0.01f, 50f); // 50m x 50m floor
            box.center = new Vector3(0f, -0.005f, 0f); // top surface at y=0

            // Mark as static for performance
            floor.isStatic = true;

            EditorUtility.SetDirty(floor);
            results.AppendLine("Created _MRFloorCollider: 50m x 50m invisible floor at y=0 for placement raycasting");
        }

        // Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
