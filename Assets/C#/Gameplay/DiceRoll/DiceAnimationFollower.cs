using System.Collections.Generic;
using UnityEngine;

// Dice follow the animated hand bones through physics (original: FollowAnimation).
public class DiceAnimationFollower
{
    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;
    private readonly IList<GameObject> controlBones;
    private readonly float[] forceMultipliers;
    private readonly Transform diceOrigin;
    private readonly Transform plate;
    private readonly IDiceAnimationState animationState;
    private readonly IDiceCharge charge;

    public DiceAnimationFollower(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice,
        IList<GameObject> controlBones,
        float[] forceMultipliers,
        Transform diceOrigin,
        Transform plate,
        IDiceAnimationState animationState,
        IDiceCharge charge)
    {
        this.settings = settings;
        this.dice = dice;
        this.controlBones = controlBones;
        this.forceMultipliers = forceMultipliers;
        this.diceOrigin = diceOrigin;
        this.plate = plate;
        this.animationState = animationState;
        this.charge = charge;
    }

    public void Follow()
    {
        int count = 0;

        foreach (GameObject d in dice)
        {
            if (animationState.IsCharging)
            {
                d.GetComponent<Rigidbody>().position = controlBones[count].transform.position;
                d.GetComponent<Rigidbody>().rotation = controlBones[count].transform.rotation;
            }
            else
            {
                float ChargeMult = Mathf.Lerp(settings.ChargeMin, settings.ChargeMax, charge.Charge);
                Vector3 Local = diceOrigin.InverseTransformPoint(controlBones[count].transform.position);
                Local = Vector3.Scale(Local, new Vector3(1f, 0.9f, ChargeMult * forceMultipliers[count]));
                Vector3 ScaledPos = diceOrigin.TransformPoint(Local);

                Vector3 FollowCenter = (ScaledPos - d.transform.position) / Time.fixedDeltaTime - d.GetComponent<Rigidbody>().linearVelocity;
                d.GetComponent<Rigidbody>().AddForceAtPosition(FollowCenter * settings.FollowWeight, d.transform.position + Vector3.up * settings.DieRadius * 0.5f, ForceMode.VelocityChange);

                if (Vector3.Scale((d.transform.position - plate.position), new Vector3(1f, 0f, 1f)).magnitude >= settings.PlateRadius)
                {
                    d.layer = LayerMask.NameToLayer("Ignore Raycast");
                    d.GetComponent<Collider>().enabled = false;
                }
                else
                {
                    d.layer = LayerMask.NameToLayer("DiceSimulation");
                    d.GetComponent<Collider>().enabled = true;
                }
            }

            count += 1;
        }
    }
}
