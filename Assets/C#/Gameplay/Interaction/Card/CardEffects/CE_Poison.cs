using UnityEngine;

[CreateAssetMenu(
    fileName = "CE_Poison",
    menuName = "Game/Card Effects/Poison")]
public class CE_Poison : CE_Base
{
    [SerializeField] private float duration = 10f;
    [SerializeField] private int swapCards = 2;

    public override void ApplyEffectToTarget(
        Player player)
    {
        if (player == null)
            return;

        PlayerEffects effects =
            player.GetComponentInChildren<PlayerEffects>(true);

        if (effects == null)
            return;

        effects.ApplyBlind(
            duration);
    }

    public override void ApplyEffect(
        Player attacker,
        Player target)
    {
        ApplyEffectToTarget(target);

        if (attacker == null ||
            target == null ||
            attacker.Hand == null ||
            target.Hand == null)
        {
            return;
        }

        attacker.Hand.OpenSwap(
            target.Hand,
            duration,
            swapCards);
    }
}
