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
        if (player == null)
            return;

        PlayerEffects effects =
            player.GetComponent<PlayerEffects>();

        if (effects == null)
            return;

        effects.BlockRandomCard(
            duration);
    }
}