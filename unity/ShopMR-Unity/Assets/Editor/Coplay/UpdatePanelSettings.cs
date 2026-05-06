using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UpdatePanelSettings
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // Update serialized spawnDistance and verticalOffset on CatalogPanelController
        var panelGO = GameObject.Find("CatalogPanel");
        if (panelGO != null)
        {
            var controller = panelGO.GetComponent<ShopMR.Catalog.CatalogPanelController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);

                var spawnDist = so.FindProperty("spawnDistance");
                if (spawnDist != null)
                {
                    spawnDist.floatValue = 1.2f;
                    results.AppendLine("Set spawnDistance = 1.2m");
                }

                var vertOff = so.FindProperty("verticalOffset");
                if (vertOff != null)
                {
                    vertOff.floatValue = -0.2f;
                    results.AppendLine("Set verticalOffset = -0.2m");
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
            }
        }

        // Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
