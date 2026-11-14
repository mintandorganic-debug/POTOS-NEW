#if UNITY_EDITOR
using Mirror;
using UnityEngine;

public class HandsRollAnimation
{
    private const string HandsControllerName = "Player_AC";
    private const string DiceLayerName = "DiceHands";
    private const string RollState = "GameDiceRoll";
    private const string WristName = "Wrist.R";

    private readonly Animator hands;
    private readonly int layer;

    private HandsRollAnimation(
        Animator hands,
        int layer,
        float length,
        Transform wrist)
    {
        this.hands = hands;
        this.layer = layer;

        Length = length;
        Wrist = wrist;
    }

    public float Length { get; }

    public Transform Wrist { get; }

    public static HandsRollAnimation Find()
    {
        Animator hands =
            FindHands();

        int layer =
            hands != null
                ? hands.GetLayerIndex(
                    DiceLayerName)
                : -1;

        float length =
            hands != null
                ? ClipLength(
                    hands)
                : 0f;

        Transform wrist =
            hands != null
                ? FindDeep(
                    hands.transform,
                    WristName)
                : null;

        if (hands == null ||
            layer < 0 ||
            length <= 0f ||
            wrist == null)
        {
            GameLog.Write(
                "HandsRollAnimation",
                $"Find failed hands={(hands != null)} layer={layer} clipLength={length} wrist={(wrist != null)}");

            return null;
        }

        return new HandsRollAnimation(
            hands,
            layer,
            length,
            wrist);
    }

    public void SetWeight(
        float weight)
    {
        hands.SetLayerWeight(
            layer,
            weight);
    }

    public void Scrub(
        float time)
    {
        hands.Play(
            RollState,
            layer,
            Mathf.Clamp01(time / Length));
    }

    private static float ClipLength(
        Animator hands)
    {
        foreach (AnimationClip clip in
                 hands.runtimeAnimatorController.animationClips)
        {
            if (clip.name.EndsWith("DICE_ROLL"))
            {
                return clip.length;
            }
        }

        return 0f;
    }

    private static Animator FindHands()
    {
        if (NetworkClient.localPlayer == null)
        {
            return null;
        }

        foreach (Animator candidate in
                 NetworkClient.localPlayer.GetComponentsInChildren<Animator>(true))
        {
            if (candidate.runtimeAnimatorController != null &&
                candidate.runtimeAnimatorController.name == HandsControllerName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDeep(
        Transform root,
        string name)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
#endif
