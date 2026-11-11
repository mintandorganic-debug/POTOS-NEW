using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DiceAnimatorSetup
{
    private const string TargetPath = "Assets/Player_AC.controller";
    private const string ModelPath = "Assets/props/models/hands.fbx";
    private const string MaskPath = "Assets/DiceHandsMask.mask";
    private const string TestLayer = "DiceHands";
    private const string RollState = "GameDiceRoll";
    private const string SlotState = "GameSlotCall";

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

    [MenuItem("Potos/Setup Hands Test Layer")]
    public static void Setup()
    {
        AnimatorController target =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                TargetPath);

        if (target == null)
        {
            Debug.LogError(
                $"DiceAnimatorSetup: controller not found at {TargetPath}");

            return;
        }

        AnimationClip roll =
            FindClip("DICE_ROLL");

        AnimationClip slot =
            FindClip("SLOT_MACHINE_CALL");

        if (roll == null ||
            slot == null)
        {
            Debug.LogError(
                $"DiceAnimatorSetup: clips not found in {ModelPath} roll={(roll != null)} slot={(slot != null)}");

            return;
        }

        for (int i = target.layers.Length - 1;
             i >= 1;
             i--)
        {
            AnimatorControllerLayer layer =
                target.layers[i];

            if (layer.name == TestLayer ||
                IsLeftover(layer.stateMachine))
            {
                target.RemoveLayer(
                    i);
            }
        }

        RemoveLeftoverStates(
            target.layers[0].stateMachine);

        target.AddLayer(
            TestLayer);

        AnimatorControllerLayer[] layers =
            target.layers;

        int index =
            layers.Length - 1;

        layers[index].defaultWeight = 0f;
        layers[index].avatarMask = null;
        target.layers = layers;

        AnimatorStateMachine machine =
            target.layers[index].stateMachine;

        AnimatorState rollState =
            machine.AddState(
                RollState);

        rollState.motion = roll;

        AnimatorState slotState =
            machine.AddState(
                SlotState);

        slotState.motion = slot;

        machine.defaultState = rollState;

        if (AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath) != null)
        {
            AssetDatabase.DeleteAsset(
                MaskPath);
        }

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"DiceAnimatorSetup: layer {TestLayer} added to Player_AC with states {RollState} ({roll.name}, {roll.length:0.00}s) and {SlotState} ({slot.name}, {slot.length:0.00}s) from the game hands");
    }

    private static bool IsConfigured()
    {
        AnimatorController target =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                TargetPath);

        if (target == null)
        {
            return true;
        }

        AnimatorControllerLayer layer =
            target.layers.FirstOrDefault(
                l => l.name == TestLayer);

        return layer != null &&
               layer.defaultWeight == 0f &&
               layer.stateMachine.states.Any(
                   s => s.state.name == RollState) &&
               layer.stateMachine.states.Any(
                   s => s.state.name == SlotState) &&
               !target.layers.Skip(1).Any(
                   l => l.name != TestLayer && IsLeftover(l.stateMachine)) &&
               !target.layers[0].stateMachine.states.Any(
                   s => IsLeftoverName(s.state.name));
    }

    private static AnimationClip FindClip(
        string clipName)
    {
        foreach (Object asset in
                 AssetDatabase.LoadAllAssetsAtPath(
                     ModelPath))
        {
            if (asset is AnimationClip clip &&
                !clip.name.StartsWith("__preview__") &&
                clip.name.Substring(
                    clip.name.LastIndexOf('|') + 1) == clipName)
            {
                return clip;
            }
        }

        return null;
    }

    private static bool IsLeftoverName(
        string name)
    {
        return name.StartsWith("Dice") ||
               name == RollState ||
               name == SlotState;
    }

    private static bool IsLeftover(
        AnimatorStateMachine machine)
    {
        return machine.states.Length > 0 &&
               machine.states.All(
                   s => IsLeftoverName(s.state.name));
    }

    private static void RemoveLeftoverStates(
        AnimatorStateMachine machine)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (IsLeftoverName(child.state.name))
            {
                machine.RemoveState(
                    child.state);
            }
        }
    }
}
