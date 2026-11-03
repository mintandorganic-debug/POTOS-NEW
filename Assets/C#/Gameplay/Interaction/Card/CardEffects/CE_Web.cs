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
        GameLog.Write(
            "CE_Web",
            $"ApplyEffectToTarget effect={name} target={(player != null ? player.name : "NULL")} multiplier={multiplier} duration={duration}");

        if (player == null)
        {
            GameLog.Write(
                "CE_Web",
                $"ApplyEffectToTarget skipped effect={name}: player is NULL");

            return;
        }

        PlayerEffects effects =
            player.GetComponentInChildren<PlayerEffects>(true);

        if (effects == null)
        {
            GameLog.Write(
                "CE_Web",
                $"ApplyEffectToTarget skipped effect={name} target={player.name}: PlayerEffects is missing");

            Debug.LogWarning(
                "CE_Web: PlayerEffects is missing on target player.",
                player);

            return;
        }

        GameLog.Write(
            "CE_Web",
            $"ApplyEffectToTarget effect={name} target={player.name}: calling ApplyCameraMultiplier multiplier={multiplier} duration={duration}");

        effects.ApplyCameraMultiplier(
            multiplier,
            duration);
    }
}