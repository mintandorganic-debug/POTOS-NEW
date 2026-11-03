using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DiceLiftSetup
{
    private const string ControllerPath = "Assets/Lift_AC.controller";
    private const string ModelPath = "Assets/props/models/platform.fbx";
    private const string PlatformLayer = "PlatformLift";
    private const string PlateLayer = "DicePlate";

    private static readonly string[] Triggers =
    {
        "DropEmpty",
        "PlatformDown",
        "PlatformUp",
        "PlateUp",
        "PlateDown"
    };

    [InitializeOnLoadMethod]
    private static void AutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                !IsConfigured())
            {
                Setup();
            }
        };
    }

    [MenuItem("Potos/Setup Dice Lift")]
    public static void Setup()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);

        if (controller == null)
        {
            Debug.LogError(
                $"DiceLiftSetup: controller not found at {ControllerPath}");

            return;
        }

        Dictionary<string, AnimationClip> clips =
            LoadClips();

        string[] required =
        {
            "PLATFORM_APPEAR",
            "PLATFORM_HIDE",
            "DICE_PLATE_APPEAR",
            "DICE_PLATE_HIDE"
        };

        foreach (string name in required)
        {
            if (!clips.ContainsKey(name))
            {
                Debug.LogError(
                    $"DiceLiftSetup: clip {name} not found in {ModelPath}");

                return;
            }
        }

        foreach (string trigger in Triggers)
        {
            if (!controller.parameters.Any(
                    p => p.name == trigger))
            {
                controller.AddParameter(
                    trigger,
                    AnimatorControllerParameterType.Trigger);
            }
        }

        RemoveLayer(
            controller,
            PlatformLayer);

        RemoveLayer(
            controller,
            PlateLayer);

        AddPlatformLayer(
            controller,
            clips);

        AddPlateLayer(
            controller,
            clips);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "DiceLiftSetup: layers PlatformLift and DicePlate added to Lift_AC, Base Layer untouched");
    }

    private static bool IsConfigured()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);

        return controller != null &&
               controller.layers.Any(
                   l => l.name == PlatformLayer) &&
               controller.layers.Any(
                   l => l.name == PlateLayer) &&
               controller.parameters.Any(
                   p => p.name == "PlatformUp");
    }

    private static void RemoveLayer(
        AnimatorController controller,
        string layerName)
    {
        for (int i = controller.layers.Length - 1;
             i > 0;
             i--)
        {
            if (controller.layers[i].name == layerName)
            {
                controller.RemoveLayer(
                    i);
            }
        }
    }

    private static Dictionary<string, AnimationClip> LoadClips()
    {
        Dictionary<string, AnimationClip> clips =
            new Dictionary<string, AnimationClip>();

        foreach (Object asset in
                 AssetDatabase.LoadAllAssetsAtPath(
                     ModelPath))
        {
            if (asset is AnimationClip clip &&
                !clip.name.StartsWith("__preview__"))
            {
                string name =
                    clip.name.Substring(
                        clip.name.LastIndexOf('|') + 1);

                clips[name] = clip;
            }
        }

        return clips;
    }

    private static void AddPlatformLayer(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips)
    {
        AnimatorStateMachine machine =
            AddLayer(
                controller,
                PlatformLayer);

        AnimatorState idle =
            machine.AddState(
                "Idle");

        AnimatorState down =
            machine.AddState(
                "Down");

        down.motion = clips["PLATFORM_HIDE"];

        AnimatorState up =
            machine.AddState(
                "Up");

        up.motion = clips["PLATFORM_APPEAR"];

        machine.defaultState = idle;

        AddTriggerTransition(
            idle,
            down,
            "PlatformDown");

        AddTriggerTransition(
            idle,
            down,
            "DropEmpty");

        AddTriggerTransition(
            up,
            down,
            "PlatformDown");

        AddTriggerTransition(
            up,
            down,
            "DropEmpty");

        AddTriggerTransition(
            down,
            up,
            "PlatformUp");
    }

    private static void AddPlateLayer(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips)
    {
        AnimatorStateMachine machine =
            AddLayer(
                controller,
                PlateLayer);

        AnimatorState hide =
            machine.AddState(
                "Hide");

        hide.motion = clips["DICE_PLATE_HIDE"];

        AnimatorState appear =
            machine.AddState(
                "Appear");

        appear.motion = clips["DICE_PLATE_APPEAR"];

        machine.defaultState = hide;

        AddTriggerTransition(
            hide,
            appear,
            "PlateUp");

        AddTriggerTransition(
            appear,
            hide,
            "PlateDown");
    }

    private static AnimatorStateMachine AddLayer(
        AnimatorController controller,
        string layerName)
    {
        controller.AddLayer(
            layerName);

        AnimatorControllerLayer[] layers =
            controller.layers;

        int index =
            layers.Length - 1;

        layers[index].defaultWeight = 1f;
        controller.layers = layers;

        return controller.layers[index].stateMachine;
    }

    private static void AddTriggerTransition(
        AnimatorState from,
        AnimatorState to,
        string trigger)
    {
        AnimatorStateTransition transition =
            from.AddTransition(
                to);

        transition.hasExitTime = false;
        transition.duration = 0f;

        transition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            trigger);
    }
}
