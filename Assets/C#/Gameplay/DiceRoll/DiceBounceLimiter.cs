using System.Collections.Generic;
using UnityEngine;

// Limits the bounce while a die moves up (original: LimitBounce).
public class DiceBounceLimiter
{
    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;

    public DiceBounceLimiter(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice)
    {
        this.settings = settings;
        this.dice = dice;
    }

    public void Limit()
    {
        foreach (GameObject d in dice)
        {
            Vector3 NewVel = d.GetComponent<Rigidbody>().linearVelocity;
            float LimitedVel = Mathf.Clamp(NewVel.y, 0f, settings.VerticalLimit);

            if (NewVel.y > 0f)
            {
                d.GetComponent<Rigidbody>().linearVelocity = new Vector3(NewVel.x, Mathf.Lerp(LimitedVel, NewVel.y, settings.FollowWeight), NewVel.z);

                if (d.GetComponent<Rigidbody>().linearVelocity.magnitude <= 0.01f)
                {
                    d.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                }
            }
        }
    }
}
