#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Audio > ...  Wires the 22 customer sounds without manual dragging.
/// Safe to run several times.
/// </summary>
public static class CustomerSoundsSetup
{
    private const string SoundsRoot = "Assets/Sounds/";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    // Idle
    private static readonly string[] IdleFiles =
    {
        "EXHALE_SOUND.wav",
        "BURP_SOUND.wav",
        "CAUGH_SOUND.wav",
        "CREAKING_CHAIR_SOUND.wav"
    };

    // Reactions
    private static readonly string[] SuccessFiles =
    {
        "JOYFUL_SOUND.wav",
        "WHISTLE_SOUND.wav",
        "WONDERMENT_SOUND.wav"
    };

    private static readonly string[] FailureFiles =
    {
        "disappointment_sound.wav",
        "SCOFF_SOUND.wav"
    };

    // Item sounds. keyword = part of the CardData asset name (lower case).
    // NOTE: the customer's files spell it WHISKY (ATK) and WHISKEY (AFTERMATH).
    private struct Item
    {
        public string keyword;
        public string atk;
        public string aftermath;

        public Item(string keyword, string atk, string aftermath)
        {
            this.keyword = keyword;
            this.atk = atk;
            this.aftermath = aftermath;
        }
    }

    private static readonly Item[] Items =
    {
        new Item("knife",   "ATK_KNIFE_VIC_HIT_SOUND.wav",   "AFTERMATH_KNIFE_SOUND.wav"),
        new Item("fork",    "ATK_FORK_VIC_HIT_SOUND.wav",    "AFTERMATH_FORK_SOUND.wav"),
        new Item("bucket",  "ATK_BUCKET_VIC_HIT_SOUND.wav",  "AFTERMATH_BUCKET_SOUND.wav"),
        new Item("shears",  "ATK_SHEARS_VIC_HIT_SOUND.wav",  "AFTERMATH_SHEARS_SOUND.wav"),
        new Item("whisk",   "ATK_WHISKY_VIC_HIT_SOUND.wav",  "AFTERMATH_WHISKEY_SOUND.wav"),
        // no ATK file was delivered for these three
        new Item("revolver", null,                           "AFTERMATH_REVOLVER_SOUND.wav"),
        new Item("colt",     null,                           "AFTERMATH_REVOLVER_SOUND.wav"),
        new Item("shotgun",  null,                           "AFTERMATH_SHOTGUN_SOUND.wav"),
        new Item("grenade",  null,                           "AFTERMATH_GRENADE_SOUND.wav"),
    };

    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Audio/1. Assign Customer Sounds")]
    public static void AssignAll()
    {
        AssignPlayerPrefab();
        AssignCardData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "CustomerSounds: done. Next: Tools/Audio/2. Create GameAudio, " +
            "and move AudienceSoundPoint to the hall below the table.");
    }

    [MenuItem("Tools/Audio/2. Create GameAudio In Open Scene")]
    public static void CreateGameAudio()
    {
        if (Object.FindFirstObjectByType<GameAudio>() != null)
        {
            Debug.Log("GameAudio already exists in the open scene.");
            return;
        }

        GameObject root = new GameObject("GameAudio");
        GameAudio gameAudio = root.AddComponent<GameAudio>();

        GameObject point = new GameObject("AudienceSoundPoint");
        point.transform.SetParent(root.transform, false);

        AudioSource source = point.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 4f;
        source.maxDistance = 40f;

        SerializedObject so = new SerializedObject(gameAudio);
        so.FindProperty("audienceSource").objectReferenceValue = source;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root, "Create GameAudio");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = point;

        Debug.Log(
            "GameAudio created. Move 'AudienceSoundPoint' to the " +
            "audience hall below the table.", point);
    }

    [MenuItem("Tools/Audio/3. Force Customer Clips To Mono (for 3D)")]
    public static void ForceMono()
    {
        // Unity positions only MONO clips in 3D space; stereo clips ignore
        // the source position (volume falloff only).
        List<string> files = new List<string>();
        files.AddRange(IdleFiles);
        files.AddRange(SuccessFiles);
        files.AddRange(FailureFiles);

        foreach (Item item in Items)
        {
            if (item.atk != null) files.Add(item.atk);
            files.Add(item.aftermath);
        }

        int changed = 0;

        foreach (string file in files)
        {
            AudioImporter importer =
                AssetImporter.GetAtPath(SoundsRoot + file) as AudioImporter;

            if (importer == null || importer.forceToMono)
                continue;

            importer.forceToMono = true;
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"CustomerSounds: {changed} clip(s) set to Force To Mono.");
    }

    // =========================================================
    // PLAYER PREFAB
    // =========================================================

    private static void AssignPlayerPrefab()
    {
        GameObject contents =
            PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        try
        {
            Player playerComponent =
                contents.GetComponentInChildren<Player>(true);

            if (playerComponent == null)
            {
                Debug.LogError(
                    "CustomerSounds: no Player component in " +
                    PlayerPrefabPath);

                return;
            }

            GameObject host = playerComponent.gameObject;

            PlayerSounds sounds = host.GetComponent<PlayerSounds>();

            if (sounds == null)
                sounds = host.AddComponent<PlayerSounds>();

            SerializedObject so = new SerializedObject(sounds);

            SetClips(so.FindProperty("idleSounds"), IdleFiles);
            SetClips(so.FindProperty("successSounds"), SuccessFiles);
            SetClips(so.FindProperty("failureSounds"), FailureFiles);

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);

            Debug.Log("CustomerSounds: Player.prefab updated.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void SetClips(
        SerializedProperty array,
        string[] files)
    {
        int oldSize = array.arraySize;

        array.arraySize = files.Length;

        for (int i = 0; i < files.Length; i++)
        {
            SerializedProperty element =
                array.GetArrayElementAtIndex(i);

            AudioClip clip = LoadClip(files[i]);

            element.FindPropertyRelative("clip").objectReferenceValue = clip;
            element.FindPropertyRelative("name").stringValue =
                clip != null ? clip.name : string.Empty;

            // keep volumes the user already tuned; new entries get 1
            if (i >= oldSize)
                element.FindPropertyRelative("volume").floatValue = 1f;
        }
    }

    // =========================================================
    // CARD DATA
    // =========================================================

    private static void AssignCardData()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData data = AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (data == null)
                continue;

            string key = data.name.ToLowerInvariant();
            bool matched = false;

            foreach (Item item in Items)
            {
                if (!key.Contains(item.keyword))
                    continue;

                SerializedObject so = new SerializedObject(data);

                so.FindProperty("attackHitSound").objectReferenceValue =
                    item.atk != null ? LoadClip(item.atk) : null;

                so.FindProperty("aftermathSound").objectReferenceValue =
                    LoadClip(item.aftermath);

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);

                Debug.Log(
                    $"CustomerSounds: '{data.name}' <- " +
                    $"{item.atk ?? "(no ATK file)"} / {item.aftermath}",
                    data);

                matched = true;
                break;
            }

            if (!matched)
            {
                Debug.Log(
                    $"CustomerSounds: no sounds for '{data.name}' (skipped).",
                    data);
            }
        }
    }

    private static AudioClip LoadClip(string file)
    {
        AudioClip clip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(SoundsRoot + file);

        if (clip == null)
            Debug.LogError("CustomerSounds: clip not found: " + SoundsRoot + file);

        return clip;
    }
}
#endif
