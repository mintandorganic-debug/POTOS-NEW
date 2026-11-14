using System.Collections.Generic;
using UnityEngine;

// Checks the speed of the dice and the animation state (original: AllStopped).
public class DiceStopDetector : IDiceStopSource
{
    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;
    private readonly IDiceAnimationState animationState;
    private readonly bool[] diceStop = { true, true, true };

    public DiceStopDetector(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice,
        IDiceAnimationState animationState)
    {
        this.settings = settings;
        this.dice = dice;
        this.animationState = animationState;
    }

    public bool AllStopped()
    {
        // Preserved from the original: the counter is incremented after the loop,
        // so only diceStop[0] is written (by the last die in the list).
        int count = 0;

        foreach (GameObject d in dice)
        {
            if (d.gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude <= settings.MinVelocity)
            {
                diceStop[count] = true;
            }
            else
            {
                diceStop[count] = false;
            }
        }

        count += 1;

        if (diceStop[0] == true && diceStop[1] == true && diceStop[2] == true)
        {
            return animationState.IsAttackOrReady;
        }

        return false;
    }
}
