using UnityEngine;

[CreateAssetMenu(
    fileName = "CE_Knife",
    menuName = "Game/Card Effects/Knife")]
public class CE_Knife : CE_Base
{
    [SerializeField] private float duration = 30f;

    public override void ApplyEffectToTarget(
        Player player)
    {
        GameLog.Write(
            "CE_Knife",
            $"ApplyEffectToTarget effect={name} target={(player != null ? player.name : "NULL")} duration={duration}");

        if (player == null)
        {
            GameLog.Write(
                "CE_Knife",
                $"ApplyEffectToTarget skipped effect={name}: player is NULL");

            return;
        }

        PlayerEffects effects =
            player.GetComponent<PlayerEffects>();

        if (effects == null)
        {
            GameLog.Write(
                "CE_Knife",
                $"ApplyEffectToTarget skipped effect={name} target={player.name}: PlayerEffects is missing");

            return;
        }

        GameLog.Write(
            "CE_Knife",
            $"ApplyEffectToTarget effect={name} target={player.name}: calling BlockAllCards duration={duration}");

        effects.BlockAllCards(
            duration);
    }
}