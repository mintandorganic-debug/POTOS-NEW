using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copies the UI dice with the green glow particles from the artist's FXDemoScene into a prefab
// (Assets/Resources/Dice/DiceUIGlow.prefab) exactly as they are set up there.
public static class DiceUiPrefabExtractor
{
    private const string ScenePath =
        "Assets/Scenes/Sandbox/FXDemoScene.unity";

    private const string PrefabPath =
        "Assets/Resources/Dice/DiceUIGlow.prefab";

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        EditorApplication.delayCall +=
            () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                {
                    Extract();
                }
            };
    }

    [MenuItem("Potos/Extract Dice UI Glow Prefab")]
    private static void Extract()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            return;
        }

        Scene opened =
            EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);

        GameObject source = null;

        foreach (GameObject root in opened.GetRootGameObjects())
        {
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "DiceUIBase")
                {
                    source = child.gameObject;
                    break;
                }
            }

            if (source != null)
            {
                break;
            }
        }

        if (source != null)
        {
            PrefabUtility.SaveAsPrefabAsset(
                source,
                PrefabPath);

            Debug.Log(
                "DiceUiPrefabExtractor: saved " + PrefabPath);
        }
        else
        {
            Debug.LogWarning(
                "DiceUiPrefabExtractor: DiceUIBase not found in " + ScenePath);
        }

        EditorSceneManager.CloseScene(
            opened,
            true);
    }
}
