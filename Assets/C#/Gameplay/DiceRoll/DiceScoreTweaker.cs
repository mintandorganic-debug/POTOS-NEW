using System.Collections.Generic;
using UnityEngine;

// Turns each die to the required face without touching physics (original: TweakScore).
public class DiceScoreTweaker
{
    // 1 opposite 6, 2 opposite 5, 3 opposite 4: X-Y-Z- = 123, Z+Y+X+ = 456.
    private static readonly Vector3 DirOne = new Vector3(-1.0f, 0.0f, 0.0f);
    private static readonly Vector3 DirTwo = new Vector3(0.0f, 1.0f, 0.0f);
    private static readonly Vector3 DirThree = new Vector3(0.0f, 0.0f, -1.0f);
    private static readonly Vector3 DirFour = new Vector3(0.0f, 0.0f, 1.0f);
    private static readonly Vector3 DirFive = new Vector3(0.0f, -1.0f, 0.0f);
    private static readonly Vector3 DirSix = new Vector3(1.0f, 0.0f, 0.0f);

    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;

    public DiceScoreTweaker(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice)
    {
        this.settings = settings;
        this.dice = dice;
    }

    public void Tweak()
    {
        int count = 0;

        foreach (GameObject d in dice)
        {
            Vector3 RequiredFace = Vector3.zero;

            switch (settings.score[count])
            {
                case 1:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirOne) * settings.DieRadius;
                    break;
                case 2:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirTwo) * settings.DieRadius;
                    break;
                case 3:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirThree) * settings.DieRadius;
                    break;
                case 4:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirFour) * settings.DieRadius;
                    break;
                case 5:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirFive) * settings.DieRadius;
                    break;
                case 6:
                    RequiredFace = (d.transform.GetChild(0).rotation * DirSix) * settings.DieRadius;
                    break;
            }

            Quaternion TargetRot = Quaternion.FromToRotation(RequiredFace.normalized, Vector3.up);
            float SnapOrNot = 1f - Mathf.Clamp(d.GetComponent<Rigidbody>().linearVelocity.magnitude / settings.MinVelocity, 0f, 1f);

            // This turning algorithm does not affect physics.
            d.transform.GetChild(0).rotation = Quaternion.Slerp(d.transform.GetChild(0).rotation, TargetRot * d.transform.GetChild(0).rotation, SnapOrNot * settings.TweakForce);
            count += 1;
        }
    }
}
