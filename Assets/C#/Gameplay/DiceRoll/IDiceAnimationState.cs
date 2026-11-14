// Animation state of the hands as the dice logic needs it.
public interface IDiceAnimationState
{
    // Original: current state is "DiceCharge" (dice are glued to the hand bones).
    bool IsCharging { get; }

    // Original: current state is "DiceAttack" or "DiceReady" (dice may be considered stopped).
    bool IsAttackOrReady { get; }
}
