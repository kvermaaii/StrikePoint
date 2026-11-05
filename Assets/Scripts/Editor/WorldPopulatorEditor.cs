using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility allowing 1-click permanent scene population with
/// Bullseye Targets and Explosive Barrels across the full course.
/// </summary>
public class WorldPopulatorEditor : Editor
{
    [MenuItem("Tools/Populate Scene with Targets & Barrels")]
    public static void PopulateCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        // Remove existing auto-population root if present to refresh
        GameObject oldRoot = GameObject.Find("Auto_Combat_Populations");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        AutoLevelPopulator.PopulateScene(activeScene.name);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        EditorUtility.DisplayDialog("StrikePoint Populator",
            $"Course '{activeScene.name}' has been populated with full-course Targets & Explosive Barrels!", "Awesome!");
    }

    [MenuItem("Tools/Clear Populated Targets & Barrels")]
    public static void ClearCurrentScene()
    {
        GameObject oldRoot = GameObject.Find("Auto_Combat_Populations");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("StrikePoint Populator", "Cleared populated objects.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("StrikePoint Populator", "No populated objects found in scene.", "OK");
        }
    }
}
