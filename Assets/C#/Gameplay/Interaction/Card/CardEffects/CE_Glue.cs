using UnityEngine;

[CreateAssetMenu(
    fileName = "CE_Glue",
    menuName = "Game/Card Effects/Glue")]
public class CE_Glue : CE_Base
{
    [SerializeField] private float duration = 30f;

    public override void ApplyEffectToTarget(
        Player player)
    {
        GameLog.Write(
            "CE_Glue",
            $"ApplyEffectToTarget effect={name} target={(player != null ? player.name : "NULL")} duration={duration}");

        if (player == null)
        {
            GameLog.Write(
                "CE_Glue",
                $"ApplyEffectToTarget skipped effect={name}: player is NULL");

            return;
        }

        PlayerEffects effects =
            player.GetComponent<PlayerEffects>();

        if (effects == null)
        {
            GameLog.Write(
                "CE_Glue",
                $"ApplyEffectToTarget skipped effect={name} target={player.name}: PlayerEffects is missing");

            return;
        }

        GameLog.Write(
            "CE_Glue",
            $"ApplyEffectToTarget effect={name} target={player.name}: calling BlockRandomCard duration={duration}");

        effects.BlockRandomCard(
            duration);
    }
}