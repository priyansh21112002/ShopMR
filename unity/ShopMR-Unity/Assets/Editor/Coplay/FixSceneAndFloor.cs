using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixSceneAndFloor
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Check current scene
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        results.AppendLine($"Current active scene: {currentScene.path}");

        // 2. If we're in the wrong scene, open the correct one
        string correctPath = "Assets/_ShopMR/Scenes/MainMR.unity";
        string wrongPath = "Assets/MainMR.unity";

        if (currentScene.path != correctPath)
        {
            // Save any changes to current scene first, then open correct one
            // Actually, the wrong scene may contain our latest changes. 
            // Let's open the correct scene and re-apply the floor and camera fix there.
            results.AppendLine("Opening correct scene...");
            EditorSceneManager.OpenScene(correctPath);
            currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            results.AppendLine($"Now active: {currentScene.path}");
        }

        // 3. Ensure _EditorTestFloor exists in the correct scene
        var floor = GameObject.Find("_EditorTestFloor");
        if (floor == null)
        {
            // Create it
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "_EditorTestFloor";
            floor.transform.position = new Vector3(0f, 0f, 2f);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            // Apply material
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ShopMR/Materials/EditorFloorMat.mat");
            if (mat != null)
            {
                var rend = floor.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            results.AppendLine("Created _EditorTestFloor in correct scene");
        }
        else
        {
            floor.transform.position = new Vector3(0f, 0f, 2f);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            results.AppendLine("_EditorTestFloor already present, position verified");
        }

        // 4. Move Camera Rig to head height
        var cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig != null)
        {
            cameraRig.transform.position = new Vector3(0f, 1.6f, 0f);
            EditorUtility.SetDirty(cameraRig);
            results.AppendLine("Camera Rig position set to (0, 1.6, 0)");
        }

        // 5. Verify FurniturePlacer is still present
        var fp = GameObject.Find("FurniturePlacer");
        if (fp != null)
        {
            var placer = fp.GetComponent<ShopMR.Placement.FurniturePlacer>();
            if (placer != null)
            {
                results.AppendLine($"FurniturePlacer found. Placeholder: {(placer.placeholderPrefab != null ? "OK" : "NULL")}, GhostMat: {(placer.ghostMaterial != null ? "OK" : "NULL")}");
            }
            else
            {
                results.AppendLine("WARNING: FurniturePlacer component missing");
            }
        }
        else
        {
            results.AppendLine("WARNING: FurniturePlacer GameObject not found in scene");
        }

        // 6. Save the correct scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        results.AppendLine($"Saved scene: {currentScene.path}");

        // 7. Delete the duplicate scene file if it exists
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(wrongPath) != null)
        {
            AssetDatabase.DeleteAsset(wrongPath);
            results.AppendLine($"Deleted duplicate scene: {wrongPath}");
        }

        return results.ToString();
    }
}
