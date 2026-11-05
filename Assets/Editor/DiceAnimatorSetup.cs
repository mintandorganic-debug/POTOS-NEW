using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DiceAnimatorSetup
{
    private const string SourcePath = "Assets/Scenes/Sandbox/PotosVFX/Anims/TorchTestController.controller";
    private const string TargetPath = "Assets/Player_AC.controller";
    private const string StatePrefix = "Dice";
    private const string ProbeState = "DicePrep";

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

    [MenuItem("Potos/Copy Dice States To Player_AC")]
    public static void Setup()
    {
        AnimatorController source =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                SourcePath);

        AnimatorController target =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                TargetPath);

        if (source == null ||
            target == null)
        {
            Debug.LogError(
                $"DiceAnimatorSetup: controller missing source={(source != null)} target={(target != null)}");

            return;
        }

        CopyParameters(
            source,
            target);

        int copied = 0;

        for (int i = 0;
             i < source.layers.Length;
             i++)
        {
            AnimatorStateMachine sourceMachine =
                source.layers[i].stateMachine;

            if (!sourceMachine.states.Any(
                    s => s.state.name.StartsWith(StatePrefix)))
            {
                continue;
            }

            int targetIndex =
                i == 0
                    ? 0
                    : FindOrAddLayer(
                        source,
                        target,
                        i);

            copied +=
                CopyLayerStates(
                    sourceMachine,
                    target.layers[targetIndex].stateMachine,
                    target);
        }

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"DiceAnimatorSetup: copied {copied} Dice states into Player_AC");
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

        return target.layers.Any(
            l => l.stateMachine.states.Any(
                s => s.state.name == ProbeState));
    }

    private static void CopyParameters(
        AnimatorController source,
        AnimatorController target)
    {
        foreach (AnimatorControllerParameter parameter in source.parameters)
        {
            AnimatorControllerParameter existing =
                target.parameters.FirstOrDefault(
                    p => p.name == parameter.name);

            if (existing == null)
            {
                target.AddParameter(
                    parameter.name,
                    parameter.type);
            }
            else if (existing.type != parameter.type)
            {
                Debug.LogWarning(
                    $"DiceAnimatorSetup: parameter {parameter.name} has type {existing.type} in Player_AC and {parameter.type} in the artist controller, kept as is");
            }
        }
    }

    private static int FindOrAddLayer(
        AnimatorController source,
        AnimatorController target,
        int sourceIndex)
    {
        string name =
            source.layers[sourceIndex].name;

        for (int i = 1;
             i < target.layers.Length;
             i++)
        {
            if (target.layers[i].name == name)
            {
                return i;
            }
        }

        target.AddLayer(
            name);

        AnimatorControllerLayer[] layers =
            target.layers;

        int last =
            layers.Length - 1;

        layers[last].defaultWeight =
            source.layers[sourceIndex].defaultWeight;

        layers[last].blendingMode =
            source.layers[sourceIndex].blendingMode;

        target.layers = layers;

        return last;
    }

    private static int CopyLayerStates(
        AnimatorStateMachine sourceMachine,
        AnimatorStateMachine targetMachine,
        AnimatorController target)
    {
        foreach (ChildAnimatorState child in targetMachine.states)
        {
            if (child.state.name.StartsWith(StatePrefix))
            {
                targetMachine.RemoveState(
                    child.state);
            }
        }

        Dictionary<AnimatorState, AnimatorState> map =
            new Dictionary<AnimatorState, AnimatorState>();

        foreach (ChildAnimatorState child in sourceMachine.states)
        {
            if (!child.state.name.StartsWith(StatePrefix))
            {
                continue;
            }

            AnimatorState copy =
                targetMachine.AddState(
                    child.state.name);

            copy.motion = child.state.motion;
            copy.speed = child.state.speed;
            copy.speedParameterActive = child.state.speedParameterActive;
            copy.speedParameter = child.state.speedParameter;
            copy.cycleOffset = child.state.cycleOffset;
            copy.mirror = child.state.mirror;
            copy.writeDefaultValues = child.state.writeDefaultValues;
            copy.tag = child.state.tag;

            if (child.state.motion is BlendTree)
            {
                Debug.LogWarning(
                    $"DiceAnimatorSetup: state {child.state.name} uses a blend tree shared with the artist controller");
            }

            if (child.state.behaviours.Length > 0)
            {
                Debug.LogWarning(
                    $"DiceAnimatorSetup: state {child.state.name} has {child.state.behaviours.Length} state behaviours that were not copied");
            }

            map[child.state] = copy;
        }

        foreach (KeyValuePair<AnimatorState, AnimatorState> pair in map)
        {
            foreach (AnimatorStateTransition transition in pair.Key.transitions)
            {
                AnimatorStateTransition copy;

                if (transition.isExit)
                {
                    copy = pair.Value.AddExitTransition();
                }
                else if (transition.destinationState != null &&
                         map.ContainsKey(transition.destinationState))
                {
                    copy = pair.Value.AddTransition(
                        map[transition.destinationState]);
                }
                else
                {
                    continue;
                }

                CopyTransition(
                    transition,
                    copy,
                    target);
            }
        }

        foreach (AnimatorStateTransition transition in sourceMachine.anyStateTransitions)
        {
            if (transition.destinationState == null ||
                !map.ContainsKey(transition.destinationState))
            {
                continue;
            }

            AnimatorStateTransition copy =
                targetMachine.AddAnyStateTransition(
                    map[transition.destinationState]);

            CopyTransition(
                transition,
                copy,
                target);
        }

        return map.Count;
    }

    private static void CopyTransition(
        AnimatorStateTransition source,
        AnimatorStateTransition copy,
        AnimatorController target)
    {
        copy.hasExitTime = source.hasExitTime;
        copy.exitTime = source.exitTime;
        copy.hasFixedDuration = source.hasFixedDuration;
        copy.duration = source.duration;
        copy.offset = source.offset;
        copy.interruptionSource = source.interruptionSource;
        copy.orderedInterruption = source.orderedInterruption;
        copy.canTransitionToSelf = source.canTransitionToSelf;

        foreach (AnimatorCondition condition in source.conditions)
        {
            AnimatorControllerParameter parameter =
                target.parameters.FirstOrDefault(
                    p => p.name == condition.parameter);

            if (parameter == null)
            {
                Debug.LogWarning(
                    $"DiceAnimatorSetup: condition parameter {condition.parameter} missing, skipped");

                continue;
            }

            copy.AddCondition(
                condition.mode,
                condition.threshold,
                condition.parameter);
        }
    }
}
