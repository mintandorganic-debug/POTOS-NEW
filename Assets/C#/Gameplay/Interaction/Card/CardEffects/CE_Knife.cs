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
        if (player == null)
            return;

        PlayerEffects effects =
            player.GetComponent<PlayerEffects>();

        if (effects == null)
            return;

        effects.BlockAllCards(
            duration);
    }
}