using UnityEngine;

[CreateAssetMenu(
    fileName = "CE_Web",
    menuName = "Game/Card Effects/Web")]
public class CE_Web : CE_Base
{
    [SerializeField] private float multiplier = 0.5f;
    [SerializeField] private float duration = 30f;

    public override void ApplyEffectToTarget(
        Player player)
    {
        if (player == null)
            return;

        PlayerEffects effects =
            player.GetComponentInChildren<PlayerEffects>(true);

        if (effects == null)
        {
            Debug.LogWarning(
                "CE_Web: PlayerEffects is missing on target player.",
                player);

            return;
        }

        effects.ApplyCameraMultiplier(
            multiplier,
            duration);
    }
}