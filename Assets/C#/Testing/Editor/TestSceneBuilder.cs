#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using kcp2k;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Testing > ...
/// Builds Assets/Scenes/TestScene.unity: a copy of GameplayScene plus the
/// TestHarness object (network manager, bots, auto tester, test panel).
/// </summary>
public static class TestSceneBuilder
{
    private const string SourceScene = "Assets/Scenes/GameplayScene.unity";
    private const string TestScenePath = "Assets/Scenes/TestScene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [MenuItem("Tools/Testing/Create Test Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode first.");
            return;
        }

        if (!File.Exists(SourceScene))
        {
            Debug.LogError("TestSceneBuilder: not found: " + SourceScene);
            return;
        }

        GameObject playerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

        if (playerPrefab == null)
        {
            Debug.LogError("TestSceneBuilder: not found: " + PlayerPrefabPath);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(TestScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Create Test Scene",
                    TestScenePath + " already exists. Rebuild it from " +
                    "GameplayScene? (your changes in TestScene are lost)",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(TestScenePath);
        }

        if (!AssetDatabase.CopyAsset(SourceScene, TestScenePath))
        {
            Debug.LogError("TestSceneBuilder: could not copy the scene.");
            return;
        }

        AssetDatabase.Refresh();

        Scene scene =
            EditorSceneManager.OpenScene(
                TestScenePath,
                OpenSceneMode.Single);

        BuildHarness(
            scene,
            playerPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AddToBuildSettings(TestScenePath);

        Debug.Log(
            "TestSceneBuilder: " + TestScenePath + " is ready. " +
            "Press Play (it hosts automatically). F1 = test panel.");
    }

    [MenuItem("Tools/Testing/Open Test Scene")]
    public static void Open()
    {
        if (!File.Exists(TestScenePath))
        {
            Debug.LogError(
                "TestScene does not exist yet. Run Tools > Testing > Create Test Scene.");

            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(
                TestScenePath,
                OpenSceneMode.Single);
        }
    }

    [MenuItem("Tools/Testing/Open Log Folder")]
    public static void OpenLogFolder()
    {
        string dir =
            Path.Combine(
                Application.persistentDataPath,
                "GameLogs");

        Directory.CreateDirectory(dir);

        EditorUtility.RevealInFinder(dir);
    }

    // =========================================================
    // BUILD
    // =========================================================

    private static void BuildHarness(
        Scene scene,
        GameObject playerPrefab)
    {
        // Two network managers would destroy each other.
        foreach (NetworkManager existing
                 in Object.FindObjectsByType<NetworkManager>())
        {
            Debug.LogWarning(
                "TestSceneBuilder: removing " + existing.name +
                " from the test scene.");

            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject harness =
            new GameObject("TestHarness");

        SceneManager.MoveGameObjectToScene(
            harness,
            scene);

        KcpTransport transport =
            harness.AddComponent<KcpTransport>();

        TestNetworkManager manager =
            harness.AddComponent<TestNetworkManager>();

        harness.AddComponent<BotDirector>();
        harness.AddComponent<AutoTester>();
        harness.AddComponent<TestSceneController>();

        List<GameObject> spawnable =
            FindNetworkPrefabs();

        SerializedObject so =
            new SerializedObject(manager);

        so.FindProperty("transport").objectReferenceValue = transport;
        so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
        so.FindProperty("autoCreatePlayer").boolValue = true;
        so.FindProperty("maxConnections").intValue = 5;
        so.FindProperty("dontDestroyOnLoad").boolValue = false;
        so.FindProperty("runInBackground").boolValue = true;
        so.FindProperty("networkAddress").stringValue = "localhost";

        SerializedProperty list = so.FindProperty("spawnPrefabs");
        list.arraySize = spawnable.Count;

        for (int i = 0; i < spawnable.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue =
                spawnable[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log(
            $"TestSceneBuilder: {spawnable.Count} network prefab(s) registered.");
    }

    // every prefab with a NetworkIdentity (except Mirror's own examples)
    private static List<GameObject> FindNetworkPrefabs()
    {
        List<GameObject> result =
            new List<GameObject>();

        foreach (string guid
                 in AssetDatabase.FindAssets(
                     "t:Prefab",
                     new[] { "Assets" }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            if (path.StartsWith("Assets/Mirror/") ||
                path.StartsWith("Assets/TextMesh Pro/") ||
                path.Contains("/Samples/"))
            {
                continue;
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null &&
                prefab.GetComponent<NetworkIdentity>() != null)
            {
                result.Add(prefab);
            }
        }

        return result;
    }

    private static void AddToBuildSettings(
        string path)
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

        foreach (EditorBuildSettingsScene existing in scenes)
        {
            if (existing.path == path)
                return;
        }

        scenes.Add(
            new EditorBuildSettingsScene(
                path,
                true));

        EditorBuildSettings.scenes =
            scenes.ToArray();
    }
}
#endif
