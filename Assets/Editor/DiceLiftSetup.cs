using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DiceLiftSetup
{
    private const string ControllerPath = "Assets/Lift_AC.controller";
    private const string ModelPath = "Assets/props/models/platform.fbx";

    private static readonly string[] Triggers =
    {
        "DropEmpty",
        "PlatformUp",
        "PlatformDown",
        "PlateUp",
        "PlateDown",
        "SlotUp",
        "SlotDown"
    };

    [InitializeOnLoadMethod]
    private static void AutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!IsConfigured())
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
            "DICE_PLATE_HIDE",
            "SLOT_MACHINE_APPEAR",
            "SLOT_MACHINE_HIDE"
        };

        foreach (string name in required)
        {
            if (!clips.ContainsKey(name))
            {
                Debug.LogError(
                    $"DiceLiftSetup: clip {name} not found in {ModelPath}");

                return;
            }

            if (clips[name].isLooping)
            {
                Debug.LogWarning(
                    $"DiceLiftSetup: clip {name} is looping, it should hold the last frame");
            }
        }

        while (controller.parameters.Length > 0)
        {
            controller.RemoveParameter(0);
        }

        foreach (string trigger in Triggers)
        {
            controller.AddParameter(
                trigger,
                AnimatorControllerParameterType.Trigger);
        }

        while (controller.layers.Length > 1)
        {
            controller.RemoveLayer(
                controller.layers.Length - 1);
        }

        AnimatorControllerLayer[] layers =
            controller.layers;

        layers[0].name = "Platform";
        layers[0].defaultWeight = 1f;
        controller.layers = layers;

        BuildLayer(
            controller,
            0,
            clips["PLATFORM_HIDE"],
            clips["PLATFORM_APPEAR"],
            "PlatformUp",
            "PlatformDown");

        controller.AddLayer(
            "DicePlate");

        BuildLayer(
            controller,
            1,
            clips["DICE_PLATE_HIDE"],
            clips["DICE_PLATE_APPEAR"],
            "PlateUp",
            "PlateDown");

        controller.AddLayer(
            "SlotMachine");

        BuildLayer(
            controller,
            2,
            clips["SLOT_MACHINE_HIDE"],
            clips["SLOT_MACHINE_APPEAR"],
            "SlotUp",
            "SlotDown");

        layers =
            controller.layers;

        for (int i = 0;
             i < layers.Length;
             i++)
        {
            layers[i].defaultWeight = 1f;
        }

        controller.layers = layers;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "DiceLiftSetup: Lift_AC configured (layers Platform, DicePlate, SlotMachine)");
    }

    private static bool IsConfigured()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);

        return controller != null &&
               controller.layers.Length == 3 &&
               controller.parameters.Any(
                   p => p.name == "PlatformUp");
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

    private static void BuildLayer(
        AnimatorController controller,
        int index,
        AnimationClip hideClip,
        AnimationClip appearClip,
        string upTrigger,
        string downTrigger)
    {
        AnimatorStateMachine machine =
            controller.layers[index].stateMachine;

        foreach (ChildAnimatorState child in machine.states)
        {
            machine.RemoveState(
                child.state);
        }

        AnimatorState hide =
            machine.AddState(
                "Hide");

        hide.motion = hideClip;

        AnimatorState appear =
            machine.AddState(
                "Appear");

        appear.motion = appearClip;

        machine.defaultState = hide;

        AddTriggerTransition(
            hide,
            appear,
            upTrigger);

        AddTriggerTransition(
            appear,
            hide,
            downTrigger);
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
