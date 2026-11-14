using UnityEngine;

public class AnimatorDiceState : IDiceAnimationState
{
    private readonly Animator anim;

    public AnimatorDiceState(
        Animator anim)
    {
        this.anim = anim;
    }

    public bool IsCharging =>
        anim.GetCurrentAnimatorStateInfo(0).IsName("DiceCharge");

    public bool IsAttackOrReady =>
        anim.GetCurrentAnimatorStateInfo(0).IsName("DiceAttack") ||
        anim.GetCurrentAnimatorStateInfo(0).IsName("DiceReady");
}
