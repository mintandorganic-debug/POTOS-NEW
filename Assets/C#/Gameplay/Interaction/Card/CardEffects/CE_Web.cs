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
            player.GetComponent<PlayerEffects>();

        if (effects == null)
            return;

        effects.ApplyCameraMultiplier(
            multiplier,
            duration);
    }
}