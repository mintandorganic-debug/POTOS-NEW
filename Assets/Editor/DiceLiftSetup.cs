using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DiceLiftSetup
{
    private const string ControllerPath = "Assets/Lift_AC.controller";
    private const string ModelPath = "Assets/props/models/platform.fbx";
    private const string LiftLayer = "Lift";

    private static readonly string[] OldLayers =
    {
        "PlatformLift",
        "DicePlate",
        "SlotMachine"
    };

    private static readonly string[] Triggers =
    {
        "DropEmpty",
        "PlatformDown",
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

        foreach (string oldLayer in OldLayers)
        {
            RemoveLayer(
                controller,
                oldLayer);
        }

        RemoveLayer(
            controller,
            LiftLayer);

        AddLiftLayer(
            controller,
            clips);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "DiceLiftSetup: layer Lift added to Lift_AC (platform hide, dice plate appear, dice plate hide), Base Layer untouched");
    }

    private static bool IsConfigured()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);

        return controller != null &&
               controller.layers.Any(
                   l => l.name == LiftLayer) &&
               !controller.layers.Any(
                   l => OldLayers.Contains(l.name)) &&
               controller.parameters.Any(
                   p => p.name == "PlateUp");
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

    private static void AddLiftLayer(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips)
    {
        controller.AddLayer(
            LiftLayer);

        AnimatorControllerLayer[] layers =
            controller.layers;

        int index =
            layers.Length - 1;

        layers[index].defaultWeight = 1f;
        controller.layers = layers;

        AnimatorStateMachine machine =
            controller.layers[index].stateMachine;

        AnimatorState idle =
            machine.AddState(
                "Idle");

        AnimatorState platformHide =
            machine.AddState(
                "PlatformHide");

        platformHide.motion = clips["PLATFORM_HIDE"];

        AnimatorState diceAppear =
            machine.AddState(
                "DiceAppear");

        diceAppear.motion = clips["DICE_PLATE_APPEAR"];

        AnimatorState diceHide =
            machine.AddState(
                "DiceHide");

        diceHide.motion = clips["DICE_PLATE_HIDE"];

        machine.defaultState = idle;

        AddTriggerTransition(
            idle,
            platformHide,
            "PlatformDown");

        AddTriggerTransition(
            idle,
            platformHide,
            "DropEmpty");

        AddTriggerTransition(
            idle,
            diceAppear,
            "PlateUp");

        AddTriggerTransition(
            platformHide,
            diceAppear,
            "PlateUp");

        AddTriggerTransition(
            diceHide,
            diceAppear,
            "PlateUp");

        AddTriggerTransition(
            diceAppear,
            diceHide,
            "PlateDown");
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
        transition.duration = 0.1f;

        transition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            trigger);
    }
}
